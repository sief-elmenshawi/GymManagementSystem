using GymManagement.BLL.Services.Interfaces;
using GymManagement.BLL.ViewModels.AnalyticsViewModels;
using GymManagement.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GymManagement.Tests;

public class DashboardControllerTests
{
    private readonly Mock<IAnalyticsService> _analytics = new();

    [Fact]
    public async Task Index_ReturnsAnalyticsModel()
    {
        var expected = new AnalyticsViewModel
        {
            TotalMembers = 12,
            ActiveMembers = 8,
            TotalTrainers = 3,
        };
        _analytics
            .Setup(s => s.GetAnalyticsDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = new DashboardController(_analytics.Object);
        var result = await controller.Index(CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(expected, view.Model);
        _analytics.Verify(
            s => s.GetAnalyticsDataAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
