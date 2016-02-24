using Prism.Mvvm;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MinimalEmailClient.Models
{
    class AccountManager : BindableBase
    {
        ObservableCollection<Account> Accounts;
        public string Error = string.Empty;
        public readonly int MaxAccountNameLength = 30;

        public AccountManager()
        {
            Accounts = new ObservableCollection<Account>();
        }

        // Loads all accounts from database.
        public void LoadAccounts()
        {
            DatabaseManager databaseManager = new DatabaseManager();
            List<Account> accounts = databaseManager.GetAccounts();

            Accounts.Clear();
            foreach (Account account in accounts)
            {
                Accounts.Add(account);
            }
        }

        // Returns true if successfully added the account. False, otherwise.
        public bool AddAccount(Account ac)
        {
            DatabaseManager dm = new DatabaseManager();
            bool success = dm.AddAccount(ac);
            if (!success)
            {
                Error = dm.Error;
            }

            return success;
        }
    }
}
