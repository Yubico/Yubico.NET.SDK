// Copyright 2023 Yubico AB
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
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Scp.Commands
{
    /// <summary>
    /// Use this command to delete one of the SCP03 key sets on the YubiKey.
    /// </summary>
    /// <remarks>
    /// See the <xref href="UsersManualScp03">User's Manual entry</xref> on SCP03.
    /// <para>
    /// This will execute the Delete Command. That is, there is a general purpose
    /// command that can delete various elements, including keys. However, this
    /// class can build the general purpose delete command in a way that it will
    /// only be able to delete keys.
    /// </para>
    /// <para>
    /// Note that if all three key sets are deleted, then the first key set (the
    /// key set with a KeyVersionNumber of 1) will be the default key set.
    /// </para>
    /// </remarks>
    internal class GetDataCommand : IYubiKeyCommand<GetDataCommandResponse>
    {
        private const byte INS_GET_DATA = 0xCA;
        private readonly int _tag;
        private readonly ReadOnlyMemory<byte> _data;

        public YubiKeyApplication Application => YubiKeyApplication.InterIndustry;

        public GetDataCommand(int tag, ReadOnlyMemory<byte>? data = null)
        {
            _tag = tag;
            _data = data ?? ReadOnlyMemory<byte>.Empty;
        }

        public CommandApdu CreateCommandApdu() => new CommandApdu
        {
            Cla = 0,
            Ins = INS_GET_DATA,
            P1 = (byte)(_tag >> 8),
            P2 = (byte)(_tag & 0xFF),
            Data = _data
        };

        public GetDataCommandResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new GetDataCommandResponse(responseApdu);
    }

    internal class GetDataCommandResponse : ScpResponse, IYubiKeyResponseWithData<ReadOnlyMemory<byte>>
    {
        public GetDataCommandResponse(ResponseApdu responseApdu) : base(responseApdu)
        {
        }

        public ReadOnlyMemory<byte> GetData() => ResponseApdu.Data;
    }
}
