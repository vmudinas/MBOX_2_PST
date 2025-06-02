using MboxToPstConverter;
using MboxToPstBlazorApp.Models;
using MboxToPstBlazorApp.Hubs;
using Microsoft.AspNetCore.SignalR;
using MimeKit;

namespace MboxToPstBlazorApp.Services
{
    public class IncrementalParsingService
    {
        private readonly MboxParser _mboxParser;
        private readonly UploadSessionService _sessionService;
        private readonly IHubContext<EmailParsingHub> _hubContext;
        private readonly Dictionary<string, long> _sessionParsePositions;
        private readonly ILogger<IncrementalParsingService> _logger;

        public IncrementalParsingService(
            UploadSessionService sessionService, 
            IHubContext<EmailParsingHub> hubContext,
            ILogger<IncrementalParsingService> logger)
        {
            _mboxParser = new MboxParser();
            _sessionService = sessionService;
            _hubContext = hubContext;
            _sessionParsePositions = new Dictionary<string, long>();
            _logger = logger;
            
            // Listen for session deletions to cleanup parsing data
            _sessionService.SessionDeleted += OnSessionDeleted;
        }

        private void OnSessionDeleted(string sessionId)
        {
            CleanupSessionData(sessionId);
        }

        public async Task<bool> TryParseNewChunks(string sessionId)
        {
            var session = _sessionService.GetSession(sessionId);
            if (session == null || !File.Exists(session.TempFilePath))
            {
                return false;
            }

            // Only parse .mbox files
            if (!session.FileName.EndsWith(".mbox", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            try
            {
                // Get the last parsed position for this session
                var lastPosition = _sessionParsePositions.GetValueOrDefault(sessionId, 0);

                // Check if there's new data to parse
                var fileInfo = new FileInfo(session.TempFilePath);
                if (fileInfo.Length <= lastPosition)
                {
                    return false; // No new data
                }

                _logger.LogInformation("Parsing new chunks for session {SessionId} from position {Position}", 
                    sessionId, lastPosition);

                _sessionService.UpdateSessionStatus(sessionId, UploadStatus.Parsing);

                var progress = new Progress<MboxParsingProgress>(p =>
                {
                    _logger.LogDebug("Parsing progress for session {SessionId}: {Message}", sessionId, p.Message);
                });

                var result = await Task.Run(() => 
                    _mboxParser.ParseMboxIncremental(session.TempFilePath, lastPosition, progress));

                // Convert MimeMessages to ParsedEmailInfo and add to session
                var newEmails = new List<ParsedEmailInfo>();
                foreach (var message in result.NewMessages)
                {
                    var emailInfo = new ParsedEmailInfo
                    {
                        Subject = message.Subject ?? "(No Subject)",
                        From = message.From?.ToString() ?? "Unknown",
                        To = message.To?.ToString() ?? "",
                        Date = message.Date.DateTime,
                        HasAttachments = message.Attachments.Any(),
                        Body = TruncateBody(message.TextBody ?? message.HtmlBody ?? "(No content)"),
                        Index = session.ParsedEmailCount
                    };

                    _sessionService.AddParsedEmail(sessionId, emailInfo);
                    newEmails.Add(emailInfo);
                }

                // Update parse position
                _sessionParsePositions[sessionId] = result.LastParsedPosition;

                _logger.LogInformation("Parsed {NewEmailCount} new emails for session {SessionId}. Total: {TotalCount}", 
                    result.NewMessages.Count, sessionId, session.ParsedEmailCount);

                // Send real-time notification if we have new emails
                if (newEmails.Count > 0)
                {
                    await _hubContext.NotifyNewEmails(sessionId, newEmails);
                }

                // Update session status
                if (session.Status == UploadStatus.Completed && !result.HasMoreData)
                {
                    _sessionService.UpdateSessionStatus(sessionId, UploadStatus.ParseCompleted);
                    await _hubContext.NotifyParsingStatus(sessionId, "ParseCompleted", session.ParsedEmailCount);
                }
                else if (session.Status != UploadStatus.Completed)
                {
                    _sessionService.UpdateSessionStatus(sessionId, UploadStatus.InProgress);
                    await _hubContext.NotifyParsingStatus(sessionId, "Parsing", session.ParsedEmailCount);
                }

                return result.NewMessages.Count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing chunks for session {SessionId}", sessionId);
                _sessionService.UpdateSessionStatus(sessionId, UploadStatus.Failed, 
                    $"Parsing error: {ex.Message}");
                return false;
            }
        }

        public void ResetParsePosition(string sessionId)
        {
            _sessionParsePositions.Remove(sessionId);
        }

        public long GetLastParsePosition(string sessionId)
        {
            return _sessionParsePositions.GetValueOrDefault(sessionId, 0);
        }

        private string TruncateBody(string body, int maxLength = 500)
        {
            if (string.IsNullOrEmpty(body) || body.Length <= maxLength)
                return body;

            return body.Substring(0, maxLength) + "...";
        }

        public void CleanupSessionData(string sessionId)
        {
            _sessionParsePositions.Remove(sessionId);
        }
    }
}