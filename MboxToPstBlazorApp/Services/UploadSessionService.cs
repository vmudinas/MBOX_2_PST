using System.Collections.Concurrent;
using MboxToPstBlazorApp.Models;

namespace MboxToPstBlazorApp.Services
{
    public class UploadSessionService
    {
        private readonly ConcurrentDictionary<string, UploadSession> _sessions = new();
        private readonly string _tempDirectory;

        public UploadSessionService()
        {
            _tempDirectory = Path.Combine(Directory.GetCurrentDirectory(), "temp_uploads");
            Directory.CreateDirectory(_tempDirectory);
        }

        public string CreateSession(string fileName, long totalSize)
        {
            var sessionId = Guid.NewGuid().ToString();
            var tempFilePath = Path.Combine(_tempDirectory, $"{sessionId}_{fileName}");
            
            var session = new UploadSession
            {
                Id = sessionId,
                FileName = fileName,
                TotalSize = totalSize,
                TempFilePath = tempFilePath,
                Status = UploadStatus.InProgress
            };

            _sessions[sessionId] = session;
            return sessionId;
        }

        public UploadSession? GetSession(string sessionId)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return session;
        }

        public List<UploadSessionInfo> GetAllSessions()
        {
            return _sessions.Values.Select(s => new UploadSessionInfo
            {
                Id = s.Id,
                FileName = s.FileName,
                TotalSize = s.TotalSize,
                Status = s.Status,
                ProgressPercentage = s.ProgressPercentage,
                ParsedEmailCount = s.ParsedEmailCount,
                CreatedAt = s.CreatedAt,
                ErrorMessage = s.ErrorMessage
            }).OrderByDescending(s => s.CreatedAt).ToList();
        }

        public async Task<bool> AppendChunk(string sessionId, byte[] chunkData, bool isLastChunk)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                return false;

            try
            {
                using (var fileStream = new FileStream(session.TempFilePath, FileMode.Append, FileAccess.Write))
                {
                    await fileStream.WriteAsync(chunkData, 0, chunkData.Length);
                }

                session.UploadedSize += chunkData.Length;
                session.LastChunkAt = DateTime.UtcNow;

                if (isLastChunk)
                {
                    session.Status = UploadStatus.Completed;
                }

                return true;
            }
            catch (Exception ex)
            {
                session.Status = UploadStatus.Failed;
                session.ErrorMessage = ex.Message;
                return false;
            }
        }

        public void UpdateSessionStatus(string sessionId, UploadStatus status, string? errorMessage = null)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                session.Status = status;
                if (errorMessage != null)
                    session.ErrorMessage = errorMessage;
            }
        }

        public void AddParsedEmail(string sessionId, ParsedEmailInfo email)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                session.ParsedEmails.Add(email);
                session.ParsedEmailCount = session.ParsedEmails.Count;
            }
        }

        public List<ParsedEmailInfo> GetParsedEmails(string sessionId, int page = 1, int pageSize = 20)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                return new List<ParsedEmailInfo>();

            return session.ParsedEmails
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public int GetParsedEmailCount(string sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                return 0;

            return session.ParsedEmailCount;
        }

        public bool DeleteSession(string sessionId)
        {
            if (_sessions.TryRemove(sessionId, out var session))
            {
                try
                {
                    if (File.Exists(session.TempFilePath))
                        File.Delete(session.TempFilePath);
                    return true;
                }
                catch
                {
                    // Log error but don't fail the deletion from memory
                    return true;
                }
            }
            return false;
        }

        public void CleanupOldSessions(TimeSpan maxAge)
        {
            var cutoffTime = DateTime.UtcNow - maxAge;
            var oldSessions = _sessions.Values
                .Where(s => s.LastChunkAt < cutoffTime)
                .Select(s => s.Id)
                .ToList();

            foreach (var sessionId in oldSessions)
            {
                DeleteSession(sessionId);
            }
        }
    }
}