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
            string header;
            string bodyStructure;
            Match match;

            match = Regex.Match(untaggedItem, @"^(.*\r\n)\r\n BODYSTRUCTURE (.*)\r\n$", RegexOptions.Singleline);
            if (match.Success)
            {
                header = match.Groups[1].Value;
                bodyStructure = match.Groups[2].Value;
            }
            else
            {
                Debug.WriteLine("ImapParser.ParseFetchHeader(): Unable to parse header and body structure. Received:\n" + untaggedItem);
                return null;
            }

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
            header = Regex.Replace(header, "\\?=\r\n =\\?[-\\w]+\\?[QqBb]{1}\\?", "");

            // "?= \r\n " appears in a multi-line Quoted-Printable without charset information.
            // Finally, "\r\n " and "\r\n\t" appears in multi-line non-Quoted-Printables.
            // I'm not sure if they need to be replaced with a single space or removed altogether.
            // Continue experimenting on this.
            header = header.Replace("?=\r\n ", "").Replace("\r\n ", " ").Replace("\r\n\t", " ");


            // Now we merged all multi-line blocks into their own line.
            // Let's start parsing each block.

            Message message = new Message();

            string itemHeaderPattern = "^(.*)\r\n";
            match = Regex.Match(header, itemHeaderPattern);
            int uid = -1;
            if (match.Success)
            {
                string itemHeader = match.Groups[1].ToString();

                string flagsPattern = "FLAGS \\((?<flags>[^\\(\\)]*)\\)";
                match = Regex.Match(itemHeader, flagsPattern);
                if (match.Success)
                {
                    string flagString = match.Groups["flags"].ToString();
                    message.FlagString = flagString;
                }

                string uidPattern = "UID (\\d+)";
                match = Regex.Match(itemHeader, uidPattern);
                if (match.Success)
                {
                    uid = Convert.ToInt32(match.Groups[1].ToString());
                    message.Uid = uid;
                }
            }

            string subjectPattern = "^Subject: (.*)\r\n";
            match = Regex.Match(header, subjectPattern, RegexOptions.Multiline);
            if (match.Success)
            {
                string subject = match.Groups[1].ToString();
                subject = Decoder.DecodeSingleLine(subject);
                message.Subject = subject;

            }

            string datePattern = "^Date: ([^\r\n]*)";
            match = Regex.Match(header, datePattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (match.Success)
            {
                string dateString = match.Groups[1].ToString();
                message.DateString = dateString;
            }

            string senderPattern = "^From: (.*)\r\n";
            match = Regex.Match(header, senderPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (match.Success)
            {
                message.Sender = Decoder.DecodeSingleLine(match.Groups[1].ToString());
            }

            string recipientPattern = "^To: (.*)\r\n";
            match = Regex.Match(header, recipientPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (match.Success)
            {
                string recipient = Decoder.DecodeSingleLine(match.Groups[1].ToString().Trim());
                message.Recipient = recipient;
            }

            string attachmentStartPattern = " \\(\"attachment\" ";
            match = Regex.Match(bodyStructure, attachmentStartPattern, RegexOptions.IgnoreCase);
            message.HasAttachment = match.Success ? true : false;

            return message;
        }

        public static ExamineResult ParseExamineResponse(string examineResponse)
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
