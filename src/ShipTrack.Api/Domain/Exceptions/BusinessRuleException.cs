namespace ShipTrack.Api.Domain.Exceptions;

public class BusinessRuleException : Exception
{
    public int StatusCode { get; }

    public BusinessRuleException(string message, int statusCode = StatusCodes.Status409Conflict)
        : base(message)
    {
        StatusCode = statusCode;
    }
}