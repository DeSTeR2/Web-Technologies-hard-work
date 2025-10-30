using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using ToDoAPI.Auth;

namespace ToDoAPI.API;

[ApiController]
[Route("api/auth/google")]
public class ApiAuthController : ControllerBase
{
    private const string CodeVerifierCookieName = "google_code_verifier";
    private const int CodeVerifierTtlSeconds = 600;

    [HttpGet("start")]
    public IActionResult Start([FromQuery] string scope = "")
    {
        var scopes = string.IsNullOrWhiteSpace(scope)
            ? new[]
            {
                "openid", "profile", "email", "https://www.googleapis.com/auth/userinfo.profile", "https://www.googleapis.com/auth/calendar.events"
            }
            : scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (!scopes.Contains("https://www.googleapis.com/auth/userinfo.profile"))
            scopes = scopes.Append("https://www.googleapis.com/auth/userinfo.profile").ToArray();

        var finalScope = string.Join(' ', scopes);

        string redirectUri;
        try
        {
            var scheme = Request.Scheme;
            var host = Request.Host.HasValue ? Request.Host.Value : throw new InvalidOperationException("Request host is missing");
            var basePath = Request.PathBase.HasValue ? Request.PathBase.Value : string.Empty;

            redirectUri = $"{scheme}://{host}{basePath}/auth/google/callback";
        }
        catch (Exception ex)
        {
            return BadRequest("Unable to determine redirect URI from request: " + ex.Message);
        }

        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = ComputeSha256Base64Url(codeVerifier);

        var url = GoogleOAuthService.GenerateOAuthRequestUrl(finalScope, redirectUri, codeChallenge);

        var opts = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            Expires = DateTimeOffset.UtcNow.AddSeconds(CodeVerifierTtlSeconds),
            SameSite = SameSiteMode.Lax,
            Path = "/"
        };
        Response.Cookies.Append(CodeVerifierCookieName, codeVerifier, opts);

        return Ok(new { url, redirectUri });
    }


    private static string GenerateCodeVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Base64UrlEncode(bytes);
    }

    private static string ComputeSha256Base64Url(string input)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.ASCII.GetBytes(input);
        var hash = sha.ComputeHash(bytes);
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] arg)
    {
        return WebEncoders.Base64UrlEncode(arg);
    }
}