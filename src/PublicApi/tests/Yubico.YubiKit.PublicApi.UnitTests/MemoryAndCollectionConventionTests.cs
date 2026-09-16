using System.Reflection;

namespace Yubico.YubiKit.PublicApi.UnitTests;

public sealed class MemoryAndCollectionConventionTests
{
    private static readonly IReadOnlyDictionary<string, string> ReviewedSecretArrayReturns = new Dictionary<string, string>
    {
        ["Yubico.YubiKit.Oath.OathSession.DeriveKey(System.ReadOnlyMemory<System.Byte>): System.Byte[]"] =
            "Existing password-derived secret contract transfers an owned array that callers must clear.",
        ["Yubico.YubiKit.Oath.IOathSession.DeriveKey(System.ReadOnlyMemory<System.Byte>): System.Byte[]"] =
            "Existing password-derived secret contract transfers an owned array that callers must clear."
    };

    [Fact]
    public void PublicSessionOperations_DoNotBorrowRawArraysOrAddUnreviewedSecretArrayReturns()
    {
        var violations = new List<string>();
        var observedSecretArrayReturns = new HashSet<string>();

        foreach (var (session, contract, _) in AppletSessionShapeTests.Sessions)
        {
            foreach (var method in session.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                ValidateMemoryShape(method, violations, observedSecretArrayReturns);

            foreach (var method in contract.GetMethods())
                ValidateMemoryShape(method, violations, observedSecretArrayReturns);

            foreach (var method in AppletSessionShapeTests.GetDeviceExtensionMethods(session))
                ValidateMemoryShape(method, violations, observedSecretArrayReturns);
        }

        violations.AddRange(ReviewedSecretArrayReturns.Keys
            .Except(observedSecretArrayReturns)
            .Select(static signature => $"{signature} has a stale secret-array exception"));

        Assert.Empty(violations);
    }

    private static void ValidateMemoryShape(
        MethodInfo method,
        ICollection<string> violations,
        ISet<string> observedSecretArrayReturns)
    {
        if (method.GetParameters().Any(static p => p.ParameterType == typeof(byte[])))
            violations.Add($"{PublicReturnContractScanner.FormatSignature(method)} borrows byte[]");

        Type resultType = PublicReturnContractScanner.UnwrapAsync(method.ReturnType);

        if (resultType != typeof(byte[]))
            return;

        string signature = PublicReturnContractScanner.FormatSignature(method);
        observedSecretArrayReturns.Add(signature);
        if (!ReviewedSecretArrayReturns.ContainsKey(signature))
            violations.Add($"{signature} returns an unreviewed secret byte array");
    }
}