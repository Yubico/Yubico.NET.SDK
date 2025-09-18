// Copyright 2025 Yubico AB
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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Yubico.Core.Iso7816;
using Yubico.Core.Logging;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.U2f.Commands;

namespace Yubico.YubiKey.U2f;

/// <summary>
///     Represents an active session to the FIDO U2F application on the YubiKey.
/// </summary>
/// <remarks>
///     <para>
///         When you need to perform FIDO U2F operations, instantiate this class to create a session, then call on methods
///         within the class.
///     </para>
///     <para>
///         Generally, you will choose the YubiKey to use by building an instance of <see cref="IYubiKeyDevice" />. This
///         object will represent the actual YubiKey hardware.
///         <code language="csharp">
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
///     </para>
///     <para>
///         Once you have the YubiKey to use, you will build an instance of this U2fSession class to represent the U2F
///         application on the hardware. Because this class implements <c>IDisposable</c>, use the <c>using</c> keyword.
///         For example,
///         <code language="csharp">
///     IYubiKeyDevice yubiKeyToUse = SelectYubiKey();
///     using (var u2f = new U2fSession(yubiKeyToUse))
///     {
///         /* Perform FIDO U2F operations. */
///     }
/// </code>
///     </para>
///     <para>
///         If this class is used as part of a <c>using</c> expression or statement, when the session goes out of scope,
///         the
///         <c>Dispose</c> method will be called to dispose the active U2F session. This will clear any application state,
///         and ultimately release the connection to the YubiKey.
///     </para>
/// </remarks>
public sealed partial class U2fSession : IDisposable
{
    private const double MaxTimeoutSeconds = 30.0;

    private readonly ILogger _log = Log.GetLogger<U2fSession>();
    private bool _disposed;

    // The default constructor is explicitly defined to show that we do not want it used.
    // ReSharper disable once UnusedMember.Local
    private U2fSession()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Creates an instance of <see cref="U2fSession" />, the object that represents the FIDO U2F application on the
    ///     YubiKey.
    /// </summary>
    /// <remarks>
    ///     Because this class implements <c>IDisposable</c>, use the <c>using</c> keyword. For example,
    ///     <code language="csharp">
    ///     IYubiKeyDevice yubiKeyToUse = SelectYubiKey();
    ///     using (var u2f = new U2fSession(yubiKeyToUse))
    ///     {
    ///         /* Perform U2F operations. */
    ///     }
    /// </code>
    /// </remarks>
    /// <param name="yubiKey">
    ///     The object that represents the actual YubiKey on which the U2F operations should be performed.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     The <paramref name="yubiKey" /> argument is <c>null</c>.
    /// </exception>
    public U2fSession(IYubiKeyDevice yubiKey)
    {
        _log.LogInformation("Create a new instance of U2fSession.");

        if (yubiKey is null)
        {
            throw new ArgumentNullException(nameof(yubiKey));
        }

        Connection = yubiKey.Connect(YubiKeyApplication.FidoU2f);
    }

    /// <summary>
    ///     The object that represents the connection to the YubiKey. Most applications can ignore this, but it can be
    ///     used to call command classes and send APDUs directly to the YubiKey during advanced scenarios.
    /// </summary>
    public IYubiKeyConnection Connection { get; }

