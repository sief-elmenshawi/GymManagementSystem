using AutoMapper;
using GymManagement.BLL.Common;
using GymManagement.BLL.Service.Interfaces;
using GymManagement.BLL.Services.Classes;
using GymManagement.BLL.Services.Interfaces;
using GymManagement.BLL.ViewModels.MemberViewModels;
using GymManagement.DAL.Models;
using GymManagement.DAL.Repositorities.Classes;
using GymManagement.DAL.Repositorities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagement.BLL.Service.Classes
{
    public class MemberService : IMemberService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        private readonly IAttachementService attachement;

        public MemberService(IUnitOfWork unitOfWork , IMapper mapper , IAttachementService attachement)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
            this.attachement = attachement;
        }

        public async Task<Result> CreateMemberAsync(CreateMemberViewModel model, CancellationToken ct)
        {
            var repo = unitOfWork.GetRepository<Member>();

            if (await repo.AnyAsync(m => m.Email == model.Email, ct))
                return Result.Fail("A member with this email already exists.");

            if (await repo.AnyAsync(m => m.Phone == model.Phone, ct))
                return Result.Fail("A member with this phone number already exists.");

            var photo = await attachement.UploadAsync(model.PhotoFile.OpenReadStream(), model.PhotoFile.FileName, "MembersPictures", ct);
            if (string.IsNullOrEmpty(photo))
                return Result.Validation("Profile photo upload failed (check file type and size).");

            var member = mapper.Map<Member>(model);
            member.Photo=photo;


            repo.Add(member);
            var result = await unitOfWork.SaveChangesAsync(ct);

            if (result == 0)
            {
                if (!string.IsNullOrEmpty(member.Photo))        
                    attachement.Delete(member.Photo, "MembersPictures");

                return Result.Fail("Failed To Create Member");
            }
            else

                return Result.Ok();
         }

        public async Task<IEnumerable<MemberViewModel>> GetAllMembersAsync(CancellationToken ct = default)
        {
            var members = await unitOfWork.GetRepository<Member>().GetAllAsync(ct: ct);
            return mapper.Map<List<MemberViewModel>>(members);

            #region MyRegion
            //List<MemberViewModel> membersViewModel= new List<MemberViewModel>();
            //foreach (var member in members)
            //{
            //    var memberViewModel = new MemberViewModel()
            //    {
            //        Name = member.Name,
            //        Email = member.Email,
            //        Gender = member.Gender.ToString(),
            //        Phone = member.Phone,
            //        Photo = member.Photo,
            //        Id = member.Id,
            //    };
            //   membersViewModel.Add(memberViewModel);
            //}
            //return membersViewModel; 
            #endregion
        }

        public async Task<MemberViewModel?> GetMemberDetailsByIdAsync(int MemberId, CancellationToken ct = default)
        {
            var member = await unitOfWork.GetRepository<Member>().GetByIdAsync(MemberId, ct);
            if (member == null) return null;    

            var model = mapper.Map<Member , MemberViewModel>(member);

            var activeMembership = await unitOfWork.GetRepository<Membership>().FirstOrDefaultAsync(x => x.MemberId == MemberId && x.EndDate>=DateTime.Now);

            if (activeMembership is not null)
            {
                var activePlan = await unitOfWork.GetRepository<Plan>().GetByIdAsync(activeMembership.PlanId, ct);

                model.PlanName = activePlan?.Name;
                model.MembershipStartDate = activeMembership.CreatedAt.ToShortDateString();
                model.MembershipEndDate   = activeMembership.EndDate.ToShortDateString();
            }
            return model;

            #region Load All MemberShip in Memory to get One Its Not Performance

            //var memberships = await membershipRepository.GetAllAsync();
            //var activeMembership = memberships.FirstOrDefault(m => m.MemberId == MemberId && m.IsActive); 
            #endregion
        }

        public async Task<HealthRecordViewModel?> GetMemberHealthRecordAsync(int MemberId, CancellationToken ct = default)
        {
            var record = await unitOfWork.GetRepository<HealthRecord>().FirstOrDefaultAsync(x => x.MemberId == MemberId, ct:ct);
            return record is null ? null : mapper.Map<HealthRecordViewModel>(record);

        }

        public async Task<MemberToUpdateViewModel?> GetMemberToUpdatAsync(int MemberId, CancellationToken ct = default)
        {
            var member = await unitOfWork.GetRepository<Member>().GetByIdAsync(MemberId, ct);
            return member is null ? null :  mapper.Map<MemberToUpdateViewModel>(member);

        }

        public async Task<Result> RemoveMemberAsync(int MemberId, CancellationToken ct = default)
        {
            var memberRepo = unitOfWork.GetRepository<Member>();
            var member = await memberRepo.GetByIdAsync(MemberId, ct);
            if (member == null) return Result.NotFound("Member Not Found ");

            var hasFutureBooking = await unitOfWork.BookingRepository.AnyAsync(b=>b.MemberId == MemberId && b.Session.StartDate > DateTime.Now);

            if (hasFutureBooking) 
                return Result.Fail("Cannot delete a member with upcoming sessions");

            memberRepo.Delete(member);
            var result = await unitOfWork.SaveChangesAsync(ct);

            if (result > 0)
            {
                if (!string.IsNullOrEmpty(member.Photo))
                    attachement.Delete(member.Photo, "MembersPictures");

                return Result.Ok();
            }
            return Result.Fail("Failed To Delete Member");
        }

        public async Task<Result> UpdateMemberDetailsAsync(int id, MemberToUpdateViewModel model, CancellationToken ct = default)
        {
            var repo = unitOfWork.GetRepository<Member>();

            var member = await repo.GetByIdAsync(id, ct);
            if (member == null) return Result.Validation("Member Not Found ");

            if (await repo.AnyAsync(m => m.Email == model.Email && m.Id != id, ct))
                return Result.Fail("Another member is already using this email.");
            if (await repo.AnyAsync(m => m.Phone == model.Phone && m.Id != id, ct))
                return Result.Fail("Another member is already using this phone number.");

            mapper.Map(model, member);
            member.UpdatedAt = DateTime.Now;
            repo.Update(member);
            var result = await unitOfWork.SaveChangesAsync(ct);

            return result > 0 ? Result.Ok() : Result.Fail("Failed To update Membe");

        }
    }
}    