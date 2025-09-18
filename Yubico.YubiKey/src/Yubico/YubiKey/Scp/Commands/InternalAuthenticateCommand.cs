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

namespace Yubico.YubiKey.Scp.Commands;

/// <summary>
///     Represents the second command in the SCP03 and SCP11a/c authentication handshakes, 'INTERNAL_AUTHENTICATE'
/// </summary>
/// <remarks>
///     Clients should not generally build this manually. See <see cref="Pipelines.ScpApduTransform" /> for more.
/// </remarks>
internal class InternalAuthenticateCommand : IYubiKeyCommand<InternalAuthenticateResponse>
{
    internal const byte GpInternalAuthenticateIns = 0x88;
    private readonly byte[] _data;
    private readonly byte _keyReferenceId;
    private readonly byte _keyReferenceVersionNumber;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InternalAuthenticateCommand" /> class.
    /// </summary>
    /// <param name="keyReferenceVersionNumber">The version number of the key reference.</param>
    /// <param name="keyReferenceId">The ID of the key reference.</param>
    /// <param name="data">The data to be used for internal authentication.</param>
    public InternalAuthenticateCommand(byte keyReferenceVersionNumber, byte keyReferenceId, byte[] data)
    {
        _keyReferenceVersionNumber = keyReferenceVersionNumber;
        _keyReferenceId = keyReferenceId;
        _data = data;
    }

    #region IYubiKeyCommand<InternalAuthenticateResponse> Members

    public YubiKeyApplication Application => YubiKeyApplication.SecurityDomain;

    public CommandApdu CreateCommandApdu() =>
        new()
        {
            Cla = 0x80,
            Ins = GpInternalAuthenticateIns,
            P1 = _keyReferenceVersionNumber,
            P2 = _keyReferenceId,
            Data = _data
        };

    public InternalAuthenticateResponse CreateResponseForApdu(ResponseApdu responseApdu) => new(responseApdu);

    #endregion
}

internal class InternalAuthenticateResponse : ScpResponse
{
    /// <summary>
    ///     Creates a new <see cref="InternalAuthenticateResponse" /> from the provided <see cref="ResponseApdu" />.
    /// </summary>
    /// <param name="responseApdu">The <see cref="ResponseApdu" /> to create the response from.</param>
    /// <returns>A new <see cref="InternalAuthenticateResponse" />.</returns>
    public InternalAuthenticateResponse(ResponseApdu responseApdu) : base(responseApdu)
    {
    }
}
