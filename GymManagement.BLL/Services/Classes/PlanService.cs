using AutoMapper;
using GymManagement.BLL.Common;
using GymManagement.BLL.Services.Interfaces;
using GymManagement.BLL.ViewModels.PlanViewModels;
using GymManagement.DAL.Data.Models;
using GymManagement.DAL.Repositorities.Interfaces;
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

        public PlanService(IUnitOfWork unitOfWork ,IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }



        public async Task<IEnumerable<PlanViewModel>> GetAllPlansAsync(CancellationToken ct = default)
        {
            var plans = await unitOfWork.GetRepository<Plan>().GetAllAsync(ct : ct);
           
            return mapper.Map<IEnumerable<PlanViewModel>>(plans);
        }

        public async Task<PlanViewModel?> GetPlanByIdAsync(int PlanId, CancellationToken ct = default)
        {
            var plan =await unitOfWork.GetRepository<Plan>().GetByIdAsync(PlanId, ct);
            if (plan == null)
                return null;
            else
              return mapper.Map<PlanViewModel>(plan);
        }

        public async Task<UpdatePlanViewModel?> GetPlanToUpdateAsync(int PlanId, CancellationToken ct = default)
        {
            var plan =await unitOfWork.GetRepository<Plan>().GetByIdAsync(PlanId, ct);
            if (plan is null || !plan.IsActive) return null;
            if (await HasActiveMembershipsAsync(PlanId, ct))
                return null;
            else
            return mapper.Map<UpdatePlanViewModel>(plan);
        }

        public async Task<Result> ToggleActivationAsync(int PlanId, CancellationToken ct = default)
        {
            var plan = await unitOfWork.GetRepository<Plan>().GetByIdAsync(PlanId, ct);
            if (plan is null) return Result.Validation("Not Found Plan");

            if (plan.IsActive && await HasActiveMembershipsAsync(PlanId, ct))
                return Result.Validation("Not Active This Plan");
            else
            {
                plan.IsActive = !plan.IsActive;
                plan.UpdatedAt = DateTime.Now;
                unitOfWork.GetRepository<Plan>().Update(plan);
                var result = await unitOfWork.SaveChangesAsync(ct);
                return result > 0 ? Result.Ok() : Result.Fail("Not Allow Now");
            }
        }

        public async Task<Result> UpdatePlanAsync(int id, UpdatePlanViewModel model, CancellationToken ct = default)
        {
            var plan =await unitOfWork.GetRepository<Plan>().GetByIdAsync(id, ct);
            if (plan is null) return Result.NotFound("Plan Not Found");
            if (await HasActiveMembershipsAsync(id, ct))
                return Result.Validation("Can Not Update");
            else
            {

                mapper.Map(model, plan);

                plan.UpdatedAt = DateTime.Now;
                unitOfWork.GetRepository<Plan>().Update(plan);
                var result = await unitOfWork.SaveChangesAsync(ct);
                return result > 0 ? Result.Ok() : Result.Fail("Cant Update This Plan");
            }

        }


        #region Helper Methods

        private async Task<bool> HasActiveMembershipsAsync(int planId, CancellationToken ct)
        {
            return await unitOfWork.GetRepository<Membership>().AnyAsync(m => m.PlanId == planId && m.EndDate > DateTime.Now, ct);
        } 
        #endregion
    }
}
