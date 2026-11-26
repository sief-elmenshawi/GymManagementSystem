using AutoMapper;
using GymManagement.BLL.MappingProfile;
using Microsoft.Extensions.Logging.Abstractions;

namespace GymManagement.Tests.Support;

internal static class TestMapper
{
    public static IMapper Create()
    {
        var configuration = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance);

        return configuration.CreateMapper();
    }
}
