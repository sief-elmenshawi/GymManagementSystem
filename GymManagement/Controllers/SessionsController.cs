using GymManagement.BLL.Services.Classes;
using GymManagement.BLL.Services.Interfaces;
using GymManagement.BLL.ViewModels.MemberViewModels;
using GymManagement.BLL.ViewModels.SessionViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace GymManagement.PL.Controllers
{
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class SessionsController : Controller
    {
        private readonly ISessionService sessionService;

        public SessionsController(ISessionService sessionService)
        {
            this.sessionService = sessionService;
        }


        public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
        {
            var sessions = await sessionService.GetAllSessionsAync(ct, page);
            return View(sessions);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            await PopulateDropDownAsync(ct);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateSessionViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropDownAsync(ct);
                return View(model);
            }

            var result = await sessionService.CreateSessionAsync(model , ct);
            if (result.succes)
            {
                TempData["SuccessMessage"] = "Session Created";
                return RedirectToAction(nameof(Index));
            }
            TempData["ErrorMessage"] = result.error;
            await PopulateDropDownAsync(ct);
            return View(model);

        }

        private async Task PopulateDropDownAsync(CancellationToken ct)
        {
            ViewBag.Trainers = new SelectList(await sessionService.GetTrainersForDropDownAsync(ct), "Id", "Name");
            ViewBag.Categories = new SelectList(await sessionService.GetCategoriesForDropDownAsync(ct), "Id", "CategoryName");
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id , CancellationToken ct)
        {
            var result = await sessionService.GetSessionByIdAysnc(id , ct);
            if(result.success) return View(result.value);
            else
            {
                TempData["ErrorMessage"] = result.error;
                return RedirectToAction(nameof(Index));
            }
            
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id , CancellationToken ct )
        {
            var result = await sessionService.GetSessionToUpdateAsync(id , ct);
            if(result.success)
            {
                ViewBag.Trainers = new SelectList(await sessionService.GetTrainersForDropDownAsync(ct), "Id", "Name");
                return View (result.value);
            }
            else
            {
                TempData["ErrorMessage"] = result.error;
                return RedirectToAction(nameof(Index));
            }
        }
        [HttpPost]
        public async Task<IActionResult> Edit(int id , UpdateSessionViewModel model , CancellationToken ct)
        {
            if(!ModelState.IsValid)
            {
                ViewBag.Trainers = new SelectList(await sessionService.GetTrainersForDropDownAsync(ct), "Id", "Name");
                return View(model);
            }
            var result = await sessionService.UpdateSessionAsync(id, model , ct);
            if(result.succes)
            {
                TempData["SuccessMessage"] = "Session Update";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                TempData["ErrorMessage"] = result.error;
                ViewBag.Trainers = new SelectList(await sessionService.GetTrainersForDropDownAsync(ct), "Id", "Name");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id , CancellationToken ct )
        {
            var result =await sessionService.GetSessionByIdAysnc(id, ct);
            if(result.success)
            {
                return View(result.value);
            }
            else
            {
                TempData["ErrorMessage"] = result.error;
                return RedirectToAction(nameof(Index));
            }

        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
        {
            var result = await sessionService.RemoveSessionAsync(id, ct);
            TempData[result.succes ? "SuccessMessage" : "ErrorMessage"] =
                result.succes ? "Session deleted successfully." : result.error;
            return RedirectToAction(nameof(Index));
        }
            
    }
}
