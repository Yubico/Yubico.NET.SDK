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
///     exactly once when the wrapped connection is disposed. Disposal runs through a <see cref="DisposalGate" />,
///     so the inner connection is torn down exactly once, the lease is released only afterwards, and every
///     disposal call — sync or async, concurrent or repeated — returns only once teardown has finished. Pure
///     passthrough otherwise — behavior of the inner connection is unchanged.
/// </summary>
internal sealed class RegisteredSmartCardConnection : ISmartCardConnection
{
    private readonly DisposalGate _disposal;
    private readonly ISmartCardConnection _inner;

    public RegisteredSmartCardConnection(ISmartCardConnection inner, IDisposable registration)
    {
        _inner = inner;
        _disposal = new DisposalGate(registration);
    }

    public ConnectionType Type => _inner.Type;

    public Transport Transport => _inner.Transport;

    public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        ReadOnlyMemory<byte> command,
        CancellationToken cancellationToken = default) =>
        _inner.TransmitAndReceiveAsync(command, cancellationToken);

    public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
        _inner.BeginTransaction(cancellationToken);

    public bool SupportsExtendedApdu() => _inner.SupportsExtendedApdu();

    public void Dispose() => _disposal.Dispose(_inner.Dispose);

    public ValueTask DisposeAsync() => _disposal.DisposeAsync(_inner.DisposeAsync);
}

/// <inheritdoc cref="RegisteredSmartCardConnection" />
internal sealed class RegisteredFidoHidConnection : IFidoHidConnection
{
    private readonly DisposalGate _disposal;
    private readonly IFidoHidConnection _inner;

    public RegisteredFidoHidConnection(IFidoHidConnection inner, IDisposable registration)
    {
        _inner = inner;
        _disposal = new DisposalGate(registration);
    }

    public ConnectionType Type => _inner.Type;

    public int PacketSize => _inner.PacketSize;

    public Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken = default) =>
        _inner.SendAsync(packet, cancellationToken);

    public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
        _inner.ReceiveAsync(cancellationToken);

    public void Dispose() => _disposal.Dispose(_inner.Dispose);

    public ValueTask DisposeAsync() => _disposal.DisposeAsync(_inner.DisposeAsync);
}

/// <inheritdoc cref="RegisteredSmartCardConnection" />
internal sealed class RegisteredOtpHidConnection : IOtpHidConnection
{
    private readonly DisposalGate _disposal;
    private readonly IOtpHidConnection _inner;

    public RegisteredOtpHidConnection(IOtpHidConnection inner, IDisposable registration)
    {
        _inner = inner;
        _disposal = new DisposalGate(registration);
    }

    public ConnectionType Type => _inner.Type;

    public int FeatureReportSize => _inner.FeatureReportSize;

    public Task SendAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken = default) =>
        _inner.SendAsync(report, cancellationToken);

    public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
        _inner.ReceiveAsync(cancellationToken);

    public void Dispose() => _disposal.Dispose(_inner.Dispose);

    public ValueTask DisposeAsync() => _disposal.DisposeAsync(_inner.DisposeAsync);
}