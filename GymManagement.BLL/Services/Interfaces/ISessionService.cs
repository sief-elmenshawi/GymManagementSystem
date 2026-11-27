using GymManagement.BLL.Common;
using GymManagement.BLL.ViewModels.SessionViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagement.BLL.Services.Interfaces
{
    public interface ISessionService
    {
        Task<Result<SessionViewModel>> GetSessionByIdAysnc(int sessionId, CancellationToken ct);
         
        Task<PagedResult<SessionViewModel>?> GetAllSessionsAync(
            CancellationToken ct = default, int page = 1, int pageSize = 12);

        Task<Result> CreateSessionAync(CreateSessionViewModel model, CancellationToken ct = default);

        Task<IEnumerable<TrainerSelectViewModel>> GetTrainersForDropDownAsync(CancellationToken ct = default);  
        Task<IEnumerable<CategorySelectViewModel>> GetCategoriesForDropDownAsync(CancellationToken ct = default);

        Task<Result<UpdateSessionViewModel>> GetSessionToUpdateAsync(int sessionId , CancellationToken ct =default);

        Task<Result> UpdateSessionAsync(int id,UpdateSessionViewModel model , CancellationToken ct = default);

        Task<Result> RemoveSessionAysnc(int sessionId, CancellationToken ct=default);

    }
}
