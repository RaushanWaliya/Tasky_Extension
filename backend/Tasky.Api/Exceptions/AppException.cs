namespace Tasky.Api.Exceptions;

/// <summary>Base class for errors we expect and want to show to the user with a proper HTTP status.</summary>
public abstract class AppException : Exception
{
    protected AppException(int statusCode, string code, string message) : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }

    public int StatusCode { get; }
    public string Code { get; }
}

/// <summary>404 - the requested API source does not exist (or is switched off).</summary>
public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(StatusCodes.Status404NotFound, "not_found", message)
    {
    }
}

/// <summary>400 - the user typed something that does not match the input rules.</summary>
public class InputValidationException : AppException
{
    public InputValidationException(IDictionary<string, string> errors)
        : base(StatusCodes.Status400BadRequest, "invalid_input", "One or more inputs are invalid.")
    {
        Errors = errors;
    }

    public IDictionary<string, string> Errors { get; }
}

/// <summary>502 - the external API failed, timed out, or returned an error.</summary>
public class UpstreamException : AppException
{
    public UpstreamException(string message, int upstreamStatus = 0)
        : base(StatusCodes.Status502BadGateway, "upstream_error", message)
    {
        UpstreamStatus = upstreamStatus;
    }

    public int UpstreamStatus { get; }
}