    /// <summary>
    ///     A callback that this class will call when it needs the YubiKey
    ///     touched or a PIN to be verified.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The callback will need to read the <see cref="KeyEntryData" /> parameter which contains the information
    ///         needed to determine what to collect, and methods to submit what has been collected. The callback shall
    ///         return <c>true</c> for success or <c>false</c> for "cancel". A cancellation will usually happen when the
    ///         user has clicked the "Cancel" button when this has been implemented in UI. That is often the case when the
    ///         user has entered the wrong value a number of times, and they would like to stop trying before they exhaust
    ///         their remaining retries and the YubiKey becomes blocked.
    ///     </para>
    ///     <para>
    ///         With a U2F Session, there are two situations where the SDK will call
    ///         a <c>KeyCollector</c>: PIN and Touch. A PIN is needed only with a
    ///         version 4 FIPS series YubiKey, and only if it is in FIPS mode. See
    ///         the user's manual entry on
    ///         <xref href="FidoU2fFipsMode"> FIDO U2F FIPS mode</xref> for more
    ///         information on this topic. In addition, it is possible to set the
    ///         PIN without using the <c>KeyCollector</c>, see
    ///         <see cref="TryVerifyPin()" />. With Touch, the <c>KeyCollector</c>
    ///         will call when the YubiKey is waiting for proof of user presence.
    ///         This is so that the calling app can alert the user that touch is
    ///         required. There is nothing the <c>KeyCollector</c> needs to return to
    ///         the SDK.
    ///     </para>
    ///     <para>
    ///         If your app is calling a version 4 FIPS YubiKey, it is possible to
    ///         directly verify the PIN at the beginning of a session. In that case,
    ///         a <c>KeyCollector</c> is not necessary. However, if you do not call
    ///         this direct PIN verification method, and a PIN is needed later on,
    ///         the SDK will throw an exception.
    ///     </para>
    ///     <para>
    ///         If you do not provide a <c>KeyCollector</c> and an operation requires
    ///         touch, then the SDK will simply wait for the touch without informing
    ///         the caller. However, it will be much more difficult to know when
    ///         touch is needed. Namely, the end user will have to know that touch is
    ///         needed and look for the flashing YubiKey.
    ///     </para>
    ///     <para>
    ///         This means that it is possible to perform U2F operations without a
    ///         <c>KeyCollector</c>. However, it is very useful, especially to be
    ///         able to know precisely when touch is needed.
    ///     </para>
    ///     <para>
    ///         When a touch is needed, the SDK will call the <c>KeyCollector</c>
    ///         with a <c>Request</c> of <c>KeyEntryRequest.TouchRequest</c>. During
    ///         registration or authentication, the YubiKey will not perform the
    ///         operation until the user has touched the sensor. When that touch is
    ///         needed, the SDK will call the <c>KeyCollector</c> which can then
    ///         present a message (likely launch a Window) requesting the user touch
    ///         the YubiKey's sensor. After the YubiKey completes the task, the SDK
    ///         will call the <c>KeyCollector</c> with <c>KeyEntryRequest.Release</c>
    ///         and the app can know it is time to remove the message requesting the
    ///         touch.
    ///     </para>
    ///     <para>
    ///         The SDK will call the <c>KeyCollector</c> with a <c>Request</c> of <c>Release</c> when the process
    ///         completes. In this case, the <c>KeyCollector</c> MUST NOT throw an exception. The <c>Release</c> is called
    ///         from inside a <c>finally</c> block, and it is best practice not to throw exceptions in this context.
    ///     </para>
    /// </remarks>
    public Func<KeyEntryData, bool>? KeyCollector { get; set; }

    #region IDisposable Members

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

    #endregion

