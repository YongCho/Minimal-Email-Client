// #undef TRACE
using MinimalEmailClient.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MinimalEmailClient.Models
{
    public class MessageManager
    {
        public List<Message> Messages;
        public string Error = string.Empty;
        private ConcurrentDictionary<string, int> abortLatches = new ConcurrentDictionary<string, int>();  // set to 1 to abort sync
        private ConcurrentDictionary<string, int> openSyncOps = new ConcurrentDictionary<string, int>();  // # of sync methods running
        public event EventHandler<Message> MessageAdded;
        public event EventHandler<Message> MessageRemoved;

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
                    Trace.WriteLine("Timeout");
                    break;
                }
            }

            // Remove all messages associated with this account.
            Trace.WriteLine("Deleting messages for account " + accountName);
            for (int i = Messages.Count - 1; i >= 0; --i)
            {
                if (Messages[i].AccountName == accountName)
                {
                    Message removedMsg = Messages[i];
                    Messages.RemoveAt(i);
                    OnMessageRemoved(removedMsg);
                }
            }
        }

        private void LoadMessagesFromDb()
        {
            if (Messages == null)
            {
                Messages = new List<Message>();
            }
            else
            {
                Messages.Clear();
            }

            Messages = DatabaseManager.GetMessages();
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

        public void BeginSyncMessages(Account account)
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
                if (mailbox.DisplayName.ToLower() == "inbox")
                {
                    Task.Run(() => { SyncMessage(account, mailbox.DirectoryPath); });
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

            // Download recent messages.
            int localLargestUid = DatabaseManager.GetMaxUid(account.AccountName, mailboxName);
            int serverLargestUid = status.UidNext - 1;
            if (serverLargestUid > localLargestUid)
            {
                SyncHelper(account, mailboxName, imapClient, localLargestUid + 1, serverLargestUid);
            }

            // Download old messages. (Maybe the program shut down previously in the middle of downloading?)
            int localSmallestUid = DatabaseManager.GetMinUid(account.AccountName, mailboxName);
            if (localSmallestUid > 1)
            {
                SyncHelper(account, mailboxName, imapClient, 1, localSmallestUid - 1);
            }

            imapClient.Disconnect();
        }

        private void SyncHelper(Account account, string mailboxName, ImapClient imapClient, int firstUid, int lastUid)
        {
            Trace.WriteLine("SyncHelper: Mailbox(" + mailboxName + "), UIDs(" + firstUid + ", " + lastUid + ")");

            // Decrement this at every exit point.
            openSyncOps.AddOrUpdate(account.AccountName, 1, (k, v) => v + 1);

            int messagesCount = lastUid - firstUid + 1;
            int downloadChunk = 30;
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
                List<Message> msgs = imapClient.FetchHeaders(startUid, endUid - startUid + 1, true);
                if (msgs.Count > 0)
                {
                    foreach (Message msg in msgs)
                    {
                        msg.AccountName = account.AccountName;
                        msg.MailboxPath = mailboxName;
                        Messages.Add(msg);
                        OnMessageAdded(msg);
                    }
                    DatabaseManager.StoreMessages(msgs);
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
                Messages.Remove(msg);
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

    }
}
