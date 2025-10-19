using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ToDoAPI.Tool;

public class UserDataService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserDataService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ClaimsPrincipal GetCurrentUser() => _httpContextAccessor.HttpContext?.User;
    
    public string? GetCurrentUserId()
    {
        var user = GetCurrentUser();
        if (user == null)
            return null;

        var claim = user.FindFirst(ClaimTypes.NameIdentifier);
        return claim?.Value;
    }
}