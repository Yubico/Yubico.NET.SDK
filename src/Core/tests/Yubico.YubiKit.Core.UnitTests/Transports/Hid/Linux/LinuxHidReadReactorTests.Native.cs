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
using System.Threading.Channels;
using Yubico.YubiKit.Core.Native.Linux.Libc;
using Yubico.YubiKit.Core.Transports.Hid.Linux;
using Yubico.YubiKit.Core.UnitTests.BoundaryInventory;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux;

public partial class LinuxHidReadReactorTests
{
    private static LinuxFileSafeHandle Descriptor(int fd, Action? onClose = null)
    {
        var handle = new LinuxFileSafeHandle(_ =>
        {
            onClose?.Invoke();
            return new LinuxCloseResult(0, 0);
        });
        handle.Initialize(fd);
        return handle;
    }

    private sealed class ReadinessNative : ILinuxHidReadinessNative, IDisposable
    {
        private readonly AutoResetEvent _wake = new(false);
        private readonly Channel<PollCall> _polls = Channel.CreateUnbounded<PollCall>();
        private int _inFlight;
        internal bool HoldNextPoll { get; set; }
        internal LinuxIoResult? PollFailure { get; init; }
        internal ConcurrentQueue<LinuxIoResult> Writes { get; } = new();
        internal ConcurrentQueue<LinuxIoResult> Reads { get; } = new();
        internal int WakeCloseError { get; init; }
        internal Action? WakeClose { get; init; }
        internal int WriteCalls;
        internal int ReadCalls;
        internal int PollCalls;
        internal int Created;
        internal int Closed;
        internal int MaxWakeHandles;
        internal int MaxPollsInFlight;
        internal ConcurrentBag<int> PollThreads { get; } = [];
        internal ConcurrentBag<int> WakeCloseThreads { get; } = [];
        internal int PoolCleanupCalls;
        internal TaskCompletionSource AllClosed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public LinuxFileSafeHandle CreateWake()
        {
            int created = Interlocked.Increment(ref Created);
            MaxWakeHandles = Math.Max(MaxWakeHandles, created - Volatile.Read(ref Closed));
            _wake.Reset();
            var handle = new LinuxFileSafeHandle(_ =>
            {
                WakeCloseThreads.Add(Environment.CurrentManagedThreadId);
                if (Thread.CurrentThread.IsThreadPoolThread)
                    Interlocked.Increment(ref PoolCleanupCalls);
                WakeClose?.Invoke();
                Interlocked.Increment(ref Closed);
                AllClosed.TrySetResult();
                return new LinuxCloseResult(WakeCloseError == 0 ? 0 : -1, WakeCloseError);
            });
            handle.Initialize(1000);
            return handle;
        }

        public LinuxIoResult Poll(NativeMethods.PollFd[] descriptors)
        {
            Interlocked.Increment(ref PollCalls);
            PollThreads.Add(Environment.CurrentManagedThreadId);
            if (PollFailure is { } failure)
                return failure;
            int inFlight = Interlocked.Increment(ref _inFlight);
            MaxPollsInFlight = Math.Max(MaxPollsInFlight, inFlight);
            using var call = new PollCall(descriptors);
            bool hold = HoldNextPoll;
            HoldNextPoll = false;
            _polls.Writer.TryWrite(call);
            try
            {
                if (hold)
                    call.Done.WaitOne(TimeSpan.FromSeconds(10));
                else if (WaitHandle.WaitAny([call.Done, _wake], TimeSpan.FromSeconds(10)) == 1)
                    descriptors[0].revents = NativeMethods.POLLIN;
                if (_wake.WaitOne(0))
                    descriptors[0].revents = NativeMethods.POLLIN;
                return call.Result;
            }
            finally { Interlocked.Decrement(ref _inFlight); }
        }

