using GymManagement.DAL.Data.Models;
using GymManagement.DAL.Repositorities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.Controllers
{
    public class PlansController : Controller
    {

        //public readonly GymDbContext dbContext;

        private readonly IGenericRepository<Plan> planRepository;

        public PlansController(IGenericRepository<Plan> planRepository)
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