using GymManagement.BLL.Services.Interfaces;
using GymManagement.BLL.ViewModels.AnalyticsViewModels;
using GymManagement.DAL.Models;
using GymManagement.DAL.Repositories.Classes;
using GymManagement.DAL.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagement.BLL.Services.Classes
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly TimeProvider _timeProvider;

        public AnalyticsService(IUnitOfWork unitOfWork, TimeProvider? timeProvider = null)
        {
            this.unitOfWork = unitOfWork;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }
        public async Task<AnalyticsViewModel> GetAnalyticsDataAsync(CancellationToken ct)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var sessionRepo = unitOfWork.GetRepository<Session>();
            var upcomingSessions = await sessionRepo.CountAsync(s => s.StartDate > now, ct);
            var ongoingSessions = await sessionRepo.CountAsync(x => x.StartDate <= now && x.EndDate >= now, ct);
            var completedSessions = await sessionRepo.CountAsync(x => x.EndDate < now, ct);
            var totalMembers = await unitOfWork.GetRepository<Member>().CountAsync(ct: ct);
            var totalTrainers = await unitOfWork.GetRepository<Trainer>().CountAsync(ct: ct);
            var activeMembers = await unitOfWork.GetRepository<Membership>().CountAsync(m => m.EndDate > now, ct);

            return new AnalyticsViewModel
            {
                TotalMembers = totalMembers,
                TotalTrainers = totalTrainers,
                ActiveMembers = activeMembers,
                UpcomingSessions = upcomingSessions,
                OngoingSessions = ongoingSessions,
                CompletedSessions = completedSessions

            };

        }
    }
}
