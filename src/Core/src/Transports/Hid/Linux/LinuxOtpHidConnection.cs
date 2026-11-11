// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.Transports.Hid.Linux;

/// <summary>Provides the built-in typed Linux OTP connection over a hidraw feature-report native owner.</summary>
/// <remarks>
///     Selected by the Linux typed connection factory. Each connection owns a separate native lifetime with
///     one dedicated worker and one ordinary operation slot; feature I/O does not use the read reactor.
///     The synchronous expert facade uses the same owner type, not the same owner instance.
/// </remarks>
internal sealed class LinuxOtpHidConnection : IOtpHidConnection
{
    private readonly LinuxOtpHidNativeOwner _owner;
    /// <summary>Takes ownership of an already-open native lifetime.</summary>
    /// <param name="owner">The owner to shut down when this connection is disposed.</param>
    private LinuxOtpHidConnection(LinuxOtpHidNativeOwner owner) => _owner = owner;

    /// <summary>Gets the eight-byte OTP payload size, excluding the native zero report-ID prefix.</summary>
    public int FeatureReportSize => 8;

    /// <summary>Gets the OTP HID transport type.</summary>
    public ConnectionType Type => ConnectionType.HidOtp;

    /// <summary>Opens a native feature-report lifetime on its dedicated worker.</summary>
    /// <remarks>
    ///     Cancellation requests shutdown; failed or canceled opens await cleanup rather than detaching held
    ///     native work. The native owner accepts the physical claim even on failure and retains it if native
    ///     release cannot be proven.
    /// </remarks>
    /// <param name="path">The hidraw device-node path.</param>
    /// <param name="reportApi">The borrowed native report seam; this connection does not dispose it.</param>
    /// <param name="cancellationToken">Requests cancellation of open and cleanup of acquired native state.</param>
    /// <param name="physicalClaim">An optional claim transferred to the native owner, not retained by the caller.</param>
    /// <returns>The caller-owned connection, which must be disposed, preferably asynchronously.</returns>
    internal static async Task<LinuxOtpHidConnection> OpenAsync(string path, ILinuxHidReportApi reportApi,
        CancellationToken cancellationToken, IDisposable? physicalClaim = null)
    {
        var owner = await LinuxOtpHidNativeOwner.OpenAsync(path, reportApi, cancellationToken, physicalClaim).ConfigureAwait(false);
        try { return new LinuxOtpHidConnection(owner); }
        catch (Exception constructionError)
        {
            try { await owner.ShutdownAsync().ConfigureAwait(false); }
            catch (Exception releaseError) { throw new AggregateException(constructionError, releaseError); }
            throw;
        }
    }

    /// <inheritdoc cref="IOtpHidConnection.SendAsync" />
    /// <remarks>
    ///     The owner copies the eight payload bytes before publishing work to its worker and clears that copy
    ///     before task completion. Native framing adds a zero report-ID byte. The caller retains its source
    ///     memory and must clear sensitive input after the task is terminal; the SDK never clears that source.
    ///     Cancellation before dispatch skips SET; after dispatch the task waits for native completion.
    ///     The caller remains responsible for OTP sequencing and must not interleave session or raw I/O.
    /// </remarks>
    /// <param name="report">Exactly eight caller-owned payload bytes, without a native report-ID prefix.</param>
    /// <param name="cancellationToken">Cancels SET only before native dispatch.</param>
    /// <returns>A task terminal only after any dispatched SET returns and its staging copy is cleared.</returns>
    public Task SendAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return report.Length != 8
            ? throw new ArgumentException("OTP feature reports must contain exactly 8 bytes.", nameof(report))
            : _owner.SendAsync(report, cancellationToken);
    }

    /// <inheritdoc cref="IOtpHidConnection.ReceiveAsync" />
    /// <remarks>
    ///     The worker allocates eight bytes for GET. Failed or partial output is cleared; successful output
    ///     transfers to the caller through a read-only view and is not cleared by connection disposal. The view
    ///     does not make its backing array immutable or automatically clear sensitive data: the caller owns
    ///     that storage's lifetime and sensitive-data cleanup. Native framing uses nine bytes, including a zero
    ///     report ID that is not returned. Cancellation after native dispatch cannot detach GET.
    ///     For these built-in array-backed results, use <c>MemoryMarshal.TryGetArray</c> to obtain the owned
    ///     segment, then <c>CryptographicOperations.ZeroMemory(segment.AsSpan())</c> after use. Clearing a
    ///     separate copy does not clear the original. Do not apply this ownership assumption to arbitrary
    ///     custom-provider results without that provider's contract.
    /// </remarks>
    /// <param name="cancellationToken">Cancels GET only before native dispatch.</param>
    /// <returns>A task yielding eight caller-owned payload bytes after native GET completes.</returns>
    public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
        _owner.ReceiveAsync(cancellationToken);

    /// <summary>Stops admission and synchronously waits for the shared native shutdown outcome.</summary>
    /// <remarks>
    ///     Blocks until an admitted feature call returns and checked close runs on the original worker.
    ///     There is no drain timeout; unproven release retains the owner and any physical claim. Do not call
    ///     from the native worker. Prefer <see cref="DisposeAsync" /> to avoid blocking the caller.
    /// </remarks>
    public void Dispose()
    {
        _owner.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Stops admission and asynchronously observes the same native shutdown as <see cref="Dispose" />.</summary>
    /// <remarks>
    ///     Waits for any admitted feature call and checked close. Close diagnostics can fault disposal after
    ///     proven release; unproven release instead retains the native owner and any physical claim.
    /// </remarks>
    /// <returns>The shared shutdown outcome; it can remain pending indefinitely while a native call is held.</returns>
    public async ValueTask DisposeAsync()
    {
        await _owner.ShutdownAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>Requests nonblocking, nonthrowing shutdown without asserting native release.</summary>
    ~LinuxOtpHidConnection() => _owner?.RequestShutdown();
}