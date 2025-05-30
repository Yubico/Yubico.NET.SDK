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
using System.Globalization;
using Yubico.Core.Logging;
using Yubico.YubiKey.Oath;
using Yubico.YubiKey.Scp;
using Yubico.YubiKey.YubiHsmAuth.Commands;

namespace Yubico.YubiKey.YubiHsmAuth
{
    /// <summary>
    /// The main entry-point for all YubiHSM Auth related operations.
    /// </summary>
    public sealed partial class YubiHsmAuthSession : ApplicationSession
    {
        /// <summary>
        /// The delegate this class will call when it needs a management key or
        /// credential password.
        /// </summary>
        /// <remarks>
        /// The delegate provided will read the <see cref="KeyEntryData"/> which
        /// contains the information needed to determine what to collect and
        /// methods to submit what was collected. The delegate will return
        /// <c>true</c> for success or <c>false</c> for "cancel". Cancellation
        /// usually happens when the user has clicked a "Cancel" button. That is
        /// often the case when the user has entered the wrong value a number of
        /// times, and they would like to stop trying before the application is
        /// blocked.
        /// <para>
        /// Note that the SDK will call the <c>KeyCollector</c> with a
        /// <see cref="KeyEntryData.Request"/> of
        /// <see cref="KeyEntryRequest.Release"/> when the process completes. In
        /// this case, the <c>KeyCollector</c> MUST NOT throw an exception. The
        /// <c>Release</c> is called from inside a <c>finally</c> block, and it
        /// is a bad idea to throw exceptions from inside <c>finally</c>.
        /// </para>
        /// </remarks>
        public Func<KeyEntryData, bool>? KeyCollector { get; set; }

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
        /// <param name="keyParameters">If supplied, the parameters used open the SCP connection.</param>
        /// <exception cref="ArgumentNullException">
        /// The <c>yubiKey</c> argument is null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Failed to connect to the YubiHSM Auth application.
        /// </exception>
        public YubiHsmAuthSession(IYubiKeyDevice yubiKey, ScpKeyParameters? keyParameters = null)
            : base(Log.GetLogger<YubiHsmAuthSession>(), yubiKey, YubiKeyApplication.YubiHsmAuth, keyParameters)
        {
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
            var response = Connection.SendCommand(new ResetApplicationCommand());
            if (response.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(response.StatusMessage);
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
            var applicationVersionResponse = Connection.SendCommand(new GetApplicationVersionCommand());

            if (applicationVersionResponse.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(applicationVersionResponse.StatusMessage);
            }

            return applicationVersionResponse.GetData();
        }

        // Returns the KeyCollector. Throws exception if it's null.
        private Func<KeyEntryData, bool> GetKeyCollector()
        {
            if (KeyCollector is null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.MissingKeyCollector));
            }

            return KeyCollector;
        }
    }
}
