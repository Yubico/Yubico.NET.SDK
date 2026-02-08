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

using System.Buffers.Binary;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.Interfaces;

/// <summary>
/// Delegate for reading device identity from a YubiKey reference.
/// </summary>
/// <remarks>
/// <para>
/// This delegate is used by <see cref="Yubico.YubiKit.Core.YubiKey.ICompositeYubiKeyFactory"/>
/// to read identity information from transport references for device correlation.
/// </para>
/// <para>
/// The Management module provides the actual implementation that reads <c>DeviceInfo</c>
/// via <c>ManagementSession</c>. Core registers a default no-op implementation that
/// returns <c>null</c> for all references.
/// </para>
/// </remarks>
/// <param name="reference">The transport reference to read from.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>The device identity, or <c>null</c> if it cannot be read.</returns>
public delegate Task<IDeviceIdentity?> IdentityReaderDelegate(
    IYubiKeyReference reference,
    CancellationToken cancellationToken);

/// <summary>
/// Represents the identity and capability information of a YubiKey device.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides read-only access to device information used for:
/// <list type="bullet">
/// <item>Device identification (serial number, firmware version)</item>
/// <item>Form factor and capability detection</item>
/// <item>Device correlation across multiple transports</item>
/// <item>Configuration fingerprinting for devices without serial numbers</item>
/// </list>
/// </para>
/// <para>
/// The interface is designed to be implemented by device info readers from
/// the Management module while being consumable from Core for device correlation.
/// </para>
/// </remarks>
public interface IDeviceIdentity
{
    /// <summary>
    /// Gets the device serial number, or <c>null</c> if unavailable.
    /// </summary>
    /// <remarks>
    /// Serial number may be null for Security Keys or devices with serial number
    /// visibility disabled. When null, device correlation falls back to 
    /// configuration fingerprinting.
    /// </remarks>
    int? SerialNumber { get; }

    /// <summary>
    /// Gets the device firmware version.
    /// </summary>
    FirmwareVersion FirmwareVersion { get; }

    /// <summary>
    /// Gets the physical form factor of the device.
    /// </summary>
    FormFactor FormFactor { get; }

    /// <summary>
    /// Gets the capabilities supported over USB transport.
    /// </summary>
    DeviceCapabilities UsbSupported { get; }

    /// <summary>
    /// Gets the capabilities supported over NFC transport.
    /// </summary>
    DeviceCapabilities NfcSupported { get; }

    /// <summary>
    /// Gets the capabilities currently enabled over USB transport.
    /// </summary>
    DeviceCapabilities UsbEnabled { get; }

    /// <summary>
    /// Gets the capabilities currently enabled over NFC transport.
    /// </summary>
    DeviceCapabilities NfcEnabled { get; }

    /// <summary>
    /// Gets the auto-eject timeout in seconds for CCID touch-eject feature.
    /// </summary>
    ushort AutoEjectTimeout { get; }

    /// <summary>
    /// Gets the challenge-response timeout configuration.
    /// </summary>
    ReadOnlyMemory<byte> ChallengeResponseTimeout { get; }

    /// <summary>
    /// Gets miscellaneous device flags.
    /// </summary>
    DeviceFlags DeviceFlags { get; }

    /// <summary>
    /// Gets a value indicating whether NFC is restricted on this device.
    /// </summary>
    bool IsNfcRestricted { get; }

    /// <summary>
    /// Gets the union of all capabilities supported across any transport.
    /// </summary>
    /// <remarks>
    /// This is a computed property that returns <c>UsbSupported | NfcSupported</c>.
    /// </remarks>
    DeviceCapabilities SupportedCapabilities => UsbSupported | NfcSupported;

    /// <summary>
    /// Computes a configuration fingerprint for device correlation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The fingerprint is computed from configuration properties that are stable
    /// across transports but unique to a physical device's configuration:
    /// </para>
    /// <list type="bullet">
    /// <item>USB enabled capabilities</item>
    /// <item>NFC enabled capabilities</item>
    /// <item>Auto-eject timeout</item>
    /// <item>Challenge-response timeout</item>
    /// <item>Device flags</item>
    /// <item>NFC restricted flag</item>
    /// </list>
    /// <para>
    /// This is used as a fallback correlation key when serial number is unavailable.
    /// </para>
    /// </remarks>
    /// <returns>An 8-character lowercase hex string representing the fingerprint.</returns>
    string ComputeConfigFingerprint()
    {
        // Allocate buffer for fingerprint data on stack
        // Layout: UsbEnabled (2) + NfcEnabled (2) + AutoEjectTimeout (2) + DeviceFlags (1) + IsNfcRestricted (1) + ChallengeResponseTimeout (variable)
        int challengeTimeoutLength = ChallengeResponseTimeout.Length;
        int bufferSize = 8 + challengeTimeoutLength;
        
        Span<byte> buffer = stackalloc byte[bufferSize];
        
        // Write fixed-size fields
        BinaryPrimitives.WriteUInt16BigEndian(buffer[..2], (ushort)UsbEnabled);
        BinaryPrimitives.WriteUInt16BigEndian(buffer[2..4], (ushort)NfcEnabled);
        BinaryPrimitives.WriteUInt16BigEndian(buffer[4..6], AutoEjectTimeout);
        buffer[6] = (byte)DeviceFlags;
        buffer[7] = IsNfcRestricted ? (byte)1 : (byte)0;
        
        // Copy challenge-response timeout
        if (challengeTimeoutLength > 0)
        {
            ChallengeResponseTimeout.Span.CopyTo(buffer[8..]);
        }
        
        // Compute SHA256 hash
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(buffer, hash);
        
        // Return first 4 bytes as hex (8 characters)
        return Convert.ToHexString(hash[..4]).ToLowerInvariant();
    }
}
