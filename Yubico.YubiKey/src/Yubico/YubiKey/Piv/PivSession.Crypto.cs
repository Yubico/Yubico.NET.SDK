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
using System.Security;
using Yubico.YubiKey.Piv.Commands;

namespace Yubico.YubiKey.Piv
{
    // This portion of the PivSession class contains code for performing
    // cryptographic operations using the private keys: sign, decrypt and key
    // agree.
    public sealed partial class PivSession : IDisposable
    {

        /// <summary>
        /// Create a digital signature using the key in the given slot.
        /// </summary>
        /// <remarks>
        /// The caller supplies the data to sign in the form of a formatted
        /// message digest.
        /// <para>
        /// This method returns the digital signature created, if it can build
        /// one. Otherwise it will throw an exception.
        /// </para>
        /// <para>
        /// If the slot specified is not one that can sign, or it does not
        /// contain a key, this method will throw an exception. If the input data
        /// is not the correct length, the method will throw an exception.
        /// </para>
        /// <para>
        /// If the key is ECC P-256, then the formatted digest is simply the
        /// message digest itself, but it must be exactly 32 bytes. If the input
        /// is not exactly 32 bytes, the method will throw an exception. If the
        /// input data is shorter than 32 bytes, prepend pad bytes of 00 until
        /// the length is exactly 32 bytes. You will almost certainly want to use
        /// SHA-256 as the digest algorithm. The signature will be the BER
        /// encoding of
        /// <code>
        ///   SEQUENCE {
        ///     r   INTEGER,
        ///     s   INTEGER }
        /// </code>
        /// </para>
        /// <para>
        /// If the key is ECC P-384, then the formatted digest is simply the
        /// message digest itself, but it must be exactly 48 bytes. If the input
        /// is not exactly 48 bytes, the method will throw an exception. If the
        /// input data is shorter than 48 bytes, prepend pad bytes of 00 until
        /// the length is exactly 48 bytes. You will almost certainly want to use
        /// SHA-384 as the digest algorithm. The signature will be the BER
        /// encoding of
        /// <code>
        ///   SEQUENCE {
        ///     r   INTEGER,
        ///     s   INTEGER }
        /// </code>
        /// </para>
        /// <para>
        /// If the key is RSA 1024, then the input must be exactly 128 bytes,
        /// otherwise the method will throw an exception. You can use the
        /// <see cref="Cryptography.RsaFormat"/> class to format the data. That
        /// class will be able to format the digest into either PKCS #1 v1.5 or a
        /// subset of PKCS #1 PSS. However, if that class does not support the
        /// exact format you want, you will have to write yout own formatting
        /// code and guarantee the input to this method is exactly 128 bytes
        /// (prepend pad bytes of 00 until the length is exactly 128 if needed).
        /// The signature will be a 128-byte block.
        /// </para>
        /// <para>
        /// If the key is RSA 2048, then the input must be exactly 256 bytes,
        /// otherwise the method will throw an exception. You can use the
        /// <see cref="Cryptography.RsaFormat"/> class to format the data. That
        /// class will be able to format the digest into either PKCS #1 v1.5 or a
        /// subset of PKCS #1 PSS. However, if that class does not support the
        /// exact format you want, you will have to write yout own formatting
        /// code and guarantee the input to this method is exactly 256 bytes
        /// (prepend pad bytes of 00 until the length is exactly 256 if needed).
        /// The signature will be a 256-byte block.
        /// </para>
        /// <para>
        /// Signing might require the PIN and/or touch, depending on the PIN and
        /// touch policies specified at the time the key was generated or
        /// imported.
        /// </para>
        /// <para>
        /// If a PIN is required, this method will call the necessary
        /// routines to verify the PIN. See <see cref="VerifyPin()"/> for more
        /// information on PIN verification. If the user cancels, this method
        /// will throw an exception.
        /// </para>
        /// <para>
        /// If touch is required, the YubiKey itself will flash its touch signal
        /// and wait. If the YubiKey is not touched before the touch timeout, the
        /// YubiKey will return with an error, and this method will throw an
        /// exception (<c>OperationCanceledException</c>). Note that this method
        /// will not make another effort to sign if the YubiKey is not touched,
        /// it will simply throw the exception.
        /// </para>
        /// <para>
        /// Note that on YubiKeys prior to version 5.3, it is not possible to know
        /// programmatically what the PIN or touch policies are without actually
        /// trying to sign. Also, it is not possible to know programmatically if
        /// an authentication failure is due to PIN or touch. This means that on
        /// older YubiKeys, this method will try to sign without the PIN, and if
        /// it does not work because of authentication failure, it will not know
        /// if the failure was due to PIN or touch. Hence, it will try to verify
        /// the PIN then try to sign again. This all means that on older
        /// YubiKeys, it is possible a YubiKey slot was originally configured
        /// with a PIN policy of "never" and a touch policy of "always", and this
        /// method will call for the PIN anyway. This happens if the user does
        /// not touch the YubiKey before the timeout. See the User's Manual entry
        /// on <xref href="UsersManualPivKeepingTrack">keeping track</xref> of
        /// slot contents.
        /// </para>
        /// </remarks>
        /// <param name="slotNumber">
        /// The slot containing the key to use.
        /// </param>
        /// <param name="dataToSign">
        /// The formatted message digest.
        /// </param>
        /// <returns>
        /// The resulting signature.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The slot number given was not valid, or the data to sign was an
        /// invalid length.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// There was no key in the slot specified or the data did not match the
        /// key (e.g. the data to sign was 32 bytes long but the key was ECC
        /// P-384).
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Either the PIN was required and the user canceled collection or touch
        /// was required and the user did not touch within the timeout period.
        /// </exception>
        /// <exception cref="SecurityException">
        /// The remaining retries count indicates the PIN is blocked.
        /// </exception>
        public byte[] Sign(byte slotNumber, ReadOnlyMemory<byte> dataToSign)
        {
            // This will verify the slot number and dataToSign length. If one or
            // both are incorrect, the call will throw an exception.
            var signCommand = new AuthenticateSignCommand(dataToSign, slotNumber);

            return PerformPrivateKeyOperation(
                slotNumber,
                signCommand,
                signCommand.Algorithm,
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.IncorrectDigestLength));
        }

