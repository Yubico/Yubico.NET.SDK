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

using Microsoft.Extensions.Logging.Abstractions;
using System.Runtime.InteropServices;
using Yubico.YubiKit.Core.Native.Desktop.SCard;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.UnitTests.Devices;

namespace Yubico.YubiKit.Core.UnitTests.Transports.SmartCard;

[Collection(DiscoveryWorkerAdmissionCollection.Name)]
public class PcscReaderNameOwnershipTests
{
    [Fact]
    public async Task FindAllAsync_Success_ReleasesEveryNameAndReturnsManagedCopies()
    {
        using var names = new NativeReaderNames();
        var api = new ReaderStatusApi(names);
        var finder = new FindPcscDevices(NullLogger<FindPcscDevices>.Instance, api, names.Allocate, names.Free);

        var devices = await finder.FindAllAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, names.AllocatedCount);
        Assert.Equal(3, names.FreedCount);
        Assert.Empty(names.Outstanding);
        Assert.Equal(0, names.DuplicateFrees);
        Assert.All(api.States, state => Assert.Equal(IntPtr.Zero, state.ReaderName));
        SCARD_READER_STATE.FreeReaderNames(api.States, names.Free);
        Assert.Equal(3, names.FreedCount);
        Assert.Equal(0, names.DuplicateFrees);
        api.ClearNativeAtrs();
        var device = Assert.Single(devices);
        Assert.Equal("Generic NFC Reader", device.ReaderName);
        Assert.NotSame(api.ReaderNames[0], device.ReaderName);
        Assert.Equal(ProductAtrs.YubiKey5Nfc, device.Atr);
        Assert.Equal(PscsConnectionKind.Nfc, device.Kind);
        Assert.True(api.Context.IsClosed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FindAllAsync_NativeFailure_ReleasesEveryNameAndPreservesFailure(bool throws)
    {
        using var names = new NativeReaderNames();
        var api = new ReaderStatusApi(names) { FailStatus = true, ThrowStatus = throws };
        var finder = new FindPcscDevices(NullLogger<FindPcscDevices>.Instance, api, names.Allocate, names.Free);

        if (throws)
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                finder.FindAllAsync(TestContext.Current.CancellationToken));
            Assert.Same(api.StatusException, exception);
        }
        else
        {
            var exception = await Assert.ThrowsAsync<PlatformInteropException>(() =>
                finder.FindAllAsync(TestContext.Current.CancellationToken));
            Assert.Contains($"0x{ErrorCode.SCARD_E_INVALID_HANDLE:X8}", exception.Message, StringComparison.Ordinal);
        }

