// Copyright 2024 Yubico AB
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
    /// <summary>
    /// Helper class for loading platform-specific libraries.
    /// This allows us to test that libraries can be properly loaded.
    /// </summary>
    internal static class PlatformLibraryLoader
    {
        /// <summary>
        /// Attempts to load a native library and verify it can be accessed.
        /// </summary>
        /// <param name="libraryName">Name of the library to load.</param>
        /// <returns>A tuple indicating success and a message.</returns>
        public static (bool success, string message) TryLoadLibrary(
            string libraryName)
        {
            try
            {
                IntPtr handle = LoadLibrary(libraryName);

                if (handle == IntPtr.Zero)
                {
                    // Get platform-specific error message
                    string errorMessage = GetLastErrorMessage();
                    return (false, $"Library {libraryName} could not be loaded: {errorMessage}");
                }

                // Free the library handle since we only wanted to check if it could be loaded
                FreeLibraryHandles(handle);

                return (true, $"Successfully loaded library: {libraryName}");
            }
            catch (Exception ex)
            {
                return (false, $"Error loading library {libraryName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the platform-specific error message for the last error that occurred.
        /// </summary>
        /// <returns>A string containing the error message.</returns>
        private static string GetLastErrorMessage()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsErrorMessage();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                     RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GetUnixErrorMessage();
            }

            return "Unknown platform";
        }

        // Platform-specific implementation for loading libraries
        private static IntPtr LoadLibrary(
            string libraryName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return WindowsLoadLibrary(libraryName);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return LinuxLoadLibrary(libraryName);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return MacOSLoadLibrary(libraryName);
            }

            throw new PlatformNotSupportedException("Current OS is not supported");
        }

        private static bool FreeLibraryHandles(
            IntPtr handle)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return WindowsFreeLibrary(handle);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return UnixFreeLibrary(handle);
            }

            throw new PlatformNotSupportedException("Current OS is not supported");
        }

        // Windows implementation
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true,
            BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern IntPtr LoadLibraryW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

        
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(
            IntPtr hModule);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetLastError();

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int FormatMessageW(
            uint dwFlags,
            IntPtr lpSource,
            uint dwMessageId,
            uint dwLanguageId,
            [Out] System.Text.StringBuilder lpBuffer,
            uint nSize,
            IntPtr arguments);

        private static IntPtr WindowsLoadLibrary(
            string libraryName) => LoadLibraryW(libraryName);

        private static bool WindowsFreeLibrary(
            IntPtr handle) => FreeLibrary(handle);

        // Constants for FormatMessage
        private const uint FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
        private const uint FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;

        private static string GetWindowsErrorMessage()
        {
            uint errorCode = GetLastError();
            if (errorCode == 0)
            {
                return "No error";
            }

            var messageBuffer = new System.Text.StringBuilder(1024);
            int size = FormatMessageW(
                FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
                IntPtr.Zero,
                errorCode,
                0,
                messageBuffer,
                (uint)messageBuffer.Capacity,
                IntPtr.Zero);

            if (size == 0)
            {
                return $"Error code {errorCode} (0x{errorCode:X8}). Unable to retrieve error message.";
            }

            // Trim the end of the message to remove line breaks
            string message = messageBuffer.ToString().Trim();
            return $"Error code {errorCode} (0x{errorCode:X8}): {message}";
        }

        // Linux/macOS implementation
        [DllImport("libdl.so.2", EntryPoint = "dlopen", SetLastError = true, CharSet = CharSet.Ansi,
            ExactSpelling = true, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern IntPtr dlopen_linux(
            [MarshalAs(UnmanagedType.LPStr)] string filename,
            int flags);

        [DllImport("libdl.so.2", EntryPoint = "dlclose", SetLastError = true, ExactSpelling = true)]
        private static extern int dlclose_linux(
            IntPtr handle);

        [DllImport("libdl.dylib", EntryPoint = "dlopen", SetLastError = true, CharSet = CharSet.Ansi,
            ExactSpelling = true, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern IntPtr dlopen_macos(
            [MarshalAs(UnmanagedType.LPStr)] string filename,
            int flags);

        [DllImport("libdl.dylib", EntryPoint = "dlclose", SetLastError = true, ExactSpelling = true)]
        private static extern int dlclose_macos(
            IntPtr handle);

        [DllImport("libdl.so.2", EntryPoint = "dlerror", ExactSpelling = true)]
        private static extern IntPtr dlerror_linux();

        [DllImport("libdl.dylib", EntryPoint = "dlerror", ExactSpelling = true)]
        private static extern IntPtr dlerror_macos();

        // The dlopen flag for RTLD_NOW (resolve all symbols immediately)
        private const int RTLD_NOW = 2;

        private static IntPtr LinuxLoadLibrary(
            string libraryName) => dlopen_linux(libraryName, RTLD_NOW);

        private static IntPtr MacOSLoadLibrary(
            string libraryName) => dlopen_macos(libraryName, RTLD_NOW);

        private static bool UnixFreeLibrary(
            IntPtr handle)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return dlclose_linux(handle) == 0;
            }
            else
            {
                return dlclose_macos(handle) == 0;
            }
        }

        private static string GetUnixErrorMessage()
        {
            try
            {
                // On Linux/macOS, dlerror() will return an error description string
                // from a previous dlopen/dlsym/dlclose call that failed.

                IntPtr errorPtr;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    errorPtr = dlerror_linux();
                }
                else
                {
                    errorPtr = dlerror_macos();
                }

                if (errorPtr == IntPtr.Zero)
                {
                    return "Dynamic linker did not report an error";
                }

                string errorMessage = Marshal.PtrToStringAnsi(errorPtr) ?? string.Empty;
                return string.IsNullOrEmpty(errorMessage)
                    ? "Empty error message from dynamic linker"
                    : errorMessage;
            }
            catch (Exception ex)
            {
                return $"Error retrieving dynamic linker error message: {ex.Message}";
            }
        }
    }
}
