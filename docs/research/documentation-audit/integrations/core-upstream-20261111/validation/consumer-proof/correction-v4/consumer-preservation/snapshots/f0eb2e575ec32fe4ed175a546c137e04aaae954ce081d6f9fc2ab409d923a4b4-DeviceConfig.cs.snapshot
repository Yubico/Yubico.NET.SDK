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
///         configuration can be derived from what the key reports to preserve applications you do not
///         intend to change. This example disables OTP over USB and assumes another supported USB
///         application remains enabled and the configuration is not locked:
///     </para>
///     <code>
///     var info = await session.GetDeviceInfoAsync();
///     var config = new DeviceConfig { UsbEnabled = info.UsbEnabled &amp; ~DeviceCapabilities.Otp };
///     await session.SetDeviceConfigAsync(config, new SetDeviceConfigOptions { Reboot = true });
///     </code>
///     <para>
///         Timeout limits are checked when the property is assigned. When the configuration is applied,
///         <see cref="Validate" /> rejects <see cref="UsbEnabled" /> set to <see cref="DeviceCapabilities.None" />.
///         These checks do not verify firmware or application support on the selected device. Check
///         <see cref="DeviceInfo.UsbSupported" /> and <see cref="DeviceInfo.NfcSupported" /> before choosing
///         application masks, and keep at least one supported USB application enabled.
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

    /// <summary>Gets the complete replacement set of applications enabled over USB.</summary>
    /// <remarks>
    ///     A set bit enables the application; a cleared bit disables it. A <see langword="null" /> value
    ///     leaves the current set unchanged. Keep at least one application supported by
    ///     <see cref="DeviceInfo.UsbSupported" /> enabled. <see cref="Validate" /> rejects
    ///     <see cref="DeviceCapabilities.None" />, but does not check the mask against the device's supported applications.
    /// </remarks>
    public DeviceCapabilities? UsbEnabled { get; init; }

    /// <summary>Gets the complete replacement set of applications enabled over NFC.</summary>
    /// <remarks>
    ///     A set bit enables the application; a cleared bit disables it. A <see langword="null" /> value
    ///     leaves the current set unchanged. <see cref="DeviceCapabilities.None" /> requests that all NFC
    ///     applications be disabled. Choose applications supported by <see cref="DeviceInfo.NfcSupported" />.
    ///     This setting is separate from the temporary restriction controlled by <see cref="NfcRestricted" />.
    /// </remarks>
    public DeviceCapabilities? NfcEnabled { get; init; }

    /// <summary>
    ///     Gets the inactivity timeout, in seconds, before the smart card is automatically ejected in
    ///     CCID-only mode with <see cref="Core.Devices.DeviceFlags.TouchEject" /> set.
    /// </summary>
    /// <remarks>
    ///     Zero disables automatic ejection; <see langword="null" /> leaves the current timeout unchanged.
    ///     Setting this property does not enable <see cref="Core.Devices.DeviceFlags.TouchEject" />.
    ///     The SDK accepts 0 through 3600 seconds; this is an SDK limit, not a firmware maximum.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value exceeds 3600 seconds.</exception>
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

    /// <summary>Gets the timeout, in seconds, for YubiOTP challenge-response operations that require touch.</summary>
    /// <remarks>
    ///     Zero restores the default timeout of 15 seconds; <see langword="null" /> leaves the current
    ///     timeout unchanged. The SDK accepts 0 through 60 seconds; this is an SDK limit, not a firmware maximum.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value exceeds 60 seconds.</exception>
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

    /// <summary>Gets the complete replacement device flags, including touch-eject and remote wakeup.</summary>
    /// <remarks>
    ///     Set bits enable the corresponding settings; cleared bits disable them. A <see langword="null" />
    ///     value leaves the current flags unchanged. Touch-eject applies only in CCID-only mode.
    /// </remarks>
    public DeviceFlags? DeviceFlags { get; init; }

    /// <summary>Gets the temporary NFC restriction request for NFC-capable YubiKeys with firmware 5.7 or later.</summary>
    /// <remarks>
    ///     <para>
    ///         Set to <see langword="true" /> to request restricted NFC mode. In this mode, NFC permits
    ///         only reading the URL configured by Yubico in the NDEF tag. To remove the restriction, connect
    ///         the key to USB power for three seconds; no touch or application operation is required.
    ///         This does not change the application mask in <see cref="NfcEnabled" />.
    ///     </para>
    ///     <para>
    ///         A <see langword="null" /> value omits the restriction setting. A <see langword="false" />
    ///         value sends an explicit zero byte for the setting. Use USB power to remove the restriction.
    ///         The SDK does not check this property's firmware or NFC-support requirements before sending it.
    ///     </para>
    /// </remarks>
    public bool? NfcRestricted { get; init; }

    /// <summary>
    ///     Checks that <see cref="UsbEnabled" /> is not explicitly set to <see cref="DeviceCapabilities.None" />.
    ///     Called by <see cref="ManagementSession.SetDeviceConfigAsync" /> before anything is sent to the key.
    /// </summary>
    /// <remarks>
    ///     A <see langword="null" /> USB mask leaves the current setting unchanged and passes this check.
    ///     This method does not check mask widths, supported applications or firmware requirements.
    ///     Timeout ranges are checked separately when their properties are assigned.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///     <see cref="UsbEnabled" /> is <see cref="DeviceCapabilities.None" />.
    /// </exception>
    public void Validate()
    {
        if (UsbEnabled == DeviceCapabilities.None)
            throw new InvalidOperationException("At least one USB capability must be enabled.");
    }

    /// <summary>
    ///     Serializes to wire format: a one-byte length followed by the TLVs in this encoder's fixed order
    ///     (reboot and unlock first when present), not sorted by tag.
    /// </summary>
    /// <returns>A caller-owned buffer that must be cleared after transmission because it can contain lock codes.</returns>
    /// <remarks>Intermediate encoded buffers are cleared before this method returns.</remarks>
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