    /// <summary>
    ///     Registers a new U2F credential onto the authenticator (the YubiKey).
    /// </summary>
    /// <param name="applicationId">
    ///     Also known as the origin data. A SHA-256 hash of the UTF-8 encoding of the
    ///     application or service requesting the registration. See the
    ///     <xref href="FidoU2fRegistration">U2F registration overview</xref>
    ///     page for more information.
    /// </param>
    /// <param name="clientDataHash">
    ///     A SHA-256 hash of the client data, a stringified JSON data structure that the caller prepares. Among other
    ///     things, the client data contains the challenge from the relying party (the application or service that this
    ///     registration is for). See the <xref href="FidoU2fRegistration">U2F registration overview</xref> page for more
    ///     information.
    /// </param>
    /// <param name="timeout">
    ///     The amount of time this method will wait for user touch. The
    ///     recommended timeout is 5 seconds. The minimum is 1 second and the
    ///     maximum is 30 seconds. If the input is greater than 30 seconds, this
    ///     method will set the timeout to 30. If the timeout is greater than 0
    ///     but less than one second, the method will set the timeout to 1
    ///     second. If the timeout is zero, this method will set no timeout and
    ///     wait for touch indefinitely (zero timeout means no timeout).
    /// </param>
    /// <returns>
    ///     A structure containing the results of the credential registration, including the user public key, key handle,
    ///     attestation certificate, and the signature.
    /// </returns>
    /// <exception cref="TimeoutException">
    ///     The user presence check timed out.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     The input data was invalid (e.g. the appId was not 32 bytes) or else
    ///     the YubiKey was version 4 FIPS in FIPS mode, and needed the PIN
    ///     verified. However, it was not verified and there was no
    ///     <c>KeyCollector</c>.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    ///     The YubiKey was version 4 FIPS in FIPS mode, and needed the PIN
    ///     verified. However, it was not verified and the user canceled PIN
    ///     collection.
    /// </exception>
    /// <exception cref="SecurityException">
    ///     The YubiKey was version 4 FIPS in FIPS mode, and needed the PIN
    ///     verified. However, it was not verified and the PIN was blocked (no
    ///     more retries remaining).
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         The application ID is a SHA-256 hash of the UTF-8 encoding of the application or service (the relying party)
    ///         requesting the registration. For a website, this is typically the https address of the primary domain, not
    ///         including the final slash. For example <c>https://fido.example.com/myApp</c>. If there are multiple addresses
    ///         that can be associated with this credential, the application ID should refer to a single trusted facet list.
    ///         This is a JSON structure that contains a list of URI / IDs that the client should accept.
    ///     </para>
    ///     <para>
    ///         Non-websites may use FIDO U2F. Therefore their applicationId (origin
    ///         data) will most likely not be a URL. Android and iOS
    ///         have their own special encodings based on their application package metadata. Clients on Linux, macOS, and
    ///         Windows have fewer guidelines and can usually be defined by the application. See the
    ///         <xref href="FidoU2fRegistration">U2F registration overview</xref> page for more information.
    ///     </para>
    ///     <para>
    ///         The client data hash is a SHA-256 hash of the UTF-8 encoding of a JSON data structure that the app calling
    ///         this API (the client) must prepare. It consists of:
    ///         a <c>typ</c> field that must be set to <c>navigator.id.finishEnrollment</c>,
    ///         a <c>challenge</c> field that is a websafe-base64-encoded challenge provided by the relying party,
    ///         an <c>origin</c> field that is the exact facet id (the website or identifier) used by the relying party,
    ///         an optional <c>cid_pubkey</c> field that represents the channel ID public key used by this app to communicate
    ///         with the above origin.
    ///     </para>
    ///     <para>
    ///         The results of the registration should be sent back to the relying party service for verification. This
    ///         includes the P-256 NIST elliptic curve public key that represents this credential, a key handle that acts
    ///         as an identifier for the generated key pair, an attestation certificate of the authenticator (the YubiKey),
    ///         and a signature of the challenge (and other data).
    ///     </para>
    ///     <para>
    ///         The signature must be verified by the relying party using the public key certified in the attestation
    ///         statement. The relying party should also validate that the attestation certificate chains up to a trusted
    ///         certificate authority. Once the relying party verifies the signature, it should store the public key and
    ///         key handle for future authentication operations.
    ///     </para>
    ///     <para>
    ///         The YubiKey will not compute a signature (complete the registration)
    ///         without proof of user presence. That means the user must touch the
    ///         YubiKey's sensor. When this method gets to the point that the touch
    ///         is needed before it can continue, it will call the
    ///         <see cref="KeyCollector" /> with the <c>Request></c> of
    ///         <c>TouchRequest</c>. At that point, the calling app can display a
    ///         message (e.g. launch a window) indicating the user needs to touch the
    ///         YubiKey to complete the operation. When the Registration is complete
    ///         (or upon an error or timeout), the SDK will call the
    ///         <c>KeyCollector</c> with the <c>Request</c> of <c>Release</c>,
    ///         meaning the calling app now knows it can take away the touch request
    ///         message.
    ///     </para>
    ///     <para>
    ///         The YubiKey extends the FIDO U2F spec with its YubiKey 4 FIPS series of devices with the addition of a PIN.
    ///         This PIN is required to modify the FIDO U2F application, including the registration of new U2F credentials.
    ///         Applications that intend to interoperate with YubiKey FIPS devices should implement a
    ///         <see cref="KeyCollector" />
    ///         so that a PIN can be collected and verified when required. If no
    ///         <c>KeyCollector</c> is provided, then the calling app should call the
    ///         method that directly verifies the PIN.
    ///     </para>
    /// </remarks>
    public RegistrationData Register(
        ReadOnlyMemory<byte> applicationId,
        ReadOnlyMemory<byte> clientDataHash,
        TimeSpan timeout)
    {
        _log.LogInformation("Register a new U2F credential.");
        var response = CommonRegister(applicationId, clientDataHash, timeout, true);

        // If everything worked, this will return the correct result. If
        // there was an error, this will throw an exception.
        return response.GetData();
    }

