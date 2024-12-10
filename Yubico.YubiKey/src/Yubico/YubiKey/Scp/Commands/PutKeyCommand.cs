// Copyright 2024 Yubico AB
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

namespace Yubico.YubiKey.Scp.Commands
{
    /// <summary>
    /// Use this command to put or replace cryptographic keys on the YubiKey in a secure channel session.
    /// The command supports both SCP03 and SCP11 protocols, allowing for the management of symmetric
    /// and asymmetric keys respectively.
    /// </summary>
    /// <remarks>
    /// See the <xref href="UsersManualScp">User's Manual entry</xref> on SCP.
    /// <para>
    /// The command supports multiple key types and operations:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>SCP03 Symmetric Keys:</description>
    /// <list type="bullet">
    /// <item><description>Sets of three 16-byte keys (ENC, MAC, DEK) for secure channel operations</description></item>
    /// <item><description>Identified by a Key Version Number (KVN)</description></item>
    /// <item><description>Keys are encrypted before transmission</description></item>
    /// </list>
    /// </item>
    /// <item>
    /// <description>EC Private Keys (SCP11):</description>
    /// <list type="bullet">
    /// <item><description>NIST P-256 (secp256r1) private keys</description></item>
    /// <item><description>Identified by a Key Reference (version number and ID)</description></item>
    /// <item><description>Key data is encrypted using the session's data encryption key</description></item>
    /// <item><description>Includes checksum verification for integrity</description></item>
    /// </list>
    /// </item>
    /// <item>
    /// <description>EC Public Keys (SCP11):</description>
    /// <list type="bullet">
    /// <item><description>NIST P-256 (secp256r1) public keys</description></item>
    /// <item><description>Includes EC parameters and key data</description></item>
    /// <item><description>Includes checksum verification for integrity</description></item>
    /// </list>
    /// </item>
    /// </list>
    /// <para>
    /// To use this command, a secure channel session must be established. The command will
    /// fail if there is no active secure session or if the provided key parameters don't meet
    /// the required specifications.
    /// </para>
    /// </remarks>
    internal class PutKeyCommand : IYubiKeyCommand<PutKeyResponse>
    {
        private const byte GpPutKeyCla = 0x80;
        private const byte GpPutKeyIns = 0xD8;
        private readonly byte[] _data;
        private readonly byte _p1;
        private readonly byte _p2;

        public YubiKeyApplication Application => YubiKeyApplication.SecurityDomain;

        /// <summary>
        /// This is used internally by the <see cref="SecurityDomainSession"/>. Clients should not have to build this manually.
        /// </summary>
        /// <param name="p1">The P1 parameter for the PutKey Apdu command</param>
        /// <param name="p2">The P2 parameter for the PutKey Apdu command</param>
        /// <param name="data">The data to use for the PutKey Apdu command</param>

        public PutKeyCommand(byte p1, byte p2, ReadOnlyMemory<byte> data)
        {
            _p1 = p1;
            _p2 = p2;
            _data = data.ToArray();
        }

        public CommandApdu CreateCommandApdu() =>
            new CommandApdu
            {
                Cla = GpPutKeyCla,
                Ins = GpPutKeyIns,
                P1 = _p1,
                P2 = _p2,
                Data = _data
            };

        public PutKeyResponse CreateResponseForApdu(ResponseApdu responseApdu) => new PutKeyResponse(responseApdu);
    }
}
