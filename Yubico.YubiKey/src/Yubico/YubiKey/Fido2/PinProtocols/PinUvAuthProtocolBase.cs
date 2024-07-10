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
using System.Security.Cryptography;
using Yubico.Core.Cryptography;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2.PinProtocols
{
    /// <summary>
    ///     Base class for FIDO2 PIN/UV auth protocol implementations.
    /// </summary>
    /// <remarks>
    ///     A PIN/UV auth protocol is a set of methods defined by the FIDO2 CTAP specification. The abstract interface is
    ///     defined in section 6.5.4. As of FIDO 2.1, there are only two PIN protocols defined: protocol one and protocol
    ///     two. These two implementations share some common code. The abstract interface as well as any shared code is
    ///     defined by this class.
    /// </remarks>
    public abstract class PinUvAuthProtocolBase : IDisposable
    {
        private bool _disposed;

        /// <summary>
        ///     Gets the identifier of the PIN / UV authentication protocol that this instance implements.
        /// </summary>
        public PinUvAuthProtocol Protocol { get; protected set; }

        /// <summary>
        ///     The public key returned by the YubiKey.
        /// </summary>
        /// <remarks>
        ///     The caller will obtain the YubiKey's public key using the
        ///     <see cref="Yubico.YubiKey.Fido2.Commands.GetKeyAgreementCommand" />
        ///     and pass it to the <see cref="Encapsulate" /> method. A reference to
        ///     the key passed into that method will be stored in this property.
        ///     <para>
        ///         A call to <see cref="Initialize" /> will set this to <c>null</c>. That
        ///         is, a new public key must be obtained for each PIN/UV authentication
        ///         session with the YubiKey. Even if the new session is initiated with
        ///         the previously-used YubiKey, the public key must be obtained, because
        ///         the YubiKey might generate a new public key.
        ///     </para>
        /// </remarks>
        public CoseKey? AuthenticatorPublicKey { get; protected set; }

        /// <summary>
        ///     Gets the public key generated during the call to
        ///     <see cref="Encapsulate" />.
        /// </summary>
        /// <remarks>
        ///     This will be <c>null</c> until the <c>Encapsulate</c> method is
        ///     called.
        /// </remarks>
        public CoseKey? PlatformPublicKey { get; protected set; }

        /// <summary>
        ///     Gets the encryption key derived from the shared value computed during
        ///     the call to <see cref="Encapsulate" />. This can be the same as the
        ///     <see cref="AuthenticationKey" />.
        /// </summary>
        /// <remarks>
        ///     This will be <c>null</c> until the <c>Encapsulate</c> method is
        ///     called.
        /// </remarks>

        // Note that it is the responsibility of the subclass to overwrite any
        // buffers containing key data.
        public ReadOnlyMemory<byte>? EncryptionKey { get; protected set; }

        /// <summary>
        ///     Gets the authentication key derived from the shared value computed during
        ///     the call to <see cref="Encapsulate" />. This can be the same as the
        ///     <see cref="EncryptionKey" />.
        /// </summary>
        /// <remarks>
        ///     This will be <c>null</c> until the <c>Encapsulate</c> method is
        ///     called.
        /// </remarks>

        // Note that it is the responsibility of the subclass to overwrite any
        // buffers containing key data.
        public ReadOnlyMemory<byte>? AuthenticationKey { get; protected set; }

        /// <summary>
        ///     Release resources, overwrite sensitive data.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     This is run by the platform when starting a series of transactions
        ///     with a specific authenticator.
        /// </summary>
        /// <remarks>
        ///     This will reset the internal state of the object to the same as
        ///     immediately after instantiation. Any data acquired, generated, or
        ///     computed during operations in a session of PIN/UV Authentication with
        ///     a specific authenticator will be lost.
        ///     <para>
        ///         Generally you will create an instance of one of the protocols and use
        ///         it to perform the appropriate PIN/UV authentication operations with
        ///         an authenticator. Then, to operate on a different authenticator (or
        ///         the same authenticator in a new session), create a new instance of
        ///         the protocol or reuse an existing object but call <c>Initialize</c>
        ///         before beginning operations.
        ///     </para>
        /// </remarks>
        public virtual void Initialize()
        {
            AuthenticatorPublicKey = null;
            PlatformPublicKey = null;
            EncryptionKey = null;
            AuthenticationKey = null;
        }

        /// <summary>
        ///     Generates a new platform key pair and uses the private key along with
        ///     the peerPublicKey to compute the shared value. It then derives the
        ///     shared keys (encryption and authentication) from the shared value.
        /// </summary>
        /// <remarks>
        ///     This will generate a new public and private key, compute the shared
        ///     value, and discard the private key. The resulting public key will be
        ///     found in the <see cref="PlatformPublicKey" /> property, and the
        ///     derived keys will be found in the <see cref="EncryptionKey" /> and
        ///     <see cref="AuthenticationKey" /> properties.
        ///     <para>
        ///         This method can be called only after instantiation or a call to
        ///         <see cref="Initialize" />. Otherwise, this method will throw an
        ///         exception.
        ///     </para>
        /// </remarks>
        /// <param new="authenticatorPublicKey">
        ///     The YubiKey's public key obtained by calling the
        ///     <see cref="Yubico.YubiKey.Fido2.Commands.GetKeyAgreementCommand" />.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     The <c>authenticatorPublicKey</c> argument is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     The <c>authenticatorPublicKey</c> argument is not an appropriate key
        ///     object (e.g. wrong algorithm).
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     The object is not available for <c>Encapsulate</c> because it still
        ///     contains data from a previous operation. It is necessary to call
        ///     <c>Initialize</c> before reusing an Protocol object.
        /// </exception>
        public virtual void Encapsulate(CoseKey authenticatorPublicKey)
        {
            // This can be called only if there is currently nothing in the
            // object.
            if (!(PlatformPublicKey is null))
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidCallOrder));
            }

            if (authenticatorPublicKey is null)
            {
                throw new ArgumentNullException(nameof(authenticatorPublicKey));
            }

            // Currently, only protocol 1 and 2 are supported (the only protocols
            // in the standard). Both of them generate a new P-256 EC key pair.
            // If we ever support a different protocol that uses a different
            // algorithm, override this method.
            if (!(authenticatorPublicKey is CoseEcPublicKey))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPublicKeyData));
            }

            var authPubKey = (CoseEcPublicKey)authenticatorPublicKey;

            // Create a local copy of the authenticatorPublicKey.
            AuthenticatorPublicKey = new CoseEcPublicKey(
                CoseEcCurve.P256, authPubKey.XCoordinate, authPubKey.YCoordinate);

            ECParameters? platformKeyPair = null;
            byte[] sharedValue = Array.Empty<byte>();

            try
            {
                IEcdhPrimitives ecdh = CryptographyProviders.EcdhPrimitivesCreator();
                platformKeyPair = ecdh.GenerateKeyPair(ECCurve.NamedCurves.nistP256);

                PlatformPublicKey = new CoseEcPublicKey(platformKeyPair.Value);
                sharedValue = ecdh.ComputeSharedSecret(authPubKey.ToEcParameters(), platformKeyPair.Value.D);
                DeriveKeys(sharedValue);
            }
            finally
            {
                if (platformKeyPair.HasValue)
                {
                    CryptographicOperations.ZeroMemory(platformKeyPair.Value.D);
                }

                CryptographicOperations.ZeroMemory(sharedValue);
            }
        }

        /// <summary>
        ///     Returns the AES-256-CBC encryption of plaintext using an IV specified
        ///     by the protocol and the <see cref="EncryptionKey" />. With protocol 1
        ///     the IV is all 00 bytes. With protocol 2, it is a new, random value.
        /// </summary>
        /// <param name="plaintext">
        ///     The data to encrypt.
        /// </param>
        /// <param name="offset">
        ///     The offset in <c>plaintext</c> where the method will begin
        ///     encrypting.
        /// </param>
        /// <param name="length">
        ///     The number of bytes to encrypt.
        /// </param>
        /// <returns>
        ///     A new byte array containing the encrypted data. With protocol 2, the
        ///     ciphertext is actually the concatenation of the IV and the encrypted
        ///     data.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     The <c>plaintext</c> argument is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     The object has been created or initialized, but the
        ///     <see cref="Encapsulate" /> method has not been called.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     The length of the <c>plaintext</c> is not a multiple of the AES block
        ///     size (16 bytes).
        /// </exception>
        public abstract byte[] Encrypt(byte[] plaintext, int offset, int length);

        /// <summary>
        ///     Returns the AES-256-CBC decryption of ciphertext using an IV specified
        ///     by the protocol and the <see cref="EncryptionKey" />. With protocol 1
        ///     the IV is all 00 bytes. With protocol 2, it is the first block size
        ///     bytes of <c>ciphertext</c>.
        /// </summary>
        /// <remarks>
        ///     Note that this method will verify that the input buffer, offset, and
        ///     length are valid.
        /// </remarks>
        /// <param name="ciphertext">
        ///     The data to decrypt.
        /// </param>
        /// <param name="offset">
        ///     The offset in <c>ciphertext</c> where the method will begin
        ///     decrypting.
        /// </param>
        /// <param name="length">
        ///     The number of bytes to decrypt.
        /// </param>
        /// <returns>
        ///     A new byte array containing the decrypted data.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     The <c>ciphertext</c> argument is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     The object has been created or initialized, but the
        ///     <see cref="Encapsulate" /> method has not been called.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     The length of the <c>ciphertext</c> is not a multiple of the AES block
        ///     size (16 bytes).
        /// </exception>
        public abstract byte[] Decrypt(byte[] ciphertext, int offset, int length);

        /// <summary>
        ///     Returns the result of computing HMAC-SHA-256 on the given message
        ///     using the <see cref="AuthenticationKey" />. With protocol 1, the
        ///     result is the first 16 bytes of the HMAC, and with protocol 2 it is
        ///     the entire 32-byte result.
        /// </summary>
        /// <param name="message">
        ///     The data to be authenticated.
        /// </param>
        /// <returns>
        ///     A new byte array containing the authentication result.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     The <c>message</c> argument is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     The object has been created or initialized, but the
        ///     <see cref="Encapsulate" /> method has not been called.
        /// </exception>
        public abstract byte[] Authenticate(byte[] message);

        /// <summary>
        ///     Returns the result of computing HMAC-SHA-256 on the given message
        ///     using the provided <c>keyData</c>. With protocol 1, the result is the
        ///     first 16 bytes of the HMAC, and with protocol 2 it is the entire
        ///     32-byte result.
        /// </summary>
        /// <param name="keyData">
        ///     The key to use to authenticate.
        /// </param>
        /// <param name="message">
        ///     The data to be authenticated.
        /// </param>
        /// <returns>
        ///     A new byte array containing the authentication result.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     The <c>keyData</c> or <c>message</c> argument is null.
        /// </exception>
        protected abstract byte[] Authenticate(byte[] keyData, byte[] message);

        /// <summary>
        ///     Returns the result of computing HMAC-SHA-256 on the given message
        ///     using the <c>pinToken</c> as the key. With protocol 1, the result is
        ///     the first 16 bytes of the HMAC, and with protocol 2 it is the entire
        ///     32-byte result.
        /// </summary>
        /// <remarks>
        ///     It is possible to obtain the PIN token by calling the command
        ///     <see cref="Yubico.YubiKey.Fido2.Commands.GetPinTokenCommand" />. The
        ///     YubiKey will return the PIN token encrypted using the shared secret.
        ///     <para>
        ///         Pass that encrypted PIN token to this method as the first argument.
        ///         This method will decrypt the PIN token using the <c>EncryptionKey</c>
        ///         and then perform the authentication on the <c>message</c>.
        ///     </para>
        /// </remarks>
        /// <param name="pinToken">
        ///     The PIN token returned by the YubiKey. This is the encrypted value,
        ///     do not decrypt it.
        /// </param>
        /// <param name="message">
        ///     The data to be authenticated.
        /// </param>
        /// <returns>
        ///     A new byte array containing the authentication result.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     The <c>pinToken</c> or <c>message</c> argument is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     The object has been created or initialized, but the
        ///     <see cref="Encapsulate" /> method has not been called.
        /// </exception>
        public byte[] AuthenticateUsingPinToken(byte[] pinToken, byte[] message)
        {
            if (pinToken is null)
            {
                throw new ArgumentNullException(nameof(pinToken));
            }

            return AuthenticateUsingPinToken(pinToken, offset: 0, pinToken.Length, message);
        }

        /// <summary>
        ///     Returns the result of computing HMAC-SHA-256 on the given message
        ///     using the <c>pinToken</c> as the key. With protocol 1, the result is
        ///     the first 16 bytes of the HMAC, and with protocol 2 it is the entire
        ///     32-byte result.
        /// </summary>
        /// <remarks>
        ///     This is the same as <see cref="AuthenticateUsingPinToken(byte[],byte[])" />,
        ///     except this specifies an offset and length of the <c>pinToken</c>
        ///     argument.
        /// </remarks>
        /// <param name="pinToken">
        ///     The PIN token returned by the YubiKey. This is the encrypted value,
        ///     do not decrypt it.
        /// </param>
        /// <param name="offset">
        ///     The offset into <c>pinToken</c> buffer where the data begins.
        /// </param>
        /// <param name="length">
        ///     The length, in bytes, of the pin token.
        /// </param>
        /// <param name="message">
        ///     The data to be authenticated.
        /// </param>
        /// <returns>
        ///     A new byte array containing the authentication result.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     The <c>pinToken</c> or <c>message</c> argument is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     The object has been created or initialized, but the
        ///     <see cref="Encapsulate" /> method has not been called.
        /// </exception>
        public virtual byte[] AuthenticateUsingPinToken(byte[] pinToken, int offset, int length, byte[] message)
        {
            if (pinToken is null)
            {
                throw new ArgumentNullException(nameof(pinToken));
            }

            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            byte[] tokenKey = Decrypt(pinToken, offset, length);
            try
            {
                return Authenticate(tokenKey, message);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(tokenKey);
            }
        }

        /// <summary>
        ///     The key derivation function to run while performing ECDH. This will
        ///     derive both the <see cref="EncryptionKey" /> and the
        ///     <see cref="AuthenticationKey" />.
        /// </summary>
        /// <param name="buffer">
        ///     The shared value computed by ECDH.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     The <c>buffer</c> argument is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     The HMAC with SHA-256 provider failed.
        /// </exception>
        protected abstract void DeriveKeys(byte[] buffer);

        /// <summary>
        ///     Release resources, overwrite sensitive data.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Initialize();
                }

                _disposed = true;
            }
        }
    }
}
