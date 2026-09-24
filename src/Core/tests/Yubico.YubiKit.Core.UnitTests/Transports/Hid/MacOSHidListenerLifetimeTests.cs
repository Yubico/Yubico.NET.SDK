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

using System.Diagnostics;
using System.Runtime.InteropServices;
using Yubico.YubiKit.Core.Transports.Hid.MacOS;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid;

public class MacOSHidListenerLifetimeTests
{
    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public void HeldCallback_StopTimesOutOnce_RetainsGenerationUntilDrain_ThenRestarts()
    {
        var native = new RecordingManager { InvokeCallback = true };
        using var listener = new MacOSHidDeviceListener(native, TimeSpan.FromMilliseconds(150));
        listener.DeviceEvent = _ => { native.InCallback.Set(); native.ContinueCallback.Wait(); };
        listener.Start();
        Assert.True(native.InCallback.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));

        var watch = Stopwatch.StartNew();
        listener.Stop();
        Assert.InRange(watch.ElapsedMilliseconds, 80, 2000);
        Assert.Empty(native.Released);
        Assert.Empty(native.Unscheduled);
        Assert.Equal(0, native.CloseCalls);

        watch.Restart();
        listener.Stop();
        Assert.InRange(watch.ElapsedMilliseconds, 0, 100);
        listener.Start();
        Assert.Equal(1, native.Created);
        Assert.Empty(native.Released);
        Assert.Equal(0, native.CloseCalls);

