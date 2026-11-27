using GymManagement.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class DashboardController : Controller
    {
        private readonly IAnalyticsService _analyticsService;

        public DashboardController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        public async Task<IActionResult> Index(CancellationToken ct = default)
        {
            var analytics = await _analyticsService.GetAnalyticsDataAysnc(ct);
            return View(analytics);
        }
    }
}
