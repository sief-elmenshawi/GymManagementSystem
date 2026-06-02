using GymManagement.BLL.Service.Interfaces;
using GymManagement.BLL.ViewModels.MemberViewModels;
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.PL.Controllers
{
    public class MembersController : Controller
    {
        private readonly IMemberService memberService;

        public MembersController(IMemberService memberService)
        {
            this.memberService = memberService;
        }

        // Get BaseUrl/Member/Index
        // Index- List all Member
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var members = await memberService.GetAllMembersAsync(ct);
            return View(members);
        }



        // Get BaseUrl/MemberDetails/{id}
        // MemberDetails - show one member`s details

        // Get BaseUrl/Members/HealthRecordDetails/{id}
        // HealthRecordDetails - show one member`s details

        #region Create Member
        // Get BaseUrl/Members/Create
        // Create - show empty form 
        [HttpGet]
        public IActionResult Create() => View();

        // Post BaseUrl/Members/Create {Member}
        // CreateMEmber - Submit form 
        [HttpPost]
        public async Task<IActionResult> CreateMember(CreateMemberViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(nameof(Create), model);

            var result = await memberService.CreateMemberAsync(model, ct);
            if (result)
                TempData["SuccessMessage"] = "Member Created Successfully";
            else
                TempData["ErrorMessage"] = "Failed To Created Successfully";

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Edit Member 
        // Get BaseUrl/Members/Edit/{id}
        // Edit - Display edit form 

        // Post BaseUrl/Members/Edit {Member}
        // Edit - Submit form 
        #endregion

        #region Delete Member 
        // Get BaseUrl/Members/Delete/{id}
        // Delete - Show form 

        // Post BaseUrl/Members/Delete {Member}
        // Delete - Submit form 
        #endregion
    }
}