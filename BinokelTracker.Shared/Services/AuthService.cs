using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;

namespace BinokelTracker.Services;

public class AuthService : IAuthService
{
    private const string SupabaseUrl = "https://jabaodpqopmbgowtlvjx.supabase.co";
    private const string AnonKey     = "sb_publishable_vXGsneQ6saP4EB4SbQ9uQQ_UhcLqW3S";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly IJSRuntime _js;

    public AuthSession? Session { get; private set; }
    public event Action? SessionChanged;

    public AuthService(HttpClient http, IJSRuntime js)
    {
        http.BaseAddress = new Uri(SupabaseUrl);
        http.DefaultRequestHeaders.Add("apikey", AnonKey);
        _http = http;
        _js   = js;
    }

    public async Task<bool> TryRestoreSessionAsync()
    {
        try
        {
            var stored = await _js.InvokeAsync<StoredSession>("BinokelAuth.loadSession");
            if (string.IsNullOrEmpty(stored.AccessToken)) return false;

            var candidate = new AuthSession
            {
                AccessToken  = stored.AccessToken,
                RefreshToken = stored.RefreshToken,
                ExpiresAt    = stored.ExpiresAt,
                UserId       = ExtractJwtClaim(stored.AccessToken, "sub")   ?? "",
                Email        = ExtractJwtClaim(stored.AccessToken, "email") ?? ""
            };

            if (!candidate.IsExpired)
            {
                Session = candidate;
                SessionChanged?.Invoke();
                return true;
            }

            if (!string.IsNullOrEmpty(stored.RefreshToken))
            {
                var refreshed = await RefreshAsync(stored.RefreshToken);
                if (refreshed != null)
                {
                    Session = refreshed;
                    await PersistSessionAsync(refreshed);
                    SessionChanged?.Invoke();
                    return true;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
        => await PostAuthAsync("/auth/v1/token?grant_type=password", new { email, password });

    public async Task<AuthResult> RegisterAsync(string email, string password)
        => await PostAuthAsync("/auth/v1/signup", new { email, password });

    public async Task LogoutAsync()
    {
        try
        {
            if (Session is not null)
            {
                var req = new HttpRequestMessage(HttpMethod.Post, "/auth/v1/logout");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.AccessToken);
                await _http.SendAsync(req);
            }
        }
        catch { }
        finally
        {
            Session = null;
            await _js.InvokeVoidAsync("BinokelAuth.clearSession");
            SessionChanged?.Invoke();
        }
    }

    public async Task<string> GetValidTokenAsync()
    {
        if (Session is null)
            throw new InvalidOperationException("Not authenticated");

        if (Session.IsExpired)
        {
            var refreshed = await RefreshAsync(Session.RefreshToken);
            if (refreshed is null)
                throw new InvalidOperationException("Session expired");
            Session = refreshed;
            await PersistSessionAsync(refreshed);
            SessionChanged?.Invoke();
        }

        return Session.AccessToken;
    }

    // ── Private helpers ─────────────────────────────────────────────

    private async Task<AuthResult> PostAuthAsync(string path, object body)
    {
        try
        {
            var json    = JsonSerializer.Serialize(body, JsonOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp    = await _http.PostAsync(path, content);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync();
                return new AuthResult(false, ParseSupabaseError(err));
            }
            var authResp = await resp.Content.ReadFromJsonAsync<SupabaseAuthResponse>(JsonOpts);
            if (authResp is null) return new AuthResult(false, "Leere Antwort vom Server");

            var session = ToSession(authResp);
            Session = session;
            await PersistSessionAsync(session);
            SessionChanged?.Invoke();
            return new AuthResult(true, null);
        }
        catch (Exception ex)
        {
            return new AuthResult(false, ex.Message);
        }
    }

    private async Task<AuthSession?> RefreshAsync(string refreshToken)
    {
        try
        {
            var body    = JsonSerializer.Serialize(new { refresh_token = refreshToken });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var resp    = await _http.PostAsync("/auth/v1/token?grant_type=refresh_token", content);
            if (!resp.IsSuccessStatusCode) return null;
            var authResp = await resp.Content.ReadFromJsonAsync<SupabaseAuthResponse>(JsonOpts);
            return authResp is null ? null : ToSession(authResp);
        }
        catch
        {
            return null;
        }
    }

    private static AuthSession ToSession(SupabaseAuthResponse r) => new()
    {
        AccessToken  = r.AccessToken,
        RefreshToken = r.RefreshToken,
        ExpiresAt    = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + r.ExpiresIn,
        UserId       = r.User.Id,
        Email        = r.User.Email
    };

    private async Task PersistSessionAsync(AuthSession s)
        => await _js.InvokeVoidAsync("BinokelAuth.saveSession",
               s.AccessToken, s.RefreshToken, s.ExpiresAt);

    private static string? ExtractJwtClaim(string jwt, string claim)
    {
        try
        {
            var parts  = jwt.Split('.');
            if (parts.Length < 2) return null;
            var padded = parts[1].PadRight(parts[1].Length + (4 - parts[1].Length % 4) % 4, '=');
            var bytes  = Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/'));
            var doc    = JsonDocument.Parse(bytes);
            return doc.RootElement.TryGetProperty(claim, out var el) ? el.GetString() : null;
        }
        catch { return null; }
    }

    private static string ParseSupabaseError(string body)
    {
        try
        {
            var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error_description", out var d)) return d.GetString() ?? "Fehler";
            if (doc.RootElement.TryGetProperty("msg", out var m)) return m.GetString() ?? "Fehler";
            if (doc.RootElement.TryGetProperty("message", out var msg)) return msg.GetString() ?? "Fehler";
        }
        catch { }
        return "Anmeldung fehlgeschlagen";
    }

    private record StoredSession(string AccessToken, string RefreshToken, long ExpiresAt);
}
