using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BinokelTracker.Models;

namespace BinokelTracker.Services;

public class SupabaseGameStorageService : IGameStorageService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient  _http;
    private readonly IAuthService _auth;

    public SupabaseGameStorageService(HttpClient http, IAuthService auth, SupabaseConfig config)
    {
        http.BaseAddress = new Uri(config.Url);
        http.DefaultRequestHeaders.Add("apikey", config.AnonKey);
        _http = http;
        _auth = auth;
    }

    public async Task<AppState> LoadAsync()
    {
        try
        {
            var gameRows = await GetAsync<List<GameRow>>(
                "/rest/v1/games?select=id,data&order=id") ?? [];

            var playerRows = await GetAsync<List<PlayerRow>>(
                "/rest/v1/known_players?select=name") ?? [];

            var spielrundeRows = await GetAsync<List<SpielrundeRow>>(
                "/rest/v1/spielrunden?select=id,data&order=id") ?? [];

            var spielrunden = spielrundeRows.Select(r => r.Data).ToList();
            foreach (var s in spielrunden)
                if (string.IsNullOrEmpty(s.GameType))
                    s.GameType = GameTypeInfo.Binokel;

            return new AppState
            {
                Games        = gameRows.Select(r => r.Data).ToList(),
                KnownPlayers = playerRows.Select(r => r.Name).ToList(),
                Spielrunden  = spielrunden
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Supabase LoadAsync failed: {ex.Message}");
            return new AppState();
        }
    }

    public async Task SaveAsync(AppState state)
    {
        try
        {
            var userId = _auth.Session!.UserId;

            // Upsert games
            if (state.Games.Count > 0)
            {
                var gameRows = state.Games.Select(g => new GameRow(g.Id, g, userId));
                await UpsertAsync("/rest/v1/games", gameRows);

                var ids = string.Join(",", state.Games.Select(g => g.Id));
                await DeleteAsync($"/rest/v1/games?id=not.in.({ids})");
            }
            else
            {
                await DeleteAsync("/rest/v1/games?id=gte.0");
            }

            // Upsert known players
            if (state.KnownPlayers.Count > 0)
            {
                var playerRows = state.KnownPlayers.Select(p => new PlayerRow(p, userId));
                await UpsertAsync("/rest/v1/known_players", playerRows);
            }

            // Upsert spielrunden
            if (state.Spielrunden.Count > 0)
            {
                var srRows = state.Spielrunden.Select(s => new SpielrundeRow(s.Id, s, userId));
                await UpsertAsync("/rest/v1/spielrunden", srRows);

                var srIds = string.Join(",", state.Spielrunden.Select(s => s.Id));
                await DeleteAsync($"/rest/v1/spielrunden?id=not.in.({srIds})");
            }
            else
            {
                await DeleteAsync("/rest/v1/spielrunden?id=gte.0");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Supabase SaveAsync failed: {ex.Message}");
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private async Task<T?> GetAsync<T>(string url)
    {
        var req  = await AuthorizedRequest(HttpMethod.Get, url);
        var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        var stream = await resp.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOpts);
    }

    private async Task UpsertAsync<T>(string url, IEnumerable<T> rows)
    {
        var json    = JsonSerializer.Serialize(rows, JsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var req     = await AuthorizedRequest(HttpMethod.Post, url);
        req.Content = content;
        req.Headers.Add("Prefer", "resolution=merge-duplicates,return=minimal");
        await _http.SendAsync(req);
    }

    private async Task DeleteAsync(string url)
    {
        var req = await AuthorizedRequest(HttpMethod.Delete, url);
        await _http.SendAsync(req);
    }

    private async Task<HttpRequestMessage> AuthorizedRequest(HttpMethod method, string url)
    {
        var token = await _auth.GetValidTokenAsync();
        var req   = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    private record GameRow(long Id, Game Data, string UserId);
    private record PlayerRow(string Name, string UserId);
    private record SpielrundeRow(long Id, Spielrunde Data, string UserId);
}
