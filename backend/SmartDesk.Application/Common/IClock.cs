namespace SmartDesk.Application.Common;

/// <summary>
/// Injected time source. SLA deadlines, escalation windows and timeouts all depend on "now",
/// so tests need to control it. Production uses SystemClock.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
