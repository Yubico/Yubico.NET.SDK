// Copyright 2021 Yubico AB
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
using System.Globalization;
using System.Threading;
using Yubico.Core.Devices;
using Yubico.Core.Devices.Hid;
using Yubico.Core.Devices.SmartCard;
using Yubico.Core.Logging;
using Yubico.YubiKey.DeviceExtensions;
using Yubico.YubiKey.Scp03;
using MgmtCmd = Yubico.YubiKey.Management.Commands;

namespace Yubico.YubiKey
{
    public partial class YubiKeyDevice : IYubiKeyDevice
    {
        #region IYubiKeyDeviceInfo
        /// <inheritdoc />
        public YubiKeyCapabilities AvailableUsbCapabilities => _yubiKeyInfo.AvailableUsbCapabilities;

        /// <inheritdoc />
        public YubiKeyCapabilities EnabledUsbCapabilities => _yubiKeyInfo.EnabledUsbCapabilities;

        /// <inheritdoc />
        public YubiKeyCapabilities AvailableNfcCapabilities => _yubiKeyInfo.AvailableNfcCapabilities;

        /// <inheritdoc />
        public YubiKeyCapabilities EnabledNfcCapabilities => _yubiKeyInfo.EnabledNfcCapabilities;

        /// <inheritdoc />
        public YubiKeyCapabilities FipsApproved => _yubiKeyInfo.FipsApproved;

        /// <inheritdoc />
        public YubiKeyCapabilities FipsCapable => _yubiKeyInfo.FipsCapable;

        /// <inheritdoc />
        public YubiKeyCapabilities ResetBlocked => _yubiKeyInfo.ResetBlocked;

        /// <inheritdoc />
        public bool IsNfcRestricted => _yubiKeyInfo.IsNfcRestricted;

        /// <inheritdoc />
        public string? PartNumber => _yubiKeyInfo.PartNumber;

        /// <inheritdoc />
        public bool IsPinComplexityEnabled => _yubiKeyInfo.IsPinComplexityEnabled;

        /// <inheritdoc />
        public int? SerialNumber => _yubiKeyInfo.SerialNumber;

        /// <inheritdoc />
        public bool IsFipsSeries => _yubiKeyInfo.IsFipsSeries;

        /// <inheritdoc />
        public bool IsSkySeries => _yubiKeyInfo.IsSkySeries;

        /// <inheritdoc />
        public FormFactor FormFactor => _yubiKeyInfo.FormFactor;

        /// <inheritdoc />
        public FirmwareVersion FirmwareVersion => _yubiKeyInfo.FirmwareVersion;

        /// <inheritdoc />
        public TemplateStorageVersion? TemplateStorageVersion => _yubiKeyInfo.TemplateStorageVersion;

        /// <inheritdoc />
        public ImageProcessorVersion? ImageProcessorVersion => _yubiKeyInfo.ImageProcessorVersion;

        /// <inheritdoc />
        public int AutoEjectTimeout => _yubiKeyInfo.AutoEjectTimeout;

        /// <inheritdoc />
        public byte ChallengeResponseTimeout => _yubiKeyInfo.ChallengeResponseTimeout;

        /// <inheritdoc />
        public DeviceFlags DeviceFlags => _yubiKeyInfo.DeviceFlags;

        /// <inheritdoc />
        public bool ConfigurationLocked => _yubiKeyInfo.ConfigurationLocked;
        #endregion

        private const int _lockCodeLength = MgmtCmd.SetDeviceInfoBaseCommand.LockCodeLength;

        private static readonly ReadOnlyMemory<byte> _lockCodeAllZeros = new byte[_lockCodeLength];

        internal bool HasSmartCard => !(_smartCardDevice is null);
        internal bool HasHidFido => !(_hidFidoDevice is null);
        internal bool HasHidKeyboard => !(_hidKeyboardDevice is null);
        internal bool IsNfcDevice { get; private set; }

        private ISmartCardDevice? _smartCardDevice;
        private IHidDevice? _hidFidoDevice;
        private IHidDevice? _hidKeyboardDevice;
        private IYubiKeyDeviceInfo _yubiKeyInfo;
        private Transport _lastActiveTransport;

        private readonly Logger _log = Log.GetLogger();

        internal ISmartCardDevice GetSmartCardDevice() => _smartCardDevice!;

