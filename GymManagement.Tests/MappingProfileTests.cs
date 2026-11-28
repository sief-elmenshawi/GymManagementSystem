using AutoMapper;
using GymManagement.BLL.ViewModels.MemberViewModels;
using GymManagement.BLL.ViewModels.PlanViewModels;
using GymManagement.BLL.ViewModels.SessionViewModels;
using GymManagement.DAL.Models;
using GymManagement.DAL.Models.Enums;
using GymManagement.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using MappingProfile = GymManagement.BLL.MappingProfile.MappingProfile;

namespace GymManagement.Tests;

public class MappingProfileTests
{
    private readonly IMapper _mapper = TestMapper.Create();

    [Fact]
    public void Configuration_IsValid()
    {
        var configuration = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance);

        configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void MapsSession_WithTrainerAndCategoryNames()
    {
        var session = TestData.FutureSession();

        var viewModel = _mapper.Map<SessionViewModel>(session);

        Assert.Equal(session.Id, viewModel.Id);
        Assert.Equal("Trainer One", viewModel.TrainerName);
        Assert.Equal("Cardio", viewModel.CategoryName);
        Assert.Equal(session.Capacity, viewModel.Capacity);
    }

    [Fact]
    public void MapsSessionListInfo_ToSessionViewModel()
    {
        var info = TestData.SessionListInfo(capacity: 12, availableSlots: 5);

        var viewModel = _mapper.Map<SessionViewModel>(info);

        Assert.Equal(info.Id, viewModel.Id);
        Assert.Equal(info.Description, viewModel.Description);
        Assert.Equal(info.StartDate, viewModel.StartDate);
        Assert.Equal(info.EndDate, viewModel.EndDate);
        Assert.Equal(info.Capacity, viewModel.Capacity);
        Assert.Equal(5, viewModel.AvailableSlots);
        Assert.Equal("Trainer One", viewModel.TrainerName);
        Assert.Equal("Cardio", viewModel.CategoryName);
    }

    [Fact]
    public void MapsMember_FormatsAddressAndDateOfBirth()
    {
        var member = TestData.Member();

        var viewModel = _mapper.Map<MemberViewModel>(member);

        Assert.Equal("12 - Nile Street - Cairo", viewModel.Address);
        Assert.Equal(member.DateOfBirth.ToShortDateString(), viewModel.DateOfBirth);
        Assert.Equal(member.Name, viewModel.Name);
    }

    [Fact]
    public void MapsMemberListInfo_WithoutLoadingPhotoOrDetails()
    {
        var info = new MemberListInfo
        {
            Id = 7,
            Name = "List Member",
            Email = "list@example.com",
            Phone = "01000000000",
            Gender = Gender.Female,
            HasPhoto = true,
        };

        var viewModel = _mapper.Map<MemberViewModel>(info);

        Assert.Equal(7, viewModel.Id);
        Assert.Equal("List Member", viewModel.Name);
        Assert.Equal("list@example.com", viewModel.Email);
        Assert.Equal("01000000000", viewModel.Phone);
        Assert.Equal("Female", viewModel.Gender);
        Assert.True(viewModel.HasPhoto);
        Assert.Null(viewModel.Photo);
        Assert.Null(viewModel.Address);
        Assert.Null(viewModel.DateOfBirth);
    }

    [Fact]
    public void MapsMembershipListInfo_ToMembershipViewModel()
    {
        var start = new DateTime(2026, 1, 1);
        var end = new DateTime(2026, 12, 31);
        var info = new MembershipListInfo
        {
            MemberId = 5,
            PlanId = 9,
            MemberName = "Member One",
            PlanName = "Gold",
            StartDate = start,
            EndDate = end,
        };

        var viewModel = _mapper.Map<GymManagement.BLL.ViewModels.MembershipViewModels.MembershipViewModel>(info);

        Assert.Equal(5, viewModel.MemberId);
        Assert.Equal(9, viewModel.PlanId);
        Assert.Equal("Member One", viewModel.MemberName);
        Assert.Equal("Gold", viewModel.PlanName);
        Assert.Equal(start, viewModel.StartDate);
        Assert.Equal(end, viewModel.EndDate);
    }

    [Fact]
    public void MapsUpdatePlanToPlan_AndBack()
    {
        var viewModel = new UpdatePlanViewModel
        {
            PlanName = "Gold",
            Description = "Gold plan",
            DurationDays = 30,
            Price = 199m,
        };

        var plan = _mapper.Map<Plan>(viewModel);
        var roundTrip = _mapper.Map<UpdatePlanViewModel>(plan);

        Assert.Equal("Gold", plan.Name);
        Assert.Equal("Gold", roundTrip.PlanName);
        Assert.Equal(30, roundTrip.DurationDays);
    }

    [Fact]
    public void MapsMemberUpdate_DoesNotOverwriteDisplayOnlyNameOrPhoto()
    {
        var member = TestData.Member();
        member.Photo = "existing.png";
        var model = new MemberToUpdateViewModel
        {
            Name = "Changed By Request",
            Email = "updated@example.com",
            Phone = "01012345678",
            BuildingNumber = 20,
            Street = "Updated Street",
            City = "Alexandria",
        };

        _mapper.Map(model, member);

        Assert.Equal("Member One", member.Name);
        Assert.Equal("existing.png", member.Photo);
        Assert.Equal("Alexandria", member.Address.City);
    }
}
