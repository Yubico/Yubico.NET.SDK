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

using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.SmartCard.Scp;

/// <summary>
///     Decorator that wraps an ISmartCardProtocol with SCP (Secure Channel Protocol) functionality.
///     All APDU transmissions are encrypted and MACed through the SCP processor.
/// </summary>
public class ScpProtocolAdapter : ISmartCardProtocol
{
    private readonly ISmartCardProtocol _baseProtocol;
    private readonly DataEncryptor _dataEncryptor;
    private readonly IApduProcessor _scpProcessor;

    /// <summary>
    ///     Creates a new SCP protocol adapter.
    /// </summary>
    /// <param name="baseProtocol">The underlying base protocol</param>
    /// <param name="scpProcessor">The SCP-wrapped APDU processor</param>
    /// <param name="dataEncryptor">The data encryptor for this SCP session (may be null)</param>
    public ScpProtocolAdapter(
        ISmartCardProtocol baseProtocol,
        IApduProcessor scpProcessor,
        DataEncryptor dataEncryptor)
    {
        _baseProtocol = baseProtocol;
        _scpProcessor = scpProcessor;
        _dataEncryptor = dataEncryptor;
    }

    /// <summary>
    ///     Gets the data encryptor for this SCP session.
    /// </summary>
    public DataEncryptor GetDataEncryptor() => _dataEncryptor;

    #region ISmartCardProtocol Implementation

    public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        ApduCommand command,
        CancellationToken cancellationToken = default)
    {
        var response = await _scpProcessor.TransmitAsync(command, true, cancellationToken)
            .ConfigureAwait(false);

        return response.IsOK()
            ? response.Data
            : throw ApduException.FromResponse(response, command, "SCP command failed");
    }

    public async Task<ReadOnlyMemory<byte>> SelectAsync(
        ReadOnlyMemory<byte> applicationId,
        CancellationToken cancellationToken = default)
    {
        const byte INS_SELECT = 0xA4;
        const byte P1_SELECT = 0x04;
        const byte P2_SELECT = 0x00;

        var selectCommand = new ApduCommand { Ins = INS_SELECT, P1 = P1_SELECT, P2 = P2_SELECT, Data = applicationId };
        var response = await _scpProcessor.TransmitAsync(selectCommand, false, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsOK())
            throw ApduException.FromResponse(response, selectCommand, "SCP SELECT command failed");

        return response.Data;
    }

    public void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null) =>
        // Delegate configuration to base protocol
        // SCP state is already established and doesn't need reconfiguration
        _baseProtocol.Configure(version, configuration);

    public void Dispose() => _baseProtocol.Dispose();

    #endregion
}