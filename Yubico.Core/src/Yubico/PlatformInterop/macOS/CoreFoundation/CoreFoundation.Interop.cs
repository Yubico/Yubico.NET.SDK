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
    internal static partial class NativeMethods
    {
        // Note that the DefaultDllImportSearchPaths attribute is a security best
        // practice on the Windows platform (and required by our analyzer
        // settings). It does not currently have any effect on platforms other
        // than Windows, but is included because of the analyzer and in the hope
        // that it will be supported by these platforms in the future.
        [DllImport(Libraries.CoreFoundation)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static extern ulong CFGetTypeID(IntPtr theObject);

        /*!
            @function CFSetGetCount
            Returns the number of values currently in the set.
            @param theSet The set to be queried. If this parameter is not a valid
                CFSet, the behavior is undefined.
            @result The number of values in the set.
        */
        [DllImport(Libraries.CoreFoundation)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static extern long CFSetGetCount(IntPtr theSet);

        /*!
            @function CFSetGetValues
            Fills the buffer with values from the set.
            @param theSet The set to be queried. If this parameter is not a
                valid CFSet, the behavior is undefined.
            @param values A C array of pointer-sized values to be filled with
                values from the set. The values in the C array are ordered
                in the same order in which they appear in the set. If this
                parameter is not a valid pointer to a C array of at least
                CFSetGetCount() pointers, the behavior is undefined.
        */
        [DllImport(Libraries.CoreFoundation)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static extern void CFSetGetValues(IntPtr theSet, IntPtr[] values);

        [DllImport(Libraries.CoreFoundation)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static extern IntPtr CFStringCreateWithCString(IntPtr allocatorRef, byte[] cStr, int encoding);

        [DllImport(Libraries.CoreFoundation)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static extern ulong CFNumberGetTypeID();

        [DllImport(Libraries.CoreFoundation)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern bool CFNumberGetValue(IntPtr numberRef, int theType, byte[] valuePtr);

        [DllImport(Libraries.CoreFoundation)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern void CFRelease(IntPtr theObject);

        [DllImport(Libraries.CoreFoundation)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern IntPtr CFRunLoopGetCurrent();

        public const int kCFRunLoopRunFinished = 1;
        public const int kCFRunLoopRunStopped = 2;
        public const int kCFRunLoopRunTimedOut = 3;
        public const int kCFRunLoopRunHandledSource = 4;

        [DllImport(Libraries.CoreFoundation)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern int CFRunLoopRunInMode(IntPtr mode, double seconds, bool returnAfterSourceHandled);

        [DllImport(Libraries.CoreFoundation)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern void CFRunLoopStop(IntPtr runLoop);
    }
}
