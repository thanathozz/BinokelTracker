using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BinokelTracker.Models;

namespace BinokelTracker.Services;

public class SupabaseGameStorageService : IGameStorageService
{
    private const string SupabaseUrl = "https://jabaodpqopmbgowtlvjx.supabase.co";
    private const string AnonKey = "sb_publishable_vXGsneQ6saP4EB4SbQ9uQQ_UhcLqW3S";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;

    public SupabaseGameStorageService(HttpClient http)
    {
        http.BaseAddress = new Uri(SupabaseUrl);
        http.DefaultRequestHeaders.Add("apikey", AnonKey);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AnonKey);
        _http = http;
    }

    public async Task<AppState> LoadAsync()
    {
        try
        {
            var gameRows = await _http.GetFromJsonAsync<List<GameRow>>(
                "/rest/v1/games?select=id,data&order=id", JsonOpts) ?? [];

            var playerRows = await _http.GetFromJsonAsync<List<PlayerRow>>(
                "/rest/v1/known_players?select=name") ?? [];

            return new AppState
            {
                Games = gameRows.Select(r => r.Data).ToList(),
                KnownPlayers = playerRows.Select(r => r.Name).ToList()
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
            // Upsert games
            if (state.Games.Count > 0)
            {
                var gameRows = state.Games.Select(g => new GameRow(g.Id, g));
                var gamesJson = JsonSerializer.Serialize(gameRows, JsonOpts);
                var gamesContent = new StringContent(gamesJson, Encoding.UTF8, "application/json");
                var upsertReq = new HttpRequestMessage(HttpMethod.Post, "/rest/v1/games")
                {
                    Content = gamesContent
                };
                upsertReq.Headers.Add("Prefer", "resolution=merge-duplicates,return=minimal");
                await _http.SendAsync(upsertReq);

                // Delete games removed from state
                var ids = string.Join(",", state.Games.Select(g => g.Id));
                await _http.DeleteAsync($"/rest/v1/games?id=not.in.({ids})");
            }
            else
            {
                // All games deleted – clear table
                await _http.DeleteAsync("/rest/v1/games?id=gte.0");
            }

            // Upsert known players
            if (state.KnownPlayers.Count > 0)
            {
                var playerRows = state.KnownPlayers.Select(p => new PlayerRow(p));
                var playersJson = JsonSerializer.Serialize(playerRows, JsonOpts);
                var playersContent = new StringContent(playersJson, Encoding.UTF8, "application/json");
                var upsertReq = new HttpRequestMessage(HttpMethod.Post, "/rest/v1/known_players")
                {
                    Content = playersContent
                };
                upsertReq.Headers.Add("Prefer", "resolution=merge-duplicates,return=minimal");
                await _http.SendAsync(upsertReq);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Supabase SaveAsync failed: {ex.Message}");
        }
    }

    private record GameRow(long Id, Game Data);
    private record PlayerRow(string Name);
}
