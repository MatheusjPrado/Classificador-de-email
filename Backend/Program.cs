using System;
using System.IO;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var apiKey = builder.Configuration["Gemini:ApiKey"];

app.MapGet("/classificar-email/{id}", async (string id) =>
{
    try
    {
        if (string.IsNullOrEmpty(apiKey)) return Results.BadRequest("API Key do Gemini não configurada.");

        var credential = await GetCredentials();
        var gmailService = new GmailIntegrationService(credential);
        var details = await gmailService.GetMessageDetails(id);

        var aiService = new AiClassificationService(apiKey);
        var categoria = await aiService.ClassificarEmail(details.Subject, details.Body);

        return Results.Ok(new { 
            Assunto = details.Subject, 
            Categoria = categoria 
        });
    }
    catch (Exception ex) { return Results.Problem($"Erro na classificação: {ex.Message}"); }
});

app.Run();

async Task<UserCredential> GetCredentials()
{
    var credPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
    var path = !string.IsNullOrEmpty(credPath) ? credPath : "credentials.json";
    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
    return await GoogleWebAuthorizationBroker.AuthorizeAsync(
        GoogleClientSecrets.FromStream(stream).Secrets,
        new[] { GmailService.Scope.GmailReadonly },
        "user",
        CancellationToken.None
    );
}