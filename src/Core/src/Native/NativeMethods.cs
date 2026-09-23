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

namespace Yubico.YubiKit.Core.Native;

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

    // Windows

    // Note that the DefaultDllImportSearchPaths attribute is a security best
    // practice on the Windows platform (and required by our analyzer
    // settings). It does not currently have any effect on platforms other
    // than Windows, but is included because of the analyzer and in the hope
    // that it will be supported by these platforms in the future.
    [LibraryImport(Kernel32Dll, StringMarshalling = StringMarshalling.Utf16)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static partial SafeWindowsLibraryHandle LoadLibraryEx(string libFilename, IntPtr reserved, int flags);

    [LibraryImport(Kernel32Dll)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool FreeLibrary(IntPtr hModule);

    [LibraryImport(Kernel32Dll, StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static partial IntPtr GetProcAddress(SafeLibraryHandle hModule, string methodName);


    // MacOS

    [LibraryImport(MacDlLib, EntryPoint = "dlopen", StringMarshalling = StringMarshalling.Utf8)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    public static partial SafeMacOSLibraryHandle mac_dlopen(string fileName, DlOpenFlags flag);

    [LibraryImport(MacDlLib, EntryPoint = "dlsym", StringMarshalling = StringMarshalling.Utf8)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    public static partial IntPtr mac_dlsym(SafeLibraryHandle handle, string symbol);

    [LibraryImport(MacDlLib, EntryPoint = "dlclose")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    public static partial int mac_dlclose(IntPtr handle);

    // Linux

    [LibraryImport(LinuxDlLib, EntryPoint = "dlopen", StringMarshalling = StringMarshalling.Utf8)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    public static partial SafeLinuxLibraryHandle linux_dlopen(string fileName, DlOpenFlags flag);

    [LibraryImport(LinuxDlLib, EntryPoint = "dlsym", StringMarshalling = StringMarshalling.Utf8)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    public static partial IntPtr linux_dlsym(SafeLibraryHandle handle, string symbol);

    [LibraryImport(LinuxDlLib, EntryPoint = "dlclose")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    public static partial int linux_dlclose(IntPtr handle);
}