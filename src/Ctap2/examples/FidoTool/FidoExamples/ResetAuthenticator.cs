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

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.Ctap2.Examples.FidoTool.FidoExamples;

/// <summary>
/// Factory reset of the FIDO application on the authenticator.
/// Invalidates generated credentials (including non-discoverable/U2F), erases discoverable credentials
/// and clears the PIN; named storage/configuration effects depend on the applicable protocol/product.
/// </summary>
/// <remarks>
/// This helper uses the selected device throughout. It does not power-cycle, enforce a local reset timer or roll back.
/// CTAP 2.1/2.3 require receipt within ten seconds of power-up for displayless devices; Yubico documents
/// five seconds before firmware 5.5.2 and ten on 5.5.4+, with 5.5.2/5.5.3 unspecified in that passage.
/// Separate transport/product presence requirements apply. Current executable timing/error prompts are
/// overgeneralized and remain deferred; see the sample README's qualified reset procedure. General catches
/// flatten cancellation/error provenance. A failed result is not evidence that no destructive action occurred.
/// </remarks>
public static class ResetAuthenticator
{
    /// <summary>
    /// Result of a reset operation.
    /// </summary>
    public sealed record ResetResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }

        public static ResetResult Succeeded() => new() { Success = true };
        public static ResetResult Failed(string error) => new() { Success = false, ErrorMessage = error };
    }

    // A GetInfo fingerprint is only a consistency check; identical models can report matching fields.
    internal sealed record ResetPreflightInfo(IYubiKey Device, ConnectionType Transport, int? SerialNumber,
        AuthenticatorInfo Info)
    {
        public bool LongTouchForReset => Info.LongTouchForReset ?? false;
        public IReadOnlyList<string> TransportsForReset => Info.TransportsForReset;

        public bool MatchesFingerprint(AuthenticatorInfo actual) =>
            Info.Aaguid.Span.SequenceEqual(actual.Aaguid.Span) &&
            Info.FirmwareVersion == actual.FirmwareVersion &&
            Info.Versions.Order(StringComparer.Ordinal).SequenceEqual(actual.Versions.Order(StringComparer.Ordinal));
    }

    internal static async Task<ResetPreflightInfo> GetPreflightInfoAsync(
        IYubiKey selected, ConnectionType preferredConnection, int? serialNumber, CancellationToken cancellationToken)
    {
        await using var session = await selected.CreateCtap2SessionAsync(
            new SessionCreationOptions { PreferredConnectionType = preferredConnection }, cancellationToken);
        var info = await session.GetInfoAsync(cancellationToken);
        return new ResetPreflightInfo(selected, preferredConnection, serialNumber, info);
    }

    internal static async Task<ResetResult> ResetAsync(ResetPreflightInfo target, CancellationToken cancellationToken)
    {
        try
        {
            var options = new SessionCreationOptions { PreferredConnectionType = target.Transport };
            await using IConnection connection = target.Transport switch
            {
                ConnectionType.SmartCard => await target.Device.ConnectAsync<ISmartCardConnection>(cancellationToken),
                ConnectionType.HidFido => await target.Device.ConnectAsync<IFidoHidConnection>(cancellationToken),
                _ => throw new NotSupportedException("FIDO reset requires SmartCard or FIDO HID.")
            };

            // Both sessions borrow this handle. Dispose Management before CTAP selects its applet.
            // Reopening the interface here could reset a different card than the one verified.
            DeviceInfo? deviceInfo;
            if (target.SerialNumber is int serial)
            {
                await using var management = await ManagementSession.CreateAsync(connection, options, cancellationToken);
                deviceInfo = await management.GetDeviceInfoAsync(cancellationToken);
                if (deviceInfo.Value.SerialNumber != serial)
                    return ResetResult.Failed("Reinserted device did not report the confirmed serial number. Reset not sent.");
            }
            else
            {
                deviceInfo = target.Device.CachedDeviceInfo;
            }

            await using var session = await Ctap2Session.CreateAsync(connection, options, deviceInfo, cancellationToken);
            if (target.SerialNumber is null && !target.MatchesFingerprint(await session.GetInfoAsync(cancellationToken)))
                return ResetResult.Failed("Reinserted GetInfo fingerprint differs from the confirmed device. Reset not sent.");

            await session.ResetAsync(cancellationToken);
            return ResetResult.Succeeded();
        }
        catch (CtapException ex)
        {
            return ResetResult.Failed(MapCtapError(ex));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ResetResult.Failed($"Failed to reset authenticator: {ex.Message}");
        }
    }

    private static string MapCtapError(CtapException ex) =>
        ex.Status switch
        {
            CtapStatus.UserActionTimeout =>
                "Reset timed out. You need to touch your YubiKey to confirm the reset.",
            CtapStatus.NotAllowed =>
                "Reset not allowed. Reset must be triggered within 5 seconds after the YubiKey is inserted.",
            CtapStatus.OperationDenied =>
                "Reset was denied. Remove the YubiKey, re-insert it, and try again within 5 seconds.",
            CtapStatus.PinAuthBlocked =>
                "Reset not allowed. Remove the YubiKey, re-insert it, and try again within 5 seconds.",
            _ => $"CTAP error: {ex.Message} (0x{(byte)ex.Status:X2})"
        };
}