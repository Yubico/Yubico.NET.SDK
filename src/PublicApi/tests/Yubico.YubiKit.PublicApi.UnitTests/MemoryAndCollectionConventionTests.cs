using System.Collections;
using System.Reflection;

namespace Yubico.YubiKit.PublicApi.UnitTests;

public sealed class MemoryAndCollectionConventionTests
{
    private static readonly HashSet<string> MutableCollectionReturnAllowlist =
    [
        "OathSession.DeriveKey",
        "IOathSession.DeriveKey"
    ];

    [Fact]
    public void PublicSessionOperations_DoNotBorrowRawArraysOrReturnMutableCollections()
    {
        var violations = new List<string>();

        foreach (var (session, _, _) in AppletSessionShapeTests.Sessions)
        {
            foreach (var method in session.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                ValidateMemoryAndCollectionShape(method, session.Name, violations);

            foreach (var method in AppletSessionShapeTests.GetDeviceExtensionMethods(session))
                ValidateMemoryAndCollectionShape(method, method.DeclaringType?.Name ?? session.Name, violations);
        }

        Assert.Empty(violations);
    }

    private static void ValidateMemoryAndCollectionShape(
        MethodInfo method,
        string typeName,
        ICollection<string> violations)
    {
        if (method.GetParameters().Any(static p => p.ParameterType == typeof(byte[])))
            violations.Add($"{typeName}.{method.Name} borrows byte[]");

        Type resultType = method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)
            ? method.ReturnType.GetGenericArguments()[0]
            : method.ReturnType;

        string member = $"{typeName}.{method.Name}";
        if (!MutableCollectionReturnAllowlist.Contains(member) && IsMutableCollectionContract(resultType))
            violations.Add($"{typeName}.{method.Name} returns mutable {resultType.Name}");
    }

    private static bool IsMutableCollectionContract(Type type) =>
        type.IsArray ||
        (type.IsGenericType && type.GetGenericTypeDefinition() is { } definition &&
            (definition == typeof(List<>) || definition == typeof(Dictionary<,>) || definition == typeof(IList<>) ||
             definition == typeof(IDictionary<,>) || definition == typeof(ICollection<>))) ||
        type == typeof(IList) || type == typeof(IDictionary) || type == typeof(ICollection);
}