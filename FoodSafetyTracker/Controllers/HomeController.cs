using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FoodSafetyTracker.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index() => RedirectToAction("Index", "Dashboard");

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        _logger.LogError("Error page displayed. RequestId={RequestId}", requestId);
        ViewBag.RequestId = requestId;
        return View();
    }
}
