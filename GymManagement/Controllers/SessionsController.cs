using GymManagement.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.PL.Controllers
{
    public class SessionsController : Controller
    {
        private readonly ISessionService sessionService;

        public SessionsController(ISessionService sessionService)
        {
            this.sessionService = sessionService;
        }


        public async Task<IActionResult> Index(CancellationToken ct )
        {
            var sessions = await sessionService.GetAllSessionsAync(ct);
            return View(sessions);
        }
    }
}
