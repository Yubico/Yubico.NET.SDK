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

using Yubico.YubiKit.Core.Native.Linux.Udev;
using Yubico.YubiKit.Core.Transports.Hid.Linux;
using LibcNativeMethods = Yubico.YubiKit.Core.Native.Linux.Libc.NativeMethods;
using UdevNativeMethods = Yubico.YubiKit.Core.Native.Linux.Udev.NativeMethods;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid;

/// <summary>
/// Cross-platform policy tests for the udev monitor descriptor and eventfd shutdown write.
/// Scripted delegates ensure these tests never call Linux libc or require HID hardware.
/// </summary>
[Trait("Category", "RuntimeResilience")]
public class LinuxUdevHidEventSourceTests
{
    [Theory]
    [InlineData(int.MinValue, false)]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(7, true)]
    [InlineData(int.MaxValue, true)]
    public void IsValidFileDescriptor_AcceptsOnlyNonnegativeDescriptors(int descriptor, bool expected)
    {
        Assert.Equal(expected, LinuxUdevHidEventSource.IsValidFileDescriptor(descriptor));
    }

    [Fact]
    public void IsValidFileDescriptor_ZeroExtendedNativeNegativeOne_NarrowsToRejectedInt()
    {
        var nativeIntResult = unchecked((int)0x00000000FFFFFFFFL);

        Assert.Equal(-1, nativeIntResult);
        Assert.False(LinuxUdevHidEventSource.IsValidFileDescriptor(nativeIntResult));
    }

    [Fact]
    public void UdevMonitorGetFd_ExposesSignedIntAbi()
    {
        Func<LinuxUdevMonitorSafeHandle, int> getMonitorFileDescriptor = UdevNativeMethods.udev_monitor_get_fd;

        Assert.NotNull(getMonitorFileDescriptor);
    }

    [Fact]
    public void WriteShutdownSignal_EintrThenFullWrite_RetriesAndSucceeds()
    {
        var script = new ScriptedEventFdWrite(
            (-1, LibcNativeMethods.EINTR),
            (sizeof(ulong), 0));
        var failures = new List<(int Result, int Error)>();

        LinuxUdevHidEventSource.WriteShutdownSignal(
            script.Write,
            script.GetLastError,
            (result, error) => failures.Add((result, error)));

        Assert.Equal(2, script.WriteCalls);
        Assert.Equal(1, script.ErrorReads);
        Assert.Empty(failures);
    }

    [Fact]
    public void WriteShutdownSignal_RepeatedEintr_RetriesEveryInterruption()
    {
        var script = new ScriptedEventFdWrite(
            (-1, LibcNativeMethods.EINTR),
            (-1, LibcNativeMethods.EINTR),
            (-1, LibcNativeMethods.EINTR),
            (sizeof(ulong), 0));
        var failures = new List<(int Result, int Error)>();

        LinuxUdevHidEventSource.WriteShutdownSignal(
            script.Write,
            script.GetLastError,
            (result, error) => failures.Add((result, error)));

        Assert.Equal(4, script.WriteCalls);
        Assert.Equal(3, script.ErrorReads);
        Assert.Empty(failures);
    }

    [Fact]
    public void WriteShutdownSignal_PersistentEintr_StopsAfterFiniteAttemptsAndReportsOnce()
    {
        var script = new ScriptedEventFdWrite(
            (-1, LibcNativeMethods.EINTR),
            (-1, LibcNativeMethods.EINTR),
            (-1, LibcNativeMethods.EINTR),
            (-1, LibcNativeMethods.EINTR));
        var failures = new List<(int Result, int Error)>();

        LinuxUdevHidEventSource.WriteShutdownSignal(
            script.Write,
            script.GetLastError,
            (result, error) => failures.Add((result, error)));

        Assert.Equal(4, script.WriteCalls);
        Assert.Equal(4, script.ErrorReads);
        Assert.Equal([(-1, LibcNativeMethods.EINTR)], failures);
    }

    [Fact]
    public void WriteShutdownSignal_FullWrite_SucceedsWithoutReadingErrno()
    {
        var script = new ScriptedEventFdWrite((sizeof(ulong), 0));
        var failures = new List<(int Result, int Error)>();

        LinuxUdevHidEventSource.WriteShutdownSignal(
            script.Write,
            script.GetLastError,
            (result, error) => failures.Add((result, error)));

        Assert.Equal(1, script.WriteCalls);
        Assert.Equal(0, script.ErrorReads);
        Assert.Empty(failures);
    }

    [Fact]
    public void WriteShutdownSignal_Eagain_IsAcceptedAsAlreadySignaled()
    {
        var script = new ScriptedEventFdWrite((-1, LibcNativeMethods.EAGAIN));
        var failures = new List<(int Result, int Error)>();

        LinuxUdevHidEventSource.WriteShutdownSignal(
            script.Write,
            script.GetLastError,
            (result, error) => failures.Add((result, error)));

        Assert.Equal(1, script.WriteCalls);
        Assert.Equal(1, script.ErrorReads);
        Assert.Empty(failures);
    }

    [Fact]
    public void WriteShutdownSignal_NonretryableError_ReportsOnceWithoutSpinning()
    {
        const int Eio = 5;
        var script = new ScriptedEventFdWrite((-1, Eio));
        var failures = new List<(int Result, int Error)>();

        LinuxUdevHidEventSource.WriteShutdownSignal(
            script.Write,
            script.GetLastError,
            (result, error) => failures.Add((result, error)));

        Assert.Equal(1, script.WriteCalls);
        Assert.Equal(1, script.ErrorReads);
        Assert.Equal([(-1, Eio)], failures);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void WriteShutdownSignal_IncompleteWrite_ReportsOnceWithoutReadingErrno(int bytesWritten)
    {
        var script = new ScriptedEventFdWrite((bytesWritten, 0));
        var failures = new List<(int Result, int Error)>();

        LinuxUdevHidEventSource.WriteShutdownSignal(
            script.Write,
            script.GetLastError,
            (result, error) => failures.Add((result, error)));

        Assert.Equal(1, script.WriteCalls);
        Assert.Equal(0, script.ErrorReads);
        Assert.Equal([(bytesWritten, 0)], failures);
    }

    private sealed class ScriptedEventFdWrite(params (int Result, int Error)[] steps)
    {
        private readonly Queue<(int Result, int Error)> _steps = new(steps);
        private int _lastError;
        private bool _errorPending;

        public int WriteCalls { get; private set; }

        public int ErrorReads { get; private set; }

        public int Write()
        {
            Assert.False(_errorPending, "errno was not captured immediately after the failed write");
            Assert.True(_steps.TryDequeue(out var step), "eventfd write was invoked more times than scripted");

            WriteCalls++;
            _lastError = step.Error;
            _errorPending = step.Result < 0;
            return step.Result;
        }

        public int GetLastError()
        {
            Assert.True(_errorPending, "errno was read without a preceding failed write");
            ErrorReads++;
            _errorPending = false;
            return _lastError;
        }
    }
}