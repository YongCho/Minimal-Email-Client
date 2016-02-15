using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UIBindingWithEmailMessage
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<Message> messages = new ObservableCollection<Message>();

        public MainWindow()
        {
            InitializeComponent();

            messages.Add(new Message() { Subject = "Subject Line 1", SenderAddress = "sender1@gmail.com", RecipientAddress = "recipient@yahoo.com" });
            messages.Add(new Message() { Subject = "Subject Line 2", SenderAddress = "sender2@gmail.com", RecipientAddress = "recipient@yahoo.com" });
            messages.Add(new Message() { Subject = "Subject Line 3", SenderAddress = "sender3@gmail.com", RecipientAddress = "recipient@yahoo.com" });

            lvMessages.ItemsSource = messages;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            messages.Add(new Message() { Subject = "New Subject", SenderAddress = "newsender@gmail.com", RecipientAddress = "recipient@yahoo.com" });
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (lvMessages.SelectedItem != null)
            {
                messages.Remove(lvMessages.SelectedItem as Message);
            }
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (lvMessages.SelectedItem != null)
            {
                Message selectedMessage = (Message)lvMessages.SelectedItem;
                selectedMessage.Subject = "New Subject";
                selectedMessage.SenderAddress = "NewSender@gmail.com";
                selectedMessage.RecipientAddress = "NewRecipient@yahoo.com";
            }
        }
    }

    public class Message : INotifyPropertyChanged
    {
        private string subject;
        public string Subject
        {
            get { return this.subject; }
            set
            {
                if (this.subject != value)
                {
                    this.subject = value;
                    NotifyPropertyChanged("Subject");
                }                
            }
        }

        private string senderAddress;
        public string SenderAddress
        {
            get { return this.senderAddress; }
            set
            {
                if (this.senderAddress != value)
                {
                    this.senderAddress = value;
                    NotifyPropertyChanged("SenderAddress");
                }
            }
        }

        private string recipientAddress;
        public string RecipientAddress
        {
            get { return this.recipientAddress; }
            set
            {
                if (this.recipientAddress != value)
                {
                    this.recipientAddress = value;
                    NotifyPropertyChanged("RecipientAddress");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
