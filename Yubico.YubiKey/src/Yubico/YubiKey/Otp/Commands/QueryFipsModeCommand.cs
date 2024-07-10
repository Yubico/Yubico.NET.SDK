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

using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Otp.Commands
{
    /// <summary>
    ///     Determines if the YubiKey supports FIPS, and if so, what the status of it is.
    /// </summary>
    public class QueryFipsModeCommand : IYubiKeyCommand<QueryFipsModeResponse>
    {
        /// <summary>
        ///     Constructs a new instance of the QueryFipsModeCommand class.
        /// </summary>
        public QueryFipsModeCommand()
        {
        }

        public YubiKeyApplication Application => YubiKeyApplication.Otp;

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() =>
            new CommandApdu
            {
                Ins = OtpConstants.RequestSlotInstruction,
                P1 = OtpConstants.QueryFipsSlot
            };

        /// <inheritdoc />
        public QueryFipsModeResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new QueryFipsModeResponse(responseApdu);
    }
}
