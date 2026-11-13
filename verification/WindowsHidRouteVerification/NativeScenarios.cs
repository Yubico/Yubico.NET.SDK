// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Transports.Hid.Windows;

namespace WindowsHidRouteVerification;

internal static class NativeScenarios
{
    internal static readonly string[] Names = ["pending-read-gc", "output", "early-callback",
        "cancel-retained", "success-cancel-race", "cancel-not-found", "reject-read", "reject-write",
        "reopen-stress", "metadata-default"];

    internal static Task RunAsync(string name, Evidence evidence) => name switch
    {
        "pending-read-gc" => PendingReadAsync(evidence, false),
        "early-callback" => PendingReadAsync(evidence, true),
        "output" => OutputAsync(evidence),
        "cancel-retained" => CancelRetainedAsync(evidence),
        "success-cancel-race" => SuccessCancelRaceAsync(evidence),
        "cancel-not-found" => NotFoundAsync(evidence),
        "reject-read" => RejectAsync(evidence, WindowsHidReportDirection.Input),
        "reject-write" => RejectAsync(evidence, WindowsHidReportDirection.Output),
        "reopen-stress" => ReopenAsync(evidence),
        "metadata-default" => DefaultMetadataAsync(evidence),
        _ => throw new ArgumentException("Unknown scenario.", nameof(name))
    };

    private static async Task PendingReadAsync(Evidence evidence, bool holdReturn)
    {
        using var pipe = new NativePipeFixture();
        using var api = new ObservedReportApi();
        if (holdReturn) api.ReturnSubmit.Reset();
        WindowsFidoHidConnection connection = await pipe.OpenAsync(api);
        Task<ReadOnlyMemory<byte>> read = connection.ReceiveAsync();
        api.PublicOperation = read;
        await api.Submitted.Task;
        evidence.Check("accepted-with-empty-peer", api.SubmissionError == 0 && !api.CallbackArrived.Task.IsCompleted && !read.IsCompleted);
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        evidence.Check("pending-storage-retained", api.Storage is { Length: 65 } && api.RetireCount == 0);
        await pipe.Peer.WriteAsync(NativePipeFixture.Report(0x5a));
        await api.CallbackPublished.Task;
        if (holdReturn)
        {
            evidence.Check("callback-before-submit-return", !read.IsCompleted && api.RetireCount == 0);
            api.ReturnSubmit.Set();
        }
        ReadOnlyMemory<byte> result = await read;
        evidence.Check("native-read-payload", result.Length == 64 && result.Span.IndexOfAnyExcept((byte)0x5a) < 0);
        evidence.Check("exactly-one-success", api.CallbackCount == 1 && api.Completion == new WindowsHidCompletion(0, 65));
        await connection.DisposeAsync();
        CheckReleased(evidence, api);
    }

    private static async Task OutputAsync(Evidence evidence)
    {
        using var pipe = new NativePipeFixture();
        using var api = new ObservedReportApi();
        WindowsFidoHidConnection connection = await pipe.OpenAsync(api);
        var output = new byte[64];
        output.AsSpan().Fill(0x39);
        Task send = connection.SendAsync(output);
        api.PublicOperation = send;
        await send;
        var received = new byte[65];
        await pipe.Peer.ReadExactlyAsync(received);
        evidence.Check("native-output-framing", received[0] == 0 && received.AsSpan(1).IndexOfAnyExcept((byte)0x39) < 0);
        evidence.Check("exactly-one-success", api.CallbackCount == 1 && api.Completion == new WindowsHidCompletion(0, 65));
        await connection.DisposeAsync();
        CheckReleased(evidence, api);
    }

    private static async Task CancelRetainedAsync(Evidence evidence)
    {
        using var pipe = new NativePipeFixture();
        using var api = new ObservedReportApi();
        api.DeliverCallback.Reset();
        WindowsFidoHidConnection connection = await pipe.OpenAsync(api);
        using var cancellation = new CancellationTokenSource();
        Task<ReadOnlyMemory<byte>> read = connection.ReceiveAsync(cancellation.Token);
        api.PublicOperation = read;
        await api.Submitted.Task;
        cancellation.Cancel();
        await api.CancelReturned.Task;
        await api.CallbackArrived.Task;
        Task disposal = connection.DisposeAsync().AsTask();
        evidence.Check("targeted-cancel-requested", api.CancelResult == 0);
        evidence.Check("abort-from-native", api.Completion.ErrorCode == 995 && api.CallbackCount == 1);
        evidence.Check("callback-barrier-retains-storage", !read.IsCompleted && !disposal.IsCompleted && api.RetireCount == 0 && api.CloseCount == 0 && api.Storage is { Length: 65 });
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        api.DeliverCallback.Set();
        bool canceled = false;
        try { await read; }
        catch (OperationCanceledException) { canceled = true; }
        await disposal;
        evidence.Check("public-canceled-before-disposal", canceled && read.IsCompleted && disposal.IsCompletedSuccessfully);
        CheckReleased(evidence, api);
    }

    private static async Task SuccessCancelRaceAsync(Evidence evidence)
    {
        using var pipe = new NativePipeFixture();
        using var api = new ObservedReportApi();
        api.ReturnSubmit.Reset();
        WindowsFidoHidConnection connection = await pipe.OpenAsync(api);
        using var cancellation = new CancellationTokenSource();
        Task<ReadOnlyMemory<byte>> read = connection.ReceiveAsync(cancellation.Token);
        api.PublicOperation = read;
        await api.Submitted.Task;
        cancellation.Cancel();
        await pipe.Peer.WriteAsync(NativePipeFixture.Report(0x42));
        await api.CallbackPublished.Task;
        evidence.Check("cancel-intent-with-native-success", !read.IsCompleted && !api.CancelReturned.Task.IsCompleted);
        api.ReturnSubmit.Set();
        ReadOnlyMemory<byte> result = await read;
        evidence.Check("native-success-wins", result.Span.IndexOfAnyExcept((byte)0x42) < 0 && api.Completion.ErrorCode == 0);
        await connection.DisposeAsync();
        CheckReleased(evidence, api);
    }

