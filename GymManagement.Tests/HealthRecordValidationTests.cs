using System.ComponentModel.DataAnnotations;
using GymManagement.BLL.ViewModels.MemberViewModels;
using GymManagement.DAL.Data.DbContexts;
using GymManagement.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GymManagement.Tests;

public sealed class HealthRecordValidationTests
{
    [Fact]
    public void FourDecimalPlaces_AreAccepted()
    {
        var model = ValidModel();
        model.Height = 175.1234m;
        model.Weight = 70.1234m;

        Assert.Empty(Validate(model));
    }

    [Theory]
    [InlineData("Height", 175.12345)]
    [InlineData("Weight", 70.12345)]
    public void MoreThanFourDecimalPlaces_AreRejected(string propertyName, double value)
    {
        var model = ValidModel();
        if (propertyName == nameof(HealthRecordViewModel.Height))
        {
            model.Height = (decimal)value;
        }
        else
        {
            model.Weight = (decimal)value;
        }

        var errors = Validate(model);

        Assert.Contains(errors, error =>
            (error.ErrorMessage ?? string.Empty).Contains(propertyName, StringComparison.OrdinalIgnoreCase)
            && (error.ErrorMessage ?? string.Empty).Contains("4 decimal", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EntityConfiguration_UsesDecimal18By4()
    {
        var options = new DbContextOptionsBuilder<GymDbContext>()
            .UseSqlServer("Server=.;Database=ModelOnly;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        using var context = new GymDbContext(options);

        var entity = context.Model.FindEntityType(typeof(HealthRecord));
        Assert.NotNull(entity);
        var entityType = entity!;

        foreach (var propertyName in new[] { nameof(HealthRecord.Height), nameof(HealthRecord.Weight) })
        {
            var property = entityType.FindProperty(propertyName);
            Assert.NotNull(property);
            Assert.Equal(18, property!.GetPrecision());
            Assert.Equal(4, property.GetScale());
        }
    }

    private static HealthRecordViewModel ValidModel() => new()
    {
        Height = 175.5m,
        Weight = 70.5m,
        BloodType = "O+",
    };

    private static List<ValidationResult> Validate(HealthRecordViewModel model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
