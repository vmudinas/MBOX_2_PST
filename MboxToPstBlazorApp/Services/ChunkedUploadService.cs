using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;
using MboxToPstBlazorApp.Models;

namespace MboxToPstBlazorApp.Services
{
    public class ChunkedUploadService
    {
        private readonly HttpClient _httpClient;
        private const int CHUNK_SIZE = 1024 * 1024; // 1MB chunks

        public ChunkedUploadService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> CreateUploadSession(string fileName, long totalSize)
        {
            var request = new { FileName = fileName, TotalSize = totalSize };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/upload/session", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseJson);
            return result.GetProperty("sessionId").GetString() ?? throw new InvalidOperationException("Failed to get session ID");
        }

        public async Task<UploadResult> UploadFileInChunks(
            IBrowserFile file, 
            string sessionId,
            IProgress<UploadProgressInfo>? progress = null)
        {
            try
            {
                var totalSize = file.Size;
                var uploadedSize = 0L;
                var chunkIndex = 0;

                using var stream = file.OpenReadStream(maxAllowedSize: 50L * 1024 * 1024 * 1024);
                var buffer = new byte[CHUNK_SIZE];
                
                while (uploadedSize < totalSize)
                {
                    var remainingSize = totalSize - uploadedSize;
                    var currentChunkSize = (int)Math.Min(CHUNK_SIZE, remainingSize);
                    
                    var actualRead = await stream.ReadAsync(buffer, 0, currentChunkSize);
                    if (actualRead == 0) break; // End of stream
                    
                    // Create chunk data
                    var chunkData = new byte[actualRead];
                    Array.Copy(buffer, chunkData, actualRead);
                    
                    var isLastChunk = uploadedSize + actualRead >= totalSize;
                    
                    // Upload chunk
                    var chunkResult = await UploadChunk(sessionId, chunkIndex, chunkData, isLastChunk);
                    if (!chunkResult.Success)
                    {
                        return new UploadResult { Success = false, Message = chunkResult.Message };
                    }
                    
                    uploadedSize += actualRead;
                    chunkIndex++;
                    
                    // Report progress
                    progress?.Report(new UploadProgressInfo
                    {
                        UploadedBytes = uploadedSize,
                        TotalBytes = totalSize,
                        ProgressPercentage = (double)uploadedSize / totalSize * 100,
                        ChunkIndex = chunkIndex,
                        ParsedEmailCount = chunkResult.ParsedEmailCount
                    });
                    
                    // Small delay to prevent overwhelming the server
                    await Task.Delay(10);
                }

                return new UploadResult { Success = true, Message = "Upload completed successfully" };
            }
            catch (Exception ex)
            {
                return new UploadResult { Success = false, Message = $"Upload failed: {ex.Message}" };
            }
        }

        private async Task<UploadChunkResponse> UploadChunk(string sessionId, int chunkIndex, byte[] chunkData, bool isLastChunk)
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(sessionId), "SessionId");
            form.Add(new StringContent(chunkIndex.ToString()), "ChunkIndex");
            form.Add(new StringContent(isLastChunk.ToString()), "IsLastChunk");
            form.Add(new ByteArrayContent(chunkData), "ChunkFile", "chunk.dat");

            var response = await _httpClient.PostAsync("/api/upload/chunk", form);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<UploadChunkResponse>(responseJson, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                }) ?? new UploadChunkResponse { Success = false, Message = "Failed to parse response" };
            }
            else
            {
                var errorResult = JsonSerializer.Deserialize<UploadChunkResponse>(responseJson, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                return errorResult ?? new UploadChunkResponse { Success = false, Message = "Unknown error occurred" };
            }
        }

        public async Task<UploadSessionInfo?> GetSessionStatus(string sessionId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/upload/session/{sessionId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<UploadSessionInfo>(json, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<EmailPageResponse?> GetParsedEmails(string sessionId, int page = 1, int pageSize = 20)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/upload/session/{sessionId}/emails?page={page}&pageSize={pageSize}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<EmailPageResponse>(json, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<UploadSessionInfo>> GetAllSessions()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/upload/sessions");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<UploadSessionInfo>>(json, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    }) ?? new List<UploadSessionInfo>();
                }
                return new List<UploadSessionInfo>();
            }
            catch
            {
                return new List<UploadSessionInfo>();
            }
        }
    }

    public class UploadResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class UploadProgressInfo
    {
        public long UploadedBytes { get; set; }
        public long TotalBytes { get; set; }
        public double ProgressPercentage { get; set; }
        public int ChunkIndex { get; set; }
        public int ParsedEmailCount { get; set; }
    }

    public class EmailPageResponse
    {
        public List<ParsedEmailInfo> Emails { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}