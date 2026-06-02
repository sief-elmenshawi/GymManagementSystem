using GymManagement.DAL.Repositorities.Classes;
using GymManagement.DAL.Repositorities.Interfaces;
using GymManagement.DbContexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    public class PlansController : Controller
    {

        //public readonly GymDbContext dbContext;s

        private readonly IPlanRepository planRepository;

        public PlansController(IPlanRepository planRepository)
        {
            this.planRepository = planRepository;
        }
        // Index Action 
        // GET BaseUrl/Plans/Index -> Listing All Plans
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var plans = await planRepository.GetAllAsync(ct: ct);
            return View(plans);
        }

        // Details Action
        // GET BaseUrl/Plans/Details/{id}

        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            var plan = await planRepository.GetByIdAsync(id, ct);
            if (plan is null)
                return RedirectToAction(nameof(Index));
            else
                return View(plan);
        }


    }
}