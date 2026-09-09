namespace SmartDesk.Application.Agents;

/// <summary>Bounded execution limits for the agent subsystem (spec section 9.10: timeouts and retry limits).</summary>
public sealed class AgentOptions
{
    public const string SectionName = "Agents";

    /// <summary>
    /// Per-LLM-call timeout. Sized for a real hosted model: a reasoning-capable model can take
    /// 20-30 s for a single structured response, so a tight timeout turns normal latency into a
    /// spurious failure.
    /// </summary>
    public int LlmTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Whole-workflow budget. Exceeding it is a safe, recorded failure rather than a hang.
    /// Five agents plus retries against a live provider need real headroom - an earlier 120 s
    /// budget caused the workflow to time out legitimately when the provider was rate limiting.
    /// </summary>
    public int WorkflowTimeoutSeconds { get; set; } = 300;

    /// <summary>Retries per agent step after the first attempt.</summary>
    public int MaxRetriesPerStep { get; set; } = 2;

    /// <summary>Base backoff between retries, in milliseconds. Doubles each attempt.</summary>
    public int RetryBackoffMs { get; set; } = 500;
}
