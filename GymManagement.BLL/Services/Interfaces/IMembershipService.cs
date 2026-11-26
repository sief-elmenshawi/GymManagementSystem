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
        Task<IEnumerable<MemberShipViewModel>> GetAllMembershipsAsync(CancellationToken ct);
        Task<IEnumerable<PlanSelectListViewModel>> GetPlansForDropDownAsync(CancellationToken ct);
        Task<IEnumerable<MemberSelectListViewModel>> GetMembersForDropDownAsync(CancellationToken ct);

        Task<Result> CreateMembershipAsync(CreateMemberShipViewModel model,CancellationToken ct);
        Task<Result> DeleteActiveMembershipAsync(int id,CancellationToken ct);

    }
}
