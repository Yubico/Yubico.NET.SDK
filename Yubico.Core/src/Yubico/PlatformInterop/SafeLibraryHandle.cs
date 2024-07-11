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

using Microsoft.Win32.SafeHandles;

namespace Yubico.PlatformInterop
{
    internal abstract class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        protected SafeLibraryHandle() : base(true)
        {
        }
    }

    internal sealed class SafeWindowsLibraryHandle : SafeLibraryHandle
    {
        // CA1419: Provide a parameterless constructor that is as visible as the
        // containing type for concrete types derived from
        // 'System.Runtime.InteropServices.SafeHandle'
        public SafeWindowsLibraryHandle() : base()
        {
        }

        protected override bool ReleaseHandle() => NativeMethods.FreeLibrary(handle);
    }

    internal sealed class SafeMacOSLibraryHandle : SafeLibraryHandle
    {
        // CA1419: Provide a parameterless constructor that is as visible as the
        // containing type for concrete types derived from
        // 'System.Runtime.InteropServices.SafeHandle'
        public SafeMacOSLibraryHandle() : base()
        {
        }

        protected override bool ReleaseHandle() => NativeMethods.mac_dlclose(handle) == 0;
    }

    internal sealed class SafeLinuxLibraryHandle : SafeLibraryHandle
    {
        // CA1419: Provide a parameterless constructor that is as visible as the
        // containing type for concrete types derived from
        // 'System.Runtime.InteropServices.SafeHandle'
        public SafeLinuxLibraryHandle() : base()
        {
        }

        protected override bool ReleaseHandle() => NativeMethods.linux_dlclose(handle) == 0;
    }
}
