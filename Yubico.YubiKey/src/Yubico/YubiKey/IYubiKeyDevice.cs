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
using System.Diagnostics.CodeAnalysis;
using Yubico.Core.Devices;
using Yubico.YubiKey.Scp;
using MgmtCmd = Yubico.YubiKey.Management.Commands;


namespace Yubico.YubiKey
{
    /// <summary>
    /// Interface for class that provides device and enumeration capabilities.
    /// </summary>
    public interface IYubiKeyDevice : IYubiKeyDeviceInfo, IEquatable<IYubiKeyDevice>, IComparable<IYubiKeyDevice>
    {
        /// <summary>
        /// Indicates which logical device transports are available to this YubiKey device.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A YubiKey can be connected to a computer in multiple ways: physically connected via USB or Lightning, or by
        /// being present in an NFC reader's field. Further, when connected through USB, the YubiKey appears to the
        /// computer as multiple devices. It can look like a HID Keyboard, a HID FIDO device, and a smart card reader.
        /// This property shows which of these connections are present.
        /// </para>
        /// <para>
        /// For example: if this YubiKey instance is connected through an NFC reader, this value will be
        /// <see cref="Transport.NfcSmartCard" />. If it is connected through USB and all of the three USB interfaces
        /// are available, it will contain the set <see cref="Transport.HidKeyboard" />, <see cref="Transport.HidFido" />,
        /// and <see cref="Transport.UsbSmartCard" />.
        /// </para>
        /// </remarks>
        Transport AvailableTransports { get; }

        /// <summary>
        /// Initiate a connection to the specified application on a YubiKey
        /// device.
        /// </summary>
        /// <param name="application">
        /// The application to reference on the device.
        /// </param>
        /// <returns>
        /// An instance of a class that implements the
        /// <see cref="IYubiKeyConnection"/> interface.
        /// </returns>
        IYubiKeyConnection Connect(YubiKeyApplication application);

        /// <summary>
        /// Initiate a connection to the specified application represented as an
        /// <c>applicationId</c> on a YubiKey device.
        /// </summary>
        /// <param name="applicationId">
        /// A byte array representing the smart card Application ID (AID) for the
        /// application to open.
        /// </param>
        /// <returns>
        /// An instance of a class that implements the
        /// <see cref="IYubiKeyConnection"/> interface.
        /// </returns>
        IYubiKeyConnection Connect(byte[] applicationId);

        /// <summary>
        /// Initiate a connection to the specified application on a YubiKey device using SCP protocol.
        /// </summary>
        /// <param name="application">
        /// The <see cref="YubiKeyApplication"/> to reference on the device.
        /// </param>
        /// <param name="keyParameters">
        /// The SCP key parameters to use in making an SCP connection.
        /// </param>
        /// <remarks>
        /// Note that SCP works only with SmartCard applications, namely PIV, OATH, OTP, Security Domain and YubiHsmAuth
        /// and OpenPgp. However, SCP03 is supported only on series 5 YubiKeys with
        /// firmware version on 5.3 and above. SCP 11 is supported only firmware version 5.7.2 and above.
        /// <para>
        /// Note also that the return is an instance of a class that implements
        /// <see cref="IScpYubiKeyConnection"/> which is a "subclass" of
        /// <see cref="IYubiKeyConnection"/>.
        /// </para>
        /// </remarks>
        /// <returns>
        /// An instance of a class that implements the <see cref="IScpYubiKeyConnection"/> interface.
        /// </returns>
        IScpYubiKeyConnection Connect(YubiKeyApplication application, ScpKeyParameters keyParameters);

