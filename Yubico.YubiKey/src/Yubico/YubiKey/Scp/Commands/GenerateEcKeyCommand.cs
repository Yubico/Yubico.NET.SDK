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

namespace Yubico.YubiKey.Scp.Commands;

internal class GenerateEcKeyCommand : IYubiKeyCommand<GenerateEcKeyResponse>
{
    internal const byte GpGenerateKeyIns = 0xF1;

    private readonly ReadOnlyMemory<byte> _data;
    private readonly byte _keyId;
    private readonly byte _keyVersionNumber;

    public GenerateEcKeyCommand(byte keyVersionNumber, byte keyId, ReadOnlyMemory<byte> data)
    {
        _data = data;
        _keyVersionNumber = keyVersionNumber;
        _keyId = keyId;
    }

    #region IYubiKeyCommand<GenerateEcKeyResponse> Members

    public YubiKeyApplication Application => YubiKeyApplication.SecurityDomain;

    public CommandApdu CreateCommandApdu() =>
        new()
        {
            Cla = 0x80,
            Ins = GpGenerateKeyIns,
            P1 = _keyVersionNumber,
            P2 = _keyId,
            Data = _data
        };

    public GenerateEcKeyResponse CreateResponseForApdu(ResponseApdu responseApdu) => new(responseApdu);

    #endregion
}

internal class GenerateEcKeyResponse : ScpResponse, IYubiKeyResponseWithData<ReadOnlyMemory<byte>>
{
    public GenerateEcKeyResponse(ResponseApdu responseApdu) : base(responseApdu)
    {
    }

    #region IYubiKeyResponseWithData<ReadOnlyMemory<byte>> Members

    public ReadOnlyMemory<byte> GetData() => ResponseApdu.Data;

    #endregion
}
