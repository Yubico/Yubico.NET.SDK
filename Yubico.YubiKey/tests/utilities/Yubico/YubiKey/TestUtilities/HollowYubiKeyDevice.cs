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
using Yubico.Core.Devices;
using Yubico.YubiKey.Scp03;

namespace Yubico.YubiKey.TestUtilities
{
    // This is a class that implements IYubiKeyDevice. However, it is hollow, because
    // there's nothing in it. It can't really do anything.
    // But it can be used in testing.
    // Suppose you are testing a feature that requires an actual connection to a
    // YubiKey. As long as that test does not actually need to contact a real
    // YubiKey, it can use this Hollow object. It is possible to get an instance
    // of an object that implements IYubiKeyDevice without requiring an actual YubiKey.
    // Now make a hollow connection and run the tests. If some operation tries to
    // actually send a command, this object will throw an exception.
    public sealed class HollowYubiKeyDevice : IYubiKeyDevice
    {
        private readonly bool _alwaysAuthenticatePiv;

        public bool HasSmartCard { get; }
        public bool HasHidFido { get; }
        public bool HasHidKeyboard { get; }

        public Transport AvailableTransports => Transport.All;

        #region IYubiKeyDeviceInfo

        /// <inheritdoc />
        public YubiKeyCapabilities AvailableUsbCapabilities { get; set; }

        /// <inheritdoc />
        public YubiKeyCapabilities EnabledUsbCapabilities { get; private set; }

        /// <inheritdoc />
        public YubiKeyCapabilities AvailableNfcCapabilities { get; private set; }

        /// <inheritdoc />
        public YubiKeyCapabilities EnabledNfcCapabilities { get; private set; }

        /// <inheritdoc />
        public YubiKeyCapabilities FipsApproved { get; private set; }

        /// <inheritdoc />
        public YubiKeyCapabilities FipsCapable { get; private set; }

        public YubiKeyCapabilities ResetBlocked { get; private set; }

        public bool IsNfcRestricted { get; } = false;
        public string PartNumber { get; } = string.Empty;
        public bool IsPinComplexityEnabled { get; } = false;

        /// <inheritdoc />
        public int? SerialNumber { get; private set; }

        /// <inheritdoc />
        public bool IsFipsSeries { get; private set; }

        /// <inheritdoc />
        public bool IsSkySeries { get; private set; }

        /// <inheritdoc />
        public FormFactor FormFactor { get; private set; }

        /// <inheritdoc />
        public FirmwareVersion FirmwareVersion { get; set; }

        /// <inheritdoc />
        public TemplateStorageVersion TemplateStorageVersion { get; set; }

        /// <inheritdoc />
        public ImageProcessorVersion ImageProcessorVersion { get; set; }

        /// <inheritdoc />
        public int AutoEjectTimeout { get; private set; }

        /// <inheritdoc />
        public byte ChallengeResponseTimeout { get; private set; }

        /// <inheritdoc />
        public DeviceFlags DeviceFlags { get; private set; }

        /// <inheritdoc />
        public bool ConfigurationLocked { get; private set; }

        #endregion

        // If no arg is given, the object will never authenticate or verify PIV
        // elements. But if it is given and it is true, any time PIV auth or
        // verification is requested, it will do so, assuming the caller uses the
        // default key or PIN.
        public HollowYubiKeyDevice(bool alwaysAuthenticatePiv = false)
        {
            _alwaysAuthenticatePiv = alwaysAuthenticatePiv;

            HasSmartCard = false;
            HasHidFido = false;
            HasHidKeyboard = false;

            // We initialize this to zeros, but if you need a version,
            // the setter is public. HollowConnection takes a version
            // and feeds it back in the ReadStatusCommand.
            FirmwareVersion = new FirmwareVersion
            {
                Major = 0,
                Minor = 0,
                Patch = 0
            };
            TemplateStorageVersion = new TemplateStorageVersion
            {
                Major = 0,
                Minor = 0,
                Patch = 0
            };
            ImageProcessorVersion = new ImageProcessorVersion
            {
                Major = 0,
                Minor = 0,
                Patch = 0
            };

            SerialNumber = null;
            FormFactor = FormFactor.Unknown;
            AutoEjectTimeout = 0;
            ConfigurationLocked = false;
            AvailableNfcCapabilities = 0;
            AvailableUsbCapabilities = 0;
            EnabledNfcCapabilities = 0;
            EnabledUsbCapabilities = 0;
        }

        public IYubiKeyConnection Connect(YubiKeyApplication yubikeyApplication)
        {
            var connection = new HollowConnection(yubikeyApplication, FirmwareVersion)
            {
                AlwaysAuthenticatePiv = _alwaysAuthenticatePiv,
            };

            return connection;
        }

        public IScp03YubiKeyConnection ConnectScp03(YubiKeyApplication yubikeyApplication, StaticKeys scp03Keys)
        {
            throw new NotImplementedException();
        }

        public IYubiKeyConnection Connect(byte[] applicationId)
        {
            throw new NotImplementedException();
        }

        public IScp03YubiKeyConnection ConnectScp03(byte[] applicationId, StaticKeys scp03Keys)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        bool IYubiKeyDevice.HasSameParentDevice(IDevice other)
        {
            return false;
        }

        bool IYubiKeyDevice.TryConnect(
            YubiKeyApplication application,
            out IYubiKeyConnection connection)
        {
            throw new NotImplementedException();
        }

        bool IYubiKeyDevice.TryConnectScp03(
            YubiKeyApplication application,
            StaticKeys scp03Keys,
            out IScp03YubiKeyConnection connection)
        {
            throw new NotImplementedException();
        }

        bool IYubiKeyDevice.TryConnect(
            byte[] applicationId,
            out IYubiKeyConnection connection)
        {
            throw new NotImplementedException();
        }

        bool IYubiKeyDevice.TryConnectScp03(
            byte[] applicationId,
            StaticKeys scp03Keys,
            out IScp03YubiKeyConnection connection)
        {
            throw new NotImplementedException();
        }

        public void SetEnabledNfcCapabilities(YubiKeyCapabilities yubiKeyCapabilities) =>
            throw new NotImplementedException();

        public void SetEnabledUsbCapabilities(YubiKeyCapabilities yubiKeyCapabilities) =>
            throw new NotImplementedException();

        public void SetChallengeResponseTimeout(int seconds) =>
            throw new NotImplementedException();

        public int CompareTo(IYubiKeyDevice? other)
        {
            return 1;
        }

        public bool Equals(IYubiKeyDevice? other)
        {
            return false;
        }

        bool IYubiKeyDevice.Contains(IDevice other)
        {
            return false;
        }

        public void SetAutoEjectTimeout(int seconds)
        {
            throw new NotImplementedException();
        }

        public void SetDeviceFlags(DeviceFlags deviceFlags)
        {
            throw new NotImplementedException();
        }

        public void LockConfiguration(ReadOnlySpan<byte> lockCode)
        {
            throw new NotImplementedException();
        }

        public void UnlockConfiguration(ReadOnlySpan<byte> lockCode)
        {
            throw new NotImplementedException();
        }

        public void SetLegacyDeviceConfiguration(
            YubiKeyCapabilities yubiKeyInterfaces, byte challengeResponseTimeout, bool touchEjectEnabled,
            int autoEjectTimeout = 0)
        {
            throw new NotImplementedException();
        }

        public void SetIsNfcRestricted(bool enabled)
        {
            throw new NotImplementedException();
        }

    }
}
