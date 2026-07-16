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

    protected async Task<IProtocol> InitializeProtocolAsync(
        IProtocol protocol,
        FirmwareVersion firmwareVersion,
        ProtocolConfiguration? configuration = null,
        ScpKeyParameters? scpKeyParams = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(protocol);

        if (IsInitialized)
            return Protocol ?? protocol;

        protocol.Configure(firmwareVersion, configuration);

        IProtocol effectiveProtocol = protocol;
        var isAuthenticated = false;

        if (scpKeyParams is not null)
        {
            if (effectiveProtocol is not PcscProtocol pcscProtocol)
            {
                throw new NotSupportedException(
                    "SCP is only supported on PC/SC SmartCard protocols created by Core.");
            }

            effectiveProtocol = await pcscProtocol
                .InitializeScpAsync(scpKeyParams, cancellationToken)
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

    /// <summary>
    ///     Releases resources owned by a session whose asynchronous factory failed. Disposal failures
    ///     are logged and suppressed so the initialization exception remains the primary failure.
    /// </summary>
    protected void DisposeAfterInitializationFailure()
    {
        try
        {
            Dispose();
        }
        catch (Exception ex)
        {
            try
            {
                Logger.LogWarning(ex, "Failed to dispose session resources after initialization failed");
            }
            catch
            {
                // Initialization is already failing. Logging must not replace that original exception.
            }
        }
    }

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

        _disposed = true;

        if (disposing)
        {
            var protocol = Protocol;
            Protocol = null;
            protocol?.Dispose();
        }
    }

    protected virtual ValueTask DisposeAsyncCore()
    {
        if (_disposed)
            return ValueTask.CompletedTask;

        _disposed = true;
        var protocol = Protocol;
        Protocol = null;
        protocol?.Dispose();
        return ValueTask.CompletedTask;
    }
}