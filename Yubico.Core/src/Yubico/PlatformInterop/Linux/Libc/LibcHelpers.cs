// Copyright 2025 Yubico AB
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

using System.Runtime.InteropServices;

namespace Yubico.PlatformInterop;

public static class LibcHelpers
{
    public static string GetErrnoString() =>
        Marshal.GetLastWin32Error() switch
        {
            7 => "E2BIG(7): Argument list too long",
            13 => "EACCES(13): Permission denied",
            11 => "EAGAIN(11): No more processes or not enough memory or maximum nesting level reached.",
            9 =>
                "EBADF(9): Bad file number. Either the file descriptor is invalid or does not refer to a file, or an attempt to write to a read-only file was made.",
            16 => "EBUSY(16): Device or resource is busy.",
            10 => "ECHILD(10): No spawned processes.",
            36 => "EDEADLK(36): Resource deadlock would occur.",
            33 => "EDOM(33): The argument to a math function is not in the domain of the function.",
            17 => "EEXIST(17): The file or resource already exists.",
            14 => "EFAULT(14): Bad address.",
            27 => "EFBIG(27): File too large.",
            42 => "EILSEQ(42): Illegal sequence of bytes (for example, in an MBCS string).",
            4 => "EINTR(4): Interrupted function.",
            22 => "EINVAL(22): Invalid argument.",
            5 => "EIO(5): I/O error.",
            21 => "EISDIR(21): Object is a directory.",
            24 => "EMFILE(24): Too many open files.",
            31 => "EMLINK(31): Too many links.",
            38 => "ENAMETOOLONG(38): Filename is too long.",
            23 => "ENFILE(23): Too many files open on the system.",
            19 => "ENODEV(19): No such device.",
            2 => "ENOENT(2): No such file or directory.",
            8 => "ENOEXEC(8): Exec format error",
            39 => "ENOLCK(39): No locks available.",
            12 => "ENOMEM(12): Not enough memory is available for the attempted operation.",
            28 => "ENOSPC(28): No space left on the device.",
            40 => "ENOSYS(40): Function not supported.",
            20 => "ENOTDIR(20): Not a directory.",
            41 => "ENOTDIR(41): Directory is not empty.",
            25 => "ENOTTY(25): Inappropriate I/O control operation.",
            6 => "ENXIO(6): No such device or address.",
            1 => "EPERM(1): Operation not permitted.",
            32 => "EPIPE(32): Broken pipe.",
            34 => "ERANGE(34): Result too large.",
            30 => "EROFS(30): Read only file system.",
            29 => "ESPIPE(29): Invalid seek.",
            3 => "ESRCH(3): No such process.",
            18 => "EXDEV(18): An attempt was made to move a file to a different device.",
            80 => "STRUNCATE(80): A string copy or concatenation resulted in a truncated string.",
            _ => "Unmapped error"
        };
}
