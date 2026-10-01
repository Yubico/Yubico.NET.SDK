using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

// macOS arm64 export and smoke check. This does not load a historical managed Core assembly.
if (args.Length != 3)
{
    Console.Error.WriteLine("usage: NativeCompatibilityVerification <baseline-exports.json> <1.18.0 dylib> <async.8 dylib>");
    return 2;
}

if (!OperatingSystem.IsMacOS() || RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
    throw new PlatformNotSupportedException("This verification requires macOS arm64");

string[] baseline = JsonSerializer.Deserialize<string[]>(File.ReadAllText(args[0]))
    ?? throw new InvalidOperationException("Baseline exports missing");
if (baseline.Length != 36 || baseline.Distinct(StringComparer.Ordinal).Count() != 36)
    throw new InvalidOperationException("Expected 36 distinct baseline exports");

string oldPath = Path.GetFullPath(args[1]);
string newPath = Path.GetFullPath(args[2]);
string oldHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(oldPath)));
string newHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(newPath)));
Require(oldHash == "96FEAAB51E8D8079D1BD0109B0479C750BE09FAA008361DC4F46DC1642009EAC",
    $"1.18.0 macOS arm64 binary identity mismatch: {oldHash}");
Require(newHash == "65499E77151D483D8E04BD793C2D520B4F344478FFCACFF91BFF8E771AE9D8AF",
    $"1.18.1-async.8 macOS arm64 binary identity mismatch: {newHash}");
Console.WriteLine($"1.18.0 dylib SHA-256: {oldHash}");
Console.WriteLine($"1.18.1-async.8 dylib SHA-256: {newHash}");

string[] bridge = ["Native_HidInputCreate", "Native_HidInputStart", "Native_HidInputCancel",
    "Native_HidInputWaitShutdown", "Native_HidInputDestroy"];
VerifyLibrary(oldPath, "1.18.0", baseline, bridge, expectInputOwner: false);
VerifyLibrary(newPath, "1.18.1-async.8", baseline, bridge, expectInputOwner: true);
return 0;

static void VerifyLibrary(string path, string version, string[] baseline, string[] bridge, bool expectInputOwner)
{
    nint library = NativeLibrary.Load(path);
    try
    {
        HashSet<string> legacy = ReadExports(library, baseline);
        Require(legacy.Count == baseline.Length,
            $"legacy contract + native {version}: missing {string.Join(", ", baseline.Except(legacy))}");
        ExerciseBignum(library);
        Console.WriteLine($"PASS legacy contract + native {version} (36/36 exports, BIGNUM allocation/free)");

        HashSet<string> inputOwner = ReadExports(library, bridge);
        if (expectInputOwner)
        {
            Require(inputOwner.Count == bridge.Length,
                $"input-owner contract + native {version}: missing {string.Join(", ", bridge.Except(inputOwner))}");
            Console.WriteLine("PASS input-owner contract + native async.8 (5/5 input exports; signatures and runtime behavior not exercised)");
        }
        else
        {
            Require(inputOwner.Count == 0,
                $"native {version} unexpectedly contains input bridge: {string.Join(", ", inputOwner)}");
            // Never invoke missing input-owner exports: this row is unsupported.
            Console.WriteLine("UNSUPPORTED input-owner contract + native 1.18.0: 0/5 input exports; no input-owner calls made");
        }
    }
    finally
    {
        NativeLibrary.Free(library);
    }
}

static HashSet<string> ReadExports(nint library, IEnumerable<string> names) =>
    names.Where(name => NativeLibrary.TryGetExport(library, name, out _)).ToHashSet(StringComparer.Ordinal);

static unsafe void ExerciseBignum(nint library)
{
    var create = (delegate* unmanaged[Cdecl]<nint>)NativeLibrary.GetExport(library, "Native_BN_new");
    var free = (delegate* unmanaged[Cdecl]<nint, void>)NativeLibrary.GetExport(library, "Native_BN_clear_free");
    nint value = create();
    Require(value != 0, "Native_BN_new returned null");
    free(value);
}

static void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
