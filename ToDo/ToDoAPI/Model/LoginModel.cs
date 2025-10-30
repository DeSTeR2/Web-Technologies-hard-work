using Microsoft.Extensions.Logging;

namespace ToDoAPI.Model;

public class LoginModel
{
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(ILogger<LoginModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
    }
}