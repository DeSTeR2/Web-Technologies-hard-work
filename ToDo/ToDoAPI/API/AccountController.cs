using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ToDoAPI.Tool;

namespace ToDoAPI.API;

[ApiController]
[Route("[controller]/[action]")]
public class AccountController : Controller
{
    private readonly TokenProtector _tokenProtector;
    private readonly GoogleTokenStore _tokenStore;
    private readonly UserDataService _userDataService;

    public AccountController(GoogleTokenStore tokenStore, TokenProtector tokenProtector, UserDataService userDataService)
    {
        _userDataService = userDataService;
        _tokenStore = tokenStore;
        _tokenProtector = tokenProtector;
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }

    [HttpGet("UserPhoto")]
    public async Task<IActionResult> Get()
    {
        var userId = _userDataService.GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var cred = await _tokenStore.GetByUserIdAsync(userId);
        if (cred == null) return NotFound();

        var accessToken = cred.EncryptedAccessToken != null
            ? _tokenProtector.Unprotect(cred.EncryptedAccessToken)
            : null;
        if (accessToken == null) return NotFound();

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var resp = await http.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
        if (!resp.IsSuccessStatusCode) return StatusCode((int)resp.StatusCode);

        dynamic o = JsonConvert.DeserializeObject(await resp.Content.ReadAsStringAsync());
        string pictureUrl = o.picture;

        var imgBytes = await http.GetByteArrayAsync(pictureUrl);
        return File(imgBytes, "image/jpeg");
    }
}