        /// <summary>
        /// Initiate a connection to the specified application on a YubiKey device using SCP protocol.
        /// </summary>
        /// <param name="applicationId">A byte array representing the smart card Application ID (AID) for the
        /// application to open.
        /// </param>
        /// <param name="keyParameters">
        /// The SCP key parameters to use in making an SCP connection.
        /// </param>
        /// <remarks>
        /// Note that SCP works only with SmartCard applications, namely PIV, OATH, OTP, Security Domain and YubiHsmAuth
        /// and OpenPgp. However, SCP03 is supported only on series 5 YubiKeys with
        /// firmware version on 5.3 and above. SCP 11 is supported only firmware version 5.7.2 and above.
        /// <para>
        /// Note also that the return is an instance of a class that implements
        /// <see cref="IScpYubiKeyConnection"/> which is a "subclass" of
        /// <see cref="IYubiKeyConnection"/>.
        /// </para>
        /// </remarks>
        /// <returns>
        /// An instance of a class that implements the <see cref="IScpYubiKeyConnection"/> interface.
        /// </returns>
        IScpYubiKeyConnection Connect(byte[] applicationId, ScpKeyParameters keyParameters);


        /// <summary>
        /// Attempt to connect to the YubiKey device.
        /// </summary>
        /// <param name="application">The application to reference on the device.</param>
        /// <param name="connection">Out parameter containing the <see cref="IYubiKeyConnection"/> instance.</param>
        /// <returns>Boolean indicating whether the call was successful.</returns>
        bool TryConnect(
            YubiKeyApplication application,
            [MaybeNullWhen(returnValue: false)] out IYubiKeyConnection connection);

        /// <summary>
        /// Attempt to connect to the YubiKey device.
        /// </summary>
        /// <param name="applicationId">A byte array representing the smart card Application ID (AID) for the
        /// application to open.</param>
        /// <param name="connection">Out parameter containing the <see cref="IYubiKeyConnection"/> instance.</param>
        /// <returns>Boolean indicating whether the call was successful.</returns>
        bool TryConnect(
            byte[] applicationId,
            [MaybeNullWhen(returnValue: false)] out IYubiKeyConnection connection);


        /// <summary>
        /// Attempt to connect to the YubiKey device over SCP using the specified <see cref="ScpKeyParameters"/>
        /// </summary>
        /// <remarks>
        /// Note that SCP works only with SmartCard applications, namely PIV, OATH, OTP, Security Domain and YubiHsmAuth
        /// and OpenPgp. However, SCP03 is supported only on series 5 YubiKeys with
        /// firmware version on 5.3 and above. SCP 11 is supported only firmware version 5.7.2 and above.
        /// <para>
        /// Note also that the return is an instance of a class that implements
        /// <see cref="IScpYubiKeyConnection"/> which is a "subclass" of
        /// <see cref="IYubiKeyConnection"/>.
        /// </para>
        /// </remarks>
        /// <param name="application">The application to reference on the device.</param>
        /// <param name="keyParameters">
        /// The <see cref="ScpKeyParameters"/> key set to use in making an SCP connection.
        /// </param>
        /// <param name="connection">Out parameter containing the <see cref="IYubiKeyConnection"/> instance.</param>
        /// <returns>Boolean indicating whether the call was successful.</returns>
        bool TryConnect(
            YubiKeyApplication application,
            ScpKeyParameters keyParameters,
            [MaybeNullWhen(returnValue: false)] out IScpYubiKeyConnection connection);

        /// <summary>
        /// Attempt to connect to the YubiKey device over SCP using the specified <see cref="ScpKeyParameters"/>
        /// </summary>
        /// <remarks>
        /// Note that SCP works only with SmartCard applications, namely PIV, OATH, OTP, Security Domain and YubiHsmAuth
        /// and OpenPgp. However, SCP03 is supported only on series 5 YubiKeys with
        /// firmware version on 5.3 and above. SCP 11 is supported only firmware version 5.7.2 and above.
        /// <para>
        /// Note also that the return is an instance of a class that implements
        /// <see cref="IScpYubiKeyConnection"/> which is a "subclass" of
        /// <see cref="IYubiKeyConnection"/>.
        /// </para>
        /// </remarks>
        /// <param name="applicationId">The Iso7816 application ID to use for the connection.</param>
        /// <param name="keyParameters">The <see cref="ScpKeyParameters"/> parameters for the SCP connection.</param>
        /// <param name="connection">The connection to the YubiKey, or null if unable to connect.</param>
        /// <returns>True if the connection was successful, false otherwise.</returns>
        bool TryConnect(
            byte[] applicationId,
            ScpKeyParameters keyParameters,
            [MaybeNullWhen(returnValue: false)] out IScpYubiKeyConnection connection);

