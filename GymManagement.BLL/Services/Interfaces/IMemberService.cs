using GymManagement.BLL.Common;
using GymManagement.BLL.ViewModels.MemberViewModels;

namespace GymManagement.BLL.Service.Interfaces
{
    public interface IMemberService
    {
        Task<PagedResult<MemberViewModel>> GetAllMembersAsync(
            CancellationToken ct = default, int page = 1, int pageSize = 20);

        Task<Result> CreateMemberAsync(CreateMemberViewModel model, CancellationToken ct = default);

        Task<MemberViewModel?> GetMemberDetailsByIdAsync(int memberId, CancellationToken ct = default);

        Task<string?> GetMemberPhotoAsync(int memberId, CancellationToken ct = default);

        Task<HealthRecordViewModel?> GetMemberHealthRecordAsync(int memberId, CancellationToken ct = default);

        Task<MemberToUpdateViewModel?> GetMemberToUpdatAsync(int memberId, CancellationToken ct = default); 

        Task<Result> UpdateMemberDetailsAsync(int id, MemberToUpdateViewModel model, CancellationToken ct = default);

        Task<Result> RemoveMemberAsync(int memberId, CancellationToken ct = default);

    }
}