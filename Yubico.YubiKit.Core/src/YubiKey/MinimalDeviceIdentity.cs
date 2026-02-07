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

using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.Core.YubiKey;

/// <summary>
/// Minimal device identity for uncorrelatable references when identity reading fails or returns null.
/// </summary>
internal sealed class MinimalDeviceIdentity(string deviceId) : IDeviceIdentity
{
    public int? SerialNumber => null;
    public FirmwareVersion FirmwareVersion => FirmwareVersion.Default;
    public FormFactor FormFactor => FormFactor.Unknown;
    public DeviceCapabilities UsbSupported => DeviceCapabilities.None;
    public DeviceCapabilities NfcSupported => DeviceCapabilities.None;
    public DeviceCapabilities UsbEnabled => DeviceCapabilities.None;
    public DeviceCapabilities NfcEnabled => DeviceCapabilities.None;
    public ushort AutoEjectTimeout => 0;
    public ReadOnlyMemory<byte> ChallengeResponseTimeout => ReadOnlyMemory<byte>.Empty;
    public DeviceFlags DeviceFlags => DeviceFlags.None;
    public bool IsNfcRestricted => false;

    /// <summary>
    /// Overrides default fingerprint computation because MinimalDeviceIdentity has all-default
    /// config values, which would produce identical fingerprints for all instances.
    /// Using deviceId ensures each uncorrelatable reference becomes a unique singleton composite.
    /// </summary>
    public string ComputeConfigFingerprint()
    {
        Span<byte> hash = stackalloc byte[32];
        var bytes = Encoding.UTF8.GetBytes(deviceId);
        SHA256.HashData(bytes, hash);
        return Convert.ToHexString(hash[..4]).ToLowerInvariant();
    }
}
