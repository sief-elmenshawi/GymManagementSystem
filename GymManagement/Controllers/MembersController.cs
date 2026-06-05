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



        // Get BaseUrl/Members/MemberDetails/{id}
        // MemberDetails - show one member`s details
        public async Task<IActionResult> MemberDetails(int id , CancellationToken ct)
        {
            // Get Member By Id
            var member = await memberService.GetMemberDetailsByIdAsync(id, ct);
            if (member is null)
            {
                TempData["ErrorMessage"] = "Member Not Found";
                return RedirectToAction(nameof(Index));
            }
            return View(member);

        }

        //Get BaseUrl/Members/HealthRecordDetails/{id}
        // HealthRecordDetails - show one member`s details
        public async Task<IActionResult> HealthRecordDetails(int id, CancellationToken ct)
        {
            // Get Health Record By MemberId
            var result =  await memberService.GetMemberHealthRecordAsync(id, ct);
            if (result is null)
            {
                TempData["ErrorMessage"] = "Member Not Found";
                return RedirectToAction(nameof(Index));
            }
            return View(result);

            //Check Is Health Record null => return Index With Error Message
            //Health Record is Not Null => return View(Data)
        }

        #region Create Member

        // Get BaseUrl/Members/Create
        // Create - show empty form 
        [HttpGet]
        public IActionResult Create() => View();

        // Post BaseUrl/Members/Create {Member}
        // CreateMember - Submit form 
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
        [HttpGet]
        public async Task<IActionResult> EditMember(int id, CancellationToken ct)
        {
            // Get Member To Update By Id
            var member =await memberService.GetMemberToUpdatAsync(id, ct);
            if (member is null)
            {
                TempData["ErrorMessage"] = "Member Not Found";
                return RedirectToAction(nameof(Index));
            } 
            return View(member);
 

        }

        // Post BaseUrl/Members/Edit {Member}
        // Edit - Submit form 
        [HttpPost]
        public async Task<IActionResult> EditMember([FromRoute] int id, MemberToUpdateViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await memberService.UpdateMemberDetailsAsync(id, model, ct);
            if(result)
                TempData["SuccessMessage"] = "Member Updated Successfully";
            else
                TempData["ErrorMessage"] = "Failed To Update Member"; 
            return RedirectToAction(nameof(Index));

        }
        #endregion

        #region Delete Member 
        // Get BaseUrl/Members/Delete/{id}
        // Delete - Show form 
        [HttpGet]
        public async Task<IActionResult> Delete(int id , CancellationToken ct)
        {
            var member = await memberService.GetMemberDetailsByIdAsync(id, ct);
            if(member is null)
            {
                TempData["ErrorMessage"] = "Member Not Found";
                return RedirectToAction(nameof(Index));
            } return View(member);
        }

        // Post BaseUrl/Members/Delete {Member}
        // Delete - Submit form 
        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed([FromRoute] int id, CancellationToken ct)
        {
            var result =await memberService.RemoveMemberAsync(id, ct);
            if (result)
                TempData["SuccessMessage"] = "Member Deleted Successfully";
            else
                TempData["ErrorMessage"] = "Failed To Delete Member";
            return RedirectToAction(nameof(Index));
        }
        #endregion
     }
}