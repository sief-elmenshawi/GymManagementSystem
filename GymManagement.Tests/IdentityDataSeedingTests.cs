using GymManagement.DAL.Data.DataSeeding;
using GymManagement.DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GymManagement.Tests;

public sealed class IdentityDataSeedingTests
{
    [Fact]
    public async Task SeedAsync_RoleAssignmentFailure_ThrowsWithDetails()
    {
        var roleManager = CreateRoleManager();
        roleManager.Setup(manager => manager.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);
        userManager.Setup(manager => manager.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(manager => manager.IsInRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(false);
        userManager.Setup(manager => manager.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Code = "RoleAssignFailed",
                Description = "role assignment failed",
            }));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            IdentityDataSeeding.SeedAsync(roleManager.Object, userManager.Object, Mock.Of<ILogger>(), "P@ssw0rd"));

        Assert.Contains("RoleAssignFailed", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SeedAsync_UserCreationFailure_ThrowsWithDetails()
    {
        var roleManager = CreateRoleManager();
        roleManager.Setup(manager => manager.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);
        userManager.Setup(manager => manager.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordTooWeak",
                Description = "password was rejected",
            }));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            IdentityDataSeeding.SeedAsync(roleManager.Object, userManager.Object, Mock.Of<ILogger>(), "weak"));

        Assert.Contains("PasswordTooWeak", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SeedAsync_WithoutPassword_SkipsUserCreation()
    {
        var roleManager = CreateRoleManager();
        roleManager.Setup(manager => manager.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

        var userManager = CreateUserManager();

        await IdentityDataSeeding.SeedAsync(roleManager.Object, userManager.Object, Mock.Of<ILogger>(), null);

        userManager.Verify(
            manager => manager.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
            Times.Never);
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManager() => new(
        Mock.Of<IUserStore<ApplicationUser>>(),
        null!, null!, null!, null!, null!, null!, null!, null!);

    private static Mock<RoleManager<IdentityRole>> CreateRoleManager() => new(
        Mock.Of<IRoleStore<IdentityRole>>(),
        null!, null!, null!, null!);
}
