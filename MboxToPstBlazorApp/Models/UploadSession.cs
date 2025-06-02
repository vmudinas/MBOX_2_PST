using System.Collections.Concurrent;

namespace MboxToPstBlazorApp.Models
{
    public class UploadSession
    {
        public string Id { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long TotalSize { get; set; }
        public long UploadedSize { get; set; }
        public UploadStatus Status { get; set; } = UploadStatus.InProgress;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastChunkAt { get; set; } = DateTime.UtcNow;
        public string TempFilePath { get; set; } = string.Empty;
        public int ParsedEmailCount { get; set; }
        public List<ParsedEmailInfo> ParsedEmails { get; set; } = new();
        public string? ErrorMessage { get; set; }
        
        public double ProgressPercentage => TotalSize > 0 ? (double)UploadedSize / TotalSize * 100 : 0;
        public bool IsCompleted => Status == UploadStatus.Completed;
        public bool HasError => Status == UploadStatus.Failed;
    }

    public enum UploadStatus
    {
        InProgress,
        Completed, 
        Failed,
        Parsing,
        ParseCompleted
    }

    public class ParsedEmailInfo
    {
        public string Subject { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public bool HasAttachments { get; set; }
        public string Body { get; set; } = string.Empty;
        public int Index { get; set; }
    }

    public class UploadChunkRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        public byte[] ChunkData { get; set; } = Array.Empty<byte>();
        public bool IsLastChunk { get; set; }
    }

    public class UploadChunkResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public double ProgressPercentage { get; set; }
        public int ParsedEmailCount { get; set; }
    }

    public class UploadSessionInfo
    {
        public string Id { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long TotalSize { get; set; }
        public UploadStatus Status { get; set; }
        public double ProgressPercentage { get; set; }
        public int ParsedEmailCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }
}