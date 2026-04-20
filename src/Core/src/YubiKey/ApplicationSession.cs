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
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;

namespace Yubico.YubiKit.Core.YubiKey;

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

    protected async Task InitializeCoreAsync(
        IProtocol protocol,
        FirmwareVersion firmwareVersion,
        ProtocolConfiguration? configuration = null,
        ScpKeyParameters? scpKeyParams = null,
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
            return;

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
    }

    // Major version 0 is a sentinel for alpha/beta firmware whose applets report a
    // placeholder version (e.g. 0.0.1) from their SELECT response. The true firmware
    // version is only available via the Management session. Production firmware always
    // has Major >= 4. On sentinel devices all features are assumed supported.
    public bool IsSupported(Feature feature) =>
        FirmwareVersion.Major == 0 || FirmwareVersion >= feature.Version;

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
        Protocol?.Dispose();
        Protocol = null;
        return ValueTask.CompletedTask;
    }
}


