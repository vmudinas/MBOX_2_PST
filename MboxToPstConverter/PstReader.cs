using Aspose.Email;
using Aspose.Email.Storage.Pst;
using Aspose.Email.Mapi;
using MimeKit;
using System.Linq;

namespace MboxToPstConverter;

public class PstReader
{
    public async Task<int> CountPstMessagesAsync(string pstFilePath, IProgress<PstParsingProgress>? progress = null)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(pstFilePath))
            {
                throw new FileNotFoundException($"PST file not found: {pstFilePath}");
            }

            var fileInfo = new FileInfo(pstFilePath);
            var fileSizeInMB = fileInfo.Length / 1024.0 / 1024.0;
            
            progress?.Report(new PstParsingProgress 
            { 
                Message = $"Counting messages in PST file ({fileSizeInMB:F2} MB)", 
                MessageCount = 0,
                FileSizeMB = fileSizeInMB
            });

            using var pst = PersonalStorage.FromFile(pstFilePath);
            
            int messageCount = CountMessagesInFolder(pst, pst.RootFolder, progress, fileSizeInMB);
            
            progress?.Report(new PstParsingProgress 
            { 
                Message = $"Finished counting. Total messages: {messageCount}", 
                MessageCount = messageCount,
                FileSizeMB = fileSizeInMB,
                IsCompleted = true
            });

            return messageCount;
        });
    }

    public async Task<List<MimeMessage>> GetFirstPstMessagesAsync(string pstFilePath, int maxCount, IProgress<PstParsingProgress>? progress = null)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(pstFilePath))
            {
                throw new FileNotFoundException($"PST file not found: {pstFilePath}");
            }

            var messages = new List<MimeMessage>();
            var fileInfo = new FileInfo(pstFilePath);
            var fileSizeInMB = fileInfo.Length / 1024.0 / 1024.0;
            
            progress?.Report(new PstParsingProgress 
            { 
                Message = $"Loading first {maxCount} messages from PST file ({fileSizeInMB:F2} MB)", 
                MessageCount = 0,
                FileSizeMB = fileSizeInMB
            });

            using var pst = PersonalStorage.FromFile(pstFilePath);
            
            int messageCount = 0;
            foreach (var message in ExtractMessagesFromFolder(pst, pst.RootFolder))
            {
                if (messages.Count >= maxCount) break;
                
                messages.Add(message);
                messageCount++;
                
                if (messageCount % 10 == 0)
                {
                    progress?.Report(new PstParsingProgress 
                    { 
                        Message = $"Loaded {messageCount} messages for preview...", 
                        MessageCount = messageCount,
                        FileSizeMB = fileSizeInMB
                    });
                }
            }
            
            progress?.Report(new PstParsingProgress 
            { 
                Message = $"Loaded {messages.Count} messages for preview", 
                MessageCount = messages.Count,
                FileSizeMB = fileSizeInMB,
                IsCompleted = true
            });

            return messages;
        });
    }

    public IEnumerable<MimeMessage> ParsePstFile(string pstFilePath)
    {
        if (!File.Exists(pstFilePath))
        {
            throw new FileNotFoundException($"PST file not found: {pstFilePath}");
        }

        Console.WriteLine($"Parsing PST file: {pstFilePath}");
        
        var fileInfo = new FileInfo(pstFilePath);
        Console.WriteLine($"File size: {fileInfo.Length / 1024 / 1024:F2} MB");

        using var pst = PersonalStorage.FromFile(pstFilePath);
        
        int messageCount = 0;
        
        // Recursively extract messages from all folders
        foreach (var message in ExtractMessagesFromFolder(pst, pst.RootFolder))
        {
            messageCount++;
            if (messageCount % 50 == 0)
            {
                Console.WriteLine($"Parsed {messageCount} messages...");
            }
            yield return message;
        }
        
        Console.WriteLine($"Finished parsing PST file. Total messages found: {messageCount}");
    }

    private IEnumerable<MimeMessage> ExtractMessagesFromFolder(PersonalStorage pst, FolderInfo folder)
    {
        // Extract messages from current folder
        var messages = new List<MimeMessage>();
        
        foreach (var messageInfo in folder.EnumerateMessages())
        {
            try
            {
                var mapiMessage = pst.ExtractMessage(messageInfo);
                var mimeMessage = ConvertMapiMessageToMimeMessage(mapiMessage);
                if (mimeMessage != null)
                {
                    messages.Add(mimeMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to extract message from folder '{folder.DisplayName}': {ex.Message}");
                // Continue processing other messages
            }
        }

        foreach (var message in messages)
        {
            yield return message;
        }

        // Recursively process subfolders
        foreach (var subFolder in folder.GetSubFolders())
        {
            foreach (var message in ExtractMessagesFromFolder(pst, subFolder))
            {
                yield return message;
            }
        }
    }

    private static MimeMessage? ConvertMapiMessageToMimeMessage(MapiMessage mapiMessage)
    {
        try
        {
            // Convert MapiMessage to MailMessage first
            var mailMessage = mapiMessage.ToMailMessage(new MailConversionOptions());
            
            // Create a new MimeMessage
            var mimeMessage = new MimeMessage();

            // Set basic properties
            if (!string.IsNullOrEmpty(mailMessage.From?.Address))
            {
                mimeMessage.From.Add(new MailboxAddress(mailMessage.From.DisplayName ?? "", mailMessage.From.Address));
            }

            if (mailMessage.To?.Count > 0)
            {
                foreach (var recipient in mailMessage.To)
                {
                    if (!string.IsNullOrEmpty(recipient.Address))
                    {
                        mimeMessage.To.Add(new MailboxAddress(recipient.DisplayName ?? "", recipient.Address));
                    }
                }
            }

            if (mailMessage.CC?.Count > 0)
            {
                foreach (var recipient in mailMessage.CC)
                {
                    if (!string.IsNullOrEmpty(recipient.Address))
                    {
                        mimeMessage.Cc.Add(new MailboxAddress(recipient.DisplayName ?? "", recipient.Address));
                    }
                }
            }

            if (mailMessage.Bcc?.Count > 0)
            {
                foreach (var recipient in mailMessage.Bcc)
                {
                    if (!string.IsNullOrEmpty(recipient.Address))
                    {
                        mimeMessage.Bcc.Add(new MailboxAddress(recipient.DisplayName ?? "", recipient.Address));
                    }
                }
            }

            mimeMessage.Subject = mailMessage.Subject ?? "";
            mimeMessage.Date = mailMessage.Date;

            // Create body builder to handle content
            var bodyBuilder = new BodyBuilder();

            if (!string.IsNullOrEmpty(mailMessage.Body))
            {
                if (mailMessage.IsBodyHtml)
                {
                    bodyBuilder.HtmlBody = mailMessage.Body;
                }
                else
                {
                    bodyBuilder.TextBody = mailMessage.Body;
                }
            }

            // Add attachments
            if (mailMessage.Attachments?.Count > 0)
            {
                foreach (var attachment in mailMessage.Attachments)
                {
                    if (attachment.ContentStream != null)
                    {
                        var memoryStream = new MemoryStream();
                        attachment.ContentStream.CopyTo(memoryStream);
                        memoryStream.Position = 0;
                        
                        bodyBuilder.Attachments.Add(attachment.Name ?? "attachment", memoryStream.ToArray());
                    }
                }
            }

            mimeMessage.Body = bodyBuilder.ToMessageBody();

            return mimeMessage;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to convert MAPI message to MIME message: {ex.Message}");
            return null;
        }
    }

    private int CountMessagesInFolder(PersonalStorage pst, FolderInfo folder, IProgress<PstParsingProgress>? progress, double fileSizeInMB)
    {
        int count = 0;
        
        // Count messages in current folder
        foreach (var messageInfo in folder.EnumerateMessages())
        {
            count++;
            if (count % 100 == 0)
            {
                progress?.Report(new PstParsingProgress 
                { 
                    Message = $"Counted {count} messages...", 
                    MessageCount = count,
                    FileSizeMB = fileSizeInMB
                });
            }
        }

        // Recursively count messages in subfolders
        foreach (var subFolder in folder.GetSubFolders())
        {
            count += CountMessagesInFolder(pst, subFolder, progress, fileSizeInMB);
        }

        return count;
    }
}

public class PstParsingProgress
{
    public string Message { get; set; } = string.Empty;
    public int MessageCount { get; set; }
    public double FileSizeMB { get; set; }
    public bool IsCompleted { get; set; }
}