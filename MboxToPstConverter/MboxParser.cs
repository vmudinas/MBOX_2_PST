using MimeKit;
using System.Text;

namespace MboxToPstConverter;

public class MboxParser
{
    public IEnumerable<MimeMessage> ParseMboxFile(string mboxFilePath)
    {
        if (!File.Exists(mboxFilePath))
        {
            throw new FileNotFoundException($"MBOX file not found: {mboxFilePath}");
        }

        Console.WriteLine($"Parsing MBOX file: {mboxFilePath}");
        
        var fileInfo = new FileInfo(mboxFilePath);
        Console.WriteLine($"File size: {fileInfo.Length / 1024 / 1024:F2} MB");

        using var stream = File.OpenRead(mboxFilePath);
        var parser = new MimeParser(stream, MimeFormat.Mbox);

        int messageCount = 0;
        while (!parser.IsEndOfStream)
        {
            var message = parser.ParseMessage();
            if (message != null)
            {
                messageCount++;
                if (messageCount % 50 == 0)
                {
                    Console.WriteLine($"Parsed {messageCount} messages...");
                }
                yield return message;
            }
        }
        
        Console.WriteLine($"Finished parsing MBOX file. Total messages found: {messageCount}");
    }
}