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

using System.Diagnostics;
using System.Globalization;
using Yubico.YubiKit.Core.Core.PlatformInterop.Linux;
using Yubico.YubiKit.Core.Core.PlatformInterop.MacOS;
using Yubico.YubiKit.Core.Core.PlatformInterop.Windows;

namespace Yubico.YubiKit.Core.Core.PlatformInterop;

internal abstract class UnmanagedDynamicLibrary : IDisposable
{
    protected readonly SafeLibraryHandle _handle;
    private bool disposedValue;

    protected UnmanagedDynamicLibrary(SafeLibraryHandle handle)
    {
        _handle = handle;
    }

    #region IDisposable Members

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion

    public static UnmanagedDynamicLibrary Open(string fileName) =>
        SdkPlatformInfo.OperatingSystem switch
        {
            SdkPlatform.Windows => new WindowsUnmanagedDynamicLibrary(fileName),
            SdkPlatform.MacOS => new MacOSUnmanagedDynamicLibrary(fileName),
            SdkPlatform.Linux => new LinuxUnmanagedDynamicLibrary(fileName),
            _ => throw new PlatformNotSupportedException()
        };

    public void GetFunction<TDelegate>(string functionName, out TDelegate d) where TDelegate : class
    {
        if (!TryGetFunction(functionName, out TDelegate? temp))
            throw new PlatformApiException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    "ExceptionMessages.GetUnmanagedFunctionFailed",
                    functionName));

        Debug.Assert(temp is TDelegate);
        d = temp!;
    }

    public abstract bool TryGetFunction<TDelegate>(string functionName, out TDelegate? d) where TDelegate : class;

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing) _handle.Dispose();

            disposedValue = true;
        }
    }
}