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

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;

namespace Yubico.YubiKit.Management.Backend;

/// <summary>
/// Backend implementation for Management operations over OTP HID (8-byte feature reports).
/// Encodes operations as OTP slot commands with CRC validation.
/// </summary>
internal sealed class OtpBackend(IOtpHidProtocol otpProtocol) : IManagementBackend
{
    private readonly IOtpHidProtocol _otpProtocol = otpProtocol ?? throw new ArgumentNullException(nameof(otpProtocol));

    public async ValueTask<FirmwareVersion?> InitializeAsync(CancellationToken cancellationToken)
    {
        var status = await _otpProtocol.ReadStatusAsync(cancellationToken).ConfigureAwait(false);
        return _otpProtocol.FirmwareVersion ?? new FirmwareVersion(status.Span[0], status.Span[1], status.Span[2]);
    }

    public async ValueTask WriteConfigAsync(ReadOnlyMemory<byte> config, CancellationToken cancellationToken)
    {
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

    public ValueTask DeviceResetAsync(CancellationToken cancellationToken) =>
        throw new NotSupportedException("Device reset is only available over SmartCard (CCID) connections.");
}