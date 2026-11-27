using GymManagement.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace GymManagement.DAL.Repositorities.Interfaces
{
        public interface IMembershipRepository : IGenericRepository<Membership>
        {
            Task<List<MembershipListInfo>> GetMembershipsForListAsync(
                Expression<Func<Membership, bool>>? predicate = null,
                int page = 1,
                int pageSize = 20,
                CancellationToken ct = default);
        }
    
}
