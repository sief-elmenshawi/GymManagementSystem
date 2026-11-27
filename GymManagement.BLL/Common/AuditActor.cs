namespace GymManagement.BLL.Common;

/// <summary>
/// Identity of the user who performed a sensitive change. The email is stored as a
/// snapshot so the audit trail remains useful if the account is later renamed or removed.
/// </summary>
public sealed record AuditActor(string? UserId, string Email)
{
    public static AuditActor System => new(null, "system");
}
