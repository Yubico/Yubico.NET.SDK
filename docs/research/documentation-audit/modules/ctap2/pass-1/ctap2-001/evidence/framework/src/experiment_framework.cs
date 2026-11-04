// Pure framework experiment: no SDK reference, test runner or device access.
using System.Formats.Cbor;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

Console.WriteLine($"Runtime={RuntimeInformation.FrameworkDescription}; RID={RuntimeInformation.RuntimeIdentifier}; LittleEndian={BitConverter.IsLittleEndian}");
PrintAssembly(typeof(CborWriter).Assembly);
PrintAssembly(typeof(Guid).Assembly);

var textWriter = new CborWriter(CborConformanceMode.Ctap2Canonical);
textWriter.WriteStartMap(3);
foreach (string key in new[] { "displayName", "id", "name" })
{
    textWriter.WriteTextString(key);
    textWriter.WriteInt32(0);
}
textWriter.WriteEndMap();
byte[] textBytes = textWriter.Encode();
Check(textBytes.AsSpan().SequenceEqual(Convert.FromHexString("A362696400646E616D65006B646973706C61794E616D6500")), "Text map auto-sorted id,name,displayName");
Console.WriteLine($"Text insertion=displayName,id,name; wire={Convert.ToHexString(textBytes)}");

var integerWriter = new CborWriter(CborConformanceMode.Ctap2Canonical);
integerWriter.WriteStartMap(5);
foreach (int key in new[] { -3, -2, -1, 1, 3 })
{
    integerWriter.WriteInt32(key);
    integerWriter.WriteInt32(0);
}
integerWriter.WriteEndMap();
byte[] integerBytes = integerWriter.Encode();
Check(integerBytes.AsSpan().SequenceEqual(Convert.FromHexString("A501000300200021002200")), "Integer map auto-sorted 1,3,-1,-2,-3");
Console.WriteLine($"Integer insertion=-3,-2,-1,1,3; wire={Convert.ToHexString(integerBytes)}");

foreach (CborConformanceMode mode in new[] { CborConformanceMode.Ctap2Canonical, CborConformanceMode.Lax })
{
    foreach (bool multiple in new[] { false, true })
    {
        var reader = multiple ? new CborReader(new byte[] { 0xA0, 0x01 }, mode, true)
            : new CborReader(new byte[] { 0xA0, 0x01 }, mode);
        Check(reader.AllowMultipleRootLevelValues == multiple, "Reader default multiple=false");
        reader.ReadStartMap();
        reader.ReadEndMap();
        Check(reader.BytesRemaining == 1, "ReadEndMap leaves trailing item");
        Console.WriteLine($"Reader mode={mode}; multiple={reader.AllowMultipleRootLevelValues}; ReadEndMap=returned; BytesRemaining={reader.BytesRemaining}; PeekState={reader.PeekState()}");
        if (multiple)
        {
            Check(reader.ReadInt32() == 1 && reader.BytesRemaining == 0, "Multiple=true permits second root");
        }
        else
        {
            try
            {
                reader.ReadInt32();
                throw new Exception("Expected refusal to read second root");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Second root read={ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}

ReadOnlySpan<byte> network = Convert.FromHexString("00112233445566778899AABBCCDDEEFF");
var expectedGuid = new Guid("00112233-4455-6677-8899-aabbccddeeff");
Span<byte> reversed = stackalloc byte[16];
network.CopyTo(reversed);
ReverseComponents(reversed);
var littleBranchGuid = new Guid(reversed);
Check(BitConverter.IsLittleEndian, "Fixture executed only on little-endian host");
Check(littleBranchGuid == expectedGuid && new Guid(network, bigEndian: true) == expectedGuid, "Reversed default constructor agrees with explicit network constructor");
Check(expectedGuid.ToByteArray().AsSpan().SequenceEqual(reversed), "Default ToByteArray is mixed-endian fixture");
Span<byte> defaultWrite = stackalloc byte[16];
Check(expectedGuid.TryWriteBytes(defaultWrite) && defaultWrite.SequenceEqual(reversed), "Default TryWriteBytes agrees with ToByteArray");
ReverseComponents(defaultWrite);
Check(defaultWrite.SequenceEqual(network) && expectedGuid.ToByteArray(bigEndian: true).AsSpan().SequenceEqual(network), "Reversed default write agrees with network output");
Console.WriteLine($"Guid expected={expectedGuid}; default constructor(network)={new Guid(network)}; reversed constructor={littleBranchGuid}");
Console.WriteLine($"Guid ToByteArray={Convert.ToHexString(expectedGuid.ToByteArray())}; explicit bigEndian={Convert.ToHexString(expectedGuid.ToByteArray(true))}");
Console.WriteLine("No-reversal fixture demonstrates default API interpretation only, NOT big-endian platform execution.");
Console.WriteLine("PROBE PASSED");

static void ReverseComponents(Span<byte> bytes)
{
    bytes[..4].Reverse();
    bytes[4..6].Reverse();
    bytes[6..8].Reverse();
}

static void Check(bool condition, string description)
{
    if (!condition) throw new Exception(description);
    Console.WriteLine($"PASS: {description}");
}

static void PrintAssembly(Assembly assembly)
{
    Console.WriteLine($"Assembly={assembly.FullName}; Info={assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}; Path={assembly.Location}; SHA256={Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location)))}");
}