        /// <summary>
        /// Checks whether a IYubiKeyDevice instance contains a particular platform <see cref="IDevice" />.
        /// </summary>
        /// <param name="other">The device to check.</param>
        /// <returns>True, if the IYubiKeyDevice contains the platform device.</returns>
        internal bool Contains(IDevice other);

        /// <summary>
        /// Checks whether a IYubiKeyDevice instance contains another <see cref="IDevice" /> with the same
        /// <see cref="IDevice.ParentDeviceId" />.
        /// </summary>
        /// <param name="other">The device to check against.</param>
        /// <returns>True, if the IYubiKeyDevice contains a platform device that shares the same parent.</returns>
        internal bool HasSameParentDevice(IDevice other);

        /// <summary>
        /// Sets which NFC features are enabled (and disabled).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Requires firmware version &gt;= 5.0.0.
        /// </para>
        ///
        /// <para>
        /// Modifies the value of <see cref="IYubiKeyDeviceInfo.EnabledNfcCapabilities"/>. This
        /// requires the YubiKey's configuration to be unlocked (see
        /// <see cref="IYubiKeyDeviceInfo.ConfigurationLocked"/> and
        /// <see cref="UnlockConfiguration(ReadOnlySpan{byte})"/>.
        /// </para>
        ///
        /// <para>
        /// The YubiKey will reboot as part of this change. This will cause
        /// this <c>IYubiKeyDevice</c> object to become stale, and future connection
        /// attempts using this object are likely to fail. To get fresh
        /// <c>IYubiKeys</c>, use the YubiKey enumeration functions such as
        /// <see cref="YubiKeyDevice.FindAll"/> and <see cref="YubiKeyDevice.FindByTransport(Transport)"/>.
        /// </para>
        ///
        /// <para>
        /// To see which NFC features are available on the YubiKey,
        /// see <see cref="IYubiKeyDeviceInfo.AvailableNfcCapabilities"/>.
        /// </para>
        ///
        /// <example>
        /// This example shows how to enable only the <see cref="YubiKeyCapabilities.Piv"/>
        /// capability over NFC on all YubiKeys where <c>Piv</c> is available. All other
        /// capabilities will be disabled. The new set of enabled NFC capabilities will be
        /// printed to the console, showing that only <c>Piv</c> is enabled over NFC.
        /// <code language="csharp">
        /// IEnumerable&lt;IYubiKeyDevice&gt; yubiKeys =
        ///     YubiKey.FindAll()
        ///     .Where(d => d.AvailableNfcCapabilities.HasFlag(YubiKeyCapabilities.Piv));
        ///
        /// foreach (IYubiKeyDevice yubiKey in yubiKeys)
        /// {
        ///     device.SetEnabledNfcCapabilities(YubiKeyCapabilities.Piv);
        /// }
        ///
        /// // The devices may need a little time to finish rebooting
        /// sleep(100);
        ///
        /// // Get fresh IYubiKeys
        /// IEnumerable&lt;IYubiKeyDevice&gt; freshYubiKeys =
        ///     YubiKey.FindAll()
        ///     .Where(d => d.AvailableNfcCapabilities.HasFlag(YubiKeyCapabilities.Piv));
        ///
        /// int i = 1;
        /// foreach (IYubiKeyDevice yubiKey in freshYubiKeys)
        /// {
        ///     Console.PrintLine($"{i:} {yubiKey.SerialNumber} - {yubiKey.EnabledNfcCapabilities}");
        /// }
        /// </code>
        /// </example>
        ///
        /// <example>
        /// To change the state of some capabilities without affecting the others, you
        /// can do something like the following:
        /// <code language="csharp">
        /// IYubiKeyDevice yubiKey = YubiKey.FindAll().First();
        ///
        /// YubiKeyCapabilities desiredNfcCapabilities = yubiKey.EnabledNfcCapabilities;
        ///
        /// // Selectively enable Piv
        /// desiredNfcCapabilities |= YubiKeyCapabilities.Piv;
        ///
        /// // Selectively disable Otp
        /// desiredNfcCapabilities &amp;= ~YubiKeyCapabilities.Otp;
        ///
        /// yubiKey.SetEnabledNfcCapabilities(desiredNfcCapabilities);
        /// </code>
        /// </example>
        /// </remarks>
        /// <param name="yubiKeyCapabilities">
        /// The desired set of NFC features to enable on the YubiKey. A set flag
        /// means that the related capability is enabled. Otherwise, the capability
        /// is disabled.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The command failed to complete.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// An error occurred when attempting to connect to the device.
        /// </exception>
        /// <seealso cref="MgmtCmd.SetDeviceInfoCommand"/>
        void SetEnabledNfcCapabilities(YubiKeyCapabilities yubiKeyCapabilities);

