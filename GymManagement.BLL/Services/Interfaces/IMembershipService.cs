using GymManagement.BLL.Common;
using GymManagement.BLL.ViewModels.MembershipViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagement.BLL.Services.Interfaces
{
    public interface IMembershipService
    {
        Task<PagedResult<MemberShipViewModel>> GetAllMembershipsAsync(
            CancellationToken ct = default, int page = 1, int pageSize = 20);
        Task<IEnumerable<PlanSelectListViewModel>> GetPlansForDropDownAsync(CancellationToken ct);
        Task<IEnumerable<MemberSelectListViewModel>> GetMembersForDropDownAsync(CancellationToken ct);

        Task<Result> CreateMembershipAsync(CreateMemberShipViewModel model,CancellationToken ct);
        Task<Result> DeleteActiveMembershipAsync(int id,CancellationToken ct);

    }
}
