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

        public PlanService(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }



        public async Task<IEnumerable<PlanViewModel>> GetAllPlansAsync(CancellationToken ct = default)
        {
            var plans = await unitOfWork.GetRepository<Plan>().GetAllAsync(ct : ct);
            return plans.Select(p => new PlanViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                DurationDays = p.DurationDays,
                Price = p.Price,
                IsActive = p.IsActive
            }); 
        }

        public async Task<PlanViewModel?> GetPlanByIdAsync(int PlanId, CancellationToken ct = default)
        {
            var plan =await unitOfWork.GetRepository<Plan>().GetByIdAsync(PlanId, ct);
            if (plan == null)
                return null;
            else
                return new PlanViewModel
                {
                    Id = plan.Id,
                    Name = plan.Name,
                    Description = plan.Description,
                    DurationDays = plan.DurationDays,
                    Price = plan.Price,
                    IsActive = plan.IsActive
                };
        }

        public async Task<UpdatePlanViewModel?> GetPlanToUpdateAsync(int PlanId, CancellationToken ct = default)
        {
            var plan =await unitOfWork.GetRepository<Plan>().GetByIdAsync(PlanId, ct);
            if (plan is null || !plan.IsActive) return null;
            if (await HasActiveMembershipsAsync(PlanId, ct))
                return null;
            else
            {
                return new UpdatePlanViewModel
                {
                    PlanName = plan.Name,
                    Description = plan.Description,
                    DurationDays = plan.DurationDays,
                    Price = plan.Price
                };
            }
        }

        public async Task<bool> ToggleActivationAsync(int PlanId, CancellationToken ct = default)
        {
            var plan = await unitOfWork.GetRepository<Plan>().GetByIdAsync(PlanId, ct);
            if (plan is null) return false;

            if(plan.IsActive && await HasActiveMembershipsAsync(PlanId, ct))
                return false;
            else
            {
                plan.IsActive = !plan.IsActive;
                plan.UpdatedAt = DateTime.Now;
                unitOfWork.GetRepository<Plan>().Update(plan);
                var result = await unitOfWork.SaveChangesAsync(ct);
                return result > 0;
            }
        }

        public async Task<bool> UpdatePlanAsync(int id, UpdatePlanViewModel model, CancellationToken ct = default)
        {
            var plan =await unitOfWork.GetRepository<Plan>().GetByIdAsync(id, ct);
            if (plan is null) return false;
            if(await HasActiveMembershipsAsync(id, ct))
                return false;
            else
            {
                plan.Description = model.Description;
                plan.DurationDays = model.DurationDays;
                plan.Price = model.Price;
                plan.UpdatedAt = DateTime.Now;
                unitOfWork.GetRepository<Plan>().Update(plan);
                var result = await unitOfWork.SaveChangesAsync(ct);
                return result > 0;
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
