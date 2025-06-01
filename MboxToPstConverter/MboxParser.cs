using MimeKit;
using System.Text;

namespace MboxToPstConverter;

public class MboxParsingProgress
{
    public string Message { get; set; } = string.Empty;
    public int MessageCount { get; set; }
    public double FileSizeMB { get; set; }
    public bool IsCompleted { get; set; }
}

public class MboxParser
{
    public IEnumerable<MimeMessage> ParseMboxFile(string mboxFilePath)
    {
        return ParseMboxFile(mboxFilePath, null);
    }

    public IEnumerable<MimeMessage> ParseMboxFile(string mboxFilePath, IProgress<MboxParsingProgress>? progress)
    {
        if (!File.Exists(mboxFilePath))
        {
            throw new FileNotFoundException($"MBOX file not found: {mboxFilePath}");
        }

        Console.WriteLine($"Parsing MBOX file: {mboxFilePath}");
        
        var fileInfo = new FileInfo(mboxFilePath);
        var fileSizeInMB = fileInfo.Length / 1024.0 / 1024.0;
        Console.WriteLine($"File size: {fileSizeInMB:F2} MB");

        progress?.Report(new MboxParsingProgress 
        { 
            Message = $"Starting to parse MBOX file ({fileSizeInMB:F2} MB)", 
            MessageCount = 0,
            FileSizeMB = fileSizeInMB
        });

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
                    progress?.Report(new MboxParsingProgress 
                    { 
                        Message = $"Parsed {messageCount} messages...", 
                        MessageCount = messageCount,
                        FileSizeMB = fileSizeInMB
                    });
                }
                yield return message;
            }
        }
        
        Console.WriteLine($"Finished parsing MBOX file. Total messages found: {messageCount}");
        progress?.Report(new MboxParsingProgress 
        { 
            Message = $"Finished parsing MBOX file. Total messages found: {messageCount}", 
            MessageCount = messageCount,
            FileSizeMB = fileSizeInMB,
            IsCompleted = true
        });
    }
}