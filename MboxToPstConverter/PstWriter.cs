using Aspose.Email;
using Aspose.Email.Storage.Pst;
using Aspose.Email.Mapi;
using MimeKit;

namespace MboxToPstConverter;

public class PstWriter
{
    public void CreatePstFromMessages(IEnumerable<MimeMessage> messages, string pstFilePath)
    {
        // Create a new PST file
        var pst = PersonalStorage.Create(pstFilePath, FileFormatVersion.Unicode);
        
        try
        {
            // Get the root folder and create a default inbox folder
            var rootFolder = pst.RootFolder;
            var inboxFolder = rootFolder.AddSubFolder("Inbox");

            foreach (var mimeMessage in messages)
            {
                try
                {
                    // Convert MimeMessage to Aspose MapiMessage
                    var mapiMessage = ConvertMimeMessageToMapiMessage(mimeMessage);
                    
                    // Add the message to the inbox folder
                    inboxFolder.AddMessage(mapiMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to add message: {ex.Message}");
                    // Continue processing other messages
                }
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

        // Set basic properties
        if (mimeMessage.From?.Count > 0)
        {
            mailMessage.From = mimeMessage.From[0].ToString();
        }

        if (mimeMessage.To?.Count > 0)
        {
            foreach (var recipient in mimeMessage.To)
            {
                mailMessage.To.Add(recipient.ToString());
            }
        }

        if (mimeMessage.Cc?.Count > 0)
        {
            foreach (var recipient in mimeMessage.Cc)
            {
                mailMessage.CC.Add(recipient.ToString());
            }
        }

        if (mimeMessage.Bcc?.Count > 0)
        {
            foreach (var recipient in mimeMessage.Bcc)
            {
                mailMessage.Bcc.Add(recipient.ToString());
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
}