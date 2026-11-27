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
    public void MapsMember_FormatsAddressAndDateOfBirth()
    {
        var member = TestData.Member();

        var viewModel = _mapper.Map<MemberViewModel>(member);

        Assert.Equal("12 - Nile Street - Cairo", viewModel.Address);
        Assert.Equal(member.DateOfBirth.ToShortDateString(), viewModel.DateOfBirth);
        Assert.Equal(member.Name, viewModel.Name);
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
