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
using System.Security;
using Yubico.YubiKey.U2f.Commands;

namespace Yubico.YubiKey.U2f
{
    /// <summary>
    /// Represents an active session to the FIDO U2F application on the YubiKey.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When you need to perform FIDO U2F operations, instantiate this class to create a session, then call on methods
    /// within the class.
    /// </para>
    /// <para>
    /// Generally, you will choose the YubiKey to use by building an instance of <see cref="IYubiKeyDevice" />. This
    /// object will represent the actual YubiKey hardware.
    /// <code>
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
    /// Once you have the YubiKey to use, you will build an instance of this U2fSession class to represent the U2F
    /// application on the hardware. Because this class implements <c>IDisposable</c>, use the <c>using</c> keyword.
    /// For example,
    /// <code>
    ///     IYubiKeyDevice yubiKeyToUse = SelectYubiKey();
    ///     using (var u2f = new U2fSession(yubiKeyToUse))
    ///     {
    ///         /* Perform FIDO U2F operations. */
    ///     }
    /// </code>
    /// </para>
    /// <para>
    /// If this class is used as part of a <c>using</c> expression or statement, when the session goes out of scope, the
    /// <c>Dispose</c> method will be called to dispose the active U2F session. This will clear any application state,
    /// and ultimately release the connection to the YubiKey.
    /// </para>
    /// </remarks>
    public sealed class U2fSession : IDisposable
    {
        private readonly IYubiKeyDevice _yubiKeyDevice;
        private bool _disposed;

        /// <summary>
        /// The object that represents the connection to the YubiKey. Most applications can ignore this, but it can be
        /// used to call command classes and send APDUs directly to the YubiKey during advanced scenarios.
        /// </summary>
        public IYubiKeyConnection Connection { get; private set; }

        /// <summary>
        /// A callback that this class will call when it needs a PIN to be verified.
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
        /// Note that the SDK will call the <c>KeyCollector</c> with a <c>Request</c> of <c>Release</c> when the process
        /// completes. In this case, the <c>KeyCollector</c> MUST NOT thow an exception. The <c>Release</c> is called
        /// from inside a <c>finally</c> block, and it is best practice not to throw exceptions in this context.
        /// </para>
        /// </remarks>
        public Func<KeyEntryData, bool>? KeyCollector { get; set; }

        // The default constructor is explicitly defined to show that we do not want it used.
        private U2fSession()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates an instance of <see cref="U2fSession"/>, the object that represents the FIDO U2F application on the
        /// YubiKey.
        /// </summary>
        /// <remarks>
        /// Because this class implements <c>IDisposable</c>, use the <c>using</c> keyword. For example,
        /// <code>
        ///     IYubiKeyDevice yubiKeyToUse = SelectYubiKey();
        ///     using (var u2f = new U2fSession(yubiKeyToUse))
        ///     {
        ///         /* Perform U2F operations. */
        ///     }
        /// </code>
        /// </remarks>
        /// <param name="yubiKey">
        /// The object that represents the actual YubiKey on which the U2F operations should be performed.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="yubiKey"/> argument is <c>null</c>.
        /// </exception>
        public U2fSession(IYubiKeyDevice yubiKey)
        {
            if (yubiKey is null)
            {
                throw new ArgumentNullException(nameof(yubiKey));
            }

            _yubiKeyDevice = yubiKey;

            Connection = yubiKey.Connect(YubiKeyApplication.FidoU2f);
        }

