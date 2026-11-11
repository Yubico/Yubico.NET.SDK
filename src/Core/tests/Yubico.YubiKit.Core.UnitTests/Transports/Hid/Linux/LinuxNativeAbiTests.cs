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

using System.Reflection;
using System.Runtime.InteropServices;
using Yubico.YubiKit.Core.Native.Linux.Libc;
using Yubico.YubiKit.Core.Transports.Hid.Linux;
using Yubico.YubiKit.Core.UnitTests.Transports.SmartCard.Fakes;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux;

public class LinuxNativeAbiTests
{
    [Fact]
    public void NativeImports_PinExactAbiWidthsAndDoNotDuplicateReadWriteDeclarations()
    {
        // Small successful syscalls alone cannot detect an int/ssize_t or int/nfds_t
        // regression. Pin the actual imported ABI as well as exercising real descriptors.
        var signatures = new Dictionary<string, (Type Return, Type[] Parameters)>
        {
            ["open"] = (typeof(int), [typeof(string), typeof(NativeMethods.OpenFlags)]),
            ["eventfd"] = (typeof(int), [typeof(uint), typeof(int)]),
            ["close"] = (typeof(int), [typeof(int)]),
            ["read"] = (typeof(nint), [typeof(int), typeof(byte).MakePointerType(), typeof(nuint)]),
            ["write"] = (typeof(nint), [typeof(int), typeof(byte).MakePointerType(), typeof(nuint)]),
            ["poll"] = (typeof(int), [typeof(NativeMethods.PollFd[]), typeof(nuint), typeof(int)]),
            ["ioctl"] = (typeof(int), [typeof(int), typeof(nuint), typeof(void).MakePointerType()]),
            ["fcntl"] = (typeof(int), [typeof(int), typeof(int), typeof(int)])
        };
        var imports = typeof(NativeMethods).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Select(method => (Method: method, Import: method.GetCustomAttribute<LibraryImportAttribute>()))
            .Where(item => item.Import is not null).ToArray();
        Assert.Equal(signatures.Count, imports.Length);
        foreach (var signature in signatures)
        {
            var (Method, Import) = Assert.Single(imports, item => item.Import?.EntryPoint == signature.Key);
            Assert.Equal(signature.Value.Return, Method.ReturnType);
            Assert.Equal(signature.Value.Parameters, Method.GetParameters().Select(parameter => parameter.ParameterType));
        }
    }

    [Fact]
    public void FailedOpen_PreservesSignedDescriptorAndErrno()
    {
        if (!OperatingSystem.IsLinux())
            return;
        using var handle = NativeMethods.Open("/yubikit-nonexistent-l1/path", NativeMethods.OpenFlags.O_RDONLY);
        int error = Marshal.GetLastPInvokeError();
        Assert.True(handle.IsInvalid);
        Assert.Equal(new IntPtr(-1), handle.DangerousGetHandle());
        Assert.Equal(2, error);
    }

    [Fact]
    public void InvalidEventFdFlags_PreserveSignedDescriptorAndErrno()
    {
        if (!OperatingSystem.IsLinux())
            return;
        using var handle = NativeMethods.EventFd(0, -1);
        int error = Marshal.GetLastPInvokeError();
        Assert.True(handle.IsInvalid);
        Assert.Equal(new IntPtr(-1), handle.DangerousGetHandle());
        Assert.Equal(22, error);
    }