    /// <summary>
    ///     Attempts to register a new U2F credential onto the authenticator (the
    ///     YubiKey). This will return <c>false</c> if the user cancels PIN collection
    ///     (FIPS series 4 YubiKey in FIPS mode only) or if there is some other
    ///     error, such as bad application ID data.
    /// </summary>
    /// <param name="applicationId">
    ///     Also known as the origin data. A SHA-256 hash of the UTF-8 encoding of the
    ///     application or service requesting the registration. See the
    ///     <xref href="FidoU2fRegistration">U2F registration overview</xref>
    ///     page for more information.
    /// </param>
    /// <param name="clientDataHash">
    ///     A SHA-256 hash of the client data, a stringified JSON data structure that the caller prepares. Among other
    ///     things, the client data contains the challenge from the relying party (the application or service that this
    ///     registration is for). See the <xref href="FidoU2fRegistration">U2F registration overview</xref> page for more
    ///     information.
    /// </param>
    /// <param name="timeout">
    ///     The amount of time this method will wait for user touch. The
    ///     recommended timeout is 5 seconds. The minimum is 1 second and the
    ///     maximum is 30 seconds. If the input is greater than 30 seconds, this
    ///     method will set the timeout to 30. If the timeout is greater than 0
    ///     but less than one second, the method will set the timeout to 1
    ///     second. If the timeout is zero, this method will set no timeout and
    ///     wait for touch indefinitely (zero timeout means no timeout).
    /// </param>
    /// <param name="registrationData">
    ///     A structure containing the results of the credential registration, including the user public key, key handle,
    ///     attestation certificate, and the signature.
    /// </param>
    /// <returns>
    ///     <c>true</c> when the credential was successfully registered,
    ///     <c>false</c> when the input data was not correct or the user canceled
    ///     PIN collection.
    /// </returns>
    /// <exception cref="TimeoutException">
    ///     The user presence check timed out.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     The YubiKey was version 4 FIPS in FIPS mode, and needed the PIN
    ///     verified. However, it was not verified and there was no
    ///     <c>KeyCollector</c>.
    /// </exception>
    /// <exception cref="SecurityException">
    ///     The YubiKey was version 4 FIPS in FIPS mode, and needed the PIN
    ///     verified. However, it was not verified and the PIN was blocked (no
    ///     more retries remaining).
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         The application ID is a SHA-256 hash of the UTF-8 encoding of the application or service (the relying party)
    ///         requesting the registration. For a website, this is typically the https address of the primary domain, not
    ///         including the final slash. For example <c>https://fido.example.com/myApp</c>. If there are multiple addresses
    ///         that can be associated with this credential, the application ID should refer to a single trusted facet list.
    ///         This is a JSON structure that contains a list of URI / IDs that the client should accept.
    ///     </para>
    ///     <para>
    ///         Non-websites may use FIDO U2F. Therefore their applicationId (origin
    ///         data) will most likely not be a URL. Android and iOS
    ///         have their own special encodings based on their application package metadata. Clients on Linux, macOS, and
    ///         Windows have fewer guidelines and can usually be defined by the application. See the
    ///         <xref href="FidoU2fRegistration">U2F registration overview</xref> page for more information.
    ///     </para>
    ///     <para>
    ///         The client data hash is a SHA-256 hash of the UTF-8 encoding of a JSON data structure that the app calling
    ///         this API (the client) must prepare. It consists of:
    ///         a <c>typ</c> field that must be set to <c>navigator.id.finishEnrollment</c>,
    ///         a <c>challenge</c> field that is a websafe-base64-encoded challenge provided by the relying party,
    ///         an <c>origin</c> field that is the exact facet id (the website or identifier) used by the relying party,
    ///         an optional <c>cid_pubkey</c> field that represents the channel ID public key used by this app to communicate
    ///         with the above origin.
    ///     </para>
    ///     <para>
    ///         The results of the registration should be sent back to the relying party service for verification. This
    ///         includes the P-256 NIST elliptic curve public key that represents this credential, a key handle that acts
    ///         as an identifier for the generated key pair, an attestation certificate of the authenticator (the YubiKey),
    ///         and a signature of the challenge (and other data).
    ///     </para>
    ///     <para>
    ///         The signature must be verified by the relying party using the public key certified in the attestation
    ///         statement. The relying party should also validate that the attestation certificate chains up to a trusted
    ///         certificate authority. Once the relying party verifies the signature, it should store the public key and
    ///         key handle for future authentication operations.
    ///     </para>
    ///     <para>
    ///         The YubiKey extends the FIDO U2F spec with its YubiKey 4 FIPS series of devices with the addition of a PIN.
    ///         This PIN is required to modify the FIDO U2F application, including the registration of new U2F credentials.
    ///         Applications that intend to interoperate with YubiKey FIPS devices should implement a
    ///         <see cref="KeyCollector" />
    ///         so that a PIN can be collected and verified when required. The
    ///         alternative is to call the <see cref="TryVerifyPin()" />
    ///         method at the beginning of the session. In that case, no
    ///         <c>KeyCollector</c> will be necessary.
    ///     </para>
    /// </remarks>
    public bool TryRegister(
        ReadOnlyMemory<byte> applicationId,
        ReadOnlyMemory<byte> clientDataHash,
        TimeSpan timeout,
        [MaybeNullWhen(returnValue: false)] out RegistrationData registrationData)
    {
        _log.LogInformation("Try to register a new U2F credential.");
        var response = CommonRegister(applicationId, clientDataHash, timeout, false);

        if (response.Status == ResponseStatus.Success)
        {
            registrationData = response.GetData();

            return true;
        }

        registrationData = null;

        return false;
    }

