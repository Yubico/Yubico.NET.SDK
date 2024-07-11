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
using System.Globalization;
using Yubico.YubiKey.InterIndustry.Commands;
using Yubico.YubiKey.Oath.Commands;

namespace Yubico.YubiKey.Oath
{
    /// <summary>
    /// The main entry-point for all OATH related operations.
    /// </summary>
    public sealed partial class OathSession : IDisposable
    {
        private bool _disposed;
        private readonly IYubiKeyDevice _yubiKeyDevice;
        internal OathApplicationData _oathData;

        /// <summary>
        /// Indicates whether the OATH application on the YubiKey is
        /// password-protected or not, whether password verification is required
        /// before operations can be executed.
        /// </summary>
        public bool IsPasswordProtected => !_oathData.Challenge.IsEmpty;

        /// <summary>
        /// The object that represents the connection to the YubiKey.
        /// </summary>
        public IYubiKeyConnection Connection { get; private set; }

        /// <summary>
        /// The Delegate this class will call when it needs a password to unlock the OATH application.
        /// </summary>
        /// <remarks>
        /// The delegate provided will read the <c>KeyEntryData</c> which contains the information needed
        /// to determine what to collect and methods to submit what was collected. The delegate will return
        /// <c>true</c> for success or <c>false</c> for "cancel". A cancel will usually happen when the user
        /// has clicked a "Cancel" button.
        /// <p>
        /// Note that the SDK will call the <c>KeyCollector</c> with a <c>Request</c> of <c>Release</c>
        /// when the process completes. In this case, the <c>KeyCollector</c> MUST NOT throw an exception.
        /// The <c>Release</c> is called from inside a <c>finally</c> block, and it is a bad idea to throw
        /// exceptions from inside <c>finally</c>.
        /// </p>
        /// </remarks>
        public Func<KeyEntryData, bool>? KeyCollector { get; set; }

        // The default constructor explicitly defined. We don't want it to be used.
        private OathSession()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create an instance of <c>OathSession</c> class, the object that represents
        /// the OATH application on the YubiKey.
        /// </summary>
        /// <remarks>
        /// Because this class implements <c>IDisposable</c>, use the <c>using</c> keyword.
        /// For example,
        /// <code language="csharp">
        ///     IYubiKeyDevice yubiKeyToUse = SelectYubiKey();
        ///     using (var oath = new OathSession(yubiKeyToUse))
        ///     {
        ///         /* Perform OATH operations. */
        ///     }
        /// </code>
        /// </remarks>
        /// <param name="yubiKey">
        /// The object that represents the actual YubiKey which will perform the operations.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>yubiKey</c> argument is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The <c>SelectApplicationData</c> recived from the <c>yubiKey</c> is null.
        /// </exception>
        public OathSession(IYubiKeyDevice yubiKey)
        {
            if (yubiKey is null)
            {
                throw new ArgumentNullException(nameof(yubiKey));
            }

            _yubiKeyDevice = yubiKey;

            Connection = yubiKey.Connect(YubiKeyApplication.Oath);

            if (!(Connection.SelectApplicationData is OathApplicationData))
            {
                throw new InvalidOperationException(nameof(Connection.SelectApplicationData));
            }

            _oathData = (Connection.SelectApplicationData as OathApplicationData)!;

            _disposed = false;
        }

        /// <summary>
        /// Resets the YubiKey's OATH application back to a factory default state.
        /// </summary>
        /// <remarks>
        /// This will remove the password if one set and delete all credentials stored on the YubiKey.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// The <c>ResetCommand</c> failed.
        /// </exception>
        public void ResetApplication()
        {
            OathResponse resetResponse = Connection.SendCommand(new ResetCommand());

            if (resetResponse.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(resetResponse.StatusMessage);
            }

            SelectOathResponse response = Connection.SendCommand(new SelectOathCommand());
            _oathData = response.GetData();
        }

        // Checks if the KeyCollector delegate is null
        private void EnsureKeyCollector()
        {
            if (KeyCollector is null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.MissingKeyCollector));
            }
        }

        /// <summary>
        /// When the OathSession object goes out of scope, this method is called. It will close the session.
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
            KeyCollector = null;
            Connection.Dispose();
            _disposed = true;
        }
    }
}
