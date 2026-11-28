using GymManagement.DAL.Data.DbContexts;
using GymManagement.DAL.Models;
using GymManagement.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace GymManagement.DAL.Repositories.Classes
{
    public class MembershipRepository :GenericRepository<Membership> , IMembershipRepository
    {
        private readonly GymDbContext _dbContext;

        public MembershipRepository(GymDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<MembershipListInfo>> GetMembershipsForListAsync(
            Expression<Func<Membership, bool>>? predicate = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default)
        {
            IQueryable<Membership> memberships = _dbContext.Memberships.AsNoTracking();
            if (predicate is not null)
            {
                memberships = memberships.Where(predicate);
            }

            return await memberships
                .Select(m => new MembershipListInfo
                {
                    MemberId = m.MemberId,
                    PlanId = m.PlanId,
                    MemberName = m.Member.Name,
                    PlanName = m.Plan.Name,
                    StartDate = m.CreatedAt,
                    EndDate = m.EndDate,
                })
                .OrderByDescending(m => m.EndDate)
                .ThenBy(m => m.MemberId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
        }

    }
}
