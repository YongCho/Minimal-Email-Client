#undef TRACE
using System.Collections.Generic;
using System.IO;
using System.Data.SqlServerCe;
using System;
using MinimalEmailClient.Common;
using System.Diagnostics;

namespace MinimalEmailClient.Models
{
    public static class DatabaseManager
    {
        public static readonly string DatabaseFolder = Globals.UserSettingsFolder;
        public static readonly string DatabasePath = Path.Combine(DatabaseFolder, Properties.Settings.Default.DatabaseFileName);

        private static string connString;
        // Manually increment this when you want to recreate the database (maybe you changed the schema?).
        private static readonly int schemaVersion = 13;

        public static bool Initialize()
        {
            SqlCeConnectionStringBuilder connBuilder = new SqlCeConnectionStringBuilder();
            connBuilder["Data Source"] = DatabasePath;
            connBuilder["Case Sensitive"] = true;

            connString = connBuilder.ConnectionString;

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
        public static bool IsSchemaCurrent()
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

                using (SqlCeCommand cmd = new SqlCeCommand())
                {
                    try
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = @"SELECT EntryValue FROM DbInfo WHERE EntryName = 'SchemaVersion';";
                        object result = cmd.ExecuteScalar();
                        retVal = schemaVersion == Convert.ToInt32(result);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message);
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
                Trace.WriteLine(ex.Message);
                return false;
            }

            using (SqlCeConnection conn = new SqlCeConnection(connString))
            {
                using (SqlCeCommand cmd = new SqlCeCommand())
                {
                    try
                    {
                        conn.Open();
                        cmd.Connection = conn;

                        cmd.CommandText = @"CREATE TABLE Accounts (AccountName NVARCHAR(50) PRIMARY KEY, EmailAddress NVARCHAR(50), ImapLoginName NVARCHAR(50), ImapLoginPassword NVARCHAR(50), ImapServerName NVARCHAR(50), ImapPortNumber INT, SmtpLoginName NVARCHAR(50), SmtpLoginPassword NVARCHAR(50), SmtpServerName NVARCHAR(50), SmtpPortNumber INT);";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = @"CREATE TABLE Mailboxes (AccountName NVARCHAR(50) REFERENCES Accounts(AccountName) ON DELETE CASCADE ON UPDATE CASCADE, Path NVARCHAR(100), Separator NVARCHAR(5), UidNext INT, UidValidity INT, FlagString NVARCHAR(500), PRIMARY KEY (AccountName, Path));";
                        cmd.ExecuteNonQuery();

                        // NVARCHAR would not work for the Recipient column because some email messages have many recipients. I've seen messages with 'Recipient' header containing up to 26000+ characters.
                        cmd.CommandText = @"CREATE TABLE Messages (AccountName NVARCHAR(50), MailboxPath NVARCHAR(100), Uid INT, Subject NVARCHAR(500), DateString NVARCHAR(500), Sender NVARCHAR(500), Recipient NTEXT, FlagString NVARCHAR(500), Body NTEXT, PRIMARY KEY (AccountName, MailboxPath, Uid), FOREIGN KEY (AccountName, MailboxPath) REFERENCES Mailboxes(AccountName, Path) ON DELETE CASCADE ON UPDATE CASCADE);";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = @"CREATE TABLE DbInfo (EntryName NVARCHAR(50) PRIMARY KEY, EntryValue NVARCHAR(500));";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = @"INSERT INTO DbInfo VALUES('SchemaVersion', @SchemaVersion);";
                        cmd.Parameters.AddWithValue("@SchemaVersion", schemaVersion);
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine("Unable to create database.\n\n" + ex.Message);
                        return false;
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }

            return true;
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
                using (SqlCeConnection dbConnection = new SqlCeConnection(connString))
                {
                    dbConnection.Open();
                    string cmdString = @"SELECT * FROM Accounts;";
                    using (SqlCeCommand cmd = new SqlCeCommand(cmdString, dbConnection))
                    {
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
                    dbConnection.Close();
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
            int numRowsInserted = 0;

            if (!DatabaseExists())
            {
                CreateDatabase();
            }

            using (SqlCeConnection dbConnection = new SqlCeConnection(connString))
            {
                dbConnection.Open();
                using (SqlCeCommand cmd = new SqlCeCommand())
                {
                    cmd.Connection = dbConnection;
                    cmd.CommandText = @"INSERT INTO Accounts VALUES(@AccountName, @EmailAddress, @ImapLoginName, @ImapLoginPassword, @ImapServerName, @ImapPortNumber, @SmtpLoginName, @SmtpLoginPassword, @SmtpServerName, @SmtpPortNumber);";
                    cmd.Prepare();

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

                    try
                    {
                        numRowsInserted = cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message);
                        errorMsg = ex.Message;
                    }
                }

                dbConnection.Close();
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

            using (SqlCeConnection dbConnection = new SqlCeConnection(connString))
            {
                dbConnection.Open();
                using (SqlCeCommand cmd = new SqlCeCommand())
                {
                    cmd.Connection = dbConnection;
                    cmd.CommandText = "DELETE FROM Accounts WHERE AccountName = @AccountName;";
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@AccountName", account.AccountName);
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message);
                        errorMsg = ex.Message;
                        return false;
                    }

                    // Following two queries should not be necessary if we are using foreign key restrictions on
                    // Mailboxes and Messages tables' AccountName attribute. I am executing them here anyway because
                    // they are harmless.
                    cmd.CommandText = "DELETE FROM Mailboxes WHERE AccountName = @AccountName;";
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@AccountName", account.AccountName);
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message);
                        errorMsg = ex.Message;
                        return false;
                    }

                    cmd.CommandText = "DELETE FROM Messages WHERE AccountName = @AccountName;";
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@AccountName", account.AccountName);
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message);
                        errorMsg = ex.Message;
                        return false;
                    }
                }

