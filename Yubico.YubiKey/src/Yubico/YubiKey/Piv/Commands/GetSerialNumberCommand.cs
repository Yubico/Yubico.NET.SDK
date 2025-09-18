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

namespace Yubico.YubiKey.Piv.Commands;

/// <summary>
///     Get the YubiKey's serial number.
/// </summary>
/// <remarks>
///     The partner Response class is <see cref="GetSerialNumberResponse" />.
///     <para>
///         Example:
///     </para>
///     <code language="csharp">
///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);
///   SerialCommand serialCommand = new GetSerialNumberCommand();
///   GetSerialNumberResponse serialResponse = connection.SendCommand(serialCommand);<br />
///   if (serialResponse.Status == ResponseStatus.Success)
///   {
///       int serialNum = serialResponse.GetData();
///   }
/// </code>
/// </remarks>
public sealed class GetSerialNumberCommand : IYubiKeyCommand<GetSerialNumberResponse>
{
    private const byte PivGetSerialNumberInstruction = 0xF8;

    /// <summary>
    ///     Initializes a new instance of the GetSerialNumberCommand class. This
    ///     command has no input.
    /// </summary>
    public GetSerialNumberCommand()
    {
    }

    #region IYubiKeyCommand<GetSerialNumberResponse> Members

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
        new()
        {
            Ins = PivGetSerialNumberInstruction
        };

    /// <inheritdoc />
    public GetSerialNumberResponse CreateResponseForApdu(ResponseApdu responseApdu) => new(responseApdu);

    #endregion
}
