using GymManagement.BLL.Services.Interfaces;
using GymManagement.BLL.ViewModels.TrainerViewModels;
using GymManagement.DAL.Data.Models;
using GymManagement.DAL.Repositorities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagement.BLL.Services.Classes
{
    public class TrainerService : ITrainerService
    {
        private readonly IUnitOfWork unitOfWork;

        public TrainerService(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }

        public async Task<bool> CreateTrainerAsync(CreateTrainerViewModel model, CancellationToken ct = default)
        {
            if(await unitOfWork.GetRepository<Trainer>().AnyAsync(t => t.Email == model.Email, ct))
                return false; // Email already exists
            if (await unitOfWork.GetRepository<Trainer>().AnyAsync(t => t.Phone == model.Phone, ct))
                return false; // Phone number already exists

            var trainer = new Trainer()
            {
                Name = model.Name,
                Email = model.Email,
                Phone = model.Phone,
                DateOfBirth = model.DateOfBirth,
                Specialty = model.Specialties,
                Gender = model.Gender,
                Address = new Address()
                {
                    BuildingNumber = model.BuildingNumber,
                    Street = model.Street,
                    City = model.City
                },
            };
            unitOfWork.GetRepository<Trainer>().Add(trainer);
            var result = await unitOfWork.SaveChangesAsync(ct);
            return result > 0;
        }

        public async Task<IEnumerable<TrainerViewModel>> GetAllTrainersAsync(CancellationToken ct = default)
        {
            var trainers = await unitOfWork.GetRepository<Trainer>().GetAllAsync(ct: ct);
            return trainers.Select(t => new TrainerViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Email = t.Email,
                Phone = t.Phone,
                Specialties = t.Specialty.ToString()
            });
        }
        public async Task<TrainerViewModel?> GetTrainerDetailsAsync(int TrainerId, CancellationToken ct = default)
        {
            var trainer =await unitOfWork.GetRepository<Trainer>().GetByIdAsync(TrainerId, ct);
            if (trainer == null)
                return null;
            return new TrainerViewModel()
            {
                Name = trainer.Name,
                Email = trainer.Email,
                Phone = trainer.Phone,
                Specialties = trainer.Specialty.ToString(),
                DateOfBirth = trainer.DateOfBirth.ToString(),
                Address = $"{trainer.Address.BuildingNumber}, {trainer.Address.Street}, {trainer.Address.City}",
            }; 
        }

        public async Task<TrainerToUpdateViewModel?> GetTrainerToUpdateAsync(int TrainerId, CancellationToken ct = default)
        {
            var trainer = await unitOfWork.GetRepository<Trainer>().GetByIdAsync(TrainerId, ct);
            if (trainer == null) 
                return null;
            else
                return new TrainerToUpdateViewModel()
                {
                    Name = trainer.Name,
                    Email = trainer.Email,
                    Phone = trainer.Phone,
                    Specialties = trainer.Specialty,
                    BuildingNumber = trainer.Address.BuildingNumber,
                    Street = trainer.Address.Street,
                    City = trainer.Address.City

                };
        }

        public async Task<bool> RemoveTrainerAsync(int TrainerId, CancellationToken ct = default)
        {
            var trainer = await unitOfWork.GetRepository<Trainer>().GetByIdAsync(TrainerId, ct);
            if (trainer is null) return false;

            var hasFutureSessions = await unitOfWork.GetRepository<Session>().AnyAsync(s => s.TrainerId == TrainerId && s.StartDate > DateTime.Now, ct);
            if (hasFutureSessions) return false;

            unitOfWork.GetRepository<Trainer>().Delete(trainer);
            var result = await unitOfWork.SaveChangesAsync(ct);
            return result > 0;
        }

        public async Task<bool> UpdateTrainerDetailsAsync(int TrainerId, TrainerToUpdateViewModel model, CancellationToken ct = default)
        {
            var trainer = await unitOfWork.GetRepository<Trainer>().GetByIdAsync(TrainerId, ct);
            if (trainer is null)
                return false; // Trainer not found

            if(await unitOfWork.GetRepository<Trainer>().AnyAsync(t => t.Email == model.Email && t.Id != TrainerId, ct))
                return false; // Email already exists for another trainer
            if(await unitOfWork.GetRepository<Trainer>().AnyAsync(t => t.Phone == model.Phone && t.Id != TrainerId, ct))
                return false; // Phone number already exists for another trainer

            trainer.Email = model.Email;
            trainer.Phone = model.Phone;
            trainer.Specialty = model.Specialties;
            trainer.Address.BuildingNumber = model.BuildingNumber;
            trainer.Address.Street = model.Street;
            trainer.Address.City = model.City;
            trainer.UpdatedAt = DateTime.Now;

            //var result = await trainerRepository.UpdateAsync(trainer, ct);
            unitOfWork.GetRepository<Trainer>().Update(trainer);
            var result = await unitOfWork.SaveChangesAsync(ct);
            return result > 0;
        }
    }
}
