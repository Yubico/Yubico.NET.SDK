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

/// <summary>
///     Implements the GlobalPlatform STORE DATA command for transferring data to the Security Domain or Applications.
/// </summary>
/// <remarks>
///     This is an internal implementation of the STORE DATA command. For storing data in the Security Domain,
///     it is recommended to use the methods provided by <see cref="SecurityDomainSession" /> instead, such as
///     <see cref="SecurityDomainSession.StoreData" />, <see cref="SecurityDomainSession.StoreCaIssuer" />,
///     <see cref="SecurityDomainSession.StoreCertificates" />, and <see cref="SecurityDomainSession.StoreAllowlist" />.
///     This command supports single block transfer with BER-TLV formatted data according to ISO 8825.
/// </remarks>
internal class StoreDataCommand : IYubiKeyCommand<StoreDataCommandResponse>
{
    private const byte GpStoreDataIns = 0xE2;
    private readonly ReadOnlyMemory<byte> _data;

    /// <summary>
    ///     Initializes a new instance of the <see cref="StoreDataCommand" /> class, with the given data to be stored.
    /// </summary>
    /// <remarks>
    ///     This command supports single block transfer with BER-TLV formatted data according to ISO 8825.
    ///     <para>
    ///         For storing data in the Security Domain,
    ///         it is recommended to use the methods provided by <see cref="SecurityDomainSession" /> instead, such as
    ///         <see cref="SecurityDomainSession.StoreData" />, <see cref="SecurityDomainSession.StoreCaIssuer" />,
    ///         <see cref="SecurityDomainSession.StoreCertificates" />, and <see cref="SecurityDomainSession.StoreAllowlist" />
    ///         .
    ///     </para>
    /// </remarks>
    /// <param name="data">The data to store, which must be formatted as BER-TLV structures according to ISO 8825.</param>
    public StoreDataCommand(ReadOnlyMemory<byte> data)
    {
        _data = data;
    }

    // The default constructor explicitly defined. We don't want it to be
    // used.
    private StoreDataCommand()
    {
        throw new NotImplementedException();
    }

    #region IYubiKeyCommand<StoreDataCommandResponse> Members

    public YubiKeyApplication Application => YubiKeyApplication.SecurityDomain;

    public CommandApdu CreateCommandApdu() =>
        new()
        {
            Cla = 0,
            Ins = GpStoreDataIns,
            P1 = 0x90,
            P2 = 0x00,
            Data = _data
        };

    public StoreDataCommandResponse CreateResponseForApdu(ResponseApdu responseApdu) => new(responseApdu);

    #endregion
}

internal class StoreDataCommandResponse : ScpResponse, IYubiKeyResponseWithData<ReadOnlyMemory<byte>>
{
    public StoreDataCommandResponse(ResponseApdu responseApdu) : base(responseApdu)
    {
    }

    #region IYubiKeyResponseWithData<ReadOnlyMemory<byte>> Members

    public ReadOnlyMemory<byte> GetData() => ResponseApdu.Data;

    #endregion
}
