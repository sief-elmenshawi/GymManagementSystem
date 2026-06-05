using GymManagement.DAL.Data.DbContexts;
using GymManagement.DAL.Data.Models;
using GymManagement.DAL.Repositorities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagement.DAL.Repositorities.Classes
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly GymDbContext dbContext;
        private readonly Dictionary<string, object> repositories = [];
        public UnitOfWork(GymDbContext dbContext , ISessionRepository sessionRepository)
        {
            this.dbContext = dbContext;
            this.SessionRepository = sessionRepository;
        }

        public ISessionRepository SessionRepository {  get;}

        public IGenericRepository<TEntity> GetRepository<TEntity>() where TEntity : BaseEntity, new()
        {
            var typeName = typeof(TEntity).Name;
            if(repositories.TryGetValue(typeName, out object? value))
            {
                return (IGenericRepository<TEntity>)value;
            }

            var repository = new GenericRepository<TEntity>(dbContext);
            repositories[typeName] = repository;
            return repository;
        }

        public async Task<int> SaveChangesAsync(CancellationToken ct = default) => await dbContext.SaveChangesAsync(ct);
    }
}
