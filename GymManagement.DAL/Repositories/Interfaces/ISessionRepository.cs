using GymManagement.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace GymManagement.DAL.Repositories.Interfaces
{
    public interface ISessionRepository : IGenericRepository<Session>
    {
        Task<IEnumerable<Session>> GetAllSessionsWithTrainerAndCategoryAsync(
              Expression<Func<Session, bool>>? predicate = null,
              CancellationToken ct = default);

        Task<List<SessionListInfo>> GetSessionsWithAvailableSlotsAsync(
              Expression<Func<Session, bool>>? predicate = null,
              int page = 1,
              int pageSize = 12,
              CancellationToken ct = default);

        Task<int> GetCountOfBookedSlotsAsync(int sessionId , CancellationToken ct = default);

        Task<Session?> GetSessionByIdWithTrainerAndCategoryAsync(int sesstionId , CancellationToken ct);

    }
}
