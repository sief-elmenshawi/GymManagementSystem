using System.ComponentModel.DataAnnotations;

namespace GymManagement.BLL.Validation;

/// <summary>
/// Rejects decimal values with more fractional digits than the database column can store.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MaxDecimalPlacesAttribute : ValidationAttribute
{
    public MaxDecimalPlacesAttribute(int maxPlaces)
        : base("{0} must have at most " + maxPlaces + " decimal places.")
    {
        if (maxPlaces < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPlaces));
        }

        MaxPlaces = maxPlaces;
    }

    public int MaxPlaces { get; }

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        if (value is not decimal decimalValue)
        {
            return false;
        }

        var scale = (decimal.GetBits(decimalValue)[3] >> 16) & 0x7F;
        return scale <= MaxPlaces;
    }
}
