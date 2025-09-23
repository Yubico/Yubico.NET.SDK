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

namespace Yubico.YubiKit.Core.PlatformInterop.Windows;

internal sealed class WindowsUnmanagedDynamicLibrary : UnmanagedDynamicLibrary
{
    public WindowsUnmanagedDynamicLibrary(string fileName) :
        base(OpenLibrary(fileName))
    {
    }

    private static SafeLibraryHandle OpenLibrary(string fileName)
    {
        var handle = NativeMethods.LoadLibraryEx(fileName, IntPtr.Zero, 0);
        if (handle.IsInvalid)
        {
            var hr = Marshal.GetHRForLastWin32Error();
            Marshal.ThrowExceptionForHR(hr);
        }

        return handle;
    }

    public override bool TryGetFunction<TDelegate>(string functionName, out TDelegate? d) where TDelegate : class
    {
        if (!TryGetFunctionInternal(functionName, out d)) return TryGetFunctionInternal(functionName + "W", out d);

        return true;
    }

    private bool TryGetFunctionInternal<TDelegate>(string functionName, out TDelegate? d) where TDelegate : class
    {
        var p = NativeMethods.GetProcAddress(_handle, functionName);

        if (p != IntPtr.Zero)
        {
            d = Marshal.GetDelegateForFunctionPointer<TDelegate>(p);
            return true;
        }

        d = null;
        return false;
    }
}