        /// <summary>
        /// Sets which USB features are enabled (and disabled).
        /// </summary>
        /// <remarks>
        /// <para>
        /// YubiKeys prior to firmware version 5 must use
        /// <see cref="SetLegacyDeviceConfiguration(YubiKeyCapabilities, byte, bool, int)"/>.
        /// </para>
        ///
        /// <para>
        /// Modifies the value of <see cref="IYubiKeyDeviceInfo.EnabledUsbCapabilities"/>. This
        /// requires the YubiKey's configuration to be unlocked (see
        /// <see cref="IYubiKeyDeviceInfo.ConfigurationLocked"/> and
        /// <see cref="UnlockConfiguration(ReadOnlySpan{byte})"/>.
        /// </para>
        ///
        /// <para>
        /// The YubiKey will reboot as part of this change. This will cause
        /// this <c>IYubiKeyDevice</c> object to become stale, and future connection
        /// attempts using this object are likely to fail. To get fresh
        /// <c>IYubiKeys</c>, use the YubiKey enumeration functions such as
        /// <see cref="YubiKeyDevice.FindAll"/> and <see cref="YubiKeyDevice.FindByTransport(Transport)"/>.
        /// </para>
        ///
        /// <para>
        /// To see which USB features are available on the YubiKey,
        /// see <see cref="IYubiKeyDeviceInfo.AvailableUsbCapabilities"/>. At least
        /// one of these capabilities must be enabled.
        /// </para>
        ///
        /// <example>
        /// This example shows how to enable only the <see cref="YubiKeyCapabilities.Piv"/>
        /// capability over USB on all YubiKeys where <c>Piv</c> is available. All other
        /// capabilities will be disabled. The new set of enabled USB capabilities will be
        /// printed to the console, showing that only <c>Piv</c> is enabled over USB.
        /// <code language="csharp">
        /// IEnumerable&lt;IYubiKeyDevice&gt; yubiKeys =
        ///     YubiKey.FindAll()
        ///     .Where(d => d.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));
        ///
        /// foreach (IYubiKeyDevice yubiKey in yubiKeys)
        /// {
        ///     device.SetEnabledUsbCapabilities(YubiKeyCapabilities.Piv);
        /// }
        ///
        /// // The devices may need a little time to finish rebooting
        /// sleep(100);
        ///
        /// // Get fresh IYubiKeys
        /// IEnumerable&lt;IYubiKeyDevice&gt; freshYubiKeys =
        ///     YubiKey.FindAll()
        ///     .Where(d => d.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));
        ///
        /// int i = 1;
        /// foreach (IYubiKeyDevice yubiKey in freshYubiKeys)
        /// {
        ///     Console.PrintLine($"{i:} {yubiKey.SerialNumber} - {yubiKey.EnabledUsbCapabilities}");
        /// }
        /// </code>
        /// </example>
        ///
        /// <example>
        /// To change the state of some capabilities without affecting the others, you
        /// can do something like the following:
        /// <code language="csharp">
        /// IYubiKeyDevice yubiKey = YubiKey.FindAll().First();
        ///
        /// YubiKeyCapabilities desiredUsbCapabilities = yubiKey.EnabledUsbCapabilities;
        ///
        /// // Selectively enable Piv
        /// desiredUsbCapabilities |= YubiKeyCapabilities.Piv;
        ///
        /// // Selectively disable Otp
        /// desiredUsbCapabilities &amp;= ~YubiKeyCapabilities.Otp;
        ///
        /// yubiKey.SetEnabledUsbCapabilities(desiredUsbCapabilities);
        /// </code>
        /// </example>
        /// </remarks>
        /// <param name="yubiKeyCapabilities">
        /// <para>
        /// The desired set of USB features to enable on the YubiKey. A set flag
        /// means that the related capability is enabled. Otherwise, the capability
        /// is disabled. At least one available USB capability must be enabled.
        /// </para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Either the command failed to complete.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// An error occurred when attempting to connect to the device.
        /// </exception>
        /// <seealso cref="MgmtCmd.SetDeviceInfoCommand"/>
        void SetEnabledUsbCapabilities(YubiKeyCapabilities yubiKeyCapabilities);

