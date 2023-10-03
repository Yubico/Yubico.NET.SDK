// Copyright 2023 Yubico AB
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
using Yubico.YubiKey.Scp03;

namespace Yubico.YubiKey
{
    /// <summary>
    /// A static class containing extension methods for YubiKeyDevide objects,
    /// such as <see cref="WithScp03"/>.
    /// </summary>
    public static class YubiKeyDeviceExtensions
    {
        /// <summary>
        /// Use this method to make sure any Smart Card communication is
        /// conducted using SCP03. If the input <c>device</c> is already set for
        /// SCP03, this method will verify that the input <c>staticKeys</c> are
        /// the same as those used to build the <c>device</c> and if they are,
        /// simply return the input device.
        /// </summary>
        /// <remarks>
        /// This method of making an SCP03 connection is deprecated. See the
        /// <xref href="UsersManualScp03">User's Manual entry</xref> on SCP03 for
        /// information on how best to make an SCP03 connection..
        /// <para>
        /// The YubiKey itself is represented by an instance of
        /// <see cref="YubiKeyDevice"/>. After choosing the YubiKey to use, you
        /// can specify that Smart Card communications be conduced using SCP03.
        /// The key set you supply will be the keys used to encrypt and
        /// authenticate/verify. That key set must already be loaded onto the
        /// YubiKey.
        /// </para>
        /// <para>
        /// For example, use SCP03 in a PivSession.
        /// <code language="csharp">
        ///  if (!YubiKeyDevice.TryGetYubiKey(serialNumber, out IYubiKeyDevice yubiKeyDevice))
        ///  {
        ///      // error, can't find YubiKey
        ///  }
        ///  yubiKeyDevice = (YubiKeyDevice)yubiKeyDevice.WithScp03(scp03Keys);
        ///  using (var pivSession = new pivSession(yubiKeyDevice))
        ///  {
        ///    . . .
        ///  }
        /// </code>
        /// </para>
        /// </remarks>
        /// <param name="device">
        /// The underlying YubiKey device for which SCP03 communications are to
        /// be used.
        /// </param>
        /// <param name="scp03Keys">
        /// The symmetric key set to use. This call will copy the keys (not just
        /// a reference).
        /// </param>
        /// <returns>
        /// A wrapped <see cref="YubiKeyDevice"/> that uses SCP03.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <c>device</c> or <c>scp03Keys</c> arg is null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The YubiKey device does not have an available smart card interface or
        /// it does not support SCP03.
        /// </exception>
        [Obsolete("The WithScp03 extension will be deprecated, please specify SCP03 during the Connect call.", error: false)]
        public static IYubiKeyDevice WithScp03(this YubiKeyDevice device, StaticKeys scp03Keys) =>
            GetScp03Device(device, scp03Keys);

        /// <summary>
        /// This is the same as <c>WithScp03</c>, except it returns an
        /// <c>Scp03YubiKeyDevice</c> instead of an <c>IYubiKeyDevice</c>.
        /// Internally, we want an instance of this class, not a non-specific
        /// object.
        /// </summary>
        internal static Scp03YubiKeyDevice GetScp03Device(this IYubiKeyDevice device, StaticKeys scp03Keys)
        {
            if (device is null)
            {
                throw new ArgumentNullException(nameof(device));
            }
            if (scp03Keys is null)
            {
                throw new ArgumentNullException(nameof(scp03Keys));
            }

            if (device is Scp03YubiKeyDevice scp03Device)
            {
                if (scp03Device.StaticKeys.AreKeysSame(scp03Keys))
                {
                    return scp03Device;
                }

                throw new ArgumentException(ExceptionMessages.Scp03KeyMismatch);
            }

            if (device is YubiKeyDevice yubiKeyDevice)
            {
                if (!yubiKeyDevice.HasSmartCard)
                {
                    throw new NotSupportedException(ExceptionMessages.CcidNotSupported);
                }

                return new Scp03YubiKeyDevice(yubiKeyDevice, scp03Keys);
            }

            throw new NotSupportedException(ExceptionMessages.NotSupportedByYubiKeyVersion);
        }
    }
}
