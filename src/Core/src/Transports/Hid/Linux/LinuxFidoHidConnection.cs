// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;

namespace Yubico.YubiKit.Core.Transports.Hid.Linux;

/// <summary>Provides the built-in typed Linux FIDO connection over a hidraw native owner.</summary>
/// <remarks>
///     Selected by the Linux typed connection factory. Each connection owns a native lifetime with one
///     dedicated worker and reactor registration; the synchronous expert facade uses the same owner type,
///     not the same owner instance. Only one ordinary send or receive is admitted at a time.
/// </remarks>
internal sealed class LinuxFidoHidConnection : IFidoHidConnection, ITerminalWakeControl
{
    private readonly LinuxFidoHidNativeOwner _owner;
    /// <summary>Takes ownership of an already-open native lifetime.</summary>
    /// <param name="owner">The owner to shut down when this connection is disposed.</param>
    private LinuxFidoHidConnection(LinuxFidoHidNativeOwner owner) => _owner = owner;

    /// <summary>Gets the 64-byte CTAPHID packet size, excluding the native zero report-ID prefix.</summary>
    public int PacketSize => 64;

    /// <summary>Gets the FIDO HID transport type.</summary>
    public ConnectionType Type => ConnectionType.HidFido;

    /// <summary>Opens a native lifetime and waits for its input registration to activate.</summary>
    /// <remarks>
    ///     Native open and metadata operations run on the owner's worker. Cancellation requests shutdown;
    ///     failed or canceled opens await cleanup rather than detaching held native work. The native owner
    ///     accepts the physical claim even on failure and retains it if native release cannot be proven.
    /// </remarks>
    /// <param name="path">The hidraw device-node path.</param>
    /// <param name="reportApi">The borrowed native report seam; this connection does not dispose it.</param>
    /// <param name="reactor">The borrowed readiness reactor; only this lifetime's registration is removed.</param>
    /// <param name="cancellationToken">Requests cancellation of open and cleanup of acquired native state.</param>
    /// <param name="physicalClaim">An optional claim transferred to the native owner, not retained by the caller.</param>
    /// <returns>The caller-owned connection, which must be disposed, preferably asynchronously.</returns>
    internal static async Task<LinuxFidoHidConnection> OpenAsync(string path, ILinuxHidReportApi reportApi,
        ILinuxHidReadReactor reactor, CancellationToken cancellationToken, IDisposable? physicalClaim = null)
    {
        var owner = await LinuxFidoHidNativeOwner.OpenAsync(path, reportApi, reactor, cancellationToken, physicalClaim).ConfigureAwait(false);
        try { return new LinuxFidoHidConnection(owner); }
        catch (Exception constructionError)
        {
            try { await owner.ShutdownAsync().ConfigureAwait(false); }
            catch (Exception releaseError) { throw new AggregateException(constructionError, releaseError); }
            throw;
        }
    }

    /// <inheritdoc cref="IFidoHidConnection.SendAsync" />
    /// <remarks>
    ///     The owner copies all 64 payload bytes before publishing work to its worker, and clears that copy
    ///     before task completion. Native framing adds a zero report-ID byte. The caller retains its source
    ///     memory and must clear sensitive input after the task is terminal; the SDK never clears that source.
    ///     Cancellation before dispatch skips output; after dispatch the task waits for native completion.
    ///     The caller remains responsible for CTAPHID sequencing and must not interleave session or raw I/O.
    /// </remarks>
    /// <param name="packet">Exactly 64 caller-owned payload bytes, without a native report-ID prefix.</param>
    /// <param name="cancellationToken">Cancels output only before native dispatch.</param>
    /// <returns>A task terminal only after any dispatched output returns and its staging copy is cleared.</returns>
    public Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return packet.Length != 64
            ? throw new ArgumentException("FIDO packets must contain exactly 64 bytes.", nameof(packet))
            : _owner.SendAsync(packet, cancellationToken);
    }

    /// <inheritdoc cref="IFidoHidConnection.ReceiveAsync" />
    /// <remarks>
    ///     Successful receipt transfers an exact-length array to the caller through a read-only view; the
    ///     connection no longer owns or clears it, including on disposal. The view does not make the backing
    ///     storage immutable or provide automatic sensitive-data cleanup. The caller owns its lifetime and
    ///     clearing of sensitive contents. Cancellation detaches only this reader; native input remains active.
    ///     A non-64-byte report terminates input rather than returning a partial typed packet.
    ///     For these built-in array-backed results, use <c>MemoryMarshal.TryGetArray</c> to obtain the owned
    ///     segment, then <c>CryptographicOperations.ZeroMemory(segment.AsSpan())</c> after use. Clearing a
    ///     separate copy does not clear the original. Do not apply this ownership assumption to arbitrary
    ///     custom-provider results without that provider's contract.
    /// </remarks>
    /// <param name="cancellationToken">Cancels this pending receive, not the device's protocol exchange.</param>
    /// <returns>A task yielding 64 caller-owned payload bytes, with no synthetic report-ID prefix.</returns>
    public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
        _owner.ReceiveAsync(true, cancellationToken);

    /// <summary>Terminates input and wakes a pending reader without waiting for native release.</summary>
    /// <remarks>Disposal must still drain the worker and reactor registration and check native close.</remarks>
    public void RequestTerminalWake() => _owner.Terminal(new ObjectDisposedException(nameof(LinuxFidoHidConnection)));

    /// <summary>Stops admission and synchronously waits for the shared native shutdown outcome.</summary>
    /// <remarks>
    ///     Blocks through held output, actual reactor quiescence and checked close on the original worker.
    ///     There is no drain timeout; an unproven release retains the owner and any physical claim. Do not call
    ///     from the native worker or read callback. Prefer <see cref="DisposeAsync" /> to avoid blocking.
    /// </remarks>
    public void Dispose()
    {
        _owner.Dispose();
        GC.SuppressFinalize(this);
    }
    /// <summary>Stops admission and asynchronously observes the same native shutdown as <see cref="Dispose" />.</summary>
    /// <remarks>
    ///     Completion waits for held output, true reactor quiescence and checked close. A disposal error alone
    ///     is not proof of unreleased state: close diagnostics can be reported after proven release, whereas
    ///     unproven release retains the native owner and any physical claim.
    /// </remarks>
    /// <returns>The shared shutdown outcome; it can remain pending indefinitely while native drain is held.</returns>
    public async ValueTask DisposeAsync()
    {
        await _owner.ShutdownAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
    /// <summary>Requests nonblocking, nonthrowing shutdown without asserting native release.</summary>
    ~LinuxFidoHidConnection() => _owner?.RequestShutdown();
}