using GymManagement.BLL.ViewModels.MemberViewModels;

namespace GymManagement.BLL.Service.Interfaces
{
    public interface IMemberService
    {
        Task<IEnumerable<MemberViewModel>> GetAllMembersAsync(CancellationToken ct = default);

        Task<bool> CreateMemberAsync(CreateMemberViewModel model, CancellationToken ct);
    }
}