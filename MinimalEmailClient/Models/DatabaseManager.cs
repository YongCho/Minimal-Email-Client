#undef TRACE
using System.Collections.Generic;
using System.IO;
using System.Data.SQLite;
using System;
using MinimalEmailClient.Common;
using System.Diagnostics;

namespace MinimalEmailClient.Models
{
    public class DatabaseManager
    {
        public static readonly string DatabaseFolder = Globals.UserSettingsFolder;
        public static readonly string DatabasePath = DatabaseFolder + "\\" + Properties.Settings.Default.DatabaseFileName;

        private static string ConnString()
        {
            SQLiteConnectionStringBuilder connBuilder = new SQLiteConnectionStringBuilder();
            connBuilder.DataSource = DatabasePath;
            connBuilder.Version = 3;
            connBuilder.ForeignKeys = true;
            connBuilder.UseUTF16Encoding = true;

            // Having not enough BusyTimeout seems to cause "SQLite error (5): database is locked" error
            // in some asynchronous read/write operation.
            connBuilder.BusyTimeout = 400;

            return connBuilder.ToString();
        }

        private static bool DatabaseExists()
        {
            return File.Exists(DatabasePath);
        }

        public static void CreateDatabase()
        {
            if (!DatabaseExists())
            {
                Trace.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
                Directory.CreateDirectory(DatabaseFolder);
                SQLiteConnection.CreateFile(DatabasePath);
                using (SQLiteConnection dbConnection = new SQLiteConnection(ConnString()))
                {
                    dbConnection.Open();

                    using (SQLiteCommand cmd = new SQLiteCommand(dbConnection))
                    {
                        cmd.CommandText = @"CREATE TABLE Accounts (AccountName TEXT PRIMARY KEY, EmailAddress TEXT, ImapLoginName TEXT, ImapLoginPassword TEXT, ImapServerName TEXT, ImapPortNumber INT, SmtpLoginName TEXT, SmtpLoginPassword TEXT, SmtpServerName TEXT, SmtpPortNumber INT);";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = @"CREATE TABLE Mailboxes (AccountName TEXT REFERENCES Accounts(AccountName) ON DELETE CASCADE ON UPDATE CASCADE, Path TEXT, Separator TEXT, UidNext INT, UidValidity INT, FlagString TEXT, PRIMARY KEY (AccountName, Path));";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = @"CREATE TABLE Messages (AccountName TEXT, MailboxPath TEXT, Uid INT, Subject TEXT, DateString TEXT, SenderName TEXT, SenderAddress TEXT, Recipient TEXT, FlagString TEXT, Body TEXT, PRIMARY KEY (AccountName, MailboxPath, Uid), FOREIGN KEY (AccountName, MailboxPath) REFERENCES Mailboxes(AccountName, Path) ON DELETE CASCADE ON UPDATE CASCADE);";
                        cmd.ExecuteNonQuery();
                    }

                    dbConnection.Close();
                }
            }
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
                using (SQLiteConnection dbConnection = new SQLiteConnection(ConnString()))
                {
                    dbConnection.Open();
                    string cmdString = @"SELECT * FROM Accounts;";
                    using (SQLiteCommand cmd = new SQLiteCommand(cmdString, dbConnection))
                    {
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
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

            using (SQLiteConnection dbConnection = new SQLiteConnection(ConnString()))
            {
                dbConnection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(dbConnection))
                {
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

            using (SQLiteConnection dbConnection = new SQLiteConnection(ConnString()))
            {
                dbConnection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(dbConnection))
                {
                    cmd.CommandText = "DELETE FROM Accounts WHERE AccountName = @AccountName;";
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@AccountName", account.AccountName);
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
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
                using (SQLiteConnection dbConnection = new SQLiteConnection(ConnString()))
                {
                    dbConnection.Open();

                    using (SQLiteCommand cmd = new SQLiteCommand(dbConnection))
                    {
                        cmd.CommandText = @"SELECT * FROM Mailboxes WHERE AccountName = @AccountName ORDER BY Path;";
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@AccountName", accountName);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
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

            using (SQLiteConnection dbConnection = new SQLiteConnection(ConnString()))
            {
                dbConnection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(dbConnection))
                {
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

            using (SQLiteConnection dbConnection = new SQLiteConnection(ConnString()))
            {
                dbConnection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(dbConnection))
                {
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
                using (SQLiteConnection dbConnection = new SQLiteConnection(ConnString()))
                {
                    dbConnection.Open();

                    using (SQLiteCommand cmd = new SQLiteCommand(dbConnection))
                    {
                        cmd.CommandText = @"SELECT MAX(Uid) from Messages WHERE AccountName = @AccountName AND MailboxPath = @MailboxPath;";
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@AccountName", accountName);
                        cmd.Parameters.AddWithValue("@MailboxPath", mailboxPath);
                        try
                        {
                            object result = cmd.ExecuteScalar();
                            if (result is long)
                            {
                                maxUid = Convert.ToInt32(result);
                            }
                        }
                        catch
                        {
                            maxUid = 0;
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
                using (SQLiteConnection dbConnection = new SQLiteConnection(ConnString()))
                {
                    dbConnection.Open();

                    using (SQLiteCommand cmd = new SQLiteCommand(dbConnection))
                    {
                        cmd.CommandText = @"SELECT MIN(Uid) from Messages WHERE AccountName = @AccountName AND MailboxPath = @MailboxPath;";
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@AccountName", accountName);
                        cmd.Parameters.AddWithValue("@MailboxPath", mailboxPath);
                        try
                        {
                            object result = cmd.ExecuteScalar();
                            if (result is long)
                            {
                                minUid = Convert.ToInt32(result);
                            }
                        }
                        catch
                        {
                            minUid = 0;
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
                using (SQLiteConnection dbConnection = new SQLiteConnection(ConnString()))
                {
                    dbConnection.Open();

                    using (SQLiteCommand cmd = new SQLiteCommand(dbConnection))
                    {
                        cmd.CommandText = @"SELECT * FROM Messages;";
                        cmd.Prepare();
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Message message = new Message();
                                message.AccountName = (string)reader["AccountName"];
                                message.MailboxPath = (string)reader["MailboxPath"];
                                message.Uid = (int)reader["Uid"];
                                message.Subject = (string)reader["Subject"];
                                message.DateString = (string)reader["DateString"];
                                message.SenderName = (string)reader["SenderName"];
                                message.SenderAddress = (string)reader["SenderAddress"];
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

            using (SQLiteConnection dbConnection = new SQLiteConnection(ConnString()))
            {
                dbConnection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(dbConnection))
                {
                    cmd.CommandText = "INSERT INTO Messages VALUES(@AccountName, @MailboxPath, @Uid, @Subject, @DateString, @SenderName, @SenderAddress, @Recipient, @FlagString, @Body);";

                    foreach (Message msg in messages)
                    {
                        cmd.Prepare();
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@AccountName", msg.AccountName);
                        cmd.Parameters.AddWithValue("@MailboxPath", msg.MailboxPath);
                        cmd.Parameters.AddWithValue("@Uid", msg.Uid);
                        cmd.Parameters.AddWithValue("@Subject", msg.Subject);
                        cmd.Parameters.AddWithValue("@DateString", msg.DateString);
                        cmd.Parameters.AddWithValue("@SenderName", msg.SenderName);
                        cmd.Parameters.AddWithValue("@SenderAddress", msg.SenderAddress);
                        cmd.Parameters.AddWithValue("@Recipient", msg.Recipient);
                        cmd.Parameters.AddWithValue("@FlagString", msg.FlagString);
                        cmd.Parameters.AddWithValue("@Body", msg.Body);

                        try
                        {
                            numRowsInserted = cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
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

                using (SQLiteConnection dbConnection = new SQLiteConnection(ConnString()))
                {
                    dbConnection.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(dbConnection))
                    {
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
    }
}
