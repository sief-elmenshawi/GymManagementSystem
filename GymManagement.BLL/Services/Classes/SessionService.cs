using GymManagement.BLL.Services.Interfaces;
using GymManagement.BLL.ViewModels.SessionViewModels;
using GymManagement.DAL.Data.Models;
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

        public SessionService(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }
        public async Task<IEnumerable<SessionViewModel>?> GetAllSessionsAync(CancellationToken ct = default)
        {
            var sessionRepository = unitOfWork.SessionRepository;
            var sessions = await sessionRepository.GetAllSessionsWithTrainerAndCategoryAsync(ct);
            if (sessions == null || !sessions.Any()) return null;

            var mappedSession = sessions.Select(s => new SessionViewModel()
            {
                Id = s.Id,
                Capacity = s.Capacity,
                CategoryName = s.Category.CategoryName,
                TrainerName = s.Trainer.Name,
                Description = s.Description,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
            });
            
            foreach(var session in mappedSession)
            {
                session.AvailableSlots = session.Capacity - await sessionRepository.GetCountOfBookSlotsAsync(session.Id, ct);
            };
            return mappedSession;

        }
    }
}
