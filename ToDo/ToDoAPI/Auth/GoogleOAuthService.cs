using Microsoft.AspNetCore.WebUtilities;

namespace ToDoAPI.Auth;

public class GoogleOAuthService
{
    private const string ClientSecret = "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";
    private const string ClientId = "1062607140347-dlpt886qrcd1pedio84s2u93am197s6t.apps.googleusercontent.com";

    private const string OAuthServerEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenServerEndpoint = "https://oauth2.googleapis.com/token";

    public static string GenerateOAuthRequestUrl(string scope, string redirectUrl, string codeChellange)
    {
        var queryParams = new Dictionary<string, string>
        {
            { "client_id", ClientId },
            { "redirect_uri", redirectUrl },
            { "response_type", "code" },
            { "scope", scope },
            { "code_challenge", codeChellange },
            { "code_challenge_method", "S256" },
            { "access_type", "offline" }
        };

        var url = QueryHelpers.AddQueryString(OAuthServerEndpoint, queryParams);
        return url;
    }

    public static async Task<TokenResult> ExchangeCodeOnTokenAsync(string code, string codeVerifier, string redirectUrl)
    {
        var authParams = new Dictionary<string, string>
        {
            { "client_id", ClientId },
            { "client_secret", ClientSecret },
            { "code", code },
            { "code_verifier", codeVerifier },
            { "grant_type", "authorization_code" },
            { "redirect_uri", redirectUrl }
        };

        var tokenResult = await HttpClientHelper.SendPostRequest<TokenResult>(TokenServerEndpoint, authParams);
        return tokenResult;
    }

    public static async Task<TokenResult> RefreshTokenAsync(string refreshToken)
    {
        var refreshParams = new Dictionary<string, string>
        {
            { "client_id", ClientId },
            { "client_secret", ClientSecret },
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken }
        };

        var tokenResult = await HttpClientHelper.SendPostRequest<TokenResult>(TokenServerEndpoint, refreshParams);

        return tokenResult;
    }
}