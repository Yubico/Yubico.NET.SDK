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
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    ///     Decrypt data using the private RSA key in one of the PIV slots.
    /// </summary>
    /// <remarks>
    ///     In the PIV standard, there is a command called GENERAL AUTHENTICATE.
    ///     Although it is one command, it can do four things: authenticate a
    ///     management key (challenge-response), sign arbitrary data, RSA decryption,
    ///     and EC Diffie-Hellman. The SDK breaks these four operations into separate
    ///     classes. This class is how you perform "GENERAL AUTHENTICATE: RSA
    ///     Decryption".
    ///     <para>
    ///         The partner Response class is <see cref="AuthenticateDecryptResponse" />.
    ///     </para>
    ///     <para>
    ///         Use this Command class only if the slot selected holds an RSA private
    ///         key. If the private key in a slot called upon to perform this command is
    ///         ECC, the YubiKey will return an error. While there is an algorithm known
    ///         as "EC Encryption Scheme" (aka "EC El Gamal"), the YubiKey does not
    ///         support it. Hence, this command will not be able to decrypt using an EC
    ///         key. Therefore, you should know which algorithm (and size) the key in the
    ///         requested slot is before calling on this class.
    ///     </para>
    ///     <para>
    ///         In order to decrypt, it is possible you must verify the PIN. The PIN is
    ///         not part of this command. For information on how to verify a PIN in order
    ///         to perform operations, see the User's Manual entry on
    ///         <xref href="UsersManualPivAccessControl"> PIV commands access control</xref>.
    ///     </para>
    ///     <para>
    ///         The caller supplies the slot to use. Slot <c>9D</c> is the "key
    ///         management" slot, but any PIV slot that holds a private key, other then
    ///         <c>F9</c>, will be able to decrypt (as long as it contains an RSA private
    ///         key). That is, any PIV slot other than <c>80</c>, <c>81</c>, <c>9B</c>,
    ///         or <c>F9</c> will be able to decrypt. Note that slot <c>F9</c> contains
    ///         the attestation key, which will sign a certificate it creates, but it
    ///         cannot decrypt.
    ///     </para>
    ///     <para>
    ///         The caller supplies the data to decrypt. It must be a block the same size
    ///         as the key. For an RSA-1024 key, the block must be 128 bytes, for an
    ///         RSA-2048 key, the block must be 256 bytes. If the actual data to decrypt
    ///         is shorter, it must be provided with as many prepended 00 bytes as needed
    ///         to make sure the block is the appropriate length.
    ///     </para>
    ///     <para>
    ///         This class will copy a reference to the data to decrypt, so you should not
    ///         clear or alter that input data until this class is done with it, which is
    ///         after the call to <c>SendCommand</c>.
    ///     </para>
    ///     <para>
    ///         Example:
    ///     </para>
    ///     <code language="csharp">
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br />
    ///   var decryptCommand = new AuthenticateDecryptCommand(dataToDecrypt, PivSlot.KeyManagement);
    ///   AuthenticateDecryptResponse decryptResponse = connection.SendCommand(decryptCommand);<br />
    ///   if (decryptResponse.Status != ResponseStatus.Success)
    ///   {
    ///     // handle error
    ///   }
    ///   byte[] decryptedData = decryptResponse.GetData();
    /// </code>
    /// </remarks>
    public sealed class AuthenticateDecryptCommand : AuthenticateCommand, IYubiKeyCommand<AuthenticateDecryptResponse>
    {
        private const int DataToDecryptTag = 0x81;

        private const int Rsa1024BlockSize = 128;
        private const int Rsa2048BlockSize = 256;
        private const int Rsa3072BlockSize = 384;
        private const int Rsa4096BlockSize = 512;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private AuthenticateDecryptCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Initializes a new instance of the AuthenticateDecryptCommand class.
        ///     This command takes the slot number and the data to decrypt.
        /// </summary>
        /// <remarks>
        ///     The slot number must be for a slot that holds an RSA private key. It
        ///     cannot be <c>F9</c> (the attestation key).
        ///     <para>
        ///         If the key that will be used to decrypt is RSA-1024, then the data to
        ///         decrypt must be 128 (1024 bits) bytes long. If the key is RSA-2048,
        ///         then the data must be 256 bytes (2048 bits) long. See also the User's
        ///         Manual entry on
        ///         <xref href="UsersManualPivCommands#authenticate-decrypt"> decrypting </xref>
        ///         in the PIV commands page.
        ///     </para>
        /// </remarks>
        /// <param name="dataToDecrypt">
        ///     The data to decrypt.
        /// </param>
        /// <param name="slotNumber">
        ///     The slot holding the private key to use.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     The ciphertext is not the correct length.
        /// </exception>
        public AuthenticateDecryptCommand(ReadOnlyMemory<byte> dataToDecrypt, byte slotNumber)
        {
            DataTag = DataToDecryptTag;
            Data = dataToDecrypt;
            SlotNumber = slotNumber;

            Algorithm = dataToDecrypt.Length switch
            {
                Rsa1024BlockSize => PivAlgorithm.Rsa1024,
                Rsa2048BlockSize => PivAlgorithm.Rsa2048,
                Rsa3072BlockSize => PivAlgorithm.Rsa3072,
                Rsa4096BlockSize => PivAlgorithm.Rsa4096,
                _ => throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncorrectCiphertextLength))
            };
        }

        /// <inheritdoc />
        public AuthenticateDecryptResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new AuthenticateDecryptResponse(responseApdu);
    }
}
