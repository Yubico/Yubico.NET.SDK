using System.Runtime.CompilerServices;
using Yubico.YubiKit.Core.Abstractions;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
/// Discovers YubiKeys on demand, streams changes, and creates shared monitors for continuous tracking.
/// </summary>
/// <remarks>
/// Find operations perform independent one-shot discovery. Dispose watch enumerations when finished;
/// dispose shared monitors with <c>await using</c>. Manager watch creates a producer; monitor watch
/// subscribes to an existing one. Discovery failures and cancellation propagate to the caller;
/// watch enumerations also throw on subscriber overflow or terminal monitoring failure.
/// </remarks>
/// <example>
/// <code>
/// using System;
/// using System.Threading;
/// using System.Threading.Tasks;
/// using Yubico.YubiKit.Core.Devices;
///
/// static async Task TrackAsync(CancellationToken cancellationToken)
/// {
///     await using var monitor = await YubiKeyManager.CreateMonitorAsync(cancellationToken);
///     await foreach (var change in monitor.WatchAsync(cancellationToken))
///     {
///         Console.WriteLine(change switch
///         {
///             DeviceAdded added => $"Added: {added.Device.DeviceId}",
///             DeviceRemoved removed => $"Removed: {removed.Device.DeviceId}",
///             DeviceUpdated updated => $"Updated: {updated.Device.DeviceId}",
///             SnapshotComplete complete => $"Snapshot complete at revision {complete.Revision}",
///             _ => throw new InvalidOperationException("Unknown device change.")
///         });
///     }
/// }
/// </code>
/// </example>
public static class YubiKeyManager
{
    private static readonly YubiKeyDiscovery Discovery = new(FindYubiKeys.CreateForInterfaces);

    /// <summary>
    /// Performs fresh one-shot discovery using only the requested interfaces.
    /// </summary>
    /// <param name="interfaces">
    /// Interfaces to enumerate and probe, not a result filter. Flags select places to look, not a
    /// requirement that each result expose every selected interface. <see cref="ConnectionType.Hid"/>
    /// includes FIDO and OTP; <see cref="ConnectionType.Unknown"/> scans nothing. Undefined bits are ignored.
    /// </param>
    /// <param name="cancellationToken">Cancels discovery waits, not already-dispatched native work.</param>
    /// <returns>
    /// The YubiKeys found, each a device view exposing only its observed in-scope interfaces, or an empty list.
    /// </returns>
    /// <exception cref="OperationCanceledException">The cancellation token is canceled.</exception>
    /// <exception cref="PlatformInteropException">A requested platform API fails.</exception>
    /// <remarks>
    /// Each call uses fresh identity and metadata evidence without changing running monitors. Ordering is
    /// unspecified. Grouping is conservative: ambiguous evidence can split one physical key into several
    /// results. Complementary partial interfaces from same-PID keys can also resemble one complete key
    /// and group across keys until serial, topology, or fuller enumeration distinguishes them. Metadata
    /// is best-effort last-known data. For full composite keys, discover All and filter
    /// locally with <see cref="IYubiKey.SupportsConnection"/>. Coordinate YubiKeys from disjoint scopes before
    /// opening connections: physical-key exclusion relies on overlapping known interface IDs. Cancellation
    /// bounds waits, not dispatched native work; returned YubiKeys remain usable after discovery ends.
    /// Each transport enumeration has a five-second wait budget. A timeout logs a warning and omits that
    /// transport for this call; repeat discovery after the service recovers if the result is incomplete.
    /// </remarks>
    public static Task<IReadOnlyList<IYubiKey>> FindAllAsync(
        ConnectionType interfaces = ConnectionType.All,
        CancellationToken cancellationToken = default) =>
        Discovery.FindAllAsync(interfaces, cancellationToken);

    /// <summary>Discovers now and selects the first YubiKey satisfying an optional predicate.</summary>
    /// <param name="predicate">Optional device selection predicate, evaluated after discovery.</param>
    /// <param name="interfaces">Interfaces to enumerate and probe, as for <see cref="FindAllAsync(ConnectionType, CancellationToken)"/>.</param>
    /// <param name="cancellationToken">Requests discovery cancellation.</param>
    /// <returns>The first matching device.</returns>
    /// <exception cref="InvalidOperationException">No matching YubiKey was found.</exception>
    /// <exception cref="OperationCanceledException">The cancellation token is canceled.</exception>
    /// <exception cref="PlatformInteropException">A requested platform API fails.</exception>
    /// <remarks>
    /// Performs one independent discovery operation; does not wait for insertion. Ordering is unspecified.
    /// Discovery and predicate exceptions propagate unchanged. No monitor or shared cache is initialized.
    /// </remarks>
    public static Task<IYubiKey> FindFirstAsync(
        Func<IYubiKey, bool>? predicate = null,
        ConnectionType interfaces = ConnectionType.All,
        CancellationToken cancellationToken = default) =>
        Discovery.FindFirstAsync(predicate, interfaces, cancellationToken);

