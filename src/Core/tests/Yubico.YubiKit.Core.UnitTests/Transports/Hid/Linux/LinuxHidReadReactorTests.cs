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
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native.Linux.Libc;
using Yubico.YubiKit.Core.Transports.Hid.Linux;
using Yubico.YubiKit.Core.UnitTests.BoundaryInventory;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux;

[Trait("Category", "RuntimeResilience")]
public partial class LinuxHidReadReactorTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private static TaskCompletionSource Completion() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static Task Wait(Task task) => task.WaitAsync(TimeSpan.FromSeconds(5), Token);

    private static async Task Drain(ILinuxHidReadRegistration registration)
    {
        registration.RequestRemoval(); // fault outcome is independent of actual drain
        await Wait(registration.Quiescence);
        registration.ReleaseReference(); // test caller substitutes for native worker unless tested explicitly
    }

    [Fact]
    public async Task PublishedFailedAdmission_AlwaysReturnsRecoverableRegistration()
    {
        using var native = new ReadinessNative { HoldNextPoll = true };
        ILinuxHidReadReactor reactor = new LinuxHidReadReactor(native);
        using var worker = new OwnerWorker();
        using var first = Descriptor(41);
        int closeThread = 0;
        int closes = 0;
        using var second = Descriptor(42, () => { closeThread = Environment.CurrentManagedThreadId; closes++; });
        string interfaceId = $"l2-published-admission-{Guid.NewGuid()}";
        using var sameNativeOwnerClaim = await DeviceConnectionRegistry.AcquireConnectionAsync([interfaceId], Token);
        var a = reactor.Register(first, 1, (_, _) => { }, _ => { });
        var poll = await native.NextPoll();
        native.Writes.Enqueue(new LinuxIoResult(-1, 9));
        var ownerToken = reactor.Register(second, 2, (_, _) => GC.KeepAlive(sameNativeOwnerClaim), _ => GC.KeepAlive(sameNativeOwnerClaim));
        var initialFailure = await ownerToken.Activation.WaitAsync(TimeSpan.FromSeconds(5), Token);
        Assert.IsType<IOException>(initialFailure);
        Assert.Equal(2, ownerToken.Generation);
        Assert.IsType<IOException>(ownerToken.RequestRemoval());
        Assert.False(ownerToken.Quiescence.IsCompleted);
        Assert.Throws<InvalidOperationException>(ownerToken.ReleaseReference);
        await Assert.ThrowsAsync<ConnectionInUseException>(() => DeviceConnectionRegistry.AcquireConnectionAsync([interfaceId], Token).AsTask());
        Assert.Equal(0, closes);
        await Assert.ThrowsAsync<IOException>(() => reactor.DisposeAsync().AsTask());
        Assert.False(ownerToken.Quiescence.IsCompleted); // faulted shutdown is not actual quiescence
        poll.CompleteWake(); // genuine late poll return, not a timeout/close fallback
        await Wait(ownerToken.Quiescence);
        Assert.Same(initialFailure, await ownerToken.Activation.WaitAsync(TimeSpan.FromSeconds(5), Token));
        await Assert.ThrowsAsync<ConnectionInUseException>(() => DeviceConnectionRegistry.AcquireConnectionAsync([interfaceId], Token).AsTask());
        Assert.Equal(0, closes); // the reactor never releases a device reference, even after quiescence
        await Wait(worker.Invoke(() =>
        {
            ownerToken.ReleaseReference();
            if (second.CloseOnce().Released)
                sameNativeOwnerClaim.Dispose();
        }));
        using var replacementClaim = await DeviceConnectionRegistry.AcquireConnectionAsync([interfaceId], Token);
        Assert.Equal(worker.ThreadId, closeThread);
        Assert.Equal(1, closes);
        ownerToken.ReleaseReference();
        Assert.Equal(1, closes);
        await Drain(a);
    }

    [Fact]
    public async Task HeldWakeCleanup_CannotCreateReplacementResources()
    {
        using var release = new ManualResetEventSlim();
        var entered = Completion();
        using var native = new ReadinessNative
        {
            WakeClose = () =>
        {
            entered.TrySetResult();
            release.Wait(TimeSpan.FromSeconds(5), Token);
        }
        };
        await using var reactor = new LinuxHidReadReactor(native);
        using var first = Descriptor(41);
        var a = reactor.Register(first, 1, (_, _) => { }, _ => { });
        await native.NextPoll();
        await Drain(a);
        await Wait(entered.Task);
        var handles = new List<LinuxFileSafeHandle>();
        var tokens = new List<ILinuxHidReadRegistration>();
        try
        {
            for (int index = 0; index < 40; index++)
            {
                var handle = Descriptor(100 + index);
                handles.Add(handle);
                var token = reactor.Register(handle, index, (_, _) => { }, _ => { });
                tokens.Add(token);
                Assert.False(token.Activation.IsCompleted);
            }
            Assert.Equal(1, native.Created);
            Assert.Equal(0, native.Closed);
            Assert.Equal(0, native.PoolCleanupCalls);
            Assert.Single(native.PollThreads.Distinct());
            Assert.Equal(native.PollThreads.Single(), native.WakeCloseThreads.Single());
            release.Set();
            Assert.Equal(41, (await native.NextPoll()).Fds.Length);
            foreach (var token in tokens)
                Assert.Null(await token.Activation.WaitAsync(TimeSpan.FromSeconds(5), Token));
            Assert.Single(native.PollThreads.Distinct()); // same poller resumes after local close
            Assert.Equal(1, native.MaxWakeHandles);
        }
        finally
        {
            release.Set();
            await reactor.DisposeAsync();
            foreach (var token in tokens)
                await Drain(token);
            foreach (var handle in handles)
                handle.Dispose();
        }
        Assert.Equal(native.Created, native.Closed);
        Assert.Equal(0, native.PoolCleanupCalls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    public async Task RemovalDuringHeldRetirement_KeepsTokenUntilActualQuiescenceWithoutReplacement(int closeError)
    {
        using var release = new ManualResetEventSlim();
        var entered = Completion();
        using var native = new ReadinessNative
        {
            WakeCloseError = closeError,
            WakeClose = () =>
        {
            entered.TrySetResult();
            release.Wait(TimeSpan.FromSeconds(5), Token);
        }
        };
        var reactor = new LinuxHidReadReactor(native);
        using var first = Descriptor(41);
        using var second = Descriptor(42);
        var a = reactor.Register(first, 1, (_, _) => { }, _ => { });
        await native.NextPoll();
        await Drain(a);
        await Wait(entered.Task);
        int callbacks = 0;
        var b = reactor.Register(second, 2, (_, _) => Interlocked.Increment(ref callbacks), _ => { });
        try
        {
            Assert.Null(b.RequestRemoval());
            Assert.False(b.Activation.IsCompleted);
            Assert.False(b.Quiescence.IsCompleted);
            Assert.Throws<InvalidOperationException>(b.ReleaseReference);
            Assert.Equal(1, native.Created);
        }
        finally { release.Set(); }
        Assert.NotNull(await b.Activation.WaitAsync(TimeSpan.FromSeconds(5), Token));
        await Drain(b);
        if (closeError == 0) await reactor.DisposeAsync();
        else await Assert.ThrowsAsync<IOException>(() => reactor.DisposeAsync().AsTask());
        Assert.Equal(0, callbacks);
        Assert.Equal(1, native.Created);
        Assert.Equal(0, native.PoolCleanupCalls);
    }

    [Fact]
    public async Task SignalFailure_IsObservableButDoesNotCompleteActualQuiescence()
    {
        using var native = new ReadinessNative { HoldNextPoll = true };
        var reactor = new LinuxHidReadReactor(native);
        using var handle = Descriptor(42);
        var token = reactor.Register(handle, 1, (_, _) => { }, _ => { });
        var poll = await native.NextPoll();
        native.Writes.Enqueue(new LinuxIoResult(-1, 9));
        Assert.Throws<IOException>(token.Signal);
        Assert.False(token.Quiescence.IsCompleted);
        Assert.IsType<IOException>(token.RequestRemoval());
        poll.CompleteWake();
        await Drain(token);
        await Assert.ThrowsAsync<IOException>(() => reactor.DisposeAsync().AsTask());
    }

    [Fact]
    public async Task HeldOwnerReferenceRelease_DoesNotBlockReactorOrCreateCleanupJobs()
    {
        using var probe = new WithheldNativeProbe();
        using var native = new ReadinessNative();
        using var worker = new OwnerWorker();
        await using var reactor = new LinuxHidReadReactor(native);
        int deviceCloseThread = 0;
        using var first = Descriptor(41, () => { deviceCloseThread = Environment.CurrentManagedThreadId; probe.EnterNative(); });
        using var second = Descriptor(42);
        var a = reactor.Register(first, 1, (_, _) => { }, _ => { });
        await native.NextPoll();
        var ready = Completion();
        var b = reactor.Register(second, 2, (_, _) => ready.TrySetResult(), error => ready.TrySetException(error));
        await native.NextPoll();
        Assert.False(first.CloseOnce().Executed); // deliberately early owner disposal, reference still retained
        Assert.Null(a.RequestRemoval());
        await Wait(a.Quiescence);
        probe.Start(() => worker.Invoke(a.ReleaseReference));
        try
        {
            probe.AssertResponsive();
            Assert.Throws<InvalidOperationException>(a.ReleaseReference); // concurrent release is misuse, not another job
            (await native.NextPoll()).Complete(42, NativeMethods.POLLIN);
            await Wait(ready.Task);
            Assert.Equal(1, native.Created);
            Assert.Equal(0, native.PoolCleanupCalls);
        }
        finally { await probe.ReleaseAndDrainAsync(); }
        Assert.Equal(worker.ThreadId, deviceCloseThread);
        Assert.DoesNotContain(deviceCloseThread, native.PollThreads);
        Assert.True(first.CloseOnce().Released);
        a.ReleaseReference(); // exactly-once completed release is idempotent
        await Drain(b);
    }

    [Fact]
    public async Task ReleaseFromSiblingReactorCallback_IsRejectedEvenAfterQuiescence()
    {
        using var native = new ReadinessNative();
        await using var reactor = new LinuxHidReadReactor(native);
        using var first = Descriptor(41);
        using var second = Descriptor(42);
        var a = reactor.Register(first, 1, (_, _) => { }, _ => { });
        await native.NextPoll();
        var checkedGuard = Completion();
        var b = reactor.Register(second, 2, (_, _) =>
        {
            Assert.Throws<InvalidOperationException>(a.ReleaseReference);
            Assert.Throws<InvalidOperationException>(() => reactor.Register(first, 9, (_, _) => { }, _ => { }));
            checkedGuard.TrySetResult();
        }, error => checkedGuard.TrySetException(error));
        await native.NextPoll();
        a.RequestRemoval();
        await Wait(a.Quiescence);
        (await native.NextPoll()).Complete(42, NativeMethods.POLLIN);
        await Wait(checkedGuard.Task);
        a.ReleaseReference();
        await Drain(b);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Shutdown_RuntimeFailureAfterSuccessfulWakeDoesNotPublishSuccess(bool pollFails)
    {
        using var native = new ReadinessNative { HoldNextPoll = true };
        var reactor = new LinuxHidReadReactor(native);
        using var handle = Descriptor(42);
        var token = reactor.Register(handle, 1, (_, _) => { }, _ => { });
        var poll = await native.NextPoll();
        if (pollFails) poll.Result = new LinuxIoResult(-1, 9);
        else native.Reads.Enqueue(new LinuxIoResult(-1, 9));
        var shutdown = reactor.DisposeAsync().AsTask();
        Assert.False(shutdown.IsCompleted);
        poll.CompleteWake();
        await Assert.ThrowsAsync<IOException>(() => shutdown);
        await Drain(token); // actual quiescence is still observable after failure outcome
    }

    [Fact]
    public async Task CallbackAndTerminalCallback_AreBothIncludedInQuiescence()
    {
        using var native = new ReadinessNative();
        await using var reactor = new LinuxHidReadReactor(native);
        using var handle = Descriptor(42);
        using var callbackRelease = new ManualResetEventSlim();
        using var terminalRelease = new ManualResetEventSlim();
        var entered = Completion();
        var terminal = Completion();
        var token = reactor.Register(handle, 1, (_, _) =>
        {
            entered.TrySetResult();
            callbackRelease.Wait(TimeSpan.FromSeconds(5), Token);
            throw new IOException("fault");
        }, _ =>
        {
            terminal.TrySetResult();
            terminalRelease.Wait(TimeSpan.FromSeconds(5), Token);
        });
        try
        {
            (await native.NextPoll()).Complete(42, NativeMethods.POLLIN);
            await Wait(entered.Task);
            Assert.Null(token.RequestRemoval());
            Assert.False(token.Quiescence.IsCompleted);
            callbackRelease.Set();
            await Wait(terminal.Task);
            Assert.False(token.Quiescence.IsCompleted);
            Assert.Throws<InvalidOperationException>(token.ReleaseReference);
        }
        finally
        {
            callbackRelease.Set();
            terminalRelease.Set();
            await Drain(token);
        }
    }

    [Fact]
    public async Task LateSnapshot_IsRejectedAndDuplicateFdWaitsForOwnerReferenceRelease()
    {
        using var native = new ReadinessNative { HoldNextPoll = true };
        await using var reactor = new LinuxHidReadReactor(native);
        using var first = Descriptor(42);
        int oldCalls = 0;
        var old = reactor.Register(first, 1, (_, _) => Interlocked.Increment(ref oldCalls), _ => { });
        var snapshot = await native.NextPoll();
        old.RequestRemoval();
        Assert.False(old.Quiescence.IsCompleted);
        snapshot.Complete(42, NativeMethods.POLLIN);
        await Wait(old.Quiescence);
        Assert.Equal(0, oldCalls);
        Assert.Throws<InvalidOperationException>(() => reactor.Register(first, 2, (_, _) => { }, _ => { }));
        old.ReleaseReference();
        using var replacement = Descriptor(42);
        var ready = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        var current = reactor.Register(replacement, 200, (generation, _) => ready.TrySetResult(generation), _ => { });
        (await native.NextPoll()).Complete(42, NativeMethods.POLLIN);
        Assert.Equal(200, await ready.Task.WaitAsync(TimeSpan.FromSeconds(5), Token));
        Assert.Equal(0, oldCalls);
        await Drain(current);
    }

    [Fact]
    public async Task ReentrantRemoval_ReturnsWithoutWaitingOnItself()
    {
        using var native = new ReadinessNative();
        await using var reactor = new LinuxHidReadReactor(native);
        using var handle = Descriptor(42);
        ILinuxHidReadRegistration? registration = null;
        var requested = Completion();
        registration = reactor.Register(handle, 3, (_, _) =>
        {
            var token = registration ?? throw new InvalidOperationException();
            Assert.Null(token.RequestRemoval());
            Assert.False(token.Quiescence.IsCompleted);
            requested.TrySetResult();
        }, error => requested.TrySetException(error));
        (await native.NextPoll()).Complete(42, NativeMethods.POLLIN);
        await Wait(requested.Task);
        await Drain(registration);
    }

    [Theory]
    [InlineData(NativeMethods.POLLERR)]
    [InlineData(NativeMethods.POLLHUP)]
    [InlineData(NativeMethods.POLLNVAL)]
    [InlineData(NativeMethods.POLLIN | NativeMethods.POLLHUP)]
    [InlineData(0)] // callback exception rather than native terminal flags
    public async Task TerminalOrCallbackFault_RemovesOnlyAffectedIdentity(short flags)
    {
        using var native = new ReadinessNative { HoldNextPoll = true };
        await using var reactor = new LinuxHidReadReactor(native);
        using var first = Descriptor(41);
        using var second = Descriptor(42);
        int faults = 0;
        var a = reactor.Register(first, 1, (_, _) => throw new IOException("callback fault"), _ =>
        {
            Interlocked.Increment(ref faults);
            throw new IOException("terminal callback fault");
        });
        var oldPoll = await native.NextPoll();
        var ready = Completion();
        var b = reactor.Register(second, 2, (_, _) => ready.TrySetResult(), error => ready.TrySetException(error));
        oldPoll.Complete(41, flags == 0 ? NativeMethods.POLLIN : flags);
        await Wait(a.Quiescence);
        (await native.NextPoll()).Complete(42, NativeMethods.POLLIN);
        await Wait(ready.Task);
        Assert.Equal(1, faults);
        await Drain(a);
        await Drain(b);
    }

    [Theory]
    [InlineData(-1, NativeMethods.EINTR, 4)]
    [InlineData(-1, 9, 1)]
    [InlineData(0, 0, 1)]
    public async Task PersistentPollFailure_ExitsRatherThanSpinning(int count, int errno, int expectedCalls)
    {
        using var native = new ReadinessNative { PollFailure = new LinuxIoResult(count, errno) };
        var reactor = new LinuxHidReadReactor(native);
        using var handle = Descriptor(42);
        var token = reactor.Register(handle, 1, (_, _) => { }, _ => { });
        await Wait(token.Quiescence);
        Assert.Equal(expectedCalls, native.PollCalls);
        await Assert.ThrowsAsync<IOException>(() => reactor.DisposeAsync().AsTask());
        token.ReleaseReference();
    }

    [Fact]
    public async Task SuccessfulPollWithoutFlags_IsTerminal()
    {
        using var native = new ReadinessNative();
        var reactor = new LinuxHidReadReactor(native);
        using var handle = Descriptor(42);
        var token = reactor.Register(handle, 1, (_, _) => { }, _ => { });
        (await native.NextPoll()).Complete(42, 0);
        await Wait(token.Quiescence);
        Assert.Equal(1, native.PollCalls);
        await Assert.ThrowsAsync<IOException>(() => reactor.DisposeAsync().AsTask());
        token.ReleaseReference();
    }

    [Theory]
    [InlineData(8, 0, 1)]
    [InlineData(-1, NativeMethods.EAGAIN, 1)]
    [InlineData(-1, NativeMethods.EINTR, 4)]
    [InlineData(-1, 9, 1)]
    [InlineData(7, 0, 1)]
    public async Task WakeOutcome_IsSeparateFromLateQuiescence(int count, int error, int attempts)
    {
        using var native = new ReadinessNative { HoldNextPoll = true };
        var reactor = new LinuxHidReadReactor(native);
        using var handle = Descriptor(42);
        var token = reactor.Register(handle, 1, (_, _) => { }, _ => { });
        var poll = await native.NextPoll();
        Assert.Null(await token.Activation.WaitAsync(TimeSpan.FromSeconds(5), Token));
        for (int index = 0; index < attempts; index++)
            native.Writes.Enqueue(new LinuxIoResult(count, error));
        bool failed = count != 8 && error != NativeMethods.EAGAIN;
        var outcome = token.RequestRemoval();
        Assert.Equal(failed, outcome is not null);
        Assert.Same(outcome, token.RequestRemoval());
        Assert.Equal(attempts, native.WriteCalls);
        Assert.False(token.Quiescence.IsCompleted);
        Assert.False(handle.CloseOnce().Executed);
        if (failed)
            await Assert.ThrowsAsync<IOException>(() => reactor.DisposeAsync().AsTask());
        poll.CompleteWake();
        await Wait(token.Quiescence);
        Assert.False(handle.CloseOnce().Executed); // no automatic last-reference release
        token.ReleaseReference();
        Assert.True(handle.CloseOnce().Released);
        Assert.Null(await token.Activation.WaitAsync(TimeSpan.FromSeconds(5), Token));
        if (!failed)
            await reactor.DisposeAsync();
    }

    [Theory]
    [InlineData(-1, NativeMethods.EINTR, 4)]
    [InlineData(-1, 9, 1)]
    [InlineData(7, 0, 1)]
    public async Task WakeDrainFailure_IsBoundedAndTerminal(int count, int error, int attempts)
    {
        using var native = new ReadinessNative();
        var reactor = new LinuxHidReadReactor(native);
        using var handle = Descriptor(42);
        var token = reactor.Register(handle, 1, (_, _) => { }, _ => { });
        await native.NextPoll();
        for (int index = 0; index < attempts; index++)
            native.Reads.Enqueue(new LinuxIoResult(count, error));
        token.Signal();
        await Wait(token.Quiescence);
        Assert.Equal(attempts, native.ReadCalls);
        Assert.Equal(1, native.PollCalls);
        await Assert.ThrowsAsync<IOException>(() => reactor.DisposeAsync().AsTask());
        token.ReleaseReference();
    }

    [Theory]
    [InlineData(NativeMethods.POLLERR)]
    [InlineData(NativeMethods.POLLHUP)]
    [InlineData(NativeMethods.POLLNVAL | NativeMethods.POLLIN)]
    public async Task WakeTerminalFlags_AreTerminalEvenWhenReadable(short flags)
    {
        using var native = new ReadinessNative();
        var reactor = new LinuxHidReadReactor(native);
        using var handle = Descriptor(42);
        var token = reactor.Register(handle, 1, (_, _) => { }, _ => { });
        (await native.NextPoll()).Complete(1000, flags);
        await Wait(token.Quiescence);
        Assert.Equal(0, native.ReadCalls);
        await Assert.ThrowsAsync<IOException>(() => reactor.DisposeAsync().AsTask());
        token.ReleaseReference();
    }

    [Fact]
    public async Task InterruptedWakeAndDrainWouldBlock_AreBoundedAndBenign()
    {
        using var native = new ReadinessNative();
        await using var reactor = new LinuxHidReadReactor(native);
        using var handle = Descriptor(42);
        var token = reactor.Register(handle, 1, (_, _) => { }, _ => { });
        await native.NextPoll();
        native.Writes.Enqueue(new LinuxIoResult(-1, NativeMethods.EINTR));
        native.Writes.Enqueue(new LinuxIoResult(-1, NativeMethods.EINTR));
        native.Reads.Enqueue(new LinuxIoResult(-1, NativeMethods.EAGAIN));
        token.Signal();
        await native.NextPoll();
        Assert.Equal(3, native.WriteCalls);
        Assert.Equal(1, native.ReadCalls);
        await Drain(token);
    }

    [Fact]
    public async Task PollInterruption_StillAppliesRemovalBeforeNewSnapshot()
    {
        using var native = new ReadinessNative { HoldNextPoll = true };
        await using var reactor = new LinuxHidReadReactor(native);
        using var handle = Descriptor(42);
        var token = reactor.Register(handle, 1, (_, _) => { }, _ => { });
        var poll = await native.NextPoll();
        token.RequestRemoval();
        poll.Result = new LinuxIoResult(-1, NativeMethods.EINTR);
        poll.CompleteWake();
        await Drain(token);
        Assert.Equal(1, native.PollCalls);
    }

    [Fact]
    public async Task BusyBatch_IsBoundedAndRotates()
    {
        using var native = new ReadinessNative { HoldNextPoll = true };
        await using var reactor = new LinuxHidReadReactor(native);
        var handles = new List<LinuxFileSafeHandle>();
        var registrations = new List<ILinuxHidReadRegistration>();
        var calls = new ConcurrentDictionary<long, int>();
        try
        {
            var first = Descriptor(100);
            handles.Add(first);
            registrations.Add(reactor.Register(first, 0, (generation, _) => calls.AddOrUpdate(generation, 1, (_, old) => old + 1), _ => { }));
            var initial = await native.NextPoll();
            for (int index = 1; index < 40; index++)
            {
                var handle = Descriptor(100 + index);
                handles.Add(handle);
                registrations.Add(reactor.Register(handle, index, (generation, _) => calls.AddOrUpdate(generation, 1, (_, old) => old + 1), _ => { }));
            }
            initial.CompleteWake();
            (await native.NextPoll()).CompleteAll();
            var next = await native.NextPoll();
            Assert.Equal(32, calls.Values.Sum());
            next.CompleteAll();
            await native.NextPoll();
            Assert.Equal(40, calls.Count);
            Assert.Equal(64, calls.Values.Sum());
        }
        finally
        {
            await reactor.DisposeAsync();
            foreach (var registration in registrations) await Drain(registration);
            foreach (var handle in handles) handle.Dispose();
        }
    }

    [Fact]
    public async Task SequentialRegistrations_ExitIdleWithoutAccumulatingResources()
    {
        using var native = new ReadinessNative();
        await using var reactor = new LinuxHidReadReactor(native);
        for (int index = 0; index < 40; index++)
        {
            using var handle = Descriptor(42);
            var token = reactor.Register(handle, index, (_, _) => { }, _ => { });
            await native.NextPoll();
            await Drain(token);
        }
        await reactor.DisposeAsync();
        Assert.Equal(native.Created, native.Closed);
        Assert.Equal(1, native.MaxPollsInFlight);
        Assert.Equal(1, native.MaxWakeHandles);
        Assert.Equal(0, native.PoolCleanupCalls);
    }

    [Fact]
    public async Task FailedWakeRetirement_RetainsResourceAndNeverStartsReplacement()
    {
        using var native = new ReadinessNative { WakeCloseError = 9 };
        var reactor = new LinuxHidReadReactor(native);
        using var handle = Descriptor(42);
        var token = reactor.Register(handle, 1, (_, _) => { }, _ => { });
        await native.NextPoll();
        await Drain(token);
        await Wait(native.AllClosed.Task);
        await Assert.ThrowsAsync<IOException>(() => reactor.DisposeAsync().AsTask());
        Assert.Throws<ObjectDisposedException>(() => reactor.Register(handle, 2, (_, _) => { }, _ => { }));
        Assert.Equal(1, native.Created);
    }

    [Fact]
    public async Task RealPoll_LateOldReadinessCannotReachReplacementAtSameDescriptorNumber()
    {
        if (!OperatingSystem.IsLinux()) return;
        using var probe = new WithheldNativeProbe();
        var native = new ObservedRealNative(probe);
        await using var reactor = new LinuxHidReadReactor(native);
        using var source = NativeMethods.EventFd(1, NativeMethods.EFD_NONBLOCK | NativeMethods.EFD_CLOEXEC);
        using var oldHandle = DuplicateAtHighDescriptor(source);
        int oldFd = oldHandle.DangerousGetHandle().ToInt32();
        int oldCalls = 0;
        var old = reactor.Register(oldHandle, 10, (_, _) => Interlocked.Increment(ref oldCalls), _ => { });
        await Wait(native.Returned.Task);
        probe.Start(() => Drain(old));
        try
        {
            probe.AssertResponsive();
            Assert.False(oldHandle.CloseOnce().Executed);
            Assert.Equal(0, oldCalls);
        }
        finally { await probe.ReleaseAndDrainAsync(); }
        Assert.True(oldHandle.CloseOnce().Released);
        using var replacementSource = NativeMethods.EventFd(0, NativeMethods.EFD_NONBLOCK | NativeMethods.EFD_CLOEXEC);
        using var replacement = DuplicateAtHighDescriptor(replacementSource);
        Assert.Equal(oldFd, replacement.DangerousGetHandle().ToInt32());
        var ready = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reports = new LinuxHidNativeApi();
        var current = reactor.Register(replacement, 11, (generation, flags) =>
        {
            Assert.Equal(NativeMethods.POLLIN, flags);
            reports.Read(replacement, new byte[8]);
            ready.TrySetResult(generation);
        }, error => ready.TrySetException(error));
        Assert.Equal(8, reports.Write(replacement, BitConverter.GetBytes(1UL)).Count);
        Assert.Equal(11, await ready.Task.WaitAsync(TimeSpan.FromSeconds(5), Token));
        Assert.Equal(0, oldCalls);
        await Drain(current);
    }

    [Fact]
    public async Task RealPoll_ControlAndShutdownWakeWithoutPeriodicTimeouts()
    {
        if (!OperatingSystem.IsLinux()) return;
        var native = new ObservedRealNative();
        await using var reactor = new LinuxHidReadReactor(native);
        using var first = NativeMethods.EventFd(0, NativeMethods.EFD_NONBLOCK | NativeMethods.EFD_CLOEXEC);
        using var second = NativeMethods.EventFd(0, NativeMethods.EFD_NONBLOCK | NativeMethods.EFD_CLOEXEC);
        var a = reactor.Register(first, 1, (_, _) => { }, _ => { });
        Assert.Equal(2, (await native.NextPoll()).Length);
        var b = reactor.Register(second, 2, (_, _) => { }, _ => { });
        Assert.Equal(3, (await native.NextPoll()).Length);
        await Drain(b);
        Assert.Equal(2, (await native.NextPoll()).Length);
        await Wait(reactor.DisposeAsync().AsTask());
        await Drain(a);
        Assert.All(native.Wakes, wake => Assert.True(wake.CloseOnce().Released));
        Assert.Throws<ObjectDisposedException>(() => reactor.Register(first, 3, (_, _) => { }, _ => { }));
    }

    private static LinuxFileSafeHandle DuplicateAtHighDescriptor(LinuxFileSafeHandle source)
    {
        int fd = NativeMethods.Fcntl(source, 1030, 1000); // F_DUPFD_CLOEXEC: own allocation, never overwrite stdio
        Assert.True(fd >= 1000);
        var result = new LinuxFileSafeHandle();
        result.Initialize(fd);
        return result;
    }
}