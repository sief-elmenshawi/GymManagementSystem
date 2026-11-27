using GymManagement.BLL.Service.Classes;
using GymManagement.BLL.Service.Interfaces;
using GymManagement.BLL.Services.Classes;
using GymManagement.BLL.Services.Interfaces;
using GymManagement.BLL.ViewModels.MemberViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.PL.Controllers
{
    [Authorize(Roles ="SuperAdmin")]
    public class MembersController : Controller
    {
        private readonly IMemberService memberService;
        private readonly IAttachementService attachementService;

        public MembersController(IMemberService memberService , IAttachementService attachementService)
        {
            this.memberService = memberService;
            this.attachementService = attachementService;
        }

        // Get BaseUrl/Member/Index
        // Index- List all Member
        public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
        {
            var members = await memberService.GetAllMembersAsync(ct, page);
            return View(members);
        }


        #region Show Member Data 
        [HttpGet]
        public async Task<IActionResult> Picture(int id, CancellationToken ct)
        {
            var photo = await memberService.GetMemberPhotoAsync(id, ct);
            if (string.IsNullOrEmpty(photo))
                return NotFound();


            var result = attachementService.GetFile(photo, "MembersPictures");
            if (result is null) return NotFound();

            return File(result.Value.Stream, result.Value.ContentType);
        }

        // Get BaseUrl/Members/MemberDetails/{id}
        // MemberDetails - show one member`s details
        [HttpGet]
        public async Task<IActionResult> MemberDetails(int id, CancellationToken ct)
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
        [HttpGet]
        public async Task<IActionResult> HealthRecordDetails(int id, CancellationToken ct)
        {
            // Get Health Record By MemberId
            var result = await memberService.GetMemberHealthRecordAsync(id, ct);
            if (result is null)
            {
                TempData["ErrorMessage"] = "Health record not found";
                return RedirectToAction(nameof(Index));
            }
            return View(result);

            //Check Is Health Record null => return Index With Error Message
            //Health Record is Not Null => return View(Data)
        }

        [HttpGet]
        public async Task<IActionResult> HealthRecordCreate(int id, CancellationToken ct)
        {
            if (!await TrySetMemberNameAsync(id, ct))
                return RedirectToAction(nameof(Index));

            return View(new HealthRecordViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> HealthRecordCreate(
            int id,
            HealthRecordViewModel model,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await TrySetMemberNameAsync(id, ct);
                return View(model);
            }

            var result = await memberService.SaveMemberHealthRecordAsync(id, model, ct);
            if (!result.succes)
            {
                if (!await TrySetMemberNameAsync(id, ct))
                    return RedirectToAction(nameof(Index));

                ModelState.AddModelError(string.Empty, result.error ?? "Failed to save health record.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Health record created successfully.";
            return RedirectToAction(nameof(HealthRecordDetails), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> HealthRecordEdit(int id, CancellationToken ct)
        {
            var record = await memberService.GetMemberHealthRecordAsync(id, ct);
            if (record is null)
            {
                TempData["ErrorMessage"] = "Health record not found.";
                return RedirectToAction(nameof(HealthRecordCreate), new { id });
            }

            if (!await TrySetMemberNameAsync(id, ct))
                return RedirectToAction(nameof(Index));

            return View(record);
        }

        [HttpPost]
        public async Task<IActionResult> HealthRecordEdit(
            int id,
            HealthRecordViewModel model,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await TrySetMemberNameAsync(id, ct);
                return View(model);
            }

            var result = await memberService.SaveMemberHealthRecordAsync(id, model, ct);
            if (!result.succes)
            {
                if (!await TrySetMemberNameAsync(id, ct))
                    return RedirectToAction(nameof(Index));

                ModelState.AddModelError(string.Empty, result.error ?? "Failed to save health record.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Health record updated successfully.";
            return RedirectToAction(nameof(HealthRecordDetails), new { id });
        }

        private async Task<bool> TrySetMemberNameAsync(int memberId, CancellationToken ct)
        {
            var member = await memberService.GetMemberDetailsByIdAsync(memberId, ct);
            if (member is null)
            {
                TempData["ErrorMessage"] = "Member not found.";
                return false;
            }

            ViewBag.MemberName = member.Name;
            return true;
        }
        #endregion

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
            if (result.succes)
                TempData["SuccessMessage"] = "Member Created Successfully";
            else
                TempData["ErrorMessage"] = result.error;

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
            if (result.succes)
            {
                TempData["SuccessMessage"] = "Member Updated Successfully";
                return RedirectToAction(nameof(Index));
            }
            else
                TempData["ErrorMessage"] = result.error;
            return View(model);

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
            } 
            return View();
        }

        // Post BaseUrl/Members/Delete {Member}
        // Delete - Submit form 
        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed([FromRoute] int id, CancellationToken ct)
        {
            var result = await memberService.RemoveMemberAsync(id, ct);
            TempData[result.succes ? "SuccessMessage" : "ErrorMessage"] =
                result.succes ? "Member deleted successfully." : result.error;
            return RedirectToAction(nameof(Index));
        }
        #endregion
     }
}