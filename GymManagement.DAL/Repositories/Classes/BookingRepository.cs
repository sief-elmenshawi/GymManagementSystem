using GymManagement.DAL.Data.DbContexts;
using GymManagement.DAL.Models;
using GymManagement.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagement.DAL.Repositories.Classes
{
    public class BookingRepository : GenericRepository<Booking>, IBookingRepository
    {
        private readonly GymDbContext _dbContext;

        public BookingRepository(GymDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }
        public Task<List<Booking>> GetBySessionIdAsync(int sessionId, CancellationToken ct = default)
                => _dbContext.Bookings.AsNoTracking().Include(b => b.Member).Where(b => b.SessionId == sessionId).ToListAsync(ct);

        public async Task<IEnumerable<Member>> GetAvailableMembersAsync(int sessionId, CancellationToken ct = default) =>
            await _dbContext.Members.AsNoTracking()
                .Where(m => !_dbContext.Bookings.Any(b => b.SessionId == sessionId && b.MemberId == m.Id))
                .Select(m => new Member { Id = m.Id, Name = m.Name })
                .ToListAsync(ct);

    }
}