        /// <inheritdoc />
        public Transport AvailableTransports
        {
            get
            {
                Transport transports = Transport.None;

                if (HasHidKeyboard)
                {
                    transports |= Transport.HidKeyboard;
                }

                if (HasHidFido)
                {
                    transports |= Transport.HidFido;
                }

                if (HasSmartCard)
                {
                    transports |= IsNfcDevice ? Transport.NfcSmartCard : Transport.UsbSmartCard;
                }

                return transports;
            }
        }

        /// <summary>
        /// Constructs a <see cref="YubiKeyDevice"/> instance.
        /// </summary>
        /// <param name="device">A valid device; either a smart card, keyboard, or FIDO device.</param>
        /// <param name="info">The YubiKey device information that describes the device.</param>
        /// <exception cref="ArgumentException">An unrecognized device type was given.</exception>
        public YubiKeyDevice(IDevice device, IYubiKeyDeviceInfo info)
        {
            switch (device)
            {
                case ISmartCardDevice scardDevice:
                    _smartCardDevice = scardDevice;
                    break;
                case IHidDevice hidDevice when hidDevice.IsKeyboard():
                    _hidKeyboardDevice = hidDevice;
                    break;
                case IHidDevice hidDevice when hidDevice.IsFido():
                    _hidFidoDevice = hidDevice;
                    break;
                default:
                    throw new ArgumentException(ExceptionMessages.DeviceTypeNotRecognized, nameof(device));
            }

            _log.LogInformation("Created a YubiKeyDevice based on the {Transport} transport.", _lastActiveTransport);

            _yubiKeyInfo = info;
            IsNfcDevice = _smartCardDevice?.IsNfcTransport() ?? false;
            _lastActiveTransport = GetTransportIfOnlyDevice();
        }

        /// <summary>
        /// Construct a <see cref="YubiKeyDevice"/> instance.
        /// </summary>
        /// <param name="smartCardDevice"><see cref="ISmartCardDevice"/> for the YubiKey.</param>
        /// <param name="hidKeyboardDevice"><see cref="IHidDevice"/> for normal HID interaction with the YubiKey.</param>
        /// <param name="hidFidoDevice"><see cref="IHidDevice"/> for FIDO interaction with the YubiKey.</param>
        /// <param name="yubiKeyDeviceInfo"><see cref="IYubiKeyDeviceInfo"/> with remaining properties of the YubiKey.</param>
        public YubiKeyDevice(
            ISmartCardDevice? smartCardDevice,
            IHidDevice? hidKeyboardDevice,
            IHidDevice? hidFidoDevice,
            IYubiKeyDeviceInfo yubiKeyDeviceInfo)
        {
            _smartCardDevice = smartCardDevice;
            _hidFidoDevice = hidFidoDevice;
            _hidKeyboardDevice = hidKeyboardDevice;
            _yubiKeyInfo = yubiKeyDeviceInfo;
            IsNfcDevice = smartCardDevice?.IsNfcTransport() ?? false;
            _lastActiveTransport = GetTransportIfOnlyDevice(); // Must be after setting the three device fields.
        }

        /// <summary>
        /// Updates current <see cref="YubiKeyDevice" /> with a newly found device.
        /// </summary>
        /// <param name="device">
        /// A new operating system device that is known to belong to this YubiKey.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The device does not have the same ParentDeviceId, or
        /// The device is not of a recognizable type.
        /// </exception>
        public void Merge(IDevice device)
        {
            if (!((IYubiKeyDevice)this).HasSameParentDevice(device))
            {
                throw new ArgumentException(ExceptionMessages.CannotMergeDifferentParents);
            }

            MergeDevice(device);
        }

        /// <summary>
        /// Updates current <see cref="YubiKeyDevice"/> with new info from SmartCard device or HID device.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="info"></param>
        public void Merge(IDevice device, IYubiKeyDeviceInfo info)
        {
            // First merge the devices
            MergeDevice(device);

            // Then merge the YubiKey device information / metadata
            if (_yubiKeyInfo is YubiKeyDeviceInfo first && info is YubiKeyDeviceInfo second)
            {
                _yubiKeyInfo = first.Merge(second);
            }
            else
            {
                _yubiKeyInfo = info;
            }
        }

