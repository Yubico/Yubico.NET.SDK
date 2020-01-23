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
using System.Linq;
using System.Collections.Generic;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// Set the value of the PIV management key.
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="SetManagementKeyResponse"/>.
    /// <para>
    /// The PIV management key is needed to perform some PIV operations, such as
    /// generating a key pair. See the User's Manual entry on
    /// <xref href="UsersManualPivAccessControl"> PIV commands access control</xref>
    /// for information on when the management key is required.
    /// </para>
    /// <para>
    /// Note that you need to authenticate the current PIV management key before
    /// setting it to a new value with this command.
    /// </para>
    /// <para>
    /// Upon manufacture of a YubiKey, the PIV application begins with a default
    /// management key (see the User's Manual entry on
    /// <xref href="UsersManualPinPukMgmtKey"> the management key</xref>). This
    /// command changes it. Note that this command can be run at any time, either
    /// during initialization, later, to change from the default management key,
    /// or to change it again later on.
    /// </para>
    /// <para>
    /// The management key is a Triple-DES key, so it is 24 byte long, no more,
    /// no less. It is binary. That's 192 bits. But note that because of "parity"
    /// bits, the actual bit strength of a Triple-DES key is 124 bits. And then
    /// further, there are attacks on Triple-DES that leave its effective bit
    /// strength at 112 bits.
    /// </para>
    /// <para>
    /// Along with the key data itself, a management key has a touch policy.
    /// </para>
    /// <para>
    /// Note: touch policy is available only on YubiKey 4 and later. A YubiKey
    /// prior to 4 will ignore the touch policy and simply perform its default.
    /// </para>
    /// <para>
    /// The touch policy refers to whether use of the managment key will require
    /// touch or not, and if so, always or cached. The policy is specified using
    /// the <c>PivTouchPolicy</c> enum. If the input is <c>None</c> or
    /// <c>Never</c>, the YubiKey will not require touch to complete an operation
    /// that requires the management key. <c>Always</c> means every operation
    /// requires touch, even if the YubiKey had been touched for an operation
    /// shortly before. If <c>Cached</c>, one touch will last for 15 seconds.
    /// That is, touch for an operation, and if a second operation requires the
    /// management key, and it is executing less than 15 seconds after the first,
    /// touch is not required. <c>Default</c> will use the YubiKey's default
    /// touch policy.
    /// </para>
    /// <para>
    /// When you pass the new management key to this class, it will copy a
    /// reference to the object passed in, it will not copy the value. Because of
    /// this, you cannot overwrite the key data until this object is done with
    /// it. It will be safe to overwrite the key data after calling
    /// <c>connection.SendCommand</c>. See the User's Manual
    /// <xref href="UsersManualSensitive"> entry on sensitive data</xref> for
    /// more information on this topic.
    /// </para>
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    ///   /* This example assumes the application has a method to collect a
    ///    * management key.
    ///    */
    ///   using System.Security.Cryptography;<br/>
    ///   byte[] mgmtKey;<br/>
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br/>
    ///   mgmtKey = CollectMgmtKey();
    ///   var setManagementKeyCommand =
    ///       new SetManagementKeyCommand(PivTouchPolicy.Never, mgmtKey);
    ///   SetManagementKeyResponse setManagementKeyResponse =
    ///        connection.SendCommand(setManagementKeyCommand);<br/>
    ///   if (setManagementKeyResponse != ResponseStatus.Success)
    ///   {
    ///     // Handle error
    ///   }
    ///
    ///   CryptographicOperations.ZeroMemory(mgmtKey);
    /// </code>
    /// </remarks>
    public sealed class SetManagementKeyCommand : IYubiKeyCommand<SetManagementKeyResponse>
    {
        private const byte PivSetManagementKeyInstruction = 0xFF;

        private const byte TouchPolicyP2Never = 0xFF;
        private const byte TouchPolicyP2Always = 0xFE;
        private const byte TouchPolicyP2Cached = 0xFD;

        private const int PrefixLength = 3;
        private const int ManagementKeyLength = 24;
        private static readonly byte[] EncodedKeyPrefix = new byte[] { 0x03, 0x9B, 0x18 };

        private readonly ReadOnlyMemory<byte> _newKey;

        /// <summary>
        /// The touch policy the key will have. None and Default are equivalent
        /// to Never.
        /// </summary>
        public PivTouchPolicy TouchPolicy { get; set; }

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs. For this
        /// command it's PIV.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.Piv
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Piv;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        // Note that there is an object-initializer constructor, but it cannot be
        // no-arg because one of the constructor args is a secret byte array.
        private SetManagementKeyCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initializes a new instance of the <c>SetManagementKeyCommand</c> class.
        /// This command takes the new management key as input and will set the
        /// <c>TouchPolicy</c> to the default.
        /// </summary>
        /// <remarks>
        /// This constructor is provided for those developers who want to use the
        /// object initializer pattern. For example:
        /// <code>
        ///   var command = new SetManagementKeyCommand(keyData)
        ///   {
        ///       TouchPolicy = PivTouchPolicy.Cached,
        ///   };
        /// </code>
        /// <para>
        /// Note that you need to authenticate the current PIV management key before
        /// setting it to a new value with this command.
        /// </para>
        /// </remarks>
        /// <param name="newKey">
        /// The bytes that make up the new management key.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The Triple-DES key is an invalid length.
        /// </exception>
        public SetManagementKeyCommand(ReadOnlyMemory<byte> newKey)
        {
            if (newKey.Length != ManagementKeyLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncorrectTripleDesKeyLength));
            }

            TouchPolicy = PivTouchPolicy.Default;
            _newKey = newKey;
        }

        /// <summary>
        /// Initializes a new instance of the SetManagementKeyCommand class. This
        /// command takes the touch policy and the new management key as input.
        /// </summary>
        /// <remarks>
        /// Note that a <c>touchPolicy</c> of <c>PivTouchPolicy.Default</c> or
        /// <c>None</c> is equivalent to <c>Never</c>.
        /// <para>
        /// Note also that you need to authenticate the current PIV management
        /// key before setting it to a new value with this command.
        /// </para>
        /// </remarks>
        /// <param name="touchPolicy">
        /// The touch policy for the management key.
        /// </param>
        /// <param name="newKey">
        /// The bytes that make up the new management key.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The Triple-DES key is an invalid length.
        /// </exception>
        public SetManagementKeyCommand(ReadOnlyMemory<byte> newKey, PivTouchPolicy touchPolicy)
        {
            if (newKey.Length != ManagementKeyLength)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.IncorrectTripleDesKeyLength));
            }

            TouchPolicy = touchPolicy;
            _newKey = newKey;
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => new CommandApdu
        {
            Ins = PivSetManagementKeyInstruction,
            P1 = PivSetManagementKeyInstruction,
            P2 = TouchPolicy switch
            {
                PivTouchPolicy.Always => TouchPolicyP2Always,
                PivTouchPolicy.Cached => TouchPolicyP2Cached,
                _ => TouchPolicyP2Never,
            },
            Data = BuildSetManagementKeyApduData(),
        };

        // Build a byte array that contains the data portion of the APDU.
        // It should be prefix || new key.
        private byte[] BuildSetManagementKeyApduData()
        {
            byte[] returnValue = new byte[PrefixLength + ManagementKeyLength];
            Array.Copy(EncodedKeyPrefix, returnValue, PrefixLength);
            _newKey.CopyTo(returnValue.AsMemory(PrefixLength, ManagementKeyLength));

            return returnValue;
        }

        /// <inheritdoc />
        public SetManagementKeyResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
          new SetManagementKeyResponse(responseApdu);
    }
}
