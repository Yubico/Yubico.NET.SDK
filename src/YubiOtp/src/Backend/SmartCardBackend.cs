// Copyright 2025 Yubico AB
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

using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;

namespace Yubico.YubiKit.YubiOtp.Backend;

/// <summary>
/// Backend implementation for YubiOTP operations over SmartCard (CCID/NFC).
/// Encodes operations as ISO 7816 APDUs with programming sequence validation.
/// </summary>
internal sealed class SmartCardBackend : IYubiOtpBackend
{
    private static readonly ILogger Logger =
        YubiKitLogging.LoggerFactory.CreateLogger<SmartCardBackend>();

    private static readonly FirmwareVersion Firmware500 = new(5, 0, 0);
    private static readonly FirmwareVersion Firmware543 = new(5, 4, 3);

    private readonly ISmartCardProtocol _protocol;
    private readonly FirmwareVersion _firmwareVersion;
    private byte _lastProgSeq;

    public SmartCardBackend(
        ISmartCardProtocol protocol,
        FirmwareVersion firmwareVersion,
        byte initialProgSeq)
    {
        ArgumentNullException.ThrowIfNull(protocol);
        _protocol = protocol;
        _firmwareVersion = firmwareVersion;
        _lastProgSeq = initialProgSeq;
    }

    public static async ValueTask<YubiOtpInitialization> ReadInitialStateAsync(
        ISmartCardProtocol protocol,
        CancellationToken cancellationToken,
        FirmwareVersion? cachedVersion = null)
    {
        var managementVersion = cachedVersion is null
            ? await TryReadManagementVersionAsync(protocol, cancellationToken).ConfigureAwait(false)
            : null;

        var status = await protocol
            .SelectAsync(ApplicationIds.Otp, cancellationToken)
            .ConfigureAwait(false);

        if (status.Length < YubiOtpConstants.StatusBytesLength)
        {
            throw new BadResponseException(
                $"OTP SELECT returned {status.Length} bytes, expected at least {YubiOtpConstants.StatusBytesLength}.");
        }

        var otpVersion = new FirmwareVersion(status.Span[0], status.Span[1], status.Span[2]);

        var firmwareVersion = managementVersion is not null && otpVersion.Major == 3
            ? (managementVersion > otpVersion ? managementVersion : otpVersion)
            : managementVersion ?? otpVersion;

        return new YubiOtpInitialization(firmwareVersion, status);
    }

    private static async Task<FirmwareVersion?> TryReadManagementVersionAsync(
        ISmartCardProtocol protocol, CancellationToken cancellationToken)
    {
        try
        {
            var response = await protocol.SelectAsync(ApplicationIds.Management, cancellationToken).ConfigureAwait(false);
            return FirmwareVersion.FromSelectResponse(response.Span);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Logger.LogDebug(ex, "Could not get version from Management SELECT, continuing with OTP SELECT.");
            return null;
        }
    }

    public async ValueTask<ReadOnlyMemory<byte>> WriteUpdateAsync(
        ConfigSlot slot,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken)
    {
        var apdu = new ApduCommand
        {
            Cla = 0,
            Ins = YubiOtpConstants.InsConfig,
            P1 = (byte)slot,
            P2 = 0,
            Data = data
        };

        Logger.LogDebug("SmartCardBackend WriteUpdate: slot={Slot}", slot);

        var response = await _protocol.TransmitAndReceiveAsync(apdu, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var statusBytes = response.Data;

        // A short write response needs a separate status read; validate before returning it to the session.
        if (statusBytes.Length < YubiOtpConstants.StatusBytesLength)
        {
            statusBytes = await ReadStatusAsync(cancellationToken).ConfigureAwait(false);
        }

        ValidateProgrammingSequence(statusBytes.Span);

        return statusBytes;
    }

    public async ValueTask<ReadOnlyMemory<byte>> SendAndReceiveAsync(
        ConfigSlot slot,
        ReadOnlyMemory<byte> data,
        int expectedLength,
        UserPresenceNotification userPresenceNotification,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userPresenceNotification);

        Logger.LogDebug("SmartCardBackend SendAndReceive: slot={Slot}, expectedLength={Length}", slot, expectedLength);

        Exception? primaryException = null;
        var outcome = UserPresenceOutcome.Failed;
        ReadOnlyMemory<byte> responseData = default;
        var responseTransferred = false;

        try
        {
            await userPresenceNotification.RequestAsync(cancellationToken).ConfigureAwait(false);

            var apdu = new ApduCommand
            {
                Cla = 0,
                Ins = YubiOtpConstants.InsConfig,
                P1 = (byte)slot,
                P2 = 0,
                Data = data
            };

            var response = await _protocol.TransmitAndReceiveAsync(apdu, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            responseData = response.Data;

            if (responseData.Length < expectedLength)
            {
                throw new BadResponseException(
                    $"Expected {expectedLength} bytes from slot {slot}, got {responseData.Length}.");
            }

            outcome = UserPresenceOutcome.Completed;
            await userPresenceNotification.ResolveAsync(outcome).ConfigureAwait(false);
            if (slot is ConfigSlot.ChalHmac1 or ConfigSlot.ChalHmac2)
            {
                CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(responseData[expectedLength..]).Span);
            }

            responseTransferred = true;
            return responseData[..expectedLength];
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            outcome = UserPresenceOutcome.Cancelled;
            primaryException = ex;
            throw;
        }
        catch (Exception ex)
        {
            primaryException = ex;
            throw;
        }
        finally
        {
            await userPresenceNotification.ResolveAsync(outcome, primaryException).ConfigureAwait(false);
            if (!responseTransferred && !responseData.IsEmpty)
            {
                CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(responseData).Span);
            }
        }
    }

    private async Task<ReadOnlyMemory<byte>> ReadStatusAsync(CancellationToken cancellationToken)
    {
        var apdu = new ApduCommand
        {
            Cla = 0,
            Ins = YubiOtpConstants.InsYk2Status,
            P1 = 0,
            P2 = 0
        };

        var response = await _protocol.TransmitAndReceiveAsync(apdu, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return response.Data;
    }

    /// <summary>
    /// Validates status length and accepts an increment, wraparound, or reset to zero from a positive
    /// predecessor when empty or in the legacy 5.0.0–5.4.2 programming-state window.
    /// </summary>
    private void ValidateProgrammingSequence(ReadOnlySpan<byte> statusBytes)
    {
        if (statusBytes.Length < YubiOtpConstants.StatusBytesLength)
        {
            throw new BadResponseException("OTP write status is shorter than six bytes.");
        }

        var newProgSeq = statusBytes[3];

        var expectedProgSeq = (byte)((_lastProgSeq + 1) & 0xFF);
        bool legacyWindow = _firmwareVersion >= Firmware500 && _firmwareVersion < Firmware543;
        bool acceptReset = newProgSeq == 0 && _lastProgSeq > 0 &&
                           ((statusBytes[4] & 0x1F) == 0 || legacyWindow);

        if (newProgSeq != expectedProgSeq && !acceptReset)
        {
            throw new InvalidOperationException(
                $"Programming sequence validation failed. Expected {expectedProgSeq}, got {newProgSeq}. " +
                "The YubiKey may have rejected the configuration.");
        }

        _lastProgSeq = newProgSeq;
    }

}