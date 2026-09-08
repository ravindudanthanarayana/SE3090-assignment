namespace SmartDesk.Application.Common;

/// <summary>Base for exceptions that the global handler maps to a specific HTTP status code.</summary>
public abstract class AppException(string message) : Exception(message);

/// <summary>400 - request failed server-side validation.</summary>
public sealed class ValidationException(string message) : AppException(message);

/// <summary>404 - the resource does not exist (or the caller may not know that it does).</summary>
public sealed class NotFoundException(string entity, object key)
    : AppException($"{entity} '{key}' was not found.");

/// <summary>403 - authenticated and correctly-rolled, but not permitted on this specific resource.</summary>
public sealed class ForbiddenException(string message) : AppException(message);

/// <summary>409 - the operation is not legal in the entity's current state.</summary>
public sealed class ConflictException(string message) : AppException(message);

/// <summary>422 - an agent produced output that failed deterministic validation.</summary>
public sealed class AgentValidationException(string message) : AppException(message);

/// <summary>503 - a downstream service (LLM or notification provider) is unavailable.</summary>
public sealed class DownstreamUnavailableException(string message) : AppException(message);