        private void MergeDevice(IDevice device)
        {
            switch (device)
            {
                case ISmartCardDevice scardDevice:
                    _smartCardDevice = scardDevice;
                    IsNfcDevice = scardDevice.IsNfcTransport();
                    break;
                case IHidDevice hidDevice when hidDevice.IsKeyboard():
                    _hidKeyboardDevice = hidDevice;
                    break;
                case IHidDevice hidDevice when hidDevice.IsFido():
                    _hidFidoDevice = hidDevice;
                    break;
                default:
                    throw new ArgumentException(ExceptionMessages.DeviceTypeNotRecognized, nameof(device));
            }

            _lastActiveTransport = GetTransportIfOnlyDevice();
        }

        bool IYubiKeyDevice.HasSameParentDevice(IDevice device) => HasSameParentDevice(device);

        internal protected bool HasSameParentDevice(IDevice device)
        {
            if (device is null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            // Never match on a missing parent ID
            if (device.ParentDeviceId is null)
            {
                return false;
            }

            return _smartCardDevice?.ParentDeviceId == device.ParentDeviceId
                || _hidFidoDevice?.ParentDeviceId == device.ParentDeviceId
                || _hidKeyboardDevice?.ParentDeviceId == device.ParentDeviceId;
        }

        /// <inheritdoc />
        public IYubiKeyConnection Connect(YubiKeyApplication yubikeyApplication)
        {
            _ = TryConnect(yubikeyApplication, null, true, out IYubiKeyConnection? returnValue);
            return returnValue!;
        }

        /// <inheritdoc />
        public IScp03YubiKeyConnection ConnectScp03(YubiKeyApplication yubikeyApplication, StaticKeys scp03Keys)
        {
            _ = TryConnectScp03(yubikeyApplication, null, scp03Keys, true, out IScp03YubiKeyConnection? returnValue);
            return returnValue!;
        }

        /// <inheritdoc />
        public IYubiKeyConnection Connect(byte[] applicationId)
        {
            _ = TryConnect(null, applicationId, true, out IYubiKeyConnection? returnValue);
            return returnValue!;
        }

        /// <inheritdoc />
        public IScp03YubiKeyConnection ConnectScp03(byte[] applicationId, StaticKeys scp03Keys)
        {
            _ = TryConnectScp03(null, applicationId, scp03Keys, true, out IScp03YubiKeyConnection? returnValue);
            return returnValue!;
        }

        /// <inheritdoc />
        public bool TryConnect(
            YubiKeyApplication application,
            [MaybeNullWhen(returnValue: false)]
            out IYubiKeyConnection connection) =>
        TryConnect(application, null, false, out connection);

        /// <inheritdoc />
        public bool TryConnectScp03(
            YubiKeyApplication application,
            StaticKeys scp03Keys,
            [MaybeNullWhen(returnValue: false)]
            out IScp03YubiKeyConnection connection) =>
        TryConnectScp03(application, null, scp03Keys, false, out connection);

        /// <inheritdoc />
        public bool TryConnect(
            byte[] applicationId,
            [MaybeNullWhen(returnValue: false)]
            out IYubiKeyConnection connection) =>
        TryConnect(null, applicationId, false, out connection);

        /// <inheritdoc />
        public bool TryConnectScp03(
            byte[] applicationId,
            StaticKeys scp03Keys,
            [MaybeNullWhen(returnValue: false)]
            out IScp03YubiKeyConnection connection) =>
        TryConnectScp03(null, applicationId, scp03Keys, false, out connection);

        // Calls the common code and throws an exception if there is a problem
        // and throwOnFail is true.
        private bool TryConnect(
            YubiKeyApplication? application,
            byte[]? applicationId,
            bool throwOnFail,
            [MaybeNullWhen(returnValue: false)]
            out IYubiKeyConnection connection)
        {
            IYubiKeyConnection? returnValue = Connect(application, applicationId, null);
            if (!(returnValue is null) || !throwOnFail)
            {
                connection = returnValue;
                return !(returnValue is null);
            }

            throw new NotSupportedException(ExceptionMessages.NoInterfaceAvailable);
        }

        private bool TryConnectScp03(
            YubiKeyApplication? application,
            byte[]? applicationId,
            StaticKeys scp03Keys,
            bool throwOnFail,
            [MaybeNullWhen(returnValue: false)]
            out IScp03YubiKeyConnection connection)
        {
            IYubiKeyConnection? returnValue = Connect(application, applicationId, scp03Keys);
            if (!(returnValue is null) && returnValue is IScp03YubiKeyConnection scp03Connection)
            {
                connection = scp03Connection;
                return true;
            }

            if (!throwOnFail)
            {
                connection = null;
                return false;
            }

            throw new NotSupportedException(ExceptionMessages.NoInterfaceAvailable);
        }

        // Common to all Connect methods.
        // If application is given, use it. If not, use applicationId.
        // If scp03Keys are given, make an SCP03 connection. If not, normal
        // connection.
        // If a connection is made, return it, if there's an error, return null.
        internal virtual IYubiKeyConnection? Connect(
            YubiKeyApplication? application,
            byte[]? applicationId,
            StaticKeys? scp03Keys)
        {
            _log.LogInformation(
                "YubiKey {Serial} connecting to {Application} application" + (scp03Keys is null ? "." : " over SCP03."),
                SerialNumber,
                application is null ? applicationId is null ? "Unknown" : applicationId.ToString()
                : Enum.GetName(typeof(YubiKeyApplication), application));

            if (application is null)
            {
                if (!(applicationId is null) && HasSmartCard && !(_smartCardDevice is null))
                {
                    _log.LogInformation("Connecting via the SmartCard interface.");
                    WaitForReclaimTimeout(Transport.SmartCard);
                    return scp03Keys is null
                        ? new SmartCardConnection(_smartCardDevice, applicationId)
                        : new Scp03Connection(_smartCardDevice, applicationId, scp03Keys);
                }

                _log.LogInformation(
                    (applicationId is null ? "No application given." : "No smart card interface present.") +
                    "Unable to establish connection to YubiKey.");

                return null;
            }

            if (!(scp03Keys is null))
            {
                if (HasSmartCard && !(_smartCardDevice is null))
                {
                    _log.LogInformation("Connecting via the SmartCard interface.");
                    WaitForReclaimTimeout(Transport.SmartCard);
                    return new Scp03Connection(_smartCardDevice, (YubiKeyApplication)application, scp03Keys);
                }

                return null;
            }

            // OTP application should prefer the HIDKeyboard transport, but fall back on smart card
            // if unavailable.
            if (application == YubiKeyApplication.Otp && HasHidKeyboard)
            {
                _log.LogInformation("Connecting via the Keyboard interface.");
                WaitForReclaimTimeout(Transport.HidKeyboard);
                return new KeyboardConnection(_hidKeyboardDevice!);
            }

            // FIDO applications should prefer the HIDFido transport, but fall back on smart card
            // if unavailable.
            if ((application == YubiKeyApplication.Fido2 || application == YubiKeyApplication.FidoU2f)
                && HasHidFido)
            {
                _log.LogInformation("Connecting via the FIDO interface.");
                WaitForReclaimTimeout(Transport.HidFido);
                return new FidoConnection(_hidFidoDevice!);
            }

            if (HasSmartCard && !(_smartCardDevice is null))
            {
                _log.LogInformation("Connecting via the SmartCard interface.");
                WaitForReclaimTimeout(Transport.SmartCard);
                return new SmartCardConnection(_smartCardDevice, (YubiKeyApplication)application);
            }

            _log.LogInformation("No smart card interface present. Unable to establish connection to YubiKey.");
            return null;
        }

        /// <inheritdoc/>
        public void SetEnabledNfcCapabilities(YubiKeyCapabilities yubiKeyCapabilities)
        {
            var command = new MgmtCmd.SetDeviceInfoCommand
            {
                EnabledNfcCapabilities = yubiKeyCapabilities,
                ResetAfterConfig = true,
            };

            IYubiKeyResponse response = SendConfiguration(command);

            if (response.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(response.StatusMessage);
            }
        }

        /// <inheritdoc/>
        public void SetEnabledUsbCapabilities(YubiKeyCapabilities yubiKeyCapabilities)
        {
            if ((AvailableUsbCapabilities & yubiKeyCapabilities) == YubiKeyCapabilities.None)
            {
                throw new InvalidOperationException(ExceptionMessages.MustEnableOneAvailableUsbCapability);
            }

            var command = new MgmtCmd.SetDeviceInfoCommand
            {
                EnabledUsbCapabilities = yubiKeyCapabilities,
                ResetAfterConfig = true,
            };

            IYubiKeyResponse response = SendConfiguration(command);

            if (response.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(response.StatusMessage);
            }
        }

        /// <inheritdoc/>
        public void SetChallengeResponseTimeout(int seconds)
        {
            if (seconds < 0 || seconds > byte.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds));
            }