        native.ContinueCallback.Set();
        Assert.True(native.Cleaned.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Equal(["close", "unschedule", "mode", "loop", "manager"], native.Cleanup);

        native.HoldCallback = false;
        Assert.True(SpinWait.SpinUntil(() => { listener.Start(); return native.Created == 2; }, TimeSpan.FromSeconds(5)));
        Assert.Equal(2, native.Created);
        listener.Stop();
        Assert.Equal(2, native.Unscheduled.Count);
        Assert.Equal(2, native.Released.Count(handle => handle == 1));
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public void CallbackSelfStop_DoesNotJoinItself_AndDrainsBeforeRelease()
    {
        var native = new RecordingManager { HoldCallback = false };
        using var listener = new MacOSHidDeviceListener(native, TimeSpan.FromMilliseconds(150));
        listener.DeviceEvent = _ => listener.Stop();
        native.InvokeCallback = true;
        listener.Start();
        Assert.True(native.Cleaned.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Equal(["close", "unschedule", "mode", "loop", "manager"], native.Cleanup);
        Assert.Single(native.Unscheduled);
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public void FailedUnschedule_QuarantinesGenerationAndRefusesRestart()
    {
        var native = new RecordingManager { HoldCallback = false, FailUnschedule = true };
        using var listener = new MacOSHidDeviceListener(native, TimeSpan.FromMilliseconds(150));
        listener.Start();
        listener.Stop();
        listener.Start();
        Assert.Equal(1, native.Created);
        Assert.Empty(native.Released);
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task ConcurrentDispose_WaitsForSameCallbackDrainAsStop()
    {
        var native = new RecordingManager { InvokeCallback = true };
        var listener = new MacOSHidDeviceListener(native, TimeSpan.FromSeconds(2));
        listener.DeviceEvent = _ => { native.InCallback.Set(); native.ContinueCallback.Wait(); };
        listener.Start();
        Assert.True(native.InCallback.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        var first = Task.Run(listener.Stop, TestContext.Current.CancellationToken);
        Assert.True(native.StopSignaled.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        var second = Task.Run(listener.Dispose, TestContext.Current.CancellationToken);
        try
        {
            Assert.True(SpinWait.SpinUntil(() => native.StopCalls == 2, TimeSpan.FromSeconds(5)));
            Assert.False(await Task.WhenAny(second, Task.Delay(100, TestContext.Current.CancellationToken)) == second);
            Assert.Empty(native.Released);
        }
        finally
        {
            native.ContinueCallback.Set();
        }

        await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(["close", "unschedule", "mode", "loop", "manager"], native.Cleanup);
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task ConcurrentStopAfterDeadline_UsesRemainingBudgetAndLaterDisposeDoesNotWaitAgain()
    {
        var native = new RecordingManager { InvokeCallback = true };
        var listener = new MacOSHidDeviceListener(native, TimeSpan.FromMilliseconds(250));
        listener.DeviceEvent = _ => { native.InCallback.Set(); native.ContinueCallback.Wait(); };
        listener.Start();
        Assert.True(native.InCallback.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        var first = Task.Run(listener.Stop, TestContext.Current.CancellationToken);
        Assert.True(native.StopSignaled.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        var second = Task.Run(listener.Stop, TestContext.Current.CancellationToken);
        try
        {
            Assert.True(SpinWait.SpinUntil(() => native.StopCalls == 2, TimeSpan.FromSeconds(5)));
            Assert.False(await Task.WhenAny(second, Task.Delay(80, TestContext.Current.CancellationToken)) == second);
            await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            var watch = Stopwatch.StartNew();
            listener.Dispose();
            Assert.InRange(watch.ElapsedMilliseconds, 0, 100);
            Assert.Empty(native.Released);
        }
        finally
        {
            native.ContinueCallback.Set();
        }

        Assert.True(native.Cleaned.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public void ReleaseFailure_DoesNotStopAlreadyReleasedRunLoopAgain()
    {
        var native = new RecordingManager { HoldCallback = false, FailManagerRelease = true };
        using var listener = new MacOSHidDeviceListener(native, TimeSpan.FromMilliseconds(150));
        listener.Start();
        listener.Stop();
        listener.Stop();
        Assert.Equal(1, native.Released.Count(handle => handle == 2));
        Assert.Empty(native.StopOnReleasedLoop);
        listener.Start();
        Assert.Equal(1, native.Created);
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public void Start_RegistersAndSchedulesBeforeOpen_ThenRunsAndClosesBeforeRelease()
    {
        var native = new RecordingManager { HoldCallback = false };
        using var listener = new MacOSHidDeviceListener(native, TimeSpan.FromSeconds(2));
        listener.Start();
        Assert.True(native.Running.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        listener.Stop();
        Assert.Equal(["vendor:4176", "enumerate", "matching", "removal", "schedule", "open", "run", "close", "unschedule", "mode", "loop", "manager"], native.Calls);
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public void NonzeroOpen_StillRunsAndClosesAfterCallbackDrain()
    {
        var native = new RecordingManager { OpenResult = unchecked((int)0xE00002C1), InvokeCallback = true };
        using var listener = new MacOSHidDeviceListener(native, TimeSpan.FromSeconds(2));
        listener.DeviceEvent = _ => { native.InCallback.Set(); native.ContinueCallback.Wait(); };
        listener.Start();
        Assert.True(native.InCallback.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Equal(DeviceListenerStatus.Started, listener.Status);
        try
        {
            listener.Stop();
            Assert.Empty(native.Released);
            Assert.Equal(0, native.CloseCalls);
        }
        finally { native.ContinueCallback.Set(); }
        Assert.True(native.Cleaned.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Equal(["close", "unschedule", "mode", "loop", "manager"], native.Cleanup);
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public void NonzeroClose_ContinuesCheckedCleanupAndCanRestart()
    {
        var native = new RecordingManager { HoldCallback = false, CloseResult = unchecked((int)0xE00002C1) };
        using var listener = new MacOSHidDeviceListener(native, TimeSpan.FromSeconds(2));
        listener.Start();
        Assert.True(native.Running.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        listener.Stop();
        Assert.Equal(DeviceListenerStatus.Stopped, listener.Status);
        listener.Start();
        Assert.Equal(2, native.Created);
        Assert.Equal(1, native.CloseCalls);
        Assert.Single(native.Unscheduled);
        Assert.Equal(["close", "unschedule", "mode", "loop", "manager"], native.Cleanup);
        listener.Stop();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("Category", "RuntimeResilience")]
    public void ThrownOpenOrClose_QuarantinesGeneration(bool throwOnOpen)
    {
        var native = new RecordingManager { HoldCallback = false, ThrowOnOpen = throwOnOpen, ThrowOnClose = !throwOnOpen };
        using var listener = new MacOSHidDeviceListener(native, TimeSpan.FromSeconds(2));
        listener.Start();
        Assert.True(SpinWait.SpinUntil(() => throwOnOpen ? listener.Status == DeviceListenerStatus.Error : native.Running.IsSet, TimeSpan.FromSeconds(5)));
        listener.Stop();
        listener.Start();
        Assert.Equal(1, native.Created);
        Assert.Equal(throwOnOpen ? 0 : 1, native.CloseCalls);
        Assert.Empty(native.Released);
    }

    private sealed class RecordingManager : IMacOSHidListenerNative
    {
        private int _created;
        private nint _callback;
        private nint _context;
        private int _stopCalls;
        private int _closeCalls;
        public int Created => Volatile.Read(ref _created);
        public int StopCalls => Volatile.Read(ref _stopCalls);
        public int CloseCalls => Volatile.Read(ref _closeCalls);
        public int OpenResult { get; set; }
        public int CloseResult { get; set; }
        public bool ThrowOnOpen { get; set; }
        public bool ThrowOnClose { get; set; }
        public bool HoldCallback { get; set; } = true;
        public bool FailUnschedule { get; set; }
        public bool FailManagerRelease { get; set; }
        public bool InvokeCallback { get; set; }
        public ManualResetEventSlim InCallback { get; } = new();
        public ManualResetEventSlim ContinueCallback { get; } = new();
        public ManualResetEventSlim Cleaned { get; } = new();
        public ManualResetEventSlim Running { get; } = new();
        public ManualResetEventSlim StopSignaled { get; } = new();
        public List<nint> StopOnReleasedLoop { get; } = [];
        public List<string> Cleanup { get; } = [];
        public List<string> Calls { get; } = [];
        public List<nint> Released { get; } = [];
        public List<nint> Unscheduled { get; } = [];

        public nint CreateManager() { Interlocked.Increment(ref _created); return 1; }
        public void SetDeviceMatching(nint manager, int vendorId) { Calls.Add($"vendor:{vendorId}"); }
        public long[] GetInitialEntryIds(nint manager) { Calls.Add("enumerate"); return []; }
        public long GetEntryId(nint device) => 42;
        public void RegisterMatching(nint manager, nint callback, nint context) { _callback = callback; _context = context; Calls.Add("matching"); }
        public void RegisterRemoval(nint manager, nint callback, nint context) { Calls.Add("removal"); }
        public nint RetainCurrentRunLoop() => 2;
        public nint CreateRunLoopMode() => 3;
        public void Schedule(nint manager, nint loop, nint mode) { Calls.Add("schedule"); }
        public int Open(nint manager) { Calls.Add("open"); if (ThrowOnOpen) throw new InvalidOperationException("open threw"); return OpenResult; }
        public int Close(nint manager) { Interlocked.Increment(ref _closeCalls); Calls.Add("close"); Cleanup.Add("close"); if (ThrowOnClose) throw new InvalidOperationException("close threw"); return CloseResult; }
        public int Run(nint mode, double seconds)
        {
            if (!Running.IsSet) { Calls.Add("run"); Running.Set(); }
            if (InvokeCallback)
            {
                InvokeCallback = false;
                var callback = Marshal.GetDelegateForFunctionPointer<NativeCallback>(_callback);
                callback(_context, 0, 0, 42);
                if (HoldCallback) ContinueCallback.Wait();
            }
            else if (HoldCallback)
            {
                InCallback.Set();
                ContinueCallback.Wait();
            }

            return 3;
        }
        public void StopRunLoop(nint loop)
        {
            if (Released.Contains(loop)) StopOnReleasedLoop.Add(loop);
            Interlocked.Increment(ref _stopCalls);
            StopSignaled.Set();
        }
        public void Unschedule(nint manager, nint loop, nint mode)
        {
            if (FailUnschedule) throw new InvalidOperationException("unschedule failed");
            Unscheduled.Add(manager);
            Cleanup.Add("unschedule");
            Calls.Add("unschedule");
        }
        public void Release(nint handle)
        {
            if (handle == 1 && FailManagerRelease) throw new InvalidOperationException("manager release failed");
            Released.Add(handle);
            Cleanup.Add(handle switch { 3 => "mode", 2 => "loop", _ => "manager" });
            Calls.Add(handle switch { 3 => "mode", 2 => "loop", _ => "manager" });
            if (handle == 1) Cleaned.Set();
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void NativeCallback(nint context, int result, nint sender, nint device);
    }
}
