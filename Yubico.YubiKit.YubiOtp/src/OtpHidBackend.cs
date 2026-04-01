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
using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Hid.Otp;

namespace Yubico.YubiKit.YubiOtp;

/// <summary>
/// Backend implementation for YubiOTP operations over OTP HID (8-byte feature reports).
/// Delegates to <see cref="IOtpHidProtocol"/> and validates CRC on responses.
/// </summary>
internal sealed class OtpHidBackend : IYubiOtpBackend
{
    private readonly IOtpHidProtocol _protocol;
    private readonly ILogger _logger;

    public OtpHidBackend(IOtpHidProtocol protocol, ILogger? logger = null)
    {
        _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
        _logger = logger ?? NullLogger.Instance;
    }

    public async ValueTask<ReadOnlyMemory<byte>> WriteUpdateAsync(
        ConfigSlot slot,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("OtpHidBackend WriteUpdate: slot={Slot}", slot);

        var response = await _protocol.SendAndReceiveAsync((byte)slot, data, cancellationToken)
            .ConfigureAwait(false);

        return response;
    }

    public async ValueTask<ReadOnlyMemory<byte>> SendAndReceiveAsync(
        ConfigSlot slot,
        ReadOnlyMemory<byte> data,
        int expectedLength,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("OtpHidBackend SendAndReceive: slot={Slot}, expectedLength={Length}", slot, expectedLength);

        var response = await _protocol.SendAndReceiveAsync((byte)slot, data, cancellationToken)
            .ConfigureAwait(false);

        // CRC validation: response contains [data...][crc_lo][crc_hi]
        var totalLength = expectedLength + 2;
        if (response.Length < totalLength)
        {
            throw new BadResponseException(
                $"Response too short for CRC validation. Expected at least {totalLength} bytes, got {response.Length}.");
        }

        if (!ChecksumUtils.CheckCrc(response.Span, totalLength))
        {
            throw new BadResponseException("Invalid CRC in OTP HID response.");
        }

        return response[..expectedLength];
    }
}
