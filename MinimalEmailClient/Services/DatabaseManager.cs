﻿#undef TRACE
using MinimalEmailClient.Common;
using MinimalEmailClient.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;
using System.Diagnostics;
using System.IO;

namespace MinimalEmailClient.Services
{
    public static class DatabaseManager
    {
        private static readonly string DatabaseFolder = Globals.UserSettingsFolder;
        private static readonly string DatabasePath = Path.Combine(DatabaseFolder, Properties.Settings.Default.DatabaseFileName);

        private static readonly string connString = string.Format("Case Sensitive=True;Data Source={0}", DatabasePath);
        // Manually increment this when you want to recreate the database (maybe you changed the schema?).
        private static readonly int schemaVersion = 16;

        public static bool Initialize()
        {
            if (!IsSchemaCurrent())
            {
                return CreateDatabase();
            }

            return true;
        }

        private static bool DatabaseExists()
        {
            return File.Exists(DatabasePath);
        }

        // Checks if the user's database schema matches the current development schema.
        private static bool IsSchemaCurrent()
        {
            Trace.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            if (!DatabaseExists())
            {
                return false;
            }

            bool retVal = false;
            using (SqlCeConnection conn = new SqlCeConnection(connString))
            {
                conn.Open();
                using (SqlCeCommand cmd = conn.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = @"SELECT EntryValue FROM DbInfo WHERE EntryName = 'SchemaVersion';";
                        object result = cmd.ExecuteScalar();
                        retVal = schemaVersion == Convert.ToInt32(result);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }

            return retVal;
        }

        // Deletes the database (if it exists) and creates a new empty one.
        public static bool CreateDatabase()
        {
            Trace.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            try
            {
                if (!Directory.Exists(DatabaseFolder))
                    Directory.CreateDirectory(DatabaseFolder);
                else
                    File.Delete(DatabasePath);

                SqlCeEngine engine = new SqlCeEngine(connString);
                engine.CreateDatabase();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }

            bool success = true;

            using (SqlCeConnection conn = new SqlCeConnection(connString))
            {
                conn.Open();
                using (SqlCeCommand cmd = conn.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = @"CREATE TABLE Accounts (AccountName NVARCHAR(50) PRIMARY KEY, EmailAddress NVARCHAR(50), ImapLoginName NVARCHAR(50), ImapLoginPassword NVARCHAR(50), ImapServerName NVARCHAR(50), ImapPortNumber INT, SmtpLoginName NVARCHAR(50), SmtpLoginPassword NVARCHAR(50), SmtpServerName NVARCHAR(50), SmtpPortNumber INT);";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = @"CREATE TABLE Mailboxes (AccountName NVARCHAR(50) REFERENCES Accounts(AccountName) ON DELETE CASCADE ON UPDATE CASCADE, Path NVARCHAR(100), Separator NVARCHAR(5), UidNext INT, UidValidity INT, FlagString NVARCHAR(500), PRIMARY KEY (AccountName, Path));";
                        cmd.ExecuteNonQuery();

                        // NVARCHAR would not work for the Recipient column because some email messages have many recipients. I've seen messages with 'Recipient' header containing up to 26000+ characters.
                        cmd.CommandText = @"CREATE TABLE Messages (AccountName NVARCHAR(50), MailboxPath NVARCHAR(100), Uid INT, Subject NVARCHAR(500), DateString NVARCHAR(500), Sender NVARCHAR(500), Recipient NTEXT, FlagString NVARCHAR(500), HasAttachment BIT, Body NTEXT, PRIMARY KEY (AccountName, MailboxPath, Uid), FOREIGN KEY (AccountName, MailboxPath) REFERENCES Mailboxes(AccountName, Path) ON DELETE CASCADE ON UPDATE CASCADE);";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = @"CREATE TABLE DbInfo (EntryName NVARCHAR(50) PRIMARY KEY, EntryValue NVARCHAR(500));";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = @"CREATE TABLE Contacts (AccountName NVARCHAR(50), EmailAddress NVARCHAR(50));";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = @"INSERT INTO DbInfo VALUES('SchemaVersion', @SchemaVersion);";
                        cmd.Parameters.AddWithValue("@SchemaVersion", schemaVersion);
                        cmd.Prepare();
                        if (cmd.ExecuteNonQuery() != 1)
                        {
                            success = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Unable to create database.\n\n" + ex.Message);
                        success = false;
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }

            return success;
        }

        // Loads all accounts from Accounts table and returns them as a list of Account objects.
        public static List<Account> GetAccounts()
        {
            Trace.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            List<Account> accounts = new List<Account>();

            if (!DatabaseExists())
            {
                CreateDatabase();
            }
            else
            {
                using (SqlCeConnection conn = new SqlCeConnection(connString))
                {
                    conn.Open();
                    using (SqlCeCommand cmd = conn.CreateCommand())
                    {
                        try
                        {
                            cmd.CommandText = @"SELECT * FROM Accounts;";
                            using (SqlCeDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    Account account = new Account();
                                    account.AccountName = (string)reader["AccountName"];
                                    account.EmailAddress = (string)reader["EmailAddress"];
                                    account.ImapLoginName = (string)reader["ImapLoginName"];
                                    account.ImapLoginPassword = (string)reader["ImapLoginPassword"];
                                    account.ImapServerName = (string)reader["ImapServerName"];
                                    account.ImapPortNumber = (int)reader["ImapPortNumber"];
                                    account.SmtpLoginName = (string)reader["SmtpLoginName"];
                                    account.SmtpLoginPassword = (string)reader["SmtpLoginPassword"];
                                    account.SmtpServerName = (string)reader["SmtpServerName"];
                                    account.SmtpPortNumber = (int)reader["SmtpPortNumber"];
                                    accounts.Add(account);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                        finally
                        {
                            conn.Close();
                        }
                    }
                }
            }

            return accounts;
        }

        public static bool InsertAccount(Account account)
        {
            string ignoredErrorMsg;
            return InsertAccount(account, out ignoredErrorMsg);
        }

        // Stores an Account object into the Accounts table.
        public static bool InsertAccount(Account account, out string errorMsg)
        {
            Trace.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            errorMsg = string.Empty;

            if (!DatabaseExists())
            {
                CreateDatabase();
            }

            int numRowsInserted = 0;

            using (SqlCeConnection conn = new SqlCeConnection(connString))
            {
                conn.Open();
                using (SqlCeCommand cmd = conn.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = @"INSERT INTO Accounts VALUES(@AccountName, @EmailAddress, @ImapLoginName, @ImapLoginPassword, @ImapServerName, @ImapPortNumber, @SmtpLoginName, @SmtpLoginPassword, @SmtpServerName, @SmtpPortNumber);";

                        cmd.Parameters.AddWithValue("@AccountName", account.AccountName);
                        cmd.Parameters.AddWithValue("@EmailAddress", account.EmailAddress);
                        cmd.Parameters.AddWithValue("@ImapLoginName", account.ImapLoginName);
                        cmd.Parameters.AddWithValue("@ImapLoginPassword", account.ImapLoginPassword);
                        cmd.Parameters.AddWithValue("@ImapServerName", account.ImapServerName);
                        cmd.Parameters.AddWithValue("@ImapPortNumber", account.ImapPortNumber);
                        cmd.Parameters.AddWithValue("@SmtpLoginName", account.SmtpLoginName);
                        cmd.Parameters.AddWithValue("@SmtpLoginPassword", account.SmtpLoginPassword);
                        cmd.Parameters.AddWithValue("@SmtpServerName", account.SmtpServerName);
                        cmd.Parameters.AddWithValue("@SmtpPortNumber", account.SmtpPortNumber);
                        cmd.Prepare();

                        numRowsInserted = cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        errorMsg = ex.Message;
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }

            return numRowsInserted == 1 ? true : false;
        }

        // Deletes an account from Accounts table and also deletes all mailboxes and messages
        // associated with the account.
        public static bool DeleteAccount(Account account, out string errorMsg)
        {
            Trace.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            errorMsg = string.Empty;
            if (!DatabaseExists())
            {
                errorMsg = "Unable to locate database.";
                return false;
            }

            int numRowsDeleted = 0;

            using (SqlCeConnection conn = new SqlCeConnection(connString))
            {
                conn.Open();
                using (SqlCeCommand cmd = conn.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = "DELETE FROM Accounts WHERE AccountName = @AccountName;";
                        cmd.Parameters.AddWithValue("@AccountName", account.AccountName);
                        cmd.Prepare();
                        numRowsDeleted = cmd.ExecuteNonQuery();

                        // Following two queries should not be necessary since we are using foreign key restrictions on
                        // Mailboxes and Messages tables' AccountName attributes with DELETE ON UPDATE policy.
                        // I am executing them here anyway in case we decide to change the foreign key policy sometime later.
                        cmd.CommandText = "DELETE FROM Mailboxes WHERE AccountName = @AccountName;";
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@AccountName", account.AccountName);
                        cmd.Prepare();
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = "DELETE FROM Messages WHERE AccountName = @AccountName;";
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@AccountName", account.AccountName);
                        cmd.Prepare();
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        errorMsg = ex.Message;
                    }
                    finally
                    {
                        conn.Close();
                    }
                }

                return numRowsDeleted == 1;
            }
        }

        public static List<string> GetContacts(string user)
        {
            Trace.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            List<string> contacts = new List<string>();

            if (!DatabaseExists())
            {
                CreateDatabase();
            }
            else
            {
                using (SqlCeConnection conn = new SqlCeConnection(connString))
                {
                    conn.Open();
                    using (SqlCeCommand cmd = conn.CreateCommand())
                    {
                        try
                        {
                            cmd.CommandText = @"SELECT * FROM Contacts WHERE AccountName = @AccountName;";
                            cmd.Parameters.AddWithValue("@AccountName", user);
                            using (SqlCeDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    contacts.Add((string)reader["EmailAddress"]);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                        finally
                        {
                            conn.Close();
                        }
                    }
                }
            }

            return contacts;
        }

        public static bool ContactExists(string user, string newContact)
        {
            Trace.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            List<string> contacts = new List<string>();

            if (!DatabaseExists())
            {
                CreateDatabase();
            }
            else
            {
                using (SqlCeConnection conn = new SqlCeConnection(connString))
                {
                    conn.Open();
                    using (SqlCeCommand cmd = conn.CreateCommand())
                    {
                        try
                        {
                            cmd.CommandText = @"SELECT * FROM Contacts;";
                            using (SqlCeDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    if ( newContact == (string)reader["EmailAddress"]  && user == (string)reader["AccountName"])
                                        return true;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                        finally
                        {
                            conn.Close();
                        }
                    }
                }
            }

            return false;
        }

        public static bool InsertContact(string user, string newContact)
        {
            if (ContactExists(user, newContact))
            {
                return false;
            }
            string ignoredErrorMsg;
            return InsertContact(user, newContact, out ignoredErrorMsg);
        }

        // Stores an Account object into the Accounts table.
        public static bool InsertContact(string user, string newContact, out string errorMsg)
        {
            Trace.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            errorMsg = string.Empty;

            if (!DatabaseExists())
            {
                CreateDatabase();
            }

            int numRowsInserted = 0;

            using (SqlCeConnection conn = new SqlCeConnection(connString))
            {
                conn.Open();
                using (SqlCeCommand cmd = conn.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = @"INSERT INTO Contacts VALUES(@AccountName, @EmailAddress);";
                        cmd.Parameters.AddWithValue("@AccountName", user);
                        cmd.Parameters.AddWithValue("@EmailAddress", newContact);
                        cmd.Prepare();

                        numRowsInserted = cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        errorMsg = ex.Message;
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }

            return numRowsInserted == 1 ? true : false;
        }

        // Loads all mailboxes from the Mailboxes table and returns them as a list of Mailbox objects.
        public static List<Mailbox> GetMailboxes(string accountName)
        {
            Trace.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            List<Mailbox> mailboxes = new List<Mailbox>();

            if (!DatabaseExists())
            {
                CreateDatabase();
                return mailboxes;
            }
            else
            {
                using (SqlCeConnection conn = new SqlCeConnection(connString))
                {
                    conn.Open();
                    using (SqlCeCommand cmd = conn.CreateCommand())
                    {
                        try
                        {
                            cmd.CommandText = @"SELECT * FROM Mailboxes WHERE AccountName = @AccountName ORDER BY Path;";
                            cmd.Parameters.AddWithValue("@AccountName", accountName);
                            cmd.Prepare();
                            using (SqlCeDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    Mailbox mailbox = new Mailbox();
                                    mailbox.AccountName = accountName;
                                    mailbox.DirectoryPath = (string)reader["Path"];
                                    mailbox.PathSeparator = (string)reader["Separator"];
                                    mailbox.UidNext = (int)reader["UidNext"];
                                    mailbox.UidValidity = (int)reader["UidValidity"];
                                    mailbox.FlagString = (string)reader["FlagString"];

                                    mailboxes.Add(mailbox);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                        finally
                        {
                            conn.Close();
                        }
                    }
                }
            }

            return mailboxes;
        }

        public static int DeleteMailboxes(List<Mailbox> mailboxes)
        {
            string ignoredErrorMsg;
            return DeleteMailboxes(mailboxes, out ignoredErrorMsg);
        }

        // Returns the number of mailboxes deleted.
        public static int DeleteMailboxes(List<Mailbox> mailboxes, out string errorMsg)
        {
            Trace.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            errorMsg = string.Empty;
            if (!DatabaseExists())
            {
                CreateDatabase();
            }

            int numRowsDeleted = 0;

            using (SqlCeConnection conn = new SqlCeConnection(connString))
            {
                conn.Open();
                using (SqlCeCommand cmd = conn.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = "DELETE FROM Mailboxes WHERE AccountName = @AccountName AND Path = @DirectoryPath;";
                        foreach (Mailbox mbox in mailboxes)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@AccountName", mbox.AccountName);
                            cmd.Parameters.AddWithValue("@DirectoryPath", mbox.DirectoryPath);
                            cmd.Prepare();
                            numRowsDeleted += cmd.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        errorMsg = ex.Message;
                    }
                    finally
                    {
                        conn.Close();
                    }
                }

                return numRowsDeleted;
            }
        }

        public static int InsertMailboxes(List<Mailbox> mailboxes)
        {
            string ignoredErrorMsg;
            return InsertMailboxes(mailboxes, out ignoredErrorMsg);
        }

        // Returns the number of mailboxes inserted.
        public static int InsertMailboxes(List<Mailbox> mailboxes, out string errorMsg)
        {
            Trace.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            errorMsg = string.Empty;
            if (!DatabaseExists())
            {
                CreateDatabase();
            }

            int numRowsInserted = 0;

            using (SqlCeConnection conn = new SqlCeConnection(connString))
            {
                conn.Open();
                using (SqlCeCommand cmd = conn.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = "INSERT INTO Mailboxes VALUES(@AccountName, @Path, @Separator, @UidNext, @UidValidity, @FlagString);";

                        foreach (Mailbox mailbox in mailboxes)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@AccountName", mailbox.AccountName);
                            cmd.Parameters.AddWithValue("@Path", mailbox.DirectoryPath);
                            cmd.Parameters.AddWithValue("@Separator", mailbox.PathSeparator);
                            cmd.Parameters.AddWithValue("@UidNext", mailbox.UidNext);
                            cmd.Parameters.AddWithValue("@UidValidity", mailbox.UidValidity);
                            cmd.Parameters.AddWithValue("@FlagString", mailbox.FlagString);
                            cmd.Prepare();

                            numRowsInserted += cmd.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        errorMsg = ex.Message;
                    }
                    finally
                    {
                        conn.Close();
                    }
                }

                return numRowsInserted;
            }
        }

        public static int GetMaxUid(string accountName, string mailboxPath)
        {
            Trace.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            int maxUid = 0;

            if (!DatabaseExists())
            {
                CreateDatabase();
                return 0;
            }
            else
            {
                using (SqlCeConnection conn = new SqlCeConnection(connString))
                {
                    conn.Open();
                    using (SqlCeCommand cmd = conn.CreateCommand())
                    {
                        try
                        {
                            cmd.CommandText = @"SELECT MAX(Uid) from Messages WHERE AccountName = @AccountName AND MailboxPath = @MailboxPath;";
                            cmd.Parameters.AddWithValue("@AccountName", accountName);
                            cmd.Parameters.AddWithValue("@MailboxPath", mailboxPath);
                            cmd.Prepare();

                            object result = cmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                maxUid = Convert.ToInt32(result);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                        finally
                        {
                            conn.Close();
                        }
                    }
                }
            }

            return maxUid;
        }

        public static int GetMinUid(string accountName, string mailboxPath)
        {
            Trace.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            int minUid = 0;

            if (!DatabaseExists())
            {
                CreateDatabase();
                return 0;
            }
            else
            {
                using (SqlCeConnection conn = new SqlCeConnection(connString))
                {
                    conn.Open();
                    using (SqlCeCommand cmd = conn.CreateCommand())
                    {
                        try
                        {
                            cmd.CommandText = @"SELECT MIN(Uid) from Messages WHERE AccountName = @AccountName AND MailboxPath = @MailboxPath;";
                            cmd.Parameters.AddWithValue("@AccountName", accountName);
                            cmd.Parameters.AddWithValue("@MailboxPath", mailboxPath);
                            cmd.Prepare();

                            object result = cmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                minUid = Convert.ToInt32(result);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                        finally
                        {
                            conn.Close();
                        }
                    }
                }
            }

            return minUid;
        }

        // Loads all messages from Messages table and returns them as a list of Message objects.
        public static List<Message> GetMessages()
        {
            Trace.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            List<Message> messages = new List<Message>();

            if (!DatabaseExists())
            {
                CreateDatabase();
            }
            else
            {
                using (SqlCeConnection conn = new SqlCeConnection(connString))
                {
                    conn.Open();
                    using (SqlCeCommand cmd = conn.CreateCommand())
                    {
                        try
                        {
                            cmd.CommandText = @"SELECT * FROM Messages;";
                            cmd.Prepare();
                            using (SqlCeDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    Message message = new Message();
                                    message.AccountName = (string)reader["AccountName"];
                                    message.MailboxPath = (string)reader["MailboxPath"];
                                    message.Uid = (int)reader["Uid"];
                                    message.Subject = (string)reader["Subject"];
                                    message.DateString = (string)reader["DateString"];
                                    message.Sender = (string)reader["Sender"];
                                    message.Recipient = (string)reader["Recipient"];
                                    message.FlagString = (string)reader["FlagString"];
                                    message.HasAttachment = (bool)reader["HasAttachment"];
                                    message.Body = (string)reader["Body"];

                                    messages.Add(message);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                        finally
                        {
                            conn.Close();
                        }
                    }
                }
            }

            return messages;
        }

        public static int StoreMessage(Message msg)
        {
            string ignoredErrorMsg;
            return StoreMessage(msg, out ignoredErrorMsg);
        }

        public static int StoreMessage(Message msg, out string errorMsg)
        {
            List<Message> wrapperList = new List<Message>();
            wrapperList.Add(msg);
            return StoreMessages(wrapperList, out errorMsg);
        }

        public static int StoreMessages(List<Message> messages)
        {
            string ignoredErrorMsg;
            return StoreMessages(messages, out ignoredErrorMsg);
        }

        // Stores the messages in the Messages table.
        public static int StoreMessages(List<Message> messages, out string errorMsg)
        {
            Trace.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            errorMsg = string.Empty;
            if (!DatabaseExists())
            {
                CreateDatabase();
            }

            int numRowsInserted = 0;

            using (SqlCeConnection conn = new SqlCeConnection(connString))
            {
                conn.Open();
                using (SqlCeCommand cmd = conn.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = "INSERT INTO Messages VALUES(@AccountName, @MailboxPath, @Uid, @Subject, @DateString, @Sender, @Recipient, @FlagString, @HasAttachment, @Body);";

                        foreach (Message msg in messages)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@AccountName", msg.AccountName);
                            cmd.Parameters.AddWithValue("@MailboxPath", msg.MailboxPath);
                            cmd.Parameters.AddWithValue("@Uid", msg.Uid);
                            cmd.Parameters.AddWithValue("@Subject", msg.Subject);
                            cmd.Parameters.AddWithValue("@DateString", msg.DateString);
                            cmd.Parameters.AddWithValue("@Sender", msg.Sender);
                            cmd.Parameters.AddWithValue("@Recipient", msg.Recipient);
                            cmd.Parameters.AddWithValue("@FlagString", msg.FlagString);
                            cmd.Parameters.AddWithValue("@HasAttachment", msg.HasAttachment);
                            cmd.Parameters.AddWithValue("@Body", msg.Body);
                            cmd.Prepare();

                            numRowsInserted = cmd.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        errorMsg = ex.Message;
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }

            return numRowsInserted;
        }

        public static int DeleteMessage(Message message)
        {
            string ignoredErrorMsg;
            return DeleteMessage(message, out ignoredErrorMsg);
        }

        public static int DeleteMessage(Message message, out string errorMsg)
        {
            List<Message> wrap = new List<Message>();
            wrap.Add(message);
            return DeleteMessages(wrap, out errorMsg);
        }

        public static int DeleteMessages(List<Message> messages)
        {
            string ignoredErrorMsg;
            return DeleteMessages(messages, out ignoredErrorMsg);
        }

        public static int DeleteMessages(List<Message> messages, out string errorMsg)
        {
            Trace.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            errorMsg = string.Empty;

            if (!DatabaseExists())
            {
                CreateDatabase();
                return 0;
            }

            int numRowsDeleted = 0;

            using (SqlCeConnection conn = new SqlCeConnection(connString))
            {
                conn.Open();
                using (SqlCeCommand cmd = conn.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = "DELETE FROM Messages WHERE AccountName = @AccountName AND MailboxPath = @MailboxPath AND Uid = @Uid;";

                        foreach (Message message in messages)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@AccountName", message.AccountName);
                            cmd.Parameters.AddWithValue("@MailboxPath", message.MailboxPath);
                            cmd.Parameters.AddWithValue("@Uid", message.Uid);
                            cmd.Prepare();
                            numRowsDeleted += cmd.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        errorMsg = ex.Message;
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }

            return numRowsDeleted;
        }

        public static int Update(Message message)
        {
            string ignoredErrorMsg;
            return Update(message, out ignoredErrorMsg);
        }

        public static int Update(Message message, out string errorMsg)
        {
            List<Message> msgList = new List<Message>();
            msgList.Add(message);
            return Update(msgList, out errorMsg);
        }

        public static int Update(List<Message> messages)
        {
            string ignoredErrorMsg;
            return Update(messages, out ignoredErrorMsg);
        }

        public static int Update(List<Message> messages, out string errorMsg)
        {
            Trace.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            errorMsg = string.Empty;
            if (!DatabaseExists())
            {
                CreateDatabase();
                return 0;
            }

            int numRowsUpdated = 0;

            using (SqlCeConnection conn = new SqlCeConnection(connString))
            {
                conn.Open();
                using (SqlCeCommand cmd = conn.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = "Update Messages SET Subject = @Subject, DateString = @DateString, Sender = @Sender, Recipient = @Recipient, FlagString = @FlagString, Body = @Body WHERE AccountName = @AccountName AND MailboxPath = @MailboxPath AND Uid = @Uid;";

                        foreach (Message message in messages)
                        {
                            cmd.Parameters.Clear();

                            cmd.Parameters.AddWithValue("@Subject", message.Subject);
                            cmd.Parameters.AddWithValue("@DateString", message.DateString);
                            cmd.Parameters.AddWithValue("@Sender", message.Sender);
                            cmd.Parameters.AddWithValue("@Recipient", message.Recipient);
                            cmd.Parameters.AddWithValue("@FlagString", message.FlagString);
                            cmd.Parameters.AddWithValue("@Body", message.Body);

                            cmd.Parameters.AddWithValue("@AccountName", message.AccountName);
                            cmd.Parameters.AddWithValue("@MailboxPath", message.MailboxPath);
                            cmd.Parameters.AddWithValue("@Uid", message.Uid);
                            cmd.Prepare();

                            numRowsUpdated += cmd.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        errorMsg = ex.Message;
                    }
                    finally
                    {
                        conn.Close();
                    }
                }

                return numRowsUpdated;
            }
        }

        public static int Update(Mailbox mailbox)
        {
            string ignoredErrorMsg;
            return Update(mailbox, out ignoredErrorMsg);
        }

        public static int Update(Mailbox mailbox, out string errorMsg)
        {
            List<Mailbox> mailboxList = new List<Mailbox>();
            mailboxList.Add(mailbox);
            return Update(mailboxList, out errorMsg);
        }

        public static int Update(List<Mailbox> mailboxes)
        {
            string ignoredErrorMsg;
            return Update(mailboxes, out ignoredErrorMsg);
        }

        public static int Update(List<Mailbox> mailboxes, out string errorMsg)
        {
            errorMsg = string.Empty;
            throw new NotImplementedException();
        }

        public static int Update(Account account)
        {
            string ignoredErrorMsg;
            return Update(account, out ignoredErrorMsg);
        }

        public static int Update(Account account, out string errorMsg)
        {
            List<Account> accountList = new List<Account>();
            accountList.Add(account);
            return Update(accountList, out errorMsg);
        }

        public static int Update(List<Account> accounts)
        {
            string ignoredErrorMsg;
            return Update(accounts, out ignoredErrorMsg);
        }

        public static int Update(List<Account> accounts, out string errorMsg)
        {
            errorMsg = string.Empty;
            throw new NotImplementedException();
        }

    }
}
