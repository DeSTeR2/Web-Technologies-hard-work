using System.Security.Claims;

namespace ToDoAPI.Tool;

public class UserDataService
{
    private ClaimsPrincipal user;

    public ClaimsPrincipal GetCurrentUser() => user;

    public void SetUser(ClaimsPrincipal user) => this.user = user;

    public string? GetCurrentUserId()
    {
        var user = GetCurrentUser();
        if (user == null)
            return null;

        var claim = user.FindFirst(ClaimTypes.NameIdentifier);
        return claim?.Value;
    }
}