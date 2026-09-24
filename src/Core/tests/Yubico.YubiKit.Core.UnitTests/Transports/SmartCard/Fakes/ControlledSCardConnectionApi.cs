// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using Yubico.YubiKit.Core.Native.Desktop.SCard;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Transports.SmartCard.Fakes;

internal sealed class ControlledSCardConnectionApi : ISCardConnectionApi
{
    private readonly ConcurrentQueue<string> _events = new();
    private readonly ConcurrentQueue<int> _nativeThreadIds = new();
    private int _connectCalls;
    private int _beginTransactionCalls;
    private int _disconnectCalls;
    private int _endTransactionCalls;
    private int _releaseContextCalls;
    private int _transmitCalls;
    private nint _releasedContextHandle;
    private nint _establishedContextHandle;
    private nint _connectedContextHandle;

    public TaskCompletionSource FirstTransmitEntered { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskCompletionSource ContextReleased { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ManualResetEventSlim ReleaseTransmit { get; } = new();
    public TaskCompletionSource BeginEntered { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    public ManualResetEventSlim ReleaseBegin { get; } = new();

    public int TransmitCalls => Volatile.Read(ref _transmitCalls);
    public int ConnectCalls => Volatile.Read(ref _connectCalls);
    public int BeginTransactionCalls => Volatile.Read(ref _beginTransactionCalls);
    public int DisconnectCalls => Volatile.Read(ref _disconnectCalls);
    public int EndTransactionCalls => Volatile.Read(ref _endTransactionCalls);
    public int ReleaseContextCalls => Volatile.Read(ref _releaseContextCalls);
    public nint ReleasedContextHandle => Volatile.Read(ref _releasedContextHandle);
    public nint EstablishedContextHandle => Volatile.Read(ref _establishedContextHandle);
    public nint ConnectedContextHandle => Volatile.Read(ref _connectedContextHandle);
    public bool FailNextConnect { get; set; }
    public bool HoldBegin { get; set; }
    public uint EndTransactionResult { get; set; } = ErrorCode.SCARD_S_SUCCESS;
    public uint DisconnectResult { get; set; } = ErrorCode.SCARD_S_SUCCESS;
    public uint ReleaseContextResult { get; set; } = ErrorCode.SCARD_S_SUCCESS;
    public string[] Events => [.. _events];
    public int[] NativeThreadIds => [.. _nativeThreadIds];
    public WeakReference? ContextReference { get; private set; }
    public WeakReference? CardReference { get; private set; }

    public uint EstablishContext(SCARD_SCOPE scope, out SCardContext context)
    {
        Record("establish");
        context = new RecordingSCardContext((nint)1);
        Volatile.Write(ref _establishedContextHandle, context.DangerousGetHandle());
        ContextReference = new WeakReference(context);
        return ErrorCode.SCARD_S_SUCCESS;
    }

    public uint Connect(
        SCardContext context,
        string readerName,
        SCARD_SHARE shareMode,
        SCARD_PROTOCOL preferredProtocols,
        out SCardCardHandle cardHandle,
        out SCARD_PROTOCOL activeProtocol)
    {
        Volatile.Write(ref _connectedContextHandle, context.DangerousGetHandle());
        Record("connect");
        _ = Interlocked.Increment(ref _connectCalls);
        if (FailNextConnect)
        {
            FailNextConnect = false;
            cardHandle = new SCardCardHandle();
            activeProtocol = SCARD_PROTOCOL.Undefined;
            return ErrorCode.SCARD_E_NO_SMARTCARD;
        }

        cardHandle = new RecordingSCardCardHandle((nint)2);
        CardReference = new WeakReference(cardHandle);
        activeProtocol = SCARD_PROTOCOL.T1;
        return ErrorCode.SCARD_S_SUCCESS;
    }

    public uint BeginTransaction(SCardCardHandle cardHandle)
    {
        _ = Interlocked.Increment(ref _beginTransactionCalls);
        Record(HoldBegin ? "begin-enter" : "begin");
        if (HoldBegin)
        {
            BeginEntered.TrySetResult();
            ReleaseBegin.Wait(TestContext.Current.CancellationToken);
            Record("begin-exit");
        }
        return ErrorCode.SCARD_S_SUCCESS;
    }

    public uint EndTransaction(SCardCardHandle cardHandle, SCARD_DISPOSITION disposition)
    {
        Record("end");
        _ = Interlocked.Increment(ref _endTransactionCalls);
        return EndTransactionResult;
    }

    public uint Transmit(
        SCardCardHandle cardHandle,
        SCARD_IO_REQUEST sendPci,
        ReadOnlySpan<byte> sendBuffer,
        Span<byte> receiveBuffer,
        out int bytesReceived)
    {
        var call = Interlocked.Increment(ref _transmitCalls);
        if (call == 1)
        {
            Record("transmit-enter");
            FirstTransmitEntered.TrySetResult();
            ReleaseTransmit.Wait(TestContext.Current.CancellationToken);
            Record("transmit-exit");
        }

        receiveBuffer[0] = 0x90;
        receiveBuffer[1] = 0x00;
        bytesReceived = 2;
        return ErrorCode.SCARD_S_SUCCESS;
    }

    public uint Disconnect(SCardCardHandle cardHandle, SCARD_DISPOSITION disposition)
    {
        Record("disconnect");
        _ = Interlocked.Increment(ref _disconnectCalls);
        return DisconnectResult;
    }

    public uint ReleaseContext(SCardContext context)
    {
        Volatile.Write(ref _releasedContextHandle, context.DangerousGetHandle());
        Record("release-context");
        _ = Interlocked.Increment(ref _releaseContextCalls);
        ContextReleased.TrySetResult();
        return ReleaseContextResult;
    }

    private void Record(string eventName)
    {
        _events.Enqueue(eventName);
        _nativeThreadIds.Enqueue(Environment.CurrentManagedThreadId);
    }
}

internal sealed class RecordingSCardContext(nint handle) : SCardContext(handle)
{
    private static int _releaseHandleCalls;
    public static int ReleaseHandleCalls => Volatile.Read(ref _releaseHandleCalls);
    public static void Reset() => Volatile.Write(ref _releaseHandleCalls, 0);

    protected override bool ReleaseHandle()
    {
        _ = Interlocked.Increment(ref _releaseHandleCalls);
        return true;
    }
}

internal sealed class RecordingSCardCardHandle(nint handle) : SCardCardHandle(handle)
{
    private static int _releaseHandleCalls;
    public static int ReleaseHandleCalls => Volatile.Read(ref _releaseHandleCalls);
    public static void Reset() => Volatile.Write(ref _releaseHandleCalls, 0);

    protected override bool ReleaseHandle()
    {
        _ = Interlocked.Increment(ref _releaseHandleCalls);
        return true;
    }
}