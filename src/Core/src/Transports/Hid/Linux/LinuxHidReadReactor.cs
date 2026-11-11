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

using Yubico.YubiKit.Core.Native.Linux.Libc;

namespace Yubico.YubiKit.Core.Transports.Hid.Linux;

/// <summary>Multiplexes internal nonblocking input readiness on one lazy poller with explicit activation/drain barriers.</summary>
/// <remarks>
/// Retains registration references until explicit owner-worker release after quiescence. Idle runs
/// retire only their private eventfd after poll exits; a failed wake or retirement never fabricates
/// drain or starts overlapping replacement resources. Does not own device close or physical claims.
/// See <see cref="ILinuxHidReadReactor"/> and <see cref="ILinuxHidReadRegistration"/> for consumer obligations.
/// </remarks>
internal sealed class LinuxHidReadReactor : ILinuxHidReadReactor
{
    private const short TerminalFlags = NativeMethods.POLLERR | NativeMethods.POLLHUP | NativeMethods.POLLNVAL;
    private const int BatchSize = 32;
    private static readonly Lazy<LinuxHidReadReactor> SharedInstance = new(() => new());
    [ThreadStatic] private static bool _onReactorThread;
    private readonly Lock _gate = new();
    private readonly ILinuxHidReadinessNative _native;
    // Quiescent tokens stay rooted here until the owner's worker releases their references.
    private readonly List<Registration> _registrations = [];
    private Run? _run;
    private bool _shutdown;
    private Exception? _failure;
    private Task? _shutdownTask;

    /// <summary>Gets the process-shared lazy reactor; its poller exists only while a run needs service.</summary>
    internal static LinuxHidReadReactor Shared => SharedInstance.Value;
    /// <summary>Gets whether this thread is executing any reactor instance, for owner self-drain guards.</summary>
    internal static bool IsReactorThread => _onReactorThread;

    /// <summary>Creates an idle reactor using the borrowed injected syscall seam or the production libc implementation.</summary>
    internal LinuxHidReadReactor(ILinuxHidReadinessNative? native = null) =>
        _native = native ?? new LinuxHidReadinessNative();