    /// <summary>Discovers now and selects the first matching YubiKey, or returns null.</summary>
    /// <param name="predicate">Optional device selection predicate, evaluated after discovery.</param>
    /// <param name="interfaces">Interfaces to enumerate and probe, as for <see cref="FindAllAsync(ConnectionType, CancellationToken)"/>.</param>
    /// <param name="cancellationToken">Requests discovery cancellation.</param>
    /// <returns>The first matching device, or null when none matches.</returns>
    /// <exception cref="OperationCanceledException">The cancellation token is canceled.</exception>
    /// <exception cref="PlatformInteropException">A requested platform API fails.</exception>
    /// <remarks>
    /// Performs one independent discovery operation; does not wait for insertion. Ordering is unspecified.
    /// Discovery and predicate exceptions propagate unchanged. No monitor or shared cache is initialized.
    /// </remarks>
    public static Task<IYubiKey?> FindFirstOrDefaultAsync(
        Func<IYubiKey, bool>? predicate = null,
        ConnectionType interfaces = ConnectionType.All,
        CancellationToken cancellationToken = default) =>
        Discovery.FindFirstOrDefaultAsync(predicate, interfaces, cancellationToken);

    /// <summary>
    /// Creates a private monitor for the lifetime of this enumeration; replay as <see cref="DeviceAdded"/>,
    /// then <see cref="SnapshotComplete"/>, then live changes.
    /// </summary>
    /// <param name="cancellationToken">Cancels initialization or ends this enumeration and its private monitor.</param>
    /// <returns>A lazy stream that creates a monitor on the first <c>MoveNextAsync</c>.</returns>
    /// <remarks>
    /// Each enumeration runs its own listeners and scans; to share one monitor across several consumers,
    /// use <see cref="CreateMonitorAsync"/>. All interfaces are scanned; filter changes locally.
    /// Dispose the enumeration when finished, for example with <c>await foreach</c>; its monitor is disposed
    /// on completion, cancellation, an exception, or a consumer break. First-scan discovery exceptions
    /// propagate unchanged from the first <c>MoveNextAsync</c>.
    /// </remarks>
    /// <exception cref="OperationCanceledException">The cancellation token is canceled.</exception>
    /// <exception cref="InvalidOperationException">The subscriber overflowed, or monitoring failed terminally.</exception>
    /// <exception cref="PlatformInteropException">A requested platform API fails during the first scan.</exception>
    public static IAsyncEnumerable<DeviceChange> WatchAsync(CancellationToken cancellationToken = default) =>
        WatchAsync(YubiKeyMonitor.CreateAsync, cancellationToken);

    // Factory injection preserves per-enumeration ownership without mutable global configuration.
    internal static async IAsyncEnumerable<DeviceChange> WatchAsync(
        Func<CancellationToken, Task<YubiKeyMonitor>> createMonitor,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var monitor = await createMonitor(cancellationToken).ConfigureAwait(false);
        await foreach (var change in monitor.WatchAsync(cancellationToken).ConfigureAwait(false))
            yield return change;
    }

    /// <summary>Creates an independent, active monitor with its initial membership already published.</summary>
    /// <param name="cancellationToken">Cancels initialization only; after return the caller owns the monitor lifetime.</param>
    /// <returns>An initialized monitor, including when its first successful snapshot is empty.</returns>
    /// <remarks>
    /// Dispose the returned monitor with <c>await using</c> and share it across consumers to use one set of
    /// listeners and scans. It scans all interfaces even without watchers. Initialization failure or
    /// cancellation cleans up partial ownership. First-scan discovery exceptions propagate unchanged;
    /// successful initialization does not guarantee every interface or metadata field was accessible.
    /// </remarks>
    /// <exception cref="OperationCanceledException">Initialization was canceled.</exception>
    /// <exception cref="PlatformInteropException">A requested platform API fails during the first scan.</exception>
    public static Task<YubiKeyMonitor> CreateMonitorAsync(CancellationToken cancellationToken = default) =>
        YubiKeyMonitor.CreateAsync(cancellationToken);
}