using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MinimalEmailClient.Models
{
    public class FetchParser
    {
        // Constructs a Message object from an untagged response string returned by a FETCH command.
        public static Message CreateHeader(string untaggedItem)
        {
            Debug.WriteLine(untaggedItem);

            // Server divides long subjects and senders, etc. into multiple lines.
            // We have to merge these multi-line blocks into a single block first.
            // Here is an example.
            //
            // The subject received from the server
            //
            // "[Msgs] Fwd: Graduate Research Opportunity: The DOE Office of\r\n"
            // " Science Graduate Student Research (SCGSR)program is now accepting\r\n"
            // " applications!"
            //
            // must be converted to
            //
            // "[Msgs] Fwd: Graduate Research Opportunity: The DOE Office ofScience Graduate Student Research (SCGSR) program is now acceptingapplications!"

            // Most multi-line Quoted-Printables include encoding information in each line. We only need the encoding information
            // in the first line. The ones in the subsequent lines must be removed as we merge the lines.
            // Matches "?=\r\n =?<charset>?Q?" where <charset> is whatever charset it is ("utf-8", "euc-kr", etc.).
            untaggedItem = Regex.Replace(untaggedItem, "\\?=\r\n =\\?[-\\w]+\\?[QqBb]{1}\\?", "");

            // "?= \r\n " appears in a multi-line Quoted-Printable without charset information.
            // Finally, "\r\n " appears in multi-line non-Quoted-Printable. They all need to go.
            untaggedItem = untaggedItem.Replace("?=\r\n ", "").Replace("\r\n ", "");


            // Now we merged all multi-line blocks into their own line.
            // Let's start parsing each block.

            Message message = new Message();

            string itemHeaderPattern = "^(.*)\r\n";
            string itemHeader = Regex.Match(untaggedItem, itemHeaderPattern).Groups[1].ToString();
            Debug.WriteLine("Item Header: " + itemHeader);

            string subjectPattern = "\r\nSubject: (.*)\r\n";
            string subject = Regex.Match(untaggedItem, subjectPattern).Groups[1].ToString();
            subject = Decoder.DecodeHeaderElement(subject);
            Debug.WriteLine("Subject: " + subject);
            message.Subject = subject;

            string datePattern = "\r\nDate: (.*)\r\n";
            string dtString = Regex.Match(untaggedItem, datePattern, RegexOptions.IgnoreCase).Groups[1].ToString();
            Debug.WriteLine("Date: " + dtString);
            DateTime dt;
            if (!DateTime.TryParse(dtString, out dt))
            {
                dt = DateTimeParser.Parse(dtString);
            }
            message.DateString = dtString;
            message.Date = dt;

            string senderPattern = "\r\nFrom: (.*)<(.*)>\r\n";
            Match m = Regex.Match(untaggedItem, senderPattern);
            Debug.WriteLine("From: " + m.ToString());

            string senderName = Decoder.DecodeHeaderElement(m.Groups[1].ToString());
            string senderAddress = m.Groups[2].ToString();
            if (string.IsNullOrWhiteSpace(senderName))
            {
                senderName = senderAddress;
            }
            Debug.WriteLine("Sender Name: " + senderName);
            Debug.WriteLine("Sender Address: " + senderAddress);
            message.SenderAddress = senderAddress;
            message.SenderName = senderName;

            Debug.WriteLine("\n\n==========================\n\n");
            return message;
        }
    }
}
