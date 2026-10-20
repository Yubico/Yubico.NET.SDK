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
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Management;

/// <summary>
///     The settings of a YubiKey that the Management application can change. Every property is optional;
///     a <see langword="null" /> leaves the device's current value untouched.
/// </summary>
/// <remarks>
///     <para>
///         This is the write-side counterpart of <see cref="DeviceInfo" />: <see cref="UsbEnabled" /> and
///         <see cref="NfcEnabled" /> use the same <see cref="DeviceCapabilities" /> flags that
///         <see cref="DeviceInfo.UsbEnabled" /> and <see cref="DeviceInfo.NfcEnabled" /> report, so a
///         configuration is usually derived from what the key says:
///     </para>
///     <code>
///     var info = await session.GetDeviceInfoAsync();
///     var config = new DeviceConfig { UsbEnabled = info.UsbEnabled &amp; ~DeviceCapabilities.Otp };
///     await session.SetDeviceConfigAsync(config, new SetDeviceConfigOptions { Reboot = true });
///     </code>
///     <para>
///         Single-property limits are checked when the property is set. Rules that involve the whole
///         configuration, such as keeping at least one USB application enabled, are checked by
///         <see cref="Validate" /> when the configuration is applied.
///     </para>
/// </remarks>
public sealed record DeviceConfig
{
    // Management SET DEVICE CONFIG TLV tags
    private const int TagUsbEnabled = 0x03;
    private const int TagAutoEjectTimeout = 0x06;
    private const int TagChallengeResponseTimeout = 0x07;
    private const int TagDeviceFlags = 0x08;
    private const int TagConfigurationLock = 0x0A;
    private const int TagUnlock = 0x0B;
    private const int TagReboot = 0x0C;
    private const int TagNfcEnabled = 0x0E;
    private const int TagNfcRestricted = 0x17;

    private const int MaxConfigLength = 0xFF; // The TLV block is prefixed by a one-byte length
    private const ushort MaxAutoEjectTimeoutSeconds = 3600;
    private const byte MaxChallengeResponseTimeoutSeconds = 60;

    private readonly ushort? _autoEjectTimeout;
    private readonly byte? _challengeResponseTimeout;

    /// <summary>Applications to enable over USB. At least one must remain enabled.</summary>
    public DeviceCapabilities? UsbEnabled { get; init; }

    /// <summary>Applications to enable over NFC.</summary>
    public DeviceCapabilities? NfcEnabled { get; init; }

    /// <summary>
    ///     Seconds before the smart card is ejected in CCID-only mode with <see cref="Core.Devices.DeviceFlags.TouchEject" />
    ///     set (0-3600).
    /// </summary>
    public ushort? AutoEjectTimeout
    {
        get => _autoEjectTimeout;
        init
        {
            if (value > MaxAutoEjectTimeoutSeconds)
                throw new ArgumentOutOfRangeException(nameof(AutoEjectTimeout), value,
                    $"Must be at most {MaxAutoEjectTimeoutSeconds} seconds.");
            _autoEjectTimeout = value;
        }
    }

    /// <summary>Seconds that YubiOTP challenge-response waits for touch (0-60).</summary>
    public byte? ChallengeResponseTimeout
    {
        get => _challengeResponseTimeout;
        init
        {
            if (value > MaxChallengeResponseTimeoutSeconds)
                throw new ArgumentOutOfRangeException(nameof(ChallengeResponseTimeout), value,
                    $"Must be at most {MaxChallengeResponseTimeoutSeconds} seconds.");
            _challengeResponseTimeout = value;
        }
    }

    /// <summary>Device flags (touch-eject, remote wakeup).</summary>
    public DeviceFlags? DeviceFlags { get; init; }

    /// <summary>Whether NFC is restricted until the key has been used over USB.</summary>
    public bool? NfcRestricted { get; init; }

    /// <summary>
    ///     Checks the rules that involve more than one property. Called by
    ///     <see cref="ManagementSession.SetDeviceConfigAsync" /> before anything is sent to the key.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     <see cref="UsbEnabled" /> is <see cref="DeviceCapabilities.None" />: the key would become unreachable over USB.
    /// </exception>
    public void Validate()
    {
        if (UsbEnabled == DeviceCapabilities.None)
            throw new InvalidOperationException("At least one USB capability must be enabled.");
    }

    /// <summary>Serializes to wire format: a one-byte length followed by the TLVs in a fixed order (reboot and unlock first), not sorted by tag.</summary>
    internal Memory<byte> GetBytes(SetDeviceConfigOptions? options = null)
    {
        Validate();

        var tlvs = new List<Tlv>(9);
        try
        {
            if (options?.Reboot == true)
                tlvs.Add(new Tlv(TagReboot, []));

            if (options?.CurrentLockCode is { } currentLockCode)
                tlvs.Add(new Tlv(TagUnlock, currentLockCode.Span));

            if (UsbEnabled is { } usb)
                tlvs.Add(new Tlv(TagUsbEnabled, UInt16BigEndian((ushort)usb)));

            if (NfcEnabled is { } nfc)
                tlvs.Add(new Tlv(TagNfcEnabled, UInt16BigEndian((ushort)nfc)));

            if (AutoEjectTimeout is { } autoEject)
                tlvs.Add(new Tlv(TagAutoEjectTimeout, UInt16BigEndian(autoEject)));

            if (ChallengeResponseTimeout is { } challengeResponse)
                tlvs.Add(new Tlv(TagChallengeResponseTimeout, [challengeResponse]));

            if (DeviceFlags is { } flags)
                tlvs.Add(new Tlv(TagDeviceFlags, [(byte)flags]));

            if (options?.NewLockCode is { } newLockCode)
                tlvs.Add(new Tlv(TagConfigurationLock, newLockCode.Span));

            if (NfcRestricted is { } nfcRestricted)
                tlvs.Add(new Tlv(TagNfcRestricted, [nfcRestricted ? (byte)0x01 : (byte)0x00]));

            var tlvData = TlvHelper.EncodeList([.. tlvs]);
            try
            {
                if (tlvData.Length > MaxConfigLength)
                    throw new InvalidOperationException($"DeviceConfig exceeds maximum size ({MaxConfigLength} bytes)");

                Memory<byte> result = new byte[tlvData.Length + 1];
                result.Span[0] = (byte)tlvData.Length;
                tlvData.Span.CopyTo(result.Span[1..]);
                return result;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(tlvData.Span);
            }
        }
        finally
        {
            // Tlv.Dispose zeroes its buffer, which covers the lock codes copied in above.
            foreach (var tlv in tlvs)
                tlv.Dispose();
        }
    }

    private static byte[] UInt16BigEndian(ushort value) => [(byte)(value >> 8), (byte)value];
}