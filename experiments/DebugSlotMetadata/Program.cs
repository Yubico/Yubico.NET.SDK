// Debug: Inspect slot metadata and public key format from YubiKey PIV
// Purpose: Understand why ImportSubjectPublicKeyInfo fails with "ASN1 corrupted data"

using System.Security.Cryptography;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Piv;

Console.WriteLine("=== PIV Slot Metadata Debug ===\n");

// Find YubiKey
var manager = new YubiKeyManager();
var devices = await manager.FindAllAsync();

if (!devices.Any())
{
    Console.WriteLine("No YubiKey found!");
    return;
}

var device = devices.First();
Console.WriteLine($"Found device: {device.DeviceId} ({device.ConnectionType})");

await using var session = await device.CreatePivSessionAsync();

// Check slot 9A (Authentication)
var slot = PivSlot.Authentication;
Console.WriteLine($"\n--- Slot 0x{(byte)slot:X2} ({slot}) ---");

var metadata = await session.GetSlotMetadataAsync(slot);

if (metadata is null)
{
    Console.WriteLine("Slot is empty (metadata is null)");
    return;
}

var m = metadata.Value;
Console.WriteLine($"Algorithm: {m.Algorithm}");
Console.WriteLine($"PIN Policy: {m.PinPolicy}");
Console.WriteLine($"Touch Policy: {m.TouchPolicy}");
Console.WriteLine($"Is Generated: {m.IsGenerated}");
Console.WriteLine($"Public Key Length: {m.PublicKey.Length} bytes");
Console.WriteLine($"Public Key Empty: {m.PublicKey.IsEmpty}");

if (!m.PublicKey.IsEmpty)
{
    Console.WriteLine($"\nPublic Key Hex ({m.PublicKey.Length} bytes):");
    Console.WriteLine(Convert.ToHexString(m.PublicKey.Span));
    
    // Try to parse the first few bytes to understand the format
    var pk = m.PublicKey.Span;
    Console.WriteLine($"\nFirst bytes: {Convert.ToHexString(pk[..Math.Min(16, pk.Length)])}");
    
    if (pk.Length > 0)
    {
        Console.WriteLine($"First byte: 0x{pk[0]:X2}");
        // 0x30 = SEQUENCE (expected for SubjectPublicKeyInfo)
        // 0x04 = OCTET STRING (might be raw point)
        // 0x7F/0x49 = PIV specific tags
        
        if (pk[0] == 0x30)
            Console.WriteLine("  -> Looks like ASN.1 SEQUENCE (SubjectPublicKeyInfo?)");
        else if (pk[0] == 0x04)
            Console.WriteLine("  -> Looks like uncompressed EC point (04 || X || Y)");
        else if (pk[0] == 0x7F)
            Console.WriteLine("  -> Looks like PIV TLV format (0x7F49 = public key template)");
        else
            Console.WriteLine("  -> Unknown format");
    }
    
    // Try ImportSubjectPublicKeyInfo
    Console.WriteLine("\n--- Attempting ImportSubjectPublicKeyInfo ---");
    try
    {
        if (m.Algorithm.IsRsa())
        {
            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(m.PublicKey.Span, out int bytesRead);
            Console.WriteLine($"✓ RSA import succeeded! Bytes read: {bytesRead}");
            Console.WriteLine($"  Key size: {rsa.KeySize} bits");
        }
        else if (m.Algorithm.IsEcc())
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(m.PublicKey.Span, out int bytesRead);
            Console.WriteLine($"✓ ECDSA import succeeded! Bytes read: {bytesRead}");
            Console.WriteLine($"  Key size: {ecdsa.KeySize} bits");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Import failed: {ex.Message}");
        
        // Try alternative: raw EC point import for ECC
        if (m.Algorithm.IsEcc() && m.PublicKey.Span[0] == 0x04)
        {
            Console.WriteLine("\n--- Trying ECParameters with raw point ---");
            try
            {
                var curve = m.Algorithm == PivAlgorithm.EccP256 
                    ? ECCurve.NamedCurves.nistP256 
                    : ECCurve.NamedCurves.nistP384;
                
                var coordSize = m.Algorithm == PivAlgorithm.EccP256 ? 32 : 48;
                var point = m.PublicKey.Span;
                
                // Skip 0x04 prefix
                var x = point.Slice(1, coordSize).ToArray();
                var y = point.Slice(1 + coordSize, coordSize).ToArray();
                
                var ecParams = new ECParameters
                {
                    Curve = curve,
                    Q = new ECPoint { X = x, Y = y }
                };
                
                using var ecdsa = ECDsa.Create(ecParams);
                Console.WriteLine($"✓ ECParameters import succeeded!");
                Console.WriteLine($"  Key size: {ecdsa.KeySize} bits");
            }
            catch (Exception ex2)
            {
                Console.WriteLine($"✗ ECParameters import also failed: {ex2.Message}");
            }
        }
    }
}

Console.WriteLine("\n=== Done ===");