    private static async Task NotFoundAsync(Evidence evidence)
    {
        using var pipe = new NativePipeFixture();
        var api = new WindowsHidReportApi(NativePipeFixture.Metadata);
        api.Open(pipe.Path);
        await pipe.Connected;
        var completion = new TaskCompletionSource<WindowsHidCompletion>(TaskCreationOptions.RunContinuationsAsynchronously);
        int callbacks = 0;
        var operation = new WindowsHidOverlappedOperation(WindowsHidReportDirection.Input, new byte[65], (_, result) =>
        {
            Interlocked.Increment(ref callbacks);
            completion.TrySetResult(result);
        });
        // Data ready is an immediate-success candidate, not proof of ReadFile's raw BOOL outcome.
        await pipe.Peer.WriteAsync(NativePipeFixture.Report(0x23));
        WindowsHidSubmission submitted = api.Submit(operation);
        WindowsHidCompletion result = await completion.Task;
        uint cancel = api.Cancel(operation);
        evidence.Observe("normalized-submission-error", submitted.ErrorCode.ToString(System.Globalization.CultureInfo.InvariantCulture));
        evidence.Observe("cancel-error", cancel.ToString(System.Globalization.CultureInfo.InvariantCulture));
        evidence.Check("data-ready-native-success", submitted.ErrorCode == 0 && result == new WindowsHidCompletion(0, 65));
        evidence.Check("completed-request-cancel-not-found", cancel == 1168);
        api.Retire(operation);
        WindowsHidRelease release = api.CloseOnce();
        evidence.Check("callback-tail-retirement", callbacks == 1 && release.NativeReleaseProven && release.ErrorCode == 0);
    }

    private static async Task RejectAsync(Evidence evidence, WindowsHidReportDirection direction)
    {
        using var pipe = new NativePipeFixture();
        var api = new WindowsHidReportApi(NativePipeFixture.Metadata);
        api.Open(pipe.Path);
        await pipe.Connected;
        // Disconnect only the owned server endpoint. The production client SafeHandle remains
        // valid and singly owned; no stale/recycled numeric handle or double-close injection.
        pipe.Peer.Disconnect();
        int callbacks = 0;
        var operation = new WindowsHidOverlappedOperation(direction, new byte[65], (_, _) => Interlocked.Increment(ref callbacks));
        WindowsHidSubmission submission = api.Submit(operation);
        evidence.Check("disconnected-peer-rejected", submission.ErrorCode != 0);
        evidence.Observe("native-rejection-error", submission.ErrorCode.ToString(System.Globalization.CultureInfo.InvariantCulture));
        api.Retire(operation);
        WindowsHidRelease release = api.CloseOnce();
        // Wait for delayed erroneous callbacks while still inside an isolated child watchdog.
        await Task.Delay(100);
        evidence.Check("rejected-no-callback", callbacks == 0);
        evidence.Check("rejected-tracking-retired", release.NativeReleaseProven && release.ErrorCode == 0);
    }

    private static async Task ReopenAsync(Evidence evidence)
    {
        using var pipe = new NativePipeFixture();
        for (int i = 0; i < 100; i++)
        {
            using var api = new ObservedReportApi();
            WindowsFidoHidConnection connection = await pipe.OpenAsync(api);
            Task<ReadOnlyMemory<byte>> read = connection.ReceiveAsync();
            api.PublicOperation = read;
            await api.Submitted.Task;
            await pipe.Peer.WriteAsync(NativePipeFixture.Report(0x17));
            ReadOnlyMemory<byte> result = await read;
            await connection.DisposeAsync();
            if (result.Length != 64 || api.CallbackCount != 1 || api.RetireCount != 1 || !api.Release.NativeReleaseProven || !api.PublicTerminalAtClose)
                throw new InvalidOperationException($"Close/reopen iteration {i} failed.");
            if (i < 99) pipe.PrepareReopen();
        }
        evidence.Check("100-owned-pipe-close-reopen-callback-tails", true);
    }

    private static async Task DefaultMetadataAsync(Evidence evidence)
    {
        using var pipe = new NativePipeFixture();
        var api = new WindowsHidReportApi();
        bool descriptorRejected = false;
        try { api.Open(pipe.Path); }
        catch (PlatformApiException) { descriptorRejected = true; }
        await pipe.Connected;
        evidence.Check("ordinary-construction-queries-hid-metadata", descriptorRejected);
        WindowsHidRelease release = api.CloseOnce();
        evidence.Check("metadata-failure-handle-released", release.NativeReleaseProven && release.ErrorCode == 0);
    }

    private static void CheckReleased(Evidence evidence, ObservedReportApi api)
    {
        evidence.Observe("normalized-submission-error", api.SubmissionError.ToString(System.Globalization.CultureInfo.InvariantCulture));
        evidence.Observe("native-completion", $"error={api.Completion.ErrorCode}; bytes={api.Completion.BytesTransferred}; callbacks={api.CallbackCount}");
        evidence.Check("terminal-storage-cleared", api.Storage is { } storage && storage.AsSpan().IndexOfAnyExcept((byte)0) < 0);
        evidence.Check("retired-and-checked-close", api.RetireCount == 1 && api.CloseCount == 1 && api.Release == new WindowsHidRelease(true, 0));
        evidence.Check("public-terminal-before-native-close", api.PublicTerminalAtClose);
    }
}