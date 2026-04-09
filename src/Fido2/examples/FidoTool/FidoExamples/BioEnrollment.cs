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
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Fido2.BioEnrollment;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;

namespace Yubico.YubiKit.Fido2.Examples.FidoTool.FidoExamples;

/// <summary>
/// Biometric (fingerprint) enrollment operations.
/// Requires firmware 5.2+ and the bioEnroll authenticator option.
/// </summary>
public static class BioEnrollmentExample
{
    /// <summary>
    /// Result of a sensor info query.
    /// </summary>
    public sealed record SensorInfoResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public FingerprintSensorInfo? SensorInfo { get; init; }

        public static SensorInfoResult Succeeded(FingerprintSensorInfo info) =>
            new() { Success = true, SensorInfo = info };

        public static SensorInfoResult Failed(string error) =>
            new() { Success = false, ErrorMessage = error };
    }

    /// <summary>
    /// Result of enumerating enrolled fingerprints.
    /// </summary>
    public sealed record EnumerateResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public IReadOnlyList<TemplateInfo> Templates { get; init; } = [];

        public static EnumerateResult Succeeded(IReadOnlyList<TemplateInfo> templates) =>
            new() { Success = true, Templates = templates };

        public static EnumerateResult Failed(string error) =>
            new() { Success = false, ErrorMessage = error };
    }

    /// <summary>
    /// Result of a bio enrollment operation.
    /// </summary>
    public sealed record BioResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }

        public static BioResult Succeeded() => new() { Success = true };
        public static BioResult Failed(string error) => new() { Success = false, ErrorMessage = error };
    }

    /// <summary>
    /// Result of a full fingerprint enrollment process.
    /// </summary>
    public sealed record EnrollResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public ReadOnlyMemory<byte> TemplateId { get; init; }

        public static EnrollResult Succeeded(ReadOnlyMemory<byte> templateId) =>
            new() { Success = true, TemplateId = templateId };

        public static EnrollResult Failed(string error) =>
            new() { Success = false, ErrorMessage = error };
    }

    /// <summary>
    /// Gets fingerprint sensor information.
    /// </summary>
    public static async Task<SensorInfoResult> GetSensorInfoAsync(
        IYubiKey yubiKey,
        ReadOnlyMemory<byte> pinUtf8,
        CancellationToken cancellationToken = default)
    {
        byte[]? pinToken = null;
        try
        {
            await using var session = await yubiKey.CreateFidoSessionAsync(
                cancellationToken: cancellationToken);

            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                pinUtf8,
                PinUvAuthTokenPermissions.BioEnrollment,
                cancellationToken: cancellationToken);

            var bio = new FingerprintBioEnrollment(session, protocol, pinToken);
            var sensorInfo = await bio.GetFingerprintSensorInfoAsync(cancellationToken);

            return SensorInfoResult.Succeeded(sensorInfo);
        }
        catch (CtapException ex)
        {
            return SensorInfoResult.Failed(MapCtapError(ex));
        }
        catch (Exception ex)
        {
            return SensorInfoResult.Failed($"Failed to get sensor info: {ex.Message}");
        }
        finally
        {
            if (pinToken is not null)
            {
                CryptographicOperations.ZeroMemory(pinToken);
            }
        }
    }

    /// <summary>
    /// Enrolls a fingerprint with a multi-sample capture loop.
    /// Calls <paramref name="onSampleCaptured"/> after each sample for progress reporting.
    /// </summary>
    public static async Task<EnrollResult> EnrollFingerprintAsync(
        IYubiKey yubiKey,
        ReadOnlyMemory<byte> pinUtf8,
        string? friendlyName,
        Action<int, int, FingerprintSampleStatus>? onSampleCaptured = null,
        CancellationToken cancellationToken = default)
    {
        byte[]? pinToken = null;
        try
        {
            await using var session = await yubiKey.CreateFidoSessionAsync(
                cancellationToken: cancellationToken);

            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                pinUtf8,
                PinUvAuthTokenPermissions.BioEnrollment,
                cancellationToken: cancellationToken);

            var bio = new FingerprintBioEnrollment(session, protocol, pinToken);

            // Begin enrollment - first sample
            var result = await bio.EnrollBeginAsync(cancellationToken: cancellationToken);
            var templateId = result.TemplateId;
            var sampleNumber = 1;

            onSampleCaptured?.Invoke(sampleNumber, result.RemainingSamples, result.LastSampleStatus);

            // Capture remaining samples
            while (!result.IsComplete && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    result = await bio.EnrollCaptureNextSampleAsync(
                        templateId, cancellationToken: cancellationToken);
                    sampleNumber++;

                    onSampleCaptured?.Invoke(sampleNumber, result.RemainingSamples, result.LastSampleStatus);
                }
                catch (CtapException ex) when (ex.Status == CtapStatus.UserActionTimeout)
                {
                    onSampleCaptured?.Invoke(sampleNumber, result.RemainingSamples,
                        FingerprintSampleStatus.NoUserActivity);
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                await bio.EnrollCancelAsync(CancellationToken.None);
                return EnrollResult.Failed("Enrollment cancelled.");
            }

            // Set friendly name if provided
            if (!string.IsNullOrWhiteSpace(friendlyName))
            {
                await bio.SetFriendlyNameAsync(templateId, friendlyName, cancellationToken);
            }

            return EnrollResult.Succeeded(templateId);
        }
        catch (CtapException ex)
        {
            return EnrollResult.Failed(MapCtapError(ex));
        }
        catch (Exception ex)
        {
            return EnrollResult.Failed($"Failed to enroll fingerprint: {ex.Message}");
        }
        finally
        {
            if (pinToken is not null)
            {
                CryptographicOperations.ZeroMemory(pinToken);
            }
        }
    }

    /// <summary>
    /// Enumerates all enrolled fingerprint templates.
    /// </summary>
    public static async Task<EnumerateResult> EnumerateEnrollmentsAsync(
        IYubiKey yubiKey,
        ReadOnlyMemory<byte> pinUtf8,
        CancellationToken cancellationToken = default)
    {
        byte[]? pinToken = null;
        try
        {
            await using var session = await yubiKey.CreateFidoSessionAsync(
                cancellationToken: cancellationToken);

            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                pinUtf8,
                PinUvAuthTokenPermissions.BioEnrollment,
                cancellationToken: cancellationToken);

            var bio = new FingerprintBioEnrollment(session, protocol, pinToken);
            var templates = await bio.EnumerateEnrollmentsAsync(cancellationToken);

            return EnumerateResult.Succeeded(templates);
        }
        catch (CtapException ex)
        {
            return EnumerateResult.Failed(MapCtapError(ex));
        }
        catch (Exception ex)
        {
            return EnumerateResult.Failed($"Failed to enumerate enrollments: {ex.Message}");
        }
        finally
        {
            if (pinToken is not null)
            {
                CryptographicOperations.ZeroMemory(pinToken);
            }
        }
    }

    /// <summary>
    /// Renames a fingerprint enrollment.
    /// </summary>
    public static async Task<BioResult> RenameEnrollmentAsync(
        IYubiKey yubiKey,
        ReadOnlyMemory<byte> pinUtf8,
        ReadOnlyMemory<byte> templateId,
        string friendlyName,
        CancellationToken cancellationToken = default)
    {
        byte[]? pinToken = null;
        try
        {
            await using var session = await yubiKey.CreateFidoSessionAsync(
                cancellationToken: cancellationToken);

            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                pinUtf8,
                PinUvAuthTokenPermissions.BioEnrollment,
                cancellationToken: cancellationToken);

            var bio = new FingerprintBioEnrollment(session, protocol, pinToken);
            await bio.SetFriendlyNameAsync(templateId, friendlyName, cancellationToken);

            return BioResult.Succeeded();
        }
        catch (CtapException ex)
        {
            return BioResult.Failed(MapCtapError(ex));
        }
        catch (Exception ex)
        {
            return BioResult.Failed($"Failed to rename enrollment: {ex.Message}");
        }
        finally
        {
            if (pinToken is not null)
            {
                CryptographicOperations.ZeroMemory(pinToken);
            }
        }
    }

    /// <summary>
    /// Removes a fingerprint enrollment.
    /// </summary>
    public static async Task<BioResult> RemoveEnrollmentAsync(
        IYubiKey yubiKey,
        ReadOnlyMemory<byte> pinUtf8,
        ReadOnlyMemory<byte> templateId,
        CancellationToken cancellationToken = default)
    {
        byte[]? pinToken = null;
        try
        {
            await using var session = await yubiKey.CreateFidoSessionAsync(
                cancellationToken: cancellationToken);

            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            pinToken = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                pinUtf8,
                PinUvAuthTokenPermissions.BioEnrollment,
                cancellationToken: cancellationToken);

            var bio = new FingerprintBioEnrollment(session, protocol, pinToken);
            await bio.RemoveEnrollmentAsync(templateId, cancellationToken);

            return BioResult.Succeeded();
        }
        catch (CtapException ex)
        {
            return BioResult.Failed(MapCtapError(ex));
        }
        catch (Exception ex)
        {
            return BioResult.Failed($"Failed to remove enrollment: {ex.Message}");
        }
        finally
        {
            if (pinToken is not null)
            {
                CryptographicOperations.ZeroMemory(pinToken);
            }
        }
    }

    /// <summary>
    /// Checks if the authenticator supports bio enrollment.
    /// </summary>
    public static async Task<bool> IsSupported(
        IYubiKey yubiKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var session = await yubiKey.CreateFidoSessionAsync(
                cancellationToken: cancellationToken);
            var info = await session.GetInfoAsync(cancellationToken);
            return info.Options.TryGetValue("bioEnroll", out var supported) && supported;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets a user-friendly description of a fingerprint sample status.
    /// </summary>
    public static string FormatSampleStatus(FingerprintSampleStatus status) =>
        status switch
        {
            FingerprintSampleStatus.Good => "Good sample captured",
            FingerprintSampleStatus.TooHigh => "Finger too high on sensor",
            FingerprintSampleStatus.TooLow => "Finger too low on sensor",
            FingerprintSampleStatus.TooLeft => "Finger too far left",
            FingerprintSampleStatus.TooRight => "Finger too far right",
            FingerprintSampleStatus.TooFast => "Finger moved too fast",
            FingerprintSampleStatus.TooSlow => "Finger moved too slow",
            FingerprintSampleStatus.PoorQuality => "Poor quality sample",
            FingerprintSampleStatus.TooSkewed => "Finger too skewed",
            FingerprintSampleStatus.TooShort => "Touch too short",
            FingerprintSampleStatus.MergeFailure => "Merge failure",
            FingerprintSampleStatus.StorageFull => "Fingerprint storage full",
            FingerprintSampleStatus.NoUserActivity => "No finger detected (timeout)",
            FingerprintSampleStatus.NoUserPresence => "No user presence",
            _ => $"Unknown status: {status}"
        };

    private static string MapCtapError(CtapException ex) =>
        ex.Status switch
        {
            CtapStatus.UserActionTimeout =>
                "Operation timed out. Please try again and touch the sensor when prompted.",
            CtapStatus.PinInvalid => "The PIN is incorrect.",
            CtapStatus.PinBlocked => "The PIN is blocked. The authenticator must be reset.",
            CtapStatus.PinAuthInvalid => "PIN authentication failed.",
            CtapStatus.PinNotSet => "No PIN is set. Set a PIN first.",
            CtapStatus.NotAllowed => "Operation not allowed. Bio enrollment may not be supported.",
            CtapStatus.InvalidCommand => "Bio enrollment is not supported on this authenticator.",
            _ => $"CTAP error: {ex.Message} (0x{(byte)ex.Status:X2})"
        };
}
