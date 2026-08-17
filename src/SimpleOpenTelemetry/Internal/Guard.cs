using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SimpleOpenTelemetry.Internal;

internal static class Guard
{
#if NETSTANDARD2_0
    public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }
#else
    public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        => ArgumentNullException.ThrowIfNull(argument, paramName);
#endif
}
