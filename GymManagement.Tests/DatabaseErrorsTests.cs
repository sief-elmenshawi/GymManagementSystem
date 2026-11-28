using GymManagement.BLL.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GymManagement.Tests;

public sealed class DatabaseErrorsTests
{
    [Theory]
    [InlineData("Duplicate key")]
    [InlineData("Violation of UNIQUE KEY constraint")]
    public void IsUniqueConstraintViolation_RecognizesUniqueKeyMessages(string message)
    {
        var exception = new DbUpdateException(message, new Exception(message));

        Assert.True(DatabaseErrors.IsUniqueConstraintViolation(exception));
    }

    [Fact]
    public void IsUniqueConstraintViolation_ReturnsFalse_ForOtherDatabaseFailures()
    {
        var exception = new DbUpdateException("Connection reset by peer");

        Assert.False(DatabaseErrors.IsUniqueConstraintViolation(exception));
    }
}
