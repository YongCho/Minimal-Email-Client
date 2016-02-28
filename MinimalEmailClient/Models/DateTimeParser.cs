using System;
using System.Text.RegularExpressions;

namespace MinimalEmailClient.Models
{
    public class DateTimeParser
    {
        private static string[] patterns = { "\\d+ \\w+ \\d+ \\d+:\\d+:\\d+ ?[-+\\d]*", "\\d+-\\d+-\\d+ \\d+:\\d+:\\d+ ?[-+\\d]*" };
        public static DateTime Parse(string str)
        {
            Regex regex;
            Match m;

            foreach (string pattern in patterns)
            {
                regex = new Regex(pattern);
                m = regex.Match(str);
                if (m.Success)
                {
                    return DateTime.Parse(m.ToString());
                }

            }

            return new DateTime(1970,1,1);
        }
    }
}
