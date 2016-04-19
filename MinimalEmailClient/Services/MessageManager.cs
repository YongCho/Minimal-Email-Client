#undef TRACE
using MinimalEmailClient.Events;
using MinimalEmailClient.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MinimalEmailClient.Services
{
    public class MessageManager
    {
        public Dictionary<string, Message> MessagesDico;  // Key is the Message object unique id.
        private ConcurrentDictionary<string, int> abortLatches = new ConcurrentDictionary<string, int>();  // <account name, 0 or 1>; set to 1 to abort sync
        private ConcurrentDictionary<string, int> openSyncOps = new ConcurrentDictionary<string, int>();  // <account name, # of sync methods running>
        public event EventHandler<Message> MessageAdded;
        public event EventHandler<Message> MessageRemoved;
        public event EventHandler<Message> MessageModified;

        public string Error = string.Empty;
        private int downloadChunkSize = 50;

        private static readonly MessageManager instance = new MessageManager();
        public static MessageManager Instance
        {
            get { return instance; }
        }

        protected MessageManager()
        {
            LoadMessagesFromDb();
            GlobalEventAggregator.Instance.GetEvent<MailboxListSyncFinishedEvent>().Subscribe(HandleMailboxListSyncFinished);
            GlobalEventAggregator.Instance.GetEvent<AccountDeletedEvent>().Subscribe(HandleAccountDeleted);
        }

        private async void HandleAccountDeleted(string accountName)
        {
            abortLatches.AddOrUpdate(accountName, 1, (k, v) => v = 1);

            // Wait until all sync operations are stopped.
            // TODO: Is there a better way to check this?
            int syncOpsCount;
            int waitedMilisec = 0;
            while (openSyncOps.TryGetValue(accountName, out syncOpsCount) && syncOpsCount > 0)
            {
                Trace.WriteLine("# of open sync ops: " + syncOpsCount);
                await Task.Delay(1000);
                waitedMilisec += 1000;
                if (waitedMilisec > 10000)
                {
                    Trace.WriteLine("Sync Abort Timeout");
                    break;
                }
            }

            // Remove all messages associated with this account.
            List<string> keysToRemove = new List<string>();
            foreach (KeyValuePair<string, Message> pair in MessagesDico)
            {
                if (pair.Value.AccountName == accountName)
                {
                    keysToRemove.Add(pair.Key);
                }
            }
            foreach (string key in keysToRemove)
            {
                Message removedMsg = MessagesDico[key];
                MessagesDico.Remove(key);
                OnMessageRemoved(removedMsg);
            }
        }

        private void LoadMessagesFromDb()
        {
            if (MessagesDico == null)
            {
                MessagesDico = new Dictionary<string, Message>();
            }
            else
            {
                MessagesDico.Clear();
            }

            List<Message> dbMessages = DatabaseManager.GetMessages();
            foreach (Message msg in dbMessages)
            {
                MessagesDico.Add(msg.UniqueKeyString, msg);
            }
        }

        private void HandleMailboxListSyncFinished(Account account)
        {
            BeginSyncMessages(account);
        }

        protected virtual void OnMessageAdded(Message newMessage)
        {
            if (MessageAdded != null)
            {
                MessageAdded(this, newMessage);
            }
        }

        protected virtual void OnMessageRemoved(Message removedMessage)
        {
            if (MessageRemoved != null)
            {
                MessageRemoved(this, removedMessage);
            }
        }

        protected virtual void OnMessageModified(Message modifiedMessage)
        {
            if (MessageModified != null)
            {
                MessageModified(this, modifiedMessage);
            }
        }

        private void BeginSyncMessages(Account account)
        {
            Trace.WriteLine("BeginSyncMessages: " + account.AccountName);
            abortLatches.AddOrUpdate(account.AccountName, 0, (k, v) => v = 0);

            List<Mailbox> mailboxes = DatabaseManager.GetMailboxes(account.AccountName);

            for (int i = mailboxes.Count - 1; i >= 0; --i)
            {
                if (mailboxes[i].Flags.Contains(@"\Noselect"))
                {
                    mailboxes.RemoveAt(i);
                }
            }

            // Sync inbox immediately from a separate thread.
            foreach (Mailbox mailbox in mailboxes)
            {
                if (mailbox.MailboxName.ToLower() == "inbox")
                {
                    Task.Run(() => { SyncMessage(account, mailbox.DirectoryPath); });
                    BeginMonitor(account, mailbox.DirectoryPath);
                    mailboxes.Remove(mailbox);
                    break;
                }
            }

            // Sync the other mailboxes one at a time.
            List<string> mailboxNames = new List<string>();
            foreach (Mailbox mbox in mailboxes)
            {
                mailboxNames.Add(mbox.DirectoryPath);
            }
            SyncMessagesMultipleMailboxes(account, mailboxNames);
        }

        // Syncs messages one mailbox at a time.
        private async void SyncMessagesMultipleMailboxes(Account account, List<string> mailboxNames)
        {
            foreach (string mailboxName in mailboxNames)
            {
                await Task.Run(() => { SyncMessage(account, mailboxName); });
            }
        }

        private void SyncMessage(Account account, string mailboxName)
        {
            int abort = 0;
            if (abortLatches.TryGetValue(account.AccountName, out abort) && abort == 1)
            {
                // Abort sync.
                return;
            }

            ImapClient imapClient = new ImapClient(account);
            if (!imapClient.Connect())
            {
                Trace.WriteLine(imapClient.Error);
                return;
            }

            ExamineResult status;
            bool readOnly = true;
            if (!imapClient.SelectMailbox(mailboxName, readOnly, out status))
            {
                Trace.WriteLine(imapClient.Error);
                imapClient.Disconnect();
                return;
            }

            int localLargestUid = DatabaseManager.GetMaxUid(account.AccountName, mailboxName);

            // Download the recent message headers that have never been downloaded yet.
            int serverLargestUid = status.UidNext - 1;
            if (serverLargestUid > localLargestUid)
            {
                SyncNewMessageHeaders(account, mailboxName, imapClient, localLargestUid + 1, serverLargestUid);
            }

            // Sync changes to the existing message headers (flags, etc.).
            if (localLargestUid > 0)
            {
                SyncExistingMessageHeaders(account, mailboxName, imapClient, 1, localLargestUid);
            }

            imapClient.Disconnect();
        }

        private void SyncNewMessageHeaders(Account account, string mailboxName, ImapClient imapClientOnSelectedStatus, int firstUid, int lastUid)
        {
            Trace.WriteLine("SyncNewMessageHeaders: Mailbox(" + mailboxName + "), UIDs(" + firstUid + ", " + lastUid + ")");

            // Decrement this at every exit point.
            openSyncOps.AddOrUpdate(account.AccountName, 1, (k, v) => v + 1);

            int messagesCount = lastUid - firstUid + 1;
            int downloadChunk = this.downloadChunkSize;
            int startUid = lastUid - downloadChunk + 1;
            int endUid = lastUid;

            while (endUid >= firstUid)
            {
                int abort = 0;
                if (abortLatches.TryGetValue(account.AccountName, out abort) && abort == 1)
                {
                    Trace.WriteLine("Aborting sync...");
                    openSyncOps.AddOrUpdate(account.AccountName, 0, (k, v) => v - 1);
                    return;
                }

                if (startUid < firstUid)
                {
                    startUid = firstUid;
                }
                List<Message> msgs = imapClientOnSelectedStatus.FetchHeaders(startUid, endUid - startUid + 1, true);
                if (msgs.Count > 0)
                {
                    foreach (Message msg in msgs)
                    {
                        if (!MessagesDico.ContainsKey(msg.UniqueKeyString))
                        {
                            MessagesDico.Add(msg.UniqueKeyString, msg);
                            OnMessageAdded(msg);
                        }
                    }
                    DatabaseManager.StoreMessages(msgs);
                }

                endUid = startUid - 1;
                startUid = endUid - downloadChunk + 1;
            }

            openSyncOps.AddOrUpdate(account.AccountName, 0, (k, v) => v - 1);
        }

        private void SyncExistingMessageHeaders(Account account, string mailboxName, ImapClient imapClientOnSelectedStatus, int firstUid, int lastUid)
        {
            Trace.WriteLine("SyncExistingMessageHeaders: Mailbox(" + mailboxName + "), UIDs(" + firstUid + ", " + lastUid + ")");

            // Decrement this at every exit point.
            openSyncOps.AddOrUpdate(account.AccountName, 1, (k, v) => v + 1);

            // Delete all messages that have been deleted from server.
            List<int> serverUids = imapClientOnSelectedStatus.SearchUids("ALL");
            List<string> keysToDelete = new List<string>();
            foreach (KeyValuePair<string, Message> entry in MessagesDico)
            {
                Message msg = entry.Value;
                if (msg.AccountName == account.AccountName &&
                    msg.MailboxPath == mailboxName &&
                    !serverUids.Contains(msg.Uid))
                {
                    keysToDelete.Add(entry.Key);
                }
            }

            List<Message> msgsDeleted = new List<Message>();
            foreach (string key in keysToDelete)
            {
                Message msg = MessagesDico[key];
                MessagesDico.Remove(key);
                OnMessageRemoved(msg);
                msgsDeleted.Add(msg);
            }

            DatabaseManager.DeleteMessages(msgsDeleted);

            // Now synchronize the remaining messages.
            int messagesCount = lastUid - firstUid + 1;
            int downloadChunk = this.downloadChunkSize;
            int startUid = lastUid - downloadChunk + 1;
            int endUid = lastUid;

            while (endUid >= firstUid)
            {
                int abort = 0;
                if (abortLatches.TryGetValue(account.AccountName, out abort) && abort == 1)
                {
                    Trace.WriteLine("Aborting sync...");
                    openSyncOps.AddOrUpdate(account.AccountName, 0, (k, v) => v - 1);
                    return;
                }

                if (startUid < firstUid)
                {
                    startUid = firstUid;
                }
                List<Message> serverMsgs = imapClientOnSelectedStatus.FetchHeaders(startUid, endUid - startUid + 1, true);
                if (serverMsgs.Count > 0)
                {
                    List<Message> messagesAdded = new List<Message>();
                    foreach (Message serverMsg in serverMsgs)
                    {
                        Message localMsg;
                        if (MessagesDico.TryGetValue(serverMsg.UniqueKeyString, out localMsg))
                        {
                            // A local copy exists. Update its flags.
                            if (localMsg.FlagString != serverMsg.FlagString)
                            {
                                localMsg.FlagString = serverMsg.FlagString;
                                OnMessageModified(localMsg);
                            }
                        }
                        else
                        {
                            MessagesDico.Add(serverMsg.UniqueKeyString, serverMsg);
                            OnMessageAdded(serverMsg);
                            messagesAdded.Add(serverMsg);
                        }
                    }
                    DatabaseManager.StoreMessages(messagesAdded);
                }

                endUid = startUid - 1;
                startUid = endUid - downloadChunk + 1;
            }

            openSyncOps.AddOrUpdate(account.AccountName, 0, (k, v) => v - 1);
        }

        public void DeleteMessages(List<Message> messages)
        {
            // Delete from memory.
            foreach (Message msg in messages)
            {
                MessagesDico.Remove(msg.UniqueKeyString);
                OnMessageRemoved(msg);
            }

            // Delete from database.
            DatabaseManager.DeleteMessages(messages);

            // Delete from server.
            Task.Run(() => {
                while (messages.Count > 0)
                {
                    Account account = AccountManager.Instance.GetAccountByName(messages[0].AccountName);

                    List<Message> accountMessages = new List<Message>();
                    for (int i = messages.Count - 1; i >= 0; --i)
                    {
                        Message msg = messages[i];
                        if (msg.AccountName == account.AccountName)
                        {
                            accountMessages.Add(msg);
                            messages.RemoveAt(i);
                        }
                    }

                    ImapClient imapClient = new ImapClient(account);
                    if (imapClient.Connect())
                    {
                        imapClient.DeleteMessages(accountMessages);
                        imapClient.Disconnect();
                    }
                }
            });
        }

        private void BeginMonitor(Account account, string mailboxName)
        {
            Trace.WriteLine("BeginMonitor " + account.AccountName);
            ImapClient imapClient = new ImapClient(account);
            if (!imapClient.Connect())
            {
                Debug.WriteLine(imapClient.Error);
                return;
            }

            imapClient.NewMessageAtServer += HandleNewMessageAtServer;
            imapClient.MessageDeletedAtServer += HandleMessageDeletedAtServer;
            imapClient.MessageSeenAtServer += HandleMessageSeenAtServer;
            imapClient.MessageUnseenAtServer += HandleMessageUnseenAtServer;
            imapClient.BeginMonitor(mailboxName);
        }

        private void HandleNewMessageAtServer(object sender, Message newMessage)
        {
            if (!MessagesDico.ContainsKey(newMessage.UniqueKeyString))
            {
                MessagesDico.Add(newMessage.UniqueKeyString, newMessage);
                OnMessageAdded(newMessage);
                DatabaseManager.StoreMessage(newMessage);
            }
        }

        private void HandleMessageDeletedAtServer(object sender, ImapMonitorEventArgs e)
        {
            string messageKey = Message.CreateUniqueKeyString(e.AccountName, e.MailboxName, e.Uid);
            Message msg;
            if (MessagesDico.TryGetValue(messageKey, out msg))
            {
                MessagesDico.Remove(messageKey);
                OnMessageRemoved(msg);
                DatabaseManager.DeleteMessage(msg);
            }
        }

        private void HandleMessageSeenAtServer(object sender, ImapMonitorEventArgs e)
        {
            string messageKey = Message.CreateUniqueKeyString(e.AccountName, e.MailboxName, e.Uid);
            Message msg;
            if (MessagesDico.TryGetValue(messageKey, out msg) && !msg.IsSeen)
            {
                msg.IsSeen = true;
                OnMessageModified(msg);
                DatabaseManager.Update(msg);
            }
        }

        private void HandleMessageUnseenAtServer(object sender, ImapMonitorEventArgs e)
        {
            string messageKey = Message.CreateUniqueKeyString(e.AccountName, e.MailboxName, e.Uid);
            Message msg;
            if (MessagesDico.TryGetValue(messageKey, out msg) && msg.IsSeen)
            {
                msg.IsSeen = false;
                OnMessageModified(msg);
                DatabaseManager.Update(msg);
            }
        }

    }
}
