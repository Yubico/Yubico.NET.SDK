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

using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.SmartCard;

public interface IProtocol : IDisposable
{
    void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null);
}

public interface ISmartCardProtocol : IProtocol
{
    Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        CommandApdu command,
        CancellationToken cancellationToken = default);

    Task<ReadOnlyMemory<byte>> SelectAsync(ReadOnlyMemory<byte> applicationId,
        CancellationToken cancellationToken = default);

    Task<DataEncryptor?> InitScpAsync(ScpKeyParams keyParams, CancellationToken cancellationToken = default);
}

public readonly record struct ProtocolConfiguration
{
    public bool? ForceShortApdus { get; init; }
}

internal class PcscProtocol : ISmartCardProtocol
{
    private const byte INS_SELECT = 0xA4;
    private const byte P1_SELECT = 0x04;
    private const byte P2_SELECT = 0x00;
    private const byte INS_SEND_REMAINING = 0xC0;
    private readonly ISmartCardConnection _connection;
    private readonly byte _insSendRemaining;
    private readonly ILogger<PcscProtocol> _logger;
    private IApduProcessor _processor;
    private bool _useExtendedApdus = true;
    private int MaxApduSize = SmartCardMaxApduSizes.Neo; // Lowest as default


    public PcscProtocol(
        ILogger<PcscProtocol> logger,
        ISmartCardConnection connection,
        ReadOnlyMemory<byte> insSendRemaining = default)
    {
        _logger = logger;
        _connection = connection;
        _insSendRemaining = insSendRemaining.Length > 0 ? insSendRemaining.Span[0] : INS_SEND_REMAINING;
        _processor = BuildBaseProcessor();
    }

    #region ISmartCardProtocol Members

    public void Dispose() => _connection.Dispose();

    public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        CommandApdu command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Transmitting APDU: {CommandApdu}", command);

        var response = await _processor.TransmitAsync(command, true, cancellationToken).ConfigureAwait(false);
        if (!response.IsOK())
            throw new InvalidOperationException(
                $"Command failed with status: {response.SW1:X2}{response.SW2:X2}");

        return response.Data;
    }

    public async Task<ReadOnlyMemory<byte>> SelectAsync(
        ReadOnlyMemory<byte> applicationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Selecting application ID: {ApplicationId}", Convert.ToHexString(applicationId.ToArray()));

        var response =
            await _processor.TransmitAsync(
                new CommandApdu { Ins = INS_SELECT, P1 = P1_SELECT, P2 = P2_SELECT, Data = applicationId },
                false,
                cancellationToken).ConfigureAwait(false);

        if (!response.IsOK())
            throw new InvalidOperationException(
                $"Select command failed with status: {response.SW1:X2}{response.SW2:X2}");

        return response.Data;
    }

    public void Configure(FirmwareVersion firmwareVersion, ProtocolConfiguration? configuration)
    {
        if (firmwareVersion.IsAtLeast(4, 0, 0))
        {
            var forceShortApdu = configuration.HasValue && configuration.Value.ForceShortApdus == true;
            _useExtendedApdus = _connection.SupportsExtendedApdu() && !forceShortApdu;
            MaxApduSize = firmwareVersion.IsAtLeast(4, 3, 0)
                ? SmartCardMaxApduSizes.Yk43
                : SmartCardMaxApduSizes.Yk4;
            ReconfigureProcessor();
        }
    }

    public async Task<DataEncryptor?> InitScpAsync(ScpKeyParams keyParams,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return keyParams switch
            {
                Scp03KeyParams scp03 => await InitScp03Async(scp03, cancellationToken).ConfigureAwait(false),
                Scp11KeyParams scp11 => await InitScp11Async(scp11, cancellationToken).ConfigureAwait(false),
                _ => throw new ArgumentException($"Unsupported ScpKeyParams type: {keyParams.GetType().Name}",
                    nameof(keyParams))
            };
        }
        catch (ApduException ex) when (ex.SW == SWConstants.ClaNotSupported)
        {
            throw new NotSupportedException("SCP is not supported by this YubiKey", ex);
        }
    }

    #endregion

    private IApduProcessor BuildBaseProcessor()
    {
        var processor = _useExtendedApdus
            ? new ExtendedApduProcessor(_connection, new ExtendedApduFormatter(MaxApduSize))
            : new CommandChainingProcessor(_connection, new ShortApduFormatter());

        return new ChainedResponseProcessor(processor, _insSendRemaining);
    }

    private void ReconfigureProcessor()
    {
        var newProcessor = BuildBaseProcessor();
        if (_processor is ScpProcessor scpp)
            // Keep existing SCP state
            newProcessor = new ScpProcessor(newProcessor, scpp.Formatter, scpp.State);

        _processor = newProcessor;
    }

    private async Task<DataEncryptor?> InitScp03Async(Scp03KeyParams keyParams, CancellationToken cancellationToken)
    {
        // Build base processor without SCP
        var baseProcessor = BuildBaseProcessor();

        // Initialize SCP03 session (sends INITIALIZE UPDATE)
        var (state, hostCryptogram) = await ScpState.Scp03InitAsync(
            baseProcessor,
            keyParams,
            null,
            cancellationToken).ConfigureAwait(false);

        // Wrap base processor with SCP
        var scpProcessor = new ScpProcessor(baseProcessor, baseProcessor.Formatter, state);

        // Send EXTERNAL AUTHENTICATE with host cryptogram
        // Security level: 0x33 (C-MAC + C-DECRYPTION + R-MAC + R-ENCRYPTION)
        // Must use useScp=false to bypass SCP for this specific command
        var authCommand = new CommandApdu(0x84, 0x82, 0x33, 0x00, hostCryptogram);
        var authResponse = await scpProcessor.TransmitAsync(authCommand, false, false, cancellationToken)
            .ConfigureAwait(false);

        if (authResponse.SW != SWConstants.Success)
            throw new ApduException($"EXTERNAL AUTHENTICATE failed with SW=0x{authResponse.SW:X4}")
            {
                SW = authResponse.SW
            };

        // Replace the processor with SCP-enabled processor
        _processor = scpProcessor;

        return state.GetDataEncryptor();
    }

    private async Task<DataEncryptor?> InitScp11Async(Scp11KeyParams keyParams, CancellationToken cancellationToken)
    {
        // Build base processor without SCP
        var baseProcessor = BuildBaseProcessor();

        // Initialize SCP11 session (performs ECDH key agreement)
        var state = await ScpState.Scp11InitAsync(
            baseProcessor,
            keyParams,
            cancellationToken).ConfigureAwait(false);

        // Wrap base processor with SCP
        var scpProcessor = new ScpProcessor(baseProcessor, baseProcessor.Formatter, state);

        // Replace the processor with SCP-enabled processor
        _processor = scpProcessor;

        return state.GetDataEncryptor();
    }
}