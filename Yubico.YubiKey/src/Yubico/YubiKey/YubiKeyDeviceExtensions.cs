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
using Yubico.YubiKey.Scp03;

namespace Yubico.YubiKey
{
    /// <summary>
    /// A static class containing extension methods for <see cref="YubiKeyDevice"/>.
    /// </summary>
    public static class YubiKeyDeviceExtensions
    {
        /// <summary>
        /// Wrap an existing device to enable connections using Secure Channel Protocol 3 ("SCP03").
        /// </summary>
        /// <param name="device">The underlying YubiKey device to enable connections using SCP03 to.</param>
        /// <param name="staticKeys">The symmetric secret used to securely connect to the YubiKey device.</param>
        /// <returns>A wrapped <see cref="IYubiKeyDevice"/> that uses SCP03 to perform connections.</returns>
        /// <exception cref="ArgumentNullException">The YubiKey device was null.</exception>
        /// <exception cref="NotSupportedException">The YubiKey device does not have an available smart card interface.</exception>
        /// <remarks>
        /// <para>
        /// Use this method to communicate with YubiKeys over SCP03. 
        /// Call this method on a <see cref="YubiKeyDevice"/> you want to connect to using SCP03, and supply the 
        /// symmetric keys known to both you and the YubiKey in the <c>staticKeys</c> parameter.
        /// The method will return a 'wrapped' device; when you subsequently call <see cref="YubiKeyDevice.Connect(byte[])"/>
        /// or its overloads on the wrapped device, the connection will be secured using SCP03 with the 
        /// <c>staticKeys</c> supplied.
        /// </para>
        /// <para>
        /// See also the user's manual entry on
        /// <xref href="UsersManualScp03"> SCP03</xref>.
        /// </para>
        /// </remarks>
        /// <example>
        /// Connect to a YubiKey using the SCP03 default keys, start a PivSession, and execute a command:
        ///
        /// <code language="csharp">
        /// public int ConnectToPivUsingSCP03(YubiKeyDevice yubiKeyDevice)
        /// {
        ///   IYubiKeyDevice scp03Device = yubiKeyDevice.WithScp03(new StaticKeys());
        ///   
        ///   using PivSession piv = new PivSession(scp03Device);
        ///   var metadata = piv.GetMetadata(PivSlot.Pin);
        ///   return metadata.RetryCount;
        /// }
        /// </code>
        /// </example>
        public static IYubiKeyDevice WithScp03(this YubiKeyDevice device, StaticKeys staticKeys)
        {
            if (device is null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            if (!device.HasSmartCard)
            {
                throw new NotSupportedException(ExceptionMessages.CcidNotSupported);
            }

            return new Scp03YubiKeyDevice(device, staticKeys);
        }
    }
}
