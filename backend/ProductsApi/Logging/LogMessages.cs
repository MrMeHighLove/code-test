namespace ProductsApi.Logging;

public static partial class LogMessages
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Auth event {EventType} outcome {Outcome} subject {SubjectId}.")]
    public static partial void AuthEvent(ILogger logger, string eventType, string outcome, string? subjectId);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Returning 304 for products query with colour filter {ColourFilter}.")]
    public static partial void ProductsNotModified(ILogger logger, string? colourFilter);
}
