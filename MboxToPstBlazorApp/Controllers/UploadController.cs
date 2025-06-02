using Microsoft.AspNetCore.Mvc;
using MboxToPstBlazorApp.Models;
using MboxToPstBlazorApp.Services;

namespace MboxToPstBlazorApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly UploadSessionService _sessionService;
        private readonly IncrementalParsingService _parsingService;
        private readonly ILogger<UploadController> _logger;

        public UploadController(
            UploadSessionService sessionService, 
            IncrementalParsingService parsingService,
            ILogger<UploadController> logger)
        {
            _sessionService = sessionService;
            _parsingService = parsingService;
            _logger = logger;
        }

        [HttpPost("session")]
        public IActionResult CreateSession([FromBody] CreateSessionRequest request)
        {
            try
            {
                var sessionId = _sessionService.CreateSession(request.FileName, request.TotalSize);
                return Ok(new { SessionId = sessionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create upload session");
                return BadRequest(new { Message = "Failed to create upload session" });
            }
        }

        [HttpPost("chunk")]
        public async Task<IActionResult> UploadChunk([FromForm] UploadChunkFormRequest request)
        {
            try
            {
                if (request.ChunkFile == null || request.ChunkFile.Length == 0)
                {
                    return BadRequest(new { Message = "No chunk data provided" });
                }

                var session = _sessionService.GetSession(request.SessionId);
                if (session == null)
                {
                    return NotFound(new { Message = "Upload session not found" });
                }

                // Read chunk data
                using var memoryStream = new MemoryStream();
                await request.ChunkFile.CopyToAsync(memoryStream);
                var chunkData = memoryStream.ToArray();

                var success = await _sessionService.AppendChunk(
                    request.SessionId, 
                    chunkData, 
                    request.IsLastChunk);

                if (!success)
                {
                    return BadRequest(new { Message = "Failed to process chunk" });
                }

                // Try to parse new emails incrementally (async, don't wait)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _parsingService.TryParseNewChunks(request.SessionId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during incremental parsing for session {SessionId}", request.SessionId);
                    }
                });

                var updatedSession = _sessionService.GetSession(request.SessionId);
                return Ok(new UploadChunkResponse
                {
                    Success = true,
                    Message = "Chunk uploaded successfully",
                    ProgressPercentage = updatedSession?.ProgressPercentage ?? 0,
                    ParsedEmailCount = updatedSession?.ParsedEmailCount ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload chunk for session {SessionId}", request.SessionId);
                return BadRequest(new UploadChunkResponse
                {
                    Success = false,
                    Message = $"Failed to upload chunk: {ex.Message}"
                });
            }
        }

        [HttpGet("session/{sessionId}")]
        public IActionResult GetSessionStatus(string sessionId)
        {
            var session = _sessionService.GetSession(sessionId);
            if (session == null)
            {
                return NotFound(new { Message = "Session not found" });
            }

            return Ok(new UploadSessionInfo
            {
                Id = session.Id,
                FileName = session.FileName,
                TotalSize = session.TotalSize,
                Status = session.Status,
                ProgressPercentage = session.ProgressPercentage,
                ParsedEmailCount = session.ParsedEmailCount,
                CreatedAt = session.CreatedAt,
                ErrorMessage = session.ErrorMessage
            });
        }

        [HttpGet("sessions")]
        public IActionResult GetAllSessions()
        {
            var sessions = _sessionService.GetAllSessions();
            return Ok(sessions);
        }

        [HttpGet("session/{sessionId}/emails")]
        public IActionResult GetParsedEmails(string sessionId, int page = 1, int pageSize = 20)
        {
            var emails = _sessionService.GetParsedEmails(sessionId, page, pageSize);
            var totalCount = _sessionService.GetParsedEmailCount(sessionId);
            
            return Ok(new
            {
                Emails = emails,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }

        [HttpDelete("session/{sessionId}")]
        public IActionResult DeleteSession(string sessionId)
        {
            var success = _sessionService.DeleteSession(sessionId);
            if (success)
            {
                return Ok(new { Message = "Session deleted successfully" });
            }
            return NotFound(new { Message = "Session not found" });
        }
    }

    public class CreateSessionRequest
    {
        public string FileName { get; set; } = string.Empty;
        public long TotalSize { get; set; }
    }

    public class UploadChunkFormRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        public bool IsLastChunk { get; set; }
        public IFormFile? ChunkFile { get; set; }
    }
}