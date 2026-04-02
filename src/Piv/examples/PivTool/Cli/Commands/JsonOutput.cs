// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Nodes;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

namespace Yubico.YubiKit.Piv.Examples.PivTool.Cli.Commands;

/// <summary>
/// Serializes PIV operation results to JSON for machine consumption.
/// All output goes to stdout; errors go to stderr.
/// </summary>
internal static class JsonOutput
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    internal static void Write(PinOperationResult result)
    {
        var node = new JsonObject
        {
            ["success"] = result.Success,
            ["retriesRemaining"] = result.RetriesRemaining,
            ["error"] = result.ErrorMessage
        };
        Console.WriteLine(node.ToJsonString(Options));
    }

    internal static void Write(ResetResult result)
    {
        var node = new JsonObject
        {
            ["success"] = result.Success,
            ["error"] = result.ErrorMessage
        };
        Console.WriteLine(node.ToJsonString(Options));
    }

    internal static void Write(VerificationResult result)
    {
        var node = new JsonObject
        {
            ["success"] = result.Success,
            ["isValid"] = result.IsValid,
            ["elapsedMs"] = result.ElapsedMilliseconds,
            ["error"] = result.ErrorMessage
        };
        Console.WriteLine(node.ToJsonString(Options));
    }

    internal static void Write(DeviceInfoResult result)
    {
        var node = new JsonObject
        {
            ["success"] = result.Success,
            ["pinRetriesRemaining"] = result.PinRetriesRemaining,
            ["pukRetriesRemaining"] = result.PukRetriesRemaining,
            ["error"] = result.ErrorMessage
        };

        if (result.DeviceInfo is DeviceInfo info)
        {
            node["device"] = new JsonObject
            {
                ["serialNumber"] = info.SerialNumber,
                ["firmwareVersion"] = info.FirmwareVersion.ToString(),
                ["formFactor"] = info.FormFactor.ToString(),
                ["isFips"] = info.IsFips,
                ["isSky"] = info.IsSky,
                ["usbSupported"] = SerializeCapabilities(info.UsbSupported),
                ["usbEnabled"] = SerializeCapabilities(info.UsbEnabled),
                ["nfcSupported"] = SerializeCapabilities(info.NfcSupported),
                ["nfcEnabled"] = SerializeCapabilities(info.NfcEnabled)
            };
        }

        Console.WriteLine(node.ToJsonString(Options));
    }

    internal static void Write(KeyGenerationResult result)
    {
        var node = new JsonObject
        {
            ["success"] = result.Success,
            ["slot"] = result.Slot.ToString(),
            ["algorithm"] = result.Algorithm.ToString(),
            ["publicKeyBase64"] = result.PublicKey.Length > 0
                ? Convert.ToBase64String(result.PublicKey.Span)
                : null,
            ["error"] = result.ErrorMessage
        };
        Console.WriteLine(node.ToJsonString(Options));
    }

    internal static void Write(SigningResult result)
    {
        var node = new JsonObject
        {
            ["success"] = result.Success,
            ["signatureBase64"] = result.Signature.Length > 0
                ? Convert.ToBase64String(result.Signature.Span)
                : null,
            ["signatureHex"] = result.Signature.Length > 0
                ? Convert.ToHexString(result.Signature.Span)
                : null,
            ["elapsedMs"] = result.ElapsedMilliseconds,
            ["error"] = result.ErrorMessage
        };
        Console.WriteLine(node.ToJsonString(Options));
    }

    internal static void Write(DecryptionResult result)
    {
        var node = new JsonObject
        {
            ["success"] = result.Success,
            ["decryptedBase64"] = result.DecryptedData.Length > 0
                ? Convert.ToBase64String(result.DecryptedData.Span)
                : null,
            ["elapsedMs"] = result.ElapsedMilliseconds,
            ["error"] = result.ErrorMessage
        };
        Console.WriteLine(node.ToJsonString(Options));
    }

    internal static void Write(AttestationResult result)
    {
        var node = new JsonObject
        {
            ["success"] = result.Success,
            ["attestationCertificate"] = SerializeCert(result.AttestationCertificate),
            ["intermediateCertificate"] = SerializeCert(result.IntermediateCertificate),
            ["error"] = result.ErrorMessage
        };
        Console.WriteLine(node.ToJsonString(Options));
    }

    internal static void Write(CertificateResult result)
    {
        var node = new JsonObject
        {
            ["success"] = result.Success,
            ["certificate"] = SerializeCert(result.Certificate),
            ["csrPem"] = result.CsrPem,
            ["error"] = result.ErrorMessage
        };
        Console.WriteLine(node.ToJsonString(Options));
    }

    internal static void Write(SlotInfoResult result)
    {
        var slots = new JsonArray();
        foreach (var slot in result.Slots)
        {
            var slotNode = new JsonObject
            {
                ["slot"] = slot.Slot.ToString(),
                ["name"] = slot.Name,
                ["hasKey"] = slot.HasKey,
                ["hasCertificate"] = slot.HasCertificate,
                ["certificate"] = SerializeCert(slot.Certificate)
            };

            if (slot.Metadata is PivSlotMetadata m)
            {
                slotNode["metadata"] = new JsonObject
                {
                    ["algorithm"] = m.Algorithm.ToString(),
                    ["pinPolicy"] = m.PinPolicy.ToString(),
                    ["touchPolicy"] = m.TouchPolicy.ToString(),
                    ["isGenerated"] = m.IsGenerated,
                    ["publicKeyBase64"] = m.PublicKey.Length > 0
                        ? Convert.ToBase64String(m.PublicKey.Span)
                        : null
                };
            }

            slots.Add(slotNode);
        }

        var node = new JsonObject
        {
            ["success"] = result.Success,
            ["slots"] = slots,
            ["error"] = result.ErrorMessage
        };
        Console.WriteLine(node.ToJsonString(Options));
    }

    internal static void WriteError(string message)
    {
        var node = new JsonObject
        {
            ["success"] = false,
            ["error"] = message
        };
        Console.WriteLine(node.ToJsonString(Options));
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static JsonNode? SerializeCert(X509Certificate2? cert)
    {
        if (cert is null)
        {
            return null;
        }

        return new JsonObject
        {
            ["subject"] = cert.Subject,
            ["issuer"] = cert.Issuer,
            ["serialNumber"] = cert.SerialNumber,
            ["thumbprint"] = cert.Thumbprint,
            ["notBefore"] = cert.NotBefore.ToString("O"),
            ["notAfter"] = cert.NotAfter.ToString("O"),
            ["pem"] = cert.ExportCertificatePem()
        };
    }

    private static JsonArray SerializeCapabilities(DeviceCapabilities caps)
    {
        var arr = new JsonArray();
        if (caps.HasFlag(DeviceCapabilities.Otp)) arr.Add("OTP");
        if (caps.HasFlag(DeviceCapabilities.U2f)) arr.Add("U2F");
        if (caps.HasFlag(DeviceCapabilities.Fido2)) arr.Add("FIDO2");
        if (caps.HasFlag(DeviceCapabilities.Oath)) arr.Add("OATH");
        if (caps.HasFlag(DeviceCapabilities.Piv)) arr.Add("PIV");
        if (caps.HasFlag(DeviceCapabilities.OpenPgp)) arr.Add("OpenPGP");
        if (caps.HasFlag(DeviceCapabilities.HsmAuth)) arr.Add("HSM");
        return arr;
    }
}