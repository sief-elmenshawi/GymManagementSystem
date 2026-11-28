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
            IdentityDataSeeding.SeedAsync(roleManager.Object, userManager.Object, Mock.Of<ILogger>(), "P@ssw0rd", TestContext.Current.CancellationToken));

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
            IdentityDataSeeding.SeedAsync(roleManager.Object, userManager.Object, Mock.Of<ILogger>(), "weak", TestContext.Current.CancellationToken));

        Assert.Contains("PasswordTooWeak", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SeedAsync_WithoutPassword_SkipsUserCreation()
    {
        var roleManager = CreateRoleManager();
        roleManager.Setup(manager => manager.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

        var userManager = CreateUserManager();

        await IdentityDataSeeding.SeedAsync(roleManager.Object, userManager.Object, Mock.Of<ILogger>(), null, TestContext.Current.CancellationToken);

        userManager.Verify(
            manager => manager.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task SeedAsync_CreatesMemberRole()
    {
        var roleManager = CreateRoleManager();
        roleManager.Setup(manager => manager.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        roleManager.Setup(manager => manager.CreateAsync(It.IsAny<IdentityRole>()))
            .ReturnsAsync(IdentityResult.Success);

        var userManager = CreateUserManager();

        await IdentityDataSeeding.SeedAsync(roleManager.Object, userManager.Object, Mock.Of<ILogger>(), null, TestContext.Current.CancellationToken);

        roleManager.Verify(
            manager => manager.CreateAsync(It.Is<IdentityRole>(role => role.Name == "Member")),
            Times.Once);
    }

    [Fact]
    public async Task SeedAsync_ExistingUnconfirmedUser_IsConfirmed()
    {
        var roleManager = CreateRoleManager();
        roleManager.Setup(manager => manager.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

        var existing = new ApplicationUser
        {
            Email = "Serag@gmail.com",
            EmailConfirmed = false,
        };

        var confirmedOtherUser = new ApplicationUser
        {
            Email = "Nader@gmail.com",
            EmailConfirmed = true,
        };

        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((string? email) =>
                string.Equals(email, existing.Email, StringComparison.OrdinalIgnoreCase)
                    ? existing
                    : confirmedOtherUser);
        userManager.Setup(manager => manager.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(manager => manager.IsInRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        await IdentityDataSeeding.SeedAsync(
            roleManager.Object,
            userManager.Object,
            Mock.Of<ILogger>(),
            "P@ssw0rd",
            TestContext.Current.CancellationToken);

        Assert.True(existing.EmailConfirmed);
        userManager.Verify(
            manager => manager.UpdateAsync(It.Is<ApplicationUser>(user => user.EmailConfirmed)),
            Times.Once);
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
