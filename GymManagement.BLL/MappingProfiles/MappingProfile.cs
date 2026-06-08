using AutoMapper;
using GymManagement.BLL.ViewModels.MemberViewModels;
using GymManagement.BLL.ViewModels.PlanViewModels;
using GymManagement.BLL.ViewModels.SessionViewModels;
using GymManagement.BLL.ViewModels.TrainerViewModels;
using GymManagement.DAL.Data.Models;
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

        }

        private void PlanAutoMapper()
        {
            CreateMap<Plan, PlanViewModel>();

            CreateMap<UpdatePlanViewModel, Plan>()
                .ForMember(dest=>dest.Name ,opt=>opt.MapFrom(src=>src.PlanName))
                .ReverseMap();               
        }

        private void MemberAutoMapper()
        {
            CreateMap<Member, MemberViewModel>()
             .ForMember(dest => dest.Address, opt => opt.MapFrom(src => $"{src.Address.BuildingNumber} - {src.Address.Street} - {src.Address.City}"))
             .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth.ToShortDateString()));

            CreateMap<HealthRecord, HealthRecordViewModel>().ReverseMap();

            CreateMap<Member, MemberToUpdateViewModel>()
              .ForMember(dest => dest.BuildingNumber, opt => opt.MapFrom(src => src.Address.BuildingNumber))
              .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Address.Street))
              .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Address.City));

            CreateMap<MemberToUpdateViewModel, Member>()
                .ForMember(dest => dest.Name, opt => opt.Ignore())
                .ForMember(dest => dest.Photo, opt => opt.Ignore())
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
               .ForMember(dest => dest.HealthRecord, opt => opt.MapFrom(src => src.HealthRecordViewModel));
        }

        private void TrainerAutoMapper()
        {
            CreateMap<Trainer, TrainerViewModel>()
                .ForMember(dest => dest.Specialties, opt => opt.MapFrom(src => src.Specialty))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => $"{src.Address.BuildingNumber} - {src.Address.Street} - {src.Address.City}"))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth.ToShortDateString()));


            CreateMap<Trainer, TrainerToUpdateViewModel>()
                .ForMember(dest => dest.Specialties, opt => opt.MapFrom(src => src.Specialty))
                .ForMember(dest => dest.BuildingNumber, opt => opt.MapFrom(src => src.Address.BuildingNumber))
                .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Address.Street))
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Address.City));

            CreateMap<TrainerToUpdateViewModel, Trainer>()
                .ForMember(dest => dest.Specialty, opt => opt.MapFrom(src => src.Specialties))
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

                }));
                
        }

        private void SessionAutoMapper()
        {
            CreateMap<Session, SessionViewModel>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.CategoryName))
                .ForMember(dest => dest.TrainerName, opt => opt.MapFrom(src => src.Trainer.Name)).ReverseMap();

            CreateMap<CreateSessionViewModel, Session>();

            CreateMap<Trainer, TrainerSelectViewModel>();
            CreateMap<Category,CategorySelectViewModel>();

            CreateMap<Session , UpdateSessionViewModel>().ReverseMap();
        }

    }
}
