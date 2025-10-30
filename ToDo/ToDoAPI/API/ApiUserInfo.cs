using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToDoAPI.Tool;

namespace ToDoAPI.API;

[ApiController]
[Route("api/[controller]")]
public class ApiUserInfo : ControllerBase
{
    private readonly UserDataService _userDataService;

    public ApiUserInfo(UserDataService userDataService)
    {
        _userDataService = userDataService;
    }

    [HttpGet]
    [Authorize]
    public IActionResult Get()
    {
        var user = _userDataService.GetCurrentUser();

        var nameClaim = user.FindFirst(ClaimTypes.Name);
        var name = nameClaim != null ? nameClaim.Value : "Unknown User";

        var pictureClaim = user.FindFirst("picture");
        var picture = pictureClaim != null ? pictureClaim.Value : "/images/default_user.jpg";

        return Ok(new
        {
            name,
            picture
        });
    }
}