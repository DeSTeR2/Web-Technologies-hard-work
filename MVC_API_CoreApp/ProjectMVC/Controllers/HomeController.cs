using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProjectInfrastructure.Models;

namespace ProjectMVC.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly UserManager<User> _userManager;
    private readonly LeaderboardsController _leaderboardController;

    public HomeController(ILogger<HomeController> logger, UserManager<User> userManager, LeaderboardsController leaderboardController)
    {
        _logger = logger;
        _userManager = userManager;
        _leaderboardController = leaderboardController;
    }
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _leaderboardController.Get(user.Id) as OkObjectResult;
        var leaderboards = result?.Value;
        ViewBag.Leaderboards = JsonSerializer.Serialize(leaderboards);

        return View();
    }



    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}