            var command = new MgmtCmd.SetDeviceInfoCommand
            {
                ChallengeResponseTimeout = (byte)seconds,
            };

            IYubiKeyResponse response = SendConfiguration(command);

            if (response.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(response.StatusMessage);
            }
        }

        /// <inheritdoc/>
        public void SetAutoEjectTimeout(int seconds)
        {
            if (seconds < ushort.MinValue || seconds > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds));
            }

            var command = new MgmtCmd.SetDeviceInfoCommand
            {
                AutoEjectTimeout = seconds,
            };

            IYubiKeyResponse response = SendConfiguration(command);

            if (response.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(response.StatusMessage);
            }
        }

        /// <inheritdoc/>
        public void SetIsNfcRestricted(bool enabled)
        {
            if (!this.HasFeature(YubiKeyFeature.ManagementNfcRestricted))
            {
                throw new NotSupportedException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.NotSupportedByYubiKeyVersion));
            }

            var command = new MgmtCmd.SetDeviceInfoCommand
            {
                RestrictNfc = enabled
            };

            IYubiKeyResponse response = SendConfiguration(command);
            if (response.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(response.StatusMessage);
            }
        }

        /// <inheritdoc/>
        public void SetDeviceFlags(DeviceFlags deviceFlags)
        {
            var command = new MgmtCmd.SetDeviceInfoCommand
            {
                DeviceFlags = deviceFlags,
            };

            IYubiKeyResponse response = SendConfiguration(command);
            if (response.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(response.StatusMessage);
            }
        }

        /// <inheritdoc/>
        public void LockConfiguration(ReadOnlySpan<byte> lockCode)
        {
            if (lockCode.Length != _lockCodeLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.LockCodeWrongLength,
                        _lockCodeLength,
                        lockCode.Length),
                        nameof(lockCode));
            }

            if (lockCode.SequenceEqual(_lockCodeAllZeros.Span))
            {
                throw new ArgumentException(
                    ExceptionMessages.LockCodeAllZeroNotAllowed,
                    nameof(lockCode));
            }

            var command = new MgmtCmd.SetDeviceInfoCommand();
            command.SetLockCode(lockCode);

            IYubiKeyResponse response = SendConfiguration(command);
            if (response.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(response.StatusMessage);
            }
        }

        /// <inheritdoc/>
        public void UnlockConfiguration(ReadOnlySpan<byte> lockCode)
        {
            if (lockCode.Length != _lockCodeLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.LockCodeWrongLength,
                        _lockCodeLength,
                        lockCode.Length),
                        nameof(lockCode));
            }

            var command = new MgmtCmd.SetDeviceInfoCommand();
            command.ApplyLockCode(lockCode);
            command.SetLockCode(_lockCodeAllZeros.Span);

            IYubiKeyResponse response = SendConfiguration(command);

            if (response.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(response.StatusMessage);
            }
        }

        /// <inheritdoc/>
        public void SetLegacyDeviceConfiguration(
            YubiKeyCapabilities yubiKeyInterfaces,
            byte challengeResponseTimeout,
            bool touchEjectEnabled,
            int autoEjectTimeout)
        {
            #region argument checks
            // Keep only flags related to interfaces. This makes the operation easier for users
            // who may be doing bitwise operations on [Available/Enabled]UsbCapabilities.
            yubiKeyInterfaces &=
                YubiKeyCapabilities.Ccid
                | YubiKeyCapabilities.FidoU2f
                | YubiKeyCapabilities.Otp;

            // Check if at least one interface is enabled.
            if (yubiKeyInterfaces == YubiKeyCapabilities.None
                || (AvailableUsbCapabilities != YubiKeyCapabilities.None
                && (AvailableUsbCapabilities & yubiKeyInterfaces) == YubiKeyCapabilities.None))
            {
                throw new InvalidOperationException(ExceptionMessages.MustEnableOneAvailableUsbInterface);
            }

            if (touchEjectEnabled)
            {
                if (yubiKeyInterfaces != YubiKeyCapabilities.Ccid)
                {
                    throw new ArgumentException(
                        ExceptionMessages.TouchEjectTimeoutRequiresCcidOnly,
                        nameof(touchEjectEnabled));
                }

                if (autoEjectTimeout < ushort.MinValue || autoEjectTimeout > ushort.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(autoEjectTimeout));
                }
            }
            else
            {
                if (autoEjectTimeout != 0)
                {
                    throw new ArgumentException(
                        ExceptionMessages.AutoEjectTimeoutRequiresTouchEjectEnabled,
                        nameof(autoEjectTimeout));
                }
            }
            #endregion

            IYubiKeyResponse response;

            // Newer YubiKeys should use SetDeviceInfo
            if (FirmwareVersion.Major >= 5)
            {
                DeviceFlags deviceFlags =
                    touchEjectEnabled
                    ? DeviceFlags | DeviceFlags.TouchEject
                    : DeviceFlags & ~DeviceFlags.TouchEject;

                var setDeviceInfoCommand = new MgmtCmd.SetDeviceInfoCommand
                {
                    EnabledUsbCapabilities = yubiKeyInterfaces.ToDeviceInfoCapabilities(),
                    ChallengeResponseTimeout = challengeResponseTimeout,
                    AutoEjectTimeout = autoEjectTimeout,
                    DeviceFlags = deviceFlags,
                    ResetAfterConfig = true,
                };

                response = SendConfiguration(setDeviceInfoCommand);
            }
            else
            {
                var setLegacyDeviceConfigCommand = new MgmtCmd.SetLegacyDeviceConfigCommand(
                    yubiKeyInterfaces,
                    challengeResponseTimeout,
                    touchEjectEnabled,
                    autoEjectTimeout);

                response = SendConfiguration(setLegacyDeviceConfigCommand);
            }

            if (response.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(response.StatusMessage);
            }
        }

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">
        /// The YubiKey does not support this feature.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value is less than `6` or greater than `255`.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The YubiKey encountered an error and could not set the setting.
        /// </exception>
        public void SetTemporaryTouchThreshold(int value)
        {
            if (!this.HasFeature(YubiKeyFeature.TemporaryTouchThreshold))
            {
                throw new NotSupportedException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.NotSupportedByYubiKeyVersion));
            }

            if (value < 6 || value > 255)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            var command = new MgmtCmd.SetDeviceInfoCommand
            {
                TemporaryTouchThreshold = value
            };

            IYubiKeyResponse response = SendConfiguration(command);
            if (response.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(response.StatusMessage);
            }
        }

        private IYubiKeyResponse SendConfiguration(MgmtCmd.SetDeviceInfoBaseCommand baseCommand)
        {
            IYubiKeyConnection? connection = null;
            try
            {
                IYubiKeyCommand<IYubiKeyResponse> command;

                if (TryConnect(YubiKeyApplication.Management, out connection))
                {
                    command = new MgmtCmd.SetDeviceInfoCommand(baseCommand);
                }
                else if (TryConnect(YubiKeyApplication.Otp, out connection))
                {
                    command = new Otp.Commands.SetDeviceInfoCommand(baseCommand);
                }
                else if (TryConnect(YubiKeyApplication.FidoU2f, out connection))
                {
                    command = new U2f.Commands.SetDeviceInfoCommand(baseCommand);
                }
                else
                {
                    throw new NotSupportedException(ExceptionMessages.NoInterfaceAvailable);
                }

                return connection.SendCommand(command);
            }
            finally
            {
                connection?.Dispose();
            }
        }

        private IYubiKeyResponse SendConfiguration(
            MgmtCmd.SetLegacyDeviceConfigBase baseCommand)
        {
            IYubiKeyConnection? connection = null;
            try
            {
                IYubiKeyCommand<IYubiKeyResponse> command;

                if (TryConnect(YubiKeyApplication.Management, out connection))
                {
                    command = new MgmtCmd.SetLegacyDeviceConfigCommand(baseCommand);
                }
                else if (TryConnect(YubiKeyApplication.Otp, out connection))
                {
                    command = new Otp.Commands.SetLegacyDeviceConfigCommand(baseCommand);
                }
                else
                {
                    throw new NotSupportedException(ExceptionMessages.NoInterfaceAvailable);
                }

                return connection.SendCommand(command);
            }
            finally
            {
                connection?.Dispose();
            }
        }

        #region IEquatable<T> and IComparable<T>
        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (!(obj is IYubiKeyDevice other))
            {
                return false;
            }
            else
            {
                return Equals(other);
            }
        }

        /// <inheritdoc/>
        public bool Equals(IYubiKeyDevice other)
        {
            if (this is null && other is null)
            {
                return true;
            }

            if (this is null || other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (SerialNumber == null && other.SerialNumber == null)
            {
                // fingerprint match
                if (!Equals(FirmwareVersion, other.FirmwareVersion))
                {
                    return false;
                }

                return CompareTo(other) == 0;
            }
            else if (SerialNumber == null || other.SerialNumber == null)
            {
                return false;
            }
            else
            {
                return SerialNumber.Equals(other.SerialNumber);
            }
        }

        /// <inheritdoc/>
        bool IYubiKeyDevice.Contains(IDevice other) => Contains(other);

        /// <inheritdoc/>
        internal protected bool Contains(IDevice other) =>
            other switch
            {
                ISmartCardDevice scDevice => scDevice.Path == _smartCardDevice?.Path,
                IHidDevice hidDevice => hidDevice.Path == _hidKeyboardDevice?.Path ||
                                        hidDevice.Path == _hidFidoDevice?.Path,
                _ => false
            };

        /// <inheritdoc/>
        public int CompareTo(IYubiKeyDevice other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            if (SerialNumber == null && other.SerialNumber == null)
            {
                if (!(other is YubiKeyDevice concreteKey))
                {
                    return 1;
                }

                if (HasSmartCard)
                {
                    int delta = string.Compare(_smartCardDevice!.Path, concreteKey._smartCardDevice!.Path, StringComparison.Ordinal);
                    if (delta != 0)
                    {
                        return delta;
                    }
                }
                else if (concreteKey.HasSmartCard)
                {
                    return -1;
                }

                if (HasHidFido)
                {
                    int delta = string.Compare(_hidFidoDevice!.Path, concreteKey._hidFidoDevice!.Path, StringComparison.Ordinal);
                    if (delta != 0)
                    {
                        return delta;
                    }
                }
                else if (concreteKey.HasHidFido)
                {
                    return -1;
                }

                if (HasHidKeyboard)
                {
                    int delta = string.Compare(_hidKeyboardDevice!.Path, concreteKey._hidKeyboardDevice!.Path, StringComparison.Ordinal);
                    if (delta != 0)
                    {
                        return delta;
                    }
                }
                else if (concreteKey.HasHidFido)
                {
                    return -1;
                }

                return 0;
            }
            else if (SerialNumber == null)
            {
                return -1;
            }
            else if (other.SerialNumber == null)
            {
                return 1;
            }
            else
            {
                return SerialNumber.Value.CompareTo(other.SerialNumber.Value);
            }
        }

        /// <inheritdoc/>
        public static bool operator ==(YubiKeyDevice left, YubiKeyDevice right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(YubiKeyDevice left, YubiKeyDevice right) => !(left == right);

        /// <inheritdoc/>
        public static bool operator <(YubiKeyDevice left, YubiKeyDevice right) =>
            left is null ? right is object : left.CompareTo(right) < 0;

        /// <inheritdoc/>
        public static bool operator <=(YubiKeyDevice left, YubiKeyDevice right) =>
            left is null || left.CompareTo(right) <= 0;

        /// <inheritdoc/>
        public static bool operator >(YubiKeyDevice left, YubiKeyDevice right) =>
            left is object && left.CompareTo(right) > 0;

        /// <inheritdoc/>
        public static bool operator >=(YubiKeyDevice left, YubiKeyDevice right) =>
            left is null ? right is null : left.CompareTo(right) >= 0;
        #endregion

        #region System.Object overrides
        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return !(SerialNumber is null)
                ? SerialNumber!.GetHashCode()
                : HashCode.Combine(
                    _smartCardDevice?.Path,
                    _hidFidoDevice?.Path,
                    _hidKeyboardDevice?.Path);
        }

        private static readonly string EOL = Environment.NewLine;
        /// <inheritdoc/>
        public override string ToString()
        {
            string res = "- Firmware Version: " + FirmwareVersion + EOL
                + "- Serial Number: " + SerialNumber + EOL
                + "- Form Factor: " + FormFactor + EOL
                + "- FIPS: " + IsFipsSeries + EOL
                + "- SKY: " + IsSkySeries + EOL
                + "- Has SmartCard: " + HasSmartCard + EOL
                + "- Has HID FIDO: " + HasHidFido + EOL
                + "- Has HID Keyboard: " + HasHidKeyboard + EOL
                + "- Available USB Capabilities: " + AvailableUsbCapabilities + EOL
                + "- Available NFC Capabilities: " + AvailableNfcCapabilities + EOL
                + "- Enabled USB Capabilities: " + EnabledUsbCapabilities + EOL
                + "- Enabled NFC Capabilities: " + EnabledNfcCapabilities + EOL;
            return res;
        }
        #endregion

        private DateTime GetLastActiveTime() =>
            _lastActiveTransport switch
            {
                Transport.SmartCard when _smartCardDevice is { } => _smartCardDevice.LastAccessed,
                Transport.HidFido when _hidFidoDevice is { } => _hidFidoDevice.LastAccessed,
                Transport.HidKeyboard when _hidKeyboardDevice is { } => _hidKeyboardDevice.LastAccessed,
                Transport.None => DateTime.Now,
                _ => throw new InvalidOperationException(ExceptionMessages.DeviceTypeNotRecognized)
            };

        // If this YubiKey only has a single active USB transport attached to it, we do not really need to worry about
        // the reclaim timeout. This can help speed up the first access of the device as we know for a fact that there
        // is no transport switching involved. In the case that there are multiple transports present, we set the
        // transport to "none" to force the wait on first device access. We must force this wait because enumeration
        // cheats by not waiting between switching transports.
        private Transport GetTransportIfOnlyDevice()
        {
            if (HasHidKeyboard && !HasHidFido && !HasSmartCard)
            {
                return Transport.HidKeyboard;
            }

            if (HasHidFido && !HasHidKeyboard && !HasSmartCard)
            {
                return Transport.HidFido;
            }

            if (HasSmartCard && !HasHidKeyboard && !HasHidFido)
            {
                return Transport.SmartCard;
            }

            return Transport.None;
        }

        // This function handles waiting for the reclaim timeout on the YubiKey to elapse. The reclaim timeout requires
        // the SDK to wait 3 seconds since the last USB message to an interface before switching to a different interface.
        // Failure to wait can result in very strange behavior from the USB devices ultimately resulting in communication
        // failures (i.e. exceptions).
        private void WaitForReclaimTimeout(Transport newTransport)
        {
            // Newer YubiKeys are able to switch interfaces much, much faster. Maybe this is being paranoid, but we
            // should still probably wait a few milliseconds for things to stabilize. But definitely not the full
            // three seconds! For older keys, we use a value of 3.01 seconds to give us a little wiggle room as the
            // YubiKey's measurement for the reclaim timout is likely not as accurate as our system clock.
            TimeSpan reclaimTimeout = CanFastReclaim() ? TimeSpan.FromMilliseconds(100) : TimeSpan.FromSeconds(3.01);

            // We're only affected by the reclaim timeout if we're switching USB transports.
            if (_lastActiveTransport == newTransport)
            {
                _log.LogInformation(
                    "{Transport} transport is already active. No need to wait for reclaim.",
                    _lastActiveTransport);

                return;
            }

            _log.LogInformation(
                "Switching USB transports from {OldTransport} to {NewTransport}.",
                _lastActiveTransport,
                newTransport);

            TimeSpan timeSinceLastActivation = DateTime.Now - GetLastActiveTime();

            // If we haven't already waited the duration of the reclaim timeout, we need to do so.
            // Otherwise, we've already waited and can immediately switch the transport.
            if (timeSinceLastActivation < reclaimTimeout)
            {
                TimeSpan waitNeeded = reclaimTimeout - timeSinceLastActivation;

                _log.LogInformation(
                    "Reclaim timeout still active. Need to wait {TimeMS} milliseconds.",
                    waitNeeded.TotalMilliseconds);

                Thread.Sleep(waitNeeded);
            }

            _lastActiveTransport = newTransport;

            _log.LogInformation("Reclaim timeout has lapsed. It is safe to switch USB transports.");
        }

        private bool CanFastReclaim()
        {
            if (AppContext.TryGetSwitch(YubiKeyCompatSwitches.UseOldReclaimTimeoutBehavior, out bool useOldBehavior) &&
                useOldBehavior)
            {
                return false;
            }

            return this.HasFeature(YubiKeyFeature.FastUsbReclaim);
        }
    }
}
