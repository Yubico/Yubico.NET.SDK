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

/// <summary>
///     Provides guarded, application-agnostic APDU exchanges over one SmartCard connection.
/// </summary>
/// <remarks>
///     Creation performs no application selection or applet feature checks. The session borrows a connection
///     passed to <see cref="CreateAsync(ISmartCardConnection,CancellationToken)" />; an
///     <see cref="Abstractions.IYubiKey" /> convenience factory owns its hidden connection. Operations refuse overlap,
///     while direct <see cref="ISmartCardConnection.TransmitAndReceiveAsync" /> calls bypass that guard.
/// </remarks>
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

    // Existing alpha overload family: preserve the simple and SCP-specific raw-session entry points.
#pragma warning disable RS0026
    /// <summary>Creates a raw APDU session that borrows <paramref name="connection" />.</summary>
    /// <remarks>No APDU is transmitted and no application is selected during creation.</remarks>
    public static Task<RawSmartCardSession> CreateAsync(
        ISmartCardConnection connection,
        CancellationToken cancellationToken = default) =>
        CreateCoreAsync(
            connection,
            scpKeyParameters: null,
            new FirmwareVersion(),
            configuration: null,
            cancellationToken);

    /// <summary>Creates a configured raw APDU session and establishes SCP.</summary>
    /// <remarks>
    ///     APDU framing is configured before the secure channel is established. The session borrows
    ///     <paramref name="connection" />. Dispose <paramref name="scpKeyParameters" /> according to its
    ///     ownership contract.
    /// </remarks>
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
#pragma warning restore RS0026

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

        try
        {
            ISmartCardProtocol protocol = ProtocolFactory.Create(connection);
            session.Protocol = protocol;

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

    /// <summary>Selects exactly the supplied application identifier.</summary>
    public Task<ReadOnlyMemory<byte>> SelectAsync(
        ReadOnlyMemory<byte> applicationId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return SmartCardProtocol.SelectAsync(applicationId, cancellationToken);
    }

    /// <summary>Transmits one complete APDU logical exchange.</summary>
    /// <param name="command">The caller-defined APDU.</param>
    /// <param name="throwOnError">
    ///     Whether a non-success status word throws <see cref="ApduException" />. When <see langword="false" />,
    ///     the returned response preserves both data and status bytes.
    /// </param>
    /// <param name="cancellationToken">Cancellation checked before the stateful exchange is admitted.</param>
    public Task<ApduResponse> TransmitAndReceiveAsync(
        ApduCommand command,
        bool throwOnError = true,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return SmartCardProtocol.TransmitAndReceiveAsync(command, throwOnError, cancellationToken);
    }

    /// <summary>Configures APDU formatting for a known device firmware version.</summary>
    /// <remarks>
    ///     This does not apply applet capability or feature gates. SCP sessions must provide configuration during
    ///     creation and cannot be reconfigured after the secure channel is established.
    /// </remarks>
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