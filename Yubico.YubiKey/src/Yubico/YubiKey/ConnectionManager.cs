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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Yubico.Core.Devices;
using Yubico.Core.Devices.Hid;
using Yubico.Core.Devices.SmartCard;

namespace Yubico.YubiKey
{
    /// <summary>
    /// A service class that tracks open connections to the YubiKeys on the device.
    /// </summary>
    /// <remarks>
    ///
    /// </remarks>
    // JUSTIFICATION: This class is a singleton, which means its lifetime can span the process lifetime. It contains
    // a lock which is disposable, so we must call its Dispose method at some point. The only reasonable time to do that
    // is in this class's finalizer. This analyzer doesn't see this and still warns.
    #pragma warning disable CA1001
    internal class ConnectionManager
    #pragma warning restore CA1001
    {
        // Easy thread-safe singleton pattern using Lazy<>
        private static readonly Lazy<ConnectionManager> _instance =
            new Lazy<ConnectionManager>(() => new ConnectionManager());

        /// <summary>
        /// Gets the process-global singleton instance of the connection manager.
        /// </summary>
        public static ConnectionManager Instance => _instance.Value;

        /// <summary>
        /// Determines if the given device object supports the requested YubiKey application.
        /// </summary>
        /// <param name="device">A concrete device object from Yubico.Core.</param>
        /// <param name="application">An application of the YubiKey.</param>
        /// <returns>`true` if the device object supports the application, `false` otherwise.</returns>
        // This function uses C# 8.0 pattern matching to build a concise table.
        public static bool DeviceSupportsApplication(IDevice device, YubiKeyApplication application) =>
            (device, application) switch
            {
                // FIDO interface
                (IHidDevice { UsagePage: HidUsagePage.Fido }, YubiKeyApplication.FidoU2f) => true,
                (IHidDevice { UsagePage: HidUsagePage.Fido }, YubiKeyApplication.Fido2) => true,
                // Keyboard interface
                (IHidDevice { UsagePage: HidUsagePage.Keyboard }, YubiKeyApplication.Otp) => true,
                // All Smart Card based interfaces
                (ISmartCardDevice _, YubiKeyApplication.Management) => true,
                (ISmartCardDevice _, YubiKeyApplication.Oath) => true,
                (ISmartCardDevice _, YubiKeyApplication.Piv) => true,
                (ISmartCardDevice _, YubiKeyApplication.OpenPgp) => true,
                (ISmartCardDevice _, YubiKeyApplication.InterIndustry) => true,
                // NB: Certain past models of YK NEO and YK 4 supported these applications over CCID
                (ISmartCardDevice _, YubiKeyApplication.FidoU2f) => true,
                (ISmartCardDevice _, YubiKeyApplication.Fido2) => true,
                (ISmartCardDevice _, YubiKeyApplication.Otp) => true,
                // NFC interface
                (ISmartCardDevice { Kind: SmartCardConnectionKind.Nfc }, YubiKeyApplication.OtpNdef) => true,
                _ => false
            };

        private readonly HashSet<IYubiKeyDevice> _openConnections = new HashSet<IYubiKeyDevice>();
        private readonly ReaderWriterLockSlim _hashSetLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        /// <summary>
        /// Finalizes the ConnectionManager singleton
        /// </summary>
        /// <remarks>
        /// As this class contains a disposable resource, we need some place to properly dispose of it. The finalizer
        /// is the only real chance we get to invoke this.
        /// </remarks>
        ~ConnectionManager()
        {
            _hashSetLock.Dispose();
        }

        /// <summary>
        /// Tries to connect to a known application on the YubiKey, failing if a connection already exists.
        /// </summary>
        /// <param name="yubiKeyDevice">
        /// The YubiKey that we are attempting to connect to.
        /// </param>
        /// <param name="device">
        /// The actual physical device exposed by the YubiKey that we're attempting to connect to.
        /// </param>
        /// <param name="application">
        /// The YubiKey application that we're attempting to connect to.
        /// </param>
        /// <param name="connection">
        /// The connection, if established. `null` otherwise.
        /// </param>
        /// <returns>
        /// 'true' if the connection was established, 'false' if there is already an outstanding connection to this
        /// device. Note that this method throws exceptions for all other failure modes.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The specified device does not support requested application.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The device type is not recognized.
        /// </exception>
        public bool TryCreateConnection(
            IYubiKeyDevice yubiKeyDevice,
            IDevice device,
            YubiKeyApplication application,
            [MaybeNullWhen(returnValue: false)]
            out IYubiKeyConnection connection)
        {
            if (!DeviceSupportsApplication(device, application))
            {
                throw new ArgumentException(
                    "The specified device does not support this application.",
                    nameof(application));
            }

            // Since taking a write lock is potentially very expensive, let's try and make a best effort to see if the
            // YubiKey is already present. This way we can fail fast.
            _hashSetLock.EnterReadLock();

            try
            {
                if (_openConnections.Contains(yubiKeyDevice))
                {
                    connection = null;

                    return false;
                }
            }
            finally
            {
                _hashSetLock.ExitReadLock();
            }

            // The YubiKey wasn't present just a few microseconds ago, so hopefully we will be able to succeed in
            // establishing a connection to it.
            _hashSetLock.EnterWriteLock();

            try
            {
                // We still need to double check that the key wasn't added in the time between exiting the read lock
                // and entering the write lock.
                if (_openConnections.Contains(yubiKeyDevice))
                {
                    connection = null;

                    return false;
                }

                connection = device switch
                {
                    IHidDevice { UsagePage: HidUsagePage.Fido } d => new FidoConnection(d),
                    IHidDevice { UsagePage: HidUsagePage.Keyboard } d => new KeyboardConnection(d),
                    ISmartCardDevice d => new CcidConnection(d, application),
                    _ => throw new NotSupportedException("Device type not recognized.")
                };

                _ = _openConnections.Add(yubiKeyDevice);
            }
            finally
            {
                _hashSetLock.ExitWriteLock();
            }

            return true;
        }

        /// <summary>
        /// Tries to connect to an arbitrary application on the YubiKey, failing if a connection already exists.
        /// </summary>
        /// <param name="yubiKeyDevice"></param>
        /// <param name="device"></param>
        /// <param name="applicationId"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public bool TryCreateConnection(
            IYubiKeyDevice yubiKeyDevice,
            IDevice device,
            byte[] applicationId,
            [MaybeNullWhen(returnValue: false)]
            out IYubiKeyConnection connection)
        {
            var smartCardDevice = device as ISmartCardDevice;

            if (smartCardDevice == null)
            {
                throw new ArgumentException(
                    "The specified device does not support this application.",
                    nameof(applicationId));
            }

            // Since taking a write lock is potentially very expensive, let's try and make a best effort to see if the
            // YubiKey is already present. This way we can fail fast.
            _hashSetLock.EnterReadLock();

            try
            {
                if (_openConnections.Contains(yubiKeyDevice))
                {
                    connection = null;

                    return false;
                }
            }
            finally
            {
                _hashSetLock.ExitReadLock();
            }

            // The YubiKey wasn't present just a few microseconds ago, so hopefully we will be able to succeed in
            // establishing a connection to it.
            _hashSetLock.EnterWriteLock();

            try
            {
                // We still need to double check that the key wasn't added in the time between exiting the read lock
                // and entering the write lock.
                if (_openConnections.Contains(yubiKeyDevice))
                {
                    connection = null;

                    return false;
                }

                connection = new CcidConnection(smartCardDevice, applicationId);
            }
            finally
            {
                _hashSetLock.ExitWriteLock();
            }

            return true;
        }

        public void EndConnection(IYubiKeyDevice yubiKeyDevice)
        {
            // Since taking a write lock is potentially very expensive, let's try and make a best effort to see if the
            // YubiKey is still present. This way we can fail fast.
            _hashSetLock.EnterReadLock();

            try
            {
                if (!_openConnections.Contains(yubiKeyDevice))
                {
                    throw new KeyNotFoundException("No active connections for that YubiKey were found.");
                }
            }
            finally
            {
                _hashSetLock.ExitReadLock();
            }

            _hashSetLock.EnterWriteLock();

            try
            {
                if (!_openConnections.Remove(yubiKeyDevice))
                {
                    throw new KeyNotFoundException("No active connections for that YubiKey were found.");
                }
            }
            finally
            {
                _hashSetLock.ExitWriteLock();
            }
        }
   }
}
