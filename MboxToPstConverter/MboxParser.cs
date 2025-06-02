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

public class IncrementalParseResult
{
    public List<MimeMessage> NewMessages { get; set; } = new();
    public long LastParsedPosition { get; set; }
    public int TotalParsedCount { get; set; }
    public bool HasMoreData { get; set; }
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
        int skippedCount = 0;
        
        while (!parser.IsEndOfStream)
        {
            MimeMessage? message = null;
            
            try
            {
                message = parser.ParseMessage();
            }
            catch (Exception ex)
            {
                skippedCount++;
                // Log malformed message with index and error details
                Console.WriteLine($"Skipped malformed message at index {messageCount + skippedCount}: {ex.Message}");
                
                // Continue parsing - MimeParser should handle recovery automatically
                continue;
            }
            
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
                
                // Yield immediately for true streaming
                yield return message;
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

    /// <summary>
    /// Parse MBOX file incrementally from a specific position, useful for streaming uploads
    /// </summary>
    public IncrementalParseResult ParseMboxIncremental(string mboxFilePath, long startPosition = 0, IProgress<MboxParsingProgress>? progress = null)
    {
        if (!File.Exists(mboxFilePath))
        {
            throw new FileNotFoundException($"MBOX file not found: {mboxFilePath}");
        }

        var result = new IncrementalParseResult();
        var fileInfo = new FileInfo(mboxFilePath);
        var fileSizeInMB = fileInfo.Length / 1024.0 / 1024.0;

        // If file is smaller than start position, nothing to parse
        if (fileInfo.Length <= startPosition)
        {
            result.LastParsedPosition = startPosition;
            result.HasMoreData = false;
            return result;
        }

        progress?.Report(new MboxParsingProgress 
        { 
            Message = $"Parsing new content from position {startPosition} ({fileSizeInMB:F2} MB total)", 
            MessageCount = 0,
            FileSizeMB = fileSizeInMB
        });

        using var stream = File.OpenRead(mboxFilePath);
        
        // If starting from the beginning, parse normally
        if (startPosition == 0)
        {
            var parser = new MimeParser(stream, MimeFormat.Mbox);
            int messageCount = 0;
            
            while (!parser.IsEndOfStream)
            {
                try
                {
                    var message = parser.ParseMessage();
                    if (message != null)
                    {
                        result.NewMessages.Add(message);
                        messageCount++;
                        
                        if (messageCount % 10 == 0)
                        {
                            progress?.Report(new MboxParsingProgress 
                            { 
                                Message = $"Parsed {messageCount} new messages...", 
                                MessageCount = messageCount,
                                FileSizeMB = fileSizeInMB
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Skipped malformed message during incremental parse: {ex.Message}");
                    continue;
                }
            }
            
            result.LastParsedPosition = stream.Position;
            result.TotalParsedCount = messageCount;
        }
        else
        {
            // For incremental parsing from a position, we need to be more careful
            // Try to find a good starting point (look for message boundaries)
            result = ParseFromPosition(stream, startPosition, fileSizeInMB, progress);
        }

        result.HasMoreData = result.LastParsedPosition < fileInfo.Length;
        return result;
    }

    private IncrementalParseResult ParseFromPosition(FileStream stream, long startPosition, double fileSizeMB, IProgress<MboxParsingProgress>? progress)
    {
        var result = new IncrementalParseResult();
        
        // Seek to start position
        stream.Seek(startPosition, SeekOrigin.Begin);
        
        // Look backwards to find the last complete message boundary (line starting with "From ")
        var goodStartPosition = FindMessageBoundary(stream, startPosition);
        if (goodStartPosition != startPosition)
        {
            stream.Seek(goodStartPosition, SeekOrigin.Begin);
        }

        try
        {
            var parser = new MimeParser(stream, MimeFormat.Mbox);
            int messageCount = 0;
            
            while (!parser.IsEndOfStream)
            {
                try
                {
                    var message = parser.ParseMessage();
                    if (message != null)
                    {
                        result.NewMessages.Add(message);
                        messageCount++;
                        
                        if (messageCount % 5 == 0)
                        {
                            progress?.Report(new MboxParsingProgress 
                            { 
                                Message = $"Parsed {messageCount} new messages from incremental data...", 
                                MessageCount = messageCount,
                                FileSizeMB = fileSizeMB
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Skipped malformed message during incremental parse: {ex.Message}");
                    continue;
                }
            }
            
            result.LastParsedPosition = stream.Position;
            result.TotalParsedCount = messageCount;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during incremental parsing: {ex.Message}");
            result.LastParsedPosition = startPosition;
        }

        return result;
    }

    private long FindMessageBoundary(FileStream stream, long startPosition)
    {
        // Simple approach: go back and look for "From " at beginning of line
        var buffer = new byte[8192];
        var searchPosition = Math.Max(0, startPosition - 4096);
        
        stream.Seek(searchPosition, SeekOrigin.Begin);
        var bytesRead = stream.Read(buffer, 0, buffer.Length);
        
        // Look for "\nFrom " pattern
        for (int i = bytesRead - 6; i >= 0; i--)
        {
            if (buffer[i] == '\n' && 
                i + 5 < bytesRead &&
                buffer[i + 1] == 'F' && 
                buffer[i + 2] == 'r' && 
                buffer[i + 3] == 'o' && 
                buffer[i + 4] == 'm' && 
                buffer[i + 5] == ' ')
            {
                return searchPosition + i + 1; // Position right after the \n
            }
        }
        
        // If no boundary found, return original position
        return startPosition;
    }
}