using System.Linq.Expressions;
using GymManagement.BLL.Service.Interfaces;
using GymManagement.BLL.Services.Interfaces;
using GymManagement.BLL.ViewModels.MemberViewModels;
using Microsoft.AspNetCore.Mvc;
using GymManagement.DAL.Models;
using GymManagement.PL.Controllers;
using GymManagement.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace GymManagement.Tests;

public class MemberPhotoTests
{
    [Fact]
    public async Task GetMemberPhotoAsync_UsesNoTrackingLookup_AndReturnsPhoto()
    {
        var uow = new UnitOfWorkMock();
        var member = TestData.Member();
        member.Photo = "member.png";
        uow.MemberRepository
            .Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Member, bool>>>(),
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        var service = new GymManagement.BLL.Service.Classes.MemberService(
            uow.UnitOfWork.Object,
            TestMapper.Create(),
            Mock.Of<IAttachementService>());

        var photo = await service.GetMemberPhotoAsync(member.Id, CancellationToken.None);

        Assert.Equal("member.png", photo);
        uow.MemberRepository.Verify(
            r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Member, bool>>>(),
                false,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Picture_UsesPhotoOnlyLookup_InsteadOfFullMemberDetails()
    {
        var memberService = new Mock<IMemberService>();
        var attachmentService = new Mock<IAttachementService>();
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        memberService
            .Setup(s => s.GetMemberPhotoAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync("member.png");
        attachmentService
            .Setup(s => s.GetFile("member.png", "MembersPictures"))
            .Returns((stream, "image/png"));
        var userManager = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);
        var controller = new MembersController(
            memberService.Object,
            attachmentService.Object,
            userManager.Object);

        var result = await controller.Picture(5, CancellationToken.None);

        var file = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("image/png", file.ContentType);
        memberService.Verify(
            s => s.GetMemberDetailsByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        memberService.Verify(
            s => s.GetMemberPhotoAsync(5, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
