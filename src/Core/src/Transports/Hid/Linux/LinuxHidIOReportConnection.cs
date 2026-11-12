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

using System.Runtime.Versioning;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.Transports.Hid.Linux;

/// <summary>
///     Provides synchronous expert access to Linux hidraw input and output reports.
/// </summary>
/// <remarks>
///     Each instance owns a Linux FIDO native lifetime, but does not impose the typed connection's 64-byte
///     packet contract. Construction blocks for worker-owned open, descriptor metadata and reactor activation;
///     it neither initializes a CTAPHID channel nor changes process-wide settings. Reports are unnumbered
///     payloads; the native seam supplies the synthetic zero report-ID byte for output only. The caller owns
///     protocol framing, sequencing and recovery and must not interleave raw I/O with a session.
/// </remarks>
[SupportedOSPlatform("linux")]
internal sealed class LinuxHidIOReportConnection : IHidConnection
{
    private readonly LinuxFidoHidNativeOwner _owner;

    /// <summary>Synchronously opens hidraw using the production report seam and shared readiness reactor.</summary>
    /// <param name="devNode">The hidraw device-node path.</param>
    public LinuxHidIOReportConnection(string devNode) : this(devNode, LinuxHidReportApi.Instance)
    {
    }

    /// <summary>Synchronously opens hidraw using a supplied report seam and the shared readiness reactor.</summary>
    /// <param name="devNode">The hidraw device-node path.</param>
    /// <param name="reportApi">The borrowed native report seam; this connection does not dispose it.</param>
    internal LinuxHidIOReportConnection(string devNode, ILinuxHidReportApi reportApi)
        : this(devNode, reportApi, LinuxHidReadReactor.Shared)
    {
    }

    /// <summary>Synchronously waits for native open, report-size discovery and input activation.</summary>
    /// <remarks>Failure waits for cleanup of partially acquired native state; there is no constructor timeout.</remarks>
    /// <param name="devNode">The hidraw device-node path.</param>
    /// <param name="reportApi">The borrowed native report seam; this connection does not dispose it.</param>
    /// <param name="readReactor">The borrowed readiness reactor; disposal removes only this lifetime's registration.</param>
    internal LinuxHidIOReportConnection(string devNode, ILinuxHidReportApi reportApi, ILinuxHidReadReactor readReactor) =>
        _owner = LinuxFidoHidNativeOwner.OpenAsync(devNode, reportApi, readReactor, CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>Gets the descriptor-derived input payload capacity in bytes, excluding any synthetic report ID.</summary>
    /// <remarks>
    ///     The native seam falls back to 64 when descriptor queries are unavailable. Supported capacities are
    ///     1 through 4096 bytes; a successful <see cref="GetReport" /> returns the actual count, not padded capacity.
    /// </remarks>
    public int InputReportSize => _owner.InputReportSize;

    /// <summary>Gets the descriptor-derived output payload size in bytes, excluding the native zero report ID.</summary>
    /// <remarks>
    ///     The native seam falls back to 64 when descriptor queries are unavailable. This is metadata, not an
    ///     exact-length requirement on <see cref="SetReport" />; direct output preserves supported caller lengths.
    /// </remarks>
    public int OutputReportSize => _owner.OutputReportSize;

    /// <summary>Synchronously receives the next actual-length unnumbered input payload.</summary>
    /// <remarks>
    ///     Waits indefinitely for input, terminal failure or disposal; there is no read timeout. The owner
    ///     transfers its exact-length array without another copy. This escaping array deliberately satisfies
    ///     the unchanged <see cref="IHidConnection" /> contract: the caller owns it and must clear sensitive
    ///     contents after use. Neither later reads nor connection disposal clear returned storage. EOF is a
    ///     terminal failure, not an empty successful report. Only one ordinary send or receive is admitted.
    /// </remarks>
    /// <returns>A caller-owned array of the actual input bytes, without padding or a synthetic report-ID prefix.</returns>
    public byte[] GetReport()
    {
        // Deliberately indefinite, as the original Linux blocking hidraw read. No macOS timeout.
        // Raw descriptor-sized reports are not restricted to typed FIDO64.
        var report = _owner.ReceiveAsync(false, CancellationToken.None).GetAwaiter().GetResult();
        // Native owner returns an owned exact-length array; transfer it without another copy.
        return System.Runtime.InteropServices.MemoryMarshal.TryGetArray(report, out var segment) && segment.Array is { } array
            ? array
            : throw new InvalidOperationException("Linux HID owner did not return an owned report.");
    }

    /// <summary>Synchronously sends an unnumbered output payload, preserving its supplied length.</summary>
    /// <remarks>
    ///     The array signature deliberately preserves <see cref="IHidConnection" /> compatibility. The owner
    ///     copies the payload before publishing work to its worker, then clears its copy before completion.
    ///     The native seam prefixes a zero report-ID byte and clears its own framing storage. This method
    ///     blocks until native output returns; it never clears or takes ownership of the caller's array.
    ///     The caller must keep the source stable during the call and clear sensitive contents afterward.
    ///     Payload length need not equal <see cref="OutputReportSize" /> or 64; lengths up to 16383 bytes are
    ///     forwarded, subject to native/device support. Overlapping ordinary raw operations are rejected.
    /// </remarks>
    /// <param name="report">The caller-owned payload, without the native zero report-ID prefix.</param>
    public void SetReport(byte[] report)
    {
        ArgumentNullException.ThrowIfNull(report);
        _owner.SendAsync(report, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>Stops admission and blocks for the shared native shutdown outcome.</summary>
    /// <remarks>
    ///     Wakes pending input, drains held output and actual reactor quiescence, then checks close on the
    ///     original worker. There is no drain timeout; an unproven release retains the native owner. Calling
    ///     from the native worker or read callback is rejected. Prefer <see cref="DisposeAsync" />.
    /// </remarks>
    public void Dispose()
    {
        _owner.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Stops admission and asynchronously observes the same native shutdown as <see cref="Dispose" />.</summary>
    /// <remarks>
    ///     Waits for held output, actual reactor quiescence and checked close. Close diagnostics can fault
    ///     disposal after proven release; unproven release retains the native owner instead.
    /// </remarks>
    /// <returns>The shared shutdown outcome, potentially pending indefinitely while native drain is held.</returns>
    public async ValueTask DisposeAsync()
    {
        await _owner.ShutdownAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>Gets the generic HID type retained by this untyped report facade.</summary>
    public ConnectionType Type { get; } = ConnectionType.Hid;

    /// <summary>Extracts input and output payload sizes from supported unnumbered YubiKey descriptors.</summary>
    /// <remarks>
    ///     Uses the last nonzero input/output main-item size, with a default of 64 for each missing size;
    ///     it is not a general collection-aggregating HID parser. Numbered reports are rejected.
    /// </remarks>
    /// <param name="descriptor">Borrowed descriptor bytes, used only during this call.</param>
    /// <returns>Input and output payload byte counts, excluding synthetic native report-ID framing.</returns>
    internal static (int InputSize, int OutputSize) ParseReportSizes(ReadOnlySpan<byte> descriptor) =>
        LinuxHidReportSizes.Parse(descriptor);

    /// <summary>Requests nonblocking, nonthrowing shutdown without waiting for or asserting native release.</summary>
    ~LinuxHidIOReportConnection() => _owner?.RequestShutdown();
}