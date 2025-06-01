using MboxToPstConverter;
using MimeKit;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MboxToPstBlazorApp.Services
{
    public class EmailService
    {
        private readonly Converter _converter;
        private readonly MboxParser _mboxParser;
        private readonly PstReader _pstReader;

        public EmailService()
        {
            _converter = new Converter();
            _mboxParser = new MboxParser();
            _pstReader = new PstReader();
        }

        public Task<List<EmailSummary>> GetEmailsFromMboxAsync(string filePath)
        {
            return GetEmailsFromMboxAsync(filePath, null);
        }

        public async Task<List<EmailSummary>> GetEmailsFromMboxAsync(string filePath, IProgress<EmailLoadingProgress>? progress)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"MBOX file not found: {filePath}");

            return await Task.Run(() =>
            {
                var emails = new List<EmailSummary>();
                
                var mboxProgress = new Progress<MboxParsingProgress>(p =>
                {
                    progress?.Report(new EmailLoadingProgress 
                    { 
                        Message = p.Message, 
                        EmailCount = p.MessageCount,
                        IsCompleted = p.IsCompleted,
                        FileSizeMB = p.FileSizeMB
                    });
                });
                
                var messages = _mboxParser.ParseMboxFile(filePath, mboxProgress);
                
                int processedCount = 0;
                foreach (var message in messages)
                {
                    emails.Add(new EmailSummary
                    {
                        Subject = message.Subject ?? "(No Subject)",
                        From = message.From?.ToString() ?? "Unknown",
                        To = message.To?.ToString() ?? "",
                        Date = message.Date.DateTime,
                        HasAttachments = message.Attachments.Any(),
                        Body = message.TextBody ?? message.HtmlBody ?? "(No content)"
                    });
                    
                    processedCount++;
                    if (processedCount % 100 == 0)
                    {
                        progress?.Report(new EmailLoadingProgress 
                        { 
                            Message = $"Processed {processedCount} emails into summary format...", 
                            EmailCount = processedCount
                        });
                    }
                }

                progress?.Report(new EmailLoadingProgress 
                { 
                    Message = $"Successfully loaded {emails.Count} emails", 
                    EmailCount = emails.Count,
                    IsCompleted = true
                });

                return emails;
            });
        }

        public async Task<EmailMetadata> GetEmailMetadataFromMboxAsync(string filePath, IProgress<EmailLoadingProgress>? progress = null)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"MBOX file not found: {filePath}");

            return await Task.Run(async () =>
            {
                var mboxProgress = new Progress<MboxParsingProgress>(p =>
                {
                    progress?.Report(new EmailLoadingProgress 
                    { 
                        Message = p.Message, 
                        EmailCount = p.MessageCount,
                        IsCompleted = p.IsCompleted,
                        FileSizeMB = p.FileSizeMB
                    });
                });
                
                // Count total emails
                var totalCount = await _mboxParser.CountMboxMessagesAsync(filePath, mboxProgress);
                
                // Get first 20 emails for preview
                var previewMessages = await _mboxParser.GetFirstMboxMessagesAsync(filePath, 20, mboxProgress);
                
                var previewEmails = previewMessages.Select(message => new EmailSummary
                {
                    Subject = message.Subject ?? "(No Subject)",
                    From = message.From?.ToString() ?? "Unknown",
                    To = message.To?.ToString() ?? "",
                    Date = message.Date.DateTime,
                    HasAttachments = message.Attachments.Any(),
                    Body = message.TextBody ?? message.HtmlBody ?? "(No content)"
                }).ToList();

                progress?.Report(new EmailLoadingProgress 
                { 
                    Message = $"Successfully loaded metadata for {totalCount} emails (showing first {previewEmails.Count} for preview)", 
                    EmailCount = totalCount,
                    IsCompleted = true
                });

                return new EmailMetadata
                {
                    TotalEmailCount = totalCount,
                    PreviewEmails = previewEmails
                };
            });
        }

        public Task<List<EmailSummary>> GetEmailsFromPstAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"PST file not found: {filePath}");

            var emails = new List<EmailSummary>();
            var messages = _pstReader.ParsePstFile(filePath);
            
            foreach (var message in messages)
            {
                emails.Add(new EmailSummary
                {
                    Subject = message.Subject ?? "(No Subject)",
                    From = message.From?.ToString() ?? "Unknown",
                    To = message.To?.ToString() ?? "",
                    Date = message.Date.DateTime,
                    HasAttachments = message.Attachments.Any(),
                    Body = message.TextBody ?? message.HtmlBody ?? "(No content)"
                });
            }

            return Task.FromResult(emails);
        }

        public async Task<ConversionResult> ConvertMboxToPstAsync(string mboxPath, string pstPath, IProgress<ConversionProgress>? progress = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    progress?.Report(new ConversionProgress { ProgressPercentage = 0, Message = "Starting conversion..." });
                    
                    var parsingProgress = new Progress<MboxParsingProgress>(p =>
                    {
                        var percentage = Math.Min(40, (p.MessageCount > 0 ? p.MessageCount / 50 : 0));
                        progress?.Report(new ConversionProgress 
                        { 
                            ProgressPercentage = Math.Min(40, percentage), 
                            Message = $"Parsing: {p.Message}" 
                        });
                    });
                    
                    var conversionProgress = new Progress<PstConversionProgress>(p =>
                    {
                        var percentage = 40 + Math.Min(55, (p.ProcessedCount > 0 ? (p.ProcessedCount * 55) / Math.Max(1, p.ProcessedCount + p.FailedCount + 100) : 0));
                        if (p.IsCompleted) percentage = 100;
                        
                        progress?.Report(new ConversionProgress 
                        { 
                            ProgressPercentage = Math.Min(100, percentage), 
                            Message = $"Converting: {p.Message}" 
                        });
                    });
                    
                    _converter.ConvertMboxToPst(mboxPath, pstPath, parsingProgress, conversionProgress);
                    
                    progress?.Report(new ConversionProgress { ProgressPercentage = 100, Message = "Conversion completed successfully!" });
                    
                    return new ConversionResult { Success = true, Message = "Conversion completed successfully!" };
                }
                catch (Exception ex)
                {
                    progress?.Report(new ConversionProgress { ProgressPercentage = 0, Message = $"Conversion failed: {ex.Message}" });
                    return new ConversionResult { Success = false, Message = ex.Message };
                }
            });
        }

        public async Task<ConversionResult> ConvertPstToMboxAsync(string pstPath, string mboxPath, IProgress<ConversionProgress>? progress = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    progress?.Report(new ConversionProgress { ProgressPercentage = 0, Message = "Starting conversion..." });
                    
                    _converter.ConvertPstToMbox(pstPath, mboxPath);
                    
                    progress?.Report(new ConversionProgress { ProgressPercentage = 100, Message = "Conversion completed successfully!" });
                    
                    return new ConversionResult { Success = true, Message = "Conversion completed successfully!" };
                }
                catch (Exception ex)
                {
                    progress?.Report(new ConversionProgress { ProgressPercentage = 0, Message = $"Conversion failed: {ex.Message}" });
                    return new ConversionResult { Success = false, Message = ex.Message };
                }
            });
        }
    }

    public class EmailSummary
    {
        public string Subject { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public bool HasAttachments { get; set; }
        public string Body { get; set; } = string.Empty;
    }

    public class ConversionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ConversionProgress
    {
        public int ProgressPercentage { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class EmailLoadingProgress
    {
        public string Message { get; set; } = string.Empty;
        public int EmailCount { get; set; }
        public double FileSizeMB { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class EmailMetadata
    {
        public int TotalEmailCount { get; set; }
        public List<EmailSummary> PreviewEmails { get; set; } = new List<EmailSummary>();
    }
}