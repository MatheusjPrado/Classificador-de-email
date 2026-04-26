using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

public class AiClassificationService
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;

    public AiClassificationService(string apiKey)
    {
        _apiKey = apiKey;
        _httpClient = new HttpClient();
    }

    public async Task<string> ClassificarEmail(string assunto, string conteudo)
    {
        string textoLimpo = LimparHtml(conteudo);
        string prompt = $@"
            Analise este e-mail e classifique-o em UMA destas categorias: [Financeiro, Promoção, Trabalho, Spam, Outros].
            
            Assunto: {assunto}
            Conteúdo: {textoLimpo.Substring(0, Math.Min(textoLimpo.Length, 1000))} 
            
            Responda APENAS com o nome da categoria, sem explicações.";

        var payload = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } }
        };

        string jsonPayload = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";
        
        var response = await _httpClient.PostAsync(url, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Erro da API Gemini: {responseBody}");

        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement.GetProperty("candidates")[0]
            .GetProperty("content").GetProperty("parts")[0]
            .GetProperty("text").GetString()?.Trim() ?? "Outros";
    }

    private string LimparHtml(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return Regex.Replace(Regex.Replace(input, "<.*?>", " "), @"\s+", " ").Trim();
    }
}