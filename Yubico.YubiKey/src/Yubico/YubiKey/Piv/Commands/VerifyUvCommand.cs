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
        private const byte SlotOccAuth = 0x96;

        private readonly bool _requestTemporaryPin;
        private readonly bool _checkOnly;


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
        // Note that there is no object-initializer constructor. the only
        // constructor input is a secret byte arrays.
        private VerifyUvCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initializes a new instance of the VerifyUvCommand class.
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
        /// The PIN is an invalid length.
        /// </exception>
        public VerifyUvCommand(bool requestTemporaryPin, bool checkOnly)
        {
            if (requestTemporaryPin && checkOnly)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidVerifyUvArguments
                        ));
            }
            _requestTemporaryPin = requestTemporaryPin;
            _checkOnly = checkOnly;
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            ReadOnlyMemory<byte> data = ReadOnlyMemory<byte>.Empty;
            if (!_checkOnly) {
                const byte GetTemporaryPinTag = 0x02;
                const byte VerifyUvTag = 0x03;

                var tlvWriter = new TlvWriter();
                if (_requestTemporaryPin) {
                    tlvWriter.WriteValue(GetTemporaryPinTag, null);
                } else {
                    tlvWriter.WriteValue(VerifyUvTag, null);
                }
                data = tlvWriter.Encode();
            }
            return new CommandApdu
            {
                Ins = PivVerifyInstruction,
                P2 = SlotOccAuth,
                Data = data,
            };
        }

        /// <inheritdoc />
        public VerifyUvResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
          new VerifyUvResponse(responseApdu, _requestTemporaryPin);
    }
}
