using GymManagement.DAL.Data.DbContexts;
using GymManagement.DAL.Models;
using Microsoft.EntityFrameworkCore.Storage;
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
        public UnitOfWork(
            GymDbContext dbContext,
            ISessionRepository sessionRepository,
            IBookingRepository bookingRepository,
            IMembershipRepository membershipRepository,
            IMemberRepository memberRepository)
        {
            this.dbContext = dbContext;
            SessionRepository = sessionRepository;
            BookingRepository = bookingRepository;
            MembershipRepository = membershipRepository;
            MemberRepository = memberRepository;
        }

        public ISessionRepository SessionRepository {  get;}

        public IMembershipRepository MembershipRepository { get; }

        public IBookingRepository BookingRepository { get; }

        public IMemberRepository MemberRepository { get; }

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

        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default) =>
            await dbContext.Database.BeginTransactionAsync(ct);
    }
}
