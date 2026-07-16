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
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;

namespace Yubico.YubiKit.Core.Sessions;

public abstract class ApplicationSession : IApplicationSession, IAsyncDisposable
{
    private bool _disposed;

    protected ILogger Logger { get; }
    protected IProtocol? Protocol { get; set; }

    public FirmwareVersion FirmwareVersion { get; protected set; } = new();
    public bool IsInitialized { get; protected set; }
    public bool IsAuthenticated { get; protected set; }

    protected ApplicationSession()
    {
        Logger = YubiKitLogging.CreateLogger(GetType().FullName ?? GetType().Name);
    }

    protected async Task<TProtocol> InitializeCoreAsync<TProtocol>(
        TProtocol protocol,
        FirmwareVersion firmwareVersion,
        ProtocolConfiguration? configuration = null,
        ScpKeyParameters? scpKeyParams = null,
        CancellationToken cancellationToken = default)
        where TProtocol : IProtocol
    {
        ArgumentNullException.ThrowIfNull(protocol);

        var effectiveProtocol = await InitializeProtocolCoreAsync(
                UnwrapProtocol(protocol),
                firmwareVersion,
                configuration,
                scpKeyParams,
                cancellationToken)
            .ConfigureAwait(false);

        return RebindProtocol(protocol, effectiveProtocol);
    }

    private async Task<IProtocol> InitializeProtocolCoreAsync(
        IProtocol protocol,
        FirmwareVersion firmwareVersion,
        ProtocolConfiguration? configuration,
        ScpKeyParameters? scpKeyParams,
        CancellationToken cancellationToken)
    {
        if (IsInitialized)
            return Protocol ?? protocol;

        ArgumentNullException.ThrowIfNull(protocol);

        protocol.Configure(firmwareVersion, configuration);

        IProtocol effectiveProtocol = protocol;
        var isAuthenticated = false;

        if (scpKeyParams is not null)
        {
            if (effectiveProtocol is not ISmartCardProtocol smartCardProtocol)
                throw new NotSupportedException("SCP is only supported on SmartCard protocols.");

            effectiveProtocol = await smartCardProtocol
                .WithScpAsync(scpKeyParams, cancellationToken)
                .ConfigureAwait(false);

            isAuthenticated = true;
        }

        // Only mutate session state on successful completion.
        Protocol = effectiveProtocol;
        FirmwareVersion = firmwareVersion;
        IsAuthenticated = isAuthenticated;
        IsInitialized = true;

        return effectiveProtocol;
    }

    private static IProtocol UnwrapProtocol(IProtocol protocol) =>
        protocol is YubiKeyProtocol binding ? binding.Inner : protocol;

    private static TProtocol RebindProtocol<TProtocol>(TProtocol protocol, IProtocol effectiveProtocol)
        where TProtocol : IProtocol =>
        protocol is YubiKeyProtocol binding
            ? (TProtocol)(object)binding.Rebind(effectiveProtocol)
            : (TProtocol)effectiveProtocol;

    public bool IsSupported(Feature feature) =>
        feature.IsSupportedByFirmware(FirmwareVersion);

    public void EnsureSupports(Feature feature)
    {
        if (!IsSupported(feature))
            throw new NotSupportedException($"{feature.Name} requires firmware {feature.Version}+");
    }

    protected void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            Protocol?.Dispose();
            Protocol = null;
        }

        _disposed = true;
    }

    protected virtual ValueTask DisposeAsyncCore()
    {
        if (_disposed)
            return ValueTask.CompletedTask;

        Protocol?.Dispose();
        Protocol = null;
        return ValueTask.CompletedTask;
    }
}