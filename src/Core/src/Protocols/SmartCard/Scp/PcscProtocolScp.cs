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

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Core.Protocols.SmartCard.Scp;

/// <summary>
///     Decorator that wraps an ISmartCardProtocol with SCP (Secure Channel Protocol) functionality.
///     All APDU transmissions are encrypted and MACed through the SCP processor. Safe for concurrent
///     calls: exchanges are serialized on the SAME gate as the wrapped protocol (SCP MAC chaining makes
///     interleaving doubly fatal — each MAC depends on the previous command's MAC).
/// </summary>
/// <remarks>
///     <para>
///         <c>ISmartCardProtocol.WithScpAsync</c> (see <see cref="ScpExtensions" />) is the only supported way to
///         obtain an instance. The constructor is internal: an SCP wrapper must adopt the exchange gate of the concrete
///         <see cref="PcscProtocol" /> it decorates, because the SCP processor chain drives that protocol's
///         connection directly instead of going through its public methods. Any other construction path could
///         hand the wrapper a foreign gate, letting plain and encrypted traffic interleave on the wire.
///         <c>WithScpAsync</c> owns that pairing — it establishes the SCP session on the base protocol's gate and
///         then wraps the same protocol instance.
///     </para>
/// </remarks>
public sealed class PcscProtocolScp : ISmartCardProtocol
{
    private readonly ISmartCardProtocol _baseProtocol;
    private readonly DataEncryptor _dataEncryptor;
    private readonly AsyncExchangeGate _exchangeGate;
    private readonly IApduProcessor _scpProcessor;
    private bool _disposed;

    /// <summary>
    ///     Creates a new SCP protocol adapter. Internal by design — see the type-level remarks:
    ///     <c>ISmartCardProtocol.WithScpAsync</c> is the only supported construction path, because it is what
    ///     guarantees the wrapper shares the exchange gate of the protocol whose connection the SCP processor drives.
    /// </summary>
    /// <param name="baseProtocol">The underlying base protocol; must be a concrete <see cref="PcscProtocol" /></param>
    /// <param name="scpProcessor">The SCP-wrapped APDU processor</param>
    /// <param name="dataEncryptor">The data encryptor for this SCP session (may be null)</param>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="baseProtocol" /> is not a <see cref="PcscProtocol" />, since no shared gate
    ///     could be adopted.
    /// </exception>
    internal PcscProtocolScp(
        ISmartCardProtocol baseProtocol,
        IApduProcessor scpProcessor,
        DataEncryptor dataEncryptor)
    {
        ArgumentNullException.ThrowIfNull(baseProtocol);
        if (baseProtocol is not PcscProtocol pcscProtocol)
        {
            throw new ArgumentException(
                $"SCP requires a {nameof(PcscProtocol)} base so encrypted and plain exchanges share one gate.",
                nameof(baseProtocol));
        }

        _baseProtocol = baseProtocol;
        _scpProcessor = scpProcessor;
        _dataEncryptor = dataEncryptor;

        // The SCP processor chain bypasses the base protocol's public methods and drives the same
        // connection directly, so exchanges MUST share the base protocol's gate — otherwise gated
        // plain traffic and SCP traffic could interleave on the wire.
        _exchangeGate = pcscProtocol.ExchangeGate;
    }

    /// <summary>
    ///     Gets the data encryptor for this SCP session.
    /// </summary>
    public DataEncryptor GetDataEncryptor() => _dataEncryptor;


    public async Task<ApduResponse> TransmitAndReceiveAsync(
        ApduCommand command,
        bool throwOnError = true,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var response = await _exchangeGate.RunExclusiveAsync(
                exchangeToken => _scpProcessor.TransmitAsync(command, true, exchangeToken),
                cancellationToken)
            .ConfigureAwait(false);

        if (throwOnError && !response.IsOK())
        {
            throw ApduException.FromResponse(response, command, "SCP command failed");
        }

        return response;
    }

    public async Task<ReadOnlyMemory<byte>> SelectAsync(
        ReadOnlyMemory<byte> applicationId,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        const byte INS_SELECT = 0xA4;
        const byte P1_SELECT = 0x04;
        const byte P2_SELECT = 0x00;

        var selectCommand = new ApduCommand { Ins = INS_SELECT, P1 = P1_SELECT, P2 = P2_SELECT, Data = applicationId };
        var response = await _exchangeGate.RunExclusiveAsync(
                exchangeToken => _scpProcessor.TransmitAsync(selectCommand, false, exchangeToken),
                cancellationToken)
            .ConfigureAwait(false);

        return response.IsOK()
            ? response.Data
            : throw ApduException.FromResponse(response, selectCommand, "SCP SELECT command failed");
    }

    public void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Delegate configuration to base protocol
        // SCP state is already established and doesn't need reconfiguration
        _baseProtocol.Configure(version, configuration);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        (_scpProcessor as IDisposable)?.Dispose();
        _baseProtocol.Dispose();
        _disposed = true;
    }

}