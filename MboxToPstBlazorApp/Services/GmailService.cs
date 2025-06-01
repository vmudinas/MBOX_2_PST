using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using MimeKit;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace MboxToPstBlazorApp.Services
{
    public class GmailService
    {
        private const string ApplicationName = "MBOX to PST Converter";
        private readonly string[] Scopes = { Google.Apis.Gmail.v1.GmailService.Scope.GmailReadonly };
        private Google.Apis.Gmail.v1.GmailService? _service;
        private UserCredential? _credential;

        public async Task<bool> AuthenticateAsync()
        {
            try
            {
                // For development, we'll use a simple client secrets approach
                // In production, this should be loaded from secure configuration
                var clientSecrets = new ClientSecrets
                {
                    ClientId = "your-client-id.googleusercontent.com",
                    ClientSecret = "your-client-secret"
                };

                _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    clientSecrets,
                    Scopes,
                    "user",
                    CancellationToken.None);

                _service = new Google.Apis.Gmail.v1.GmailService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = _credential,
                    ApplicationName = ApplicationName,
                });

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gmail authentication failed: {ex.Message}");
                return false;
            }
        }

        public async Task<List<GmailMessage>> FetchMessagesAsync(int maxResults = 50, string? query = null)
        {
            if (_service == null)
                throw new InvalidOperationException("Gmail service not authenticated. Call AuthenticateAsync first.");

            var messages = new List<GmailMessage>();

            try
            {
                var request = _service.Users.Messages.List("me");
                request.MaxResults = maxResults;
                if (!string.IsNullOrEmpty(query))
                    request.Q = query;

                var response = await request.ExecuteAsync();

                if (response.Messages != null)
                {
                    foreach (var messageItem in response.Messages)
                    {
                        var fullMessage = await _service.Users.Messages.Get("me", messageItem.Id).ExecuteAsync();
                        messages.Add(ConvertToGmailMessage(fullMessage));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching Gmail messages: {ex.Message}");
            }

            return messages;
        }

        public async Task<List<GmailMessage>> FetchMessagesSinceAsync(DateTime since, int maxResults = 50)
        {
            var query = $"after:{since:yyyy/MM/dd}";
            return await FetchMessagesAsync(maxResults, query);
        }

        private GmailMessage ConvertToGmailMessage(Message message)
        {
            var gmailMessage = new GmailMessage
            {
                Id = message.Id,
                ThreadId = message.ThreadId,
                Snippet = message.Snippet ?? string.Empty,
                InternalDate = DateTimeOffset.FromUnixTimeMilliseconds(message.InternalDate ?? 0).DateTime
            };

            if (message.Payload?.Headers != null)
            {
                foreach (var header in message.Payload.Headers)
                {
                    switch (header.Name?.ToLower())
                    {
                        case "subject":
                            gmailMessage.Subject = header.Value ?? string.Empty;
                            break;
                        case "from":
                            gmailMessage.From = header.Value ?? string.Empty;
                            break;
                        case "to":
                            gmailMessage.To = header.Value ?? string.Empty;
                            break;
                        case "date":
                            if (DateTime.TryParse(header.Value, out var date))
                                gmailMessage.Date = date;
                            break;
                    }
                }
            }

            return gmailMessage;
        }

        public bool IsAuthenticated => _credential != null && !_credential.Token.IsStale;

        public async Task<MimeMessage?> GetMimeMessageAsync(string messageId)
        {
            if (_service == null)
                return null;

            try
            {
                var request = _service.Users.Messages.Get("me", messageId);
                request.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Raw;
                
                var message = await request.ExecuteAsync();
                
                if (message.Raw != null)
                {
                    var rawData = Convert.FromBase64String(message.Raw.Replace('-', '+').Replace('_', '/'));
                    using var stream = new MemoryStream(rawData);
                    return await MimeMessage.LoadAsync(stream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting MIME message: {ex.Message}");
            }

            return null;
        }
    }

    public class GmailMessage
    {
        public string Id { get; set; } = string.Empty;
        public string ThreadId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public DateTime InternalDate { get; set; }
    }
}