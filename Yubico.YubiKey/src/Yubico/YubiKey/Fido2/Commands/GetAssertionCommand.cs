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

using System;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Fido2.Commands;

/// <summary>
///     Instruct the YubiKey to get an assertion based on the input parameters.
/// </summary>
public class GetAssertionCommand : IYubiKeyCommand<GetAssertionResponse>
{
    private readonly GetAssertionParameters _params;

    // The default constructor explicitly defined. We don't want it to be
    // used.
    // Note that there is no object-initializer constructor. All the
    // constructor inputs have no default or are secret byte arrays.
    private GetAssertionCommand()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Constructs an instance of the <see cref="GetAssertionCommand" />
    ///     class using the given parameters.
    /// </summary>
    /// <remarks>
    ///     This class will copy a reference to the input parameters object. It
    ///     will no longer need it after the call to <c>SendCommand</c>.
    /// </remarks>
    /// <param name="getAssertionParameters">
    ///     An object containing all the parameters the YubiKey will use to get
    ///     an assertion.
    /// </param>
    public GetAssertionCommand(GetAssertionParameters getAssertionParameters)
    {
        _params = getAssertionParameters;
    }

    #region IYubiKeyCommand<GetAssertionResponse> Members

    /// <inheritdoc />
    public YubiKeyApplication Application => YubiKeyApplication.Fido2;

    /// <inheritdoc />
    public CommandApdu CreateCommandApdu()
    {
        byte[] encodedParams = _params.CborEncode();
        byte[] payload = new byte[encodedParams.Length + 1];
        payload[0] = CtapConstants.CtapGetAssertionCmd;
        Array.Copy(encodedParams, 0, payload, 1, encodedParams.Length);
        return new CommandApdu
        {
            Ins = CtapConstants.CtapHidCbor,
            Data = payload
        };
    }

    /// <inheritdoc />
    public GetAssertionResponse CreateResponseForApdu(ResponseApdu responseApdu) => new(responseApdu);

    #endregion
}
