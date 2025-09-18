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
using System.Globalization;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands;

/// <summary>
///     Perform phase 2 of EC Diffie-Hellman key agreement using the private ECC
///     key in one of the PIV slots.
/// </summary>
/// <remarks>
///     In the PIV standard, there is a command called GENERAL AUTHENTICATE.
///     Although it is one command, it can do four things: authenticate a
///     management key (challenge-response), sign arbitrary data, RSA decryption,
///     and EC Diffie-Hellman. The SDK breaks these four operations into separate
///     classes. This class is how you perform "GENERAL AUTHENTICATE: Key Agree.
///     <para>
///         The partner Response class is <see cref="AuthenticateKeyAgreeResponse" />.
///     </para>
///     <para>
///         Use this Command class only if the slot selected holds an ECC private
///         key. If the private key in a slot called upon to perform this command is
///         RSA, the YubiKey will return an error. The RSA algorithm can encrypt,
///         decrypt, sign, and verify, but it cannot perform the Diffie-Hellman Key
///         Agreement protocol.
///     </para>
///     <para>
///         In order to perform key agreement, it is possible you must verify the
///         PIN. The PIN is not part of this command. For information on how to
///         verify a PIN in order to perform operations, see the User's Manual entry
///         on <xref href="UsersManualPivAccessControl"> PIV commands access control</xref>.
///     </para>
///     <para>
///         The caller supplies the slot to use. Slot <c>9D</c> is the "key
///         management" slot, but any PIV slot that holds a private key, other then
///         <c>F9</c>, will be able to decrypt (as long as it contains an ECC private
///         key). That is, any PIV slot other than <c>80</c>, <c>81</c>, <c>9B</c>,
///         or <c>F9</c> will be able to perform key agreement. Note that slot
///         <c>F9</c> contains the attestation key, which will sign a certificate it
///         creates, but it cannot perform key agreement, even if it is an ECC key.
///     </para>
///     <para>
///         The caller supplies the corresponding party's public key. It must be a
///         block encoded as follows.
///         <code>
///   04 &lt;x-coordinate&gt; &lt;y-coordinate&gt;<br />
///   where each coordinate is the same size as the key.
///   For example, if the slot holds an ECC-P256 key, then each coordinate
///   must be 32 bytes long (256 bits). Prepend 00 bytes if necessary. The
///   total length will be 65 bytes.<br />
///   Note that there is a "compressed" form of a public key, but the YubiKey
///   does not support it. Hence, you must supply the public key as described.
/// </code>
///         This class will copy a reference to the data to decrypt, so you should not
///         clear or alter that input data until this class is done with it, which is
///         after the call to <c>SendCommand</c>.
///     </para>
///     <para>
///         Example:
///     </para>
///     <code language="csharp">
///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br />
///   var keyAgreeCommand = new AuthenticateKeyAgreeCommand(pubKeyData, PivSlot.KeyManagement);
///   AuthenticateDecryptResponse keyAgreeResponse = connection.SendCommand(keyAgreeCommand);<br />
///   if (keyAgreeResponse.Status != ResponseStatus.Success)
///   {
///     // handle error
///   }
///   byte[] sharedSecret = keyAgreeResponse.GetData();
/// </code>
/// </remarks>
public sealed class AuthenticateKeyAgreeCommand : AuthenticateCommand, IYubiKeyCommand<AuthenticateKeyAgreeResponse>
{
    private const int KeyAgreeTag = 0x85;

    private const int EccP256PublicKeySize = 65;
    private const int EccP384PublicKeySize = 97;

    // The default constructor explicitly defined. We don't want it to be
    // used.
    private AuthenticateKeyAgreeCommand()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Initializes a new instance of the AuthenticateKeyAgreeCommand class.
    ///     This command takes the slot number and the corresponding party's
    ///     public key.
    /// </summary>
    /// <remarks>
    ///     The slot number must be for a slot that holds an ECC private key. It
    ///     cannot be <c>F9</c> (the attestation key).
    ///     <para>
    ///         If the key that will be used to perform key agreement is ECC-P256,
    ///         then the correspondent public key data must be 65 bytes long. If the
    ///         key is ECC-P384, then the data must be 97 bytes long. See also the
    ///         User's Manual entry on
    ///         <xref href="UsersManualPivCommands#authenticate-key-agreement">
    ///             key
    ///             agreement
    ///         </xref>
    ///         in the PIV commands page.
    ///     </para>
    /// </remarks>
    /// <param name="correspondentPublicKey">
    ///     The public key that will be used to perform phase 2 of ECDH.
    /// </param>
    /// <param name="slotNumber">
    ///     The slot holding the private key to use.
    /// </param>
    /// <exception cref="ArgumentException">
    ///     The correspondent public value is not the correct length.
    /// </exception>
    [Obsolete("Use the constructor with the algorithm parameter instead.", false)]
    public AuthenticateKeyAgreeCommand(ReadOnlyMemory<byte> correspondentPublicKey, byte slotNumber)
    {
        DataTag = KeyAgreeTag;
        Data = correspondentPublicKey;
        SlotNumber = slotNumber;

        Algorithm = correspondentPublicKey.Length switch
        {
            EccP256PublicKeySize => PivAlgorithm.EccP256,
            EccP384PublicKeySize => PivAlgorithm.EccP384,
            _ => throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.IncorrectEccKeyLength))
        };
    }

    /// <summary>
    ///     Initializes a new instance of the AuthenticateKeyAgreeCommand class.
    ///     This command takes the slot number and the corresponding party's
    ///     public key.
    /// </summary>
    /// <remarks>
    ///     The slot number must be for a slot that holds an ECC private key. It
    ///     cannot be <c>F9</c> (the attestation key).
    ///     <para>
    ///         If the key that will be used to perform key agreement is ECC-P256,
    ///         then the correspondent public key data must be 65 bytes long. If the
    ///         key is ECC-P384, then the data must be 97 bytes long. See also the
    ///         User's Manual entry on
    ///         <xref href="UsersManualPivCommands#authenticate-key-agreement">
    ///             key
    ///             agreement
    ///         </xref>
    ///         in the PIV commands page.
    ///     </para>
    /// </remarks>
    /// <param name="correspondentPublicKey">
    ///     The public key that will be used to perform phase 2 of ECDH.
    /// </param>
    /// <param name="slotNumber">
    ///     The slot holding the private key to use.
    /// </param>
    /// <param name="algorithm"></param>
    /// <exception cref="ArgumentException">
    ///     The correspondent public value is not the correct length.
    /// </exception>
    public AuthenticateKeyAgreeCommand(
        ReadOnlyMemory<byte> correspondentPublicKey,
        byte slotNumber,
        PivAlgorithm algorithm)
    {
        DataTag = KeyAgreeTag;
        Data = correspondentPublicKey;
        SlotNumber = slotNumber;
        Algorithm = algorithm;
    }

    #region IYubiKeyCommand<AuthenticateKeyAgreeResponse> Members

    /// <inheritdoc />
    public AuthenticateKeyAgreeResponse CreateResponseForApdu(ResponseApdu responseApdu) => new(responseApdu);

    #endregion
}
