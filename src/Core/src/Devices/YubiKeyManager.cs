using Yubico.YubiKit.Core.Abstractions;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
/// Discovers YubiKeys on demand and creates explicitly owned monitors for continuous tracking.
/// </summary>
/// <remarks>
/// Find operations always perform independent one-shot discovery, without a shared cache, listeners,
/// or repository publication. For cached membership and device events, own and dispose a
/// <see cref="YubiKeyMonitor"/> created by <see cref="CreateMonitorAsync"/>.
/// </remarks>
/// <example>
/// <code>
/// var key = await YubiKeyManager.FindFirstAsync(
///     predicate: key => key.SerialNumber == serial, cancellationToken: ct);
///
/// await using var monitor = await YubiKeyManager.CreateMonitorAsync(ct);
/// await foreach (var change in monitor.WatchAsync(ct))
///     Apply(change); // Initial Added events, then ordered changes. Marshal to a UI thread if needed.
/// </code>
/// </example>
public static class YubiKeyManager
{
    /// <summary>
    /// Performs fresh one-shot discovery using only the requested interfaces.
    /// </summary>
    /// <param name="interfaces">
    /// Interfaces to enumerate and probe, not a result filter. Flags select places to look, not a
    /// requirement that each result expose every selected interface. <see cref="ConnectionType.Hid"/>
    /// includes FIDO and OTP; <see cref="ConnectionType.Unknown"/> scans nothing. Undefined bits are ignored.
    /// </param>
    /// <param name="cancellationToken">Requests discovery cancellation; dispatched native enumeration can delay completion.</param>
    /// <returns>New device views containing only observed in-scope interfaces, or an empty list.</returns>
    /// <exception cref="OperationCanceledException">The cancellation token is canceled.</exception>
    /// <exception cref="PlatformInteropException">A requested platform API fails.</exception>
    /// <remarks>
    /// <para>Each call uses fresh identity and metadata evidence. It does not initialize tracking state,
    /// publish events, or change any running monitor. SmartCard-only discovery avoids HID enumeration;
    /// HID-only discovery avoids PC/SC. OTP and FIDO share OS HID descriptor enumeration, but excluded
    /// interfaces receive no slots or protocol probes. To retain a full composite key, discover All and
    /// filter the returned devices locally with <see cref="IYubiKey.SupportsConnection"/>.</para>
    /// <para>Grouping is conservative: ambiguous evidence can split one physical key into multiple results.
    /// Metadata is best-effort last-known data, not live configuration. Ordering is unspecified, and
    /// independently obtained objects and identifiers are not durable physical-identity records.</para>
    /// <para>Connection exclusion is in-process and based on known member interface IDs. Overlapping views
    /// exclude each other; disjoint scoped views cannot guarantee physical-key exclusion, even with matching
    /// serials. Coordinate such views rather than opening them concurrently.</para>
    /// <para>Cancellation and read budgets bound identity/metadata waits, not already-dispatched native work.
    /// There is no end-to-end timeout guarantee. On exit, queued reads are retired; already-open reads
    /// retain ownership until background teardown. Returned descriptors and their connection slots remain usable.</para>
    /// </remarks>
    public static Task<IReadOnlyList<IYubiKey>> FindAllAsync(
        ConnectionType interfaces = ConnectionType.All,
        CancellationToken cancellationToken = default) =>
        FindAllAsync(interfaces, cancellationToken, FindYubiKeys.CreateForScope);

    /// <summary>Performs fresh one-shot discovery across all interfaces.</summary>
    /// <param name="cancellationToken">Requests discovery cancellation.</param>
    /// <returns>The discovered device views.</returns>
    public static Task<IReadOnlyList<IYubiKey>> FindAllAsync(CancellationToken cancellationToken) =>
        FindAllAsync(ConnectionType.All, cancellationToken);

    // Per-call factory seam: no mutable global test configuration or native enumeration in unit tests.
    internal static async Task<IReadOnlyList<IYubiKey>> FindAllAsync(
        ConnectionType interfaces,
        CancellationToken cancellationToken,
        Func<ConnectionType, FindYubiKeys> createFinder)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var finder = createFinder(interfaces);
        try
        {
            return await finder.FindAllAsync(ConnectionType.All, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            finder.RetireReadScope();
        }
    }

