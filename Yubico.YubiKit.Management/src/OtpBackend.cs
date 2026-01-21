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

using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Hid.Otp;

namespace Yubico.YubiKit.Management;

/// <summary>
/// Backend implementation for Management operations over OTP HID (8-byte feature reports).
/// Encodes operations as OTP slot commands with CRC validation.
/// </summary>
internal sealed class OtpBackend(IOtpHidProtocol otpProtocol) : IManagementBackend
{
    private readonly IOtpHidProtocol _otpProtocol = otpProtocol ?? throw new ArgumentNullException(nameof(otpProtocol));

    public async ValueTask<byte[]> ReadConfigAsync(int page, CancellationToken cancellationToken)
    {
        // Send CMD_YK4_CAPABILITIES (0x13) with page payload
        // For page 0, send empty payload (Java sends null which becomes 64 zeros)
        // For page > 0, send single byte with page number
        var pagePayload = page == 0 ? ReadOnlyMemory<byte>.Empty : new byte[] { (byte)page };
        var response = await _otpProtocol.SendAndReceiveAsync(
                OtpConstants.CmdYk4Capabilities, 
                pagePayload, 
                cancellationToken)
            .ConfigureAwait(false);

        // Response format: [length][TLV data...][CRC16]
        // Verify CRC: checkCrc(response, response[0] + 1 + 2)
        // response[0] is the length of TLV data, +1 for the length byte, +2 for CRC
        var totalLength = response.Span[0] + 1 + 2;
        if (!ChecksumUtils.CheckCrc(response.Span, totalLength))
        {
            throw new BadResponseException("Invalid CRC in OTP response");
        }

        // Return data without CRC: response[0..length+1]
        return response[..(response.Span[0] + 1)].ToArray();
    }

    public async ValueTask WriteConfigAsync(byte[] config, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);

        await _otpProtocol.SendAndReceiveAsync(
                OtpConstants.CmdYk4SetDeviceInfo, 
                config, 
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask SetModeAsync(byte[] data, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(data);

        await _otpProtocol.SendAndReceiveAsync(
                OtpConstants.CmdDeviceConfig, 
                data, 
                cancellationToken)
            .ConfigureAwait(false);
    }

    public ValueTask DeviceResetAsync(CancellationToken cancellationToken)
    {
        // Device reset is only supported over CCID, not HID
        throw new NotSupportedException("Device reset is only available over SmartCard (CCID) connections.");
    }

    public void Dispose()
    {
        // Backend doesn't own the protocol - ManagementSession handles disposal
    }
}
