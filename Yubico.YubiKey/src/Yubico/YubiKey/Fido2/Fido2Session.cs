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
    /// Represents an active session to the FIDO2 application on the YubiKey.
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
    ///           /* determine which YubiKey to use */
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
    /// <c>Dispose</c> method will be called to dispose the active FIDO2 session. This will clear any application state,
    /// and ultimately release the connection to the YubiKey.
    /// </para>
    /// </remarks>
    public sealed partial class Fido2Session : IDisposable
    {
        private readonly Logger _log = Log.GetLogger();
        private bool _disposed;

        /// <summary>
        /// The object that represents the connection to the YubiKey. Most applications can ignore this, but if can be
        /// used to call command classes and send APDUs directly to the YubiKey during advanced scenarios.
        /// </summary>
        public IYubiKeyConnection Connection { get; }

        /// <summary>
        /// A callback that this class will call when it needs the YubiKey
        /// touched or a PIN to be verified.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The callback will need to read the <see cref="KeyEntryData"/> parameter which contains the information
        /// needed to determine what to collect, and methods to submit what has been collected. The callback shall
        /// return <c>true</c> for success or <c>false</c> for "cancel". A cancellation will usually happen when the
        /// user has clicked the "Cancel" button when this has been implemented in UI. That is often the case when the
        /// user has entered the wrong value a number of times, and they would like to stop trying before they exhaust
        /// their remaining retries and the YubiKey becomes blocked.
        /// </para>
        /// <para>
        /// With a FIDO2 Session, there are three situations where the SDK will call
        /// a <c>KeyCollector</c>: PIN, non-biometric touch, and biometric touch.
        /// Biometric touch is only available on YubiKeys that support this, such as
        /// the YubiKey Bio Series.
        /// </para>
        /// <para>
        /// In addition, it is possible to set the PIN without using the <c>KeyCollector</c>, see
        /// TryVerifyPin. With Touch, the <c>KeyCollector</c>
        /// will call when the YubiKey is waiting for proof of user presence.
        /// This is so that the calling app can alert the user that touch is
        /// required. There is nothing the <c>KeyCollector</c> needs to return to
        /// the SDK.
        /// </para>
        /// <para>
        /// If you do not provide a <c>KeyCollector</c> and an operation requires
        /// touch, then the SDK will simply wait for the touch without informing
        /// the caller. However, it will be much more difficult to know when
        /// touch is needed. Namely, the end user will have to know that touch is
        /// needed and look for the flashing YubiKey.
        /// </para>
        /// <para>
        /// This means that it is possible to perform FIDO2 operations without a
        /// <c>KeyCollector</c>. However, it is very useful, especially to be
        /// able to know precisely when touch is needed.
        /// </para>
        /// <para>
        /// When a touch is needed, the SDK will call the <c>KeyCollector</c>
        /// with a <c>Request</c> of <c>KeyEntryRequest.TouchRequest</c>. During
        /// registration or authentication, the YubiKey will not perform the
        /// operation until the user has touched the sensor. When that touch is
        /// needed, the SDK will call the <c>KeyCollector</c> which can then
        /// present a message (likely launch a Window) requesting the user touch
        /// the YubiKey's sensor. After the YubiKey completes the task, the SDK
        /// will call the <c>KeyCollector</c> with <c>KeyEntryRequest.Release</c>
        /// and the app can know it is time to remove the message requesting the
        /// touch.
        /// </para>
        /// <para>
        /// The SDK will call the <c>KeyCollector</c> with a <c>Request</c> of <c>Release</c> when the process
        /// completes. In this case, the <c>KeyCollector</c> MUST NOT throw an exception. The <c>Release</c> is called
        /// from inside a <c>finally</c> block, and it is best practice not to throw exceptions in this context.
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
