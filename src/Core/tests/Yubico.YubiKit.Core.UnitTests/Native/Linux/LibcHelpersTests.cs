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

using Yubico.YubiKit.Core.Native.Linux.Libc;

namespace Yubico.YubiKit.Core.UnitTests.Native.Linux;

public class LibcHelpersTests
{
    [Theory]
    [InlineData(7, "E2BIG(7): Argument list too long")]
    [InlineData(13, "EACCES(13): Permission denied")]
    [InlineData(11, "EAGAIN(11): No more processes or not enough memory or maximum nesting level reached.")]
    [InlineData(9,
        "EBADF(9): Bad file number. Either the file descriptor is invalid or does not refer to a file, or an attempt to write to a read-only file was made.")]
    [InlineData(16, "EBUSY(16): Device or resource is busy.")]
    [InlineData(10, "ECHILD(10): No spawned processes.")]
    [InlineData(36, "EDEADLK(36): Resource deadlock would occur.")]
    [InlineData(33, "EDOM(33): The argument to a math function is not in the domain of the function.")]
    [InlineData(17, "EEXIST(17): The file or resource already exists.")]
    [InlineData(14, "EFAULT(14): Bad address.")]
    [InlineData(27, "EFBIG(27): File too large.")]
    [InlineData(42, "EILSEQ(42): Illegal sequence of bytes (for example, in an MBCS string).")]
    [InlineData(4, "EINTR(4): Interrupted function.")]
    [InlineData(22, "EINVAL(22): Invalid argument.")]
    [InlineData(5, "EIO(5): I/O error.")]
    [InlineData(21, "EISDIR(21): Object is a directory.")]
    [InlineData(24, "EMFILE(24): Too many open files.")]
    [InlineData(31, "EMLINK(31): Too many links.")]
    [InlineData(38, "ENAMETOOLONG(38): Filename is too long.")]
    [InlineData(23, "ENFILE(23): Too many files open on the system.")]
    [InlineData(19, "ENODEV(19): No such device.")]
    [InlineData(2, "ENOENT(2): No such file or directory.")]
    [InlineData(8, "ENOEXEC(8): Exec format error")]
    [InlineData(39, "ENOLCK(39): No locks available.")]
    [InlineData(12, "ENOMEM(12): Not enough memory is available for the attempted operation.")]
    [InlineData(28, "ENOSPC(28): No space left on the device.")]
    [InlineData(40, "ENOSYS(40): Function not supported.")]
    [InlineData(20, "ENOTDIR(20): Not a directory.")]
    [InlineData(41, "ENOTEMPTY(41): Directory is not empty.")]
    [InlineData(25, "ENOTTY(25): Inappropriate I/O control operation.")]
    [InlineData(6, "ENXIO(6): No such device or address.")]
    [InlineData(1, "EPERM(1): Operation not permitted.")]
    [InlineData(32, "EPIPE(32): Broken pipe.")]
    [InlineData(34, "ERANGE(34): Result too large.")]
    [InlineData(30, "EROFS(30): Read only file system.")]
    [InlineData(29, "ESPIPE(29): Invalid seek.")]
    [InlineData(3, "ESRCH(3): No such process.")]
    [InlineData(18, "EXDEV(18): An attempt was made to move a file to a different device.")]
    [InlineData(80, "STRUNCATE(80): A string copy or concatenation resulted in a truncated string.")]
    [InlineData(9999, "Unmapped error")]
    public void GetErrnoString_ReturnsExpectedMappingForErrno(int errno, string expected)
    {
        string actual = LibcHelpers.GetErrnoString(errno);

        Assert.Equal(expected, actual);
    }
}