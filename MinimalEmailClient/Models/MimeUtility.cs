using System.Text;
using System.Diagnostics;
using NI.Email.Mime.Message;
using NI.Email.Mime.Field;
using NI.Email.Mime.Decoder;
using System.IO;

namespace MinimalEmailClient.Models
{
    public class MimeUtility
    {
        public static string GetTextBody(string body)
        {
            Stream mimeMsgStream = new MemoryStream(Encoding.ASCII.GetBytes(body));
            MimeMessage mimeMsg = new MimeMessage(mimeMsgStream);
            Trace.WriteLine(body);
            return ParseFromMime(mimeMsg, "text/plain");
        }

        public static string GetHtmlBody(string body)
        {
            Stream mimeMsgStream = new MemoryStream(Encoding.ASCII.GetBytes(body));
            MimeMessage mimeMsg = new MimeMessage(mimeMsgStream);
            Trace.WriteLine(body);
            return ParseFromMime(mimeMsg, "text/html");
        }

        // Parses the content of the specified mime type.
        // Recognized mime types are "text/html" and "text/plain".
        private static string ParseFromMime(Entity mimeEntity, string mimeType)
        {
            string parsedText = string.Empty;
            if (mimeEntity.IsMultipart)
            {
                Multipart multiPart = (Multipart)mimeEntity.Body;
                foreach (Entity part in multiPart.BodyParts)
                {
                    ContentTypeField contentType = part.Header.GetField(MimeField.ContentType) as ContentTypeField;
                    if (contentType == null)
                    {
                        continue;
                    }
                    if (part.Body is ITextBody && contentType.MimeType.Contains(mimeType))
                    {
                        parsedText = ParseTextBody(part);
                    }
                    else if (part.IsMultipart)
                    {
                        parsedText = ParseFromMime((Entity)part, mimeType);
                    }
                    else if (part.Body is MimeMessage)
                    {
                        parsedText = ParseFromMime((MimeMessage)part.Body, mimeType);
                    }
                }
            }
            else
            {
                ContentTypeField contentType = mimeEntity.Header.GetField(MimeField.ContentType) as ContentTypeField;
                if (contentType != null)
                {
                    if (mimeEntity.Body is ITextBody && contentType.MimeType.Contains(mimeType))
                    {
                        parsedText = ParseTextBody(mimeEntity);
                    }
                }
            }
            return parsedText;
        }

        // Extracts the content out of ITextBody.
        private static string ParseTextBody(Entity mimeBody)
        {
            if (!(mimeBody.Body is ITextBody))
            {
                return string.Empty;
            }

            ITextBody textBody = (ITextBody)mimeBody.Body;
            MemoryStream memStream = new MemoryStream();
            textBody.WriteTo(memStream);
            memStream.Seek(0, SeekOrigin.Begin);
            string encoding = mimeBody.ContentTransferEncoding.ToLower();
            Trace.WriteLine("ContentTransferEncoding: " + encoding);
            byte[] buffer = new byte[memStream.Length];
            int bytesRead;
            if (encoding == "quoted-printable")
            {
                QuotedPrintableInputStream qpStream = new QuotedPrintableInputStream(memStream);
                bytesRead = qpStream.Read(buffer, 0, buffer.Length);
            }
            else if (encoding == "base64" || encoding == "base-64")
            {
                Base64InputStream b64Stream = new Base64InputStream(memStream);
                bytesRead = b64Stream.Read(buffer, 0, buffer.Length);
            }
            else
            {
                bytesRead = memStream.Read(buffer, 0, buffer.Length);
            }

            return mimeBody.CurrentEncoding.GetString(buffer, 0, bytesRead);
        }
    }
}
