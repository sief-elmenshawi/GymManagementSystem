using System.Security.Claims;
using GymManagement.BLL.Services.Interfaces;
using GymManagement.BLL.ViewModels.AnalyticsViewModels;
using GymManagement.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GymManagement.Tests;

public class HomeControllerTests
{
    private readonly Mock<IAnalyticsService> _analytics = new();

    [Fact]
    public async Task Index_ForSuperAdmin_RedirectsToDashboard()
    {
        var controller = CreateController("SuperAdmin");

        var result = await controller.Index(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Dashboard", redirect.ControllerName);
        Assert.Equal("Index", redirect.ActionName);
        _analytics.Verify(
            s => s.GetAnalyticsDataAysnc(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Index_ForMemberOnly_RedirectsToPortal()
    {
        var controller = CreateController("Member");

        var result = await controller.Index(CancellationToken.None);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/Member", redirect.Url);
        _analytics.Verify(
            s => s.GetAnalyticsDataAysnc(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("Member", "Admin")]
    [InlineData("Member", "SuperAdmin")]
    public async Task Index_ForMemberWithStaffRole_DoesNotUsePortal(string memberRole, string staffRole)
    {
        var expected = new AnalyticsViewModel { TotalMembers = 4 };
        _analytics
            .Setup(s => s.GetAnalyticsDataAysnc(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = CreateController(memberRole, staffRole);

        var result = await controller.Index(CancellationToken.None);

        if (staffRole == "SuperAdmin")
        {
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirect.ControllerName);
            _analytics.Verify(
                s => s.GetAnalyticsDataAysnc(It.IsAny<CancellationToken>()),
                Times.Never);
            return;
        }

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(expected, view.Model);
        _analytics.Verify(
            s => s.GetAnalyticsDataAysnc(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Index_ForNonSuperAdmin_ReturnsAnalyticsView()
    {
        var expected = new AnalyticsViewModel { TotalMembers = 4 };
        _analytics
            .Setup(s => s.GetAnalyticsDataAysnc(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = CreateController("Admin");

        var result = await controller.Index(CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(expected, view.Model);
        _analytics.Verify(
            s => s.GetAnalyticsDataAysnc(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private HomeController CreateController(params string[] roles)
    {
        var identity = new ClaimsIdentity(
            roles.Select(role => new Claim(ClaimTypes.Role, role)),
            authenticationType: "Test");
        var controller = new HomeController(NullLogger<HomeController>.Instance, _analytics.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity),
                },
            },
        };
        return controller;
    }
}
