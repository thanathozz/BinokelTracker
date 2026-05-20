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

    private readonly HttpClient   _http;
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
            var gameRows = await GetAsync<List<GameLoadRow>>(
                "/rest/v1/games?select=id,game_date,spielrunde_id,einsatz,finished,quick_winner,rules&order=id") ?? [];

            var playerRows = await GetAsync<List<PlayerRow>>(
                "/rest/v1/known_players?select=name") ?? [];

            var spielrundeRows = await GetAsync<List<SpielrundeRow>>(
                "/rest/v1/spielrunden?select=id,data&order=id") ?? [];

            List<GamePlayerLoadRow> gamePlayers = [];
            List<RoundLoadRow>      roundRows   = [];
            List<RoundScoreLoadRow> scoreRows   = [];

            if (gameRows.Count > 0)
            {
                gamePlayers = await GetAsync<List<GamePlayerLoadRow>>(
                    "/rest/v1/game_players?select=game_id,position,display_name,player_user_id&order=game_id,position") ?? [];
                roundRows = await GetAsync<List<RoundLoadRow>>(
                    "/rest/v1/rounds?select=game_id,position,type,bidder_position,bid,won,trumpf,last_trick_winner,custom_values&order=game_id,position") ?? [];
                scoreRows = await GetAsync<List<RoundScoreLoadRow>>(
                    "/rest/v1/round_player_scores?select=game_id,round_position,player_position,meld,tricks,abgegangen&order=game_id,round_position,player_position") ?? [];
            }

            var playersByGame = gamePlayers
                .GroupBy(p => p.GameId)
                .ToDictionary(g => g.Key, g => g.OrderBy(p => p.Position).ToList());

            var scoresByRound = scoreRows
                .GroupBy(s => (s.GameId, s.RoundPosition))
                .ToDictionary(g => g.Key, g => g.OrderBy(s => s.PlayerPosition).ToList());

            var roundsByGame = roundRows
                .GroupBy(r => r.GameId)
                .ToDictionary(g => g.Key, g => g
                    .OrderBy(r => r.Position)
                    .Select(r => ToRound(r, scoresByRound))
                    .ToList());

            var games = gameRows.Select(gm => new Game
            {
                Id           = gm.Id,
                Date         = gm.GameDate,
                SpielrundeId = gm.SpielrundeId,
                Einsatz      = gm.Einsatz,
                Finished     = gm.Finished,
                QuickWinner  = gm.QuickWinner,
                Rules        = gm.Rules ?? new RuleSet(),
                Players      = playersByGame.TryGetValue(gm.Id, out var pl)
                    ? pl.Select(p => new PlayerRef { DisplayName = p.DisplayName, UserId = p.PlayerUserId }).ToList()
                    : [],
                Rounds       = roundsByGame.TryGetValue(gm.Id, out var rl) ? rl : []
            }).ToList();

            var spielrunden = spielrundeRows.Select(r => r.Data).ToList();
            foreach (var s in spielrunden)
                if (string.IsNullOrEmpty(s.GameType))
                    s.GameType = GameTypeInfo.Binokel;

            return new AppState
            {
                Games        = games,
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

    private static Round ToRound(RoundLoadRow r, Dictionary<(long, int), List<RoundScoreLoadRow>> scoresByRound)
    {
        var scores = scoresByRound.TryGetValue((r.GameId, r.Position), out var sl) ? sl : [];
        return new Round
        {
            Id              = r.Position,
            Type            = Enum.TryParse<RoundType>(r.Type, out var rt) ? rt : RoundType.Normal,
            Bidder          = r.BidderPosition,
            Bid             = r.Bid,
            Won             = r.Won,
            Trumpf          = r.Trumpf != null && Enum.TryParse<TrumpSuit>(r.Trumpf, out var ts) ? ts : (TrumpSuit?)null,
            LastTrickWinner = r.LastTrickWinner,
            CustomValues    = r.CustomValues ?? new(),
            PlayerScores    = scores.Select(s => new PlayerScore
            {
                Meld       = s.Meld,
                Tricks     = s.Tricks,
                Abgegangen = s.Abgegangen
            }).ToList()
        };
    }

    public async Task<string?> SaveAsync(AppState state)
    {
        try
        {
            var userId = _auth.Session!.UserId;

            // Upsert game metadata (individual columns)
            if (state.Games.Count > 0)
            {
                var metaRows = state.Games.Select(g => new GameMetaRow(
                    g.Id, userId, g.Date, g.SpielrundeId, g.Einsatz, g.Finished, g.QuickWinner, g.Rules));
                await UpsertAsync("/rest/v1/games", metaRows);

                var ids = string.Join(",", state.Games.Select(g => g.Id));
                await DeleteAsync($"/rest/v1/games?id=not.in.({ids})");
            }
            else
            {
                await DeleteAsync("/rest/v1/games?id=gte.0");
            }

            // Upsert game_players
            var currentDisplayName = _auth.Session?.DisplayName;
            var gamePlayerRows = state.Games.SelectMany(g =>
                g.Players.Select((p, i) =>
                {
                    var effectivePlayerUserId = p.UserId
                        ?? (!string.IsNullOrEmpty(currentDisplayName) &&
                            string.Equals(p.DisplayName, currentDisplayName, StringComparison.OrdinalIgnoreCase)
                            ? userId : null);
                    return new GamePlayerRow(g.Id, i, p.DisplayName, effectivePlayerUserId, userId);
                }));
            await UpsertAsync("/rest/v1/game_players", gamePlayerRows);

            // Upsert rounds + round_player_scores (delete-then-insert for consistency)
            if (state.Games.Count > 0)
            {
                var gameIds = string.Join(",", state.Games.Select(g => g.Id));
                await DeleteAsync($"/rest/v1/rounds?game_id=in.({gameIds})");

                var roundRows = state.Games.SelectMany(g =>
                    g.Rounds.Select((r, pos) => new RoundRow(
                        g.Id, pos, userId,
                        r.Type.ToString(),
                        r.Bidder, r.Bid, r.Won,
                        r.Trumpf?.ToString(),
                        r.LastTrickWinner,
                        r.CustomValues)));
                if (roundRows.Any())
                    await UpsertAsync("/rest/v1/rounds", roundRows);

                var scoreRows = state.Games
                    .SelectMany(g => g.Rounds.Select((r, rpos) => (g, r, rpos)))
                    .SelectMany(t => t.r.PlayerScores.Select((ps, ppos) =>
                        new RoundPlayerScoreRow(t.g.Id, t.rpos, ppos, userId,
                            ps.Meld, ps.Tricks, ps.Abgegangen)));
                if (scoreRows.Any())
                    await UpsertAsync("/rest/v1/round_player_scores", scoreRows);
            }

            // Upsert known players
            if (state.KnownPlayers.Count > 0)
            {
                var kpRows = state.KnownPlayers.Select(p => new PlayerRow(p, userId));
                await UpsertAsync("/rest/v1/known_players", kpRows);
            }

            // Upsert spielrunden (still JSON blob)
            var ownedSr = state.Spielrunden
                .Where(s => string.IsNullOrEmpty(s.CreatorUserId) || s.CreatorUserId == userId)
                .ToList();
            if (ownedSr.Count > 0)
            {
                var srRows = ownedSr.Select(s => new SpielrundeRow(s.Id, s, userId));
                await UpsertAsync("/rest/v1/spielrunden", srRows);

                var srIds = string.Join(",", ownedSr.Select(s => s.Id));
                await DeleteAsync($"/rest/v1/spielrunden?id=not.in.({srIds})");
            }
            else
            {
                await DeleteAsync("/rest/v1/spielrunden?id=gte.0");
            }

            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Supabase SaveAsync failed: {ex.Message}");
            return ex.Message;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

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
        var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new Exception($"{(int)resp.StatusCode} {resp.ReasonPhrase}: {body}");
        }
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

    public async Task<(string UserId, string DisplayName)?> FindProfileByDisplayNameAsync(string displayName)
    {
        try
        {
            var encoded = Uri.EscapeDataString(displayName);
            var req  = await AuthorizedRequest(HttpMethod.Get, $"/rest/v1/profiles?display_name=ilike.{encoded}&select=user_id,display_name");
            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return null;
            var stream = await resp.Content.ReadAsStreamAsync();
            var rows = await JsonSerializer.DeserializeAsync<ProfileRow[]>(stream, JsonOpts);
            if (rows?.Length > 0) return (rows[0].UserId, rows[0].DisplayName);
            return null;
        }
        catch { return null; }
    }

    public async Task DeleteSpielrundeAsync(long spielrundeId)
    {
        await DeleteAsync($"/rest/v1/games?spielrunde_id=eq.{spielrundeId}");
        await DeleteAsync($"/rest/v1/spielrunden?id=eq.{spielrundeId}");
    }

    public async Task AddSpielrundeMembersAsync(long spielrundeId, IEnumerable<(string UserId, string DisplayName)> members)
    {
        var rows = members.Select(m => new MemberRow(spielrundeId, m.UserId, m.DisplayName)).ToList();
        if (rows.Count == 0) return;
        var json    = JsonSerializer.Serialize(rows, JsonOpts);
        var req     = await AuthorizedRequest(HttpMethod.Post, "/rest/v1/spielrunde_members");
        req.Content = new StringContent(json, Encoding.UTF8, "application/json");
        req.Headers.Add("Prefer", "return=minimal");
        var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new Exception($"{(int)resp.StatusCode} {resp.ReasonPhrase}: {body}");
        }
    }

    // ── Data Transfer Records ─────────────────────────────────────────────────

    private record GameMetaRow(
        long                                                       Id,
        [property: JsonPropertyName("user_id")]        string     UserId,
        [property: JsonPropertyName("game_date")]      long       GameDate,
        [property: JsonPropertyName("spielrunde_id")]  long?      SpielrundeId,
        string                                                     Einsatz,
        bool                                                       Finished,
        [property: JsonPropertyName("quick_winner")]   int?       QuickWinner,
        RuleSet                                                    Rules);

    private record GameLoadRow(
        long                                                       Id,
        [property: JsonPropertyName("game_date")]      long       GameDate,
        [property: JsonPropertyName("spielrunde_id")]  long?      SpielrundeId,
        string                                                     Einsatz,
        bool                                                       Finished,
        [property: JsonPropertyName("quick_winner")]   int?       QuickWinner,
        RuleSet?                                                   Rules);

    private record GamePlayerLoadRow(
        [property: JsonPropertyName("game_id")]         long     GameId,
        int                                                       Position,
        [property: JsonPropertyName("display_name")]    string   DisplayName,
        [property: JsonPropertyName("player_user_id")]  string?  PlayerUserId);

    private record RoundLoadRow(
        [property: JsonPropertyName("game_id")]            long                        GameId,
        int                                                                             Position,
        string                                                                          Type,
        [property: JsonPropertyName("bidder_position")]    int                         BidderPosition,
        int                                                                             Bid,
        bool                                                                            Won,
        string?                                                                         Trumpf,
        [property: JsonPropertyName("last_trick_winner")]  int                         LastTrickWinner,
        [property: JsonPropertyName("custom_values")]      Dictionary<string, string>  CustomValues);

    private record RoundScoreLoadRow(
        [property: JsonPropertyName("game_id")]          long   GameId,
        [property: JsonPropertyName("round_position")]   int    RoundPosition,
        [property: JsonPropertyName("player_position")]  int    PlayerPosition,
        int                                                      Meld,
        int                                                      Tricks,
        bool                                                     Abgegangen);

    private record PlayerRow(string Name, [property: JsonPropertyName("user_id")] string UserId);

    private record SpielrundeRow(long Id, Spielrunde Data, [property: JsonPropertyName("user_id")] string UserId);

    private record ProfileRow(
        [property: JsonPropertyName("user_id")]      string UserId,
        [property: JsonPropertyName("display_name")] string DisplayName);

    private record MemberRow(
        [property: JsonPropertyName("spielrunde_id")] long   SpielrundeId,
        [property: JsonPropertyName("user_id")]       string UserId,
        [property: JsonPropertyName("display_name")]  string DisplayName);

    private record GamePlayerRow(
        [property: JsonPropertyName("game_id")]         long     GameId,
        int                                                       Position,
        [property: JsonPropertyName("display_name")]    string   DisplayName,
        [property: JsonPropertyName("player_user_id")]  string?  PlayerUserId,
        [property: JsonPropertyName("user_id")]         string   UserId);

    private record RoundRow(
        [property: JsonPropertyName("game_id")]            long                        GameId,
        int                                                                             Position,
        [property: JsonPropertyName("user_id")]            string                      UserId,
        string                                                                          Type,
        [property: JsonPropertyName("bidder_position")]    int                         BidderPosition,
        int                                                                             Bid,
        bool                                                                            Won,
        string?                                                                         Trumpf,
        [property: JsonPropertyName("last_trick_winner")]  int                         LastTrickWinner,
        [property: JsonPropertyName("custom_values")]      Dictionary<string, string>  CustomValues);

    private record RoundPlayerScoreRow(
        [property: JsonPropertyName("game_id")]          long     GameId,
        [property: JsonPropertyName("round_position")]   int      RoundPosition,
        [property: JsonPropertyName("player_position")]  int      PlayerPosition,
        [property: JsonPropertyName("user_id")]          string   UserId,
        int                                                        Meld,
        int                                                        Tricks,
        bool                                                       Abgegangen);
}
