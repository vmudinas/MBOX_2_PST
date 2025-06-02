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

        Console.WriteLine($"=== Starting MBOX File Parsing ===");
        Console.WriteLine($"File path: {mboxFilePath}");
        
        var fileInfo = new FileInfo(mboxFilePath);
        var fileSizeInMB = fileInfo.Length / 1024.0 / 1024.0;
        var fileSizeInBytes = fileInfo.Length;
        Console.WriteLine($"File size: {fileSizeInMB:F2} MB ({fileSizeInBytes:N0} bytes)");
        Console.WriteLine($"File created: {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"File modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");

        progress?.Report(new MboxParsingProgress 
        { 
            Message = $"Starting to parse MBOX file ({fileSizeInMB:F2} MB)", 
            MessageCount = 0,
            FileSizeMB = fileSizeInMB
        });

        var parseStartTime = DateTime.Now;
        Console.WriteLine($"Parse start time: {parseStartTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine("Opening file stream...");

        using var stream = File.OpenRead(mboxFilePath);
        Console.WriteLine("Creating MimeParser...");
        var parser = new MimeParser(stream, MimeFormat.Mbox);

        int messageCount = 0;
        var messages = new List<MimeMessage>();
        int skippedCount = 0;
        long bytesProcessed = 0;
        var lastProgressReport = DateTime.Now;
        
        Console.WriteLine("Starting message parsing loop...");
        
        while (!parser.IsEndOfStream)
        {
            try
            {
                var messageStartTime = DateTime.Now;
                var message = parser.ParseMessage();
                var messageEndTime = DateTime.Now;
                
                if (message != null)
                {
                    messages.Add(message);
                    messageCount++;
                    bytesProcessed = stream.Position;
                    
                    // Log detailed info for first few messages
                    if (messageCount <= 5)
                    {
                        var parseTime = (messageEndTime - messageStartTime).TotalMilliseconds;
                        Console.WriteLine($"Message #{messageCount}: Subject='{message.Subject ?? "(No Subject)"}', " +
                                        $"From='{message.From?.FirstOrDefault()?.ToString() ?? "(No From)"}', " +
                                        $"Date='{message.Date:yyyy-MM-dd HH:mm:ss}', " +
                                        $"ParseTime={parseTime:F1}ms");
                    }
                    
                    // Progress reporting every 50 messages or every 10 seconds
                    if (messageCount % 50 == 0 || (DateTime.Now - lastProgressReport).TotalSeconds >= 10)
                    {
                        var progressPercent = (double)bytesProcessed / fileSizeInBytes * 100;
                        var elapsed = DateTime.Now - parseStartTime;
                        var messagesPerSecond = messageCount / elapsed.TotalSeconds;
                        var estimatedTotal = elapsed.TotalSeconds > 0 ? messageCount / (progressPercent / 100) : 0;
                        var estimatedRemaining = estimatedTotal > messageCount ? 
                            TimeSpan.FromSeconds((estimatedTotal - messageCount) / messagesPerSecond) : TimeSpan.Zero;
                        
                        var progressMessage = $"Parsed {messageCount} messages ({progressPercent:F1}% of file, " +
                                            $"{messagesPerSecond:F1} msg/sec, ETA: {estimatedRemaining:mm\\:ss})";
                        Console.WriteLine(progressMessage);
                        
                        progress?.Report(new MboxParsingProgress 
                        { 
                            Message = progressMessage, 
                            MessageCount = messageCount,
                            FileSizeMB = fileSizeInMB
                        });
                        
                        lastProgressReport = DateTime.Now;
                    }
                }
            }
            catch (Exception ex)
            {
                skippedCount++;
                var currentPosition = stream.Position;
                var progressPercent = (double)currentPosition / fileSizeInBytes * 100;
                
                // Log detailed error info for first few errors
                if (skippedCount <= 5)
                {
                    Console.WriteLine($"Detailed error #{skippedCount} at position {currentPosition} ({progressPercent:F1}%): " +
                                    $"{ex.GetType().Name}: {ex.Message}");
                }
                else if (skippedCount % 10 == 0)
                {
                    Console.WriteLine($"Skipped {skippedCount} malformed messages so far...");
                }
                
                // Continue parsing - MimeParser should handle recovery automatically
            }
        }
        
        var parseEndTime = DateTime.Now;
        var totalParseDuration = parseEndTime - parseStartTime;
        var finalMessagesPerSecond = messageCount / totalParseDuration.TotalSeconds;
        
        // Report final results including skipped messages
        Console.WriteLine();
        Console.WriteLine("=== MBOX Parsing Summary ===");
        Console.WriteLine($"Parse end time: {parseEndTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Total parsing time: {totalParseDuration.TotalSeconds:F2} seconds");
        Console.WriteLine($"Messages successfully parsed: {messageCount}");
        Console.WriteLine($"Messages skipped (malformed): {skippedCount}");
        Console.WriteLine($"Total message processing attempts: {messageCount + skippedCount}");
        Console.WriteLine($"Success rate: {(double)messageCount / (messageCount + skippedCount) * 100:F1}%");
        Console.WriteLine($"Average processing speed: {finalMessagesPerSecond:F2} messages/second");
        Console.WriteLine($"File processing speed: {fileSizeInMB / totalParseDuration.TotalSeconds:F2} MB/second");
        
        if (skippedCount > 0)
        {
            Console.WriteLine($"Note: {skippedCount} messages were skipped due to malformed data, which is normal in MBOX files");
        }
        
        progress?.Report(new MboxParsingProgress 
        { 
            Message = $"Finished parsing MBOX file. Total messages found: {messageCount}, Skipped: {skippedCount}", 
            MessageCount = messageCount,
            FileSizeMB = fileSizeInMB,
            IsCompleted = true
        });
        
        Console.WriteLine("Yielding successfully parsed messages...");
        
        // Now yield all successfully parsed messages
        foreach (var message in messages)
        {
            yield return message;
        }
        
        Console.WriteLine("=== MBOX Parsing Completed ===");
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