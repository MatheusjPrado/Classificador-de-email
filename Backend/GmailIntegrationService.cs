using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using System.Text;

public class GmailIntegrationService
{
    private readonly GmailService _gmailService;

    public GmailIntegrationService(UserCredential credential)
    {
        _gmailService = new GmailService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "EmailClassifier",
        });
    }

    // Lista os IDs dos e-mails recentes
    public async Task<List<string>> GetRecentMessageIds(int maxResults = 5)
    {
        var request = _gmailService.Users.Messages.List("me");
        request.MaxResults = maxResults;
        var response = await request.ExecuteAsync();

        return response.Messages?.Select(m => m.Id).ToList() ?? new List<string>();
    }

    // Busca o conteúdo completo de um e-mail específico
    public async Task<(string Subject, string Body)> GetMessageDetails(string messageId)
    {
        var request = _gmailService.Users.Messages.Get("me", messageId);
        // Pedimos o formato full para ter acesso ao corpo
        request.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
        var message = await request.ExecuteAsync();

        // Extrair Assunto
        string subject = message.Payload.Headers.FirstOrDefault(h => h.Name == "Subject")?.Value ?? "(Sem assunto)";

        // Extrair Corpo (o e-mail pode ser multipart, pegamos a primeira parte que seja texto)
        string body = string.Empty;
        if (message.Payload.Parts != null && message.Payload.Parts.Count > 0)
        {
            var part = message.Payload.Parts.FirstOrDefault(p => p.MimeType == "text/plain" || p.MimeType == "text/html");
            body = DecodeBase64(part?.Body?.Data);
        }
        else
        {
            body = DecodeBase64(message.Payload.Body?.Data);
        }

        return (subject, body);
    }

    // O Gmail API entrega o texto em Base64, precisamos converter
    private string DecodeBase64(string data)
    {
        if (string.IsNullOrEmpty(data)) return string.Empty;
        
        string str = data.Replace('-', '+').Replace('_', '/');
        byte[] buffer = Convert.FromBase64String(str);
        return Encoding.UTF8.GetString(buffer);
    }
}