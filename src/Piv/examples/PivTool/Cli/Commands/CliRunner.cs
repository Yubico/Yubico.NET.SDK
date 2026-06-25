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

using System.Security.Cryptography;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;

namespace Yubico.YubiKit.Piv.Examples.PivTool.Cli.Commands;

/// <summary>
/// Non-interactive CLI mode entry point.
///
/// All commands write results to stdout. Errors and diagnostics go to stderr.
/// Exit code 0 = success, 1 = failure.
///
/// SECURITY NOTE: Credentials supplied via CLI flags (--pin, --puk, --management-key)
/// will be visible in process listings. This is acceptable for an example/test tool
/// but should never be done in production applications.
/// </summary>
internal static class CliRunner
{
    internal static async Task<int> RunAsync(string[] args, CancellationToken ct = default)
    {
        var (command, flags) = ParseArgs(args);

        // Global flags
        int? serial = null;
        if (DeviceHelper.GetFlag(flags, "serial") is string serialStr &&
            int.TryParse(serialStr, out var parsedSerial))
        {
            serial = parsedSerial;
        }

        bool useJson = DeviceHelper.HasFlag(flags, "json");

        return command.ToLowerInvariant() switch
        {
            "device-info" => await HandleDeviceInfoAsync(flags, serial, useJson, ct),
            "pin-verify" => await HandlePinVerifyAsync(flags, serial, useJson, ct),
            "pin-change" => await HandlePinChangeAsync(flags, serial, useJson, ct),
            "puk-change" => await HandlePukChangeAsync(flags, serial, useJson, ct),
            "pin-unblock" => await HandlePinUnblockAsync(flags, serial, useJson, ct),
            "pin-retries" => await HandlePinRetriesAsync(flags, serial, useJson, ct),
            "mgmt-key-auth" => await HandleMgmtKeyAuthAsync(flags, serial, useJson, ct),
            "mgmt-key-change" => await HandleMgmtKeyChangeAsync(flags, serial, useJson, ct),
            "key-generate" => await HandleKeyGenerateAsync(flags, serial, useJson, ct),
            "cert-view" => await HandleCertViewAsync(flags, serial, useJson, ct),
            "cert-export" => await HandleCertExportAsync(flags, serial, useJson, ct),
            "cert-import" => await HandleCertImportAsync(flags, serial, useJson, ct),
            "cert-self-sign" => await HandleCertSelfSignAsync(flags, serial, useJson, ct),
            "cert-csr" => await HandleCertCsrAsync(flags, serial, useJson, ct),
            "cert-delete" => await HandleCertDeleteAsync(flags, serial, useJson, ct),
            "sign" => await HandleSignAsync(flags, serial, useJson, ct),
            "decrypt" => await HandleDecryptAsync(flags, serial, useJson, ct),
            "verify-signature" => await HandleVerifySignatureAsync(flags, serial, useJson, ct),
            "attest" => await HandleAttestAsync(flags, serial, useJson, ct),
            "attest-intermediate" => await HandleAttestIntermediateAsync(flags, serial, useJson, ct),
            "slot-info" => await HandleSlotInfoAsync(flags, serial, useJson, ct),
            "reset" => await HandleResetAsync(flags, serial, useJson, ct),
            "help" or "--help" or "-h" or "-?" => PrintHelp(),
            _ => PrintUnknownCommand(command)
        };
    }

    // ── Argument parser ───────────────────────────────────────────────────────

