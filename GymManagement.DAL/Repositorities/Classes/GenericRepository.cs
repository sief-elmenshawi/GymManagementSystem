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
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : BaseEntity, new()
    {
        private readonly GymDbContext _dbContext;
        private readonly DbSet<TEntity> _set;

        public GenericRepository(GymDbContext dbContext)
        {
            _dbContext = dbContext;
            _set = dbContext.Set<TEntity>();
        }
        public void Add(TEntity entity) => _set.Add(entity);
        public void Update(TEntity entity) => _set.Update(entity);
        public void Delete(TEntity entity) => _set.Remove(entity);


        public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool tracking = false,
                                                        CancellationToken ct = default)
        {
            IQueryable<TEntity> query = tracking ? _set : _set.AsNoTracking();
            return await query.FirstOrDefaultAsync(predicate, ct);
        }

        public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default)
            => _set.AsNoTracking().AnyAsync(predicate, ct);

        public Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default)
            => predicate is null ? _set.AsNoTracking().CountAsync(ct) : _set.AsNoTracking().CountAsync(predicate, ct);
        public async Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>>? predicate = null, bool tracking = false, CancellationToken ct = default)
        {
            IQueryable<TEntity> query = tracking ? _set : _set.AsNoTracking();
            if (predicate is not null) query = query.Where(predicate);
            return await query.ToListAsync(ct);
        }

        public async Task<IEnumerable<TEntity>> GetPagedAsync(
            Expression<Func<TEntity, bool>>? predicate = null,
            int page = 1,
            int pageSize = 20,
            bool tracking = false,
            CancellationToken ct = default)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            IQueryable<TEntity> query = tracking ? _set : _set.AsNoTracking();
            if (predicate is not null) query = query.Where(predicate);

            var skip = (long)(page - 1) * pageSize;

            return await query
                .OrderBy(entity => entity.Id)
                .Skip(skip > int.MaxValue ? int.MaxValue : (int)skip)
                .Take(pageSize)
                .ToListAsync(ct);
        }
        public async Task<TEntity?> GetByIdAsync(int id, CancellationToken ct = default) => await _set.FindAsync([id], ct);

    }
}