        /// <summary>
        /// Sets the timeout on OTP challenge-response operations.
        /// </summary>
        /// <remarks>
        /// <para>
        /// YubiKeys prior to firmware version 5 must use
        /// <see cref="SetLegacyDeviceConfiguration(YubiKeyCapabilities, byte, bool, int)"/>.
        /// </para>
        ///
        /// <para>
        /// Modifies the value of <see cref="IYubiKeyDeviceInfo.ChallengeResponseTimeout"/>.
        /// After successful execution, the <see cref="ChallengeResponseTimeout"/> property
        /// will immediately reflect the new value. This requires the YubiKey's configuration
        /// to be unlocked (see <see cref="IYubiKeyDeviceInfo.ConfigurationLocked"/> and
        /// <see cref="UnlockConfiguration(ReadOnlySpan{byte})"/>.
        /// </para>
        /// </remarks>
        /// <param name="seconds">
        /// The length of the timeout in seconds. The value must be in the range
        /// 0-255, where 0 resets the value to its default.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value of <paramref name="seconds"/> must be in the range 0-255.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The command failed to complete.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// An error occurred when attempting to connect to the device.
        /// </exception>
        /// <seealso cref="MgmtCmd.SetDeviceInfoCommand"/>
        void SetChallengeResponseTimeout(int seconds);

        /// <summary>
        /// Sets the CCID auto-eject timeout (in seconds).
        /// </summary>
        /// <remarks>
        /// <para>
        /// YubiKeys prior to firmware version 5 must use
        /// <see cref="SetLegacyDeviceConfiguration(YubiKeyCapabilities, byte, bool, int)"/>.
        /// </para>
        ///
        /// <para>
        /// Modifies the value of <see cref="IYubiKeyDeviceInfo.AutoEjectTimeout"/>.
        /// After successful execution, the <see cref="AutoEjectTimeout"/> property
        /// will immediately reflect the new value. This requires the YubiKey's configuration
        /// to be unlocked (see <see cref="IYubiKeyDeviceInfo.ConfigurationLocked"/> and
        /// <see cref="UnlockConfiguration(ReadOnlySpan{byte})"/>.
        /// </para>
        /// <para>
        /// A value of <c>0</c> means that the timeout is disabled (the smart card
        /// will not be ejected automatically). See <see cref="DeviceFlags.TouchEject"/>
        /// for more information on setting up the smart card to automatically eject.
        /// </para>
        /// </remarks>
        /// <param name="seconds">
        /// The length of the timeout in seconds. The value must be in the range
        /// <see cref="ushort.MinValue"/> through <see cref="ushort.MaxValue"/>.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value of <paramref name="seconds"/> must be in the range
        /// <see cref="ushort.MinValue"/> through <see cref="ushort.MaxValue"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The command failed to complete.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// An error occurred when attempting to connect to the device.
        /// </exception>
        /// <seealso cref="MgmtCmd.SetDeviceInfoCommand"/>
        void SetAutoEjectTimeout(int seconds);

