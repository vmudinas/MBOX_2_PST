using Aspose.Email;
using Aspose.Email.Storage.Pst;
using Aspose.Email.Mapi;
using MimeKit;
using System.Linq;

namespace MboxToPstConverter;

public class PstWriter
{
    public void CreatePstFromMessages(IEnumerable<MimeMessage> messages, string pstFilePath)
    {
        CreatePstFromMessages(messages, pstFilePath, null);
    }

    public void CreatePstFromMessages(IEnumerable<MimeMessage> messages, string pstFilePath, IProgress<PstConversionProgress>? progress)
    {
        Console.WriteLine("=== Starting PST File Creation ===");
        Console.WriteLine($"Output file: {pstFilePath}");
        
        var startTime = DateTime.Now;
        Console.WriteLine($"PST creation start time: {startTime:yyyy-MM-dd HH:mm:ss}");
        
        // Check output directory
        var outputDir = Path.GetDirectoryName(pstFilePath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Console.WriteLine($"Creating output directory: {outputDir}");
            Directory.CreateDirectory(outputDir);
        }
        
        progress?.Report(new PstConversionProgress { Message = "Creating PST file...", ProcessedCount = 0, SuccessfulCount = 0, FailedCount = 0 });
        
        // Create a new PST file
        Console.WriteLine("Creating new PST file with Unicode format...");
        var pst = PersonalStorage.Create(pstFilePath, FileFormatVersion.Unicode);
        
        try
        {
            // Get the root folder and create a default inbox folder
            Console.WriteLine("Setting up PST folder structure...");
            var rootFolder = pst.RootFolder;
            var inboxFolder = rootFolder.AddSubFolder("Inbox");
            Console.WriteLine("PST folder structure created: Root -> Inbox");
            
            Console.WriteLine("PST file created. Processing messages...");
            progress?.Report(new PstConversionProgress { Message = "PST file created. Processing messages...", ProcessedCount = 0, SuccessfulCount = 0, FailedCount = 0 });

            int processedCount = 0;
            int successCount = 0;
            int errorCount = 0;
            var lastProgressReport = DateTime.Now;
            var conversionStartTime = DateTime.Now;

            Console.WriteLine("Starting message conversion loop...");

            foreach (var mimeMessage in messages)
            {
                processedCount++;
                
                try
                {
                    var messageStartTime = DateTime.Now;
                    
                    // Convert MimeMessage to Aspose MapiMessage
                    var mapiMessage = ConvertMimeMessageToMapiMessage(mimeMessage);
                    
                    // Add the message to the inbox folder
                    inboxFolder.AddMessage(mapiMessage);
                    successCount++;
                    
                    var messageEndTime = DateTime.Now;
                    var messageConversionTime = (messageEndTime - messageStartTime).TotalMilliseconds;
                    
                    // Log detailed info for first few messages
                    if (processedCount <= 5)
                    {
                        Console.WriteLine($"Message #{processedCount}: Subject='{mimeMessage.Subject ?? "(No Subject)"}', " +
                                        $"ConversionTime={messageConversionTime:F1}ms, Status=Success");
                    }
                    
                    // Show progress every 10 messages or every 10 seconds
                    if (processedCount % 10 == 0 || (DateTime.Now - lastProgressReport).TotalSeconds >= 10)
                    {
                        var elapsed = DateTime.Now - conversionStartTime;
                        var messagesPerSecond = processedCount / elapsed.TotalSeconds;
                        var successRate = (double)successCount / processedCount * 100;
                        
                        var progressMessage = $"Processed {processedCount} messages ({successCount} successful, {errorCount} failed) - " +
                                            $"{messagesPerSecond:F1} msg/sec, {successRate:F1}% success rate";
                        Console.WriteLine(progressMessage);
                        
                        progress?.Report(new PstConversionProgress 
                        { 
                            Message = progressMessage, 
                            ProcessedCount = processedCount, 
                            SuccessfulCount = successCount, 
                            FailedCount = errorCount 
                        });
                        
                        lastProgressReport = DateTime.Now;
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    string subject = !string.IsNullOrEmpty(mimeMessage.Subject) ? mimeMessage.Subject : "(No Subject)";
                    string from = mimeMessage.From?.FirstOrDefault()?.ToString() ?? "(No Sender)";
                    
                    // Log detailed error info for first few errors
                    if (errorCount <= 5)
                    {
                        Console.WriteLine($"Detailed error #{errorCount} for message #{processedCount}: " +
                                        $"Subject='{subject}', From='{from}' - {ex.GetType().Name}: {ex.Message}");
                    }
                    else if (errorCount % 10 == 0)
                    {
                        Console.WriteLine($"Failed to add message #{processedCount} - Subject: '{subject}', From: '{from}' - Error: {ex.Message}");
                    }
                    
                    // Continue processing other messages
                }
            }
            
            var conversionEndTime = DateTime.Now;
            var totalConversionTime = conversionEndTime - conversionStartTime;
            var averageTimePerMessage = processedCount > 0 ? totalConversionTime.TotalMilliseconds / processedCount : 0;
            var finalMessagesPerSecond = processedCount / totalConversionTime.TotalSeconds;
            
            Console.WriteLine();
            Console.WriteLine("=== PST Conversion Summary ===");
            Console.WriteLine($"Conversion end time: {conversionEndTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Total conversion time: {totalConversionTime.TotalSeconds:F2} seconds");
            Console.WriteLine($"Messages processed: {processedCount}");
            Console.WriteLine($"Messages successfully added: {successCount}");
            Console.WriteLine($"Messages failed: {errorCount}");
            Console.WriteLine($"Success rate: {(double)successCount / processedCount * 100:F1}%");
            Console.WriteLine($"Average time per message: {averageTimePerMessage:F2} ms");
            Console.WriteLine($"Processing speed: {finalMessagesPerSecond:F2} messages/second");
            
            // Check final PST file size
            if (File.Exists(pstFilePath))
            {
                var pstFileInfo = new FileInfo(pstFilePath);
                Console.WriteLine($"Final PST file size: {pstFileInfo.Length / 1024.0 / 1024.0:F2} MB");
            }
            
            var finalMessage = $"Conversion completed: {successCount} messages successfully added to PST file, {errorCount} messages failed.";
            Console.WriteLine(finalMessage);
            progress?.Report(new PstConversionProgress 
            { 
                Message = finalMessage, 
                ProcessedCount = processedCount, 
                SuccessfulCount = successCount, 
                FailedCount = errorCount,
                IsCompleted = true
            });
            
            if (errorCount > 0)
            {
                var noteMessage = $"Note: {errorCount} messages failed due to invalid email addresses or other formatting issues. This is normal when processing MBOX files with malformed data.";
                Console.WriteLine(noteMessage);
                progress?.Report(new PstConversionProgress 
                { 
                    Message = noteMessage, 
                    ProcessedCount = processedCount, 
                    SuccessfulCount = successCount, 
                    FailedCount = errorCount,
                    IsCompleted = true
                });
            }
            
            Console.WriteLine("=== PST File Creation Completed ===");
        }
        finally
        {
            Console.WriteLine("Disposing PST file resources...");
            pst.Dispose();
        }
    }

    private static MapiMessage ConvertMimeMessageToMapiMessage(MimeMessage mimeMessage)
    {
        // Create a MailMessage first, then convert to MapiMessage
        var mailMessage = new MailMessage();

        // Set basic properties - extract clean email addresses
        if (mimeMessage.From?.Count > 0)
        {
            var fromAddress = ExtractEmailAddress(mimeMessage.From[0]);
            if (!string.IsNullOrEmpty(fromAddress))
            {
                mailMessage.From = fromAddress;
            }
        }

        if (mimeMessage.To?.Count > 0)
        {
            foreach (var recipient in mimeMessage.To)
            {
                var toAddress = ExtractEmailAddress(recipient);
                if (!string.IsNullOrEmpty(toAddress))
                {
                    mailMessage.To.Add(toAddress);
                }
            }
        }

        if (mimeMessage.Cc?.Count > 0)
        {
            foreach (var recipient in mimeMessage.Cc)
            {
                var ccAddress = ExtractEmailAddress(recipient);
                if (!string.IsNullOrEmpty(ccAddress))
                {
                    mailMessage.CC.Add(ccAddress);
                }
            }
        }

        if (mimeMessage.Bcc?.Count > 0)
        {
            foreach (var recipient in mimeMessage.Bcc)
            {
                var bccAddress = ExtractEmailAddress(recipient);
                if (!string.IsNullOrEmpty(bccAddress))
                {
                    mailMessage.Bcc.Add(bccAddress);
                }
            }
        }

        mailMessage.Subject = mimeMessage.Subject ?? "";
        mailMessage.Date = mimeMessage.Date.DateTime;

        // Set body content
        if (mimeMessage.TextBody != null)
        {
            mailMessage.Body = mimeMessage.TextBody;
        }
        else if (mimeMessage.HtmlBody != null)
        {
            mailMessage.HtmlBody = mimeMessage.HtmlBody;
        }

        // Add attachments
        foreach (var attachment in mimeMessage.Attachments)
        {
            if (attachment is MimePart mimePart)
            {
                using var stream = new MemoryStream();
                mimePart.Content.DecodeTo(stream);
                var attachmentData = stream.ToArray();
                var fileName = mimePart.FileName ?? "attachment";
                
                mailMessage.AddAttachment(new Attachment(new MemoryStream(attachmentData), fileName));
            }
        }

        // Convert MailMessage to MapiMessage
        return MapiMessage.FromMailMessage(mailMessage);
    }

    /// <summary>
    /// Extracts a clean email address from a MimeKit InternetAddress
    /// </summary>
    /// <param name="address">The internet address to extract from</param>
    /// <returns>A clean email address or empty string if invalid</returns>
    private static string ExtractEmailAddress(InternetAddress address)
    {
        if (address == null)
        {
            return string.Empty;
        }

        // Handle MailboxAddress (most common case)
        if (address is MailboxAddress mailbox)
        {
            var emailAddress = mailbox.Address?.Trim();
            
            // Basic email validation
            if (string.IsNullOrEmpty(emailAddress) || !emailAddress.Contains('@'))
            {
                return string.Empty;
            }

            return emailAddress;
        }

        // Handle GroupAddress (less common, extract first mailbox)
        if (address is GroupAddress group && group.Members.Count > 0)
        {
            var firstMember = group.Members.FirstOrDefault() as MailboxAddress;
            if (firstMember?.Address != null)
            {
                var emailAddress = firstMember.Address.Trim();
                if (!string.IsNullOrEmpty(emailAddress) && emailAddress.Contains('@'))
                {
                    return emailAddress;
                }
            }
        }

        return string.Empty;
    }
}

public class PstConversionProgress
{
    public string Message { get; set; } = string.Empty;
    public int ProcessedCount { get; set; }
    public int SuccessfulCount { get; set; }
    public int FailedCount { get; set; }
    public bool IsCompleted { get; set; }
}