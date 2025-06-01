using MimeKit;
using System.Text;

namespace MboxToPstConverter;

public class MboxWriter
{
    public void CreateMboxFromMessages(IEnumerable<MimeMessage> messages, string mboxFilePath)
    {
        Console.WriteLine("Creating MBOX file...");
        
        // Ensure output directory exists
        string? outputDir = Path.GetDirectoryName(mboxFilePath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        using var fileStream = File.Create(mboxFilePath);
        using var writer = new StreamWriter(fileStream, Encoding.UTF8);

        Console.WriteLine("MBOX file created. Processing messages...");

        int processedCount = 0;
        int successCount = 0;
        int errorCount = 0;

        foreach (var mimeMessage in messages)
        {
            processedCount++;
            
            try
            {
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
                
                successCount++;
                
                // Show progress every 10 messages
                if (processedCount % 10 == 0)
                {
                    Console.WriteLine($"Processed {processedCount} messages ({successCount} successful, {errorCount} failed)");
                }
            }
            catch (Exception ex)
            {
                errorCount++;
                string subject = !string.IsNullOrEmpty(mimeMessage.Subject) ? mimeMessage.Subject : "(No Subject)";
                string from = mimeMessage.From?.FirstOrDefault()?.ToString() ?? "(No Sender)";
                
                Console.WriteLine($"Failed to write message #{processedCount} - Subject: '{subject}', From: '{from}' - Error: {ex.Message}");
                
                // Continue processing other messages
            }
        }
        
        Console.WriteLine($"Conversion completed: {successCount} messages successfully written to MBOX file, {errorCount} messages failed.");
        
        if (errorCount > 0)
        {
            Console.WriteLine($"Note: {errorCount} messages failed due to formatting issues or other errors. This is normal when processing PST files with malformed data.");
        }
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