        /// <summary>
        /// Modifies the value of <see cref="IYubiKeyDeviceInfo.DeviceFlags"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// After successful execution, the <see cref="DeviceFlags"/> property
        /// will immediately reflect the new value.
        /// </para>
        ///
        /// <para>
        /// YubiKeys prior to firmware version 5 can use
        /// <see cref="SetLegacyDeviceConfiguration(YubiKeyCapabilities, byte, bool, int)"/> to enable
        /// <see cref="DeviceFlags.TouchEject"/>.
        /// </para>
        ///
        /// <para>
        /// This operation requires the YubiKey's configuration to be unlocked (see
        /// <see cref="IYubiKeyDeviceInfo.ConfigurationLocked"/> and
        /// <see cref="UnlockConfiguration(ReadOnlySpan{byte})"/>.
        /// </para>
        /// </remarks>
        /// <param name="deviceFlags">
        /// The desired device settings. A set flag means that the setting
        /// is enabled. Otherwise, the capability is disabled.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The command failed to complete.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// An error occurred when attempting to connect to the device.
        /// </exception>
        /// <seealso cref="MgmtCmd.SetDeviceInfoCommand"/>
        void SetDeviceFlags(DeviceFlags deviceFlags);

        /// <summary>
        /// Sets a configuration lock code, which prevents changes to YubiKey's user-settable
        /// <see cref="IYubiKeyDeviceInfo"/> values.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Requires firmware version &gt;= 5.0.0.
        /// </para>
        ///
        /// <para>
        /// See <see cref="IYubiKeyDeviceInfo.ConfigurationLocked"/>. After successful execution,
        /// the <see cref="ConfigurationLocked"/> property will immediately be set to <c>true</c>.
        /// </para>
        /// <para>
        /// Once the lock code is set, no changes can be made to the YubiKey's user-settable
        /// <see cref="IYubiKeyDeviceInfo"/> values. This will block operations that attempt to modify
        /// those values, such as <see cref="SetEnabledUsbCapabilities(YubiKeyCapabilities)"/>,
        /// <see cref="SetAutoEjectTimeout(int)"/>, and even this one
        /// (<see cref="LockConfiguration(ReadOnlySpan{byte})"/>). The lock code can be removed by
        /// calling <see cref="UnlockConfiguration(ReadOnlySpan{byte})"/>.
        /// </para>
        /// </remarks>
        /// <param name="lockCode">
        /// This lock code must have a length equal to
        /// <see cref="MgmtCmd.SetDeviceInfoBaseCommand.LockCodeLength"/>, and cannot
        /// be all zeros.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The length of <paramref name="lockCode"/> is invalid, or it contains all zeros.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The command failed to complete.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// An error occurred when attempting to connect to the device.
        /// </exception>
        /// <seealso cref="MgmtCmd.SetDeviceInfoCommand"/>
        void LockConfiguration(ReadOnlySpan<byte> lockCode);

        /// <summary>
        /// Removes the configuration lock code, allowing changes to YubiKey's user-settable
        /// <see cref="IYubiKeyDeviceInfo"/> values.
        /// </summary>
        /// <para>
        /// Requires firmware version &gt;= 5.0.0.
        /// </para>
        ///
        /// <remarks>
        /// <para>
        /// See <see cref="IYubiKeyDeviceInfo.ConfigurationLocked"/>. After successful execution,
        /// the <see cref="ConfigurationLocked"/> property will immediately be set to <c>false</c>.
        /// </para>
        /// <para>
        /// By removing the lock code, changes can be made to the YubiKey's user-settable
        /// <see cref="IYubiKeyDeviceInfo"/> values. To lock the
        /// configuration, use <see cref="LockConfiguration(ReadOnlySpan{byte})"/>.
        /// </para>
        /// <para>
        /// If this operation is attempted on a device that is already unlocked,
        /// <paramref name="lockCode"/> must be all zeros. Otherwise the operation will fail and
        /// throw an <see cref="InvalidOperationException"/>. In both cases, the device remains
        /// unlocked.
        /// </para>
        /// </remarks>
        /// <param name="lockCode">
        /// The lock code currently set on the YubiKey. This lock code must have a length equal to
        /// <see cref="MgmtCmd.SetDeviceInfoBaseCommand.LockCodeLength"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The length of <paramref name="lockCode"/> is invalid.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The command failed to complete.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// An error occurred when attempting to connect to the device.
        /// </exception>
        /// <seealso cref="MgmtCmd.SetDeviceInfoCommand"/>
        void UnlockConfiguration(ReadOnlySpan<byte> lockCode);

