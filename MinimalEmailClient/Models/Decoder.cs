using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace MinimalEmailClient.Models
{
    public class Decoder
    {
        public static string DecodeHeaderElement(string encodedString)
        {
            var regex = new Regex(@"=\?(?<charset>.*?)\?(?<encoding>[qQbB])\?(?<value>.*?)\?=");
            var decodedString = string.Empty;
            var encodedBytes = new List<byte>();

            var match = regex.Match(encodedString);
            if (match.Success)
            {
                var charset = match.Groups["charset"].Value;
                var encoding = match.Groups["encoding"].Value.ToUpper();
                var value = match.Groups["value"].Value;

                if (encoding.Equals("B"))
                {
                    // Encoded value is Base-64.
                    var bytes = Convert.FromBase64String(value);
                    decodedString = Encoding.GetEncoding(charset).GetString(bytes);
                }
                else if (encoding.Equals("Q"))
                {
                    // Encoded value is Quoted-Printable.
                    decodedString = QuotedPrintableToString(charset, value);
                }
                else
                {
                    // Unknown encoding. Return the original string.
                    decodedString = encodedString;
                }
            }
            else
            {
                // Plain ascii string; decoding is not necessary.
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
            var encodedBytes = new List<byte>();

            // Matches "=XX" followed by anything where X is a hex character.
            var quotedPrintableCharPattern = @"^=([0-9a-fA-F]{2})";
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

            return Encoding.GetEncoding(charset).GetString(encodedBytes.ToArray());
        }
    }
}
