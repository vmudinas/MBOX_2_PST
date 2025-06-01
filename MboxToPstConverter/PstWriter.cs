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
        Console.WriteLine("Creating PST file...");
        
        // Create a new PST file
        var pst = PersonalStorage.Create(pstFilePath, FileFormatVersion.Unicode);
        
        try
        {
            // Get the root folder and create a default inbox folder
            var rootFolder = pst.RootFolder;
            var inboxFolder = rootFolder.AddSubFolder("Inbox");
            
            Console.WriteLine("PST file created. Processing messages...");

            int processedCount = 0;
            int successCount = 0;
            int errorCount = 0;

            foreach (var mimeMessage in messages)
            {
                processedCount++;
                
                try
                {
                    // Convert MimeMessage to Aspose MapiMessage
                    var mapiMessage = ConvertMimeMessageToMapiMessage(mimeMessage);
                    
                    // Add the message to the inbox folder
                    inboxFolder.AddMessage(mapiMessage);
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
                    
                    Console.WriteLine($"Failed to add message #{processedCount} - Subject: '{subject}', From: '{from}' - Error: {ex.Message}");
                    
                    // Continue processing other messages
                }
            }
            
            Console.WriteLine($"Conversion completed: {successCount} messages successfully added to PST file, {errorCount} messages failed.");
            
            if (errorCount > 0)
            {
                Console.WriteLine($"Note: {errorCount} messages failed due to invalid email addresses or other formatting issues. This is normal when processing MBOX files with malformed data.");
            }
        }
        finally
        {
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