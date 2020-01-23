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
    /// Reads the status information maintained by the OTP application. This is sometimes necessary
    /// in order to determine whether a configuration update was successful or not.
    /// </summary>
    public class ReadStatusCommand : IYubiKeyCommand<ReadStatusResponse>
    {
        /// <summary>
        /// Gets the YubiKey Application to which this command belongs.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.Otp
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Otp;

        /// <summary>
        /// Constructs a new instance of the ReadStatusCommand class.
        /// </summary>
        public ReadStatusCommand()
        {

        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => new CommandApdu
        {
            Ins = OtpConstants.ReadStatusInstruction
        };

        /// <inheritdoc />
        public ReadStatusResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new ReadStatusResponse(responseApdu);
    }
}