        Assert.Equal(3, names.AllocatedCount);
        Assert.Equal(3, names.FreedCount);
        Assert.Empty(names.Outstanding);
        Assert.Equal(0, names.DuplicateFrees);
        Assert.All(api.States, state => Assert.Equal(IntPtr.Zero, state.ReaderName));
        Assert.True(api.Context.IsClosed);
    }

    [Fact]
    public async Task FindAllAsync_PartialAllocationFailure_ReleasesEarlierNamesWithoutNativeStatusCall()
    {
        using var names = new NativeReaderNames { FailOnAllocation = 3 };
        var api = new ReaderStatusApi(names);
        var finder = new FindPcscDevices(NullLogger<FindPcscDevices>.Instance, api, names.Allocate, names.Free);

        var exception = await Assert.ThrowsAsync<OutOfMemoryException>(() =>
            finder.FindAllAsync(TestContext.Current.CancellationToken));

        Assert.Same(names.AllocationException, exception);
        Assert.Equal(2, names.AllocatedCount);
        Assert.Equal(2, names.FreedCount);
        Assert.Empty(names.Outstanding);
        Assert.Equal(0, names.DuplicateFrees);
        Assert.Equal(0, api.StatusCalls);
        Assert.True(api.Context.IsClosed);
    }

    private sealed class NativeReaderNames : IDisposable
    {
        private int _allocationAttempts;
        public HashSet<IntPtr> Outstanding { get; } = [];
        public int AllocatedCount { get; private set; }
        public int FreedCount { get; private set; }
        public int DuplicateFrees { get; private set; }
        public int FailOnAllocation { get; init; }
        public OutOfMemoryException AllocationException { get; } = new("Injected reader-name allocation failure.");

        public IntPtr Allocate(string name)
        {
            if (++_allocationAttempts == FailOnAllocation)
                throw AllocationException;

            IntPtr pointer = Marshal.StringToHGlobalAnsi(name);
            Outstanding.Add(pointer);
            AllocatedCount++;
            return pointer;
        }

        public void Free(IntPtr pointer)
        {
            if (!Outstanding.Remove(pointer))
            {
                DuplicateFrees++;
                return;
            }

            Marshal.WriteByte(pointer, (byte)'X');
            Marshal.FreeHGlobal(pointer);
            FreedCount++;
        }

        public void Dispose()
        {
            // Drain leaked allocations even when a regression assertion fails in the red run.
            foreach (IntPtr pointer in Outstanding)
                Marshal.FreeHGlobal(pointer);
            Outstanding.Clear();
        }
    }

    private sealed class ReaderStatusApi(NativeReaderNames names) : ISCardApi
    {
        private static readonly int AtrOffset = (int)Marshal.OffsetOf<SCARD_READER_STATE>(nameof(SCARD_READER_STATE.AnswerToReset));
        public string[] ReaderNames { get; } = ["Generic NFC Reader", "Empty Reader", "Other Card Reader"];
        public SCardContext Context { get; } = new();
        public SCARD_READER_STATE[] States { get; private set; } = [];
        public int StatusCalls { get; private set; }
        public bool FailStatus { get; init; }
        public bool ThrowStatus { get; init; }
        public InvalidOperationException StatusException { get; } = new("Injected native status failure.");

        public uint SCardEstablishContext(SCARD_SCOPE scope, out SCardContext context)
        {
            context = Context;
            return ErrorCode.SCARD_S_SUCCESS;
        }

        public uint SCardListReaders(SCardContext context, string[]? groups, out string[] readerNames)
        {
            readerNames = ReaderNames;
            return ErrorCode.SCARD_S_SUCCESS;
        }

        public uint SCardGetStatusChange(
            SCardContext context, int timeout, SCARD_READER_STATE[] readerStates, int readerStatesCount)
        {
            StatusCalls++;
            States = readerStates;
            Assert.Equal(3, readerStatesCount);
            Assert.Equal(ReaderNames, readerStates.Select(state => state.GetReaderName()));
            Assert.All(readerStates, state => Assert.Contains(state.ReaderName, names.Outstanding));
            Assert.Equal(0, names.FreedCount);
            if (ThrowStatus)
                throw StatusException;
            if (FailStatus)
                return ErrorCode.SCARD_E_INVALID_HANDLE;

            readerStates[0].EventState = (uint)SCARD_STATE.PRESENT;
            readerStates[0].AtrLength = (uint)ProductAtrs.YubiKey5Nfc.Bytes.Length;
            ProductAtrs.YubiKey5Nfc.Bytes.CopyTo(MemoryMarshal.AsBytes(readerStates.AsSpan(0, 1)).Slice(AtrOffset, 36));
            readerStates[1].EventState = (uint)SCARD_STATE.EMPTY;
            readerStates[2].EventState = (uint)SCARD_STATE.PRESENT;
            readerStates[2].AtrLength = 1;
            MemoryMarshal.AsBytes(readerStates.AsSpan(2, 1))[AtrOffset] = 0;
            return ErrorCode.SCARD_S_SUCCESS;
        }

        public void ClearNativeAtrs()
        {
            for (int i = 0; i < States.Length; i++)
            {
                MemoryMarshal.AsBytes(States.AsSpan(i, 1)).Slice(AtrOffset, 36).Clear();
            }
        }

        public uint SCardCancel(SCardContext context) => ErrorCode.SCARD_S_SUCCESS;
    }
}