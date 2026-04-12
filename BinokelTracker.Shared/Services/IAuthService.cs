namespace BinokelTracker.Services;

public interface IAuthService
{
    AuthSession? Session { get; }
    event Action? SessionChanged;
    Task<bool>       TryRestoreSessionAsync();
    Task<AuthResult> LoginAsync(string email, string password);
    Task<AuthResult> RegisterAsync(string email, string password);
    Task             LogoutAsync();
    Task<string>     GetValidTokenAsync();
    Task<AuthResult> UpdateDisplayNameAsync(string displayName);
}
