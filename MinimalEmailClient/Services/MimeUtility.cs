using NI.Email.Mime.Decoder;
using NI.Email.Mime.Field;
using NI.Email.Mime.Message;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MinimalEmailClient.Services
{
    public class MimeUtility
    {
        public static string GetTextBody(string body)
        {
            Stream mimeMsgStream = new MemoryStream(Encoding.ASCII.GetBytes(body));
            MimeMessage mimeMsg = new MimeMessage(mimeMsgStream);
            return ParseBodyFromMime(mimeMsg, "text/plain");
        }

        public static string GetHtmlBody(string body)
        {
            Stream mimeMsgStream = new MemoryStream(Encoding.ASCII.GetBytes(body));
            MimeMessage mimeMsg = new MimeMessage(mimeMsgStream);
            return ParseBodyFromMime(mimeMsg, "text/html");
        }

        // Parses the content of the specified mime type.
        // Recognized mime types are "text/html" and "text/plain".
        private static string ParseBodyFromMime(Entity mimeEntity, string mimeType)
        {
            string parsedText = string.Empty;
            if (mimeEntity.IsMultipart)
            {
                Multipart multiPart = (Multipart)mimeEntity.Body;
                foreach (Entity part in multiPart.BodyParts)
                {
                    ContentTypeField contentType = part.Header.GetField(MimeField.ContentType) as ContentTypeField;
                    MimeField contentDispositionField = part.Header.GetField("Content-Disposition");
                    if (contentType == null)
                    {
                        continue;
                    }
                    if (contentDispositionField != null && contentDispositionField.Body.StartsWith("attachment"))
                    {
                        // An attached text file could also be ITextBody. We don't want this. We only want message body.
                        continue;
                    }
                    if (part.Body is ITextBody && contentType.MimeType.Contains(mimeType))
                    {
                        parsedText = ParseTextBody(part);
                    }
                    else if (part.IsMultipart)
                    {
                        parsedText = ParseBodyFromMime((Entity)part, mimeType);
                    }
                    else if (part.Body is MimeMessage)
                    {
                        parsedText = ParseBodyFromMime((MimeMessage)part.Body, mimeType);
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

        public static void SaveAttachments(string mimeMsg, string savePath, Dictionary<string, string> savedFiles)
        {
            Stream mimeMsgStream = new MemoryStream(Encoding.ASCII.GetBytes(mimeMsg));
            MimeMessage m = new MimeMessage(mimeMsgStream);

            SaveAttachments(m, savePath, savedFiles);
        }

        // Extracts binary contents from mime and saves them to given location.
        // Inserts <content-ID, saved file name> pairs to the dictionary.
        private static void SaveAttachments(Entity mimeEntity, string savePath, Dictionary<string, string> savedFiles)
        {
            if (mimeEntity.IsMultipart)
            {
                foreach (Entity part in ((Multipart)mimeEntity.Body).BodyParts)
                {
                    ContentTypeField contentType = part.Header.GetField(MimeField.ContentType) as ContentTypeField;
                    if (contentType == null) continue;

                    if (part.Body is MimeMessage)
                    {
                        SaveAttachments((MimeMessage)part.Body, savePath, savedFiles);
                    }
                    else if (part.IsMultipart)
                    {
                        SaveAttachments((Entity)part, savePath, savedFiles);
                    }
                    else if (!(part.Body is ITextBody))
                    {
                        string fileName;
                        if (contentType.Parameters.Contains("name"))
                        {
                            string name = contentType.Parameters["name"].ToString();
                            if (IsValidFileName(name))
                            {
                                fileName = name;
                            }
                            else
                            {
                                fileName = Path.GetFileNameWithoutExtension(Path.GetTempFileName()) + ".attachment";
                            }
                        }
                        else
                        {
                            fileName = Path.GetFileNameWithoutExtension(Path.GetTempFileName()) + ".attachment";
                        }

                        string filePath = Path.Combine(savePath, fileName);

                        FileStream outFileStream = new FileStream(filePath, FileMode.Create);
                        BinaryReader rdr = ((IBinaryBody)part.Body).Reader;

                        byte[] buf = new byte[1024];
                        int bytesRead = 0;
                        while ((bytesRead = rdr.Read(buf, 0, buf.Length)) > 0)
                            outFileStream.Write(buf, 0, bytesRead);

                        outFileStream.Flush();
                        outFileStream.Close();

                        MimeField contentIdField = part.Header.GetField("Content-ID");
                        string key = (contentIdField != null) ? contentIdField.Body.Trim('"', '<', '>', ' ') : fileName;
                        if (!savedFiles.ContainsKey(key))
                        {
                            savedFiles.Add(key, filePath);
                        }
                    }
                }
            }
        }

        private static bool IsValidFileName(string fileName)
        {
            return !string.IsNullOrEmpty(fileName) && fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }

        private static void EncodeAttachment()
        {

        }
    }
}
