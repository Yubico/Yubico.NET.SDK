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
    /// Import an existing private key into one of the asymmetric key slots (9a,
    /// 9c, 9d, 9e, 82 - 95).
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="ImportAsymmetricKeyResponse"/>.
    /// <para>
    /// In order to import a key, you must authenticate the management key. The
    /// management key is not part of this command. For information on how to
    /// authenticate a management key in order to perform operations, see the
    /// User's Manual entry on
    /// <xref href="UsersManualPivAccessControl"> PIV commands access control</xref>.
    /// </para>
    /// <para>
    /// When you import a private key, you specify which slot will hold this key.
    /// If there is a key in that slot already, this command will replace it.
    /// That old key will be gone and there will be nothing you can do to recover
    /// it. Hence, use this command with caution.
    /// </para>
    /// <para>
    /// If you have a certificate to accompany the private key you are importing
    /// using this command, you can load it using the Put Data command.
    /// </para>
    /// <para>
    /// The PIN policy determines whether using the private key to sign or
    /// decrypt will require authenticating with the PIN or not. By default, the
    /// PIN policy is always require a PIN in order to use the key in that slot.
    /// See the User's Manual entry on
    /// <xref href="UsersManualPivPinTouchPolicy"> PIN and touch policies </xref>
    /// for more information.
    /// </para>
    /// <para>
    /// Similarly, the touch policy determines whether using the private key will
    /// require touch or not. The default is never.
    /// </para>
    /// <para>
    /// When you pass the private key to this class, it will copy a reference to
    /// the object passed in, it will not copy the value. Because of this, you
    /// cannot call its <c>Clear</c> method until this object is done with it. It
    /// will be safe to clear the private key after calling
    /// <c>connection.SendCommand</c>. See the User's Manual
    /// <xref href="UsersManualSensitive"> entry on sensitive data</xref> for
    /// more information on this topic.
    /// </para>
    /// <para>
    /// Example:
    /// </para>
    /// <code language="csharp">
    ///   var privateKey = new PivEccPrivateKey(privateValue);
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br/>
    ///   var importKeyCommand = new ImportAsymmetricKeyCommand(
    ///       privateKey, PivSlot.Signing, PivPinPolicy.Default, PivTouchPolicy.Default);
    ///   ImportAsymmetricKeyResponse importAsymmetricKeyResponse =
    ///       connection.SendCommand(importAsymmetricKeyCommand);<br/>
    ///   if (importAsymmetricKeyResponse.Status != ResponseStatus.Success)
    ///   {
    ///       // Handle error
    ///   }
    ///   privateKey.Clear();
    /// </code>
    /// </remarks>
    public sealed class ImportAsymmetricKeyCommand : IYubiKeyCommand<ImportAsymmetricKeyResponse>
    {
        private const byte PivImportAsymmetricInstruction = 0xFE;

        // The Data portion of the command APDU is
        //  keyData || PIN policy || touch policy
        // However, if PIN or touch policy is default, don't write anything. So
        // the Data might be
        //  keyData || PIN policy || touch policy
        //  keyData || PIN policy
        //  keyData || touch policy
        //  keyData
        private const int PinPolicyIndex = 2;
        private const int PinPolicyLength = 3;
        private const int TouchPolicyIndex = 5;
        private const int TouchPolicyLength = 3;

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs. For this
        /// command it's PIV.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.Piv
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Piv;

        private byte _slotNumber;

        /// <summary>
        /// The slot into which the key will be imported.
        /// </summary>
        /// <value>
        /// The slot number, see <see cref="PivSlot"/>
        /// </value>
        /// <exception cref="ArgumentException">
        /// The slot specified is not valid for public key operations.
        /// </exception>
        public byte SlotNumber
        {
            get => _slotNumber;
            set
            {
                if (PivSlot.IsValidSlotNumberForGenerate(value) == false)
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
        /// The algorithm (and size) of the key being imported.
        /// </summary>
        /// <value>
        /// The algorithm.
        /// </value>
        public PivAlgorithm Algorithm { get; }

        /// <summary>
        /// The PIN policy the key will have. None is equivalent to Default.
        /// </summary>
        /// <value>
        /// The PIN policy flag.
        /// </value>
        public PivPinPolicy PinPolicy { get; set; }

        /// <summary>
        /// The touch policy the key will have. None is equivalent to Default.
        /// </summary>
        /// <value>
        /// The touch policy flag.
        /// </value>
        public PivTouchPolicy TouchPolicy { get; set; }

        private readonly byte[] _policy =
        [
            0xAA, 0x01, 0x00, 0xAB, 0x01, 0x00
        ];

        private readonly ReadOnlyMemory<byte> _encodedKey;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private ImportAsymmetricKeyCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initializes a new instance of the <c>ImportAsymmetricKeyCommand</c> class.
        /// This command takes the slot number, PIN and touch policies, and the
        /// private key as input.
        /// </summary>
        /// <remarks>
        /// The only possible private keys this command will accept are RSA-1024,
        /// RSA-2048, RSA-3072, RSA-4096, ECC-P256, and ECC-P384. If you supply any other private
        /// key, the constructor will throw an exception.
        /// <para>
        /// The slot number must be for a slot that holds an asymmetric key. See
        /// the User's Manual
        /// <xref href="UsersManualPivSlots"> entry on PIV slots </xref> and
        /// <see cref="PivSlot"/>.
        /// </para>
        /// <para>
        /// Both the touch policy and pin policy <c>enum</c> arguments have
        /// <c>None</c> as a possible value. This command will treat a policy of
        /// <c>None</c> the same as <c>Default</c>.
        /// </para>
        /// <para>
        /// The key data must be supplied as an instance of <see cref="PivPrivateKey"/>
        /// </para>
        /// </remarks>
        /// <param name="privateKey">
        /// The private key to import.
        /// </param>
        /// <param name="slotNumber">
        /// The slot which will hold the private key.
        /// </param>
        /// <param name="pinPolicy">
        /// The PIN policy the key will have.
        /// </param>
        /// <param name="touchPolicy">
        /// The touch policy the key will have.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>privateKey</c> argument is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The <c>privateKey</c> argument does not contain a key.
        /// </exception>
        [Obsolete("Usage of PivEccPublic/PivEccPrivateKey is deprecated. Use IPublicKey, IPrivateKey, ECPublicKey or ECPrivateKeyParameters instead")]
        public ImportAsymmetricKeyCommand(
            PivPrivateKey privateKey,
            byte slotNumber,
            PivPinPolicy pinPolicy,
            PivTouchPolicy touchPolicy)
        {
            if (privateKey is null)
            {
                throw new ArgumentNullException(nameof(privateKey));
            }

            if (privateKey.EncodedPrivateKey.IsEmpty)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPrivateKeyData));
            }
            
            _encodedKey = privateKey.EncodedPrivateKey;
            Algorithm = privateKey.Algorithm;
            SlotNumber = slotNumber;
            PinPolicy = pinPolicy;
            TouchPolicy = touchPolicy;
        }

        /// <summary>
        /// Initializes a new instance of the <c>ImportAsymmetricKeyCommand</c> class.
        /// This command takes the private key as input, it will set the
        /// <c>PinPolicy</c> and <c>TouchPolicy</c> to the defaults.
        /// </summary>
        /// <remarks>
        /// This constructor is provided for those developers who want to use the
        /// object initializer pattern. For example:
        /// <code language="csharp">
        ///   var command = new ImportAsymmetricKeyCommand(privateKey)
        ///   {
        ///       SlotNumber = PivSlots.Authentication,
        ///       PinPolicy = PivPinPolicy.Once,
        ///   };
        /// </code>
        /// <para>
        /// There is no default slot number, hence, for this command to be valid,
        /// the slot number must be specified. So if you create an object using
        /// this constructor, you must set the SlotNumber property at some time
        /// before using it. Otherwise you will get an exception when you do use
        /// it.
        /// </para>
        /// </remarks>
        /// <param name="privateKey">
        /// The private key to import.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>privateKey</c> argument is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The <c>privateKey</c> argument does not contain a key.
        /// </exception>
        [Obsolete("Usage of PivEccPublic/PivEccPrivateKey is deprecated. Use IPublicKey, IPrivateKey, ECPublicKey or ECPrivateKeyParameters instead")]
        public ImportAsymmetricKeyCommand(PivPrivateKey privateKey)
        {
            if (privateKey is null)
            {
                throw new ArgumentNullException(nameof(privateKey));
            }

            if (privateKey.EncodedPrivateKey.IsEmpty)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPrivateKeyData));
            }

            _slotNumber = 0;
            _encodedKey = privateKey.EncodedPrivateKey;

            Algorithm = privateKey.Algorithm;
            PinPolicy = PivPinPolicy.Default;
            TouchPolicy = PivTouchPolicy.Default;
        }

        public ImportAsymmetricKeyCommand(
            ReadOnlyMemory<byte> encodedKey,
            KeyType keyType,
            byte slotNumber,
            PivPinPolicy pinPolicy = PivPinPolicy.Default,
            PivTouchPolicy touchPolicy = PivTouchPolicy.Default)
        {
            _encodedKey = encodedKey;
            SlotNumber = slotNumber;
            Algorithm = keyType.GetPivAlgorithm();
            PinPolicy = pinPolicy;
            TouchPolicy = touchPolicy;
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() =>
            new CommandApdu
            {
                Ins = PivImportAsymmetricInstruction,
                P1 = (byte)Algorithm,
                P2 = SlotNumber,
                Data = BuildImportAsymmetricApduData(),
            };

        // Build a new byte array containing the key data and policy data.
        // Build it in such a way that there will be no "reallocation".
        private byte[] BuildImportAsymmetricApduData()
        {
            if (!PivSlot.IsValidSlotNumberForGenerate(_slotNumber))
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidSlot,
                        _slotNumber));
            }

            int policyLength = 0;
            _policy[PinPolicyIndex] = (byte)PinPolicy;
            _policy[TouchPolicyIndex] = (byte)TouchPolicy;

            bool includeTouchPolicy = TouchPolicy != PivTouchPolicy.Default && TouchPolicy != PivTouchPolicy.None;
            if (includeTouchPolicy)
            {
                policyLength += TouchPolicyLength;
            }

            bool includePinPolicy = PinPolicy != PivPinPolicy.Default && PinPolicy != PivPinPolicy.None;
            if (includePinPolicy)
            {
                policyLength += PinPolicyLength;
            }

            int apduTotalLength = _encodedKey.Length + policyLength;
            byte[] result = new byte[apduTotalLength];
            Span<byte> resultSpan = result;

            // Copy private key data
            _encodedKey.Span.CopyTo(resultSpan);

            // Write policy data
            var policyDestination = resultSpan[_encodedKey.Length..];

            if (includePinPolicy)
            {
                _policy.AsSpan(0, PinPolicyLength).CopyTo(policyDestination);
                policyDestination = policyDestination[PinPolicyLength..];
            }

            if (includeTouchPolicy)
            {
                _policy.AsSpan(PinPolicyLength, TouchPolicyLength).CopyTo(policyDestination);
            }

            return result;
        }

        /// <inheritdoc />
        public ImportAsymmetricKeyResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new ImportAsymmetricKeyResponse(responseApdu);
    }
}
