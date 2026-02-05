namespace AIRA.Agents.Tools.Exceptions;

/// <summary>
/// Base exception for tool-related errors
/// </summary>
public class ToolException : Exception
{
    public string ToolName { get; }
    public string ErrorCode { get; }

    public ToolException(string toolName, string errorCode, string message) 
        : base(message)
    {
        ToolName = toolName;
        ErrorCode = errorCode;
    }

    public ToolException(string toolName, string errorCode, string message, Exception innerException) 
        : base(message, innerException)
    {
        ToolName = toolName;
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Exception thrown when API authentication fails
/// </summary>
public class ApiAuthenticationException : ToolException
{
    public ApiAuthenticationException(string toolName, string message) 
        : base(toolName, "AUTH_ERROR", message)
    {
    }
}

/// <summary>
/// Exception thrown when API rate limit is exceeded
/// </summary>
public class ApiRateLimitException : ToolException
{
    public int? RetryAfterSeconds { get; }

    public ApiRateLimitException(string toolName, string message, int? retryAfterSeconds = null) 
        : base(toolName, "RATE_LIMIT", message)
    {
        RetryAfterSeconds = retryAfterSeconds;
    }
}

/// <summary>
/// Exception thrown when API returns invalid data
/// </summary>
public class ApiDataException : ToolException
{
    public ApiDataException(string toolName, string message) 
        : base(toolName, "INVALID_DATA", message)
    {
    }

    public ApiDataException(string toolName, string message, Exception innerException) 
        : base(toolName, "INVALID_DATA", message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when API request times out
/// </summary>
public class ApiTimeoutException : ToolException
{
    public ApiTimeoutException(string toolName, string message) 
        : base(toolName, "TIMEOUT", message)
    {
    }

    public ApiTimeoutException(string toolName, string message, Exception innerException) 
        : base(toolName, "TIMEOUT", message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : Exception
{
    public string ParameterName { get; }
    public string ValidationError { get; }

    public ValidationException(string parameterName, string validationError) 
        : base($"Validation failed for '{parameterName}': {validationError}")
    {
        ParameterName = parameterName;
        ValidationError = validationError;
    }
}
