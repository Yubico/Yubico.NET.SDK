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
internal sealed class RegisteredSmartCardConnection(
    ISmartCardConnection inner,
    ISessionLease registration) : ISmartCardConnection
{
    private readonly DisposalGate _disposal = new(registration);

    public ConnectionType Type => inner.Type;

    public Transport Transport => inner.Transport;

    /// <summary>
    ///     Claims the applet before letting an ISO SELECT reach the card. This is the last point at which the
    ///     interface lease and the applet identity are both in hand, and it is the exact moment the
    ///     destructive act happens: a SELECT of a different applet deselects whatever another open connection
    ///     on this interface was using. Claiming first means a conflict throws with the card untouched.
    /// </summary>
    public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        ReadOnlyMemory<byte> command,
        CancellationToken cancellationToken = default) =>
        TryReadSelectedApplet(command.Span, out var applicationId)
            ? SelectAppletAsync(command, applicationId, cancellationToken)
            : inner.TransmitAndReceiveAsync(command, cancellationToken);

    /// <summary>
    ///     Claims, transmits, then reconciles: the claim is a prediction made before the wire, and only the
    ///     outcome says whether the card's current application really changed. Every ending that leaves the
    ///     card on its previous applet — an error status word, a transport fault, a cancellation — hands the
    ///     claim back, so the registry cannot come to believe in a selection that never happened and wave a
    ///     later lease through to deselect the applet a session is still on.
    /// </summary>
    private async Task<ReadOnlyMemory<byte>> SelectAppletAsync(
        ReadOnlyMemory<byte> command,
        Range applicationId,
        CancellationToken cancellationToken)
    {
        registration.SelectApplet(command[applicationId]);

        ReadOnlyMemory<byte> response;
        try
        {
            response = await inner.TransmitAndReceiveAsync(command, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            registration.AbandonAppletSelect();
            throw;
        }

        if (!ChangedTheCurrentApplication(response.Span))
            registration.AbandonAppletSelect();

        return response;
    }

    /// <summary>
    ///     Whether a SELECT response says the card's current application actually changed.
    /// </summary>
    /// <remarks>
    ///     ISO 7816-4 splits the status word by whether the command was performed. Normal processing
    ///     (SW=0x9000, or SW1=0x61 with response data still to fetch) and the warning classes (SW1=0x62 and
    ///     0x63) all mean the SELECT executed, so the new application is current. Everything from SW1=0x64
    ///     up — execution errors and checking errors such as 0x6A82 "file or application not found" — means
    ///     it did not, and the previously selected application is still the current one. A truncated
    ///     response carries no status word at all and is treated the same way, since nothing on it says the
    ///     card moved.
    /// </remarks>
    private static bool ChangedTheCurrentApplication(ReadOnlySpan<byte> response) =>
        response.Length >= 2
        && response[^2] switch
        {
            0x90 => response[^1] == 0x00,
            0x61 or 0x62 or 0x63 => true,
            _ => false
        };

    /// <summary>
    ///     Recognizes an ISO 7816-4 SELECT-by-DF-name (CLA=0x00 INS=0xA4 P1=0x04) on the wire and returns the
    ///     range holding its AID, for both the short and extended encodings this SDK emits.
    /// </summary>
    /// <remarks>
    ///     Reading applet identity off the wire keeps the whole rule in one place, but it can only see
    ///     plaintext SELECTs. An SCP-wrapped SELECT carries CLA=0x04 and an encrypted AID and is deliberately
    ///     not matched here: every SELECT that establishes a session's applet is sent before SCP is layered
    ///     on, so the claim is always made from plaintext.
    /// </remarks>
    private static bool TryReadSelectedApplet(ReadOnlySpan<byte> command, out Range applicationId)
    {
        applicationId = default;
        if (command.Length < 4 || command[0] != 0x00 || command[1] != 0xA4 || command[2] != 0x04)
            return false;

        // Case 1/2S (no data): the byte after the header is Le, not Lc. Nothing to name.
        if (command.Length < 6)
            return false;

        // Extended encodings write a 0x00 marker followed by a two-byte Lc; short ones write Lc directly.
        var (start, length) = command[4] == 0x00
            ? (7, command.Length >= 7 ? (command[5] << 8) | command[6] : 0)
            : (5, command[4]);

        if (length == 0 || start + length > command.Length)
            return false;

        applicationId = new Range(start, start + length);
        return true;
    }

    public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
        inner.BeginTransaction(cancellationToken);

    public bool SupportsExtendedApdu() => inner.SupportsExtendedApdu();

    public void Dispose() => _disposal.Dispose(inner.Dispose);

    public ValueTask DisposeAsync() => _disposal.DisposeAsync(inner.DisposeAsync);
}

/// <inheritdoc cref="RegisteredSmartCardConnection" />
internal sealed class RegisteredFidoHidConnection(
    IFidoHidConnection inner,
    IDisposable registration) : IFidoHidConnection
{
    private readonly DisposalGate _disposal = new(registration);

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