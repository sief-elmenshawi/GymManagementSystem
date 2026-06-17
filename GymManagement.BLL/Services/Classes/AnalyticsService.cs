using GymManagement.BLL.Services.Interfaces;
using GymManagement.BLL.ViewModels.AnalyticsViewModels;
using GymManagement.DAL.Models;
using GymManagement.DAL.Repositorities.Classes;
using GymManagement.DAL.Repositorities.Interfaces;
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

        public AnalyticsService(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }
        public async Task<AnalyticsViewModel> GetAnalyticsDataAysnc(CancellationToken ct)
        {
            var now = DateTime.Now;
            var upcomingSessions = await unitOfWork.GetRepository<Session>().CountAsync(s => s.StartDate > now);
            var ongoingSessions = await unitOfWork.GetRepository<Session>().CountAsync(X => X.StartDate <= now && X.EndDate >= now);
            var completedSessions = await unitOfWork.GetRepository<Session>().CountAsync(X => X.EndDate < now);
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