                dbConnection.Close();

                return true;
            }
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
                using (SqlCeConnection dbConnection = new SqlCeConnection(connString))
                {
                    dbConnection.Open();

                    using (SqlCeCommand cmd = new SqlCeCommand())
                    {
                        cmd.Connection = dbConnection;
                        cmd.CommandText = @"SELECT * FROM Mailboxes WHERE AccountName = @AccountName ORDER BY Path;";
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@AccountName", accountName);
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
                    dbConnection.Close();
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

            using (SqlCeConnection dbConnection = new SqlCeConnection(connString))
            {
                dbConnection.Open();
                using (SqlCeCommand cmd = new SqlCeCommand())
                {
                    cmd.Connection = dbConnection;
                    cmd.CommandText = "DELETE FROM Mailboxes WHERE AccountName = @AccountName AND Path = @DirectoryPath;";
                    foreach (Mailbox mbox in mailboxes)
                    {
                        cmd.Parameters.Clear();
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@AccountName", mbox.AccountName);
                        cmd.Parameters.AddWithValue("@DirectoryPath", mbox.DirectoryPath);
                        try
                        {
                            numRowsDeleted += cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine(ex.Message);
                            errorMsg = ex.Message;
                            return numRowsDeleted;
                        }
                    }
                }

                dbConnection.Close();

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

            using (SqlCeConnection dbConnection = new SqlCeConnection(connString))
            {
                dbConnection.Open();
                using (SqlCeCommand cmd = new SqlCeCommand())
                {
                    cmd.Connection = dbConnection;
                    cmd.CommandText = "INSERT INTO Mailboxes VALUES(@AccountName, @Path, @Separator, @UidNext, @UidValidity, @FlagString);";
                    foreach (Mailbox mailbox in mailboxes)
                    {
                        cmd.Parameters.Clear();
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@AccountName", mailbox.AccountName);
                        cmd.Parameters.AddWithValue("@Path", mailbox.DirectoryPath);
                        cmd.Parameters.AddWithValue("@Separator", mailbox.PathSeparator);
                        cmd.Parameters.AddWithValue("@UidNext", mailbox.UidNext);
                        cmd.Parameters.AddWithValue("@UidValidity", mailbox.UidValidity);
                        cmd.Parameters.AddWithValue("@FlagString", mailbox.FlagString);

                        try
                        {
                            numRowsInserted += cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine(ex.Message);
                            errorMsg = ex.Message;
                            return numRowsInserted;
                        }
                    }
                }

                dbConnection.Close();

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
                return maxUid;
            }
            else
            {
                using (SqlCeConnection dbConnection = new SqlCeConnection(connString))
                {
                    dbConnection.Open();

                    using (SqlCeCommand cmd = new SqlCeCommand())
                    {
                        cmd.Connection = dbConnection;
                        cmd.CommandText = @"SELECT MAX(Uid) from Messages WHERE AccountName = @AccountName AND MailboxPath = @MailboxPath;";
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@AccountName", accountName);
                        cmd.Parameters.AddWithValue("@MailboxPath", mailboxPath);
                        try
                        {
                            object result = cmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                maxUid = Convert.ToInt32(result);
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine(ex.Message);
                        }
                    }
                    dbConnection.Close();
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
                return minUid;
            }
            else
            {
                using (SqlCeConnection dbConnection = new SqlCeConnection(connString))
                {
                    dbConnection.Open();

                    using (SqlCeCommand cmd = new SqlCeCommand())
                    {
                        cmd.Connection = dbConnection;
                        cmd.CommandText = @"SELECT MIN(Uid) from Messages WHERE AccountName = @AccountName AND MailboxPath = @MailboxPath;";
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@AccountName", accountName);
                        cmd.Parameters.AddWithValue("@MailboxPath", mailboxPath);
                        try
                        {
                            object result = cmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                minUid = Convert.ToInt32(result);
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine(ex.Message);
                        }
                    }
                    dbConnection.Close();
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
                using (SqlCeConnection dbConnection = new SqlCeConnection(connString))
                {
                    dbConnection.Open();

                    using (SqlCeCommand cmd = new SqlCeCommand())
                    {
                        cmd.Connection = dbConnection;
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
                                message.Body = (string)reader["Body"];

                                messages.Add(message);
                            }
                        }
                    }
                    dbConnection.Close();
                }
            }

            return messages;
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

            using (SqlCeConnection dbConnection = new SqlCeConnection(connString))
            {
                dbConnection.Open();
                using (SqlCeCommand cmd = new SqlCeCommand())
                {
                    cmd.Connection = dbConnection;
                    cmd.CommandText = "INSERT INTO Messages VALUES(@AccountName, @MailboxPath, @Uid, @Subject, @DateString, @Sender, @Recipient, @FlagString, @Body);";

                    foreach (Message msg in messages)
                    {
                        cmd.Prepare();
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@AccountName", msg.AccountName);
                        cmd.Parameters.AddWithValue("@MailboxPath", msg.MailboxPath);
                        cmd.Parameters.AddWithValue("@Uid", msg.Uid);
                        cmd.Parameters.AddWithValue("@Subject", msg.Subject);
                        cmd.Parameters.AddWithValue("@DateString", msg.DateString);
                        cmd.Parameters.AddWithValue("@Sender", msg.Sender);
                        cmd.Parameters.AddWithValue("@Recipient", msg.Recipient);
                        cmd.Parameters.AddWithValue("@FlagString", msg.FlagString);
                        cmd.Parameters.AddWithValue("@Body", msg.Body);

                        try
                        {
                            numRowsInserted = cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine(ex.Message);
                            errorMsg = ex.Message;
                        }
                    }
                }

                dbConnection.Close();
            }

            return numRowsInserted;
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
            else
            {
                int numRowsDeleted = 0;

                using (SqlCeConnection dbConnection = new SqlCeConnection(connString))
                {
                    dbConnection.Open();
                    using (SqlCeCommand cmd = new SqlCeCommand())
                    {
                        cmd.Connection = dbConnection;
                        cmd.CommandText = "DELETE FROM Messages WHERE AccountName = @AccountName AND MailboxPath = @MailboxPath AND Uid = @Uid;";
                        foreach (Message message in messages)
                        {
                            cmd.Parameters.Clear();
                            cmd.Prepare();
                            cmd.Parameters.AddWithValue("@AccountName", message.AccountName);
                            cmd.Parameters.AddWithValue("@MailboxPath", message.MailboxPath);
                            cmd.Parameters.AddWithValue("@Uid", message.Uid);
                            try
                            {
                                numRowsDeleted += cmd.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                Trace.WriteLine(ex.Message);
                                errorMsg = ex.Message;
                                return numRowsDeleted;
                            }
                        }
                    }

                    dbConnection.Close();

                    return numRowsDeleted;
                }
            }
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

            using (SqlCeConnection dbConnection = new SqlCeConnection(connString))
            {
                dbConnection.Open();
                using (SqlCeCommand cmd = new SqlCeCommand())
                {
                    cmd.Connection = dbConnection;
                    cmd.CommandText = "Update Messages SET Subject = @Subject, DateString = @DateString, Sender = @Sender, Recipient = @Recipient, FlagString = @FlagString, Body = @Body WHERE AccountName = @AccountName AND MailboxPath = @MailboxPath AND Uid = @Uid;";

                    foreach (Message message in messages)
                    {
                        cmd.Parameters.Clear();
                        cmd.Prepare();

                        cmd.Parameters.AddWithValue("@Subject", message.Subject);
                        cmd.Parameters.AddWithValue("@DateString", message.DateString);
                        cmd.Parameters.AddWithValue("@Sender", message.Sender);
                        cmd.Parameters.AddWithValue("@Recipient", message.Recipient);
                        cmd.Parameters.AddWithValue("@FlagString", message.FlagString);
                        cmd.Parameters.AddWithValue("@Body", message.Body);

                        cmd.Parameters.AddWithValue("@AccountName", message.AccountName);
                        cmd.Parameters.AddWithValue("@MailboxPath", message.MailboxPath);
                        cmd.Parameters.AddWithValue("@Uid", message.Uid);

                        try
                        {
                            numRowsUpdated += cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine(ex.Message);
                            errorMsg = ex.Message;
                            return numRowsUpdated;
                        }
                    }
                }

                dbConnection.Close();

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
