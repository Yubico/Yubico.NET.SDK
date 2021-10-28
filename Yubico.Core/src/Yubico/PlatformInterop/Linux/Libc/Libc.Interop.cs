// Copyright 2021 Yubico AB
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

using System;
using System.Runtime.InteropServices;

namespace Yubico.PlatformInterop
{
    // This file contains native methods (P/Invoke) for Linux libc functions.
    // Currently we only need open, close, and ioctl.
    internal static partial class NativeMethods
    {
        public const long HIDIOCGRAWINFO = 0x0000000080084803;
        public const long HIDIOCGRDESCSIZE = 0x0000000080044801;
        public const long HIDIOCGRDESC = 0x0000000090044802;
        public const int InfoSize = 8;
        public const int DescriptorSizeSize = 4;
        public const int DescriptorSize = 4100;

        // Where the fields in the hidraw_devinfo struct are, and how many bytes
        // they are.
        public const int OffsetInfoVendor = 4;
        public const int OffsetInfoProduct = 6;

        // Where the fields in the hidraw_report_descriptor struct are, how many
        // bytes make up the size field, and the number of bytes in the value
        // buffer.
        public const int OffsetDescSize = 0;
        public const int OffsetDescValue = 4;

        [Flags]
        public enum OpenFlags
        {
            O_RDONLY = 0,
            O_WRONLY = 1,
            O_RDWR = 2,
            O_NONBLOCK = 0x800,
        }

        [DllImport(Libraries.LinuxKernelLib, CharSet = CharSet.Ansi, BestFitMapping = false, EntryPoint = "open")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern LinuxFileSafeHandle open(string filename, OpenFlags flag);

        // This will be called from within the SafeHandle class, but should be
        // called by no one else.
        [DllImport(Libraries.LinuxKernelLib, CharSet = CharSet.Ansi, EntryPoint = "close")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern int close(IntPtr handle);

        // The SDK uses ioctl for only three requests: info, descriptor size, and
        // descriptor. All three will be called as follows.
        // Pass in one of the HIDIOCG values as the request. Pass in a buffer
        // that is the corresponding Size bytes long, that will accept the
        // result. For example, to get the RAWINFO, create an IntPtr with space
        // for the size of the data struct that is the output, call the method,
        // then copy the returned data into a managed buffer.
        // Parse the bytes after the call to extract the information, using the
        // Offset const values to find the locations of the data in the buffer
        // where the targets are located.
        [DllImport(Libraries.LinuxKernelLib, CharSet = CharSet.Ansi, EntryPoint = "ioctl")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern int ioctl(LinuxFileSafeHandle handle, long request, IntPtr result);
    }
}