        /// <summary>
        /// Registers a new U2F credential onto the authenticator (the YubiKey).
        /// </summary>
        /// <param name="applicationId">
        /// A SHA-256 hash of the UTF-8 encoding of the application or service requesting the registration. See the
        /// <xref href="FidoU2fRegistration">U2F registration overview</xref> page for more information.
        /// </param>
        /// <param name="clientDataHash">
        /// A SHA-256 hash of the client data, a stringified JSON data structure that the caller prepares. Among other
        /// things, the client data contains the challenge from the relying party (the application or service that this
        /// registration is for). See the <xref href="FidoU2fRegistration">U2F registration overview</xref> page for more
        /// information.
        /// </param>
        /// <returns>
        /// A structure containing the results of the credential registration, including the user public key, key handle,
        /// attestation certificate, and the signature.
        /// </returns>
        /// <exception cref="SecurityException">
        /// The user presence check or PIN verification failed.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The application ID is a SHA-256 hash of the UTF-8 encoding of the application or service (the relying party)
        /// requesting the registration. For a website, this is typically the https address of the primary domain, not
        /// including the final slash. For example <c>https://fido.example.com/myApp</c>. If there are multiple addresses
        /// that can be associated with this credential, the application ID should refer to a single trusted facet list.
        /// This is a JSON structure that contains a list of URI / IDs that the client should accept.
        /// </para>
        /// <para>
        /// Non-websites may use FIDO U2F. Therefor their applicationId will most likely not be a URL. Android and iOS
        /// have their own special encodings based on their application package metadata. Clients on Linux, macOS, and
        /// Windows have fewer guidelines and can usually be defined by the application. See the
        /// <xref href="FidoU2fRegistration">U2F registration overview</xref> page for more information.
        /// </para>
        /// <para>
        /// The client data hash is a SHA-256 hash of the UTF-8 encoding of a JSON data structure that the app calling
        /// this API (the client) must prepare. It consists of:
        /// a <c>typ</c> field that must be set to <c>navigator.id.finishEnrollment</c>,
        /// a <c>challenge</c> field that is a websafe-base64-encoded challenge provided by the relying party,
        /// an <c>origin</c> field that is the exact facet id (the website or identifier) used by the relying party,
        /// an optional <c>cid_pubkey</c> field that represents the channel ID public key used by this app to communicate
        /// with the above origin.
        /// </para>
        /// <para>
        /// The results of the registration should be sent back to the relying party service for verification. This
        /// includes the P-256 NIST elliptic curve public key that represents this credential, a key handle that acts
        /// as an identifier for the generated key pair, an attestation certificate of the authenticator (the YubiKey),
        /// and a signature of the challenge (and other data).
        /// </para>
        /// <para>
        /// The signature must be verified by the relying party using the public key certified in the attestation
        /// statement. The relying party should also validate that the attestation certificate chains up to a trusted
        /// certificate authority. Once the relying party verifies the signature, it should store the public key and
        /// key handle for future authentication operations.
        /// </para>
        /// <para>
        /// The YubiKey extends the FIDO U2F spec with its YubiKey 4 FIPS series of devices with the addition of a PIN.
        /// This PIN is required to modify the FIDO U2F application, including the registration of new U2F credentials.
        /// Applications that intend to interoperate with YubiKey FIPS devices should implement a <see cref="KeyCollector"/>
        /// so that a PIN can be collected and verified when required.
        /// </para>
        /// </remarks>
        public RegistrationData Register(ReadOnlyMemory<byte> applicationId, ReadOnlyMemory<byte> clientDataHash)
        {
            if (!TryRegister(applicationId, clientDataHash, out RegistrationData? registrationData))
            {
                throw new SecurityException("User presence or authentication failed.");
            }

            return registrationData;
        }