    /// <inheritdoc />
    public ILinuxHidReadRegistration Register(LinuxFileSafeHandle handle, long generation,
        Action<long, short> onReady, Action<Exception> onTerminal)
    {
        ArgumentNullException.ThrowIfNull(onReady);
        ArgumentNullException.ThrowIfNull(onTerminal);
        if (_onReactorThread)
            throw new InvalidOperationException("Register on the native owner worker, not a reactor thread.");
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_shutdown, this);
            if (_failure is not null)
                throw new IOException("The read reactor has failed.", _failure);
            using var lease = new LinuxDescriptorLease(handle);
            int descriptor = lease.Descriptor;
            if (_registrations.Any(item => item.Descriptor == descriptor))
                throw new InvalidOperationException("The descriptor is already registered (or its reference is still retained).");
            bool start = _run is null;
            var run = _run ?? new Run(_native.CreateWake());
            Registration registration;
            try
            {
                registration = new Registration(this, handle, descriptor, generation, onReady, onTerminal);
            }
            catch
            {
                if (start)
                    _ = RetireWake(run); // unpublished, local eventfd; no device/reference release
                throw;
            }
            _registrations.Add(registration);
            _run = run;
            // From publication onward the caller always receives this token. In particular,
            // failed wake outcome and eventual native quiescence must remain independently observable.
            if (start)
                StartRun(run);
            else if (!run.Retiring)
            {
                var error = TrySignal(run);
                if (error is not null)
                    _ = registration.Activated.TrySetResult(error);
            }
            return registration;
        }
    }

    private void StartRun(Run run)
    {
        try
        {
            var thread = new Thread(() => Loop(run)) { IsBackground = true, Name = "YubiKit Linux HID reads" };
            thread.Start();
        }
        catch (Exception error)
        {
            // No thread/snapshot ever existed. Keep the published token recoverable even
            // when thread creation fails; only the local eventfd needs synchronous retirement.
            _ = FinishTurn(run, error);
        }
    }

    /// <summary>Permanently stops admission and requests removal, sharing one shutdown outcome across callers.</summary>
    /// <remarks>
    /// Successful completion covers registration quiescence and poller wake retirement, not owner
    /// reference release or device close. Failure may precede actual drain; keep observing each token's
    /// Quiescence independently. A broken wake can leave shutdown pending indefinitely. Invocation
    /// takes the reactor lock; never synchronously wait for shutdown from a reactor callback.
    /// </remarks>
    public ValueTask DisposeAsync()
    {
        lock (_gate)
        {
            if (_shutdownTask is not null)
                return new ValueTask(_shutdownTask);
            _shutdown = true;
            foreach (var registration in _registrations)
                registration.Removing = true;
            if (_run is { Retiring: false } run)
                _ = TrySignal(run);
            _shutdownTask = _failure is not null ? Task.FromException(_failure)
                : Task.WhenAll(_registrations.Select(item => item.Quiescence)
                    .Append(_run?.Completion.Task ?? Task.CompletedTask));
            return new ValueTask(_shutdownTask);
        }
    }

    private void Signal(Registration registration)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(registration.Removing, registration);
            Exception? error = _run is { Retiring: false } run ? TrySignal(run) : _failure;
            if (error is not null)
                throw new IOException("Read reactor wake failed; quiescence is not acknowledged.", error);
        }
    }

    private Exception? Remove(Registration registration)
    {
        lock (_gate)
        {
            if (!registration.RemovalRequested)
            {
                registration.RemovalRequested = true;
                registration.Removing = true;
                registration.RemovalError = _run is { Retiring: false } run ? TrySignal(run) : _failure;
            }
            return registration.RemovalError; // stable outcome, separate from late actual quiescence
        }
    }

    private void Release(Registration registration)
    {
        if (_onReactorThread || !registration.Quiescence.IsCompletedSuccessfully)
            throw new InvalidOperationException("Release the registration reference on its owner worker after quiescence, never on a reactor.");
        int previous = Interlocked.CompareExchange(ref registration.ReleaseState, 1, 0);
        if (previous == 2)
            return;
        if (previous != 0)
            throw new InvalidOperationException("Registration reference release is already in progress or failed; do not retry.");
        try
        {
            // Intentionally synchronous on the calling native owner worker. A last reference
            // may execute device close; no lock, task, queue or reactor thread wraps that call.
            registration.Handle.DangerousRelease();
            lock (_gate)
                _ = _registrations.Remove(registration);
            Volatile.Write(ref registration.ReleaseState, 2);
        }
        catch
        {
            Volatile.Write(ref registration.ReleaseState, 3);
            throw;
        }
    }

    private Exception? TrySignal(Run run)
    {
        try
        {
            TransferWake(() => _native.WriteWake(run.Wake), "write");
        }
        catch (Exception error)
        {
            _failure ??= error;
        }
        return _failure;
    }

    private static void TransferWake(Func<LinuxIoResult> transfer, string operation)
    {
        for (int attempt = 0; attempt < 4; attempt++)
        {
            var result = transfer();
            if (result.Count == 8 || (result.Count == -1 && result.Error == NativeMethods.EAGAIN))
                return;
            if (result.Count == -1 && result.Error == NativeMethods.EINTR && attempt < 3)
                continue;
            throw new IOException($"eventfd {operation} failed (count {result.Count}, errno {result.Error}); no quiescence acknowledgement.");
        }
    }

    private void Loop(Run run)
    {
        _onReactorThread = true;
        try
        {
            bool restart;
            do
            {
                Exception? failure = null;
                try { PollLoop(run); }
                catch (Exception error) { failure = error; }
                restart = FinishTurn(run, failure);
            } while (restart);
        }
        finally
        {
            _onReactorThread = false;
        }
    }

    private void PollLoop(Run run)
    {
        int interruptions = 0;
        int offset = 0;
        while (true)
        {
            Registration[] snapshot;
            lock (_gate)
            {
                QuiesceRemoved();
                if (_failure is not null)
                    throw new IOException("Read reactor failed.", _failure);
                snapshot = [.. _registrations.Where(item => !item.Removing)];
                if (_shutdown || snapshot.Length == 0)
                {
                    run.Retiring = true;
                    return;
                }
                foreach (var registration in snapshot)
                    _ = registration.Activated.TrySetResult(null);
            }
            var descriptors = MakePoll(run, snapshot);
            var result = _native.Poll(descriptors);
            if (RetryInterruptedPoll(result, descriptors, ref interruptions))
                continue;
            CheckWake(run, descriptors[0].revents);
            Dispatch(snapshot, descriptors, offset);
            offset = (offset + BatchSize) % snapshot.Length;
        }
    }

    private static bool RetryInterruptedPoll(LinuxIoResult result, NativeMethods.PollFd[] descriptors, ref int interruptions)
    {
        if (result.Count == -1 && result.Error == NativeMethods.EINTR && ++interruptions < 4)
            return true;
        if (result.Count <= 0)
            throw new IOException($"poll failed (count {result.Count}, errno {result.Error}).");
        if (!descriptors.Any(item => item.revents != 0))
            throw new IOException("poll reported success without readiness flags.");
        interruptions = 0;
        return false;
    }

    private static NativeMethods.PollFd[] MakePoll(Run run, Registration[] snapshot)
    {
        var descriptors = new NativeMethods.PollFd[snapshot.Length + 1];
        descriptors[0] = new NativeMethods.PollFd { fd = run.Descriptor, events = NativeMethods.POLLIN };
        for (int index = 0; index < snapshot.Length; index++)
            descriptors[index + 1] = new NativeMethods.PollFd { fd = snapshot[index].Descriptor, events = NativeMethods.POLLIN };
        return descriptors;
    }

    private void CheckWake(Run run, short flags)
    {
        if ((flags & TerminalFlags) != 0)
            throw new IOException($"Read reactor eventfd terminal flags: {flags}.");
        if ((flags & NativeMethods.POLLIN) != 0)
            TransferWake(() => _native.ReadWake(run.Wake), "read");
    }

    private void Dispatch(Registration[] snapshot, NativeMethods.PollFd[] descriptors, int offset)
    {
        int dispatched = 0;
        for (int step = 0; step < snapshot.Length && dispatched < BatchSize; step++)
        {
            int index = (offset + step) % snapshot.Length;
            var registration = snapshot[index];
            short flags = descriptors[index + 1].revents;
            lock (_gate)
            {
                if (_shutdown || _failure is not null)
                    return;
                if (registration.Removing || flags == 0)
                    continue;
            }
            dispatched++;
            DispatchOne(registration, flags);
        }
    }

    private void DispatchOne(Registration registration, short flags)
    {
        try
        {
            if ((flags & TerminalFlags) != 0)
                throw new IOException($"Read descriptor terminal flags: {flags}.");
            if ((flags & NativeMethods.POLLIN) == 0)
                throw new IOException($"Unexpected read readiness flags: {flags}.");
            registration.OnReady(registration.Generation, flags);
        }
        catch (Exception error)
        {
            lock (_gate)
                registration.Removing = true;
            NotifyTerminal(registration, error);
        }
    }

    private static void NotifyTerminal(Registration registration, Exception error)
    {
        try { registration.OnTerminal(error); }
        catch { /* Internal notification cannot kill siblings. Quiescence follows its return. */ }
    }

    private void QuiesceRemoved()
    {
        foreach (var registration in _registrations.Where(item => item.Removing && !item.Quiescence.IsCompleted))
        {
            _ = registration.Activated.TrySetResult(_failure ?? new ObjectDisposedException("Read registration removed before activation."));
            _ = registration.Quiesced.TrySetResult();
        }
    }

    private bool FinishTurn(Run run, Exception? failure)
    {
        lock (_gate)
        {
            _failure ??= failure;
            run.Retiring = true;
        }
        // A private eventfd is a local notification object, not a hidraw/device-driver
        // close. Retire on this one poller after poll exits (see source evidence in L2 doc).
        bool released = RetireWake(run);
        lock (_gate)
        {
            QuiesceRemoved();
            if (_failure is null && !_shutdown && RestartWakeIfNeeded(run))
                return true;
            if (_failure is null && !_shutdown)
            {
                _run = null;
                _ = run.Completion.TrySetResult();
                return false;
            }
        }
        StopRegistrations();
        lock (_gate)
        {
            if (released)
                _run = null;
            if (_failure is null)
                _ = run.Completion.TrySetResult();
            else
                _ = run.Completion.TrySetException(_failure);
        }
        return false;
    }

    private bool RestartWakeIfNeeded(Run run)
    {
        if (!_registrations.Any(item => !item.Removing))
            return false;
        try
        {
            run.SetWake(_native.CreateWake());
            run.Retiring = false;
            return true; // reuse this thread; never overlap old/new eventfd retirement
        }
        catch (Exception error)
        {
            _failure = error;
            return false;
        }
    }

    private bool RetireWake(Run run)
    {
        var outcome = run.Wake.CloseOnce();
        if (outcome.Released)
            return true;
        lock (_gate)
        {
            _failure ??= new IOException("Read reactor eventfd release is unproven.");
            _run = run; // no replacement; retain the uncertain notification resource
        }
        return false;
    }

    private void StopRegistrations()
    {
        Registration[] terminal;
        lock (_gate)
        {
            terminal = _failure is null ? [] : [.. _registrations.Where(item => !item.Removing)];
            foreach (var registration in terminal)
                registration.Removing = true;
        }
        foreach (var registration in terminal)
            NotifyTerminal(registration, _failure ?? new IOException("Read reactor stopped."));
        lock (_gate)
            QuiesceRemoved();
    }

    /// <summary>One poller run's private notification resource and retirement completion, not a device lifetime.</summary>
    private sealed class Run
    {
        /// <summary>Accepts ownership of the private wake eventfd and captures its descriptor under a temporary lease.</summary>
        internal Run(LinuxFileSafeHandle wake)
        {
            Wake = wake;
            SetWake(wake);
        }
        /// <summary>Gets the owned local eventfd, never a borrowed hidraw descriptor.</summary>
        internal LinuxFileSafeHandle Wake { get; private set; }
        /// <summary>Gets the eventfd number used in this run's poll snapshots.</summary>
        internal int Descriptor { get; private set; }
        /// <summary>Gets or sets whether poll has exited and admissions must await eventfd retirement.</summary>
        internal bool Retiring { get; set; }
        /// <summary>Gets the asynchronously continued outcome of run retirement, independent of owner device release.</summary>
        internal TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>Installs the initial wake or a replacement only after prior notification-resource retirement.</summary>
        internal void SetWake(LinuxFileSafeHandle wake)
        {
            Wake = wake;
            using var lease = new LinuxDescriptorLease(wake);
            Descriptor = lease.Descriptor;
        }
    }

    /// <summary>Unique callback identity retaining one SafeHandle reference until explicit post-quiescence release.</summary>
    private sealed class Registration : ILinuxHidReadRegistration
    {
        private readonly LinuxHidReadReactor _owner;
        /// <summary>Retains the owner's handle and immutable identity/callback metadata before publication.</summary>
        internal Registration(LinuxHidReadReactor owner, LinuxFileSafeHandle handle, int descriptor,
            long generation, Action<long, short> onReady, Action<Exception> onTerminal)
        {
            bool retained = false;
            handle.DangerousAddRef(ref retained);
            Handle = handle;
            Descriptor = descriptor;
            Generation = generation;
            OnReady = onReady;
            OnTerminal = onTerminal;
            _owner = owner;
        }

        /// <inheritdoc />
        public long Generation { get; }
        /// <inheritdoc />
        public Task<Exception?> Activation => Activated.Task;
        /// <inheritdoc />
        public Task Quiescence => Quiesced.Task;
        /// <summary>Gets the owner's handle with this registration's independent retained reference.</summary>
        internal LinuxFileSafeHandle Handle { get; }
        /// <summary>Gets the immutable fd number; reuse is forbidden until this identity's reference is released.</summary>
        internal int Descriptor { get; }
        /// <summary>Gets the bounded internal readiness mechanics, never an application event handler.</summary>
        internal Action<long, short> OnReady { get; }
        /// <summary>Gets the contained terminal notification whose return is part of quiescence.</summary>
        internal Action<Exception> OnTerminal { get; }
        /// <summary>Gets or sets removal eligibility under the reactor lock; this flag alone is not drain acknowledgement.</summary>
        internal bool Removing { get; set; }
        /// <summary>Gets or sets whether an explicit removal request has captured its stable wake outcome.</summary>
        internal bool RemovalRequested { get; set; }
        /// <summary>Gets or sets the first explicit removal wake error, independently of late quiescence.</summary>
        internal Exception? RemovalError { get; set; }
        /// <summary>Exactly-once reference-release state: 0 not started, 1 in progress, 2 completed, 3 failed without retry.</summary>
        internal int ReleaseState;
        /// <summary>Gets the immutable admission-result producer; its task never faults.</summary>
        internal TaskCompletionSource<Exception?> Activated { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>Gets the nonfaulting actual snapshot/ready/terminal-callback drain producer, not close evidence.</summary>
        internal TaskCompletionSource Quiesced { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <inheritdoc />
        public void Signal() => _owner.Signal(this);
        /// <inheritdoc />
        public Exception? RequestRemoval() => _owner.Remove(this);
        /// <inheritdoc />
        public void ReleaseReference() => _owner.Release(this);
    }
}