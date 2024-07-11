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
using System.Security.Cryptography;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Scp03.Commands
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
    internal class DeleteKeyCommand : IYubiKeyCommand<Scp03Response>
    {
        private const byte GpDeleteKeyCla = 0x84;
        private const byte GpDeleteKeyIns = 0xE4;
        private const byte GpDeleteKeyP1 = 0;
        private const byte GpDeleteKeyP2 = 0;
        private const byte GpDeleteLastKeyP2 = 1;
        private const byte KvnTag = 0xD2;
        private const byte KvnLength = 1;

        private readonly byte[] _data;
        private readonly byte _p2Value;

        public YubiKeyApplication Application => YubiKeyApplication.InterIndustry;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private DeleteKeyCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create a new instance of the command. When this command is executed,
        /// the key set represented by the given <c>keyVersionNumber</c> will be
        /// deleted. If there is no such key set, this command will do nothing
        /// and the return <c>Status</c> will be <c>ResponseStatus.NoData</c>.
        /// </summary>
        /// <remarks>
        /// The key set used to make the connection cannot be the key set to be
        /// deleted, unless both of the other key sets have been deleted, and you
        /// pass <c>true</c> for <c>isLastKey</c>. In this case, the key will be
        /// deleted but the SCP03 application on the YubiKey will be reset with
        /// the default key.
        /// </remarks>
        public DeleteKeyCommand(byte keyVersionNumber, bool isLastKey)
        {
            _data = new byte[3] { KvnTag, KvnLength, keyVersionNumber };
            _p2Value = isLastKey
                ? GpDeleteLastKeyP2
                : GpDeleteKeyP2;
        }

        public CommandApdu CreateCommandApdu() =>
            new CommandApdu()
            {
                Cla = GpDeleteKeyCla,
                Ins = GpDeleteKeyIns,
                P1 = GpDeleteKeyP1,
                P2 = _p2Value,
                Data = _data
            };

        public Scp03Response CreateResponseForApdu(ResponseApdu responseApdu) => new Scp03Response(responseApdu);
    }
}
