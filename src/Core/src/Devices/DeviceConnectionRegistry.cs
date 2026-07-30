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

using System.Collections.Concurrent;
using Yubico.YubiKit.Core.Abstractions;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     A held session lease on one interface. Disposing it releases the lease (and any applet the lease
///     selected). Returned by <see cref="DeviceConnectionRegistry.AcquireSessionAsync" />.
/// </summary>
internal interface ISessionLease : IDisposable
{
    /// <summary>
    ///     Records that this lease is about to select <paramref name="applicationId" /> on its interface, and
    ///     refuses the selection if it would deselect an applet another lease is still using.
    /// </summary>
    /// <remarks>
    ///     Only smart-card interfaces select applets, so only <see cref="RegisteredSmartCardConnection" />
    ///     calls this. HID interfaces have no applet identity and are deliberately not keyed by one.
    /// </remarks>
    /// <exception cref="SmartCardAppletConflictException">
    ///     A different lease holds a different applet on this interface.
    /// </exception>
    void SelectApplet(ReadOnlyMemory<byte> applicationId);

    /// <summary>
    ///     Reports that the SELECT recorded by the preceding <see cref="SelectApplet" /> never took effect
    ///     on the card, so the registry gives that claim back. Idempotent, and a no-op when there is no
    ///     unconfirmed claim.
    /// </summary>
    /// <remarks>
    ///     The claim has to be recorded <em>before</em> the SELECT reaches the wire — that is the only
    ///     point at which a conflicting selection can still be refused without having already destroyed
    ///     the holder's applet. The cost of claiming early is that the claim is a prediction, and the
    ///     registry would drift from the card whenever the prediction is wrong. This is the other half:
    ///     every outcome that leaves the card's current application unchanged — an error status word, a
    ///     transport fault, a cancellation — comes back through here.
    /// </remarks>
    void AbandonAppletSelect();
}

/// <summary>
///     Process-wide ownership coordinator per interface device, keyed by <see cref="IYubiKey.DeviceId" />.
///     Normal connections share session ownership; discovery takes a nonblocking exclusive lease. This makes
///     the session/discovery exclusion atomic instead of relying on count/check/recheck timing.
/// </summary>
/// <remarks>
///     <para>
///         Session ownership is acquired before physical connection creation and is released if creation
///         fails or when the wrapping connection is disposed. Discovery holds its exclusive lease across
///         physical connect, device-info exchange, and connection disposal. Waiting sessions have priority
///         over a later discovery attempt so repeated scans cannot starve a session.
///     </para>
///     <para>
///         LIMITATION: in-process only. A different process holding the card can still interfere; that is
///         outside what this registry can see. Keyed by DeviceId string, which is stable across scans while
///         the device stays plugged (reader name / HID path based), so registrations made through devices
///         from one scan are visible to readers created in later scans. Idle coordinator entries are retained
///         for the process lifetime: this is bounded by unique interface IDs observed and avoids unsafe
///         remove/recreate races between lease acquisition and dictionary eviction.
///     </para>
/// </remarks>
internal static class DeviceConnectionRegistry
{
    private static readonly ConcurrentDictionary<string, InterfaceOwnership> Interfaces = new();

    /// <summary>Whether this process currently holds at least one live connection to the interface.</summary>
    public static bool IsInUse(string deviceId) =>
        Interfaces.TryGetValue(deviceId, out var ownership) && ownership.HasSessions;

    /// <summary>
    ///     Whether the interface of <paramref name="device" /> that would serve <paramref name="connection" />
    ///     is in use. Composite members are resolved through
    ///     <see cref="CompositeYubiKey.TryResolveMember" />, the same routing a connect uses, so the check
    ///     matches the interface a read would actually open.
    /// </summary>
    public static bool IsInterfaceInUse(IYubiKey device, ConnectionType connection) =>
        IsInUse(ResolveInterfaceId(device, connection));

    /// <summary>
    ///     The per-interface DeviceId that a connect for <paramref name="connection" /> would register: the
    ///     composite member selected by <see cref="CompositeYubiKey.TryResolveMember" />, or the device's
    ///     own id when it is not a composite or exposes no member for the connection. Sharing that resolver
    ///     with the connect path is what makes this agreement compiler-enforced rather than a convention.
    /// </summary>
    public static string ResolveInterfaceId(IYubiKey device, ConnectionType connection)
    {
        if (device is not CompositeYubiKey composite)
            return device.DeviceId;

        return composite.TryResolveMember(connection, out var member)
            ? member.DeviceId
            : device.DeviceId;
    }

    /// <summary>
    ///     Acquires shared session ownership before physical connection creation. Waits while discovery owns
    ///     the interface; cancellation applies only while waiting.
    /// </summary>
    public static ValueTask<ISessionLease> AcquireSessionAsync(
        string deviceId,
        CancellationToken cancellationToken = default) =>
        GetOwnership(deviceId).AcquireSessionAsync(cancellationToken);

    /// <summary>
    ///     Attempts to acquire exclusive discovery ownership without waiting. Returns <c>null</c> while any
    ///     session owns or is already waiting for the interface.
    /// </summary>
    public static IDisposable? TryAcquireDiscovery(string deviceId) =>
        GetOwnership(deviceId).TryAcquireDiscovery();

    private static InterfaceOwnership GetOwnership(string deviceId) =>
        Interfaces.GetOrAdd(deviceId, static id => new InterfaceOwnership(id));

    private enum LeaseKind
    {
        Session,
        Discovery
    }

    private sealed class InterfaceOwnership(string deviceId)
    {
        private readonly Lock _sync = new();
        private int _sessionCount;
        private int _waitingSessions;
        private bool _discoveryActive;
        private TaskCompletionSource? _discoveryReleased;

        /// <summary>The applet currently selected on this interface, or <c>null</c> when no lease holds one.</summary>
        private byte[]? _selectedApplet;

        /// <summary>How many live leases are relying on <see cref="_selectedApplet" /> staying selected.</summary>
        private int _appletHolders;

        public bool HasSessions
        {
            get
            {
                lock (_sync)
                    return _sessionCount > 0;
            }
        }

        /// <summary>
        ///     Applies the CCID applet-ownership rule for <paramref name="lease" />: selecting the applet the
        ///     interface already holds ref-counts and is allowed; selecting a different one is allowed only
        ///     when this lease is the sole holder (it can disturb nothing but itself) and is otherwise a
        ///     conflict. Called before the SELECT reaches the wire, so a refusal leaves the holder intact.
        ///     The claim stays unconfirmed until the SELECT's outcome is known — see
        ///     <see cref="AbandonAppletSelect" />.
        /// </summary>
        public void SelectApplet(Registration lease, ReadOnlyMemory<byte> applicationId)
        {
            lock (_sync)
            {
                // Evaluated here, not before the lock: this guards the very state the lock protects, and a
                // release read outside it can be stale by the time the claim below lands — leaving an
                // applet held by a lease that no longer exists to release it.
                ObjectDisposedException.ThrowIf(lease.Released, typeof(ISessionLease));

                var previousApplet = _selectedApplet;
                var wasHolder = lease.HoldsApplet;

                // A different applet than the one held is safe only when nobody else is relying on the
                // current selection: either the interface has no applet selected, or this lease is the
                // only holder (a session legitimately switching its own applet, e.g. YubiOtp's
                // Management-then-OTP probe).
                if (previousApplet is null || !previousApplet.AsSpan().SequenceEqual(applicationId.Span))
                {
                    if (_appletHolders > (wasHolder ? 1 : 0))
                        throw new SmartCardAppletConflictException(deviceId, previousApplet, applicationId);

                    _selectedApplet = applicationId.ToArray();
                }

                if (!wasHolder)
                {
                    _appletHolders++;
                    lease.HoldsApplet = true;
                }

                lease.UnconfirmedSelect = (previousApplet, wasHolder);
            }
        }

        /// <summary>
        ///     Undoes exactly this lease's part of an unconfirmed claim, so the registry agrees with a card
        ///     whose current application never changed.
        /// </summary>
        /// <remarks>
        ///     Restoring the previous selection is conditional, and both directions matter. Skipping it
        ///     would leave the registry believing nothing is selected while the card still holds the
        ///     previous applet, and the next lease would be waved through to deselect it. Doing it
        ///     unconditionally would roll back past a lease that joined the new applet while this SELECT
        ///     was in flight and has since made it real on the card. So the previous applet comes back only
        ///     when this lease is the last one left depending on the new one.
        /// </remarks>
        public void AbandonAppletSelect(Registration lease)
        {
            lock (_sync)
            {
                // Released is checked here for the same reason it is checked in SelectApplet, and under the
                // same lock: a reconcile that lands after disposal has nothing of its own left to undo,
                // because Release already gave the whole claim back. ReleaseApplet's own holder check makes
                // the holder count safe either way; what this keeps true is the stronger invariant that no
                // applet is recorded as selected once the last holder is gone.
                if (lease.Released || lease.UnconfirmedSelect is not { } claim)
                    return;

                lease.UnconfirmedSelect = null;
                if (!claim.WasHolder)
                    ReleaseApplet(lease);

                if (_appletHolders == (lease.HoldsApplet ? 1 : 0))
                    _selectedApplet = claim.PreviousApplet;
            }
        }

        private void ReleaseApplet(Registration lease)
        {
            if (!lease.HoldsApplet)
                return;

            lease.HoldsApplet = false;
            if (--_appletHolders == 0)
                _selectedApplet = null;
        }

        public async ValueTask<ISessionLease> AcquireSessionAsync(CancellationToken cancellationToken)
        {
            Task? discoveryReleased = null;
            lock (_sync)
            {
                if (!_discoveryActive)
                {
                    _sessionCount++;
                    return new Registration(this, LeaseKind.Session);
                }

                _waitingSessions++;
                discoveryReleased = _discoveryReleased?.Task
                    ?? throw new InvalidOperationException("Discovery ownership has no release signal.");
            }

            try
            {
                await discoveryReleased.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                lock (_sync)
                    _waitingSessions--;
                throw;
            }

            lock (_sync)
            {
                _waitingSessions--;
                if (_discoveryActive)
                    throw new InvalidOperationException("Discovery ownership was reacquired ahead of a waiting session.");

                _sessionCount++;
                return new Registration(this, LeaseKind.Session);
            }
        }

        public IDisposable? TryAcquireDiscovery()
        {
            lock (_sync)
            {
                if (_discoveryActive || _sessionCount > 0 || _waitingSessions > 0)
                    return null;

                _discoveryActive = true;
                _discoveryReleased = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                return new Registration(this, LeaseKind.Discovery);
            }
        }

        public void Release(Registration lease, LeaseKind kind)
        {
            TaskCompletionSource? releaseSignal = null;
            lock (_sync)
            {
                // Release is one-shot, and the flag lives under this lock rather than in an interlocked
                // field on the lease so that every read of it — here and in the applet paths — sees a
                // value that cannot change before the state it guards is touched.
                if (lease.Released)
                    return;

                lease.Released = true;
                lease.UnconfirmedSelect = null;

                if (kind == LeaseKind.Session)
                {
                    if (_sessionCount <= 0)
                        throw new InvalidOperationException("Session ownership was released without a matching acquisition.");

                    ReleaseApplet(lease);
                    _sessionCount--;
                    return;
                }

                if (!_discoveryActive)
                    throw new InvalidOperationException("Discovery ownership was released without a matching acquisition.");

                _discoveryActive = false;
                releaseSignal = _discoveryReleased;
                _discoveryReleased = null;
            }

            releaseSignal?.TrySetResult();
        }
    }

    /// <summary>
    ///     A lease is pure state plus forwarding. Every one of these members — including the released flag
    ///     that gates the others — is read and written only inside the owning
    ///     <see cref="InterfaceOwnership" />'s lock, the same lock that protects the interface state they
    ///     describe. The lease deliberately owns no synchronization of its own: an interlocked or volatile
    ///     flag here could be read before the lock and act on a value that is already stale by the time the
    ///     state it guards is touched, which is exactly the race this shape removes.
    /// </summary>
    private sealed class Registration(InterfaceOwnership ownership, LeaseKind kind) : ISessionLease
    {
        /// <summary>Whether this lease has been released. Set once, by <see cref="InterfaceOwnership.Release" />.</summary>
        public bool Released { get; set; }

        /// <summary>Whether this lease is one of the holders of its interface's currently selected applet.</summary>
        public bool HoldsApplet { get; set; }

        /// <summary>
        ///     What the interface looked like before this lease's most recent, still-unconfirmed SELECT:
        ///     the applet that was selected, and whether this lease was already a holder. Everything
        ///     <see cref="InterfaceOwnership.AbandonAppletSelect" /> needs to hand that claim back.
        /// </summary>
        public (byte[]? PreviousApplet, bool WasHolder)? UnconfirmedSelect { get; set; }

        public void SelectApplet(ReadOnlyMemory<byte> applicationId) =>
            ownership.SelectApplet(this, applicationId);

        public void AbandonAppletSelect() => ownership.AbandonAppletSelect(this);

        public void Dispose() => ownership.Release(this, kind);
    }
}