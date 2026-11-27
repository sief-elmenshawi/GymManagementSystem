using GymManagement.DAL.Data.DbContexts;
using GymManagement.DAL.Models;
using GymManagement.DAL.Repositorities.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace GymManagement.DAL.Repositorities.Classes
{
    public class SessionRepository : GenericRepository<Session>, ISessionRepository
    {
        private readonly GymDbContext dbContext;

        public SessionRepository(GymDbContext dbContext):base(dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<IEnumerable<Session>> GetAllSessionsWithTrainerAndCategoryAsync(Expression<Func<Session, bool>>? predicate = null, CancellationToken ct = default)
        {
            IQueryable<Session> query = dbContext.Sessions
                .AsNoTracking()
                .Include(s => s.Trainer)
                .Include(s => s.Category);

            if (predicate is not null) query = query.Where(predicate);

            return await query.ToListAsync(ct);
        }

        public async Task<List<SessionListInfo>> GetSessionsWithAvailableSlotsAsync(
            Expression<Func<Session, bool>>? predicate = null,
            int page = 1,
            int pageSize = 12,
            CancellationToken ct = default)
        {
            var sessions = dbContext.Sessions.AsNoTracking();
            if (predicate is not null)
            {
                sessions = sessions.Where(predicate);
            }

            var query = sessions
                .Select(s => new SessionListInfo
                {
                    Id = s.Id,
                    Description = s.Description,
                    Capacity = s.Capacity,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    TrainerName = s.Trainer.Name,
                    CategoryName = s.Category.CategoryName,
                    AvailableSlots = s.Capacity - s.Bookings.Count(),
                })
                .OrderByDescending(s => s.StartDate);

            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
        }

        public async Task<int> GetCountOfBookedSlotsAsync(int sessionId, CancellationToken ct = default)
        {
            return await dbContext.Bookings.AsNoTracking().CountAsync(b => b.SessionId == sessionId, ct);
        }

        public async Task<Session?> GetSessionByIdWithTrainerAndCategoryAsync(int sesstionId, CancellationToken ct)
        {
            return await dbContext.Sessions.AsNoTracking().Include(x=>x.Trainer).Include(x=>x.Category).FirstOrDefaultAsync(x=>x.Id == sesstionId, ct);
        }
    }
}
