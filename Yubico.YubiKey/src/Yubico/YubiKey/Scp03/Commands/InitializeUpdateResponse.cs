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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Scp03.Commands
{
    internal class InitializeUpdateResponse : Scp03Response
    {
        public IReadOnlyCollection<byte> DiversificationData { get; protected set; }
        public IReadOnlyCollection<byte> KeyInfo { get; protected set; }
        public IReadOnlyCollection<byte> CardChallenge { get; protected set; }
        public IReadOnlyCollection<byte> CardCryptogram { get; protected set; }

        /// <summary>
        /// Constructs an InitializeUpdateResponse based on a ResponseApdu received from the YubiKey.
        /// </summary>
        /// <param name="responseApdu">The ResponseApdu that corresponds to the issuance of
        /// this command.</param>
        public InitializeUpdateResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {
            if (responseApdu is null)
            {
                throw new ArgumentNullException(nameof(responseApdu));
            }

            if (responseApdu.Data.Length != 29)
            {
                throw new ArgumentException(
                    ExceptionMessages.IncorrectInitializeUpdateResponseData, nameof(responseApdu));
            }

            ReadOnlySpan<byte> responseData = responseApdu.Data.Span;
            DiversificationData = new ReadOnlyCollection<byte>(responseData[0..10].ToArray());
            KeyInfo = new ReadOnlyCollection<byte>(responseData[10..13].ToArray());
            CardChallenge = new ReadOnlyCollection<byte>(responseData[13..21].ToArray());
            CardCryptogram = new ReadOnlyCollection<byte>(responseData[21..29].ToArray());
        }
    }
}
