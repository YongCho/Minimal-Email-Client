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

                    cmd.CommandText = @"CREATE TABLE Mailboxes (AccountName TEXT, Path TEXT, Separator TEXT, FlagString TEXT, PRIMARY KEY (AccountName, MailboxName), FOREIGN KEY (AccountName) REFERENCES Accounts(AccountName));";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = @"CREATE TABLE Messages (AccountName TEXT, MailboxName TEXT, Uid INT, Subject TEXT, Date TEXT, SenderName TEXT, SenderAddress TEXT, RecipientName TEXT, RecipientAddress TEXT, FlagString TEXT, PRIMARY KEY (AccountName, MailboxName, Uid), FOREIGN KEY (AccountName) REFERENCES Accounts(AccountName), FOREIGN KEY (MailboxName) REFERENCES Mailboxes(MailboxName));";
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

        public void UpdateMailboxes(string accountName, List<Mailbox> mailboxes)
        {
            // Delete from Mailboxes where accountname = accountname
            // for each mailbox in mailboxes
            //     insert to Mailboxes values (accountname, mailboxname, ...)
        }
    }
}