    /// <summary>
    /// Parses args into (commandName, {flagName -> value}).
    /// Supports: command --flag value --bool-flag
    /// The command is the first non-flag argument.
    /// </summary>
    private static (string command, Dictionary<string, string?> flags) ParseArgs(string[] args)
    {
        var command = "help";
        var flags = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--", StringComparison.Ordinal))
            {
                var key = args[i][2..];
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    flags[key] = args[++i];
                }
                else
                {
                    flags[key] = null; // boolean flag (presence = true)
                }
            }
            else if (command == "help")
            {
                command = args[i]; // first positional = command
            }
        }

        return (command, flags);
    }

    // ── Handlers ─────────────────────────────────────────────────────────────

    private static async Task<int> HandleDeviceInfoAsync(
        Dictionary<string, string?> flags, int? serial, bool useJson, CancellationToken ct)
    {
        var device = await DeviceHelper.GetDeviceAsync(serial, ct);
        if (device is null) return 1;

        var deviceInfo = await device.GetDeviceInfoAsync(ct);

        await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);
        var result = await DeviceInfoQuery.GetPivRetryInfoAsync(session, ct);

        if (useJson)
        {
            var combined = PivExamples.Results.DeviceInfoResult.Succeeded(
                deviceInfo,
                result.PinRetriesRemaining,
                result.PukRetriesRemaining);
            JsonOutput.Write(combined);
        }
        else
        {
            Console.WriteLine($"Serial Number:    {deviceInfo.SerialNumber?.ToString() ?? "N/A"}");
            Console.WriteLine($"Firmware Version: {deviceInfo.FirmwareVersion}");
            Console.WriteLine($"Form Factor:      {deviceInfo.FormFactor}");
            Console.WriteLine($"FIPS:             {deviceInfo.IsFips}");
            Console.WriteLine($"Security Key:     {deviceInfo.IsSky}");
            Console.WriteLine($"USB Supported:    {deviceInfo.UsbSupported}");
            Console.WriteLine($"USB Enabled:      {deviceInfo.UsbEnabled}");
            if (deviceInfo.NfcSupported != Management.DeviceCapabilities.None)
            {
                Console.WriteLine($"NFC Supported:    {deviceInfo.NfcSupported}");
                Console.WriteLine($"NFC Enabled:      {deviceInfo.NfcEnabled}");
            }
            if (result.Success)
            {
                Console.WriteLine($"PIN Retries:      {result.PinRetriesRemaining}");
                Console.WriteLine($"PUK Retries:      {result.PukRetriesRemaining}");
            }
        }

        return 0;
    }

    private static async Task<int> HandlePinVerifyAsync(
        Dictionary<string, string?> flags, int? serial, bool useJson, CancellationToken ct)
    {
        var pinBytes = DeviceHelper.ParseCredentialBytes(DeviceHelper.GetFlag(flags, "pin"), "pin");
        if (pinBytes is null) return 1;

        var device = await DeviceHelper.GetDeviceAsync(serial, ct);
        if (device is null) { ZeroAndReturn(pinBytes); return 1; }

        try
        {
            await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);
            var result = await PinManagement.VerifyPinAsync(session, pinBytes, ct);
            return WriteResult(result, useJson);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pinBytes);
        }
    }

    private static async Task<int> HandlePinChangeAsync(
        Dictionary<string, string?> flags, int? serial, bool useJson, CancellationToken ct)
    {
        var pin = DeviceHelper.ParseCredentialBytes(DeviceHelper.GetFlag(flags, "pin"), "pin");
        var newPin = DeviceHelper.ParseCredentialBytes(DeviceHelper.GetFlag(flags, "new-pin"), "new-pin");
        if (pin is null || newPin is null) { ZeroAndReturn(pin, newPin); return 1; }

        var device = await DeviceHelper.GetDeviceAsync(serial, ct);
        if (device is null) { ZeroAndReturn(pin, newPin); return 1; }

        try
        {
            await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);
            var result = await PinManagement.ChangePinAsync(session, pin, newPin, ct);
            return WriteResult(result, useJson);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pin);
            CryptographicOperations.ZeroMemory(newPin);
        }
    }

    private static async Task<int> HandlePukChangeAsync(
        Dictionary<string, string?> flags, int? serial, bool useJson, CancellationToken ct)
    {
        var puk = DeviceHelper.ParseCredentialBytes(DeviceHelper.GetFlag(flags, "puk"), "puk");
        var newPuk = DeviceHelper.ParseCredentialBytes(DeviceHelper.GetFlag(flags, "new-puk"), "new-puk");
        if (puk is null || newPuk is null) { ZeroAndReturn(puk, newPuk); return 1; }

        var device = await DeviceHelper.GetDeviceAsync(serial, ct);
        if (device is null) { ZeroAndReturn(puk, newPuk); return 1; }

        try
        {
            await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);
            var result = await PinManagement.ChangePukAsync(session, puk, newPuk, ct);
            return WriteResult(result, useJson);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(puk);
            CryptographicOperations.ZeroMemory(newPuk);
        }
    }

    private static async Task<int> HandlePinUnblockAsync(
        Dictionary<string, string?> flags, int? serial, bool useJson, CancellationToken ct)
    {
        var puk = DeviceHelper.ParseCredentialBytes(DeviceHelper.GetFlag(flags, "puk"), "puk");
        var newPin = DeviceHelper.ParseCredentialBytes(DeviceHelper.GetFlag(flags, "new-pin"), "new-pin");
        if (puk is null || newPin is null) { ZeroAndReturn(puk, newPin); return 1; }

        var device = await DeviceHelper.GetDeviceAsync(serial, ct);
        if (device is null) { ZeroAndReturn(puk, newPin); return 1; }

        try
        {
            await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);
            var result = await PinManagement.UnblockPinAsync(session, puk, newPin, ct);
            return WriteResult(result, useJson);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(puk);
            CryptographicOperations.ZeroMemory(newPin);
        }
    }

    private static async Task<int> HandlePinRetriesAsync(
        Dictionary<string, string?> flags, int? serial, bool useJson, CancellationToken ct)
    {
        var device = await DeviceHelper.GetDeviceAsync(serial, ct);
        if (device is null) return 1;

        await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);
        var result = await PinManagement.GetPinRetriesAsync(session, ct);
        return WriteResult(result, useJson);
    }

    private static async Task<int> HandleMgmtKeyAuthAsync(
        Dictionary<string, string?> flags, int? serial, bool useJson, CancellationToken ct)
    {
        var key = DeviceHelper.ParseHex(DeviceHelper.GetFlag(flags, "management-key"), "management-key");
        if (key is null) return 1;

        var device = await DeviceHelper.GetDeviceAsync(serial, ct);
        if (device is null) { ZeroAndReturn(key); return 1; }

        try
        {
            await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);
            var result = await PinManagement.AuthenticateAsync(session, key, ct);
            return WriteResult(result, useJson);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private static async Task<int> HandleMgmtKeyChangeAsync(
        Dictionary<string, string?> flags, int? serial, bool useJson, CancellationToken ct)
    {
        var key = DeviceHelper.ParseHex(DeviceHelper.GetFlag(flags, "management-key"), "management-key");
        var newKey = DeviceHelper.ParseHex(DeviceHelper.GetFlag(flags, "new-management-key"), "new-management-key");
        if (key is null || newKey is null) { ZeroAndReturn(key, newKey); return 1; }

        var keyType = DeviceHelper.ParseMgmtKeyType(DeviceHelper.GetFlag(flags, "algorithm"));
        bool requireTouch = DeviceHelper.HasFlag(flags, "require-touch");

        var device = await DeviceHelper.GetDeviceAsync(serial, ct);
        if (device is null) { ZeroAndReturn(key, newKey); return 1; }

        try
        {
            await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);
            var authResult = await PinManagement.AuthenticateAsync(session, key, ct);
            if (!authResult.Success)
            {
                if (useJson) JsonOutput.Write(authResult);
                else Console.Error.WriteLine($"Error: {authResult.ErrorMessage}");
                return 1;
            }

            var result = await PinManagement.ChangeManagementKeyAsync(session, newKey, keyType, requireTouch, ct);
            return WriteResult(result, useJson);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(newKey);
        }
    }

    private static async Task<int> HandleKeyGenerateAsync(
        Dictionary<string, string?> flags, int? serial, bool useJson, CancellationToken ct)
    {
        var slot = DeviceHelper.ParseSlot(DeviceHelper.GetFlag(flags, "slot"));
        var algorithm = DeviceHelper.ParseKeyAlgorithm(DeviceHelper.GetFlag(flags, "algorithm"));
        var key = DeviceHelper.ParseHex(DeviceHelper.GetFlag(flags, "management-key"), "management-key");
        if (slot is null || algorithm is null || key is null) { ZeroAndReturn(key); return 1; }

        var pinPolicy = DeviceHelper.ParsePinPolicy(DeviceHelper.GetFlag(flags, "pin-policy"));
        var touchPolicy = DeviceHelper.ParseTouchPolicy(DeviceHelper.GetFlag(flags, "touch-policy"));

        var device = await DeviceHelper.GetDeviceAsync(serial, ct);
        if (device is null) { ZeroAndReturn(key); return 1; }

        try
        {
            await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);
            var authResult = await PinManagement.AuthenticateAsync(session, key, ct);
            if (!authResult.Success)
            {
                if (useJson) JsonOutput.Write(authResult);
                else Console.Error.WriteLine($"Error: {authResult.ErrorMessage}");
                return 1;
            }

            var result = await KeyGeneration.GenerateKeyAsync(
                session, slot.Value, algorithm.Value, pinPolicy, touchPolicy, ct);

            if (useJson)
            {
                JsonOutput.Write(result);
            }
            else
            {
                Console.WriteLine($"Success:    {result.Success}");
                Console.WriteLine($"Slot:       {result.Slot}");
                Console.WriteLine($"Algorithm:  {result.Algorithm}");
                if (result.PublicKey.Length > 0)
                {
                    Console.WriteLine($"Public Key: {Convert.ToBase64String(result.PublicKey.Span)}");
                }
                if (!result.Success) Console.Error.WriteLine($"Error: {result.ErrorMessage}");
            }

            return result.Success ? 0 : 1;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private static async Task<int> HandleCertViewAsync(
        Dictionary<string, string?> flags, int? serial, bool useJson, CancellationToken ct)
    {
        var slot = DeviceHelper.ParseSlot(DeviceHelper.GetFlag(flags, "slot"));
        if (slot is null) return 1;

        var device = await DeviceHelper.GetDeviceAsync(serial, ct);
        if (device is null) return 1;

        await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);
        var result = await Certificates.GetCertificateAsync(session, slot.Value, ct);

        if (useJson)
        {
            JsonOutput.Write(result);
        }
        else if (result.Success && result.Certificate is not null)
        {
            Console.WriteLine($"Subject:    {result.Certificate.Subject}");
            Console.WriteLine($"Issuer:     {result.Certificate.Issuer}");
            Console.WriteLine($"Serial:     {result.Certificate.SerialNumber}");
            Console.WriteLine($"Thumbprint: {result.Certificate.Thumbprint}");
            Console.WriteLine($"Not Before: {result.Certificate.NotBefore:O}");
            Console.WriteLine($"Not After:  {result.Certificate.NotAfter:O}");
        }
        else
        {
            Console.Error.WriteLine($"Error: {result.ErrorMessage}");
        }

        return result.Success ? 0 : 1;
    }

    private static async Task<int> HandleCertExportAsync(
        Dictionary<string, string?> flags, int? serial, bool useJson, CancellationToken ct)
    {
        var slot = DeviceHelper.ParseSlot(DeviceHelper.GetFlag(flags, "slot"));
        var outputPath = DeviceHelper.GetFlag(flags, "output");
        if (slot is null) return 1;

        var device = await DeviceHelper.GetDeviceAsync(serial, ct);
        if (device is null) return 1;

        await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);
        var result = await Certificates.ExportCertificatePemAsync(session, slot.Value, ct);

        if (!result.Success)
        {
            if (useJson) JsonOutput.Write(result);
            else Console.Error.WriteLine($"Error: {result.ErrorMessage}");
            return 1;
        }

        var pem = result.Certificate?.ExportCertificatePem() ?? string.Empty;

        if (outputPath is not null)
        {
            await File.WriteAllTextAsync(outputPath, pem, ct);
            if (!useJson) Console.WriteLine($"Certificate exported to: {outputPath}");
        }

        if (useJson)
        {
            JsonOutput.Write(result);
        }
        else if (outputPath is null)
        {
            Console.WriteLine(pem);
        }

        return 0;
    }

    private static async Task<int> HandleCertImportAsync(
        Dictionary<string, string?> flags, int? serial, bool useJson, CancellationToken ct)
    {
        var slot = DeviceHelper.ParseSlot(DeviceHelper.GetFlag(flags, "slot"));
        var certPath = DeviceHelper.GetFlag(flags, "cert-path");
        var key = DeviceHelper.ParseHex(DeviceHelper.GetFlag(flags, "management-key"), "management-key");
        if (slot is null || key is null) { ZeroAndReturn(key); return 1; }

        if (certPath is null)
        {
            Console.Error.WriteLine("Error: --cert-path is required.");
            ZeroAndReturn(key);
            return 1;
        }

        if (!File.Exists(certPath))
        {
            Console.Error.WriteLine($"Error: Certificate file not found: {certPath}");
            ZeroAndReturn(key);
            return 1;
        }

        bool compress = DeviceHelper.HasFlag(flags, "compress");
        var certData = await File.ReadAllBytesAsync(certPath, ct);

        var device = await DeviceHelper.GetDeviceAsync(serial, ct);
        if (device is null) { ZeroAndReturn(key); return 1; }

        try
        {
            await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);
            var authResult = await PinManagement.AuthenticateAsync(session, key, ct);
            if (!authResult.Success)
            {
                if (useJson) JsonOutput.Write(authResult);
                else Console.Error.WriteLine($"Error: {authResult.ErrorMessage}");
                return 1;
            }

            var result = await Certificates.ImportCertificateAsync(session, slot.Value, certData, compress, ct);
            if (useJson) JsonOutput.Write(result);
            else if (result.Success) Console.WriteLine("Certificate imported successfully.");
            else Console.Error.WriteLine($"Error: {result.ErrorMessage}");
            return result.Success ? 0 : 1;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private static async Task<int> HandleCertSelfSignAsync(
        Dictionary<string, string?> flags, int? serial, bool useJson, CancellationToken ct)
    {
        var slot = DeviceHelper.ParseSlot(DeviceHelper.GetFlag(flags, "slot"));
        var key = DeviceHelper.ParseHex(DeviceHelper.GetFlag(flags, "management-key"), "management-key");
        var pin = DeviceHelper.ParseCredentialBytes(DeviceHelper.GetFlag(flags, "pin"), "pin");
        if (slot is null || key is null || pin is null) { ZeroAndReturn(key, pin); return 1; }

        var subject = DeviceHelper.GetFlag(flags, "subject") ?? "CN=Test User";
        int validityDays = DeviceHelper.GetFlag(flags, "validity-days") is string vd
            && int.TryParse(vd, out var days) ? days : 365;

        var device = await DeviceHelper.GetDeviceAsync(serial, ct);
        if (device is null) { ZeroAndReturn(key, pin); return 1; }

        try
        {
            await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);
            var authResult = await PinManagement.AuthenticateAsync(session, key, ct);
            if (!authResult.Success)
            {
                if (useJson) JsonOutput.Write(authResult);
                else Console.Error.WriteLine($"Error: {authResult.ErrorMessage}");
                return 1;
            }

            var pinResult = await PinManagement.VerifyPinAsync(session, pin, ct);
            if (!pinResult.Success)
            {
                if (useJson) JsonOutput.Write(pinResult);
                else Console.Error.WriteLine($"Error: {pinResult.ErrorMessage}");
                return 1;
            }

            var result = await Certificates.GenerateSelfSignedAsync(session, slot.Value, subject, validityDays, ct);
            if (useJson) JsonOutput.Write(result);
            else if (result.Success) Console.WriteLine("Self-signed certificate generated successfully.");
            else Console.Error.WriteLine($"Error: {result.ErrorMessage}");
            return result.Success ? 0 : 1;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(pin);
        }
    }

    private static async Task<int> HandleCertCsrAsync(
        Dictionary<string, string?> flags, int? serial, bool useJson, CancellationToken ct)
    {
        var slot = DeviceHelper.ParseSlot(DeviceHelper.GetFlag(flags, "slot"));
        if (slot is null) return 1;

        var subject = DeviceHelper.GetFlag(flags, "subject") ?? "CN=Test User";
        var outputPath = DeviceHelper.GetFlag(flags, "output");

        // PIN is needed to sign the CSR on the YubiKey
        var pin = DeviceHelper.ParseCredentialBytes(DeviceHelper.GetFlag(flags, "pin"), "pin");
        if (pin is null) return 1;

        var device = await DeviceHelper.GetDeviceAsync(serial, ct);
        if (device is null) { ZeroAndReturn(pin); return 1; }

        try
        {
            await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);
            var pinResult = await PinManagement.VerifyPinAsync(session, pin, ct);
            if (!pinResult.Success)
            {
                if (useJson) JsonOutput.Write(pinResult);
                else Console.Error.WriteLine($"Error: {pinResult.ErrorMessage}");
                return 1;
            }

            var result = await Certificates.GenerateCsrAsync(session, slot.Value, subject, ct);

            if (!result.Success)
            {
                if (useJson) JsonOutput.Write(result);
                else Console.Error.WriteLine($"Error: {result.ErrorMessage}");
                return 1;
            }

            if (outputPath is not null && result.CsrPem is not null)
            {
                await File.WriteAllTextAsync(outputPath, result.CsrPem, ct);
                if (!useJson) Console.WriteLine($"CSR written to: {outputPath}");
            }

            if (useJson) JsonOutput.Write(result);
            else if (outputPath is null) Console.WriteLine(result.CsrPem ?? string.Empty);

            return 0;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pin);
        }
    }

    private static async Task<int> HandleCertDeleteAsync(
        Dictionary<string, string?> flags, int? serial, bool useJson, CancellationToken ct)
    {
        var slot = DeviceHelper.ParseSlot(DeviceHelper.GetFlag(flags, "slot"));
        var key = DeviceHelper.ParseHex(DeviceHelper.GetFlag(flags, "management-key"), "management-key");
        if (slot is null || key is null) { ZeroAndReturn(key); return 1; }

        var device = await DeviceHelper.GetDeviceAsync(serial, ct);
        if (device is null) { ZeroAndReturn(key); return 1; }

        try
        {
            await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);
            var authResult = await PinManagement.AuthenticateAsync(session, key, ct);
            if (!authResult.Success)
            {
                if (useJson) JsonOutput.Write(authResult);
                else Console.Error.WriteLine($"Error: {authResult.ErrorMessage}");
                return 1;
            }

            var result = await Certificates.DeleteCertificateAsync(session, slot.Value, ct);
            if (useJson) JsonOutput.Write(result);
            else if (result.Success) Console.WriteLine("Certificate deleted.");
            else Console.Error.WriteLine($"Error: {result.ErrorMessage}");
            return result.Success ? 0 : 1;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private static async Task<int> HandleSignAsync(
        Dictionary<string, string?> flags, int? serial, bool useJson, CancellationToken ct)
    {
        var slot = DeviceHelper.ParseSlot(DeviceHelper.GetFlag(flags, "slot"));
        var pin = DeviceHelper.ParseCredentialBytes(DeviceHelper.GetFlag(flags, "pin"), "pin");
        var inputPath = DeviceHelper.GetFlag(flags, "input");
        if (slot is null || pin is null) { ZeroAndReturn(pin); return 1; }

        if (inputPath is null)
        {
            Console.Error.WriteLine("Error: --input is required.");
            ZeroAndReturn(pin);
            return 1;
        }

        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"Error: Input file not found: {inputPath}");
            ZeroAndReturn(pin);
            return 1;
        }

        var hashAlg = DeviceHelper.ParseHashAlgorithm(DeviceHelper.GetFlag(flags, "hash"));
        var outputPath = DeviceHelper.GetFlag(flags, "output");
        var dataToSign = await File.ReadAllBytesAsync(inputPath, ct);

        var device = await DeviceHelper.GetDeviceAsync(serial, ct);
        if (device is null) { ZeroAndReturn(pin); return 1; }

        try
        {
            await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);
            var pinResult = await PinManagement.VerifyPinAsync(session, pin, ct);
            if (!pinResult.Success)
            {
                if (useJson) JsonOutput.Write(pinResult);
                else Console.Error.WriteLine($"Error: {pinResult.ErrorMessage}");
                return 1;
            }

            var result = await Signing.SignDataAsync(session, slot.Value, dataToSign, hashAlg, ct);

            if (outputPath is not null && result.Success)
            {
                await File.WriteAllBytesAsync(outputPath, result.Signature.ToArray(), ct);
                if (!useJson) Console.WriteLine($"Signature written to: {outputPath}");
            }

            if (useJson) JsonOutput.Write(result);
            else if (result.Success)
            {
                Console.WriteLine($"Signature: {Convert.ToHexString(result.Signature.Span)}");
                Console.WriteLine($"Elapsed:   {result.ElapsedMilliseconds}ms");
            }
            else Console.Error.WriteLine($"Error: {result.ErrorMessage}");

            return result.Success ? 0 : 1;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pin);
        }
    }

    private static async Task<int> HandleDecryptAsync(
        Dictionary<string, string?> flags, int? serial, bool useJson, CancellationToken ct)
    {
        var slot = DeviceHelper.ParseSlot(DeviceHelper.GetFlag(flags, "slot"));
        var pin = DeviceHelper.ParseCredentialBytes(DeviceHelper.GetFlag(flags, "pin"), "pin");
        var inputPath = DeviceHelper.GetFlag(flags, "input");
        if (slot is null || pin is null) { ZeroAndReturn(pin); return 1; }

        if (inputPath is null)
        {
            Console.Error.WriteLine("Error: --input is required.");
            ZeroAndReturn(pin);
            return 1;
        }

        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"Error: Input file not found: {inputPath}");
            ZeroAndReturn(pin);
            return 1;
        }

        var outputPath = DeviceHelper.GetFlag(flags, "output");
        var encryptedData = await File.ReadAllBytesAsync(inputPath, ct);

        var device = await DeviceHelper.GetDeviceAsync(serial, ct);
        if (device is null) { ZeroAndReturn(pin); return 1; }

        try
        {
            await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);
            var pinResult = await PinManagement.VerifyPinAsync(session, pin, ct);
            if (!pinResult.Success)
            {
                if (useJson) JsonOutput.Write(pinResult);
                else Console.Error.WriteLine($"Error: {pinResult.ErrorMessage}");
                return 1;
            }

            var result = await Decryption.DecryptDataAsync(session, slot.Value, encryptedData, cancellationToken: ct);

            if (outputPath is not null && result.Success)
            {
                await File.WriteAllBytesAsync(outputPath, result.DecryptedData.ToArray(), ct);
                if (!useJson) Console.WriteLine($"Decrypted data written to: {outputPath}");
            }

            if (useJson) JsonOutput.Write(result);
            else if (result.Success)
            {
                if (outputPath is null)
                {
                    Console.WriteLine(Convert.ToBase64String(result.DecryptedData.Span));
                }
                Console.WriteLine($"Elapsed: {result.ElapsedMilliseconds}ms");
            }
            else Console.Error.WriteLine($"Error: {result.ErrorMessage}");

            return result.Success ? 0 : 1;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pin);
        }
    }

    private static async Task<int> HandleVerifySignatureAsync(
        Dictionary<string, string?> flags, int? serial, bool useJson, CancellationToken ct)
    {
        var slot = DeviceHelper.ParseSlot(DeviceHelper.GetFlag(flags, "slot"));
        var dataPath = DeviceHelper.GetFlag(flags, "data");
        var sigPath = DeviceHelper.GetFlag(flags, "signature");
        if (slot is null) return 1;

        if (dataPath is null || sigPath is null)
        {
            Console.Error.WriteLine("Error: --data and --signature are required.");
            return 1;
        }

        if (!File.Exists(dataPath)) { Console.Error.WriteLine($"Error: Data file not found: {dataPath}"); return 1; }
        if (!File.Exists(sigPath)) { Console.Error.WriteLine($"Error: Signature file not found: {sigPath}"); return 1; }

        var hashAlg = DeviceHelper.ParseHashAlgorithm(DeviceHelper.GetFlag(flags, "hash"));
        var data = await File.ReadAllBytesAsync(dataPath, ct);
        var sig = await File.ReadAllBytesAsync(sigPath, ct);

        var device = await DeviceHelper.GetDeviceAsync(serial, ct);
        if (device is null) return 1;

        await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);
        var result = await Verification.VerifySignatureAsync(session, slot.Value, data, sig, hashAlg, ct);

        if (useJson) JsonOutput.Write(result);
        else
        {
            Console.WriteLine($"Valid:   {result.IsValid}");
            Console.WriteLine($"Elapsed: {result.ElapsedMilliseconds}ms");
            if (!result.Success) Console.Error.WriteLine($"Error: {result.ErrorMessage}");
        }

        return result.Success ? 0 : 1;
    }

    private static async Task<int> HandleAttestAsync(
        Dictionary<string, string?> flags, int? serial, bool useJson, CancellationToken ct)
    {
        var slot = DeviceHelper.ParseSlot(DeviceHelper.GetFlag(flags, "slot"));
        if (slot is null) return 1;

        var outputPath = DeviceHelper.GetFlag(flags, "output");

        var device = await DeviceHelper.GetDeviceAsync(serial, ct);
        if (device is null) return 1;

        await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);
        var result = await Attestation.GetAttestationAsync(session, slot.Value, ct);

        if (!result.Success)
        {
            if (useJson) JsonOutput.Write(result);
            else Console.Error.WriteLine($"Error: {result.ErrorMessage}");
            return 1;
        }

        if (outputPath is not null && result.AttestationCertificate is not null)
        {
            await File.WriteAllTextAsync(outputPath, result.AttestationCertificate.ExportCertificatePem(), ct);
            if (!useJson) Console.WriteLine($"Attestation certificate written to: {outputPath}");
        }

        if (useJson) JsonOutput.Write(result);
        else if (result.AttestationCertificate is not null)
        {
            Console.WriteLine($"Subject: {result.AttestationCertificate.Subject}");
            Console.WriteLine($"Issuer:  {result.AttestationCertificate.Issuer}");
            if (outputPath is null)
            {
                Console.WriteLine(result.AttestationCertificate.ExportCertificatePem());
            }
        }

        return 0;
    }

    private static async Task<int> HandleAttestIntermediateAsync(
        Dictionary<string, string?> flags, int? serial, bool useJson, CancellationToken ct)
    {
        var outputPath = DeviceHelper.GetFlag(flags, "output");

        var device = await DeviceHelper.GetDeviceAsync(serial, ct);
        if (device is null) return 1;

        await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);
        var result = await Attestation.GetIntermediateCertificateAsync(session, ct);

        if (!result.Success)
        {
            if (useJson) JsonOutput.Write(result);
            else Console.Error.WriteLine($"Error: {result.ErrorMessage}");
            return 1;
        }

        var cert = result.IntermediateCertificate ?? result.AttestationCertificate;
        if (outputPath is not null && cert is not null)
        {
            await File.WriteAllTextAsync(outputPath, cert.ExportCertificatePem(), ct);
            if (!useJson) Console.WriteLine($"Intermediate certificate written to: {outputPath}");
        }

        if (useJson) JsonOutput.Write(result);
        else if (cert is not null)
        {
            Console.WriteLine($"Subject: {cert.Subject}");
            Console.WriteLine($"Issuer:  {cert.Issuer}");
            if (outputPath is null) Console.WriteLine(cert.ExportCertificatePem());
        }

        return 0;
    }

    private static async Task<int> HandleSlotInfoAsync(
        Dictionary<string, string?> flags, int? serial, bool useJson, CancellationToken ct)
    {
        var slotStr = DeviceHelper.GetFlag(flags, "slot");
        var device = await DeviceHelper.GetDeviceAsync(serial, ct);
        if (device is null) return 1;

        await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);

        PivExamples.Results.SlotInfoResult result;
        if (slotStr is not null)
        {
            var slot = DeviceHelper.ParseSlot(slotStr);
            if (slot is null) return 1;
            result = await SlotInfoQuery.GetSlotInfoAsync(session, slot.Value, ct);
        }
        else
        {
            result = await SlotInfoQuery.GetAllSlotsInfoAsync(session, ct);
        }

        if (useJson)
        {
            JsonOutput.Write(result);
        }
        else
        {
            foreach (var s in result.Slots)
            {
                Console.WriteLine($"Slot {s.Slot} ({s.Name}): HasKey={s.HasKey} HasCert={s.HasCertificate}");
                if (s.Metadata is PivSlotMetadata m)
                {
                    Console.WriteLine($"  Algorithm:   {m.Algorithm}");
                    Console.WriteLine($"  PIN Policy:  {m.PinPolicy}");
                    Console.WriteLine($"  Touch Policy:{m.TouchPolicy}");
                    Console.WriteLine($"  Generated:   {m.IsGenerated}");
                }
            }
        }

        return result.Success ? 0 : 1;
    }

    private static async Task<int> HandleResetAsync(
        Dictionary<string, string?> flags, int? serial, bool useJson, CancellationToken ct)
    {
        bool force = DeviceHelper.HasFlag(flags, "force");

        if (!force && !useJson)
        {
            Console.Write("WARNING: This will reset the PIV application and delete all keys. Type 'RESET' to confirm: ");
            var input = Console.ReadLine();
            if (!string.Equals(input, "RESET", StringComparison.Ordinal))
            {
                Console.WriteLine("Reset cancelled.");
                return 0;
            }
        }
        else if (!force && useJson)
        {
            // In JSON non-force mode, require --force to avoid accidental reset
            Console.Error.WriteLine("Error: Use --force to confirm reset in non-interactive mode.");
            return 1;
        }

        var device = await DeviceHelper.GetDeviceAsync(serial, ct);
        if (device is null) return 1;

        await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);
        var result = await Reset.ResetPivApplicationAsync(session, ct);

        if (useJson) JsonOutput.Write(result);
        else if (result.Success) Console.WriteLine("PIV application reset to factory defaults.");
        else Console.Error.WriteLine($"Error: {result.ErrorMessage}");

        return result.Success ? 0 : 1;
    }

    // ── Help ─────────────────────────────────────────────────────────────────

    private static int PrintHelp()
    {
        Console.WriteLine("""
            PIV Tool - YubiKey PIV Management (Non-Interactive Mode)

            USAGE:
              PivTool <command> [options]

            GLOBAL OPTIONS:
              --serial <number>   Select YubiKey by serial number (required if multiple keys connected)
              --json              Output results as JSON (machine-readable)

            COMMANDS:
              device-info
              pin-verify          --pin <pin>
              pin-change          --pin <current> --new-pin <new>
              puk-change          --puk <current> --new-puk <new>
              pin-unblock         --puk <puk> --new-pin <new>
              pin-retries
              mgmt-key-auth       --management-key <hex>
              mgmt-key-change     --management-key <hex> --new-management-key <hex>
                                  [--algorithm 3des|aes128|aes192|aes256] [--require-touch]
              key-generate        --slot <9a|9c|9d|9e> --algorithm <rsa2048|rsa3072|rsa4096|eccp256|eccp384|ed25519|x25519>
                                  --management-key <hex>
                                  [--pin-policy default|never|once|always]
                                  [--touch-policy default|never|always|cached]
              cert-view           --slot <9a|9c|9d|9e>
              cert-export         --slot <9a|9c|9d|9e> [--output <path>]
              cert-import         --slot <9a|9c|9d|9e> --cert-path <path> --management-key <hex> [--compress]
              cert-self-sign      --slot <9a|9c|9d|9e> --management-key <hex> --pin <pin>
                                  [--subject <dn>] [--validity-days <n>]
              cert-csr            --slot <9a|9c|9d|9e> --pin <pin> [--subject <dn>] [--output <path>]
              cert-delete         --slot <9a|9c|9d|9e> --management-key <hex> [--force]
              sign                --slot <9a|9c|9d|9e> --input <path> --pin <pin>
                                  [--hash sha256|sha384|sha512] [--output <path>]
              decrypt             --slot <9a|9c|9d|9e> --input <path> --pin <pin> [--output <path>]
              verify-signature    --slot <9a|9c|9d|9e> --data <path> --signature <path>
                                  [--hash sha256|sha384|sha512]
              attest              --slot <9a|9c|9d|9e> [--output <path>]
              attest-intermediate [--output <path>]
              slot-info           [--slot <9a|9c|9d|9e>]
              reset               [--force]
              help

            EXAMPLES:
              PivTool device-info --json
              PivTool pin-verify --pin 123456 --json
              PivTool key-generate --slot 9a --algorithm eccp256 --management-key 010203040506070801020304050607080102030405060708 --json
              PivTool cert-self-sign --slot 9a --management-key 010203040506070801020304050607080102030405060708 --pin 123456 --json
              PivTool slot-info --json
              PivTool reset --force --json

            SECURITY NOTE:
              Credentials supplied via --pin, --puk, and --management-key flags are visible
              in process listings. Acceptable for testing; never use in production.
            """);
        return 0;
    }

    private static int PrintUnknownCommand(string command)
    {
        Console.Error.WriteLine($"Error: Unknown command '{command}'. Run 'PivTool help' for usage.");
        return 1;
    }

    // ── Utility helpers ───────────────────────────────────────────────────────

    private static int WriteResult(PivExamples.Results.PinOperationResult result, bool useJson)
    {
        if (useJson)
        {
            JsonOutput.Write(result);
        }
        else if (result.Success)
        {
            Console.WriteLine(result.RetriesRemaining.HasValue
                ? $"Success. Retries remaining: {result.RetriesRemaining}"
                : "Success.");
        }
        else
        {
            Console.Error.WriteLine($"Error: {result.ErrorMessage}");
        }

        return result.Success ? 0 : 1;
    }

    private static void ZeroAndReturn(params byte[]?[] buffers)
    {
        foreach (var buf in buffers)
        {
            if (buf is not null)
            {
                CryptographicOperations.ZeroMemory(buf);
            }
        }
    }
}