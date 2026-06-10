using AutoMapper;
using GymManagement.BLL.Common;
using GymManagement.BLL.Service.Interfaces;
using GymManagement.BLL.Services.Interfaces;
using GymManagement.BLL.ViewModels.MemberViewModels;
using GymManagement.DAL.Data.Models;
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

            // Check Email
            var emailExist = await unitOfWork.GetRepository<Member>().AnyAsync(x => x.Email == model.Email, ct);
            // Check Phone
            var phoneExist = await unitOfWork.GetRepository<Member>().AnyAsync(x => x.Phone == model.Phone, ct);
            // Email Or Phone Exist Return false
            if (emailExist || phoneExist) return Result.Validation("Email Or Phone Exist");
            // Else Return True Add Member

            var member = mapper.Map<Member>(model);
            
            var NewPhotoName = await attachement.UploadAsync(model.PhotoFile.OpenReadStream(),model.PhotoFile.FileName , "MemberPictures" , ct);

            if (string.IsNullOrEmpty(NewPhotoName)) return Result.Validation("Must Upload Photo");
            member.Photo = NewPhotoName;


            unitOfWork.GetRepository<Member>().Add(member);
            var result = await unitOfWork.SaveChangesAsync(ct);
            return result > 0 ? Result.Ok() : Result.Fail("Can not Create Member");
        }

        public async Task<IEnumerable<MemberViewModel>> GetAllMembersAsync(CancellationToken ct = default)
        {
            var members = await unitOfWork.GetRepository<Member>().GetAllAsync(ct: ct);
            if (!members.Any()) return [];

            var membersViewModel = mapper.Map<IEnumerable<Member>, IEnumerable<MemberViewModel>>(members);
            return membersViewModel;
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

            var activeMembership = await unitOfWork.GetRepository<Membership>().FirstOrDefaultAsync(x => x.Id == MemberId && x.EndDate>DateTime.Now);

            if (activeMembership is not null)
            {
                var activePlan = await unitOfWork.GetRepository<Plan>().GetByIdAsync(activeMembership.PlanId, ct);
                model.PlanName = activePlan?.Name;

                model.MembershipStartDate = activeMembership.CreatedAt.ToString();
                model.MembershipEndDate   = activeMembership.EndDate.ToString();
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
            if(record == null) return null;
            else
           
            return mapper.Map<HealthRecord, HealthRecordViewModel>(record);
        }

        public async Task<MemberToUpdateViewModel?> GetMemberToUpdatAsync(int MemberId, CancellationToken ct = default)
        {
            var member = await unitOfWork.GetRepository<Member>().GetByIdAsync(MemberId, ct);
            if (member == null) return null;
            else
                return mapper.Map<Member,MemberToUpdateViewModel>(member);
        }

        public async Task<Result> RemoveMemberAsync(int MemberId, CancellationToken ct = default)
        {
            var member = await unitOfWork.GetRepository<Member>().GetByIdAsync(MemberId, ct);
            if (member == null) return Result.Validation("Not Found Member");

            var hasFutureBooking = await unitOfWork.GetRepository<Booking>().AnyAsync(b=>b.MemberId == MemberId && b.Session.StartDate > DateTime.Now, ct);
            if (hasFutureBooking) return Result.Validation("Not Found Booking");

            //var result = await memberRepository.DeleteAsync(member , ct);
            unitOfWork.GetRepository<Member>().Delete(member);

            if (member.Photo is not null)
            {
                attachement.Delet(member.Photo, "MemberPictures");
            }

            var result = await unitOfWork.SaveChangesAsync(ct);
            return result > 0 ? Result.Ok() : Result.Fail("Can  not Remove Member ");
        }

        public async Task<Result> UpdateMemberDetailsAsync(int id, MemberToUpdateViewModel model, CancellationToken ct = default)
        {
            var member = await unitOfWork.GetRepository<Member>().GetByIdAsync(id, ct);
            if (member == null) return Result.Validation("Can Not Found Member");

            var existEmail = await unitOfWork.GetRepository<Member>().AnyAsync(x => x.Email == model.Email && x.Id != id);
            var existPhone = await unitOfWork.GetRepository<Member>().AnyAsync(x => x.Phone == model.Phone && x.Id != id);
            if (existEmail || existPhone) return Result.Validation("Email and Phone Is Exist");

            mapper.Map(model, member);

            member.UpdatedAt = DateTime.Now;

            unitOfWork.GetRepository<Member>().Update(member);
            var result = await unitOfWork.SaveChangesAsync(ct);

            return result > 0 ? Result.Ok() : Result.Fail("Can Not Update Member ");

        }
    }
}    