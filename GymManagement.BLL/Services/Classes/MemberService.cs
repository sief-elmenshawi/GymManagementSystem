using GymManagement.BLL.Service.Interfaces;
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

        public MemberService(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }

        public async Task<bool> CreateMemberAsync(CreateMemberViewModel model, CancellationToken ct)
        {

            // Check Email
            var emailExist = await unitOfWork.GetRepository<Member>().AnyAsync(x => x.Email == model.Email, ct);
            // Check Phone
            var phoneExist = await unitOfWork.GetRepository<Member>().AnyAsync(x => x.Phone == model.Phone, ct);
            // Email Or Phone Exist Return false
            if (emailExist || phoneExist) return false;
            // Else Return True Add Member
            var member = new Member()
            {
                Name = model.Name,
                Email = model.Email,
                Phone = model.Phone,
                Gender = model.Gender,
                DateOfBirth = model.DateOfBirth,
                Address = new Address()
                {
                    BuildingNumber = model.BuildingNumber,
                    City = model.City,
                    Street = model.Street,
                },
                HealthRecord = new HealthRecord()
                {
                    BloodType = model.HealthRecordViewModel.BloodType,
                    Weight = model.HealthRecordViewModel.Weight,
                    Height = model.HealthRecordViewModel.Height,
                    Note = model.HealthRecordViewModel.Note,
                }
            };
            //var result = await memberRepository.AddAsync(member);
            unitOfWork.GetRepository<Member>().Add(member);
            var result = await unitOfWork.SaveChangesAsync(ct);
            return result > 0;
        }

        public async Task<IEnumerable<MemberViewModel>> GetAllMembersAsync(CancellationToken ct = default)
        {
            var members = await unitOfWork.GetRepository<Member>().GetAllAsync(ct: ct);
            if (!members.Any()) return [];

            var membersViewModel = members.Select(m => new MemberViewModel()
            {
                Name = m.Name,
                Email = m.Email,
                Gender = m.Gender.ToString(),
                Phone = m.Phone,
                Photo = m.Photo,
                Id = m.Id,
            });
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

            var model = new MemberViewModel()
            {
                Name = member.Name,
                Phone = member.Phone,
                Email = member.Email,
                DateOfBirth = member.DateOfBirth.ToShortDateString(),
                Gender = member.Gender.ToString(),
                Address =$"{member.Address.BuildingNumber} - {member.Address.Street} - {member.Address.City}",
            };

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
            {
                return new HealthRecordViewModel()
                {
                    Height = record.Height,
                    Weight = record.Weight,
                    BloodType = record.BloodType,
                    Note = record.Note,
                };
            }
        }

        public async Task<MemberToUpdateViewModel?> GetMemberToUpdatAsync(int MemberId, CancellationToken ct = default)
        {
            var member = await unitOfWork.GetRepository<Member>().GetByIdAsync(MemberId, ct); 
            if(member == null) return null;
            else
            {
                return new MemberToUpdateViewModel()
                {
                    Name = member.Name,
                    Email = member.Email,
                    Phone = member.Phone,
                    Photo = member.Photo,
                    BuildingNumber = member.Address.BuildingNumber,
                    City = member.Address.City,
                    Street = member.Address.Street,
                };
            }
        }

        public async Task<bool> RemoveMemberAsync(int MemberId, CancellationToken ct = default)
        {
            var member = await unitOfWork.GetRepository<Member>().GetByIdAsync(MemberId, ct);
            if(member == null) return false;

            var hasFutureBooking = await unitOfWork.GetRepository<Booking>().AnyAsync(b=>b.MemberId == MemberId && b.Session.StartDate > DateTime.Now, ct);
            if (hasFutureBooking ) return false;

            //var result = await memberRepository.DeleteAsync(member , ct);
            unitOfWork.GetRepository<Member>().Delete(member);
            var result = await unitOfWork.SaveChangesAsync(ct);
            return result > 0;
        }

        public async Task<bool> UpdateMemberDetailsAsync(int id, MemberToUpdateViewModel model, CancellationToken ct = default)
        {
            var member = await unitOfWork.GetRepository<Member>().GetByIdAsync(id, ct);
            if( member == null) return false;

            var existEmail = await unitOfWork.GetRepository<Member>().AnyAsync(x => x.Email == model.Email && x.Id != id);
            var existPhone = await unitOfWork.GetRepository<Member>().AnyAsync(x => x.Phone == model.Phone && x.Id != id);
            if (existEmail || existPhone) return false;

            member.Email = model.Email;
            member.Phone = model.Phone;
            member.Address.BuildingNumber = model.BuildingNumber;
            member.Address.City = model.City;
            member.Address.Street = model.Street;
            member.UpdatedAt = DateTime.Now;

            //var result = await memberRepository.UpdateAsync(member, ct);
            unitOfWork.GetRepository<Member>().Update(member);
            var result = await unitOfWork.SaveChangesAsync(ct); 

            return result > 0;

        }
    }
}    