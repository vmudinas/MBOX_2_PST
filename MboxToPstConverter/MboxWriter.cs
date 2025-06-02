using MimeKit;
using System.Text;

namespace MboxToPstConverter;

public class MboxWriter
{
    public void CreateMboxFromMessages(IEnumerable<MimeMessage> messages, string mboxFilePath)
    {
        Console.WriteLine("=== Starting MBOX File Creation ===");
        Console.WriteLine($"Output file: {mboxFilePath}");
        
        var startTime = DateTime.Now;
        Console.WriteLine($"MBOX creation start time: {startTime:yyyy-MM-dd HH:mm:ss}");
        
        // Ensure output directory exists
        string? outputDir = Path.GetDirectoryName(mboxFilePath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Console.WriteLine($"Creating output directory: {outputDir}");
            Directory.CreateDirectory(outputDir);
        }

        Console.WriteLine("Creating new MBOX file...");
        using var fileStream = File.Create(mboxFilePath);
        using var writer = new StreamWriter(fileStream, Encoding.UTF8);

        Console.WriteLine("MBOX file created. Processing messages...");

        int processedCount = 0;
        int successCount = 0;
        int errorCount = 0;
        var lastProgressReport = DateTime.Now;
        var conversionStartTime = DateTime.Now;
        long totalBytesWritten = 0;

        Console.WriteLine("Starting message writing loop...");

        foreach (var mimeMessage in messages)
        {
            processedCount++;
            
            try
            {
                var messageStartTime = DateTime.Now;
                
                // Write the MBOX separator line
                // Format: "From sender@domain.com timestamp"
                var fromAddress = GetSenderAddress(mimeMessage);
                var timestamp = mimeMessage.Date.ToString("ddd MMM dd HH:mm:ss yyyy");
                
                writer.WriteLine($"From {fromAddress} {timestamp}");
                
                // Write the message content
                using var messageStream = new MemoryStream();
                mimeMessage.WriteTo(messageStream);
                messageStream.Position = 0;
                
                using var messageReader = new StreamReader(messageStream, Encoding.UTF8);
                string messageContent = messageReader.ReadToEnd();
                
                // Write the message content (without additional newlines that WriteTo might add)
                writer.Write(messageContent);
                if (!messageContent.EndsWith("\n"))
                {
                    writer.WriteLine();
                }
                writer.WriteLine(); // Add blank line between messages
                
                totalBytesWritten += messageContent.Length + fromAddress.Length + timestamp.Length + 10; // Approximate
                successCount++;
                
                var messageEndTime = DateTime.Now;
                var messageWriteTime = (messageEndTime - messageStartTime).TotalMilliseconds;
                
                // Log detailed info for first few messages
                if (processedCount <= 5)
                {
                    Console.WriteLine($"Message #{processedCount}: Subject='{mimeMessage.Subject ?? "(No Subject)"}', " +
                                    $"WriteTime={messageWriteTime:F1}ms, Status=Success");
                }
                
                // Show progress every 10 messages or every 10 seconds
                if (processedCount % 10 == 0 || (DateTime.Now - lastProgressReport).TotalSeconds >= 10)
                {
                    var elapsed = DateTime.Now - conversionStartTime;
                    var messagesPerSecond = processedCount / elapsed.TotalSeconds;
                    var successRate = (double)successCount / processedCount * 100;
                    var mbWritten = totalBytesWritten / 1024.0 / 1024.0;
                    
                    var progressMessage = $"Processed {processedCount} messages ({successCount} successful, {errorCount} failed) - " +
                                        $"{messagesPerSecond:F1} msg/sec, {successRate:F1}% success rate, {mbWritten:F2} MB written";
                    Console.WriteLine(progressMessage);
                    
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
                    Console.WriteLine($"Failed to write message #{processedCount} - Subject: '{subject}', From: '{from}' - Error: {ex.Message}");
                }
                
                // Continue processing other messages
            }
        }
        
        var conversionEndTime = DateTime.Now;
        var totalConversionTime = conversionEndTime - conversionStartTime;
        var averageTimePerMessage = processedCount > 0 ? totalConversionTime.TotalMilliseconds / processedCount : 0;
        var finalMessagesPerSecond = processedCount / totalConversionTime.TotalSeconds;
        var finalMbWritten = totalBytesWritten / 1024.0 / 1024.0;
        
        Console.WriteLine();
        Console.WriteLine("=== MBOX Creation Summary ===");
        Console.WriteLine($"Creation end time: {conversionEndTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Total creation time: {totalConversionTime.TotalSeconds:F2} seconds");
        Console.WriteLine($"Messages processed: {processedCount}");
        Console.WriteLine($"Messages successfully written: {successCount}");
        Console.WriteLine($"Messages failed: {errorCount}");
        Console.WriteLine($"Success rate: {(double)successCount / processedCount * 100:F1}%");
        Console.WriteLine($"Average time per message: {averageTimePerMessage:F2} ms");
        Console.WriteLine($"Processing speed: {finalMessagesPerSecond:F2} messages/second");
        Console.WriteLine($"Data written: {finalMbWritten:F2} MB");
        Console.WriteLine($"Write speed: {finalMbWritten / totalConversionTime.TotalSeconds:F2} MB/second");
        
        // Check final MBOX file size
        if (File.Exists(mboxFilePath))
        {
            var mboxFileInfo = new FileInfo(mboxFilePath);
            Console.WriteLine($"Final MBOX file size: {mboxFileInfo.Length / 1024.0 / 1024.0:F2} MB");
        }
        
        Console.WriteLine($"Conversion completed: {successCount} messages successfully written to MBOX file, {errorCount} messages failed.");
        
        if (errorCount > 0)
        {
            Console.WriteLine($"Note: {errorCount} messages failed due to formatting issues or other errors. This is normal when processing PST files with malformed data.");
        }
        
        Console.WriteLine("=== MBOX File Creation Completed ===");
    }

    private static string GetSenderAddress(MimeMessage message)
    {
        // Extract sender email address for the MBOX "From " line
        if (message.From?.Count > 0 && message.From[0] is MailboxAddress mailbox)
        {
            return mailbox.Address ?? "unknown@unknown.com";
        }
        
        return "unknown@unknown.com";
    }
}