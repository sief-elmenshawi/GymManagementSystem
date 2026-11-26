using GymManagement.DAL.Models;
using GymManagement.DAL.Repositorities.Interfaces;
using Moq;

namespace GymManagement.Tests.Support;

internal sealed class UnitOfWorkMock
{
    public Mock<IUnitOfWork> UnitOfWork { get; } = new();
    public Mock<IGenericRepository<Trainer>> TrainerRepository { get; } = new();
    public Mock<IGenericRepository<Category>> CategoryRepository { get; } = new();
    public Mock<IGenericRepository<Session>> SessionRepositoryGeneric { get; } = new();
    public Mock<IGenericRepository<Member>> MemberRepository { get; } = new();
    public Mock<IGenericRepository<Membership>> MembershipRepositoryGeneric { get; } = new();
    public Mock<ISessionRepository> SessionRepository { get; } = new();
    public Mock<IBookingRepository> BookingRepository { get; } = new();
    public Mock<IMembershipRepository> MembershipRepository { get; } = new();

    public UnitOfWorkMock()
    {
        UnitOfWork.Setup(u => u.GetRepository<Trainer>()).Returns(TrainerRepository.Object);
        UnitOfWork.Setup(u => u.GetRepository<Category>()).Returns(CategoryRepository.Object);
        UnitOfWork.Setup(u => u.GetRepository<Session>()).Returns(SessionRepositoryGeneric.Object);
        UnitOfWork.Setup(u => u.GetRepository<Member>()).Returns(MemberRepository.Object);
        UnitOfWork.Setup(u => u.GetRepository<Membership>()).Returns(MembershipRepositoryGeneric.Object);

        UnitOfWork.Setup(u => u.SessionRepository).Returns(SessionRepository.Object);
        UnitOfWork.Setup(u => u.BookingRepository).Returns(BookingRepository.Object);
        UnitOfWork.Setup(u => u.MembershipRepository).Returns(MembershipRepository.Object);

        // Happy-path default; individual tests can override it.
        UnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    public void SetSaveResult(int affectedRows) =>
        UnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(affectedRows);
}
