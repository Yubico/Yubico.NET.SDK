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
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.YubiOtp;

/// <summary>
/// Backend implementation for YubiOTP operations over SmartCard (CCID/NFC).
/// Encodes operations as ISO 7816 APDUs with programming sequence validation.
/// </summary>
internal sealed class SmartCardBackend : IYubiOtpBackend
{
    private static readonly ILogger Logger =
        YubiKitLogging.LoggerFactory.CreateLogger<SmartCardBackend>();

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

        // If response data is empty, read status separately (some firmware versions)
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

        Logger.LogDebug("SmartCardBackend SendAndReceive: slot={Slot}, expectedLength={Length}", slot, expectedLength);

        var response = await _protocol.TransmitAndReceiveAsync(apdu, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (response.Data.Length < expectedLength)
        {
            throw new BadResponseException(
                $"Expected {expectedLength} bytes from slot {slot}, got {response.Data.Length}.");
        }

        return response.Data[..expectedLength];
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
    /// Validates that the programming sequence counter incremented by 1 after a write.
    /// Handles edge cases: sequence rollover to 0, and firmware 5.0.0-5.4.3 which
    /// does not always update the programming sequence.
    /// </summary>
    private void ValidateProgrammingSequence(ReadOnlySpan<byte> statusBytes)
    {
        if (statusBytes.Length < YubiOtpConstants.StatusBytesLength)
        {
            return;
        }

        var newProgSeq = statusBytes[3];

        // Firmware 5.0.0 through 5.4.3 do not reliably update prog_seq
        if (_firmwareVersion >= new FirmwareVersion(5, 0, 0)
            && _firmwareVersion < new FirmwareVersion(5, 5, 0))
        {
            _lastProgSeq = newProgSeq;
            return;
        }

        // prog_seq should increment by 1, with rollover: 0 follows any value
        var expectedProgSeq = (byte)((_lastProgSeq + 1) & 0xFF);

        if (newProgSeq != 0 && newProgSeq != expectedProgSeq)
        {
            throw new InvalidOperationException(
                $"Programming sequence validation failed. Expected {expectedProgSeq}, got {newProgSeq}. " +
                "The YubiKey may have rejected the configuration.");
        }

        _lastProgSeq = newProgSeq;
    }

    public void Dispose()
    {
        // Backend doesn't own the protocol - YubiOtpSession handles disposal
    }
}
