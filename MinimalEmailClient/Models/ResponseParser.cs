using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MinimalEmailClient.Models
{
    public class ResponseParser
    {
        // Constructs a Message object from an untagged response string returned by a FETCH command.
        public static Message ParseFetchHeader(string untaggedItem)
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

            Match m;

            string itemHeaderPattern = "^(.*)\r\n";
            m = Regex.Match(untaggedItem, itemHeaderPattern);
            if (m.Success)
            {
                string itemHeader = m.Groups[1].ToString();
                Debug.WriteLine("Item Header: " + itemHeader);
            }

            string subjectPattern = "^Subject: (.*)\r\n";
            m = Regex.Match(untaggedItem, subjectPattern, RegexOptions.Multiline);
            if (m.Success)
            {
                string subject = m.Groups[1].ToString();
                subject = Decoder.DecodeSingleLine(subject);
                Debug.WriteLine("Subject: " + subject);
                message.Subject = subject;
                untaggedItem = Regex.Replace(untaggedItem, subjectPattern, "", RegexOptions.Multiline);

            }

            string datePattern = "^Date: ([^\r\n]*)";
            m = Regex.Match(untaggedItem, datePattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (m.Success)
            {
                string dtString = m.Groups[1].ToString();
                Debug.WriteLine("Date: " + dtString);
                DateTime dt = ResponseParser.ParseDate(dtString);
                message.DateString = dtString;
                message.Date = dt;
                untaggedItem = Regex.Replace(untaggedItem, datePattern, "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            }

            string senderName = string.Empty;
            string senderAddress = string.Empty;
            string senderPattern = "^From: (.*)<([^<>]*)>?\r\n";
            m = Regex.Match(untaggedItem, senderPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (m.Success)
            {
                senderName = Decoder.DecodeSingleLine(m.Groups[1].ToString());
                senderAddress = m.Groups[2].ToString();
                if (string.IsNullOrWhiteSpace(senderName))
                {
                    senderName = senderAddress;
                }

                untaggedItem = Regex.Replace(untaggedItem, senderPattern, "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            }
            else
            {
                senderPattern = "^From: (.*)\r\n";
                m = Regex.Match(untaggedItem, senderPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (m.Success)
                {
                    senderName = Decoder.DecodeSingleLine(m.Groups[1].ToString());
                    if (senderName.Contains("@"))
                    {
                        senderAddress = senderName;
                    }
                    untaggedItem = Regex.Replace(untaggedItem, senderPattern, "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                }
            }
            senderAddress = senderAddress.Trim();
            senderName = senderName.Trim();
            message.SenderAddress = senderAddress;
            message.SenderName = senderName;
            Debug.WriteLine("Sender Name: " + senderName);
            Debug.WriteLine("Sender Address: " + senderAddress);

            string recipientPattern = "^To: (.*)\r\n";
            m = Regex.Match(untaggedItem, recipientPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (m.Success)
            {
                string recipient = Decoder.DecodeSingleLine(m.Groups[1].ToString().Trim());
                Debug.WriteLine("Recipient: " + recipient);
                message.Recipient = recipient;
                untaggedItem = Regex.Replace(untaggedItem, recipientPattern, "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            }

            string uidPattern = "UID (\\d+)";
            m = Regex.Match(untaggedItem, uidPattern);
            if (m.Success)
            {
                int uid = Convert.ToInt32(m.Groups[1].ToString());
                Debug.WriteLine("UID: " + uid);
                message.Uid = uid;
            }

            Debug.WriteLine("\n\n==========================\n\n");
            return message;
        }

        public static ExamineResult ParseExamine(string examineResponse)
        {
            ExamineResult status = new ExamineResult();
            Regex regex;
            Match m;

            string existsPattern = "^\\* (\\d+) EXISTS\r\n";
            regex = new Regex(existsPattern, RegexOptions.Multiline);
            m = regex.Match(examineResponse);
            status.Exists = Convert.ToInt32(m.Groups[1].ToString());

            string recentPattern = "^\\* (\\d+) RECENT\r\n";
            regex = new Regex(recentPattern, RegexOptions.Multiline);
            m = regex.Match(examineResponse);
            status.Recent = Convert.ToInt32(m.Groups[1].ToString());

            string uidNextPattern = "^\\* (?<ok>\\w+) \\[UIDNEXT (?<value>\\d+)\\]";
            regex = new Regex(uidNextPattern, RegexOptions.Multiline);
            m = regex.Match(examineResponse);
            status.UidNext = Convert.ToInt32(m.Groups["value"].ToString());

            string uidValidityPattern = "^\\* (?<ok>\\w+) \\[UIDVALIDITY (?<value>\\d+)\\]";
            regex = new Regex(uidValidityPattern, RegexOptions.Multiline);
            m = regex.Match(examineResponse);
            status.UidValidity = Convert.ToInt32(m.Groups["value"].ToString());

            string flagsPattern = "^\\* FLAGS \\((.*)\\)\r\n";
            regex = new Regex(flagsPattern, RegexOptions.Multiline);
            m = regex.Match(examineResponse);
            if (m.Success)
            {
                string flagsString = m.Groups[1].ToString();
                if (!string.IsNullOrWhiteSpace(flagsString))
                {
                    string[] flags = flagsString.Split(' ');
                    status.Flags = new List<string>(flags);
                }
            }

            string permanentFlagsPattern = "^\\* (?<ok>\\w+) \\[PERMANENTFLAGS \\((?<value>.*)\\)\\]";
            regex = new Regex(permanentFlagsPattern, RegexOptions.Multiline);
            m = regex.Match(examineResponse);
            if (m.Success)
            {
                string flagsString = m.Groups["value"].ToString();
                if (!string.IsNullOrWhiteSpace(flagsString))
                {
                    string[] permFlags = flagsString.Split(' ');
                    status.PermanemtFlags = new List<string>(permFlags);
                }
            }

            return status;
        }

        // Parses date string returned by FETCH command and creates a DateTime object.
        public static DateTime ParseDate(string dateString)
        {
            DateTime dt;

            // Try System parser first. This should catch all the standard formatted inputs.
            if (DateTime.TryParse(dateString, out dt))
            {
                return dt;
            }

            // If we got here, the input string probably has some erroneous characters.
            // Try to filter them out with regex.
            string[] patterns = { "\\d+ \\w+ \\d+ \\d+:\\d+:\\d+ ?[-+\\d]*", "\\d+-\\d+-\\d+ \\d+:\\d+:\\d+ ?[-+\\d]*" };

            Regex regex;
            Match m;

            foreach (string pattern in patterns)
            {
                regex = new Regex(pattern);
                m = regex.Match(dateString);
                if (m.Success)
                {
                    return DateTime.Parse(m.ToString());
                }

            }

            // Still couldn't find a match. Let's use the Unix zero point as the fallback.
            return new DateTime(1970, 1, 1);
        }
    }
}
