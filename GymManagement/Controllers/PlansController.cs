using GymManagement.BLL.Services.Classes;
using GymManagement.BLL.Services.Interfaces;
using GymManagement.BLL.ViewModels.PlanViewModels;
using GymManagement.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.Controllers
{
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class PlansController : Controller
    {
        private readonly IPlanService planService;

        public PlansController(IPlanService planService)
        {
            this.planService = planService;
        }
        // Index Action 
        // GET BaseUrl/Plans/Index -> Listing All Plans
        public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
        {
            var plans = await planService.GetAllPlansAsync(ct, page);
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
            if (result.succes)
            {
                TempData["SuccessMessage"] = $"Plan updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            else
                TempData["ErrorMessage"] = result.error;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Activate(int id, CancellationToken ct)
        {
            var result = await planService.ToggleActivationAsync(id, ct);
            TempData[result.succes ? "SuccessMessage" : "ErrorMessage"] =
                result.succes ? "Plan status changed." : result.error;
            return RedirectToAction(nameof(Index)); ;
        }
    }
}