// Quick probe: test FIDO2 AID selection over USB CCID (SmartCard) on 5.8+ keys
// Tests: plain CCID, SCP03, and SCP11b — all over USB SmartCard
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.Core.Devices.SmartCard;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.DeviceExtensions;
using Yubico.YubiKey.Fido2;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Scp;

namespace Yubico.YubiKey.TestApp.Plugins
{
    internal class Fido2CcidProbePlugin : PluginBase
    {
        public override string Name => "Fido2CcidProbe";
        public override string Description => "Probes FIDO2 over USB CCID (SmartCard) on 5.8+ keys — SCP03 and SCP11b";

        public Fido2CcidProbePlugin(IOutput output) : base(output)
        {
            Parameters["command"].Description = "[serial] Serial number of the YubiKey to test (e.g. 125)";
        }

        public override bool Execute()
        {
            int? targetSerial = string.IsNullOrEmpty(Command) ? null : int.Parse(Command);

            Output.WriteLine("=== FIDO2 over USB CCID Probe (SCP03 + SCP11b) ===");
            Output.WriteLine();

            var allKeys = YubiKeyDevice.FindAll();
            Output.WriteLine($"Found {allKeys.Count()} YubiKey(s) total");

            foreach (var key in allKeys)
            {
                Output.WriteLine($"  Serial: {key.SerialNumber}, FW: {key.FirmwareVersion}, Transports: {key.AvailableTransports}");
                Output.WriteLine($"    USB Capabilities: {key.AvailableUsbCapabilities}");
                Output.WriteLine($"    HasSmartCard: {((YubiKeyDevice)key).HasSmartCard}, IsNfc: {((YubiKeyDevice)key).IsNfcDevice}");
            }

            var targetKey = allKeys.FirstOrDefault(k =>
                targetSerial == null || k.SerialNumber == targetSerial);

            if (targetKey == null)
            {
                Output.WriteLine($"No YubiKey found{(targetSerial.HasValue ? $" with serial {targetSerial}" : "")}");
                return false;
            }

            Output.WriteLine();
            Output.WriteLine($"--- Target: Serial={targetKey.SerialNumber}, FW={targetKey.FirmwareVersion} ---");

            var device = (YubiKeyDevice)targetKey;
            Output.WriteLine($"FIDO2 in AvailableUsbCapabilities: {targetKey.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Fido2)}");

            // ---- Test 1: Standard HID path ----
            RunTest("Test 1: Standard Connect(Fido2) — HID path", () =>
            {
                using var conn = targetKey.Connect(YubiKeyApplication.Fido2);
                Output.WriteLine($"  Connection type: {conn.GetType().Name}");
                var info = conn.SendCommand(new GetInfoCommand()).GetData();
                Output.WriteLine($"  AAGUID: {BitConverter.ToString(info.Aaguid.ToArray())}");
                Output.WriteLine($"  Versions: {string.Join(", ", info.Versions ?? Array.Empty<string>())}");
            });

            // ---- Test 2: Direct SmartCard FIDO2 ----
            RunTest("Test 2: Direct SmartCardConnection for FIDO2 over USB CCID", () =>
            {
                if (!device.HasSmartCard) { Output.WriteLine("  SKIPPED — no SmartCard interface"); return; }
                var scDevice = device.GetSmartCardDevice();
                Output.WriteLine($"  SmartCard path: {scDevice.Path}, IsNfc: {scDevice.IsNfcTransport()}");

                using var scConn = new SmartCardConnection(scDevice, YubiKeyApplication.Fido2);
                Output.WriteLine($"  SmartCardConnection created for FIDO2!");
                var info = scConn.SendCommand(new GetInfoCommand()).GetData();
                Output.WriteLine($"  AAGUID: {BitConverter.ToString(info.Aaguid.ToArray())}");
                Output.WriteLine($"  Transports: {string.Join(", ", info.Transports ?? Array.Empty<string>())}");
            });

            // ---- Test 3: FIDO2 + SCP03 (default keys) over USB CCID ----
            RunTest("Test 3: Fido2Session + SCP03 (DefaultKey) over USB CCID", () =>
            {
                using var session = new Fido2Session(targetKey, keyParameters: Scp03KeyParameters.DefaultKey);
                Output.WriteLine($"  Connection type: {session.Connection.GetType().Name}");
                Output.WriteLine($"  AAGUID: {BitConverter.ToString(session.AuthenticatorInfo.Aaguid.ToArray())}");
                Output.WriteLine($"  Versions: {string.Join(", ", session.AuthenticatorInfo.Versions ?? Array.Empty<string>())}");
            });

            // ---- Test 4: FIDO2 + SCP11b over USB CCID ----
            RunTest("Test 4: Fido2Session + SCP11b over USB CCID", () =>
            {
                // Step 1: Reset Security Domain to clean state
                Output.WriteLine("  Resetting Security Domain...");
                using (var sdSession = new SecurityDomainSession(targetKey))
                {
                    sdSession.Reset();
                }
                Output.WriteLine("  Security Domain reset OK");

                // Step 2: Get SCP11b key parameters (generates key ref on device)
                var keyReference = new KeyReference(ScpKeyIds.Scp11B, 0x1);
                Output.WriteLine($"  Getting SCP11b certificates for {keyReference}...");

                IReadOnlyCollection<X509Certificate2> certs;
                using (var sdSession = new SecurityDomainSession(targetKey))
                {
                    certs = sdSession.GetCertificates(keyReference);
                }

                var leaf = certs.Last();
                var ecDsaPublicKey = leaf.PublicKey.GetECDsaPublicKey()!;
                var keyParams = new Scp11KeyParameters(
                    keyReference,
                    ECPublicKey.CreateFromParameters(ecDsaPublicKey.ExportParameters(false)));
                Output.WriteLine($"  SCP11b key params created (leaf cert subject: {leaf.Subject})");

                // Step 3: Open FIDO2 session with SCP11b
                using var session = new Fido2Session(targetKey, keyParameters: keyParams);
                Output.WriteLine($"  Connection type: {session.Connection.GetType().Name}");
                Output.WriteLine($"  AAGUID: {BitConverter.ToString(session.AuthenticatorInfo.Aaguid.ToArray())}");
                Output.WriteLine($"  Versions: {string.Join(", ", session.AuthenticatorInfo.Versions ?? Array.Empty<string>())}");
            });

            Output.WriteLine();
            Output.WriteLine("=== Probe Complete ===");
            return true;
        }

        private void RunTest(string name, Action action)
        {
            Output.WriteLine();
            Output.WriteLine(name);
            try
            {
                action();
                Output.WriteLine($"  >>> PASS");
            }
            catch (Exception ex)
            {
                Output.WriteLine($"  >>> FAIL: {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Output.WriteLine($"  Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                }
            }
        }
    }
}
