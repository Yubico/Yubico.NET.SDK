// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux;

internal static class LinuxWorkerFailureTestSetup
{
    internal const string CleanupFailureKey = "LinuxWorkerCleanupFailure";

    internal static async Task<(Task Opening, Thread Worker)> InterruptBeforeOpenDispatchAsync(object owner, CancellationToken token, Action? afterStart = null)
    {
        var type = owner.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var sync = Assert.IsType<Lock>(type.GetField("_sync", flags)?.GetValue(owner));
        var workField = type.GetField("_work", flags) ?? throw new InvalidOperationException("Missing owner work slot.");
        var start = type.GetMethod("StartOpen", flags) ?? throw new InvalidOperationException("Missing owner startup method.");
        var startupDispatched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task opening;
        Thread worker;
        Action open;
        using (sync.EnterScope())
        {
            opening = Assert.IsType<Task>(start.Invoke(owner, ["fake", token]), exactMatch: false);
            worker = Assert.IsType<Thread>(type.GetField("_worker", flags)?.GetValue(owner));
            open = Assert.IsType<Action>(workField.GetValue(owner));
            // Let the real WorkLoop consume StartOpen's wake without dispatching native
            // open. Keep the actual accepted open completion untouched in its own slot.
            workField.SetValue(owner, (Action)(() => startupDispatched.TrySetResult()));
            afterStart?.Invoke();
        }

        await startupDispatched.Task.WaitAsync(TimeSpan.FromSeconds(5), token);
        using (sync.EnterScope())
        {
            // Restore the exact production delegate, but withhold its wake. The worker
            // must reach the unsignaled WaitOne before it can dispatch open, regardless
            // of whether Interrupt arrives before or during that wait. ThreadState alone
            // cannot prove this: startup/lock waits also report WaitSleepJoin.
            Assert.Null(workField.GetValue(owner));
            Assert.False(opening.IsCompleted);
            workField.SetValue(owner, open);
            worker.Interrupt();
        }

        return (opening, worker);
    }

    internal static async Task DrainAsync(object owner, Func<Task> shutdown, Exception? primaryFailure)
    {
        Exception? cleanupFailure = null;
        try
        {
            // Runner cancellation must not abandon the original worker. This finite
            // teardown watchdog is separate from the test's operation/placement waits.
            await shutdown().WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        }
        catch (ThreadInterruptedException) { } // Expected injected worker-loop outcome.
        catch (Exception error) { cleanupFailure = error; }
        finally
        {
            try
            {
                // Recover independently: setup can throw before returning its tuple.
                var worker = owner.GetType().GetField("_worker", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(owner) as Thread;
                if (worker is not null && (worker.ThreadState & ThreadState.Unstarted) == 0)
                {
                    // Shutdown completion precedes WorkLoop's final event retirement;
                    // allow a separate bounded join, even if shutdown itself faulted.
                    Assert.True(worker.Join(TimeSpan.FromSeconds(5)), "Original Linux worker did not retire during cleanup.");
                }
            }
            catch (Exception error)
            {
                cleanupFailure = cleanupFailure is null ? error : new AggregateException(cleanupFailure, error);
            }
        }

        if (cleanupFailure is null) return;
        if (primaryFailure is not null) primaryFailure.Data[CleanupFailureKey] = cleanupFailure;
        else ExceptionDispatchInfo.Capture(cleanupFailure).Throw();
    }
}