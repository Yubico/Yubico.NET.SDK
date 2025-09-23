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

using System.Globalization;
using System.Runtime.InteropServices;

namespace Yubico.YubiKit.Core.PlatformInterop.Linux;

internal sealed class LinuxUnmanagedDynamicLibrary : UnmanagedDynamicLibrary
{
    public LinuxUnmanagedDynamicLibrary(string fileName) :
        base(OpenLibrary(fileName))
    {
    }

    private static SafeLibraryHandle OpenLibrary(string fileName)
    {
        SafeLinuxLibraryHandle handle = NativeMethods.linux_dlopen(fileName, NativeMethods.DlOpenFlags.Lazy);
        if (handle.IsInvalid)
            throw new PlatformApiException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    """ExceptionMessages.LibraryLoadFailed""",
                    fileName));

        return handle;
    }

    public override bool TryGetFunction<TDelegate>(string functionName, out TDelegate? d) where TDelegate : class
    {
        IntPtr p = NativeMethods.linux_dlsym(_handle, functionName);

        if (p != IntPtr.Zero)
        {
            d = Marshal.GetDelegateForFunctionPointer<TDelegate>(p);
            return true;
        }

        d = null;
        return false;
    }
}