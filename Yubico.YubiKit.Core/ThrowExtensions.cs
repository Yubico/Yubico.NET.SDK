using System.Reflection;

namespace Yubico.YubiKit.Core;

public static class ThrowHelper
{
    public static void IfNull<T>(this T obj, string paramName) where T : class
    {
        if (obj is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }

    public static void ThrowIf<TException>(bool condition, string message) where TException : Exception
    {
        if (!condition)
        {
            return;
        }

        var formattedMessage = DefaultTextFormatter.Format(message);
        ConstructorInfo? constructor = typeof(TException).GetConstructor(new[] { typeof(string) }) ?? throw new InvalidOperationException($"No constructor found on {typeof(TException)} that takes a single string parameter.");
        throw (TException)constructor.Invoke(new object[] { formattedMessage })!;
    }

}

public static class DefaultTextFormatter
{
    public static string Format(string text) => string.Format(System.Globalization.CultureInfo.CurrentCulture, text);
}