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

using System.Collections.ObjectModel;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Utils;

namespace Yubico.YubiKit.Management;

/// <summary>
///     Configuration of a YubiKey that can be altered via the Management application.
/// </summary>
public sealed record DeviceConfig
{
    // Flag constants
    private const byte FlagEject = 0x80;
    private const byte FlagRemoteWakeup = 0x40;

    // Internal TLV tags
    private const byte TagUsbEnabled = 0x03;
    private const byte TagAutoEjectTimeout = 0x06;
    private const byte TagChallengeResponseTimeout = 0x07;
    private const byte TagDeviceFlags = 0x08;
    private const byte TagNfcEnabled = 0x0E;
    private const byte TagConfigurationLock = 0x0A;
    private const byte TagUnlock = 0x0B;
    private const byte TagReboot = 0x0C;
    private const byte TagNfcRestricted = 0x17;

    public required IReadOnlyDictionary<Transport, int> EnabledCapabilities { get; init; }

    /// <summary>Timeout in seconds for CCID-only mode with FLAG_EJECT enabled.</summary>
    public ushort? AutoEjectTimeout { get; init; }

    /// <summary>Timeout in seconds for YubiOTP challenge-response waiting for touch.</summary>
    public byte? ChallengeResponseTimeout { get; init; }

    /// <summary>Device flags (FLAG_EJECT, FLAG_REMOTE_WAKEUP).</summary>
    public byte? DeviceFlags { get; init; }

    /// <summary>Whether NFC is restricted.</summary>
    public bool? NfcRestricted { get; init; }

    /// <summary>
    ///     Gets enabled capabilities for a transport.
    ///     Returns null if transport not supported or status not readable (e.g., YubiKey 4 series USB).
    /// </summary>
    public int? GetEnabledCapabilities(Transport transport) =>
        EnabledCapabilities.TryGetValue(transport, out var caps) ? caps : null;

    /// <summary>Serializes to wire format (TLV-encoded bytes).</summary>
    public Memory<byte> GetBytes(bool reboot, byte[]? currentLockCode, byte[]? newLockCode)
    {
        var values = new Dictionary<int, byte[]?>();

        if (reboot)
            values.Add(TagReboot, null);

        if (currentLockCode is not null)
            values.Add(TagUnlock, currentLockCode);

        // USB capabilities (16-bit big-endian)
        if (EnabledCapabilities.TryGetValue(Transport.Usb, out var usbEnabled))
            values.Add(TagUsbEnabled, [(byte)(usbEnabled >> 8), (byte)usbEnabled]);

        // NFC capabilities (16-bit big-endian)
        if (EnabledCapabilities.TryGetValue(Transport.Nfc, out var nfcEnabled))
            values.Add(TagNfcEnabled, [(byte)(nfcEnabled >> 8), (byte)nfcEnabled]);

        // Auto-eject timeout (16-bit big-endian, unsigned)
        if (AutoEjectTimeout.HasValue)
        {
            var timeout = AutoEjectTimeout.Value;
            values.Add(TagAutoEjectTimeout, [(byte)(timeout >> 8), (byte)timeout]);
        }

        // Challenge-response timeout (8-bit unsigned)
        if (ChallengeResponseTimeout.HasValue)
            values.Add(TagChallengeResponseTimeout, [ChallengeResponseTimeout.Value]);

        // Device flags (8-bit)
        if (DeviceFlags.HasValue)
            values.Add(TagDeviceFlags, [DeviceFlags.Value]);

        if (newLockCode is not null)
            values.Add(TagConfigurationLock, newLockCode);

        if (NfcRestricted.HasValue)
            values.Add(TagNfcRestricted, [NfcRestricted.Value ? (byte)0x01 : (byte)0x00]);

        var tlvData = TlvHelper.EncodeDictionary(values);
        if (tlvData.Length > 0xFF)
            throw new InvalidOperationException("DeviceConfig exceeds maximum size (255 bytes)");

        // Prepend length byte
        Memory<byte> result = new byte[tlvData.Length + 1];
        result.Span[0] = (byte)tlvData.Length;
        tlvData.Span.CopyTo(result.Span[1..]);

        return result;
    }


    public static Builder CreateBuilder() => new();

    #region Nested type: Builder

    public sealed class Builder
    {
        private readonly Dictionary<Transport, int> _enabledCapabilities = new();
        private ushort? _autoEjectTimeout;
        private byte? _challengeResponseTimeout;
        private byte? _deviceFlags;
        private bool? _nfcRestricted;

        /// <summary>Sets enabled capabilities for a transport (bitmask).</summary>
        public Builder WithCapabilities(Transport transport, int capabilities)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(capabilities);
            _enabledCapabilities[transport] = capabilities;
            return this;
        }

        /// <summary>Sets auto-eject timeout (0-65535 seconds).</summary>
        public Builder WithAutoEjectTimeout(ushort seconds)
        {
            // Sanity check: 1 hour max seems reasonable
            // TODO whats a good value here?
            ArgumentOutOfRangeException.ThrowIfGreaterThan(seconds, 3600, nameof(seconds));
            _autoEjectTimeout = seconds;
            return this;
        }

        /// <summary>Sets challenge-response timeout (0-255 seconds).</summary>
        public Builder WithChallengeResponseTimeout(byte seconds)
        {
            // TODO whats a good value here?
            ArgumentOutOfRangeException.ThrowIfGreaterThan(seconds, 60, nameof(seconds));
            _challengeResponseTimeout = seconds;
            return this;
        }

        /// <summary>Sets device flags (FLAG_EJECT, FLAG_REMOTE_WAKEUP).</summary>
        public Builder WithDeviceFlags(byte flags)
        {
            // Validate only known flags are set
            const byte validFlags = FlagEject | FlagRemoteWakeup;
            if ((flags & ~validFlags) != 0)
                throw new ArgumentException($"Invalid flags: 0x{flags:X2}", nameof(flags));

            _deviceFlags = flags;
            return this;
        }

        /// <summary>Sets NFC restriction.</summary>
        public Builder WithNfcRestricted(bool restricted)
        {
            _nfcRestricted = restricted;
            return this;
        }

        public DeviceConfig Build()
        {
            if (_enabledCapabilities.TryGetValue(Transport.Usb, out var usb) && usb == 0)
                throw new InvalidOperationException("At least one USB capability must be enabled.");

            return new DeviceConfig
            {
                EnabledCapabilities = new ReadOnlyDictionary<Transport, int>(_enabledCapabilities),
                AutoEjectTimeout = _autoEjectTimeout,
                ChallengeResponseTimeout = _challengeResponseTimeout,
                DeviceFlags = _deviceFlags,
                NfcRestricted = _nfcRestricted
            };
        }
    }

    #endregion
}