    [Fact]
    public void EventFd_RealNativeReadWritePollAndFcntlRoundTrip()
    {
        if (!OperatingSystem.IsLinux())
            return;
        using var handle = NativeMethods.EventFd(0, NativeMethods.EFD_NONBLOCK | NativeMethods.EFD_CLOEXEC);
        Assert.False(handle.IsInvalid);
        var native = new LinuxHidNativeApi();
        byte[] buffer = new byte[8];
        Assert.Equal(new LinuxIoResult(-1, 11), native.Read(handle, buffer));
        using var lease = new LinuxDescriptorLease(handle);
        var poll = new NativeMethods.PollFd[] { new() { fd = lease.Descriptor, events = NativeMethods.POLLIN } };
        Assert.Equal(0, NativeMethods.Poll(poll, 1, 0));
        Assert.Equal(1, NativeMethods.Fcntl(handle, 1)); // F_GETFD -> FD_CLOEXEC
        Assert.NotEqual(0, NativeMethods.Fcntl(handle, LibcFcntlConstants.F_GETFL) & LibcFcntlConstants.O_NONBLOCK);
        Assert.Equal(new LinuxIoResult(8, 0), native.Write(handle, BitConverter.GetBytes(0x102030405060708UL)));
        Assert.Equal(1, NativeMethods.Poll(poll, 1, 0));
        Assert.Equal(NativeMethods.POLLIN, poll[0].revents);
        Assert.Equal(new LinuxIoResult(8, 0), native.Read(handle, buffer));
        Assert.Equal(0x102030405060708UL, BitConverter.ToUInt64(buffer));
        Assert.Equal(0, NativeMethods.Poll(poll, 1, 0));
        Assert.Equal(new LinuxIoResult(-1, 11), native.Read(handle, buffer));
    }

    [Fact]
    public async Task FdZero_IsValidAndReleasedExactlyOnceInIsolatedChild()
    {
        if (!OperatingSystem.IsLinux())
            return;
        await PcscIsolatedProbe.RunAsync("linux-fd-zero", typeof(LinuxNativeAbiTests),
            nameof(FdZero_IsValidAndReleasedExactlyOnceInIsolatedChild), () =>
            {
                // This executes only in the explicitly selected child, never the runner parent.
                using var stdin = new LinuxFileSafeHandle();
                stdin.Initialize(0);
                Assert.True(stdin.CloseOnce().Released);
                using var first = NativeMethods.Open("/dev/null", NativeMethods.OpenFlags.O_RDWR);
                int firstFd = first.DangerousGetHandle().ToInt32();
                Assert.Equal(0, firstFd);
                Assert.False(first.IsInvalid);
                Assert.Equal(1, NativeMethods.Write(first, [42], 1));
                Assert.True(first.CloseOnce().Released);
                using var second = NativeMethods.Open("/dev/null", NativeMethods.OpenFlags.O_RDWR);
                Assert.Equal(0, second.DangerousGetHandle().ToInt32());
                first.CloseOnce();
                Assert.Equal(1, NativeMethods.Write(second, [43], 1));
                Assert.Equal(0, NativeMethods.Read(second, new byte[1], 1));
                Assert.True(second.CloseOnce().Released);
                return Task.CompletedTask;
            });
    }

    [Fact]
    public void RealFile_ReadWriteCountsAndIoctlErrnoRoundTripThroughProductionAdapter()
    {
        if (!OperatingSystem.IsLinux())
            return;
        string directory = Directory.Exists("/tmp/opencode") ? "/tmp/opencode" : Path.GetTempPath();
        string path = Path.Combine(directory, $"yubikit-l1-{Guid.NewGuid():N}");
        File.WriteAllBytes(path, [1, 2, 3]);
        try
        {
            var native = new LinuxHidNativeApi();
            using (var handle = native.Open(path, NativeMethods.OpenFlags.O_RDWR))
            {
                byte[] buffer = new byte[8];
                Assert.Equal(new LinuxIoResult(3, 0), native.Read(handle, buffer));
                Assert.Equal<byte>([1, 2, 3, 0, 0, 0, 0, 0], buffer);
                Assert.Equal(new LinuxIoResult(2, 0), native.Write(handle, [4, 5]));
                Assert.Equal(new LinuxIoResult(-1, 25), native.Ioctl(handle, 0xC0094807, new byte[9]));
            }
            Assert.Equal<byte>([1, 2, 3, 4, 5], File.ReadAllBytes(path));
            using var readOnly = native.Open(path, NativeMethods.OpenFlags.O_RDONLY);
            Assert.Equal(new LinuxIoResult(-1, 9), native.Write(readOnly, [6]));
        }
        finally
        {
            File.Delete(path);
        }
    }
}