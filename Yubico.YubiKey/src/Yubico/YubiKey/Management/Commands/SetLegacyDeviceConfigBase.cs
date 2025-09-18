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

using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Yubico.YubiKey.Management.Commands;

/// <summary>
///     Writes configuration settings that are supported by YubiKeys prior to firmware version 5.
/// </summary>
/// <remarks>
///     This is the only configuration operation available to YubiKeys prior to firmware
///     version 5. These YubiKeys have limited configuration settings, and all of
///     them must be set at the same time.
/// </remarks>
public class SetLegacyDeviceConfigBase
{
    private static readonly Dictionary<YubiKeyCapabilities, byte> _interfaceCodes =
        new()
        {
            [YubiKeyCapabilities.Otp] = 0x00,
            [YubiKeyCapabilities.Ccid] = 0x01,
            [YubiKeyCapabilities.Otp | YubiKeyCapabilities.Ccid] = 0x02,
            [YubiKeyCapabilities.FidoU2f] = 0x03,
            [YubiKeyCapabilities.Otp | YubiKeyCapabilities.FidoU2f] = 0x04,
            [YubiKeyCapabilities.Ccid | YubiKeyCapabilities.FidoU2f] = 0x05,
            [YubiKeyCapabilities.Otp | YubiKeyCapabilities.Ccid | YubiKeyCapabilities.FidoU2f] = 0x06,
            [YubiKeyCapabilities.All] = 0x06 // Convenience value, auto convert to interfaces
        };

    private ushort _autoEjectTimeout;

    private YubiKeyCapabilities _yubiKeyInterfaces;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SetLegacyDeviceConfigBase" /> class.
    /// </summary>
    /// <remarks>
    ///     This command sends all configuration settings every time. Therefore all values must
    ///     be provided every time.
    /// </remarks>
    /// <param name="yubiKeyInterfaces">
    ///     Value for <see cref="YubiKeyInterfaces" />.
    /// </param>
    /// <param name="challengeResponseTimeout">
    ///     Value for <see cref="ChallengeResponseTimeout" />.
    /// </param>
    /// <param name="touchEjectEnabled">
    ///     Value for <see cref="TouchEjectEnabled" />.
    /// </param>
    /// <param name="autoEjectTimeout">
    ///     Value for <see cref="AutoEjectTimeout" />.
    /// </param>
    /// <exception cref="ArgumentException">
    ///     Thrown if <paramref name="yubiKeyInterfaces" /> contains unsupported flags.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="autoEjectTimeout" /> is out of the valid range.
    /// </exception>
    protected SetLegacyDeviceConfigBase(
        YubiKeyCapabilities yubiKeyInterfaces,
        byte challengeResponseTimeout,
        bool touchEjectEnabled,
        int autoEjectTimeout)
    {
        YubiKeyInterfaces = yubiKeyInterfaces;
        ChallengeResponseTimeout = challengeResponseTimeout;
        TouchEjectEnabled = touchEjectEnabled;
        AutoEjectTimeout = autoEjectTimeout;
    }

    /// <summary>
    ///     Copy constructor.
    /// </summary>
    /// <remarks>
    ///     Intended to be used by child classes to make it easier to convert into a different
    ///     application-specific command.
    /// </remarks>
    /// <param name="baseCommand">
    ///     The SetLegacyDeviceConfig base command object to copy from.
    /// </param>
    protected SetLegacyDeviceConfigBase(SetLegacyDeviceConfigBase baseCommand)
    {
        if (baseCommand is null)
        {
            throw new ArgumentNullException(nameof(baseCommand));
        }

        YubiKeyInterfaces = baseCommand.YubiKeyInterfaces;
        ChallengeResponseTimeout = baseCommand.ChallengeResponseTimeout;
        TouchEjectEnabled = baseCommand.TouchEjectEnabled;
        AutoEjectTimeout = baseCommand.AutoEjectTimeout;
    }

    /// <summary>
    ///     The interfaces of the YubiKey that should be enabled over USB.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is an indirect means of controlling which YubiKey applications are available to
    ///         the end user.
    ///     </para>
    ///     <para>
    ///         There must be at least one enabled interface. Only the following flags are allowed:
    ///         <UsbInterfaceRestrictions>
    ///             <list type="bullet">
    ///                 <item>
    ///                     <see cref="YubiKeyCapabilities.Otp" />
    ///                 </item>
    ///                 <item>
    ///                     <see cref="YubiKeyCapabilities.Ccid" />
    ///                 </item>
    ///                 <item>
    ///                     <see cref="YubiKeyCapabilities.FidoU2f" />
    ///                 </item>
    ///                 <item>
    ///                     <see cref="YubiKeyCapabilities.All" />
    ///                 </item>
    ///             </list>
    ///         </UsbInterfaceRestrictions>
    ///     </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    ///     Thrown by the setter if the value contains unsupported flags.
    /// </exception>
    public YubiKeyCapabilities YubiKeyInterfaces
    {
        get => _yubiKeyInterfaces;

        set
        {
            if (!ContainsOnlyValidInterfaceFlags(value))
            {
                throw new ArgumentException(ExceptionMessages.SupportsOnlyUsbInterfaces);
            }

            _yubiKeyInterfaces = value;
        }
    }

    /// <summary>
    ///     The period of time (in seconds) after which the OTP challenge-response command should
    ///     timeout.
    /// </summary>
    public byte ChallengeResponseTimeout { get; set; }

    /// <summary>
    ///     The CCID auto-eject timeout (in seconds). This field is only meaningful if touch eject
    ///     is enabled (see <see cref="TouchEjectEnabled" />).
    /// </summary>
    /// <remarks>
    ///     When setting, the value must be in the range <see cref="ushort.MinValue" /> through
    ///     <see cref="ushort.MaxValue" />. A value of <c>0</c> means that the timeout is disabled
    ///     (the smart card will not be ejected automatically).
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown by the setter when the value is out of the valid range.
    /// </exception>
    public int AutoEjectTimeout
    {
        get => _autoEjectTimeout;

        set
        {
            if (value < ushort.MinValue || value > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _autoEjectTimeout = (ushort)value;
        }
    }

    /// <inheritdoc cref="DeviceFlags.TouchEject" />
    public bool TouchEjectEnabled { get; set; }

    public static bool ContainsOnlyValidInterfaceFlags(YubiKeyCapabilities yubiKeyInterfaces) =>
        _interfaceCodes.ContainsKey(yubiKeyInterfaces);

    /// <summary>
    ///     Formats the data to be sent to the YubiKey.
    /// </summary>
    /// <returns>
    ///     A formatted byte array.
    /// </returns>
    protected byte[] GetDataForApdu()
    {
        byte[] dataField = new byte[4];

        dataField[0] = _interfaceCodes[YubiKeyInterfaces];

        if (TouchEjectEnabled)
        {
            dataField[0] |= (byte)DeviceFlags.TouchEject;
        }

        dataField[1] = ChallengeResponseTimeout;

        BinaryPrimitives.WriteUInt16LittleEndian(dataField.AsSpan(2), _autoEjectTimeout);

        return dataField;
    }
}
