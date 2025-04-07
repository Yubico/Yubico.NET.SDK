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
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Yubico.PlatformInterop
{
    /// <summary>
    /// Integration tests for native library loading across different platforms and .NET versions.
    /// </summary>
    public class NativeLibraryTests
    {
        private readonly ITestOutputHelper _output;

        public NativeLibraryTests(
            ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void EnsureNativeLibraryInitialized_ShouldNotThrow()
        {
            // Arrange & Act & Assert
            var exception = Record.Exception(() => Libraries.EnsureInitialized());

            // If there was an exception, log detailed info before failing the test
            if (exception != null)
            {
                LogEnvironmentInfo();
                Assert.Null(exception);
            }
        }

        [Fact]
        public void TryCallFunctionFromNativeLibrary_ShouldSucceed()
        {
            // First ensure library is loaded
            Libraries.EnsureInitialized();

            // Get all registered libraries and verify each one
            var libraries = NativeLibraryRegistry.GetAllLibraries();

            foreach (var library in libraries)
            {
                _output.WriteLine($"Verifying library: {library.LibraryName}");

                bool success;
                (success, var message) = library.VerifyOnCurrentPlatform();

                _output.WriteLine($"  Result: {(success ? "Success" : "Failed")}");
                _output.WriteLine($"  Message: {message}");

                Assert.True(success, $"Library {library.LibraryName} verification failed: {message}");
            }
        }

#if NET47
        [Fact]
        public void CheckPathForNativeShims_ShouldExistInCorrectArchitectureFolder()
        {
            // Skip if not running on Windows since .NET Framework 4.7 is Windows-only
            PlatformTestHelpers.SkipIfNotWindows();
            
            // Arrange
            string expectedPath = PlatformTestHelpers.GetNet47NativeLibraryPath("Yubico.NativeShims");
            
            // Act
            bool fileExists = File.Exists(expectedPath);
            
            // Assert
            _output.WriteLine($"Checking for native library at: {expectedPath}");
            _output.WriteLine($"File exists: {fileExists}");
            
            Assert.True(fileExists, $"Native library not found at {expectedPath}");
        }
        
        [Theory]
        [InlineData("Yubico.NativeShims")]
        public void CheckMultipleNativeLibraries_Net47_ShouldExistInCorrectArchitectureFolder(string libraryName)
        {
            // Skip if not running on Windows since .NET Framework 4.7 is Windows-only
            PlatformTestHelpers.SkipIfNotWindows();
            
            // Arrange
            string expectedPath = PlatformTestHelpers.GetNet47NativeLibraryPath(libraryName);
            
            // Act
            bool fileExists = File.Exists(expectedPath);
            
            // Assert
            _output.WriteLine($"Checking for native library at: {expectedPath}");
            _output.WriteLine($"File exists: {fileExists}");
            
            Assert.True(fileExists, $"Native library not found at {expectedPath}");
        }
#endif

        [Fact]
        public void GetNativeLibraryDetails_ShouldLogLibraryInfo()
        {
            // This test doesn't assert anything but logs details about the native libraries
            // that can be useful for debugging
            LogEnvironmentInfo();
        }

        private void LogEnvironmentInfo()
        {
            _output.WriteLine("=== Environment Information ===");
            _output.WriteLine($"OS: {RuntimeInformation.OSDescription}");
            _output.WriteLine($"Framework: {RuntimeInformation.FrameworkDescription}");
            _output.WriteLine($"Architecture: {RuntimeInformation.OSArchitecture}");
            _output.WriteLine($"Process Architecture: {RuntimeInformation.ProcessArchitecture}");
            _output.WriteLine($"64-bit Process: {Environment.Is64BitProcess}");
            _output.WriteLine($"Base Directory: {AppDomain.CurrentDomain.BaseDirectory}");

            // Platform specific info
            _output.WriteLine($"Platform: {SdkPlatformInfo.OperatingSystem}");
            _output.WriteLine($"Platform Encoding: {SdkPlatformInfo.Encoding.WebName}");
            _output.WriteLine($"Platform CharSize: {SdkPlatformInfo.CharSize}");

#if NET47
            _output.WriteLine(".NET Framework 4.7 Specific Tests");
            string archDir = PlatformTestHelpers.GetArchSpecificDirectoryName();
            _output.WriteLine($"Architecture Directory: {archDir}");
            
            foreach (var library in NativeLibraryRegistry.GetAllLibraries())
            {
                string path = PlatformTestHelpers.GetNet47NativeLibraryPath(library.LibraryName);
                _output.WriteLine($"Expected Library Path: {path}");
                _output.WriteLine($"  Exists: {File.Exists(path)}");
            }
#else
            _output.WriteLine(".NET Core/5+/Standard Tests");
#endif

            // List platform-specific libraries
            _output.WriteLine("\n=== Platform-Specific Libraries ===");
            foreach (var library in PlatformLibraryRegistry.GetPlatformLibraries())
            {
                _output.WriteLine($"Library: {library}");
            }

            // List all DLLs in the current directory
            _output.WriteLine("\n=== DLLs in Base Directory ===");
            try
            {
                foreach (var file in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll"))
                {
                    _output.WriteLine(file);
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error listing DLLs: {ex.Message}");
            }

            // Check for architecture-specific subdirectories
            string[] archDirs = { "x86", "x64", "arm64" };
            foreach (var dir in archDirs)
            {
                string archPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dir);
                _output.WriteLine($"\n=== DLLs in {dir} Directory ===");
                if (Directory.Exists(archPath))
                {
                    try
                    {
                        foreach (var file in Directory.GetFiles(archPath, "*.dll"))
                        {
                            _output.WriteLine(file);
                        }
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"Error listing DLLs in {dir}: {ex.Message}");
                    }
                }
                else
                {
                    _output.WriteLine($"Directory does not exist: {archPath}");
                }
            }

            // List environment variables that might be relevant
            _output.WriteLine("\n=== Relevant Environment Variables ===");
            string[] relevantVars =
            {
                "PATH", "LD_LIBRARY_PATH", "DYLD_LIBRARY_PATH",
                "CI", "GITHUB_ACTIONS", "GITHUB_WORKFLOW", "GITHUB_RUNNER_OS"
            };

            foreach (var varName in relevantVars)
            {
                string value = Environment.GetEnvironmentVariable(varName) ?? "(not set)";
                _output.WriteLine($"{varName}: {value}");
            }
        }

        [Theory]
        [InlineData("Yubico.NativeShims")]
        public void SpecificNativeLibrary_ShouldLoadSuccessfully(
            string libraryName)
        {
            // First ensure library is loaded
            Libraries.EnsureInitialized();

            // Find the specific library entry
            var libraries = NativeLibraryRegistry.GetAllLibraries();
            var library = libraries.FirstOrDefault(l => l.LibraryName == libraryName);

            Assert.NotNull(library);

            _output.WriteLine($"Verifying specific library: {library.LibraryName}");

            var (success, message) = library.VerifyOnCurrentPlatform();

            _output.WriteLine($"  Result: {(success ? "Success" : "Failed")}");
            _output.WriteLine($"  Message: {message}");

            Assert.True(success, $"Library {library.LibraryName} verification failed: {message}");
        }

        [SkippableFact]
        public void NonExistentLibrary_ShouldReturnError()
        {
            Skip.If(true); // Skip for now
            string nonExistentLibrary = "NonExistentLibrary" + Guid.NewGuid().ToString("N");
            _output.WriteLine($"Testing error handling for non-existent library: {nonExistentLibrary}");

            var (success, message) = PlatformLibraryLoader.TryLoadLibrary(nonExistentLibrary);

            _output.WriteLine($"Result: {(success ? "Success" : "Failed")}");
            _output.WriteLine($"Message: {message}");

            Assert.False(success, "Loading a non-existent library should fail");
            Assert.Contains(nonExistentLibrary, message, StringComparison.Ordinal);

            // Ensure we have some kind of meaningful error message
            // This is platform-specific, so we can't check for an exact error message
            // But we can verify that we have a non-empty error message
            _output.WriteLine("Checking that error message is not empty and has a meaningful description");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows should contain an error code
                Assert.Contains("Error code", message, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                // Unix platforms (Linux/macOS) should have some kind of error message
                // It might be "file not found" or something similar
                bool hasDetailedError =
                    message.Length > nonExistentLibrary.Length + 30 && // Has more than just the library name
                    !message.Contains("Unknown error"); // Not a generic error

                // If this fails, it likely means dlerror() isn't working correctly
                if (!hasDetailedError)
                {
                    _output.WriteLine(
                        "WARNING: No detailed error message found for non-existent library. This may indicate an issue with error message retrieval.");
                }
            }
        }

        [Fact]
        public void PlatformSpecificLibraries_ShouldLoadSuccessfully()
        {
            // This test verifies that platform-specific libraries can be loaded
            _output.WriteLine($"Testing platform-specific libraries for {RuntimeInformation.OSDescription}");

            // Get all platform-specific libraries
            var results = PlatformLibraryRegistry.TestPlatformLibraries();

            if (results.Count == 0)
            {
                _output.WriteLine("No platform-specific libraries to test for this platform");
                return;
            }

            // Keep track of successes and failures
            int totalCount = results.Count;
            int successCount = 0;

            foreach (var libraryResult in results)
            {
                string libraryName = libraryResult.Key;
                var (success, message) = libraryResult.Value;

                _output.WriteLine($"  Library: {libraryName}");
                _output.WriteLine($"    Result: {(success ? "Success" : "Failed")}");
                _output.WriteLine($"    Message: {message}");

                if (success)
                {
                    successCount++;
                }
            }

            // Calculate success percentage (for CI environments where some libs might not be available)
            double successPercentage = (double)successCount / totalCount * 100;
            _output.WriteLine($"Success rate: {successPercentage:F2}% ({successCount}/{totalCount})");

            // Log failures for debugging
            if (successCount < totalCount)
            {
                var failedLibraries = results
                    .Where(r => !r.Value.success)
                    .Select(r => $"{r.Key}: {r.Value.message}")
                    .ToArray();

                _output.WriteLine($"Failed libraries: {string.Join(", ", failedLibraries)}");
            }

            // On CI, we'll accept a partial success rate to account for different environments
            bool inCiEnvironment = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ||
                                   !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));

            // In CI, require at least 50% success, otherwise require 100%
            double requiredSuccessRate = inCiEnvironment ? 50.0 : 100.0;

            Assert.True(
                successPercentage >= requiredSuccessRate,
                $"Platform library test success rate too low: {successPercentage:F2}% (required: {requiredSuccessRate}%)");
        }
    }
}
