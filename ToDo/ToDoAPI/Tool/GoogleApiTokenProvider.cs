using ToDoAPI.Auth;

namespace ToDoAPI.Tool;

public class GoogleApiTokenProvider
{
    private readonly GoogleTokenStore _store;

    public GoogleApiTokenProvider(GoogleTokenStore store)
    {
        _store = store;
    }

    public async Task<string> GetAccessTokenForUserAsync(string userId)
    {
        var cred = await _store.GetByUserIdAsync(userId);
        if (cred == null) throw new InvalidOperationException("No Google credentials");

        var accessToken = _store.UnprotectAccessToken(cred);
        if (!string.IsNullOrEmpty(accessToken) && cred.ExpiresAtUtc > DateTime.UtcNow.AddMinutes(1))
            return accessToken;

        var refreshToken = _store.UnprotectRefreshToken(cred);
        var tokens = await GoogleOAuthService.RefreshTokenAsync(refreshToken);

        await _store.SaveAsync(userId, tokens);

        var newAccess = tokens.AccessToken ?? throw new InvalidOperationException("No access token from refresh");
        return newAccess;
    }
}