using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MinimalEmailClient.Models
{
    public class FetchParser
    {
        // Constructs a Message object from an untagged response string returned by a FETCH command.
        public static Message CreateHeader(string untaggedResponse)
        {
            Message message = new Message();

            string itemHeaderPattern = "^(.*)\r\n";
            string itemHeader = Regex.Match(untaggedResponse, itemHeaderPattern).Groups[1].ToString();
            Debug.WriteLine("Item Header: " + itemHeader);

            string subjectPattern = "\r\nSubject: (.*)\r\n";
            string subject = Regex.Match(untaggedResponse, subjectPattern).Groups[1].ToString();
            subject = Decoder.DecodeHeaderElement(subject);
            Debug.WriteLine("Subject: " + subject);
            message.Subject = subject;

            string datePattern = "\r\nDate: (.*)\r\n";
            string dtString = Regex.Match(untaggedResponse, datePattern, RegexOptions.IgnoreCase).Groups[1].ToString();
            Debug.WriteLine("Date: " + dtString);
            DateTime dt;
            if (!DateTime.TryParse(dtString, out dt))
            {
                dt = DateTimeParser.Parse(dtString);
            }
            message.DateString = dtString;
            message.Date = dt;

            string senderPattern = "\r\nFrom: (.*)<(.*)>\r\n";
            Match m = Regex.Match(untaggedResponse, senderPattern);
            string senderName = Decoder.DecodeHeaderElement(m.Groups[1].ToString());
            string senderAddress = m.Groups[2].ToString();
            if (senderName.Trim() == string.Empty)
            {
                senderName = senderAddress;
            }
            Debug.WriteLine("Sender Name: " + senderName);
            Debug.WriteLine("Sender Address: " + senderAddress);
            message.SenderAddress = senderAddress;
            message.SenderName = senderName;

            return message;
        }
    }
}
