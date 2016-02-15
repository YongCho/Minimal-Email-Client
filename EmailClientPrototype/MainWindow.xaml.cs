using System;
using System.Collections.Generic;
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
using System.ComponentModel;
using System.Net.Sockets;
using System.IO;
using System.Net.Security;
using System.Threading;
using System.Diagnostics;


namespace EmailClientPrototype
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Using SynchronizationContext to call event handlers in the UI thread so they can
        // access the UI controls.
        // SynchronizationContext https://msdn.microsoft.com/magazine/gg598924.aspx
        private readonly SynchronizationContext _syncContext;
        ImapClientProxy _imapClientProxy;

        public MainWindow()
        {
            InitializeComponent();
            _syncContext = SynchronizationContext.Current;

            _imapClientProxy = new ImapClientProxy("imap.gmail.com", 993, "test.racketscience", "12#$zxCV");
            _imapClientProxy.FetchFinishedRelay += OnFetchFinished;
        }
        
        private void AppendLineToDebugTxt1(string text)
        {
            _syncContext.Post(o => debugTxt1.AppendText(text + "\n"), null);
            debugTxt1Scroll.ScrollToEnd();
        }

        private void AppendLineToDebugTxt2(string text)
        {
            _syncContext.Post(o => debugTxt2.AppendText(text + "\n"), null);
            debugTxt2Scroll.ScrollToEnd();
        }
        
        private void btnFetchInbox_Click(object sender, RoutedEventArgs e)
        {
            _imapClientProxy.beginFetch("INBOX", 1, 20);
        }

        public void OnFetchFinished(object source, NewMessageEventArgs args)
        {
            foreach (Message msg in args.messages)
            {
                AppendLineToDebugTxt1("UID: " + msg.uid + ", Subject: " + msg.subject + ", Date: " + msg.date.ToString());
            }
        }
    }
}
