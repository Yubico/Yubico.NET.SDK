using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;

// Run in a fresh process for each native version. Only the installed resolver supplies SDK imports.
if (args.Length != 3 || !OperatingSystem.IsMacOS() || RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
    throw new ArgumentException("usage on macOS arm64: RuntimeConsumer <dylib> <core-sha256> <native-sha256>");

Assembly core = typeof(CryptographyProviders).Assembly;
string nativePath = Path.GetFullPath(args[0]);
CheckHash(core.Location, args[1]);
CheckHash(nativePath, args[2]);
int resolves = 0;
nint selectedNative = NativeLibrary.Load(nativePath);
NativeLibrary.SetDllImportResolver(core, (name, _, _) =>
{
    if (name != "Yubico.NativeShims")
        throw new InvalidOperationException($"Unexpected native import: {name}");
    resolves++;
    return selectedNative;
});

// Internal OpenSSL primitives have span-only methods. Create typed delegates to the actual SDK
// methods rather than recreating native signatures or pretending the default public AES is native.
Type arkg = core.GetType("Yubico.YubiKit.Core.Cryptography.ArkgPrimitivesOpenSsl", throwOnError: true)!;
object ec = Activator.CreateInstance(arkg, nonPublic: true)!;
var isOnCurve = arkg.GetMethod("IsPointOnCurve")!.CreateDelegate<PointCheck>(ec);
var sharedSecret = arkg.GetMethod("ComputeEcdhSharedSecret")!.CreateDelegate<SharedSecret>(ec);
byte[] generator = Convert.FromHexString(
    "04" + "6B17D1F2E12C4247F8BCE6E563A440F277037D812DEB33A0F4A13945D898C296" +
    "4FE342E2FE1A7F9B8EE7EB4A7C0F9E162BCE33576B315ECECBB6406837BF51F5");
byte[] scalar = new byte[32];
scalar[^1] = 2;
byte[] secret = sharedSecret(scalar, generator);
try
{
    Require(isOnCurve(generator), "P-256 generator rejected");
    Require(Convert.ToHexString(secret) == "7CF27B188D034F7E8A52380304B51AC3C08969E277F21B35A60B48FC47669978",
        "P-256 scalar multiplication did not return the known X coordinate");
}
finally
{
    CryptographicOperations.ZeroMemory(scalar);
    CryptographicOperations.ZeroMemory(secret);
}

Type cmacType = core.GetType("Yubico.YubiKit.Core.Cryptography.CmacPrimitivesOpenSsl", throwOnError: true)!;
using (var cmac = (IDisposable)Activator.CreateInstance(cmacType, nonPublic: true)!)
{
    var init = cmacType.GetMethod("CmacInit")!.CreateDelegate<SpanInput>(cmac);
    var update = cmacType.GetMethod("CmacUpdate")!.CreateDelegate<SpanInput>(cmac);
    var finish = cmacType.GetMethod("CmacFinal")!.CreateDelegate<SpanOutput>(cmac);
    byte[] key = Convert.FromHexString("2B7E151628AED2A6ABF7158809CF4F3C");
    Span<byte> result = stackalloc byte[16];
    try
    {
        init(key);
        update(ReadOnlySpan<byte>.Empty);
        finish(result);
        Require(Convert.ToHexString(result) == "BB1D6929E95937287FA37D129B756746",
            "AES-128 CMAC empty-message known answer mismatch");
    }
    finally
    {
        CryptographicOperations.ZeroMemory(key);
        CryptographicOperations.ZeroMemory(result);
    }
}

Require(resolves > 0, "No SDK native imports resolved through the selected library");
Console.WriteLine($"PASS Core SHA-256 {args[1].ToUpperInvariant()}, native SHA-256 {args[2].ToUpperInvariant()}: " +
    $"SDK P-256/BN and CMAC vectors; native resolver calls {resolves}");

static void CheckHash(string path, string expected)
{
    string actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    Require(actual.Equals(expected, StringComparison.OrdinalIgnoreCase), $"SHA-256 mismatch for {path}: {actual}");
}

static void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

delegate bool PointCheck(ReadOnlySpan<byte> point);
delegate byte[] SharedSecret(ReadOnlySpan<byte> scalar, ReadOnlySpan<byte> point);
delegate void SpanInput(ReadOnlySpan<byte> bytes);
delegate void SpanOutput(Span<byte> bytes);
