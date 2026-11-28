using AutoMapper;
using GymManagement.BLL.Common;
using GymManagement.BLL.Services.Interfaces;
using GymManagement.BLL.ViewModels.TrainerViewModels;
using GymManagement.DAL.Models;
using GymManagement.DAL.Repositories.Interfaces;
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
        private readonly IMapper mapper;
        private readonly TimeProvider _timeProvider;

        public TrainerService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            TimeProvider? timeProvider = null)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        public async Task<Result> CreateTrainerAsync(CreateTrainerViewModel model, CancellationToken ct = default)
        {
            if (await unitOfWork.GetRepository<Trainer>().AnyAsync(t => t.Email == model.Email, ct))
                return Result.Validation("Email is Exist"); // Email already exists
            if (await unitOfWork.GetRepository<Trainer>().AnyAsync(t => t.Phone == model.Phone, ct))
                return Result.Validation("Phone Is Exist"); // Phone number already exists

            var trainer = mapper.Map<Trainer>(model);
            trainer.HiringDate = _timeProvider.GetUtcNow().UtcDateTime;
            unitOfWork.GetRepository<Trainer>().Add(trainer);
            var result = await unitOfWork.SaveChangesAsync(ct);
            return result > 0 ? Result.Ok() : Result.Fail("Can Not Create This Trainer");
        }

        public async Task<PagedResult<TrainerViewModel>> GetAllTrainersAsync(
            CancellationToken ct = default, int page = 1, int pageSize = 12)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var repo = unitOfWork.GetRepository<Trainer>();
            var trainers = await repo.GetPagedAsync(page: page, pageSize: pageSize, ct: ct);
            var totalCount = await repo.CountAsync(ct: ct);

            return new PagedResult<TrainerViewModel>
            {
                Items = mapper.Map<List<TrainerViewModel>>(trainers),
                Paging = new PagingInfo { Page = page, PageSize = pageSize, TotalCount = totalCount },
            };
        }
        public async Task<TrainerViewModel?> GetTrainerDetailsAsync(int TrainerId, CancellationToken ct = default)
        {
            var trainer =await unitOfWork.GetRepository<Trainer>().GetByIdAsync(TrainerId, ct);
            if (trainer == null)
                return null;
            return mapper.Map<TrainerViewModel?>(trainer);
        }

        public async Task<TrainerToUpdateViewModel?> GetTrainerToUpdateAsync(int TrainerId, CancellationToken ct = default)
        {
            var trainer = await unitOfWork.GetRepository<Trainer>().GetByIdAsync(TrainerId, ct);
            if (trainer == null) 
                return null;
            else
         
            return mapper.Map<TrainerToUpdateViewModel>(trainer);
        }

        public async Task<Result> RemoveTrainerAsync(int TrainerId, CancellationToken ct = default)
        {
            var trainer = await unitOfWork.GetRepository<Trainer>().GetByIdAsync(TrainerId, ct);
            if (trainer is null) return Result.NotFound("Trainer Not Found");

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var hasFutureSessions = await unitOfWork.GetRepository<Session>().AnyAsync(
                s => s.TrainerId == TrainerId && s.StartDate > now,
                ct);
            if (hasFutureSessions) return Result.Validation("Session Not Allow");

            unitOfWork.GetRepository<Trainer>().Delete(trainer);
            var result = await unitOfWork.SaveChangesAsync(ct);
            return result > 0 ? Result.Ok() : Result.Fail("Can Not Remove This Session");
        }

        public async Task<Result> UpdateTrainerDetailsAsync(int TrainerId, TrainerToUpdateViewModel model, CancellationToken ct = default)
        {
            var trainer = await unitOfWork.GetRepository<Trainer>().GetByIdAsync(TrainerId, ct);
            if (trainer is null)
                return Result.NotFound("Trainer Not Found"); // Trainer not found

            if (await unitOfWork.GetRepository<Trainer>().AnyAsync(t => t.Email == model.Email && t.Id != TrainerId, ct))
                return Result.Validation("Email is Exist"); // Email already exists for another trainer
            if (await unitOfWork.GetRepository<Trainer>().AnyAsync(t => t.Phone == model.Phone && t.Id != TrainerId, ct))
                return Result.Validation("Phone Is Exist"); // Phone number already exists for another trainer

            mapper.Map(model, trainer);


            trainer.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;


            unitOfWork.GetRepository<Trainer>().Update(trainer);
            var result = await unitOfWork.SaveChangesAsync(ct);
            return result > 0 ? Result.Ok() : Result.Fail("Can Not Update Trainer");
        }
    }
}
