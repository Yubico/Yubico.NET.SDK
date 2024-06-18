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
using System.Globalization;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// Get information about Bio multi-protocol key.
    /// </summary>
    /// <remarks>
    /// The Get Bio Metadata command is available on YubiKey Bio multi-protocol.
    /// <para>
    /// The partner Response class is <see cref="GetBioMetadataResponse"/>.
    /// </para>
    /// <para>
    /// See the User's Manual
    /// <xref href="UsersManualPivCommands#get-bio-metadata"> entry on getting bio metadata</xref>
    /// for specific information about what information is returned. 
    /// </para>
    /// <para>
    /// Example:
    /// </para>
    /// <code language="csharp">
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br/>
    ///   GetBioMetadataCommand bioMetadataCommand = new GetBioMetadataCommand();
    ///   GetBioMetadataResponse bioMetadataResponse = connection.SendCommand(bioMetadataCommand);<br/>
    ///   if (bioMetadataResponse.Status == ResponseStatus.Success)
    ///   {
    ///       PivBioMetadata bioMetadata = bioMetadataResponse.GetData();
    ///   }
    /// </code>
    /// </remarks>
    public sealed class GetBioMetadataCommand : IYubiKeyCommand<GetBioMetadataResponse>
    {
        private const byte PivMetadataInstruction = 0xF7;

        private const byte SlotOccAuth = 0x96;

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs. For this
        /// command it's PIV.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.Piv
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Piv;

        /// <summary>
        /// Initializes a new instance of the GetBioMetadataCommand class.
        /// </summary>
        public GetBioMetadataCommand()
        {
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu()
        {
            return new CommandApdu
            {
                Ins = PivMetadataInstruction,
                P2 = SlotOccAuth,
            };
        }

        /// <inheritdoc />
        public GetBioMetadataResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new GetBioMetadataResponse(responseApdu);
    }
}
