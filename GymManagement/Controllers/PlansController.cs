using GymManagement.BLL.Services.Interfaces;
using GymManagement.BLL.ViewModels.PlanViewModels;
using GymManagement.DAL.Data.Models;
using GymManagement.DAL.Repositorities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.Controllers
{
    public class PlansController : Controller
    {
        private readonly IPlanService planService;

        public PlansController(IPlanService planService)
        {
            this.planService = planService;
        }
        // Index Action 
        // GET BaseUrl/Plans/Index -> Listing All Plans
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var plans = await planService.GetAllPlansAsync(ct);
            return View(plans);
        }

        // Details Action
        // GET BaseUrl/Plans/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            var plan = await planService.GetPlanByIdAsync(id, ct);

            if (plan is null)
            {
                TempData["ErrorMessage"] = $"Plan not found.";
                return RedirectToAction(nameof(Index));
            }
            else
                return View(plan);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            var plan = await planService.GetPlanToUpdateAsync(id, ct);
            if (plan is null)
            {
                TempData["ErrorMessage"] = $"Plan not found or has active memberships.";
                return RedirectToAction(nameof(Index));
            }
            return View(plan);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, UpdatePlanViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await planService.UpdatePlanAsync(id, model, ct);
            if (result)
                TempData["SuccessMessage"] = $"Plan updated successfully.";
            else
                TempData["ErrorMessage"] = $"Failed to update the plan.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Activate(int id, CancellationToken ct)
        {
            var result = await planService.ToggleActivationAsync(id, ct);
            if (result)
                TempData["SuccessMessage"] = $"Plan activation toggled successfully.";
            else
                TempData["ErrorMessage"] = $"Failed to toggle plan activation.";
            return RedirectToAction(nameof(Index));
        }
    }
}