        /// <summary>
        /// Manage configuration settings on YubiKeys prior to firmware version 5.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the only configuration operation available to YubiKeys prior to firmware
        /// version 5. These YubiKeys have limited configuration settings, and all of
        /// them must be set at the same time. Important: once this operation succeeds, the YubiKey
        /// must be removed from the USB slot and then reinserted. This will allow the YubiKey to
        /// initialize all of the selected modes. This operation modifies the values related to
        /// <list type="bullet">
        /// <item><see cref="IYubiKeyDeviceInfo.EnabledUsbCapabilities"/></item>
        /// <item><see cref="IYubiKeyDeviceInfo.ChallengeResponseTimeout"/></item>
        /// <item><see cref="IYubiKeyDeviceInfo.DeviceFlags"/></item>
        /// <item><see cref="IYubiKeyDeviceInfo.AutoEjectTimeout"/></item>
        /// </list>
        /// </para>
        ///
        /// <para>
        /// Interfaces are a subset of the <see cref="YubiKeyCapabilities"/>:
        /// <inheritdoc
        ///     cref="MgmtCmd.SetLegacyDeviceConfigBase.YubiKeyInterfaces"
        ///     path="/remarks/*/UsbInterfaceRestrictions"
        /// />
        /// These interfaces enable or disable access to all applications that are dependent on it.
        /// </para>
        ///
        /// <para>
        /// For YubiKeys with at least firmware version 5, it is recommended to use the other
        /// configuration operations in <see cref="IYubiKeyDevice"/> since they provide more fine control.
        /// </para>
        /// </remarks>
        /// <param name="yubiKeyInterfaces">
        /// <para>
        /// The desired set of USB interfaces to enable on the YubiKey. Any non-interface values
        /// are ignored. A set flag means that the related interface is enabled. Otherwise, the
        /// interface is disabled. At least one available USB interface must be enabled.
        /// </para>
        ///
        /// <para>
        /// If <paramref name="touchEjectEnabled"/> is <see langword="true"/>, then only the
        /// <see cref="YubiKeyCapabilities.Ccid"/> interface can be enabled.
        /// </para>
        /// </param>
        /// <param name="challengeResponseTimeout">
        /// The length of the timeout in seconds. A value of <c>0</c> resets the timeout to its
        /// default duration.
        /// </param>
        /// <param name="touchEjectEnabled">
        /// <see langword="true"/> is the equivalent of setting <see cref="DeviceFlags.TouchEject"/>.
        /// And <see langword="false"/> disables it.
        /// </param>
        /// <param name="autoEjectTimeout">
        /// <para>
        /// The length of the timeout in seconds. If <paramref name="touchEjectEnabled"/> is
        /// <see langword="false"/>, then the value must be <c>0</c>. Otherwise, the value can be in
        /// the range <see cref="ushort.MinValue"/> through <see cref="ushort.MaxValue"/>. Where a
        /// value of <c>0</c> means that the timeout is disabled (the smart card will not be ejected
        /// automatically).
        /// </para>
        /// <para>
        /// If this value is non-zero, then <paramref name="touchEjectEnabled"/> must be set to
        /// <see langword="true"/>.
        /// </para>
        /// </param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="InvalidOperationException">
        /// Either the command failed to complete, or the set of desired capabilities is invalid.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// An error occurred when attempting to connect to the device.
        /// </exception>
        /// <seealso cref="MgmtCmd.SetLegacyDeviceConfigCommand"/>
        void SetLegacyDeviceConfiguration(
            YubiKeyCapabilities yubiKeyInterfaces,
            byte challengeResponseTimeout,
            bool touchEjectEnabled,
            int autoEjectTimeout = 0);

