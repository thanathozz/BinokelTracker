using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BinokelTracker.Models;
using BinokelTracker.Services;

namespace BinokelTracker.Services;

public class MeldScanService : IMeldScanService
{
    private readonly string _apiKey;
    private readonly IHttpClientFactory _httpFactory;

    private static readonly string Prompt = """
        Du bist ein Binokel-Kartenexperte. Analysiere das Foto einer Binokel-Kartenhand.
        Binokel verwendet württembergische Spielkarten mit den Farben: Eichel, Gras, Herz, Schellen.
        Kartenwerte: Ass, Zehn, König, Ober, Unter, Neun, Acht, Sieben.

        Erkenne:
        1. Die Trumpffarbe der Runde (falls ein Trumpfsymbol oder Farbe erkennbar ist)
        2. Alle vorhandenen Meldekombinationen:
           - Binokel (Ober Schellen + Unter Eichel): 40 Punkte
           - Doppelbinokel (2× Binokel): 300 Punkte
           - Konter in Trumpf (A-10-K-O-U einer Farbe in Trumpf): 150 Punkte
           - Konter in Farbe (A-10-K-O-U einer Farbe, nicht Trumpf): 150 Punkte
           - Familie in Trumpf (K+O einer Farbe in Trumpf): 40 Punkte
           - Familie in Farbe (K+O einer Farbe, nicht Trumpf): 20 Punkte
           - Vier Asse: 100 Punkte
           - Vier Zehner: 80 Punkte
           - Vier Könige: 60 Punkte
           - Vier Ober: 40 Punkte
           - Vier Unter: 40 Punkte

        Antworte AUSSCHLIESSLICH mit gültigem JSON (kein Markdown, keine Erklärung):
        {{"trumpf":"Herz","meldungen":[{{"name":"Binokel","punkte":40}}],"gesamt":40}}
        Mögliche trumpf-Werte: "Eichel", "Gras", "Herz", "Schellen" oder null.
        """;

    public MeldScanService(IHttpClientFactory httpFactory, AnthropicConfig config)
    {
        _httpFactory = httpFactory;
        _apiKey = config.ApiKey;
    }

    public bool IsAvailable => MediaPicker.Default.IsCaptureSupported && !string.IsNullOrEmpty(_apiKey);

    public async Task<MeldScanResult> ScanHandAsync(CancellationToken ct = default)
    {
        FileResult? photo;
        try
        {
            photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
            {
                Title = "Kartenhand fotografieren"
            });
        }
        catch (Exception ex)
        {
            return Fail($"Kamera-Fehler: {ex.Message}");
        }

        if (photo is null)
            return Fail(null);

        string base64;
        string mediaType;
        try
        {
            await using var stream = await photo.OpenReadAsync();
            var bytes = new byte[stream.Length];
            _ = await stream.ReadAsync(bytes, ct);
            base64 = Convert.ToBase64String(bytes);
            mediaType = photo.ContentType ?? "image/jpeg";
        }
        catch (Exception ex)
        {
            return Fail($"Bild konnte nicht gelesen werden: {ex.Message}");
        }

        return await CallClaudeAsync(base64, mediaType, ct);
    }

    private async Task<MeldScanResult> CallClaudeAsync(string base64, string mediaType, CancellationToken ct)
    {
        var requestBody = new
        {
            model = "claude-haiku-4-5-20251001",
            max_tokens = 512,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "image", source = new { type = "base64", media_type = mediaType, data = base64 } },
                        new { type = "text", text = Prompt }
                    }
                }
            }
        };

        using var client = _httpFactory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync("https://api.anthropic.com/v1/messages", content, ct);
        }
        catch (Exception ex)
        {
            return Fail($"Netzwerkfehler: {ex.Message}");
        }

        var responseText = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            return Fail($"API-Fehler {(int)response.StatusCode}");

        return ParseResponse(responseText);
    }

    private static MeldScanResult ParseResponse(string responseText)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseText);
            var text = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? "";

            var cleaned = text.Trim();
            if (cleaned.StartsWith("```")) cleaned = cleaned[(cleaned.IndexOf('\n') + 1)..];
            if (cleaned.EndsWith("```")) cleaned = cleaned[..cleaned.LastIndexOf("```")].Trim();

            using var result = JsonDocument.Parse(cleaned);
            var root = result.RootElement;

            TrumpSuit? trump = root.TryGetProperty("trumpf", out var t) && t.ValueKind != JsonValueKind.Null
                ? Enum.TryParse<TrumpSuit>(t.GetString(), out var ts) ? ts : null
                : null;

            var combinations = new List<DetectedMeld>();
            if (root.TryGetProperty("meldungen", out var meldungen))
            {
                foreach (var m in meldungen.EnumerateArray())
                {
                    var name   = m.TryGetProperty("name",   out var n) ? n.GetString() ?? "" : "";
                    var points = m.TryGetProperty("punkte", out var p) ? p.GetInt32()        : 0;
                    if (!string.IsNullOrEmpty(name))
                        combinations.Add(new DetectedMeld(name, points));
                }
            }

            var total = root.TryGetProperty("gesamt", out var g) ? g.GetInt32() : combinations.Sum(c => c.Points);
            return new MeldScanResult(true, combinations, trump, total, null, text);
        }
        catch (Exception ex)
        {
            return Fail($"Antwort konnte nicht verarbeitet werden: {ex.Message}");
        }
    }

    private static MeldScanResult Fail(string? error) =>
        new(false, new List<DetectedMeld>(), null, 0, error);
}