        /// <summary>
        /// Attempts to register a new U2F credential onto the authenticator (the YubiKey).
        /// </summary>
        /// <param name="applicationId">
        /// A SHA-256 hash of the UTF-8 encoding of the application or service requesting the registration. See the
        /// <xref href="FidoU2fRegistration">U2F registration overview</xref> page for more information.
        /// </param>
        /// <param name="clientDataHash">
        /// A SHA-256 hash of the client data, a stringified JSON data structure that the caller prepares. Among other
        /// things, the client data contains the challenge from the relying party (the application or service that this
        /// registration is for). See the <xref href="FidoU2fRegistration">U2F registration overview</xref> page for more
        /// information.
        /// </param>
        /// <param name="registrationData">
        /// A structure containing the results of the credential registration, including the user public key, key handle,
        /// attestation certificate, and the signature.
        /// </param>
        /// <returns>
        /// <c>true</c> when the credential was successfully registered, <c>false</c> when user presence or the PIN could
        /// not be verified, and an exception in all other cases.
        /// </returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>
        /// <para>
        /// The application ID is a SHA-256 hash of the UTF-8 encoding of the application or service (the relying party)
        /// requesting the registration. For a website, this is typically the https address of the primary domain, not
        /// including the final slash. For example <c>https://fido.example.com/myApp</c>. If there are multiple addresses
        /// that can be associated with this credential, the application ID should refer to a single trusted facet list.
        /// This is a JSON structure that contains a list of URI / IDs that the client should accept.
        /// </para>
        /// <para>
        /// Non-websites may use FIDO U2F. Therefor their applicationId will most likely not be a URL. Android and iOS
        /// have their own special encodings based on their application package metadata. Clients on Linux, macOS, and
        /// Windows have fewer guidelines and can usually be defined by the application. See the
        /// <xref href="FidoU2fRegistration">U2F registration overview</xref> page for more information.
        /// </para>
        /// <para>
        /// The client data hash is a SHA-256 hash of the UTF-8 encoding of a JSON data structure that the app calling
        /// this API (the client) must prepare. It consists of:
        /// a <c>typ</c> field that must be set to <c>navigator.id.finishEnrollment</c>,
        /// a <c>challenge</c> field that is a websafe-base64-encoded challenge provided by the relying party,
        /// an <c>origin</c> field that is the exact facet id (the website or identifier) used by the relying party,
        /// an optional <c>cid_pubkey</c> field that represents the channel ID public key used by this app to communicate
        /// with the above origin.
        /// </para>
        /// <para>
        /// The results of the registration should be sent back to the relying party service for verification. This
        /// includes the P-256 NIST elliptic curve public key that represents this credential, a key handle that acts
        /// as an identifier for the generated key pair, an attestation certificate of the authenticator (the YubiKey),
        /// and a signature of the challenge (and other data).
        /// </para>
        /// <para>
        /// The signature must be verified by the relying party using the public key certified in the attestation
        /// statement. The relying party should also validate that the attestation certificate chains up to a trusted
        /// certificate authority. Once the relying party verifies the signature, it should store the public key and
        /// key handle for future authentication operations.
        /// </para>
        /// <para>
        /// The YubiKey extends the FIDO U2F spec with its YubiKey 4 FIPS series of devices with the addition of a PIN.
        /// This PIN is required to modify the FIDO U2F application, including the registration of new U2F credentials.
        /// Applications that intend to interoperate with YubiKey FIPS devices should implement a <see cref="KeyCollector"/>
        /// so that a PIN can be collected and verified when required.
        /// </para>
        /// </remarks>
        public bool TryRegister(
            ReadOnlyMemory<byte> applicationId,
            ReadOnlyMemory<byte> clientDataHash,
            [MaybeNullWhen(returnValue: false)] out RegistrationData registrationData)
        {
            var command = new RegisterCommand(clientDataHash, applicationId);

            if (_yubiKeyDevice.IsFipsSeries)
            {
                // TODO: Extra FIPS handling?
            }

            RegisterResponse response = Connection.SendCommand(command);

            if (response.Status == ResponseStatus.AuthenticationRequired)
            {
                registrationData = null;
                return false;
            }
            else if (response.Status != ResponseStatus.Success)
            {
                // TODO: throw
                throw new InvalidOperationException();
            }

            registrationData = response.GetData();

            return true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            KeyCollector = null;
            Connection.Dispose();
            _disposed = true;
        }

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
    }
}
