using AutoMapper;
using GymManagement.BLL.Common;
using GymManagement.BLL.Services.Interfaces;
using GymManagement.BLL.ViewModels.SessionViewModels;
using GymManagement.DAL.Models;
using GymManagement.DAL.Models.Enums;
using GymManagement.DAL.Repositorities.Classes;
using GymManagement.DAL.Repositorities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagement.BLL.Services.Classes
{
    public class SessionService : ISessionService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;

        public SessionService(IUnitOfWork unitOfWork,IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }

        public async Task<Result> CreateSessionAync(CreateSessionViewModel model, CancellationToken ct = default)
        {
            if (model.EndDate <= model.StartDate) return Result.Validation("End Date Must be After StaetDate");

            if (model.StartDate <= DateTime.Now) return Result.Validation("Start Date Must Be In The Future");

            if (model.Capacity < 1 || model.Capacity > 25) return Result.Validation("Capacity Must be Between 1 And 25");

            var trainer = await unitOfWork.GetRepository<Trainer>().GetByIdAsync(model.TrainerId,ct);
            if (trainer is null) return Result.NotFound("Trainer Not Found");

            var category = await unitOfWork.GetRepository<Category>().GetByIdAsync(model.CategoryId,ct);
            if (category is null) return Result.NotFound("Category Not Found");

            var isValid = Enum.TryParse<Specialty>(category.CategoryName, true, out var CategorySpecialty);
            if (!isValid || trainer.Specialty != CategorySpecialty) return Result.Validation("Can Not Create This Session To This Trainer");

            var session = mapper.Map<CreateSessionViewModel, Session>(model);

            unitOfWork.GetRepository<Session>().Add(session);
            var result = await unitOfWork.SaveChangesAsync();
            return result > 0 ? Result.Ok() : Result.Fail("Failed To Creat Seesion");

        }

        public async Task<IEnumerable<SessionViewModel>?> GetAllSessionsAync(CancellationToken ct = default)
        {
            var sessions = await unitOfWork.SessionRepository.GetAllSessionsWithTrainerAndCategoryAsync(ct: ct);


            if (sessions?.Any() != true) return null;

            sessions = sessions.OrderByDescending(X => X.StartDate);
            var MappedSessions = mapper.Map<IEnumerable<SessionViewModel>>(sessions);

            foreach (var session in MappedSessions)
            {
                session.AvailableSlots = session.Capacity - await unitOfWork.SessionRepository.GetCountOfBookedSlotsAsync(session.Id, ct);
            }
            return MappedSessions;

        }

        public async Task<IEnumerable<CategorySelectViewModel>> GetCategoriesForDropDownAsync(CancellationToken ct = default)
        {
            var result = await unitOfWork.GetRepository<Category>().GetAllAsync(ct:ct);
            return mapper.Map<IEnumerable<Category>, IEnumerable<CategorySelectViewModel>>(result);
        }

        public async Task<Result<SessionViewModel>> GetSessionByIdAysnc(int sessionId, CancellationToken ct)
        {
            var session = await unitOfWork.SessionRepository.GetSessionByIdWithTrainerAndCategoryAsync(sessionId, ct);
            if (session is null)
                return Result<SessionViewModel>.NotFound("Session Not Found");
            else
            {
                var mappedSession = mapper.Map<SessionViewModel>(session);
                mappedSession.AvailableSlots = mappedSession.Capacity - await unitOfWork.SessionRepository.GetCountOfBookedSlotsAsync(sessionId, ct);

                return Result<SessionViewModel>.Ok(mappedSession);

            }
        }

        public async Task<Result<UpdateSessionViewModel>> GetSessionToUpdateAsync(int sessionId, CancellationToken ct = default)
        {
            var session = await unitOfWork.SessionRepository.GetByIdAsync(sessionId, ct);
            if (session is null) return Result<UpdateSessionViewModel>.NotFound("Session Not Found");

            if (session.StartDate <= DateTime.Now)
                return Result<UpdateSessionViewModel>.Fail("Can not Update Session that has Started");

            var bookingCount = await unitOfWork.SessionRepository.GetCountOfBookedSlotsAsync(sessionId, ct);
            if (bookingCount > 0)
                return Result<UpdateSessionViewModel>.Fail("Can not Update Session that has Booking");

            var mappedSession = mapper.Map<Session,UpdateSessionViewModel>(session);
            return Result<UpdateSessionViewModel>.Ok(mappedSession);
        }

        public async Task<IEnumerable<TrainerSelectViewModel>> GetTrainersForDropDownAsync(CancellationToken ct = default)
        {
            var result = await unitOfWork.GetRepository<Trainer>().GetAllAsync(ct: ct);
            return mapper.Map<IEnumerable<Trainer> , IEnumerable<TrainerSelectViewModel>>(result);
        }

        public async Task<Result> RemoveSessionAysnc(int sessionId, CancellationToken ct = default)
        {
            var session = await unitOfWork.SessionRepository.GetByIdAsync(sessionId, ct);
            if (session is null) return Result.NotFound("Session Not Found");
            if (session.EndDate >= DateTime.Now)
                return Result.Fail("Can Not Delete Session That Not Ended Yet");

            var BookingCount = await unitOfWork.SessionRepository.GetCountOfBookedSlotsAsync(sessionId, ct);
            if (BookingCount > 0) return Result.Fail("Can Not Delete Session That Has Booking");

            unitOfWork.SessionRepository.Delete(session);
            var result =await unitOfWork.SaveChangesAsync(ct);
            return result > 0 ? Result.Ok() : Result.Fail("Failed To delete Session");
        }

        public async Task<Result> UpdateSessionAsync(int id, UpdateSessionViewModel model, CancellationToken ct = default)
        {
            var session = await unitOfWork.SessionRepository.GetByIdAsync(id, ct);
            if(session == null)
                 if (session is null) return Result.NotFound("Session Not Found");

            if (session.StartDate <= DateTime.Now)
                return Result.Fail("Can Not Edit Session Thas Started");

            if (model.EndDate <= model.StartDate)
                return Result.Validation("End Date Must Be After StartDate");

            var BookedCount = await unitOfWork.SessionRepository.GetCountOfBookedSlotsAsync(id);
            if (BookedCount > 0)
                return Result.Fail("Can Not Update Session That Has Booking");

            if (model.StartDate <= DateTime.Now)
                return Result.Fail("Start Date Must Be in Future");

            var trainer = await unitOfWork.GetRepository<Trainer>().GetByIdAsync(model.TrainerId, ct);
            if (trainer is null) return Result.NotFound("Trainer Not Found");

            var category = await unitOfWork.GetRepository<Category>().GetByIdAsync(session.CategoryId, ct);

            var isValid = Enum.TryParse<Specialty>(category?.CategoryName, true, out var CategorySpecialty);
            if (!isValid || trainer.Specialty != CategorySpecialty) return Result.Validation("Can Not Create This Session To This Trainer");

            mapper.Map(model, session);
            session.UpdatedAt = DateTime.Now;
            unitOfWork.SessionRepository.Update(session);

            var result = await unitOfWork.SaveChangesAsync(ct);
            return result > 0 ? Result.Ok() : Result.Fail("Fail To Update Session");

        }
    }
}