        public LinuxIoResult WriteWake(LinuxFileSafeHandle wake)
        {
            Interlocked.Increment(ref WriteCalls);
            var result = Writes.TryDequeue(out var next) ? next : new LinuxIoResult(8, 0);
            if (result.Count == 8 || result.Error == NativeMethods.EAGAIN)
                _wake.Set();
            return result;
        }

        public LinuxIoResult ReadWake(LinuxFileSafeHandle wake)
        {
            Interlocked.Increment(ref ReadCalls);
            return Reads.TryDequeue(out var result) ? result : new LinuxIoResult(8, 0);
        }

        internal async Task<PollCall> NextPoll() =>
            await _polls.Reader.ReadAsync(Token).AsTask().WaitAsync(TimeSpan.FromSeconds(5), Token);
        public void Dispose() => _wake.Dispose();
    }

    private sealed class ObservedRealNative(WithheldNativeProbe? heldReturn = null) : ILinuxHidReadinessNative
    {
        private readonly LinuxHidReadinessNative _native = new();
        private readonly Channel<NativeMethods.PollFd[]> _polls = Channel.CreateUnbounded<NativeMethods.PollFd[]>();
        private int _calls;
        internal TaskCompletionSource Returned { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal ConcurrentBag<LinuxFileSafeHandle> Wakes { get; } = [];
        public LinuxFileSafeHandle CreateWake()
        {
            var wake = _native.CreateWake();
            Wakes.Add(wake);
            return wake;
        }
        public LinuxIoResult Poll(NativeMethods.PollFd[] descriptors)
        {
            _polls.Writer.TryWrite(descriptors);
            var result = _native.Poll(descriptors);
            if (Interlocked.Increment(ref _calls) == 1 && heldReturn is not null)
            {
                Returned.TrySetResult();
                heldReturn.EnterNative();
            }
            return result;
        }
        public LinuxIoResult ReadWake(LinuxFileSafeHandle wake) => _native.ReadWake(wake);
        public LinuxIoResult WriteWake(LinuxFileSafeHandle wake) => _native.WriteWake(wake);
        internal async Task<NativeMethods.PollFd[]> NextPoll() =>
            await _polls.Reader.ReadAsync(Token).AsTask().WaitAsync(TimeSpan.FromSeconds(5), Token);
    }

    private sealed class PollCall(NativeMethods.PollFd[] fds) : IDisposable
    {
        internal NativeMethods.PollFd[] Fds { get; } = fds;
        internal ManualResetEvent Done { get; } = new(false);
        internal LinuxIoResult Result { get; set; } = new(1, 0);
        internal void CompleteAll()
        {
            for (int index = 1; index < Fds.Length; index++)
                Fds[index].revents = NativeMethods.POLLIN;
            Done.Set();
        }
        internal void Complete(int fd, short flags)
        {
            for (int index = 0; index < Fds.Length; index++)
                if (Fds[index].fd == fd)
                    Fds[index].revents = flags;
            Done.Set();
        }
        internal void CompleteWake() => Complete(1000, NativeMethods.POLLIN);
        public void Dispose() => Done.Dispose();
    }

    // Test-only stand-in for an already-owned native worker, not a production L3 owner.
    private sealed class OwnerWorker : IDisposable
    {
        private readonly BlockingCollection<(Action Action, TaskCompletionSource Completion)> _work = [];
        private readonly Thread _thread;
        internal OwnerWorker()
        {
            _thread = new Thread(() =>
            {
                foreach (var item in _work.GetConsumingEnumerable())
                {
                    try { item.Action(); item.Completion.TrySetResult(); }
                    catch (Exception error) { item.Completion.TrySetException(error); }
                }
            })
            { IsBackground = true };
            _thread.Start();
        }
        internal int ThreadId => _thread.ManagedThreadId;
        internal Task Invoke(Action action)
        {
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _work.Add((action, completion));
            return completion.Task;
        }
        public void Dispose()
        {
            _work.CompleteAdding();
            if (_thread.Join(TimeSpan.FromSeconds(5)))
                _work.Dispose();
        }
    }
}