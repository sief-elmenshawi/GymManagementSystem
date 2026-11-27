using GymManagement.DAL.Models;

namespace GymManagement.DAL.Repositorities.Interfaces;

public interface IMemberRepository : IGenericRepository<Member>
{
    Task<List<MemberListInfo>> GetMembersForListAsync(int page, int pageSize, CancellationToken ct = default);
}
