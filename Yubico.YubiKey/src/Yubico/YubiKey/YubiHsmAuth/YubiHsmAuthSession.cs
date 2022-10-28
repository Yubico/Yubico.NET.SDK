// Copyright 2022 Yubico AB
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
using Yubico.YubiKey.InterIndustry.Commands;
using Yubico.YubiKey.YubiHsmAuth.Commands;

namespace Yubico.YubiKey.YubiHsmAuth
{
    /// <summary>
    /// The main entry-point for all YubiHSM Auth related operations.
    /// </summary>
    public sealed partial class YubiHsmAuthSession : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// The object that represents the connection to the YubiKey.
        /// </summary>
        public IYubiKeyConnection Connection { get; private set; }

        // The default constructor explicitly defined. We don't want it to be used.
        private YubiHsmAuthSession()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create an instance of <c>YubiHsmAuthSession</c> class, the object
        /// that represents the YubiHSM Auth application on the YubiKey.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The YubiHSM Auth application is available in YubiKey firmware
        /// version 5.4.3 and later. You can check if the application is
        /// available by using
        /// <see cref="YubiKeyFeatureExtensions.HasFeature(IYubiKeyDevice, YubiKeyFeature)"/>,
        /// and see if it's enabled over the desired interface by checking
        /// <see cref="IYubiKeyDeviceInfo.EnabledUsbCapabilities"/> or
        /// <see cref="IYubiKeyDeviceInfo.EnabledNfcCapabilities"/>.
        /// </para>
        /// <para>
        /// Because this class implements <c>IDisposable</c>, use the
        /// <c>using</c> keyword. For example,
        /// <code language="csharp">
        ///     IYubiKeyDevice yubiKeyToUse = SelectYubiKey();
        ///     using (var yubiHsmAuth = new YubiHsmAuthSession(yubiKeyToUse))
        ///     {
        ///         /* Perform YubiHSM Auth operations. */
        ///     }
        /// </code>
        /// </para>
        /// </remarks>
        /// <param name="yubiKey">
        /// The object that represents the actual YubiKey which will perform the operations.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>yubiKey</c> argument is null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Failed to connect to the YubiHSM Auth application.
        /// </exception>
        public YubiHsmAuthSession(IYubiKeyDevice yubiKey)
        {
            if (yubiKey is null)
            {
                throw new ArgumentNullException(nameof(yubiKey));
            }

            Connection = yubiKey.Connect(YubiKeyApplication.YubiHsmAuth);

            _disposed = false;
        }

        /// <summary>
        /// Reset the YubiHSM Auth application, which will delete all credentials,
        /// reset the management key to its default value (all zeros), and reset
        /// the management key retry counter to 8.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The command to reset the application failed.
        /// </exception>
        public void ResetApplication()
        {
            ResetApplicationResponse resetResponse = Connection.SendCommand(new ResetApplicationCommand());

            if (resetResponse.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(resetResponse.StatusMessage);
            }
        }

        /// <summary>
        /// Get the version of the YubiHSM Auth application.
        /// </summary>
        /// <returns>
        /// The application version as a major, minor, and patch value.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The command to get the application version failed.
        /// </exception>
        public ApplicationVersion GetApplicationVersion()
        {
            GetApplicationVersionResponse applicationVersionResponse = Connection.SendCommand(new GetApplicationVersionCommand());

            if (applicationVersionResponse.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(applicationVersionResponse.StatusMessage);
            }

            return applicationVersionResponse.GetData();
        }

        /// <summary>
        /// When the YubiHsmAuthSession object goes out of scope, this method is called. It will close the session.
        /// </summary>
        // Note that .NET recommends a Dispose method call Dispose(true) and GC.SuppressFinalize(this).
        // The actual disposal is in the Dispose(bool) method.
        //
        // However, that does not apply to sealed classes. So the Dispose method will simply perform the
        // "closing" process, no call to Dispose(bool) or GC.
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            // At the moment, there is no "close session" method. So for now,
            // just connect to the management application.
            _ = Connection.SendCommand(new SelectApplicationCommand(YubiKeyApplication.Management));
            Connection.Dispose();
            _disposed = true;
        }
    }
}
