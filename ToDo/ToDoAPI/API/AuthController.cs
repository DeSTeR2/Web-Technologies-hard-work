using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Claims;
using ToDoAPI.Auth;
using ToDoAPI.Services;

namespace ToDoAPI.API
{
    [Route("auth/google")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserService _users;
        private readonly GoogleTokenStore _tokenStore;
        public AuthController(UserService users, GoogleTokenStore tokenStore)
        {
            _users = users;
            _tokenStore = tokenStore;
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string code)
        {
            if (string.IsNullOrEmpty(code)) return BadRequest("Missing code");

            var scheme = Request.Scheme;
            var host = Request.Host.HasValue ? Request.Host.Value : throw new InvalidOperationException("Request host is missing");
            var basePath = Request.PathBase.HasValue ? Request.PathBase.Value : string.Empty;
            var redirectUri = $"{scheme}://{host}{basePath}/auth/google/callback";

            if (!Request.Cookies.TryGetValue("google_code_verifier", out var codeVerifier) || string.IsNullOrWhiteSpace(codeVerifier))
                return BadRequest("Missing PKCE code_verifier (cookie). Try logging in again.");

            var tokens = await GoogleOAuthService.ExchangeCodeOnTokenAsync(code, codeVerifier, redirectUri);

            var accessToken = tokens.AccessToken ?? throw new InvalidOperationException("No access token returned");
            var userInfo = await FetchGoogleUserInfoAsync(accessToken);

            if (string.IsNullOrEmpty(userInfo?.Email))
                return BadRequest("Unable to obtain user email from Google");

            var user = await _users.EnsureUserByEmailAsync(userInfo.Value.Email, userInfo.Value.Name);

            await _tokenStore.SaveAsync(user.Id!, tokens);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id!),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.DisplayName ?? user.Email),
                new Claim("picture", userInfo.Value.Picture ?? "") 
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

            Response.Cookies.Delete("google_code_verifier");

            return Redirect("/Home");
        }

        private async Task<(string Email, string? Name, string? Picture)?> FetchGoogleUserInfoAsync(string accessToken)
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var resp = await http.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync();
            dynamic o = JsonConvert.DeserializeObject(json);

            string email = o.email;
            string name = o.name;
            string picture = o.picture; 

            return (email, name, picture);
        }

    }
}
