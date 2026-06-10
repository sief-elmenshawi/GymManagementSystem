using GymManagement.BLL.Services.Interfaces;
using GymManagement.BLL.ViewModels.AnalyticsViewModels;
using GymManagement.DAL.Data.Models;
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
            var sessions = await unitOfWork.GetRepository<Session>().GetAllAsync(ct:ct);

            var totalMembers = await unitOfWork.GetRepository<Member>().CountAsync(ct:ct);

            var totalTrainers = await unitOfWork.GetRepository<Trainer>().CountAsync(ct:ct);

            var ActiveMembers = await unitOfWork.GetRepository<Membership>().CountAsync(m=>m.EndDate > DateTime.Now);

            return new AnalyticsViewModel
            {
                TotalMembers = totalMembers,
                TotalTrainers = totalTrainers,
                ActiveMembers = ActiveMembers,
                UpcomingSessions = sessions.Count(x=>x.StartDate > DateTime.Now),
                OngoingSessions = sessions.Count(x=>x.StartDate <= DateTime.Now && x.EndDate >=DateTime.Now),
                CompletedSessions = sessions.Count(x=>x.EndDate  <= DateTime.Now)

            };

        }
    }
}
