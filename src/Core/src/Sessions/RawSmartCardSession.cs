// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using Yubico.YubiKit.Core.Protocols;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.Sessions;

public sealed class RawSmartCardSession : ApplicationSession
{
    private readonly bool _usesScp;
    private ISmartCardProtocol SmartCardProtocol =>
        (ISmartCardProtocol)(Protocol ?? throw new ObjectDisposedException(nameof(RawSmartCardSession)));

    private RawSmartCardSession(ISmartCardConnection connection, bool usesScp)
        : base(connection)
    {
        _usesScp = usesScp;
    }

    public static Task<RawSmartCardSession> CreateAsync(
        ISmartCardConnection connection,
        CancellationToken cancellationToken = default) =>
        CreateCoreAsync(
            connection,
            scpKeyParameters: null,
            new FirmwareVersion(),
            configuration: null,
            cancellationToken);

    public static Task<RawSmartCardSession> CreateAsync(
        ISmartCardConnection connection,
        ScpKeyParameters scpKeyParameters,
        FirmwareVersion firmwareVersion,
        ProtocolConfiguration? configuration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scpKeyParameters);
        ArgumentNullException.ThrowIfNull(firmwareVersion);
        return CreateCoreAsync(
            connection,
            scpKeyParameters,
            firmwareVersion,
            configuration,
            cancellationToken);
    }

    private static async Task<RawSmartCardSession> CreateCoreAsync(
        ISmartCardConnection connection,
        ScpKeyParameters? scpKeyParameters,
        FirmwareVersion firmwareVersion,
        ProtocolConfiguration? configuration,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RawSmartCardSession session = Construct(
            connection,
            () => new RawSmartCardSession(connection, usesScp: scpKeyParameters is not null));
        ISmartCardProtocol protocol = ProtocolFactory.Create(connection);
        session.Protocol = protocol;

        try
        {
            if (scpKeyParameters is null)
            {
                session.IsInitialized = true;
            }
            else
            {
                await session.InitializeProtocolAsync(
                        protocol,
                        firmwareVersion,
                        configuration,
                        scpKeyParams: scpKeyParameters,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }

            return session;
        }
        catch
        {
            // ApplicationSession only disposes a connection after OwnConnection transfers ownership.
            // Direct creation borrows this connection; the IYubiKey extension owns and cleans up its hidden one.
            session.DisposeAfterInitializationFailure();
            throw;
        }
    }

    public Task<ReadOnlyMemory<byte>> SelectAsync(
        ReadOnlyMemory<byte> applicationId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return SmartCardProtocol.SelectAsync(applicationId, cancellationToken);
    }

    public Task<ApduResponse> TransmitAndReceiveAsync(
        ApduCommand command,
        bool throwOnError = true,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return SmartCardProtocol.TransmitAndReceiveAsync(command, throwOnError, cancellationToken);
    }

    public void Configure(
        FirmwareVersion firmwareVersion,
        ProtocolConfiguration? configuration = null)
    {
        ThrowIfDisposed();
        if (_usesScp)
        {
            throw new InvalidOperationException(
                "An SCP raw session must be configured during creation, before SCP is established.");
        }

        SmartCardProtocol.Configure(firmwareVersion, configuration);
        FirmwareVersion = firmwareVersion;
    }
}