using MinimalEmailClient.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MinimalEmailClient.Services
{
    public class ImapParser
    {
        // Constructs a Message object from an untagged response string returned by a FETCH command.
        public static Message ParseFetchHeader(string untaggedItem)
        {
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
            // Finally, "\r\n " and "\r\n\t" appears in multi-line non-Quoted-Printables.
            // I'm not sure if they need to be replaced with a single space or removed altogether.
            // Continue experimenting on this.
            untaggedItem = untaggedItem.Replace("?=\r\n ", "").Replace("\r\n ", " ").Replace("\r\n\t", " ");


            // Now we merged all multi-line blocks into their own line.
            // Let's start parsing each block.

            Message message = new Message();

            Match m;

            string itemHeaderPattern = "^(.*)\r\n";
            m = Regex.Match(untaggedItem, itemHeaderPattern);
            int uid = -1;
            if (m.Success)
            {
                string itemHeader = m.Groups[1].ToString();

                string flagsPattern = "FLAGS \\((?<flags>[^\\(\\)]*)\\)";
                m = Regex.Match(itemHeader, flagsPattern);
                if (m.Success)
                {
                    string flagString = m.Groups["flags"].ToString();
                    message.FlagString = flagString;
                }

                string uidPattern = "UID (\\d+)";
                m = Regex.Match(itemHeader, uidPattern);
                if (m.Success)
                {
                    uid = Convert.ToInt32(m.Groups[1].ToString());
                    message.Uid = uid;
                }
            }

            // UID is not in the first line. The response must be an alternate pattern
            // where the UID is in the last line.
            if (uid == -1)
            {
                string uidPattern = "\r\n UID (\\d+)\\)\r\n";
                m = Regex.Match(untaggedItem, uidPattern);
                if (m.Success)
                {
                    uid = Convert.ToInt32(m.Groups[1].ToString());
                    message.Uid = uid;
                }
            }

            string subjectPattern = "^Subject: (.*)\r\n";
            m = Regex.Match(untaggedItem, subjectPattern, RegexOptions.Multiline);
            if (m.Success)
            {
                string subject = m.Groups[1].ToString();
                subject = Decoder.DecodeSingleLine(subject);
                message.Subject = subject;

            }

            string datePattern = "^Date: ([^\r\n]*)";
            m = Regex.Match(untaggedItem, datePattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (m.Success)
            {
                string dateString = m.Groups[1].ToString();
                message.DateString = dateString;
            }

            string senderPattern = "^From: (.*)\r\n";
            m = Regex.Match(untaggedItem, senderPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (m.Success)
            {
                message.Sender = Decoder.DecodeSingleLine(m.Groups[1].ToString());
            }

            string recipientPattern = "^To: (.*)\r\n";
            m = Regex.Match(untaggedItem, recipientPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (m.Success)
            {
                string recipient = Decoder.DecodeSingleLine(m.Groups[1].ToString().Trim());
                message.Recipient = recipient;
            }

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