    // This code actually performs the Register. If throwOnCancel is true and
    // if the PIN is needed and the user cancels, it will throw an exception.
    // This will return the RegisterResponse. The caller can check the
    // Status and if Success, get the RegistrationData. If not, either return
    // false or call GetData to force an exception.
    private RegisterResponse CommonRegister(
        ReadOnlyMemory<byte> applicationId,
        ReadOnlyMemory<byte> clientDataHash,
        TimeSpan timeout,
        bool throwOnCancel)
    {
        Task? touchMessageTask = null;
        var keyEntryData = new KeyEntryData();

        var timeoutToUseTimeSpan = GetTimeoutToUse(timeout);

        var command = new RegisterCommand(applicationId, clientDataHash);
        var response = Connection.SendCommand(command);

        // This should only apply to FIPS series devices.
        // This response happens if the PIN is not verified.
        // We know this is PIN, rather than touch because when touch is
        // required, the Status will be ConditionsNotSatisfied.
        if (response.Status == ResponseStatus.AuthenticationRequired)
        {
            if (!CommonVerifyPin(throwOnCancel))
            {
                return response;
            }

            response = Connection.SendCommand(command);
        }

        // If the response is ConditionsNotSatisfied, we need touch.
        if (response.Status == ResponseStatus.ConditionsNotSatisfied)
        {
            // On a separate thread, call the KeyCollector to announce we
            // need touch.
            if (KeyCollector is not null)
            {
                keyEntryData.Request = KeyEntryRequest.TouchRequest;
                touchMessageTask = Task.Run(() => _ = KeyCollector(keyEntryData));
            }

            var timer = new Stopwatch();

            try
            {
                timer.Start();

                do
                {
                    Thread.Sleep(100);
                    response = Connection.SendCommand(command);
                }
                while (response.Status == ResponseStatus.ConditionsNotSatisfied &&
                       timer.Elapsed < timeoutToUseTimeSpan);

                // Did we break out because of timeout or because the
                // response was something other than ConditionsNotSatisfied.
                // If the response.Status is still ConditionsNotSatisfied,
                // then it timed out.
                if (response.Status == ResponseStatus.ConditionsNotSatisfied)
                {
                    throw new TimeoutException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.UserInteractionTimeout));
                }
            }
            finally
            {
                timer.Stop();
                touchMessageTask?.Wait();

                if (KeyCollector is not null)
                {
                    keyEntryData.Request = KeyEntryRequest.Release;
                    _ = KeyCollector(keyEntryData);
                }
            }
        }

        return response;
    }

    /// <summary>
    ///     Verify that the given <c>keyHandle</c> is a YubiKey handle and
    ///     matches the <c>applicationId</c> and <c>clientDataHash</c>.
    /// </summary>
    /// <remarks>
    ///     When performing an authentication, the relying party sends the key
    ///     handle that the YubiKey should use to sign the challenge. The client
    ///     passes that key handle along to the YubiKey, along with the
    ///     applicationID (origin data) and client data hash. The YubiKey will
    ///     determine if the key handle is valid and if so, if it matches the
    ///     origin data. If it does, the YubiKey will sign the challenge and if
    ///     not, the YubiKey will simply not create a signature. This is the
    ///     YubiKey verifying the relying party.
    ///     <para>
    ///         Call this method to check the key handle before trying to execute a
    ///         full authentication operation. This operation is specified by the U2F
    ///         standard.
    ///     </para>
    ///     <para>
    ///         Note that there are three primary ways this method will return
    ///         <c>false</c>. One, the key handle does not belong to the YubiKey, two,
    ///         the key handle does not match the applicationID (origin data), and
    ///         three, the data is invalid (e.g. a 16-byte client data hash).
    ///     </para>
    /// </remarks>
    /// <param name="applicationId">
    ///     Also known as the origin data. A SHA-256 hash of the UTF-8 encoding of the
    ///     application or service requesting the registration. See the
    ///     <xref href="FidoU2fRegistration">U2F registration overview</xref>
    ///     page for more information.
    /// </param>
    /// <param name="clientDataHash">
    ///     A SHA-256 hash of the client data, a stringified JSON data structure
    ///     that the caller prepares. Among other things, the client data
    ///     contains the challenge from the relying party (the application or
    ///     service that this verification is for). See the
    ///     <xref href="FidoU2fRegistration">U2F registration overview</xref>
    ///     page for more information.
    /// </param>
    /// <param name="keyHandle">
    ///     The key handle the provided by the relying party.
    /// </param>
    /// <returns>
    ///     A boolean, <c>true</c> if the key handle matches, <c>false</c>
    ///     otherwise.
    /// </returns>
    public bool VerifyKeyHandle(
        ReadOnlyMemory<byte> applicationId,
        ReadOnlyMemory<byte> clientDataHash,
        ReadOnlyMemory<byte> keyHandle)
    {
        _log.LogInformation("Verify a U2F key handle.");

        var command = new AuthenticateCommand(
            U2fAuthenticationType.CheckOnly, applicationId, clientDataHash,
            keyHandle);

        var response = Connection.SendCommand(command);

        // The standard specifies that if the key handle matches, the token
        // must respond with the test-of-user-presence error. If the key
        // handle does not match, the token must respond with the
        // bad-key-handle error.
        return response.StatusWord == SWConstants.ConditionsNotSatisfied;
    }

    /// <summary>
    ///     Authenticates a credential. Throw an exception if the method is not
    ///     able to perform the operation.
    /// </summary>
    /// <remarks>
    ///     The client gets the key handle along with the challenge from the
    ///     relying party, and computes the application ID (origin data) and the
    ///     client data hash using the challenge. The client then sends the
    ///     relevant data to the YubiKey. If the YubiKey can build a private key
    ///     from the key handle, it will sign the appId and client data hash.
    ///     This is how the YubiKey authenticates to the relying party.
    ///     <para>
    ///         The YubiKey will also be able to use the key handle to verify it is
    ///         talking to a relying party with which it is registered. If the key
    ///         handle can be "converted" into an actual key matching the origin
    ///         data, then the YubiKey will compute a signature. If not, the YubiKey
    ///         will reject the key handle and this method will throw an exception
    ///         (with the message "The request was rejected due to an invalid key
    ///         handle.").
    ///     </para>
    ///     <para>
    ///         The application ID is a SHA-256 hash of the UTF-8 encoding of the
    ///         application or service (the relying party) requesting the
    ///         registration. This is also known as the origin data. For a website,
    ///         this is typically the https address of the primary domain, not
    ///         including the final slash. For example,
    ///         <c>https://fido.example.com/myApp</c>. If there are multiple
    ///         addresses than can be associated with this credential, the
    ///         application ID should refer to a single trusted facet list. This is a
    ///         JSON structure that contains a list of URI / IDs that the client
    ///         should accept.
    ///     </para>
    ///     <para>
    ///         Non-websites may use FIDO U2F. Therefore their applicationId will
    ///         most likely not be a URL. Android and iOS have their own special
    ///         encodings based on their application package metadata. Clients on
    ///         Linux, macOS, and Windows have fewer guidelines and can usually be
    ///         defined by the application. See the
    ///         <xref href="HowFidoU2fWorks">How FIDO U2f works</xref> page in the
    ///         user's manual for more information.
    ///     </para>
    ///     <para>
    ///         The U2F standard specifies that a client can call on the token to
    ///         authenticate and require proof of user presence or not. That is, the
    ///         client can ask the token to authenticate directly with no further
    ///         user interaction. This argument is <c>true</c> by default, meaning
    ///         proof of presence is required. With the YubiKey, proof of user
    ///         presence is touch. Note that if this argument is <c>false</c>, then
    ///         this method will ignore the <c>timeout</c> argument.
    ///     </para>
    ///     <para>
    ///         A version 4 FIPS YubiKey can have a PIN set on the U2F application.
    ///         That PIN applies to registration only. A PIN is never needed to
    ///         perform authentication.
    ///     </para>
    /// </remarks>
    /// <param name="applicationId">
    ///     Also known as the origin data. A SHA-256 hash of the UTF-8 encoding of the
    ///     application or service requesting the authentication. See the user's
    ///     manual article on
    ///     <xref href="HowFidoU2fWorks">How Fido U2F works</xref> for more
    ///     information.
    /// </param>
    /// <param name="clientDataHash">
    ///     A SHA-256 hash of the client data, a stringified JSON data structure
    ///     that the caller prepares. Among other things, the client data
    ///     contains the challenge from the relying party (the application or
    ///     service that this registration is for).  See the user's manual
    ///     article on <xref href="HowFidoU2fWorks">How Fido U2F works</xref> for
    ///     more information.
    /// </param>
    /// <param name="keyHandle">
    ///     The key handle the YubiKey returned during registration. That value
    ///     was sent to the relying party and now is being returned to the
    ///     YubiKey (via the client).
    /// </param>
    /// <param name="timeout">
    ///     The amount of time this method will wait for user touch. The
    ///     recommended timeout is 5 seconds. The minimum is 1 second and the
    ///     maximum is 30 seconds. If the input is greater than 30 seconds, this
    ///     method will set the timeout to 30. If the timeout is greater than 0
    ///     but less than one second, the method will set the timeout to 1
    ///     second. If the timeout is zero, this method will set no timeout and
    ///     wait for touch indefinitely (zero timeout means no timeout).
    /// </param>
    /// <param name="requireProofOfPresence">
    ///     If <c>true</c>, then the user must provide proof of presence in order
    ///     to complete the authentication. If <c>false</c>, proof of presence is
    ///     not necessary. The default is <c>true</c> so if no value is given for
    ///     this argument, it will be <c>true</c>. With the YubiKey proof of user
    ///     presence is touch.
    /// </param>
    /// <returns>
    ///     A structure containing the results of the credential authentication,
    ///     including the signature.
    /// </returns>
    /// <exception cref="TimeoutException">
    ///     The user presence check timed out.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     The input data was invalid (e.g. the appId was not 32 bytes) or else
    ///     the key handle was not correct for the appId.
    /// </exception>
    public AuthenticationData Authenticate(
        ReadOnlyMemory<byte> applicationId,
        ReadOnlyMemory<byte> clientDataHash,
        ReadOnlyMemory<byte> keyHandle,
        TimeSpan timeout,
        bool requireProofOfPresence = true)
    {
        _log.LogInformation("Authenticate a U2F credential.");

        var response = CommonAuthenticate(applicationId, clientDataHash, keyHandle, timeout, requireProofOfPresence);

        // If everything worked, this will return the correct result. If
        // there was an error, this will throw an exception.
        return response.GetData();
    }

    /// <summary>
    ///     Try to authenticate a credential. If this method can't authenticate
    ///     the input data or compute the signature, return <c>false</c>. Any other
    ///     error will throw an exception.
    /// </summary>
    /// <remarks>
    ///     See the comments for <see cref="Authenticate" /> as they apply to this
    ///     method as well.
    /// </remarks>
    /// <param name="applicationId">
    ///     Also known as the origin data. A SHA-256 hash of the UTF-8 encoding of the
    ///     application or service requesting the authentication. See the user's
    ///     manual article on
    ///     <xref href="HowFidoU2fWorks">How Fido U2F works</xref> for more
    ///     information.
    /// </param>
    /// <param name="clientDataHash">
    ///     A SHA-256 hash of the client data, a stringified JSON data structure
    ///     that the caller prepares. Among other things, the client data
    ///     contains the challenge from the relying party (the application or
    ///     service that this registration is for).  See the user's manual
    ///     article on <xref href="HowFidoU2fWorks">How Fido U2F works</xref> for
    ///     more information.
    /// </param>
    /// <param name="keyHandle">
    ///     The key handle the YubiKey returned during registration. That value
    ///     was sent to the relying party and now is being returned to the
    ///     YubiKey (via the client).
    /// </param>
    /// <param name="timeout">
    ///     The amount of time this method will wait for user touch. The
    ///     recommended timeout is 5 seconds. The minimum is 1 second and the
    ///     maximum is 30 seconds. If the input is greater than 30 seconds, this
    ///     method will set the timeout to 30. If the timeout is greater than 0
    ///     but less than one second, the method will set the timeout to 1
    ///     second. If the timeout is zero, this method will set no timeout and
    ///     wait for touch indefinitely (zero timeout means no timeout).
    /// </param>
    /// <param name="requireProofOfPresence">
    ///     If <c>true</c>, then the user must provide proof of presence in order
    ///     to complete the authentication. If <c>false</c>, proof of presence is
    ///     not necessary. The default is <c>true</c> so if no value is given for
    ///     this argument, it will be <c>true</c>. With the YubiKey proof of user
    ///     presence is touch.
    /// </param>
    /// <param name="authenticationData">
    ///     A structure containing the results of the credential authentication,
    ///     including the signature.
    /// </param>
    /// <returns>
    ///     <c>true</c> when the credential was successfully authenticated,
    ///     <c>false</c> when the input data could not be used, such as a key
    ///     handle that did not match the appId.
    /// </returns>
    /// <exception cref="TimeoutException">
    ///     The user presence check timed out.
    /// </exception>
    public bool TryAuthenticate(
        ReadOnlyMemory<byte> applicationId,
        ReadOnlyMemory<byte> clientDataHash,
        ReadOnlyMemory<byte> keyHandle,
        TimeSpan timeout,
        [MaybeNullWhen(returnValue: false)] out AuthenticationData authenticationData,
        bool requireProofOfPresence = true)
    {
        _log.LogInformation("Try to authenticate a U2F credential.");

        var response = CommonAuthenticate(applicationId, clientDataHash, keyHandle, timeout, requireProofOfPresence);
        if (response.Status == ResponseStatus.Success)
        {
            authenticationData = response.GetData();

            return true;
        }

        authenticationData = null;

        return false;
    }

    // This is the similar to TryAuthenticate, except this will return the
    // AuthenticateResponse. The caller can check the Status and if Success,
    // get the AuthenticationData. If not, either return false or call
    // GetData to force an exception.
    private AuthenticateResponse CommonAuthenticate(
        ReadOnlyMemory<byte> applicationId,
        ReadOnlyMemory<byte> clientDataHash,
        ReadOnlyMemory<byte> keyHandle,
        TimeSpan timeout,
        bool requireProofOfPresence)
    {
        Task? touchMessageTask = null;
        var keyEntryData = new KeyEntryData();

        var timeoutToUseTimeSpan = GetTimeoutToUse(timeout);

        var authType = requireProofOfPresence
            ? U2fAuthenticationType.EnforceUserPresence
            : U2fAuthenticationType.DontEnforceUserPresence;

        var command = new AuthenticateCommand(authType, applicationId, clientDataHash, keyHandle);
        var response = Connection.SendCommand(command);
        if (response.Status == ResponseStatus.ConditionsNotSatisfied)
        {
            // On a separate thread, call the KeyCollector to announce we
            // need touch.
            if (KeyCollector is not null)
            {
                keyEntryData.Request = KeyEntryRequest.TouchRequest;
                touchMessageTask = Task.Run(() => _ = KeyCollector(keyEntryData));
            }

            var timer = new Stopwatch();

            try
            {
                timer.Start();

                do
                {
                    Thread.Sleep(100);
                    response = Connection.SendCommand(command);
                }
                while (response.Status == ResponseStatus.ConditionsNotSatisfied &&
                       timer.Elapsed < timeoutToUseTimeSpan);

                // Did we break out because of timeout or because the
                // response was something other than ConditionsNotSatisfied.
                // If the response.Status is still ConditionsNotSatisfied,
                // then it timed out.
                if (response.Status == ResponseStatus.ConditionsNotSatisfied)
                {
                    throw new TimeoutException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.UserInteractionTimeout));
                }
            }
            finally
            {
                timer.Stop();
                touchMessageTask?.Wait();

                if (KeyCollector is not null)
                {
                    keyEntryData.Request = KeyEntryRequest.Release;
                    _ = KeyCollector(keyEntryData);
                }
            }
        }

        return response;
    }

    /// <summary>
    ///     Helper function that takes a string and computes the SHA-256 hash of the UTF-8 encoding.
    /// </summary>
    /// <param name="data">
    ///     The string to encode.
    /// </param>
    /// <returns>
    ///     The SHA-256 hash of the string encoded as UTF-8.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Use this helper function to simplify calling other U2F functions such as <see cref="Register" /> and
    ///         Authenticate. This function will take the provided string, encode it into a byte array using the UTF-8
    ///         encoding style, and then pass those bytes into a SHA256 hash function. This transformation can be used for
    ///         things like the <c>applicationId</c> and the <c>clientDataHash</c>.
    ///     </para>
    ///     <para>
    ///         This function uses the SHA256 cryptographic function. By default, this will rely on the implementation
    ///         provided by the .NET base class library. If you wish to override this implementation with your own, you
    ///         can do so by setting a SHA256 creator on the <see cref="CryptographyProviders" /> class. See the User's
    ///         manual page on
    ///         <xref href="UsersManualAlternateCrypto">
    ///             providing alternate cryptographic implementations
    ///         </xref>
    ///         for more information.
    ///     </para>
    /// </remarks>
    public static byte[] EncodeAndHashString(string data)
    {
        using (var sha = CryptographyProviders.Sha256Creator())
        {
            byte[] encodedString = Encoding.UTF8.GetBytes(data);

            return sha.ComputeHash(encodedString);
        }
    }

    private Func<KeyEntryData, bool> EnsureKeyCollector()
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

    // Get the timeout to use as the following.
    //          0.0          returns MaxValue (10 million days)
    //   0.0 < value < max   returns value rounded up to next int
    //     max <= value      returns max
    private static TimeSpan GetTimeoutToUse(TimeSpan timeout)
    {
        double secondsToUse = MaxTimeoutSeconds;

        if (timeout.TotalSeconds < MaxTimeoutSeconds)
        {
            secondsToUse = timeout.Seconds;

            if (timeout.Milliseconds != 0)
            {
                secondsToUse++;
            }
        }

        return secondsToUse == 0
            ? TimeSpan.MaxValue
            : TimeSpan.FromSeconds(secondsToUse);
    }
}