    /// <summary>Discovers now and selects the first YubiKey satisfying an optional predicate.</summary>
    /// <param name="predicate">Optional device selection predicate, evaluated after discovery.</param>
    /// <param name="interfaces">Interfaces to enumerate and probe, as for <see cref="FindAllAsync(ConnectionType, CancellationToken)"/>.</param>
    /// <param name="cancellationToken">Requests discovery cancellation.</param>
    /// <returns>The first matching device.</returns>
    /// <exception cref="InvalidOperationException">No matching YubiKey was found.</exception>
    /// <remarks>
    /// Performs one independent discovery operation; does not wait for insertion. Ordering is unspecified.
    /// Discovery and predicate exceptions propagate unchanged. No monitor or shared cache is initialized.
    /// </remarks>
    public static Task<IYubiKey> FindFirstAsync(
        Func<IYubiKey, bool>? predicate = null,
        ConnectionType interfaces = ConnectionType.All,
        CancellationToken cancellationToken = default) =>
        FindFirstAsync(predicate, interfaces, cancellationToken, FindYubiKeys.CreateForScope);

    /// <summary>Discovers all interfaces and selects the first matching YubiKey.</summary>
    /// <param name="predicate">Optional device selection predicate.</param>
    /// <param name="cancellationToken">Requests discovery cancellation.</param>
    /// <returns>The first matching device.</returns>
    /// <exception cref="InvalidOperationException">No matching YubiKey was found.</exception>
    public static Task<IYubiKey> FindFirstAsync(Func<IYubiKey, bool>? predicate, CancellationToken cancellationToken) =>
        FindFirstAsync(predicate, ConnectionType.All, cancellationToken);

    internal static async Task<IYubiKey> FindFirstAsync(
        Func<IYubiKey, bool>? predicate, ConnectionType interfaces, CancellationToken cancellationToken,
        Func<ConnectionType, FindYubiKeys> createFinder) =>
        await FindFirstOrDefaultAsync(predicate, interfaces, cancellationToken, createFinder).ConfigureAwait(false)
            ?? throw new InvalidOperationException("No matching YubiKey was found.");

    /// <summary>Discovers now and selects the first matching YubiKey, or returns null.</summary>
    /// <param name="predicate">Optional device selection predicate, evaluated after discovery.</param>
    /// <param name="interfaces">Interfaces to enumerate and probe, as for <see cref="FindAllAsync(ConnectionType, CancellationToken)"/>.</param>
    /// <param name="cancellationToken">Requests discovery cancellation.</param>
    /// <returns>The first matching device, or null when none matches.</returns>
    /// <remarks>
    /// Performs one independent discovery operation; does not wait for insertion. Ordering is unspecified.
    /// Discovery and predicate exceptions propagate unchanged. No monitor or shared cache is initialized.
    /// </remarks>
    public static Task<IYubiKey?> FindFirstOrDefaultAsync(
        Func<IYubiKey, bool>? predicate = null,
        ConnectionType interfaces = ConnectionType.All,
        CancellationToken cancellationToken = default) =>
        FindFirstOrDefaultAsync(predicate, interfaces, cancellationToken, FindYubiKeys.CreateForScope);

    /// <summary>Discovers all interfaces and selects the first matching YubiKey, or returns null.</summary>
    /// <param name="predicate">Optional device selection predicate.</param>
    /// <param name="cancellationToken">Requests discovery cancellation.</param>
    /// <returns>The first matching device, or null.</returns>
    public static Task<IYubiKey?> FindFirstOrDefaultAsync(Func<IYubiKey, bool>? predicate, CancellationToken cancellationToken) =>
        FindFirstOrDefaultAsync(predicate, ConnectionType.All, cancellationToken);

    internal static async Task<IYubiKey?> FindFirstOrDefaultAsync(
        Func<IYubiKey, bool>? predicate, ConnectionType interfaces, CancellationToken cancellationToken,
        Func<ConnectionType, FindYubiKeys> createFinder)
    {
        var devices = await FindAllAsync(interfaces, cancellationToken, createFinder).ConfigureAwait(false);
        return predicate is null ? devices.FirstOrDefault() : devices.FirstOrDefault(predicate);
    }

    /// <summary>Creates an independent, active monitor with its initial membership already published.</summary>
    /// <param name="cancellationToken">Cancels initialization only; after return the caller owns the monitor lifetime.</param>
    /// <returns>An initialized monitor, including when its first successful snapshot is empty.</returns>
    /// <remarks>
    /// Listeners and the five-second fallback scan maintain all interfaces even without watchers.
    /// Initialization failure or cancellation disposes partial ownership. Discovery remains best-effort:
    /// initialization does not guarantee every OS interface or metadata field was accessible.
    /// Dispose the returned monitor with <c>await using</c>. Share it explicitly where cached tracking is needed.
    /// </remarks>
    public static Task<YubiKeyMonitor> CreateMonitorAsync(CancellationToken cancellationToken = default) =>
        YubiKeyMonitor.CreateAsync(cancellationToken);
}