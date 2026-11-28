using System.Linq.Expressions;
using GymManagement.BLL.Services.Classes;
using GymManagement.DAL.Models;
using GymManagement.Tests.Support;
using Moq;
using Xunit;

namespace GymManagement.Tests;

public sealed class PlanServiceTests
{
    private readonly UnitOfWorkMock _uow = new();
    private readonly PlanService _service;

    public PlanServiceTests()
    {
        _service = new PlanService(_uow.UnitOfWork.Object, TestMapper.Create());
    }

    [Fact]
    public async Task GetAllPlans_UsesDatabasePaging()
    {
        _uow.UnitOfWork
            .Setup(uow => uow.GetRepository<Plan>())
            .Returns(_uow.PlanRepository.Object);
        _uow.PlanRepository
            .Setup(repository => repository.GetPagedAsync(
                It.IsAny<Expression<Func<Plan, bool>>?>(),
                2, 10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Plan>
            {
                new()
                {
                    Id = 11,
                    Name = "Paged Plan",
                    Description = "Paged description",
                    DurationDays = 30,
                    Price = 500,
                    IsActive = true,
                },
            });
        _uow.PlanRepository
            .Setup(repository => repository.CountAsync(
                It.IsAny<Expression<Func<Plan, bool>>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);

        var result = await _service.GetAllPlansAsync(CancellationToken.None, 2, 10);

        Assert.Single(result.Items);
        Assert.Equal("Paged Plan", result.Items[0].Name);
        Assert.Equal(2, result.Paging.Page);
        Assert.Equal(10, result.Paging.PageSize);
        Assert.Equal(25, result.Paging.TotalCount);
        _uow.PlanRepository.Verify(
            repository => repository.GetPagedAsync(
                It.IsAny<Expression<Func<Plan, bool>>?>(),
                2, 10, false, It.IsAny<CancellationToken>()),
            Times.Once);
        _uow.PlanRepository.Verify(
            repository => repository.GetAllAsync(
                It.IsAny<Expression<Func<Plan, bool>>?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
