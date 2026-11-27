using GymManagement.DAL.Data.DbContexts;
using GymManagement.DAL.Models;
using GymManagement.DAL.Repositorities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.DAL.Repositorities.Classes;

public class MemberRepository : GenericRepository<Member>, IMemberRepository
{
    private readonly GymDbContext _dbContext;

    public MemberRepository(GymDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<MemberListInfo>> GetMembersForListAsync(
        int page, int pageSize, CancellationToken ct = default) =>
        await _dbContext.Members
            .AsNoTracking()
            .OrderBy(m => m.Id)
            .Select(m => new MemberListInfo
            {
                Id = m.Id,
                Name = m.Name,
                Email = m.Email,
                Phone = m.Phone,
                Gender = m.Gender,
                HasPhoto = m.Photo != null && m.Photo != string.Empty,
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
}
