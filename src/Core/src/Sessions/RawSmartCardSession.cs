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
    private ISmartCardProtocol SmartCardProtocol =>
        (ISmartCardProtocol)(Protocol ?? throw new ObjectDisposedException(nameof(RawSmartCardSession)));

    private RawSmartCardSession(ISmartCardConnection connection)
        : base(connection)
    {
    }

    public static Task<RawSmartCardSession> CreateAsync(
        ISmartCardConnection connection,
        CancellationToken cancellationToken = default) =>
        CreateAsync(connection, scpKeyParameters: null, cancellationToken);

    public static async Task<RawSmartCardSession> CreateAsync(
        ISmartCardConnection connection,
        ScpKeyParameters? scpKeyParameters,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RawSmartCardSession session = Construct(connection, () => new RawSmartCardSession(connection));
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
                        new FirmwareVersion(),
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
        // A plain protocol configures directly. An SCP wrapper forwards this to its base protocol without
        // rebuilding secure-channel state. SCP establishment deliberately accepts unknown firmware so a raw
        // caller can configure framing later when the device version is known.
        SmartCardProtocol.Configure(firmwareVersion, configuration);
        FirmwareVersion = firmwareVersion;
    }
}