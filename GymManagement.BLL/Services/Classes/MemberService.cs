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
        private readonly IGenericRepository<Member> memberRepository;

        public MemberService(IGenericRepository<Member> memberRepository)
        {
            this.memberRepository = memberRepository;
        }

        public async Task<bool> CreateMemberAsync(CreateMemberViewModel model, CancellationToken ct)
        {

            // Check Email
            var emailExist = await memberRepository.AnyAsync(x => x.Email == model.Email, ct);
            // Check Phone
            var phoneExist = await memberRepository.AnyAsync(x => x.Phone == model.Phone, ct);
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
            var result = await memberRepository.AddAsync(member);
            return result > 0;
        }

        public async Task<IEnumerable<MemberViewModel>> GetAllMembersAsync(CancellationToken ct = default)
        {
            var members = await memberRepository.GetAllAsync(ct: ct);
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
    }
}