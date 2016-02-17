using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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

namespace DebugProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly SynchronizationContext _syncContext;
        public MainWindow()
        {
            InitializeComponent();
            _syncContext = SynchronizationContext.Current;
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string nonTaggedResponsePattern = "(^|\r\n)(\\* (.*\r\n)*?)(\\*|A1) ";
            string taggedResponsePattern = "^A1 .*\r\n";

            string input = "* 1 FETCH (UID 1 BODY[HEADER.FIELDS (SUBJECT DATE FROM)] {155}\r\nDate: Mon, 30 Mar 2015 13:37:39 + 0000\r\nSubject: RacketScience, get more out of your new Google account\r\nFrom: Jordan from Google < hi.jordan@google.com >\r\n\r\n)\r\n* 2 FETCH(UID 2 BODY[HEADER.FIELDS(SUBJECT DATE FROM)] { 147}\r\nDate: Mon, 30 Mar 2015 19:24:13 + 0000(UTC)\r\nSubject: Google Account recovery phone number changed\r\nFrom: Google < no - reply@accounts.google.com >\r\n\r\n)\r\n* 3 FETCH(UID 3 BODY[HEADER.FIELDS(SUBJECT DATE FROM)] { 142}\r\nDate: Mon, 30 Mar 2015 22:35:06 + 0000(UTC)\r\nSubject: Google Account: sign -in attempt blocked\r\nFrom: Google < no - reply@accounts.google.com >\r\n\r\n)\r\nA1 OK Success\r\n";

            string input2 = "* 3 FETCH (UID 3 BODY[HEADER.FIELDS (SUBJECT DATE FROM)] {142}\r\nDate: Mon, 30 Mar 2015 22:35:06 +0000 (UTC)\r\nSubject: Google Account: sign-in attempt blocked\r\nFrom: Google <no-reply@accounts.google.com>\r\n\r\n)\r\nA1 OK Success\r\n";


            Match match;
            string remainder = input2;
            bool doneMatching = false;
            int i = 0;
            while (!doneMatching)
            {
                match = Regex.Match(remainder, nonTaggedResponsePattern);
                if (match.Success)
                {
                    AppendLineToDebugTxt1("Match " + (++i));
                    AppendLineToDebugTxt1(match.Groups[2].ToString());
                    remainder = remainder.Substring(match.Groups[2].ToString().Length);
                    AppendLineToDebugTxt2(remainder);
                }
                else
                {
                    match = Regex.Match(remainder, taggedResponsePattern);
                    if (match.Success)
                    {
                        AppendLineToDebugTxt1("Match " + (++i));
                        AppendLineToDebugTxt1(match.Groups[0].ToString());
                        AppendLineToDebugTxt1("Done Matching");
                    }
                    doneMatching = true;
                }

            }
            





            //if (match.Success)
            //{
            //    AppendLineToDebugTxt1("Match Success");
            //    for (int i = 0; i < match.Groups.Count; ++i)
            //    {
            //        AppendLineToDebugTxt1("Group " + i + ": " + match.Groups[i].ToString());
            //        AppendLineToDebugTxt1("Group " + i + " Length: " + match.Groups[i].ToString().Length);
            //    }
            //    string substr = input.Substring(match.Groups[2].ToString().Length);
            //    AppendLineToDebugTxt2(substr);
            //}
            //else
            //{
            //    AppendLineToDebugTxt1("No Match");
            //}
        }
    }
}
