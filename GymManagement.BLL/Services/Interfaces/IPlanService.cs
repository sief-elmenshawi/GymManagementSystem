using GymManagement.BLL.Common;
using GymManagement.BLL.ViewModels.PlanViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagement.BLL.Services.Interfaces
{
    public interface IPlanService
    {
        Task<PagedResult<PlanViewModel>> GetAllPlansAsync(
            CancellationToken ct = default, int page = 1, int pageSize = 12);
        Task<PlanViewModel?> GetPlanByIdAsync(int PlanId, CancellationToken ct = default);
        Task<UpdatePlanViewModel?> GetPlanToUpdateAsync(int PlanId, CancellationToken ct = default);
        Task<Result> ToggleActivationAsync(int PlanId, CancellationToken ct = default);
        Task<Result> UpdatePlanAsync(int id, UpdatePlanViewModel model, CancellationToken ct = default);
    }
}