        /// <summary>
        /// Sets the <see cref="IYubiKeyDeviceInfo.IsNfcRestricted"/> value.
        /// </summary>
        /// <remarks>
        /// <para>
        /// After successful execution, the <see cref="IsNfcRestricted"/> property
        /// will immediately reflect the new value.
        /// </para>
        /// <para>
        /// This operation requires the YubiKey's configuration to be unlocked (see
        /// <see cref="IYubiKeyDeviceInfo.ConfigurationLocked"/> and
        /// <see cref="UnlockConfiguration(ReadOnlySpan{byte})"/>.
        /// </para>
        /// </remarks>
        /// <param name="enabled">Set this value to true to restrict NFC, false to unrestrict.</param>
        /// <exception cref="InvalidOperationException">
        /// The command failed to complete.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// This feature is not supported on this YubiKey version.
        /// </exception>
        void SetIsNfcRestricted(bool enabled);

        /// <summary>
        /// Temporarily set the threshold at which a capacitive touch should be considered active.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The field is using arbitrary units and has a default value of `6`. A higher value increases the sensor
        /// threshold which has the effect of decreasing the sensitivity of the sensor. Lower values increase the
        /// sensitivity, but callers cannot reduce the threshold below the default value of `6` which is locked in at
        /// manufacturing time. The value should not be any higher than `255`.
        /// </para>
        /// <para>
        /// The value set here is only valid until the next time the YubiKey is power cycled. It does not persist.
        /// </para>
        /// <para>
        /// You should typically not ever need to adjust this value. This is primarily used in the context of automatic
        /// provisioning and testing where the YubiKey is being "touched" by electrically grounding the sensor.
        /// </para>
        /// </remarks>
        /// <param name="value">The touch threshold to apply to the YubiKey. Must be a value greater than `6`.</param>
        void SetTemporaryTouchThreshold(int value);

        /// <summary>
        /// Perform a device-wide factory reset on a YubiKey Bio Multi-protocol Edition key.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Resets ALL YubiKey applications (including FIDO and PIV) on the key to factory settings. This type of reset is only available on YubiKey Bio Multi-protocol Edition keys.
        /// </para>
        /// <para>
        /// A reset will delete all FIDO2 credentials, fingerprints, and associated information, remove the shared PIN, delete all PIV keys and certificates from PIV slots (except the F9 attestation slot), remove any information added to the PIV data elements, and set the PIV PUK and management key back to their factory default states.
        /// </para>
        /// </remarks>
        void DeviceReset();

        [Obsolete("Use the new Scp methods")]
        bool TryConnectScp03(
            YubiKeyApplication application,
            Yubico.YubiKey.Scp03.StaticKeys scp03Keys,
            [MaybeNullWhen(returnValue: false)] out IScp03YubiKeyConnection connection);

        [Obsolete("Use the new Scp methods")]
        bool TryConnectScp03(
            byte[] applicationId,
            Yubico.YubiKey.Scp03.StaticKeys scp03Keys,
            [MaybeNullWhen(returnValue: false)] out IScp03YubiKeyConnection connection);

        [Obsolete("Use the new Scp methods")]
        IScp03YubiKeyConnection ConnectScp03(YubiKeyApplication application, Yubico.YubiKey.Scp03.StaticKeys scp03Keys);
        [Obsolete("Use the new Scp methods")]
        IScp03YubiKeyConnection ConnectScp03(byte[] applicationId, Yubico.YubiKey.Scp03.StaticKeys scp03Keys);
    }
}
