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
    ///     Generate a new asymmetric key pair.
    /// </summary>
    /// <remarks>
    ///     The partner Response class is <see cref="GenerateKeyPairResponse" />.
    ///     <para>
    ///         In order to generate a key pair, you must authenticate the management
    ///         key. The management key is not part of this command. For information on
    ///         how to authenticate a management key in order to perform operations, see
    ///         the User's Manual entry on
    ///         <xref href="UsersManualPivAccessControl"> PIV commands access control</xref>.
    ///     </para>
    ///     <para>
    ///         When you generate a key pair, you specify which slot will hold this new
    ///         key. If there is a key in that slot already, this command will replace
    ///         it. That old key will be gone and there will be nothing you can do to
    ///         recover it. Hence, use this command with caution.
    ///     </para>
    ///     <para>
    ///         Note that this command will generate a key pair, and from the Response
    ///         class you can retrieve the public key. However, you will still need to
    ///         obtain a certificate for this private key outside of this SDK. Once you
    ///         have the certificate, you can load it into the YubiKey using the Put Data
    ///         command.
    ///     </para>
    ///     <para>
    ///         The PIN policy determines whether using the private key to sign or
    ///         decrypt will require authenticating with the PIN or not. By default, the
    ///         PIN policy is always require a PIN in order to use the key in that slot.
    ///         See the User's Manual entry on
    ///         <xref href="UsersManualPivPinTouchPolicy"> PIN and touch policies </xref>
    ///         for more information.
    ///     </para>
    ///     <para>
    ///         Similarly, the touch policy determines whether using the private key will
    ///         require touch or not. The default is never.
    ///     </para>
    ///     <para>
    ///         Example:
    ///     </para>
    ///     <code language="csharp">
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br />
    ///   var generateKeyPairCommand = new GenerateKeyPairCommand(
    ///       PivSlot.Signing, PivAlgorithm.EccP384, PivPinPolicy.Default, PivTouchPolicy.Default);
    ///   GenerateKeyPairResponse generateKeyPairResponse =
    ///       connection.SendCommand(generateKeyPairCommand);<br />
    ///   if (generateKeyPairCommand.Status != ResponseStatus.Success)
    ///   {
    ///     // Handle error
    ///   }
    ///   PivPublicKey pubKey = generateKeyPairResponse.GetData();
    /// </code>
    /// </remarks>
    public sealed class GenerateKeyPairCommand : IYubiKeyCommand<GenerateKeyPairResponse>
    {
        private const byte PivGenerateKeyPairInstruction = 0x47;

        // The Data portion of the command APDU is
        //  algorithm || PIN policy || touch policy
        //  AC 09 80 01 alg || AA 01 pPolicy || AB 01 tPolicy
        // However, if PIN or touch policy is default, don't write anything. So
        // the Data might be
        //  algorithm || PIN policy || touch policy
        //  algorithm || PIN policy
        //  algorithm || touch policy
        //  algorithm
        // Create one List with all three elements, then remove the PIN and/or
        // touch policy bytes if one or both are default.
        // These are the indices where the actual values go.
        private const int IndexValueLength = 1;
        private const int AlgorithmCount = 3;
        private const int IndexAlgorithmByte = 4;
        private const int IndexPinPolicy = 5;
        private const int IndexPinPolicyByte = 7;
        private const int PinPolicyCount = 3;
        private const int IndexTouchPolicy = 8;
        private const int IndexTouchPolicyByte = 10;
        private const int TouchPolicyCount = 3;
        private PivAlgorithm _algorithm;

        // These are needed so we can make the check on the set of the property.
        private byte _slotNumber;

        /// <summary>
        ///     Initializes a new instance of the GenerateKeyPairCommand class. This command
        ///     takes the slot number, algorithm, and PIN and touch policies as input.
        /// </summary>
        /// <remarks>
        ///     The slot number must be for a slot that holds an asymmetric key. See
        ///     the User's Manual
        ///     <xref href="UsersManualPivSlots"> entry on PIV slots </xref> and
        ///     <see cref="PivSlot" />.
        ///     <para>
        ///         Note that the <c>algorithm</c> argument is of type
        ///         <see cref="PivAlgorithm" />,
        ///         which includes <c>None</c>, <c>TripleDes</c>, and <c>Pin</c>.
        ///         However, the only allowed values for this command are <c>Rsa1024</c>,
        ///         <c>Rsa2048</c>, <c>EccP256</c>, and <c>EccP384</c>.
        ///     </para>
        ///     <para>
        ///         Both the touch policy and pin policy <c>enum</c> arguments have
        ///         <c>None</c> as a possible value. This command, will treat a policy of
        ///         <c>None</c> the same as <c>Default</c>.
        ///     </para>
        /// </remarks>
        /// <param name="slotNumber">
        ///     The slot which will hold the private key.
        /// </param>
        /// <param name="algorithm">
        ///     The algorithm (and size) of the key to generate.
        /// </param>
        /// <param name="pinPolicy">
        ///     The PIN policy the key will have.
        /// </param>
        /// <param name="touchPolicy">
        ///     The touch policy the key will have.
        /// </param>
        public GenerateKeyPairCommand(
            byte slotNumber,
            PivAlgorithm algorithm,
            PivPinPolicy pinPolicy,
            PivTouchPolicy touchPolicy)
        {
            SlotNumber = slotNumber;
            Algorithm = algorithm;
            PinPolicy = pinPolicy;
            TouchPolicy = touchPolicy;
        }

        /// <summary>
        ///     Initializes a new instance of the <c>GenerateKeyPairCommand</c> class.
        ///     This command will set the <c>PinPolicy</c> and <c>TouchPolicy</c>
        ///     to the defaults.
        /// </summary>
        /// <remarks>
        ///     This constructor is provided for those developers who want to use the
        ///     object initializer pattern. For example:
        ///     <code language="csharp">
        ///   var command = new GenerateKeyPairCommand()
        ///   {
        ///       SlotNumber = PivSlot.Authentication,
        ///       Algorithm = PivAlgorithm.Rsa2048,
        ///       PinPolicy = PivPinPolicy.Once,
        ///   };
        /// </code>
        ///     <para>
        ///         There is no default slot number or algorithm, hence, for this command
        ///         to be valid, the slot number and algorithm must be specified. So if
        ///         you create an object using this constructor, you must set the
        ///         SlotNumber and Algorithm properties at some time before using it.
        ///         Otherwise you will get an exception when you do use it.
        ///     </para>
        /// </remarks>
        public GenerateKeyPairCommand()
        {
            _slotNumber = 0;
            _algorithm = PivAlgorithm.None;
            PinPolicy = PivPinPolicy.Default;
            TouchPolicy = PivTouchPolicy.Default;
        }

        /// <summary>
        ///     The slot for which a key pair will be generated.
        /// </summary>
        /// <value>
        ///     The slot number, see <see cref="PivSlot" />
        /// </value>
        /// <exception cref="ArgumentException">
        ///     The slot specified is not valid for public key operations.
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
        ///     The algorithm (and size) of the key to generate.
        /// </summary>
        /// <value>
        ///     The algorithm.
        /// </value>
        /// <exception cref="ArgumentException">
        ///     The algorithm specified is not valid for key pair generation.
        /// </exception>
        public PivAlgorithm Algorithm
        {
            get => _algorithm;
            set
            {
                if (value.IsValidAlgorithmForGenerate() == false)
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
        ///     The PIN policy the key will have. None is equivalent to Default.
        /// </summary>
        /// <value>
        ///     The PIN policy.
        /// </value>
        public PivPinPolicy PinPolicy { get; set; }

        /// <summary>
        ///     The touch policy the key will have. None is equivalent to Default.
        /// </summary>
        /// <value>
        ///     The touch policy.
        /// </value>
        public PivTouchPolicy TouchPolicy { get; set; }

        /// <summary>
        ///     Gets the YubiKeyApplication to which this command belongs. For this
        ///     command it's PIV.
        /// </summary>
        /// <value>
        ///     YubiKeyApplication.Piv
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Piv;

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() =>
            new CommandApdu
            {
                Ins = PivGenerateKeyPairInstruction,
                P2 = SlotNumber,
                Data = BuildGenerateKeyPairApduData()
            };

        /// <inheritdoc />
        public GenerateKeyPairResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new GenerateKeyPairResponse(responseApdu, SlotNumber, Algorithm);

        // Build a byte array that contains the data portion of the APDU.
        // This will build a list that does or does not contain the PIN and
        // touch policy elements depending on whether they are default or not.
        private byte[] BuildGenerateKeyPairApduData()
        {
            if (PivSlot.IsValidSlotNumberForGenerate(_slotNumber) == false)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidSlot,
                        _slotNumber));
            }

            if (_algorithm.IsValidAlgorithmForGenerate() == false)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidAlgorithm));
            }

            byte[] data =
            {
                0xAC, 0x09, 0x80, 0x01, 0x00, 0xAA, 0x01, 0x00, 0xAB, 0x01, 0x00
            };

            data[IndexAlgorithmByte] = (byte)Algorithm;
            data[IndexTouchPolicyByte] = (byte)TouchPolicy;
            data[IndexPinPolicyByte] = (byte)PinPolicy;
            int length = data.Length;
            int valueLength = AlgorithmCount + PinPolicyCount + TouchPolicyCount;

            if (PinPolicy == PivPinPolicy.Default || PinPolicy == PivPinPolicy.None)
            {
                Array.Copy(data, IndexTouchPolicy, data, IndexPinPolicy, TouchPolicyCount);
                length -= PinPolicyCount;
                valueLength -= PinPolicyCount;
            }

            if (TouchPolicy == PivTouchPolicy.Default || TouchPolicy == PivTouchPolicy.None)
            {
                length -= TouchPolicyCount;
                valueLength -= TouchPolicyCount;
            }

            data[IndexValueLength] = (byte)valueLength;
            Span<byte> returnValue = data.AsSpan(start: 0, length);
            return returnValue.ToArray();
        }
    }
}
