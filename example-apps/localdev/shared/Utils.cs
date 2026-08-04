namespace SimpleOpenTelemetry.Examples.Shared;

public static class Utils
{
    public static string SafeFormat(string message, object?[]? args)
    {
        if (args is null || args.Length == 0)
            return message;

        try
        {
            return string.Format(message, args);
        }
        catch (FormatException)
        {
            // Last resort: just join them manually
            return message + " | " + string.Join(", ", args.Select(arg => arg?.ToString() ?? "<null>"));
        }
    }
}
