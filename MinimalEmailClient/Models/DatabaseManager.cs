using System.Collections.Generic;
using System.IO;
using System.Data.SQLite;
using System;
using MinimalEmailClient.Common;

namespace MinimalEmailClient.Models
{
    public class DatabaseManager
    {
        public string Error = string.Empty;
        public static readonly string DatabaseFolder = Globals.UserSettingsFolder;
        public static readonly string DatabaseFileName = "ec.db";
        public static readonly string DatabasePath = DatabaseFolder + "\\" + DatabaseFileName;
        private string connectionString = string.Format("Data Source={0}; Version=3; UTF16Encoding=True", DatabasePath);

        public DatabaseManager()
        {
            if (!DatabaseExists())
            {
                CreateDatabase();
            }
        }

        private bool DatabaseExists()
        {
            return File.Exists(DatabasePath);
        }

        private void CreateDatabase()
        {
            Directory.CreateDirectory(DatabaseFolder);
            SQLiteConnection.CreateFile(DatabasePath);
            using (SQLiteConnection dbConnection = new SQLiteConnection(connectionString))
            {
                dbConnection.Open();

                using (SQLiteCommand cmd = new SQLiteCommand(dbConnection))
                {
                    cmd.CommandText = @"CREATE TABLE Accounts (AccountName TEXT PRIMARY KEY, UserName TEXT, EmailAddress TEXT, ImapLoginName TEXT, ImapLoginPassword TEXT, ImapServerName TEXT, ImapPortNumber INT, SmtpLoginName TEXT, SmtpLoginPassword TEXT, SmtpServerName TEXT, SmtpPortNumber INT);";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = @"CREATE TABLE Mailboxes (AccountName TEXT, Path TEXT, Separator TEXT, FlagString TEXT, PRIMARY KEY (AccountName, Path), FOREIGN KEY (AccountName) REFERENCES Accounts(AccountName));";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = @"CREATE TABLE Messages (AccountName TEXT, MailboxPath TEXT, Uid INT, Subject TEXT, Date TEXT, SenderName TEXT, SenderAddress TEXT, RecipientName TEXT, RecipientAddress TEXT, FlagString TEXT, Body TEXT, PRIMARY KEY (AccountName, MailboxPath, Uid), FOREIGN KEY (AccountName) REFERENCES Accounts(AccountName), FOREIGN KEY (MailboxPath) REFERENCES Mailboxes(Path));";
                    cmd.ExecuteNonQuery();
                }

                dbConnection.Close();
            }
        }

        public List<Account> GetAccounts()
        {
            List<Account> accounts = new List<Account>();

            if (!DatabaseExists())
            {
                CreateDatabase();
            }
            else
            {
                using (SQLiteConnection dbConnection = new SQLiteConnection(connectionString))
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
                                account.UserName = (string)reader["UserName"];
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

        public bool AddAccount(Account account)
        {
            int numRowsInserted = 0;

            if (!DatabaseExists())
            {
                CreateDatabase();
            }

            using (SQLiteConnection dbConnection = new SQLiteConnection(connectionString))
            {
                dbConnection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(dbConnection))
                {
                    cmd.CommandText = @"INSERT INTO Accounts VALUES(@AccountName, @UserName, @EmailAddress, @ImapLoginName, @ImapLoginPassword, @ImapServerName, @ImapPortNumber, @SmtpLoginName, @SmtpLoginPassword, @SmtpServerName, @SmtpPortNumber);";
                    cmd.Prepare();

                    cmd.Parameters.AddWithValue("@AccountName", account.AccountName);
                    cmd.Parameters.AddWithValue("@UserName", account.UserName);
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
                        Error = ex.Message;
                    }
                }

                dbConnection.Close();
            }

            return numRowsInserted == 1 ? true : false;
        }


        public List<Mailbox> GetMailboxes(string accountName)
        {
            List<Mailbox> mailboxes = new List<Mailbox>();

            if (!DatabaseExists())
            {
                CreateDatabase();
                return mailboxes;
            }
            else
            {
                using (SQLiteConnection dbConnection = new SQLiteConnection(connectionString))
                {
                    dbConnection.Open();

                    using (SQLiteCommand cmd = new SQLiteCommand(dbConnection))
                    {
                        cmd.CommandText = @"SELECT * FROM Mailboxes WHERE AccountName = @AccountName;";
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

                                string[] flags = (reader["FlagString"] as string).Split(' ');
                                mailbox.Attributes.AddRange(flags);

                                mailboxes.Add(mailbox);
                            }
                        }
                    }
                    dbConnection.Close();
                }
            }

            return mailboxes;
        }

        // Removes all entries in the Mailboxes table that has the specified AccountName
        // and inserts the given mailboxes in the table.
        // Returns the number of mailboxes inserted.
        public int UpdateMailboxes(string accountName, List<Mailbox> newMailboxes)
        {
            if (!DatabaseExists())
            {
                CreateDatabase();
            }

            int numRowsInserted = 0;

            using (SQLiteConnection dbConnection = new SQLiteConnection(connectionString))
            {
                dbConnection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(dbConnection))
                {
                    cmd.CommandText = "DELETE FROM Mailboxes WHERE AccountName = @AccountName;";
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@AccountName", accountName);
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Error = ex.Message;
                        return numRowsInserted;
                    }

                    cmd.CommandText = "INSERT INTO Mailboxes VALUES(@AccountName, @Path, @Separator, @FlagString);";

                    foreach (Mailbox mailbox in newMailboxes)
                    {
                        cmd.Prepare();
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@AccountName", mailbox.AccountName);
                        cmd.Parameters.AddWithValue("@Path", mailbox.DirectoryPath);
                        cmd.Parameters.AddWithValue("@Separator", mailbox.PathSeparator);

                        string flagString = string.Empty;
                        foreach (string flag in mailbox.Attributes)
                        {
                            flagString += (flag + " ");
                        }
                        flagString = flagString.Substring(0, flagString.Length - 1);
                        cmd.Parameters.AddWithValue("@FlagString", flagString);

                        try
                        {
                            numRowsInserted += cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            Error = ex.Message;
                        }
                    }
                }

                dbConnection.Close();

                return numRowsInserted;
            }
        }
    }
}
