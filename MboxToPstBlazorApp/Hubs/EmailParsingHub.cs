using Microsoft.AspNetCore.SignalR;
using MboxToPstBlazorApp.Models;

namespace MboxToPstBlazorApp.Hubs
{
    public class EmailParsingHub : Hub
    {
        public async Task JoinSessionGroup(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"session_{sessionId}");
        }

        public async Task LeaveSessionGroup(string sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session_{sessionId}");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Client will automatically be removed from all groups
            await base.OnDisconnectedAsync(exception);
        }
    }

    public static class EmailParsingHubExtensions
    {
        public static async Task NotifyNewEmails(this IHubContext<EmailParsingHub> hubContext, 
            string sessionId, 
            List<ParsedEmailInfo> newEmails)
        {
            await hubContext.Clients.Group($"session_{sessionId}")
                .SendAsync("NewEmailsParsed", new { SessionId = sessionId, Emails = newEmails });
        }

        public static async Task NotifyUploadProgress(this IHubContext<EmailParsingHub> hubContext, 
            string sessionId, 
            UploadSessionInfo sessionInfo)
        {
            await hubContext.Clients.Group($"session_{sessionId}")
                .SendAsync("UploadProgressUpdated", sessionInfo);
        }

        public static async Task NotifyParsingStatus(this IHubContext<EmailParsingHub> hubContext, 
            string sessionId, 
            string status, 
            int emailCount)
        {
            await hubContext.Clients.Group($"session_{sessionId}")
                .SendAsync("ParsingStatusUpdated", new { 
                    SessionId = sessionId, 
                    Status = status, 
                    EmailCount = emailCount 
                });
        }
    }
}