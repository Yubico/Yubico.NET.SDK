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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Yubico.PlatformInterop
{
    internal static partial class NativeMethods
    {
        [Flags]
        public enum DlOpenFlags
        {
            Lazy = 0x1,
            Now = 0x2,
            BindingMask = Lazy | Now,
            NoLoad = 0x4,
            DeepBind = 0x8,
            Global = 0x100,
            Local = 0x0,
            NoDelete = 0x1000
        }

        private const string Kernel32Dll = "kernel32.dll";
        private const string MacDlLib = "libdl.dylib";
        private const string LinuxDlLib = "libdl.so";

        // Note that the DefaultDllImportSearchPaths attribute is a security best
        // practice on the Windows platform (and required by our analyzer
        // settings). It does not currently have any effect on platforms other
        // than Windows, but is included because of the analyzer and in the hope
        // that it will be supported by these platforms in the future.
        [DllImport(Kernel32Dll, CharSet = CharSet.Unicode)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern SafeWindowsLibraryHandle LoadLibraryEx(string libFilename, IntPtr reserved, int flags);

        [DllImport(Kernel32Dll, CharSet = CharSet.Unicode)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern bool FreeLibrary(IntPtr hModule);

        [DllImport(
            Kernel32Dll, CharSet = CharSet.Ansi, BestFitMapping = false, SetLastError = true, ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern IntPtr GetProcAddress(SafeLibraryHandle hModule, string methodName);

        [SuppressMessage(
            "Globalization", "CA2101:Specify marshaling for P/Invoke string arguments",
            Justification = "macOS uses UTF-8 which is modeled as ANSI by the marshaler")]
        [DllImport(MacDlLib, CharSet = CharSet.Ansi, EntryPoint = "dlopen")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern SafeMacOSLibraryHandle mac_dlopen(string fileName, DlOpenFlags flag);

        [SuppressMessage(
            "Globalization", "CA2101:Specify marshaling for P/Invoke string arguments",
            Justification = "macOS uses UTF-8 which is modeled as ANSI by the marshaler")]
        [DllImport(MacDlLib, EntryPoint = "dlsym")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern IntPtr mac_dlsym(SafeLibraryHandle handle, string symbol);

        [DllImport(MacDlLib, EntryPoint = "dlclose")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern int mac_dlclose(IntPtr handle);

        [SuppressMessage(
            "Globalization", "CA2101:Specify marshaling for P/Invoke string arguments",
            Justification = "Linux uses UTF-8 which is modeled as ANSI by the marshaler")]
        [DllImport(LinuxDlLib, CharSet = CharSet.Ansi, EntryPoint = "dlopen")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern SafeLinuxLibraryHandle linux_dlopen(string fileName, DlOpenFlags flag);

        [SuppressMessage(
            "Globalization", "CA2101:Specify marshaling for P/Invoke string arguments",
            Justification = "Linux uses UTF-8 which is modeled as ANSI by the marshaler")]
        [DllImport(LinuxDlLib, EntryPoint = "dlsym")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern IntPtr linux_dlsym(SafeLibraryHandle handle, string symbol);

        [DllImport(LinuxDlLib, EntryPoint = "dlclose")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern int linux_dlclose(IntPtr handle);
    }
}
