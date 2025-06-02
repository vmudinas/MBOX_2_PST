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

        Console.WriteLine("=== Starting PST File Parsing ===");
        Console.WriteLine($"File path: {pstFilePath}");
        
        var fileInfo = new FileInfo(pstFilePath);
        var fileSizeInMB = fileInfo.Length / 1024.0 / 1024.0;
        var fileSizeInBytes = fileInfo.Length;
        Console.WriteLine($"File size: {fileSizeInMB:F2} MB ({fileSizeInBytes:N0} bytes)");
        Console.WriteLine($"File created: {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"File modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");

        var parseStartTime = DateTime.Now;
        Console.WriteLine($"Parse start time: {parseStartTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine("Opening PST file...");

        using var pst = PersonalStorage.FromFile(pstFilePath);
        Console.WriteLine("PST file opened successfully");
        Console.WriteLine($"PST format version: {pst.Format}");
        
        var rootFolder = pst.RootFolder;
        Console.WriteLine($"Root folder: {rootFolder.DisplayName}");
        
        // Count total folders
        var folderCount = CountFolders(pst, rootFolder);
        Console.WriteLine($"Total folders in PST: {folderCount}");
        
        int messageCount = 0;
        var lastProgressReport = DateTime.Now;
        
        Console.WriteLine("Starting message extraction from all folders...");
        
        // Recursively extract messages from all folders
        foreach (var message in ExtractMessagesFromFolder(pst, pst.RootFolder))
        {
            messageCount++;
            
            // Log detailed info for first few messages
            if (messageCount <= 5)
            {
                Console.WriteLine($"Message #{messageCount}: Subject='{message.Subject ?? "(No Subject)"}', " +
                                $"From='{message.From?.FirstOrDefault()?.ToString() ?? "(No From)"}', " +
                                $"Date='{message.Date:yyyy-MM-dd HH:mm:ss}'");
            }
            
            // Progress reporting every 50 messages or every 10 seconds
            if (messageCount % 50 == 0 || (DateTime.Now - lastProgressReport).TotalSeconds >= 10)
            {
                var elapsed = DateTime.Now - parseStartTime;
                var messagesPerSecond = messageCount / elapsed.TotalSeconds;
                
                var progressMessage = $"Parsed {messageCount} messages from PST ({messagesPerSecond:F1} msg/sec)";
                Console.WriteLine(progressMessage);
                
                lastProgressReport = DateTime.Now;
            }
            
            yield return message;
        }
        
        var parseEndTime = DateTime.Now;
        var totalParseDuration = parseEndTime - parseStartTime;
        var finalMessagesPerSecond = messageCount / totalParseDuration.TotalSeconds;
        
        Console.WriteLine();
        Console.WriteLine("=== PST Parsing Summary ===");
        Console.WriteLine($"Parse end time: {parseEndTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Total parsing time: {totalParseDuration.TotalSeconds:F2} seconds");
        Console.WriteLine($"Messages successfully parsed: {messageCount}");
        Console.WriteLine($"Average processing speed: {finalMessagesPerSecond:F2} messages/second");
        Console.WriteLine($"File processing speed: {fileSizeInMB / totalParseDuration.TotalSeconds:F2} MB/second");
        Console.WriteLine("=== PST Parsing Completed ===");
    }

    private IEnumerable<MimeMessage> ExtractMessagesFromFolder(PersonalStorage pst, FolderInfo folder)
    {
        // Extract messages from current folder
        var messages = new List<MimeMessage>();
        int folderMessageCount = 0;
        
        Console.WriteLine($"Processing folder: '{folder.DisplayName}' - Checking for messages...");
        
        foreach (var messageInfo in folder.EnumerateMessages())
        {
            folderMessageCount++;
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
                Console.WriteLine($"Failed to extract message #{folderMessageCount} from folder '{folder.DisplayName}': {ex.Message}");
                // Continue processing other messages
            }
        }

        Console.WriteLine($"Folder '{folder.DisplayName}': Found {folderMessageCount} message infos, converted {messages.Count} successfully");

        foreach (var message in messages)
        {
            yield return message;
        }

        // Recursively process subfolders
        var subFolders = folder.GetSubFolders().ToList();
        Console.WriteLine($"Folder '{folder.DisplayName}': Found {subFolders.Count} subfolders");
        
        foreach (var subFolder in subFolders)
        {
            Console.WriteLine($"Processing subfolder: '{subFolder.DisplayName}'");
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

    private int CountFolders(PersonalStorage pst, FolderInfo folder)
    {
        int count = 1; // Count current folder
        
        // Recursively count subfolders
        foreach (var subFolder in folder.GetSubFolders())
        {
            count += CountFolders(pst, subFolder);
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