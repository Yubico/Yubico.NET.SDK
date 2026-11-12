// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.Transports.Hid.Linux;

/// <summary>Synchronous expert feature-report facade over the Linux OTP native lifetime.</summary>
/// <remarks>
///     Each instance owns a separate native lifetime whose dedicated worker performs open, GET, SET and
///     checked close. GET uses the eight-byte OTP payload contract; direct SET preserves supported caller
///     lengths instead of imposing typed OTP8. Native framing supplies a zero report-ID byte not exposed in
///     payloads. The caller owns protocol framing, sequencing and recovery and must not interleave session I/O.
/// </remarks>
[SupportedOSPlatform("linux")]
internal sealed class LinuxHidFeatureReportConnection : IHidConnection
{
    private readonly LinuxOtpHidNativeOwner _owner;

    /// <summary>Synchronously opens hidraw feature-report access using the production native seam.</summary>
    /// <param name="devNode">The hidraw device-node path.</param>
    public LinuxHidFeatureReportConnection(string devNode) : this(devNode, LinuxHidReportApi.Instance) { }

    /// <summary>Synchronously waits for the dedicated worker to open hidraw feature-report access.</summary>
    /// <remarks>
    ///     Does not initialize an OTP protocol or change process-wide settings. Failed open waits for cleanup
    ///     of any acquired native state; neither construction nor its cleanup has a timeout.
    /// </remarks>
    /// <param name="devNode">The hidraw device-node path.</param>
    /// <param name="reportApi">The borrowed native report seam; this connection does not dispose it.</param>
    internal LinuxHidFeatureReportConnection(string devNode, ILinuxHidReportApi reportApi) =>
        _owner = LinuxOtpHidNativeOwner.OpenAsync(devNode, reportApi, CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>Gets the eight-byte GET payload size, excluding the native zero report-ID byte.</summary>
    public int InputReportSize => 8;

    /// <summary>Gets the nominal eight-byte OTP output payload size, excluding the native zero report ID.</summary>
    /// <remarks>This metadata does not constrain direct <see cref="SetReport" /> calls to eight bytes.</remarks>
    public int OutputReportSize => 8;

    /// <summary>Gets the generic HID type retained by this untyped report facade.</summary>
    public ConnectionType Type => ConnectionType.Hid;

    /// <summary>Synchronously receives an eight-byte OTP feature payload into newly allocated storage.</summary>
    /// <remarks>
    ///     The caller supplies no destination buffer. The worker allocates eight bytes; the native seam uses
    ///     nine framed bytes and strips the leading zero report ID. Native framing and failed/partial owner
    ///     output are cleared. This built-in implementation transfers the exact array to the caller without
    ///     another copy; its array signature preserves <see cref="IHidConnection" /> compatibility. The caller
    ///     must clear sensitive returned contents after use; connection disposal does not clear them. This call waits for native
    ///     GET to return, without a timeout. Only one ordinary GET or SET is admitted at a time.
    /// </remarks>
    /// <returns>A caller-owned eight-byte payload array without the native report-ID prefix.</returns>
    public byte[] GetReport()
    {
        var report = _owner.ReceiveAsync(CancellationToken.None).GetAwaiter().GetResult();
        // The owner returns an exact, exclusively owned array; transfer without a second copy.
        return MemoryMarshal.TryGetArray(report, out var segment) && segment.Array is { } array && segment.Offset == 0 && segment.Count == array.Length
            ? array
            : throw new InvalidOperationException("Linux OTP owner returned non-array storage.");
    }

    /// <summary>Synchronously sends a feature payload, preserving its supplied length.</summary>
    /// <remarks>
    ///     The array signature deliberately preserves <see cref="IHidConnection" /> compatibility. The owner
    ///     copies the payload before publishing work to its worker and clears that copy before completion.
    ///     The native seam adds a zero report-ID prefix and clears its own framing storage. This call blocks
    ///     until native SET returns, but never clears or takes ownership of the caller's source array. Keep
    ///     the source stable during the call and clear sensitive contents afterward. Length need not equal
    ///     <see cref="OutputReportSize" />: up to 16382 payload bytes are forwarded, subject to native/device
    ///     support. Overlapping ordinary GET or SET calls are rejected.
    /// </remarks>
    /// <param name="report">The caller-owned payload, without a native report-ID prefix.</param>
    public void SetReport(byte[] report)
    {
        ArgumentNullException.ThrowIfNull(report);
        _owner.SendAsync(report, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>Stops admission and blocks for the shared native shutdown outcome.</summary>
    /// <remarks>
    ///     Waits for any admitted GET/SET to return and checked close on the original worker. There is no drain
    ///     timeout; unproven release retains the native owner. Calling from the native worker is rejected.
    ///     Prefer <see cref="DisposeAsync" /> to avoid blocking the caller.
    /// </remarks>
    public void Dispose()
    {
        _owner.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Stops admission and asynchronously observes the same native shutdown as <see cref="Dispose" />.</summary>
    /// <remarks>
    ///     Waits for admitted feature I/O and checked close. Close diagnostics can fault disposal after proven
    ///     release; unproven release retains the native owner instead.
    /// </remarks>
    /// <returns>The shared shutdown outcome, potentially pending indefinitely while a native call is held.</returns>
    public async ValueTask DisposeAsync()
    {
        await _owner.ShutdownAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>Requests nonblocking, nonthrowing shutdown without waiting for or asserting native release.</summary>
    ~LinuxHidFeatureReportConnection() => _owner?.RequestShutdown();
}