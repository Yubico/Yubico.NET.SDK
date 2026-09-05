using System.Reflection;

namespace Yubico.YubiKit.PublicApi.UnitTests;

public sealed class AsyncSurfaceConventionTests
{
    private static readonly HashSet<string> SynchronousAllowlist =
    [
        "ManagementSession.IsSupported", "ManagementSession.EnsureSupports",
        "PivSession.IsSupported", "PivSession.EnsureSupports",
        "FidoSession.IsSupported", "FidoSession.EnsureSupports",
        "OathSession.IsSupported", "OathSession.EnsureSupports", "OathSession.DeriveKey",
        "OpenPgpSession.IsSupported", "OpenPgpSession.EnsureSupports",
        "SecurityDomainSession.IsSupported", "SecurityDomainSession.EnsureSupports",
        "YubiOtpSession.IsSupported", "YubiOtpSession.EnsureSupports", "YubiOtpSession.GetConfigState",
        "HsmAuthSession.IsSupported", "HsmAuthSession.EnsureSupports"
    ];

    [Fact]
    public void PublicSessionOperations_UseTaskAsyncSuffixAndFinalDefaultedCancellationToken()
    {
        var violations = new List<string>();

        foreach (var (session, _, _) in AppletSessionShapeTests.Sessions)
        {
            foreach (var method in session.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                ValidateOperation(method, session.Name, violations);

            foreach (var method in AppletSessionShapeTests.GetDeviceExtensionMethods(session))
                ValidateOperation(method, method.DeclaringType?.Name ?? session.Name, violations);
        }

        Assert.Empty(violations);
    }

    private static void ValidateOperation(MethodInfo method, string typeName, ICollection<string> violations)
    {
        if (method.IsSpecialName || method.Name is nameof(IDisposable.Dispose) or nameof(IAsyncDisposable.DisposeAsync))
            return;

        string member = $"{typeName}.{method.Name}";
        if (SynchronousAllowlist.Contains(member))
            return;

        if (!method.Name.EndsWith("Async", StringComparison.Ordinal))
            violations.Add($"{member} lacks Async suffix");

        if (method.ReturnType != typeof(Task) &&
            !(method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)))
            violations.Add($"{member} returns {method.ReturnType.Name}, not Task");

        var parameters = method.GetParameters();
        if (parameters.Length == 0 || parameters[^1].ParameterType != typeof(CancellationToken) ||
            !parameters[^1].HasDefaultValue)
            violations.Add($"{member} lacks a final defaulted CancellationToken");
    }
}
