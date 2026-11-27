namespace GymManagement.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string From { get; set; } = "no-reply@gymmanagement.local";

    public SmtpOptions Smtp { get; set; } = new();
}

public sealed class SmtpOptions
{
    public string? Host { get; set; }

    public int Port { get; set; } = 587;

    public bool UseSsl { get; set; } = true;

    public string? User { get; set; }

    public string? Password { get; set; }
}
