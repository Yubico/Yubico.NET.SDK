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
using Yubico.Core.Logging;
using Yubico.YubiKey.Fido2.Commands;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Represents an active session with the FIDO2 application on the YubiKey.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When you need to perform FIDO2 operations, instantiate this class to create a session, then call on methods
    /// within the class.
    /// </para>
    /// <para>
    /// Generally, you will choose the YubiKey to use by building an instance of <see cref="IYubiKeyDevice" />. This
    /// object will represent the actual YubiKey hardware.
    /// <code language="csharp">
    ///   IYubiKeyDevice SelectYubiKey()
    ///   {
    ///       IEnumerable&lt;IYubiKeyDevice&gt; yubiKeyList = YubiKey.FindAll();
    ///       foreach (IYubiKeyDevice current in yubiKeyList)
    ///       {
    ///           /* Determine which YubiKey to use */
    ///
    ///           if (selected)
    ///           {
    ///               return current;
    ///           }
    ///       }
    ///   }
    /// </code>
    /// </para>
    /// <para>
    /// Once you have the YubiKey to use, you will build an instance of this Fido2Session class to represent the FIDO2
    /// application on the hardware. Because this class implements <c>IDisposable</c>, use the <c>using</c> keyword.
    /// For example,
    /// <code language="csharp">
    ///     IYubiKeyDevice yubiKeyToUse = SelectYubiKey();
    ///     using (var fido2 = new Fido2Session(yubiKeyToUse))
    ///     {
    ///         /* Perform FIDO2 operations. */
    ///     }
    /// </code>
    /// </para>
    /// <para>
    /// If this class is used as part of a <c>using</c> expression or statement, when the session goes out of scope, the
    /// <c>Dispose</c> method will be called to dispose of the active FIDO2 session. This will clear any application state,
    /// and ultimately release the connection to the YubiKey.
    /// </para>
    /// </remarks>
    public sealed partial class Fido2Session : IDisposable
    {
        private readonly Logger _log = Log.GetLogger();
        private bool _disposed;

        /// <summary>
        /// The object that represents the connection to the YubiKey. Most applications can ignore this, but it can be
        /// used to call command classes and send APDUs directly to the YubiKey during advanced scenarios.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Most common FIDO2 operations can be done using the various methods contained on the <see cref="Fido2Session"/>
        /// class. There are some cases where you will need to issue a very specific command that is otherwise not
        /// available to you using the session's methods.
        /// </para>
        /// <para>
        /// This property gives you direct access to the existing connection to the YubiKey using the
        /// <see cref="IYubiKeyConnection"/> interface. To send your own commands, call the
        /// <see cref="IYubiKeyConnection.SendCommand{TResponse}"/> method like in the following example:
        /// <example lang="C#">
        /// var yubiKey = FindYubiKey();
        ///
        /// using (var fido2 = new Fido2Session(yubiKey))
        /// {
        ///     var command = new ClientPinCommand(){ /* Set properties to your needs */ };
        ///
        ///     // Sends a command to the FIDO2 application
        ///     var response = fido2.Connection.SendCommand(command);
        ///
        ///     /* Read and handle the response */
        /// }
        /// </example>
        /// </para>
        /// </remarks>
        public IYubiKeyConnection Connection { get; }

        /// <summary>
        /// A callback that this class will call when it needs the YubiKey
        /// touched or a PIN verified.
        /// </summary>
        /// <remarks>
        /// <para>
        /// With a FIDO2 Session, there are three situations where the SDK will call
        /// a <c>KeyCollector</c>: PIN, non-biometric touch, and biometric touch.
        /// Biometric touch is only available on YubiKey Bio Series keys.
        /// </para>
        /// <para>
        /// It is possible to perform PIN operations without using the <c>KeyCollector</c>. Look
        /// for the overloads of TryVerifyPin, TryChangePin, and TrySetPin that take in PIN
        /// parameters. With Touch, the <c>KeyCollector</c> will call your application
        /// when the YubiKey is waiting for proof of user presence. This is so that your
        /// application can alert the user that touch is required. There is nothing the
        /// <c>KeyCollector</c> needs to return to the SDK.
        /// </para>
        /// <para>
        /// If you do not provide a <c>KeyCollector</c> and an operation requires
        /// touch, then the SDK will simply wait for the touch without informing
        /// the caller. However, it will be much more difficult to know when
        /// touch is needed. The end user will have to know that touch is
        /// needed and look for the flashing YubiKey.
        /// </para>
        /// <para>
        /// You can read more about the KeyCollector and its implementation in its
        /// <xref href="UsersManualKeyCollector">user's manual entry</xref>.
        /// </para>
        /// </remarks>
        public Func<KeyEntryData, bool>? KeyCollector { get; set; }

        // The default constructor is explicitly defined to show that we do not want it used.
        private Fido2Session()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates an instance of <see cref="Fido2Session" />, the object that represents the FIDO2 application on the
        /// YubiKey.
        /// </summary>
        /// <remarks>
        /// Because this class implements <c>IDisposable</c>, use the <c>using</c> keyword. For example,
        /// <code language="csharp">
        ///     IYubiKeyDevice yubiKeyToUse = SelectYubiKey();
        ///     using (var fido2 = new Fido2Session(yubiKeyToUse))
        ///     {
        ///         /* Perform FIDO2 operations. */
        ///     }
        /// </code>
        /// </remarks>
        /// <param name="yubiKeyDevice">
        /// The object that represents the actual YubiKey on which the FIDO2 operations should be performed.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="yubiKeyDevice"/> argument is <c>null</c>.
        /// </exception>
        public Fido2Session(IYubiKeyDevice yubiKeyDevice)
        {
            if (yubiKeyDevice is null)
            {
                throw new ArgumentNullException(nameof(yubiKeyDevice));
            }

            _log.LogInformation(
                "Establishing a new FIDO2 session for YubiKey {SerialNumber}.",
                yubiKeyDevice.SerialNumber);

            Connection = yubiKeyDevice.Connect(YubiKeyApplication.Fido2);
        }

        /// <summary>
        /// Returns information about the authenticator (the YubiKey), including defaults and bounds for various fields
        /// and parameters used by FIDO2.
        /// </summary>
        /// <returns>
        /// An <see cref="AuthenticatorInfo"/> instance containing information provided by the YubiKey.
        /// </returns>
        public AuthenticatorInfo GetAuthenticatorInfo()
        {
            GetInfoResponse info = Connection.SendCommand(new GetInfoCommand());

            return info.GetData();
        }

        private static Fido2Status GetFido2Status(IYubiKeyResponse r) => (Fido2Status)(r.StatusWord & 0xFF);

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Connection.Dispose();
            KeyCollector = null;
            _disposed = true;
        }
    }
}
