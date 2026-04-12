namespace BinokelTracker.Services;

public record SupabaseAuthResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string RefreshToken,
    SupabaseUser User);

public record SupabaseUser(string Id, string Email, UserMetadata? UserMetadata = null);
public record UserMetadata(string? DisplayName = null);

public class AuthSession
{
    public string AccessToken  { get; init; } = "";
    public string RefreshToken { get; init; } = "";
    public long   ExpiresAt    { get; init; }
    public string UserId       { get; init; } = "";
    public string Email        { get; init; } = "";
    public string DisplayName  { get; set;  } = "";
    public bool   IsExpired    => DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= ExpiresAt - 60;
}

public record AuthResult(bool Success, string? ErrorMessage);
