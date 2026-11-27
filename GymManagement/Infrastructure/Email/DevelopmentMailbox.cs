namespace GymManagement.Infrastructure.Email;

/// <summary>
/// In-memory mailbox used only in Development so account emails can be tested
/// without configuring SMTP or committing credentials.
/// </summary>
public sealed class DevelopmentMailbox
{
    private readonly object _sync = new();
    private readonly List<DevelopmentEmailMessage> _messages = new();

    public void Add(DevelopmentEmailMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        lock (_sync)
        {
            _messages.Add(message);
        }
    }

    public IReadOnlyList<DevelopmentEmailMessage> GetByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Array.Empty<DevelopmentEmailMessage>();
        }

        lock (_sync)
        {
            return _messages
                .Where(message => string.Equals(
                    message.To,
                    email,
                    StringComparison.OrdinalIgnoreCase))
                .OrderBy(message => message.SentAt)
                .ToArray();
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            _messages.Clear();
        }
    }
}

public sealed record DevelopmentEmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    DateTimeOffset SentAt);
