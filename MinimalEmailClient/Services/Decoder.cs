using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace MinimalEmailClient.Services
{
    public class Decoder
    {
        // Decodes a line of Quoted-Printable or Base64 string and returns it
        // as a C# string.
        public static string DecodeSingleLine(string encodedString)
        {
            Regex regex = new Regex(@"=\?(?<charset>.*?)\?(?<encoding>[qQbB])\?(?<value>.*?)\?=");
            string decodedMatch = string.Empty;
            string decodedString = string.Empty;
            List<byte> encodedBytes = new List<byte>();

            var match = regex.Match(encodedString);
            if (match.Success)
            {
                string charset = match.Groups["charset"].Value;
                string encoding = match.Groups["encoding"].Value;
                string value = match.Groups["value"].Value;

                if (encoding.ToUpper().Equals("B"))
                {
                    // Encoded value is Base-64.
                    try {
                        var bytes = Convert.FromBase64String(value);
                        decodedMatch = Encoding.GetEncoding(charset).GetString(bytes);
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine(e.Message);
                        decodedMatch = match.Value;
                    }
                }
                else if (encoding.ToUpper().Equals("Q"))
                {
                    // Encoded value is Quoted-Printable.
                    decodedMatch = QuotedPrintableToString(charset, value);
                }
                else
                {
                    // Unknown encoding. leave it as is.
                    decodedMatch = match.Value;
                }

                decodedString = regex.Replace(encodedString, decodedMatch);
            }
            else
            {
                // No match. This must be a plain ASCII string.
                decodedString = encodedString;
            }

            return decodedString;
        }

        // Converts a quoted-printable string to a unicode string.
        //
        // ex1)
        // QuotedPrintableToString("utf-8", "=e6=97=a5=e6=9c=ac=e8=aa=9e=20=e3=83=86=e3=82=b9=e3=83=88?=");
        // returns "日本語 テスト"
        //
        // ex2)
        // QuotedPrintableToString("utf-8", "=?utf-8?q?Prueba=20de=20espa=c3=b1ol?=");
        // returns "Prueba de español"
        public static string QuotedPrintableToString(string charset, string encodedString)
        {
            List<byte> encodedBytes = new List<byte>();

            // Matches "=XX" followed by anything where X is a hex character.
            string quotedPrintableCharPattern = @"^=([0-9a-fA-F]{2})";
            byte b;

            while (encodedString.Length > 0)
            {
                Match m = Regex.Match(encodedString, quotedPrintableCharPattern);
                if (m.Success)
                {
                    // A quoted-printable is found. Convert it to a byte.
                    b = Convert.ToByte(m.Groups[1].ToString(), 16);
                    encodedString = encodedString.Substring(3);
                }
                else
                {
                    // Just an ASCII character. Convert it to a byte.
                    b = Convert.ToByte(encodedString[0]);
                    encodedString = encodedString.Substring(1);
                }
                encodedBytes.Add(b);
            }

            string decodedString;
            try
            {
                decodedString = Encoding.GetEncoding(charset).GetString(encodedBytes.ToArray());
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
                decodedString = encodedString;
            }

            return decodedString;
        }
    }
}
