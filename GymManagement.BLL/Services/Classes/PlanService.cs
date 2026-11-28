using AutoMapper;
using GymManagement.BLL.Common;
using GymManagement.BLL.Services.Interfaces;
using GymManagement.BLL.ViewModels.PlanViewModels;
using GymManagement.DAL.Models;
using GymManagement.DAL.Repositories.Classes;
using GymManagement.DAL.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagement.BLL.Services.Classes
{
    public class PlanService : IPlanService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        private readonly TimeProvider _timeProvider;

        public PlanService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            TimeProvider? timeProvider = null)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }



        public async Task<PagedResult<PlanViewModel>> GetAllPlansAsync(
            CancellationToken ct = default, int page = 1, int pageSize = 12)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var repo = unitOfWork.GetRepository<Plan>();
            var plans = await repo.GetPagedAsync(page: page, pageSize: pageSize, ct: ct);
            var totalCount = await repo.CountAsync(ct: ct);

            return new PagedResult<PlanViewModel>
            {
                Items = mapper.Map<List<PlanViewModel>>(plans),
                Paging = new PagingInfo { Page = page, PageSize = pageSize, TotalCount = totalCount },
            };
        }

        public async Task<PlanViewModel?> GetPlanByIdAsync(int PlanId, CancellationToken ct = default)
        {
            var plan =await unitOfWork.GetRepository<Plan>().GetByIdAsync(PlanId, ct);
            return plan is null ? null : mapper.Map<PlanViewModel>(plan);

        }

        public async Task<UpdatePlanViewModel?> GetPlanToUpdateAsync(int PlanId, CancellationToken ct = default)
        {
            var plan =await unitOfWork.GetRepository<Plan>().GetByIdAsync(PlanId, ct);
            if (plan is null || !plan.IsActive) return null;
            if (await HasActiveMembershipsAsync(PlanId, ct)) return null;

           return mapper.Map<UpdatePlanViewModel>(plan);
        }   

        public async Task<Result> ToggleActivationAsync(int PlanId, CancellationToken ct = default)
        {
            var repo = unitOfWork.GetRepository<Plan>();
            var plan = await repo.GetByIdAsync(PlanId, ct);
            if (plan is null) return Result.Validation("Plan not found");

            if (plan.IsActive && await HasActiveMembershipsAsync(PlanId, ct))
                return Result.Validation("Cannot deactivate a plan that has active memberships.");

            plan.IsActive = !plan.IsActive;
            plan.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
            repo.Update(plan);
            var result = await unitOfWork.SaveChangesAsync(ct);
            return result > 0 ? Result.Ok() : Result.Fail("Failed to Toggle Plan Status");
        }

        public async Task<Result> UpdatePlanAsync(int id, UpdatePlanViewModel model, CancellationToken ct = default)
        {
            var repo = unitOfWork.GetRepository<Plan>();
            var plan =await repo.GetByIdAsync(id, ct);
            if (plan is null) return Result.NotFound("Plan Not Found");
            if (await HasActiveMembershipsAsync(id, ct))
                return Result.Validation("Cannot edit a plan that has active memberships.");

            mapper.Map(model, plan);
            plan.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
            repo.Update(plan);
            await unitOfWork.SaveChangesAsync(ct);
            return Result.Ok();

        }


        #region Helper Methods

        private async Task<bool> HasActiveMembershipsAsync(int planId, CancellationToken ct)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            return await unitOfWork.MembershipRepository.AnyAsync(
                m => m.PlanId == planId && m.EndDate > now,
                ct);
        } 
        #endregion
    }
}
