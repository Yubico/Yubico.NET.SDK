using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Synthetic backend only: no device discovery, IOHIDDevice creation, or physical-driver claim.
unsafe partial class Program
{
    private const string Library = "libhidinput_experiment.dylib";
    private const int Ok = 0, Busy = 1, Timeout = 2, SelfWait = 3, Fault = 4;
    private const int ProbeTimeoutMs = 10000;
    private static Exception? _callbackBoundaryError;

    [LibraryImport(Library, EntryPoint = "hidinput_test_create_with_terminal")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint Create(nuint maxReport, nuint capacity,
        delegate* unmanaged[Cdecl]<nint, byte*, nuint, void> receiver,
        delegate* unmanaged[Cdecl]<nint, int, void> terminal, nint context);
    [LibraryImport(Library, EntryPoint = "hidinput_start")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int Start(nint owner);
    [LibraryImport(Library, EntryPoint = "hidinput_cancel")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void Cancel(nint owner);
    [LibraryImport(Library, EntryPoint = "hidinput_wait_shutdown")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int Wait(nint owner, uint timeoutMs);
    [LibraryImport(Library, EntryPoint = "hidinput_destroy")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int Destroy(nint owner);
    [LibraryImport(Library, EntryPoint = "hidinput_test_inject")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void Inject(nint owner, byte* data, nuint length);
    [LibraryImport(Library, EntryPoint = "hidinput_test_ack")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void Ack(nint owner);
    [LibraryImport(Library, EntryPoint = "hidinput_test_activated_with_registration")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int Activated(nint owner);
    [LibraryImport(Library, EntryPoint = "hidinput_test_remove")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void Remove(nint owner);

    private sealed class Capture
    {
        internal readonly ManualResetEventSlim Entered = new(false);
        internal readonly ManualResetEventSlim Release = new(false);
        internal nint Owner;
        internal int Count;
        internal byte[]? Bytes;
        internal int SelfWaitResult;
        internal Exception? CallbackError;
        internal int TerminalCount, TerminalReason, TerminalAfterReports, TerminalSelfWaitResult;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Terminal(nint context, int reason)
    {
        Capture? capture = null;
        try
        {
            capture = GCHandle.FromIntPtr(context).Target as Capture
                ?? throw new InvalidOperationException("invalid terminal context");
            capture.TerminalSelfWaitResult = Wait(capture.Owner, 0);
            capture.TerminalAfterReports = Volatile.Read(ref capture.Count);
            capture.TerminalReason = reason;
            Interlocked.Increment(ref capture.TerminalCount);
        }
        catch (Exception ex)
        {
            if (capture is null) _callbackBoundaryError = ex;
            else capture.CallbackError = ex;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Receive(nint context, byte* data, nuint length)
    {
        // Never propagate managed exceptions through the C callback boundary.
        Capture? capture = null;
        try
        {
            capture = GCHandle.FromIntPtr(context).Target as Capture
                ?? throw new InvalidOperationException("invalid callback context");
            capture.SelfWaitResult = Wait(capture.Owner, 0);
            Interlocked.Increment(ref capture.Count);
            capture.Entered.Set();
            if (!capture.Release.Wait(ProbeTimeoutMs))
            {
                throw new TimeoutException("callback release timed out");
            }
            capture.Bytes = new ReadOnlySpan<byte>(data, checked((int)length)).ToArray();
        }
        catch (Exception ex)
        {
            if (capture is null) _callbackBoundaryError = ex;
            else { capture.CallbackError = ex; capture.Entered.Set(); }
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static (nint Owner, GCHandle Root, Capture Capture) NewOwner(int maxReport, int capacity, bool blocked)
    {
        Capture capture = new();
        if (!blocked) capture.Release.Set();
        GCHandle root = GCHandle.Alloc(capture);
        nint owner = Create((nuint)maxReport, (nuint)capacity, &Receive, &Terminal, GCHandle.ToIntPtr(root));
        if (owner == 0) { root.Free(); throw new InvalidOperationException("create failed"); }
        capture.Owner = owner;
        Check(Start(owner) == Ok && Activated(owner) == 1, "start/registration failed");
        return (owner, root, capture);
    }

    private static void End(nint owner, GCHandle root, Capture capture, int expected)
    {
        Check(Wait(owner, 2000) == expected, "shutdown result mismatch");
        Check(capture.CallbackError is null && _callbackBoundaryError is null,
            $"callback failed: {capture.CallbackError ?? _callbackBoundaryError}");
        Check(Destroy(owner) == Ok, "destroy failed: retaining callback root");
        root.Free(); // Only successful destroy proves native callback context has been relinquished.
    }

    private static void Lifecycle()
    {
        var (owner, root, capture) = NewOwner(8, 2, blocked: true);
        byte[] source = [42, 2, 3];
        fixed (byte* data = source) Inject(owner, data, (nuint)source.Length);
        Check(capture.Entered.Wait(2000), "callback not entered");
        Array.Fill(source, (byte)99);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Check(capture.SelfWaitResult == SelfWait, "self-wait was not rejected");
        Check(Wait(owner, 0) == Timeout, "shutdown before cancel should time out");
        Cancel(owner);
        Check(Destroy(owner) == Busy, "destroy while callback blocked must be busy");
        Ack(owner);
        Check(Wait(owner, 0) == Timeout && Destroy(owner) == Busy, "ack must not release pending callback");
        capture.Release.Set();
        End(owner, root, capture, Ok);
        Check(capture.Bytes is [42, 2, 3], "native queue did not copy caller buffer");
        Check(capture.Count == 1, "callback count mismatch");
        Check(capture.TerminalCount == 0, "explicit cancellation unexpectedly delivered terminal");
    }

    private static void Isolation()
    {
        var (old, oldRoot, oldCapture) = NewOwner(1, 1, blocked: false);
        Cancel(old); Ack(old);
        Check(Wait(old, 2000) == Ok, "old owner did not shut down");
        var (next, nextRoot, nextCapture) = NewOwner(1, 1, blocked: false);
        byte b = 31;
        Inject(old, &b, 1); // late old event while old owner is still alive
        Inject(next, &b, 1);
        Cancel(next); Ack(next);
        End(next, nextRoot, nextCapture, Ok);
        Check(nextCapture.Count == 1 && nextCapture.Bytes is [31] && oldCapture.Count == 0,
            "late old event leaked into newer owner");
        Check(nextCapture.TerminalCount == 0 && oldCapture.TerminalCount == 0, "unexpected terminal");
        End(old, oldRoot, oldCapture, Ok);
    }

    private static void Overflow()
    {
        var (owner, root, capture) = NewOwner(1, 2, blocked: true);
        byte b = 7;
        Inject(owner, &b, 1);
        Check(capture.Entered.Wait(2000), "first callback not entered");
        Inject(owner, &b, 1);
        Inject(owner, &b, 1); // full queue: fault + cancellation, not an unsafe free
        Ack(owner);
        Check(Wait(owner, 0) == Timeout && Destroy(owner) == Busy, "overflow freed pending callback");
        capture.Release.Set();
        End(owner, root, capture, Fault);
        Check(capture.Count == 2, "accepted reports were not drained");
        Check(capture.TerminalCount == 1 && capture.TerminalReason == 2 &&
            capture.TerminalAfterReports == 2 && capture.TerminalSelfWaitResult == SelfWait,
            "overflow terminal did not follow accepted reports exactly once");
    }

    private static void Removal()
    {
        var (owner, root, capture) = NewOwner(1, 2, blocked: false);
        byte b = 4;
        Inject(owner, &b, 1);
        Remove(owner);
        Remove(owner);
        Ack(owner);
        End(owner, root, capture, Ok);
        Check(capture.Count == 1 && capture.TerminalCount == 1 && capture.TerminalReason == 1 &&
            capture.TerminalAfterReports == 1 && capture.TerminalSelfWaitResult == SelfWait,
            "removal terminal must follow accepted report exactly once");
    }

    private static void Malformed()
    {
        var (owner, root, capture) = NewOwner(1, 1, blocked: false);
        byte* invalid = stackalloc byte[2];
        Inject(owner, invalid, 2);
        Ack(owner);
        End(owner, root, capture, Fault);
        Check(capture.Count == 0 && capture.TerminalCount == 1 && capture.TerminalReason == 3 &&
            capture.TerminalAfterReports == 0 && capture.TerminalSelfWaitResult == SelfWait,
            "malformed report did not yield terminal fault");
    }

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 1)
            {
                switch (args[0])
                {
                    case "--lifecycle": Lifecycle(); break;
                    case "--isolation": Isolation(); break;
                    case "--overflow": Overflow(); break;
                    case "--removal": Removal(); break;
                    case "--malformed": Malformed(); break;
                    default: throw new ArgumentException("unknown probe");
                }
                Console.WriteLine($"PASS {args[0]}");
                return 0;
            }
            Check(args.Length == 0, "run without arguments");
            string libraryPath = Path.Combine(AppContext.BaseDirectory, Library);
            Check(File.Exists(libraryPath), $"missing native artifact: {libraryPath}");
            int passed = 0;
            foreach (string probe in new[] { "--lifecycle", "--isolation", "--overflow", "--removal", "--malformed" })
            {
                using Process child = new();
                child.StartInfo = new ProcessStartInfo(Environment.ProcessPath ?? throw new InvalidOperationException("missing executable"), probe)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                child.Start();
                if (!child.WaitForExit(ProbeTimeoutMs))
                {
                    child.Kill(entireProcessTree: true);
                    child.WaitForExit();
                    throw new TimeoutException($"{probe} exceeded {ProbeTimeoutMs} ms");
                }
                string output = child.StandardOutput.ReadToEnd();
                string error = child.StandardError.ReadToEnd();
                Check(child.ExitCode == 0 && output.Trim() == $"PASS {probe}",
                    $"{probe} failed (exit {child.ExitCode}): {output} {error}");
                passed++;
            }
            Console.WriteLine($"MacOS native input AOT: {passed} passed (synthetic backend); artifact: {libraryPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}
