using GymManagement.DAL.Data.DbContexts;
using GymManagement.DAL.Data.Models;
using GymManagement.DAL.Repositorities.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public async Task<IEnumerable<Session>> GetAllSessionsWithTrainerAndCategoryAsync(CancellationToken ct = default)
        {
            var query = dbContext.Sessions.AsNoTracking().Include(s=>s.Trainer).Include(s => s.Category);
            return await query.ToListAsync(ct);
        }

        public async Task<int> GetCountOfBookSlotsAsync(int sessionId, CancellationToken ct = default)
        {
            return await dbContext.Bookings.AsNoTracking().CountAsync(b => b.SessionId == sessionId);
        }

        public async Task<Session?> GetSessionByIdWithTrainerAndCategoryAsync(int sesstionId, CancellationToken ct)
        {
            return await dbContext.Sessions.AsNoTracking().Include(x=>x.Trainer).Include(x=>x.Category).FirstOrDefaultAsync(x=>x.Id == sesstionId);
        }
    }
}
