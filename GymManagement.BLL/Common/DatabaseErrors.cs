using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.BLL.Common;

/// <summary>
/// Distinguishes a genuine unique-key race from other database failures so callers
/// can keep a friendly conflict message without hiding real errors.
/// </summary>
public static class DatabaseErrors
{
    public static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception.InnerException is SqlException sql && sql.Number is 2601 or 2627)
        {
            return true;
        }

        // Keeps unit tests with lightweight inner exceptions meaningful and also
        // covers providers whose message is available without a SqlException type.
        var message = $"{exception.Message} {exception.InnerException?.Message}";
        return message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            || message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase)
            || message.Contains("unique key", StringComparison.OrdinalIgnoreCase)
            || message.Contains("UX_Bookings", StringComparison.OrdinalIgnoreCase);
    }
}
