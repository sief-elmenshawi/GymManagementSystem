using GymManagement.BLL.Common;
using GymManagement.BLL.ViewModels.MemberViewModels;

namespace GymManagement.BLL.Service.Interfaces
{
    public interface IMemberService
    {
        Task<IEnumerable<MemberViewModel>> GetAllMembersAsync(CancellationToken ct = default);

        Task<Result> CreateMemberAsync(CreateMemberViewModel model, CancellationToken ct = default);

        Task<MemberViewModel?> GetMemberDetailsByIdAsync(int MemberId, CancellationToken ct = default);

        Task<HealthRecordViewModel?> GetMemberHealthRecordAsync(int MemberId, CancellationToken ct = default);

        Task<MemberToUpdateViewModel?> GetMemberToUpdatAsync(int MemberId, CancellationToken ct = default); 

        Task<Result> UpdateMemberDetailsAsync(int id, MemberToUpdateViewModel model, CancellationToken ct = default);

        Task<Result> RemoveMemberAsync(int MemberId, CancellationToken ct = default);

    }
}