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

using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     Connection decorators that share one disposal attempt and outcome through <see cref="DisposalGate" />.
///     SmartCard disposal releases the registration after successful inner disposal; failure retains and
///     quarantines it. The FIDO and OTP compatibility wrappers release it after the attempt even when inner
///     disposal throws. Every sync or async caller observes the shared outcome, rather than a new attempt.
///     Inner disposal completion is not independent proof that concurrent native calls have drained.
///     Built-in Windows/macOS/Linux HID connections instead transfer the registration directly to their native owner.
/// </summary>
internal sealed class RegisteredSmartCardConnection(
    ISmartCardConnection inner,
    IDisposable registration) : ISmartCardConnection
{
    private readonly DisposalGate _disposal = new(
        registration,
        releaseLeaseOnFailure: false,
        failure => DeviceConnectionRegistry.TryMarkUnrecovered(registration, failure));

    public ConnectionType Type => inner.Type;

    public Transport Transport => inner.Transport;
    public bool IsExclusive => inner.IsExclusive;

    public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        ReadOnlyMemory<byte> command,
        CancellationToken cancellationToken = default) =>
        inner.TransmitAndReceiveAsync(command, cancellationToken);

    public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
        inner.BeginTransaction(cancellationToken);

    public Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        inner.BeginTransactionAsync(cancellationToken);

    public bool SupportsExtendedApdu() => inner.SupportsExtendedApdu();

    public void Dispose() => _disposal.Dispose(inner.Dispose);

    public ValueTask DisposeAsync() => _disposal.DisposeAsync(inner.DisposeAsync);
}

/// <inheritdoc cref="RegisteredSmartCardConnection" />
internal sealed class RegisteredFidoHidConnection(
    IFidoHidConnection inner,
    IDisposable registration) : IFidoHidConnection, ITerminalWakeControl
{
    private readonly DisposalGate _disposal = new(registration);

    public void RequestTerminalWake()
    {
        if (inner is ITerminalWakeControl wake) wake.RequestTerminalWake();
    }

    public ConnectionType Type => inner.Type;

    public int PacketSize => inner.PacketSize;

    public Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken = default) =>
        inner.SendAsync(packet, cancellationToken);

    public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
        inner.ReceiveAsync(cancellationToken);

    public void Dispose() => _disposal.Dispose(inner.Dispose);

    public ValueTask DisposeAsync() => _disposal.DisposeAsync(inner.DisposeAsync);
}

/// <inheritdoc cref="RegisteredSmartCardConnection" />
internal sealed class RegisteredOtpHidConnection(
    IOtpHidConnection inner,
    IDisposable registration) : IOtpHidConnection
{
    private readonly DisposalGate _disposal = new(registration);

    public ConnectionType Type => inner.Type;

    public int FeatureReportSize => inner.FeatureReportSize;

    public Task SendAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken = default) =>
        inner.SendAsync(report, cancellationToken);

    public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
        inner.ReceiveAsync(cancellationToken);

    public void Dispose() => _disposal.Dispose(inner.Dispose);

    public ValueTask DisposeAsync() => _disposal.DisposeAsync(inner.DisposeAsync);
}