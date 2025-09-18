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

using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Otp.Commands;

/// <summary>
///     Gets the serial number of the YubiKey.
/// </summary>
public class GetSerialNumberCommand : IYubiKeyCommand<GetSerialNumberResponse>
{
    /// <summary>
    ///     Constructs a new instance of the GetSerialNumberCommand class.
    /// </summary>
    public GetSerialNumberCommand()
    {
    }

    #region IYubiKeyCommand<GetSerialNumberResponse> Members

    /// <summary>
    ///     Gets the YubiKeyApplication to which this command belongs.
    /// </summary>
    /// <value>
    ///     YubiKeyApplication.Otp
    /// </value>
    public YubiKeyApplication Application => YubiKeyApplication.Otp;

    /// <inheritdoc />
    public CommandApdu CreateCommandApdu() =>
        new()
        {
            Ins = OtpConstants.RequestSlotInstruction,
            P1 = OtpConstants.SerialNumberSlot
        };

    /// <inheritdoc />
    public GetSerialNumberResponse CreateResponseForApdu(ResponseApdu responseApdu) => new(responseApdu);

    #endregion
}