        /// <summary>
        /// Decrypt the given data using the key in the given slot.
        /// </summary>
        /// <remarks>
        /// The YubiKey supports decryption only with RSA keys.
        /// <para>
        /// This method returns the raw decrypted data, if it can decrypt. It
        /// will not parse the formatted data. If it cannot decrypt for some
        /// reason, it will throw an exception.
        /// </para>
        /// <para>
        /// If the slot specified is not one that can decrypt, or it does not
        /// contain a key, or it contains an ECC key (instead of RSA), this
        /// method will throw an exception. If the input data is not the correct
        /// length, the method will throw an exception.
        /// </para>
        /// <para>
        /// If the key is RSA 1024, then the input must be exactly 128 bytes. If
        /// the key is RSA 2048, then the input must be exactly 256 bytes. If the
        /// input data is not the correct length, the method will throw an
        /// exception.
        /// </para>
        /// <para>
        /// The return will be the raw decrypted data. You can use the
        /// <see cref="Cryptography.RsaFormat"/> class to parse the data and
        /// extract the actual unpadded plaintext. That class will be able to
        /// parse from either PKCS #1 v1.5 or a subset of PKCS #1 OAEP. However,
        /// if that class does not support the exact format you want, you will
        /// have to write your own parsing code.
        /// </para>
        /// <para>
        /// Decrypting might require the PIN and/or touch, depending on the PIN
        /// and touch policies specified at the time the key was generated or
        /// imported.
        /// </para>
        /// <para>
        /// If a PIN is required, this method will call the necessary
        /// routines to verify the PIN. See <see cref="VerifyPin()"/> for more
        /// information on PIN verification. If the user cancels, this method
        /// will throw an exception.
        /// </para>
        /// <para>
        /// If touch is required, the YubiKey itself will flash its touch signal
        /// and wait. If the YubiKey is not touched before the touch timeout, the
        /// YubiKey will return with an error, and this method will throw an
        /// exception (<c>OperationCanceledException</c>). Note that this method
        /// will not make another effort to decrypt if the YubiKey is not
        /// touched, it will simply throw the exception.
        /// </para>
        /// <para>
        /// Note that on YubiKeys prior to version 5.3, it is not possible to know
        /// programmatically what the PIN or touch policies are without actually
        /// trying to decrypt. Also, it is not possible to know programmatically
        /// if an authentication failure is due to PIN or touch. This means that
        /// on older YubiKeys, this method will try to decrypt without the PIN,
        /// and if it does not work because of authentication failure, it will
        /// not know if the failure was due to PIN or touch. Hence, it will try
        /// to verify the PIN then try to sign again. This all means that on
        /// older YubiKeys, it is possible a YubiKey slot was originally
        /// configured with a PIN policy of "never" and a touch policy of
        /// "always", and this method will call for the PIN anyway. This happens
        /// if the user does not touch the YubiKey before the timeout. See the
        /// User's Manual entry on
        /// <xref href="UsersManualPivKeepingTrack">keeping track</xref> of slot
        /// contents.
        /// </para>
        /// </remarks>
        /// <param name="slotNumber">
        /// The slot containing the key to use.
        /// </param>
        /// <param name="dataToDecrypt">
        /// The ciphertext.
        /// </param>
        /// <returns>
        /// The resulting decrypted block.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The slot number given was not valid, or the data to decrypt was an
        /// invalid length.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// There was no key in the slot specified or the data did not match the
        /// key (e.g. the data to decrypt was 128 bytes long but the key was RSA
        /// 2048).
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Either the PIN was required and the user canceled collection or touch
        /// was required and the user did not touch within the timeout period.
        /// </exception>
        /// <exception cref="SecurityException">
        /// The remaining retries count indicates the PIN is blocked.
        /// </exception>
        public byte[] Decrypt(byte slotNumber, ReadOnlyMemory<byte> dataToDecrypt)
        {
            // This will verify the slot number and dataToDecrypt length. If one
            // or both are incorrect, the call will throw an exception.
            var decryptCommand = new AuthenticateDecryptCommand(dataToDecrypt, slotNumber);

            return PerformPrivateKeyOperation(
                slotNumber,
                decryptCommand,
                decryptCommand.Algorithm,
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.IncorrectCiphertextLength));
        }

        /// <summary>
        /// Perform Phase 2 of EC Diffie-Hellman Key Agreement using the private
        /// key in the given slot, and the corresponding party's public key.
        /// </summary>
        /// <remarks>
        /// The YubiKey supports key agreement only with ECC keys.
        /// <para>
        /// This method returns the raw shared secret data, if it can perform the
        /// key agreement operation. It will not perform any derivation
        /// operations. The result will be the same size as the key. That is, for
        /// a 256-bit ECC key, the shared secret is 32 bytes, and for a 384-bit
        /// key, the shared secret is 48 bytes.
        /// </para>
        /// <para>
        /// The data returned is not formatted, nor encoded, it is simply a byte
        /// array. It happens to be the x coordinate of an ECC point that is the
        /// result of an EC scalar muliplication operation.
        /// </para>
        /// <para>
        /// Key Agreement might require the PIN and/or touch, depending on the
        /// PIN and touch policies specified at the time the key was generated or
        /// imported.
        /// </para>
        /// <para>
        /// If a PIN is required, this method will call the necessary
        /// routines to verify the PIN. See <see cref="VerifyPin()"/> for more
        /// information on PIN verification. If the user cancels, this method
        /// will throw an exception.
        /// </para>
        /// <para>
        /// If touch is required, the YubiKey itself will flash its touch signal
        /// and wait. If the YubiKey is not touched before the touch timeout, the
        /// YubiKey will return with an error, and this method will throw an
        /// exception (<c>OperationCanceledException</c>). Note that this method
        /// will not make another effort to perform key agreement if the YubiKey
        /// is not touched, it will simply throw the exception.
        /// </para>
        /// <para>
        /// Note that on YubiKeys prior to version 5.3, it is not possible to know
        /// programmatically what the PIN or touch policies are without actually
        /// trying to perform key agreement. Also, it is not possible to know
        /// programmatically if an authentication failure is due to PIN or touch.
        /// This means that on older YubiKeys, this method will try to perform
        /// the key agreement operation without the PIN, and if it does not work
        /// because of authentication failure, it will not know if the failure
        /// was due to PIN or touch. Hence, it will try to verify the PIN then
        /// try to perform the key agreement operation again. This all means that
        /// on older YubiKeys, it is possible a YubiKey slot was originally
        /// configured with a PIN policy of "never" and a touch policy of
        /// "always", and this method will call for the PIN anyway. This happens
        /// if the user does not touch the YubiKey before the timeout. See the
        /// User's Manual entry on
        /// <xref href="UsersManualPivKeepingTrack">keeping track</xref> of slot
        /// contents.
        /// </para>
        /// </remarks>
        /// <param name="slotNumber">
        /// The slot containing the key to use.
        /// </param>
        /// <param name="correspondentPublicKey">
        /// The correspondent's public key.
        /// </param>
        /// <returns>
        /// The resulting shared secret data.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <c>correspondentPublicKey</c> argument is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The slot number given was not valid, or the public key was invalid
        /// (e.g. empty, wrong algorithm).
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// There was no key in the slot specified or the public key did not
        /// match the private key (e.g. the public key was for ECC P256 but the
        /// private key in the given slot was ECC P384).
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Either the PIN was required and the user canceled collection or touch
        /// was required and the user did not touch within the timeout period.
        /// </exception>
        /// <exception cref="SecurityException">
        /// The remaining retries count indicates the PIN is blocked.
        /// </exception>
        public byte[] KeyAgree(byte slotNumber, PivPublicKey correspondentPublicKey)
        {
            if (correspondentPublicKey is null)
            {
                throw new ArgumentNullException(nameof(correspondentPublicKey));
            }
            if (!correspondentPublicKey.Algorithm.IsEcc())
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPublicKeyData));
            }

            byte[] publicPoint = ((PivEccPublicKey)correspondentPublicKey).PublicPoint.ToArray();

            // This will verify the slot number and dataToSign length. If one or
            // both are incorrect, the call will throw an exception.
            var keyAgreeCommand = new AuthenticateKeyAgreeCommand(publicPoint, slotNumber);

            return PerformPrivateKeyOperation(
                slotNumber,
                keyAgreeCommand,
                keyAgreeCommand.Algorithm,
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.IncorrectEccKeyLength));
        }

        // Common code, this performs either Signing, Decryption, or Key
        // Agreement. Just pass in the actual command to run, along with some
        // other information.
        private byte[] PerformPrivateKeyOperation(
            byte slotNumber,
            IYubiKeyCommand<IYubiKeyResponseWithData<byte[]>> command,
            PivAlgorithm algorithm,
            string algorithmExceptionMessage)
        {
            bool pinRequired = true;

            // First, do we need to verify the PIN? It is possible the key in the
            // slot was generated or imported with a PIN policy of Never. If
            // that's the case, we don't want to try to verify the PIN.
            // If the PIN policy is Once and the PIN is already verified, no need
            // to verify the PIN again.
            // If the PIN policy is Always, we need to verify the PIN.

            // Metadata will give us our answer, but that feature is
            // available only on YubiKeys beginning with version 5.3.
            if (_yubiKeyDevice.HasFeature(YubiKeyFeature.PivMetadata))
            {
                var metadataCommand = new GetMetadataCommand(slotNumber);
                GetMetadataResponse metadataResponse = Connection.SendCommand(metadataCommand);

                // If there is no key in the slot, this will throw an exception.
                PivMetadata metadata = metadataResponse.GetData();

                // We know the algorithm based on the input data. Is it the
                // algorithm of the key in the slot?
                // We can make this check with metadata. Without metadata there's
                // no way to know until we try to perform the operation.
                if (metadata.Algorithm != algorithm)
                {
                    throw new ArgumentException(algorithmExceptionMessage);
                }

                // If the metadata says Never, then pinRequired is false.
                // If the metadata says Once, and the PIN is verified, then the
                // PIN is not required.
                // The only other case is Always which means we set the
                // pinRequired to true, but we init that variable to true.
                if ((metadata.PinPolicy == PivPinPolicy.Never) ||
                    ((metadata.PinPolicy == PivPinPolicy.Once) && PinVerified))
                {
                    pinRequired = false;
                }
            }
            else
            {
                // Metadata is not available on this YubiKey.
                // Try to perform the operation. If it works, we're done. If not,
                // we can get limited information on why not.
                IYubiKeyResponseWithData<byte[]> initialResponse = Connection.SendCommand(command);

                // If the response is AuthRequired, either the PIN is required or
                // touch is. The response does not tell us which.
                // If the PIN is already verified, we still don't know if
                // that means it must be touch, because the policy might be
                // PIN always.
                // So we don't know. Set pinRequired to true.
                // This means it is possible the slot is configured for a PIN
                // policy of never, and the problem was touch. If so, we're
                // asking for the PIN even though it is not required, but we have
                // no other choice.
                // Because we init pinRequired to true, what we need to do now is
                // perform the cases when the response is Success or some other
                // error.
                if (initialResponse.Status != ResponseStatus.AuthenticationRequired)
                {
                    // If the response is not AuthRequired, then GetData.
                    // If the response was Success, we'll get the result and we're
                    // done.
                    // If it was not Success, this call will throw an exception.
                    // Anything other than Success means invalid key or empty
                    // slot or pub key data does not match the key, etc.
                    return initialResponse.GetData();
                }
            }

            if (pinRequired == true)
            {
                // This is the verify method that will throw an exception if the
                // user cancels.
                VerifyPin();
            }

            IYubiKeyResponseWithData<byte[]> response = Connection.SendCommand(command);

            if (response.Status != ResponseStatus.AuthenticationRequired)
            {
                return response.GetData();
            }

            // If we reach this code, the Status is AuthRequired and the problem is touch.
            throw new OperationCanceledException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.IncompleteCommandInput));
        }
    }
}
