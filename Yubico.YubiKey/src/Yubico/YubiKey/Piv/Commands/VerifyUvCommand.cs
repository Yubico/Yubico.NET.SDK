// Copyright 2024 Yubico AB
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
using System.Collections.Generic;
using System.Globalization;
using Yubico.Core.Iso7816;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// Authenticate with YubiKey Bio multi-protocol capabilities.
    /// </summary>
    /// <remarks>
    /// The partner Response class is <see cref="VerifyUvResponse"/>.
    /// <para>
    /// Before calling this method, clients must verify that the authenticator is bio-capable and
    /// not blocked for bio matching.
    /// </para>
    /// </remarks>
    public sealed class VerifyUvCommand : IYubiKeyCommand<VerifyUvResponse>
    {
        private const byte PivVerifyInstruction = 0x20;
        private const byte OnCardComparisonAuthenticationSlot = 0x96;

        public bool RequestTemporaryPin { get; set; }
        public bool CheckOnly { get; set; }

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs. For this
        /// command it's PIV.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.Piv
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Piv;

        /// <summary>
        /// Initializes a new instance of the <c>VerifyUvCommand</c> class.
        /// </summary>
        /// <remarks>
        /// This constructor is provided for those developers who want to use the
        /// object initializer pattern. For example:
        /// <code language="csharp">
        ///   var command = new VerifyUvCommand()
        ///   {
        ///       CheckOnly = true;
        ///   };
        /// </code>
        /// <para>
        /// </para>
        /// </remarks>
        public VerifyUvCommand()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>VerifyUvCommand</c> class.
        /// </summary>
        /// <param name="requestTemporaryPin">
        /// After successful match generate a temporary PIN. Certain conditions may 
        /// lead to the clearing of the temporary PIN, such as fingerprint mismatch, 
        /// PIV PIN failed verification, timeout, power loss, failed attempt to verify 
        /// against the set value.
        /// </param>
        /// <param name="checkOnly">
        /// Check verification state of biometrics, don't perform UV.
        /// </param>        
        /// <exception cref="ArgumentException">
        /// The PIN is an invalid length or requestTemporaryPin and checkOnly are both
        /// set to true.
        /// </exception>
        public VerifyUvCommand(bool requestTemporaryPin, bool checkOnly)
        {
            ValidateParameters(requestTemporaryPin, checkOnly);

            RequestTemporaryPin = requestTemporaryPin;
            CheckOnly = checkOnly;
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            ValidateParameters(RequestTemporaryPin, CheckOnly);

            if (CheckOnly)
            {
                return new CommandApdu
                {
                    Ins = PivVerifyInstruction,
                    P2 = OnCardComparisonAuthenticationSlot,
                };
            }

            var tlvWriter = new TlvWriter();
            const byte getTemporaryPinTag = 0x02;
            if (RequestTemporaryPin)
            {
                tlvWriter.WriteValue(getTemporaryPinTag, null);
            }
            else
            {
                const byte VerifyUvTag = 0x03;
                tlvWriter.WriteValue(VerifyUvTag, null);
            }

            ReadOnlyMemory<byte> data = tlvWriter.Encode();
            return new CommandApdu
            {
                Ins = PivVerifyInstruction,
                P2 = OnCardComparisonAuthenticationSlot,
                Data = data
            };
        }

        private static void ValidateParameters(bool requestTemporaryPin, bool checkOnly)
        {
            if (requestTemporaryPin && checkOnly)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidVerifyUvArguments
                        ));
            }
        }

        /// <inheritdoc />
        public VerifyUvResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
          new VerifyUvResponse(responseApdu, RequestTemporaryPin);
    }
}
