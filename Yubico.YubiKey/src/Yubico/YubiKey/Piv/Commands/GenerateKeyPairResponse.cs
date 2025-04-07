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
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// The response to the generate key pair command, containing the public key
    /// of the pair that was generated.
    /// </summary>
    /// <remarks>
    /// This is the partner Response class to <see cref="GenerateKeyPairCommand"/>.
    /// <para>
    /// The data returned by <c>GetData</c> is a <c>PivPublicKey</c> object,
    /// containing the algorithm and encoded public key (described below). If the
    /// generate is successful, the return will actually be an instance of
    /// <c>PivRsaPublicKey</c> or <c>PivEccPublicKey</c>. Each of those objects
    /// contain the specific key data parsed. After getting the key, check the
    /// <c>Algorithm</c> property or use the "is" operation to determine the
    /// actual type.
    /// </para>
    /// <para>
    /// If the property <c>Status</c> is not <c>ResponseStatus.Success</c>, GetData
    /// <c>GetData</c> will throw an exception.
    /// </para>
    /// <para>
    /// If the key is RSA, the encoded key data will be two successive TLVs, the
    /// modulus followed by the public exponent.
    /// </para>
    /// <code>
    ///     81 || length || modulus || 82 || length || publicExponent
    ///     where the length is DER length octets.
    ///     For example:<br/>
    ///     81 82 01 00 F1 50 ... E9 82 03 01 00 01<br/>
    ///     Or to see it parsed,<br/>
    ///     81 82 01 00
    ///        F1 50 ... 50
    ///     82 03
    ///        01 00 01
    /// </code>
    /// <para>
    /// If the public key is an ECC key, the data will be a single TLV, the public
    /// point.
    /// </para>
    /// <code>
    ///     86 || length || publicPoint
    ///     where the length is DER length octets and the public point is 04 || x || y
    ///     For example:<br/>
    ///     86 41 04 C4 17 ... 26<br/>
    ///     Or to see it parsed,<br/>
    ///     86 41
    ///        04 C4 17 ... 26
    /// </code>
    /// <para>
    /// To learn about how to use the public key data, see the User's Manual entry
    /// on <xref href="UsersManualPublicKeys"> public keys</xref>.
    /// </para>
    /// <para>
    /// Example:
    /// </para>
    /// <code language="csharp">
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br/>
    ///   var generateKeyPairCommand = new GenerateKeyPairCommand(
    ///       PivSlot.Signing, PivAlgorithm.EccP384, PivPinPolicy.Default, PivTouchPolicy.Default);
    ///   GenerateKeyPairResponse generateKeyPairResponse =
    ///       connection.SendCommand(generateKeyPairCommand);<br/>
    ///   if (generateKeyPairCommand.Status != ResponseStatus.Success)
    ///   {
    ///     // Handle error
    ///   }
    ///   PivPublicKey pubKey = generateKeyPairResponse.GetData();
    /// </code>
    /// </remarks>
    public class GenerateKeyPairResponse : PivResponse, IYubiKeyResponseWithData<PivPublicKey>
    {
        private byte _slotNumber;
        private PivAlgorithm _algorithm;

        /// <summary>
        /// The slot where the key pair was generated.
        /// </summary>
        /// <value>
        /// The slot number, see <see cref="PivSlot"/>
        /// </value>
        /// <exception cref="ArgumentException">
        /// The slot specified is not one that can generate a key pair.
        /// </exception>
        public byte SlotNumber
        {
            get => _slotNumber;
            set
            {
                if (!PivSlot.IsValidSlotNumberForGenerate(value))
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.InvalidSlot,
                            value));
                }

                _slotNumber = value;
            }
        }

        /// <summary>
        /// The algorithm (and key size) of the key pair.
        /// </summary>
        /// <value>
        /// The algorithm.
        /// </value>
        /// <exception cref="ArgumentException">
        /// The algorithm specified is not a supported asymmetric algorithm.
        /// </exception>
        public PivAlgorithm Algorithm 
        {
            get => _algorithm;
            set
            {
                var keyDefinitionKeyType = value.GetPivKeyDefinition();
                bool supportsKeyGeneration = keyDefinitionKeyType is { SupportsKeyGeneration: true };
                if (!supportsKeyGeneration)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.InvalidAlgorithm));
                }

                _algorithm = value;
            }
        }

        /// <summary>
        /// Constructs a GenerateKeyPairResponse based on a ResponseApdu received from
        /// the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The object containing the response APDU<br/>returned by the YubiKey.
        /// </param>
        /// <param name="slotNumber">
        /// The slot for which the key pair was generated.
        /// </param>
        /// <param name="algorithm">
        /// The algorithm (and key size) of the key pair generated.
        /// </param>
        public GenerateKeyPairResponse(
            ResponseApdu responseApdu,
            byte slotNumber,
            PivAlgorithm algorithm) : base(responseApdu)
        {
            SlotNumber = slotNumber;
            Algorithm = algorithm;
        }
        
        /// <summary>
        /// Constructs a GenerateKeyPairResponse based on a ResponseApdu received from
        /// the YubiKey.
        /// </summary>
        /// <param name="responseApdu">
        /// The object containing the response APDU<br/>returned by the YubiKey.
        /// </param>
        /// <param name="slotNumber">
        /// The slot for which the key pair was generated.
        /// </param>
        public GenerateKeyPairResponse(
            ResponseApdu responseApdu,
            byte slotNumber) : base(responseApdu)
        {
            SlotNumber = slotNumber;
        }

        public Memory<byte> Data => ResponseApdu.Data.ToArray();

        /// <summary>
        /// Gets the public key from the YubiKey response.
        /// </summary>
        /// <remarks>
        /// Note that if there is no data to return, this method will throw an
        /// exception. Even if the response indicates
        /// <c>AuthenticationRequired</c> (see the <c>Status</c> property), which
        /// means the process was not completed because the wrong or no PIN was
        /// entered, or the YubiKey was not touched within the time period. That
        /// is, it is not an error, the process is simply incomplete.
        /// Nonetheless, in that case the method will throw an exception. Hence,
        /// do not call this method unless you know that <c>Status</c> is
        /// <c>Success</c>.
        /// </remarks>
        /// <returns>
        /// The public key as a PivPublicKey (or subclass: PivRsaPublicKey or
        /// PivEccPublicKey) object.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="YubiKeyResponse.Status"/> is not <see cref="ResponseStatus.Success"/>.
        /// </exception>
        public PivPublicKey GetData() =>
            Status switch
            {
#pragma warning disable CS0618 // Type or member is obsolete
                ResponseStatus.Success => PivPublicKey.Create(ResponseApdu.Data, Algorithm),
#pragma warning restore CS0618 // Type or member is obsolete
                _ => throw new InvalidOperationException(StatusMessage),
            };

    }
}
