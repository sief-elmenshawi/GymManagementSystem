using AutoMapper;
using GymManagement.BLL.ViewModels.MembershipViewModels;
using GymManagement.BLL.ViewModels.MemberViewModels;
using GymManagement.BLL.ViewModels.PlanViewModels;
using GymManagement.BLL.ViewModels.SessionViewModels;
using GymManagement.BLL.ViewModels.TrainerViewModels;
using GymManagement.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagement.BLL.MappingProfile
{
    public class MappingProfile : Profile
    {

        public MappingProfile()
        {
            MemberAutoMapper();
            PlanAutoMapper();
            TrainerAutoMapper();
            SessionAutoMapper();
            MembershipAutoMapper();

        }

        private void PlanAutoMapper()
        {
            CreateMap<Plan, PlanViewModel>();

            CreateMap<UpdatePlanViewModel, Plan>()
                .ForMember(dest=>dest.Name ,opt=>opt.MapFrom(src=>src.PlanName))
                .ForMember(dest=>dest.IsActive, opt=>opt.Ignore())
                .ForMember(dest=>dest.Memberships, opt=>opt.Ignore())
                .ForMember(dest=>dest.Id, opt=>opt.Ignore())
                .ForMember(dest=>dest.CreatedAt, opt=>opt.Ignore())
                .ForMember(dest=>dest.UpdatedAt, opt=>opt.Ignore())
                .ReverseMap();               
        }

        private void MemberAutoMapper()
        {
            CreateMap<Member, MemberViewModel>()
             .ForMember(dest => dest.Address, opt => opt.MapFrom(src => $"{src.Address.BuildingNumber} - {src.Address.Street} - {src.Address.City}"))
             .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth.ToShortDateString()))
             .ForMember(dest => dest.PlanName, opt => opt.Ignore())
             .ForMember(dest => dest.MembershipStartDate, opt => opt.Ignore())
             .ForMember(dest => dest.MembershipEndDate, opt => opt.Ignore());

            CreateMap<HealthRecord, HealthRecordViewModel>().ReverseMap();

            CreateMap<Member, MemberToUpdateViewModel>()
              .ForMember(dest => dest.BuildingNumber, opt => opt.MapFrom(src => src.Address.BuildingNumber))
              .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Address.Street))
              .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Address.City));

            CreateMap<MemberToUpdateViewModel, Member>()
                .ForMember(dest => dest.Name, opt => opt.Ignore())
                .ForMember(dest => dest.Photo, opt => opt.Ignore())
                .ForMember(dest => dest.HealthRecord, opt => opt.Ignore())
                .ForMember(dest => dest.Memberships, opt => opt.Ignore())
                .ForMember(dest => dest.Bookings, opt => opt.Ignore())
                .ForMember(dest => dest.DateOfBirth, opt => opt.Ignore())
                .ForMember(dest => dest.Gender, opt => opt.Ignore())
                .ForMember(dest => dest.Address, opt => opt.Ignore())
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .AfterMap((src, dest) =>
                {
                    dest.Address.BuildingNumber = src.BuildingNumber;
                    dest.Address.Street = src.Street;
                    dest.Address.City = src.City;
                });

            CreateMap<CreateMemberViewModel, Member>()
               .ForMember(dest => dest.Address, opt => opt.MapFrom(src => new Address
               {
                   BuildingNumber = src.BuildingNumber,
                   Street = src.Street,
                   City = src.City,
               }))
               .ForMember(dest => dest.HealthRecord, opt => opt.MapFrom(src => src.HealthRecordViewModel))
               .ForMember(dest => dest.Photo, opt => opt.Ignore())
               .ForMember(dest => dest.Memberships, opt => opt.Ignore())
               .ForMember(dest => dest.Bookings, opt => opt.Ignore())
               .ForMember(dest => dest.Id, opt => opt.Ignore())
               .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
               .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
        }

        private void TrainerAutoMapper()
        {
            CreateMap<Trainer, TrainerViewModel>()
                .ForMember(dest => dest.Specialties, opt => opt.MapFrom(src => src.Specialty))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => $"{src.Address.BuildingNumber} - {src.Address.Street} - {src.Address.City}"))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth.ToShortDateString()))
                .ForMember(dest => dest.Photo, opt => opt.Ignore());


            CreateMap<Trainer, TrainerToUpdateViewModel>()
                .ForMember(dest => dest.Specialties, opt => opt.MapFrom(src => src.Specialty))
                .ForMember(dest => dest.BuildingNumber, opt => opt.MapFrom(src => src.Address.BuildingNumber))
                .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Address.Street))
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Address.City));

            CreateMap<TrainerToUpdateViewModel, Trainer>()
                .ForMember(dest => dest.Specialty, opt => opt.MapFrom(src => src.Specialties))
                .ForMember(dest => dest.HiringDate, opt => opt.Ignore())
                .ForMember(dest => dest.Sessions, opt => opt.Ignore())
                .ForMember(dest => dest.DateOfBirth, opt => opt.Ignore())
                .ForMember(dest => dest.Gender, opt => opt.Ignore())
                .ForMember(dest => dest.Address, opt => opt.Ignore())
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .AfterMap((src, dest) =>
                {
                    dest.Address.BuildingNumber = src.BuildingNumber;
                    dest.Address.Street = src.Street;
                    dest.Address.City = src.City;
                });

            CreateMap<CreateTrainerViewModel, Trainer>()
                .ForMember(dest => dest.Specialty, opt => opt.MapFrom(src => src.Specialties))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => new Address
                {
                    BuildingNumber = src.BuildingNumber,
                    Street = src.Street,
                    City = src.City,

                }))
                .ForMember(dest => dest.HiringDate, opt => opt.Ignore())
                .ForMember(dest => dest.Sessions, opt => opt.Ignore())
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
                
        }

        private void SessionAutoMapper()
        {
            CreateMap<Session, SessionViewModel>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.CategoryName))
                .ForMember(dest => dest.TrainerName, opt => opt.MapFrom(src => src.Trainer.Name))
                .ForMember(dest => dest.AvailableSlots, opt => opt.Ignore())
                .ReverseMap();

            CreateMap<CreateSessionViewModel, Session>()
                .ForMember(dest => dest.Trainer, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.Bookings, opt => opt.Ignore())
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            CreateMap<Trainer, TrainerSelectViewModel>();
            CreateMap<Category,CategorySelectViewModel>();

            CreateMap<Session , UpdateSessionViewModel>().ReverseMap();
        }

        private void MembershipAutoMapper() 
        {
            CreateMap<Membership, MemberShipForMemberViewModel>()
                     .ForMember(dist => dist.MemberName, Option => Option.MapFrom(Src => Src.Member.Name))
                     .ForMember(dist => dist.PlanName, Option => Option.MapFrom(Src => Src.Plan.Name))
                     .ForMember(dist => dist.StartDate, Option => Option.MapFrom(X => X.CreatedAt));

            CreateMap<Membership, MemberShipViewModel>()
                     .ForMember(dist => dist.MemberName, Option => Option.MapFrom(Src => Src.Member.Name))
                     .ForMember(dist => dist.PlanName, Option => Option.MapFrom(Src => Src.Plan.Name))
                                          .ForMember(dist => dist.StartDate, Option => Option.MapFrom(X => X.CreatedAt));

            CreateMap<CreateMemberShipViewModel, Membership>()
                .ForMember(dest => dest.Member, opt => opt.Ignore())
                .ForMember(dest => dest.Plan, opt => opt.Ignore())
                .ForMember(dest => dest.EndDate, opt => opt.Ignore())
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
            CreateMap<Member, MemberSelectListViewModel>();
            CreateMap<Plan, PlanSelectListViewModel>();
        }

    }
}
