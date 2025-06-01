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
        var messages = new List<MimeMessage>();
        int skippedCount = 0;
        
        while (!parser.IsEndOfStream)
        {
            try
            {
                var message = parser.ParseMessage();
                if (message != null)
                {
                    messages.Add(message);
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
                }
            }
            catch (Exception ex)
            {
                skippedCount++;
                // Log malformed message with index and error details
                Console.WriteLine($"Skipped malformed message at index {messageCount + skippedCount}: {ex.Message}");
                
                // Continue parsing - MimeParser should handle recovery automatically
            }
        }
        
        // Report final results including skipped messages
        Console.WriteLine($"Finished parsing MBOX file. Total messages found: {messageCount}, Skipped: {skippedCount}");
        progress?.Report(new MboxParsingProgress 
        { 
            Message = $"Finished parsing MBOX file. Total messages found: {messageCount}, Skipped: {skippedCount}", 
            MessageCount = messageCount,
            FileSizeMB = fileSizeInMB,
            IsCompleted = true
        });
        
        // Now yield all successfully parsed messages
        foreach (var message in messages)
        {
            yield return message;
        }
    }

    public async Task<int> CountMboxMessagesAsync(string mboxFilePath, IProgress<MboxParsingProgress>? progress = null)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(mboxFilePath))
            {
                throw new FileNotFoundException($"MBOX file not found: {mboxFilePath}");
            }

            var fileInfo = new FileInfo(mboxFilePath);
            var fileSizeInMB = fileInfo.Length / 1024.0 / 1024.0;
            
            progress?.Report(new MboxParsingProgress 
            { 
                Message = $"Counting messages in MBOX file ({fileSizeInMB:F2} MB)", 
                MessageCount = 0,
                FileSizeMB = fileSizeInMB
            });

            using var stream = File.OpenRead(mboxFilePath);
            var parser = new MimeParser(stream, MimeFormat.Mbox);

            int messageCount = 0;
            int skippedCount = 0;
            
            while (!parser.IsEndOfStream)
            {
                try
                {
                    var message = parser.ParseMessage();
                    if (message != null)
                    {
                        messageCount++;
                        if (messageCount % 100 == 0)
                        {
                            progress?.Report(new MboxParsingProgress 
                            { 
                                Message = $"Counted {messageCount} messages...", 
                                MessageCount = messageCount,
                                FileSizeMB = fileSizeInMB
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    skippedCount++;
                    // Log malformed message during counting
                    Console.WriteLine($"Skipped malformed message during count at index {messageCount + skippedCount}: {ex.Message}");
                    
                    // Continue counting - MimeParser should handle recovery automatically
                }
            }
            
            progress?.Report(new MboxParsingProgress 
            { 
                Message = $"Finished counting. Total messages: {messageCount}, Skipped: {skippedCount}", 
                MessageCount = messageCount,
                FileSizeMB = fileSizeInMB,
                IsCompleted = true
            });

            return messageCount;
        });
    }

    public async Task<List<MimeMessage>> GetFirstMboxMessagesAsync(string mboxFilePath, int maxCount, IProgress<MboxParsingProgress>? progress = null)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(mboxFilePath))
            {
                throw new FileNotFoundException($"MBOX file not found: {mboxFilePath}");
            }

            var messages = new List<MimeMessage>();
            var fileInfo = new FileInfo(mboxFilePath);
            var fileSizeInMB = fileInfo.Length / 1024.0 / 1024.0;
            
            progress?.Report(new MboxParsingProgress 
            { 
                Message = $"Loading first {maxCount} messages from MBOX file ({fileSizeInMB:F2} MB)", 
                MessageCount = 0,
                FileSizeMB = fileSizeInMB
            });

            using var stream = File.OpenRead(mboxFilePath);
            var parser = new MimeParser(stream, MimeFormat.Mbox);

            int messageCount = 0;
            int skippedCount = 0;
            
            while (!parser.IsEndOfStream && messages.Count < maxCount)
            {
                try
                {
                    var message = parser.ParseMessage();
                    if (message != null)
                    {
                        messages.Add(message);
                        messageCount++;
                        
                        if (messageCount % 10 == 0)
                        {
                            progress?.Report(new MboxParsingProgress 
                            { 
                                Message = $"Loaded {messageCount} messages for preview...", 
                                MessageCount = messageCount,
                                FileSizeMB = fileSizeInMB
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    skippedCount++;
                    // Log malformed message during preview loading
                    Console.WriteLine($"Skipped malformed message during preview at index {messageCount + skippedCount}: {ex.Message}");
                    
                    // Continue trying to load more messages for preview
                    // MimeParser should handle recovery automatically
                }
            }
            
            progress?.Report(new MboxParsingProgress 
            { 
                Message = $"Loaded {messages.Count} messages for preview (skipped {skippedCount})", 
                MessageCount = messages.Count,
                FileSizeMB = fileSizeInMB,
                IsCompleted = true
            });

            return messages;
        });
    }
}