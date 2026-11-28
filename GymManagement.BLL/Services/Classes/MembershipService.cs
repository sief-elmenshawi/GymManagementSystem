using AutoMapper;
using GymManagement.BLL.Common;
using GymManagement.BLL.Services.Interfaces;
using GymManagement.BLL.ViewModels.MembershipViewModels;
using GymManagement.DAL.Models;
using GymManagement.DAL.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace GymManagement.BLL.Services.Classes
{
    public class MembershipService : IMembershipService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        private readonly TimeProvider _timeProvider;

        public MembershipService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            TimeProvider? timeProvider = null)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        public async Task<Result> CreateMembershipAsync(CreateMembershipViewModel model, CancellationToken ct = default)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            if (model.StartDate.HasValue && model.StartDate.Value.Date < now.Date)
            {
                return Result.Validation("Start date cannot be in the past.");
            }

            var memberExists = await unitOfWork.GetRepository<Member>().AnyAsync(m => m.Id == model.MemberId, ct);
            if (!memberExists) return Result.NotFound("Member not found.");

            var plan = await unitOfWork.GetRepository<Plan>().GetByIdAsync(model.PlanId, ct);
            if (plan is null) return Result.NotFound("Plan not found.");
            if (!plan.IsActive) return Result.Fail("Plan is not active.");


            var hasActive = await unitOfWork.MembershipRepository
                .AnyAsync(m => m.MemberId == model.MemberId && m.EndDate > now, ct);
            if (hasActive) return Result.Fail("Member already has an active membership.");

            var entity = new Membership
            {
                MemberId = model.MemberId,
                PlanId = plan.Id,
                CreatedAt = now,
                EndDate = (model.StartDate ?? now).AddDays(plan.DurationDays),
            };

            unitOfWork.MembershipRepository.Add(entity);
            var result = await unitOfWork.SaveChangesAsync(ct);
            return result > 0 ? Result.Ok() : Result.Fail("Failed To Create New Membership");
        }

        public async Task<Result> DeleteActiveMembershipAsync(int memberId, CancellationToken ct = default)
        {
            var active = await unitOfWork.MembershipRepository.FirstOrDefaultAsync(
                m => m.MemberId == memberId
                    && m.EndDate > _timeProvider.GetUtcNow().UtcDateTime,
                tracking: true,
                ct: ct);

            if (active is null) return Result.NotFound("No active membership for this member.");

            unitOfWork.MembershipRepository.Delete(active);
            var result = await unitOfWork.SaveChangesAsync(ct);
            return result > 0 ? Result.Ok() : Result.Fail("Failed To Delete Membership");
        }

        public async Task<PagedResult<MembershipViewModel>> GetAllMembershipsAsync(
            CancellationToken ct = default, int page = 1, int pageSize = 20)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            Expression<Func<Membership, bool>> predicate = m => m.EndDate > now;

            var memberships = await unitOfWork.MembershipRepository.GetMembershipsForListAsync(
                predicate, page, pageSize, ct);
            var totalCount = await unitOfWork.MembershipRepository.CountAsync(predicate, ct);

            return new PagedResult<MembershipViewModel>
            {
                Items = mapper.Map<List<MembershipViewModel>>(memberships),
                Paging = new PagingInfo { Page = page, PageSize = pageSize, TotalCount = totalCount },
            };
        }

        public async Task<IEnumerable<PlanSelectListViewModel>> GetPlansForDropDownAsync(CancellationToken ct = default)
        {
            var plans = await unitOfWork.GetRepository<Plan>().GetLatestAsync(
                p => p.IsActive,
                take: 100,
                ct: ct);
            return mapper.Map<IEnumerable<PlanSelectListViewModel>>(plans);
        }

        public async Task<IEnumerable<MemberSelectListViewModel>> GetMembersForDropDownAsync(CancellationToken ct = default)
        {
            // Dropdowns are bounded so a large member table cannot be pulled into
            // memory on every form render. Add server-side search before raising this cap.
            var members = await unitOfWork.GetRepository<Member>().GetLatestAsync(
                take: 500,
                ct: ct);
            return mapper.Map<IEnumerable<MemberSelectListViewModel>>(members);
        }
    }
}
