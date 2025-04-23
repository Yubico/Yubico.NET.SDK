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
using System.IO;

namespace Yubico.PlatformInterop
{
    internal partial class Libraries
    {
        private const string DefaultFrameworksPath = "/System/Library/Frameworks";

        internal const string CoreFoundation = "CoreFoundation.framework/CoreFoundation";
        internal const string IOKitFramework = "IOKit.framework/IOKit";
        internal const string WinSCard = "PCSC.framework/PCSC";
        private static void LoadMacLibraries() => MacOSLibraryLoader.Initialize();

        // public static void DisposeMacOS() => MacOSLibraryLoader.Dispose();

        private static class MacOSLibraryLoader
        {
            private static UnmanagedDynamicLibrary? _IOKitHandle;
            private static UnmanagedDynamicLibrary? _PCSCHandle;
            private static UnmanagedDynamicLibrary? _coreFoundationHandle;

            internal static void Initialize()
            {
                _coreFoundationHandle ??= GetLibraryHandle(CoreFoundation);
                _IOKitHandle ??= GetLibraryHandle(IOKitFramework);
                _PCSCHandle ??= GetLibraryHandle(WinSCard);
            }

            internal static void Dispose()
            {
                _coreFoundationHandle?.Dispose();
                _IOKitHandle?.Dispose();
                _PCSCHandle?.Dispose();
                
                _coreFoundationHandle = null;
                _IOKitHandle = null;
                _PCSCHandle = null;
            }

            private static UnmanagedDynamicLibrary GetLibraryHandle(string libraryName)
            {
                string frameworkPath = GetFrameworkPath();
                string[] pathsToSearch = frameworkPath.Split(':');
                foreach (string path in pathsToSearch)
                {
                    string fullPath = Path.Combine(path, libraryName);
                    if (Directory.Exists(path) && File.Exists(fullPath))
                    {
                        return LoadAndTrack(fullPath);
                    }
                }

                throw new DllNotFoundException($"Failed to load native library from {libraryName}.");
            }

            private static string GetFrameworkPath()
            {
                string? frameworkPath = Environment.GetEnvironmentVariable("DYLD_FRAMEWORK_PATH");
                if (!string.IsNullOrEmpty(frameworkPath))
                {
                    return frameworkPath;
                }

                string? frameworkFallbackPath = Environment.GetEnvironmentVariable("DYLD_FALLBACK_FRAMEWORK_PATH");
                if (!string.IsNullOrEmpty(frameworkFallbackPath))
                {
                    return frameworkFallbackPath;
                }

                return DefaultFrameworksPath;
            }
        }
    }
}
