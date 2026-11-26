using System.Linq.Expressions;
using GymManagement.BLL.Services.Classes;
using GymManagement.DAL.Models;
using GymManagement.Tests.Support;
using Moq;
using Xunit;

namespace GymManagement.Tests;

public class AnalyticsServiceTests
{
    [Fact]
    public async Task GetAnalyticsData_ClassifiesSessionsAndReturnsAllCounts()
    {
        var uow = new UnitOfWorkMock();
        var service = new AnalyticsService(uow.UnitOfWork.Object);
        var now = DateTime.Now;

        var sessions = new[]
        {
            TestData.FutureSession(start: now.AddDays(1), end: now.AddDays(1).AddHours(1), id: 1),
            TestData.FutureSession(start: now.AddHours(-1), end: now.AddHours(1), id: 2),
            TestData.FutureSession(start: now.AddDays(-2), end: now.AddDays(-1), id: 3),
        };
        var memberships = new[]
        {
            new Membership { EndDate = now.AddDays(10) },
            new Membership { EndDate = now.AddDays(-10) },
        };

        uow.SessionRepositoryGeneric
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Session, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Session, bool>>? predicate, CancellationToken _) =>
                sessions.Count(predicate!.Compile()));
        uow.MembershipRepositoryGeneric
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Membership, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Membership, bool>>? predicate, CancellationToken _) =>
                memberships.Count(predicate!.Compile()));
        uow.MemberRepository
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Member, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(12);
        uow.TrainerRepository
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Trainer, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);

        var result = await service.GetAnalyticsDataAysnc(CancellationToken.None);

        Assert.Equal(12, result.TotalMembers);
        Assert.Equal(4, result.TotalTrainers);
        Assert.Equal(1, result.ActiveMembers);
        Assert.Equal(1, result.UpcomingSessions);
        Assert.Equal(1, result.OngoingSessions);
        Assert.Equal(1, result.CompletedSessions);
    }

    [Fact]
    public async Task GetAnalyticsData_ForwardsCancellationTokenToRepositories()
    {
        var uow = new UnitOfWorkMock();
        var service = new AnalyticsService(uow.UnitOfWork.Object);
        using var cts = new CancellationTokenSource();

        uow.MemberRepository
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Member, bool>>?>(), cts.Token))
            .ReturnsAsync(7);

        var result = await service.GetAnalyticsDataAysnc(cts.Token);

        Assert.Equal(7, result.TotalMembers);
        uow.MemberRepository.Verify(
            r => r.CountAsync(It.IsAny<Expression<Func<Member, bool>>?>(), cts.Token),
            Times.Once);
    }
}
