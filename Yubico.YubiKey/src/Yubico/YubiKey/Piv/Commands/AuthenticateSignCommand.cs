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
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using Yubico.Core.Iso7816;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// Build a digital signature using the private key in one of the PIV slots.
    /// </summary>
    /// <remarks>
    /// In the PIV standard, there is a command called GENERAL AUTHENTICATE.
    /// Although it is one command, it can do four things: authenticate a
    /// management key (challenge-response), sign arbitrary data, RSA decryption,
    /// and EC Diffie-Hellman. The SDK breaks these four operations into separate
    /// classes. This class is how you perform "GENERAL AUTHENTICATE: Sign".
    /// <para>
    /// The partner Response class is <see cref="AuthenticateSignResponse"/>.
    /// </para>
    /// <para>
    /// In order to create a signature, it is possible you must verify the PIN.
    /// The PIN is not part of this command. For information on how to verify a
    /// PIN in order to perform operations, see the User's Manual entry on
    /// <xref href="UsersManualPivAccessControl"> PIV commands access control</xref>.
    /// </para>
    /// <para>
    /// The caller supplies the slot to use. Slot <c>9C</c> is the "digital
    /// signature" slot, but any PIV slot that holds a private key, other then
    /// <c>F9</c>, will be able to create a signature. That is, any PIV slot
    /// other than <c>80</c>, <c>81</c>, <c>9B</c>, or <c>F9</c> will be able to
    /// sign. Note that slot <c>F9</c> contains the attestation key, which will
    /// sign a certificate it creates, so it can sign. It simply cannot sign
    /// arbitrary data, only attestation statements.
    /// </para>
    /// <para>
    /// The caller also supplies the digest of the data to sign. For RSA
    /// signatures, the digest must be formatted following PKCS 1 version 1.5, or
    /// PKCS 1 PSS (Probabilistic Signature Scheme). See RFC 8017 for details on
    /// these formats. For ECC signatures, the digest provided is not formatted
    /// further. For example, if you digest the data to sign using SHA-256, the
    /// digest is 32 bytes and you provide 32 bytes to this command. See also the
    /// User's Manual entry on
    /// <xref href="UsersManualPivCommands#authenticate-sign"> signing </xref>
    /// in the PIV commands page.
    /// </para>
    /// <para>
    /// If the key is ECC-P256, the digest must be 256 bits (32 bytes). If the
    /// key is ECC-P384, the digest must be 384 bits (48 bytes).
    /// </para>
    /// <para>
    /// You should know which algorithm (and size) the key in the requested slot
    /// is, because if you provide the wrong format (or size) of digest, the
    /// YubiKey will return an error.
    /// </para>
    /// <para>
    /// This class will copy a reference to the digest data, so you should not
    /// clear or alter that input data until this class is done with it, which is
    /// after the call to <c>SendCommand</c>.
    /// </para>
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    ///   /* This example assumes there is some code that will digest the data. */
    ///   byte[] sha384Digest = DigestDataToSign(SHA384, dataToSign);<br/>
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br/>
    ///   var signCommand = new AuthenticateSignCommand(sha384Digest, PivSlot.Signing);
    ///   AuthenticateSignResponse signResponse = connection.SendCommand(signCommand);<br/>
    ///   if (signResponse.Status != ResponseStatus.Success)
    ///   {
    ///     // handle error
    ///   }
    ///   byte[] signature = signResponse.GetData();
    /// </code>
    /// </remarks>
    public sealed class AuthenticateSignCommand : AuthenticateCommand, IYubiKeyCommand<AuthenticateSignResponse>
    {
        private const byte DigestTag = 0x81;

        private const int Rsa1024DigestDataLength = 128;
        private const int Rsa2048DigestDataLength = 256;
        private const int EccP256DigestDataLength = 32;
        private const int EccP384DigestDataLength = 48;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private AuthenticateSignCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initializes a new instance of the AuthenticateSignCommand class. This
        /// command takes the slot number and the (possibly formatted) digest of
        /// the data to sign.
        /// </summary>
        /// <remarks>
        /// The slot number must be for a slot that holds an asymmetric key and
        /// can perform arbitrary signing, which is all asymmetric key slots other
        /// than <c>F9</c>. See the User's Manual
        /// <xref href="UsersManualPivSlots"> entry on PIV slots </xref>,
        /// <xref href="UsersManualPivCommands#authenticate-sign"> entry on signing </xref>,
        /// and <see cref="PivSlot"/>.
        /// <para>
        /// The digest data is formatted if RSA. If the key that will be used to
        /// sign is RSA-1024, the the digest data must be 128 (1024 bits) bytes
        /// long. If the key is RSA-2048, then the digest data must be 256 bytes
        /// (2048 bits) long. See also the User's Manual entry on
        /// <xref href="UsersManualPivCommands#authenticate-sign"> signing </xref>
        /// in the PIV commands page.
        /// </para>
        /// <para>
        /// For ECC, the digest data is not formatted, it is simply the output of
        /// the message digest algorithm. If the key that will be used to sign is
        /// ECC-P256, then the digest data must be 32 bytes (256 bits) long. You
        /// will likely use SHA-256, which is the algorithm specified in the PIV
        /// standard. If the key is ECC-P384, then the digest data must be 48
        /// bytes (384 bits) long. You will likely use SHA-384, which is the
        /// algorithm specified in the PIV standard.
        /// </para>
        /// <para>
        /// Note that if the result of the digest has leading 00 bytes, you leave
        /// those bytes in the <c>digestData</c>. For example:
        /// </para>
        /// <code>
        ///  If the result of the SHA-256 digest is
        ///    00 00 87 A9 31 ... 7C
        ///  then you pass in 32 bytes:
        ///    00 00 87 A9 31 ... 7C
        ///  Do not strip the leading 00 bytes and pass in only 30 bytes (87 A9 ... 7C).
        /// </code>
        /// <para>
        /// If you are signing with ECC and you use a digest algorithm that
        /// produces smaller output (not recommended, but if you do), prepend 00
        /// bytes to make sure the length of data passed in is the correct length.
        /// </para>
        /// </remarks>
        /// <param name="digestData">
        /// The message digest of the data to sign, formatted, if RSA.
        /// </param>
        /// <param name="slotNumber">
        /// The slot holding the private key to use.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The data to sign (formatted digest) is not the correct length.
        /// </exception>
        public AuthenticateSignCommand(ReadOnlyMemory<byte> digestData, byte slotNumber)
        {
            DataTag = DigestTag;
            Data = digestData;
            SlotNumber = slotNumber;

            // Determine the algorithm based on the length of the digest data.
            // Currently the length of the data must be 128 (RSA-1024), 256
            // (RSA-2048), 32 (ECC-P256), or 48 (ECC-P384).
            // Return the PivAlgorithm, or if the length is not supported, throw an
            // exception.
            Algorithm = digestData.Length switch
            {
                Rsa1024DigestDataLength => PivAlgorithm.Rsa1024,
                Rsa2048DigestDataLength => PivAlgorithm.Rsa2048,
                EccP256DigestDataLength => PivAlgorithm.EccP256,
                EccP384DigestDataLength => PivAlgorithm.EccP384,
                _ => throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncorrectDigestLength)),
            };
        }

        /// <inheritdoc />
        public AuthenticateSignResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new AuthenticateSignResponse(responseApdu);
    }
}
