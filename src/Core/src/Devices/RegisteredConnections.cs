// Copyright 2026 Yubico AB
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

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     Transparent connection decorators that release a <see cref="DeviceConnectionRegistry" /> ownership lease
///     exactly once when the wrapped connection is disposed. Pure passthrough otherwise — behavior of the
///     inner connection is unchanged.
/// </summary>
internal sealed class RegisteredSmartCardConnection(
    ISmartCardConnection inner,
    IDisposable registration) : ISmartCardConnection
{
    public ConnectionType Type => inner.Type;

    public Transport Transport => inner.Transport;

    public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        ReadOnlyMemory<byte> command,
        CancellationToken cancellationToken = default) =>
        inner.TransmitAndReceiveAsync(command, cancellationToken);

    public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
        inner.BeginTransaction(cancellationToken);

    public bool SupportsExtendedApdu() => inner.SupportsExtendedApdu();

    public void Dispose()
    {
        try
        {
            inner.Dispose();
        }
        finally
        {
            registration.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await inner.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            registration.Dispose();
        }
    }
}

/// <inheritdoc cref="RegisteredSmartCardConnection" />
internal sealed class RegisteredFidoHidConnection(
    IFidoHidConnection inner,
    IDisposable registration) : IFidoHidConnection
{
    public ConnectionType Type => inner.Type;

    public int PacketSize => inner.PacketSize;

    public Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken = default) =>
        inner.SendAsync(packet, cancellationToken);

    public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
        inner.ReceiveAsync(cancellationToken);

    public void Dispose()
    {
        try
        {
            inner.Dispose();
        }
        finally
        {
            registration.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await inner.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            registration.Dispose();
        }
    }
}

/// <inheritdoc cref="RegisteredSmartCardConnection" />
internal sealed class RegisteredOtpHidConnection(
    IOtpHidConnection inner,
    IDisposable registration) : IOtpHidConnection
{
    public ConnectionType Type => inner.Type;

    public int FeatureReportSize => inner.FeatureReportSize;

    public Task SendAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken = default) =>
        inner.SendAsync(report, cancellationToken);

    public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
        inner.ReceiveAsync(cancellationToken);

    public void Dispose()
    {
        try
        {
            inner.Dispose();
        }
        finally
        {
            registration.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await inner.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            registration.Dispose();
        }
    }
}