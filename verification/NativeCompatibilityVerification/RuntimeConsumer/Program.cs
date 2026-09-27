using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;

// Run in a fresh process for each native version. Only the installed resolver supplies SDK imports.
// Optional 4th argument --control-release-failure is a negative control for the context cleanup branch.
bool releaseFailureControl = args is [_, _, _, "--control-release-failure"];
if ((args.Length != 3 && !releaseFailureControl) || !OperatingSystem.IsMacOS() ||
    RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
    throw new ArgumentException(
        "usage on macOS arm64: RuntimeConsumer <dylib> <core-sha256> <native-sha256> [--control-release-failure]");

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

int cryptoResolves = resolves;
SmartCardErrorStage(core, releaseFailureControl);

Require(resolves > cryptoResolves, "No SDK SCard imports resolved through the selected library");
Console.WriteLine($"PASS Core SHA-256 {args[1].ToUpperInvariant()}, native SHA-256 {args[2].ToUpperInvariant()}: " +
    $"SDK P-256/BN and CMAC vectors; SCard context/error stage; native resolver calls {resolves} " +
    $"({cryptoResolves} crypto, {resolves - cryptoResolves} SCard)");

// Hardware-free PC/SC stage through Core's own LibraryImport methods (invoked by reflection because
// the methods and their SafeHandle/enum parameter types are internal). It establishes and releases one
// user-scope context, passes a deliberately invalid context handle to SCardCancel, and asks SCardConnect
// for a reader name that cannot exist. No card or reader is opened. Error codes are recorded, not assumed.
// With releaseFailureControl the context is released once beforehand, so the explicit release must fail,
// leave the SafeHandle valid for its disposal fallback, and fail the run.
static void SmartCardErrorStage(Assembly core, bool releaseFailureControl)
{
    const string ns = "Yubico.YubiKit.Core.Native.Desktop.SCard.";
    Type native = core.GetType(ns + "NativeMethods", throwOnError: true)!;
    Type contextType = core.GetType(ns + "SCardContext", throwOnError: true)!;
    Type errors = core.GetType(ns + "ErrorCode", throwOnError: true)!;

    object?[] establish = [Enum.ToObject(core.GetType(ns + "SCARD_SCOPE", throwOnError: true)!, 0), null];
    uint code = Call(native, "SCardEstablishContext", establish);
    Console.WriteLine($"SCardEstablishContext(USER) -> {Describe(errors, code)}");
    using var context = (SafeHandle)establish[1]!;
    Require(code == 0 && !context.IsInvalid, "SCardEstablishContext did not return a usable context");

    try
    {
        InvalidContextCancel(native, contextType, errors, context.DangerousGetHandle());
        NonexistentReaderConnect(core, native, errors, context);
    }
    finally
    {
        // Raw release: deliberately bypasses ReleaseContext so the SafeHandle stays open for the check.
        if (releaseFailureControl)
            Console.WriteLine("CONTROL pre-release -> " +
                Describe(errors, Call(native, "SCardReleaseContext", [context.DangerousGetHandle()])));
        code = ReleaseContext(native, context);
        Console.WriteLine($"SCardReleaseContext(established) -> {Describe(errors, code)}");
    }

    // A failed explicit release leaves the handle valid, so `using` disposal retries through ReleaseHandle.
    Require(code == 0, $"SCardReleaseContext failed for the established context; " +
        $"SafeHandle left for disposal fallback (IsInvalid={context.IsInvalid}, IsClosed={context.IsClosed})");
}

// Release explicitly to observe the code. Only a successful release suppresses the SafeHandle's own release.
static uint ReleaseContext(Type native, SafeHandle context)
{
    uint code = Call(native, "SCardReleaseContext", [context.DangerousGetHandle()]);
    if (code == 0)
        context.SetHandleAsInvalid();
    return code;
}

static void InvalidContextCancel(Type native, Type contextType, Type errors, nint established)
{
    nint bogus = 0x5A5A5A5A;
    Require(bogus != established, "Deliberately invalid handle collided with the established context");
    using var invalid = (SafeHandle)Activator.CreateInstance(contextType, [bogus])!;
    try
    {
        uint code = Call(native, "SCardCancel", [invalid]);
        Console.WriteLine($"SCardCancel(invalid context 0x{bogus:X}) -> {Describe(errors, code)}");
        Require(code != 0, "SCardCancel accepted a deliberately invalid context");
    }
    finally
    {
        // Never pass the bogus value to SCardReleaseContext from the finalizer/dispose path.
        invalid.SetHandleAsInvalid();
    }
}

static void NonexistentReaderConnect(Assembly core, Type native, Type errors, SafeHandle context)
{
    const string ns = "Yubico.YubiKit.Core.Native.Desktop.SCard.";
    const string reader = "YubiKit Verifier Nonexistent Reader 7d3f0c1e";
    object share = Enum.ToObject(core.GetType(ns + "SCARD_SHARE", throwOnError: true)!, 2);
    object protocols = Enum.ToObject(core.GetType(ns + "SCARD_PROTOCOL", throwOnError: true)!, 3);
    object?[] connect = [context, reader, share, protocols, null, null];
    uint code = Call(native, "SCardConnect", connect);
    using var card = (SafeHandle)connect[4]!;
    Console.WriteLine($"SCardConnect(\"{reader}\", SHARED, T0|T1) -> {Describe(errors, code)}; " +
        $"card handle invalid={card.IsInvalid}");
    // Any returned valid handle is left to `using` disposal (SCardDisconnect LEAVE_CARD), never suppressed.
    Require(code != 0, "SCardConnect unexpectedly opened the nonexistent reader");
    Require(card.IsInvalid, $"SCardConnect failed ({Describe(errors, code)}) but returned a valid card handle");
}

static uint Call(Type native, string name, object?[] arguments) =>
    (uint)native.GetMethod(name, BindingFlags.Public | BindingFlags.Static)!.Invoke(null, arguments)!;

static string Describe(Type errors, uint code)
{
    string[] names = errors.GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.IsLiteral && field.GetRawConstantValue() is uint value && value == code)
        .Select(field => field.Name)
        .ToArray();
    return $"0x{code:X8} ({(names.Length == 0 ? "unnamed" : string.Join("/", names))})";
}

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
