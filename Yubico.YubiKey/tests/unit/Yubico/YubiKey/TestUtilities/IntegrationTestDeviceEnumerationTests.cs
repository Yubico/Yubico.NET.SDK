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

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace Yubico.YubiKey.TestUtilities
{
    public class IntegrationTestDeviceEnumerationTests
    {
        [Fact]
        public void Constructor_ShouldThrowException_And_CreateAllowlistFile_IfNotExists()
        {
            // Arrange
            string customDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string allowlistFilePath = Path.Combine(customDir, "YUBIKEY_INTEGRATIONTEST_ALLOWEDKEYS.txt");

            Environment.SetEnvironmentVariable("YUBIKEY_INTEGRATIONTEST_ALLOWEDKEYS", null);

            // Act & Assert
            var exception = Assert.Throws<TestClassException>(() =>
            {
                _ = new IntegrationTestDeviceEnumeration(customDir);
            });

            // Assert
            Thread.Sleep(500);
            Assert.True(File.Exists(allowlistFilePath));
            Assert.Contains("must add your allow-listed Yubikeys serial number", exception.Message);
        }

        [Fact]
        public async Task Constructor_ShouldThrowException_IfNoAllowlistedKeys()
        {
            // Arrange
            string customDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string allowlistFilePath = Path.Combine(customDir, "YUBIKEY_INTEGRATIONTEST_ALLOWEDKEYS.txt");

            _ = Directory.CreateDirectory(customDir);

            // Wait 500 ms for directory to be created
            await Task.Delay(500);
            await File.WriteAllTextAsync(allowlistFilePath, string.Empty);

            Environment.SetEnvironmentVariable("YUBIKEY_INTEGRATIONTEST_ALLOWEDKEYS", null);

            // Act & Assert
            var exception = Assert.Throws<TestClassException>(() =>
            {
                _ = new IntegrationTestDeviceEnumeration(customDir);
            });

            // Assert
            Assert.Contains("must add your allow-listed Yubikeys serial number", exception.Message);
        }

        [Fact]
        public async Task Constructor_ShouldLoadAllowlistedKeys_FromFile()
        {
            // Arrange
            string customDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string allowlistFilePath = Path.Combine(customDir, "YUBIKEY_INTEGRATIONTEST_ALLOWEDKEYS.txt");
            var allowlistedKeys = new[] { "123456", "7891011" };

            // Wait 500 ms for directory to be created
            _ = Directory.CreateDirectory(customDir);

            await Task.Delay(500);
            await File.WriteAllLinesAsync(allowlistFilePath, allowlistedKeys);

            Environment.SetEnvironmentVariable("YUBIKEY_INTEGRATIONTEST_ALLOWEDKEYS", null);

            // Act
            var integrationTestDeviceEnumeration = new IntegrationTestDeviceEnumeration(customDir);

            // Assert
            Assert.Equal(allowlistedKeys.Length, integrationTestDeviceEnumeration.AllowedSerialNumbers.Count);
            Assert.All(allowlistedKeys,
                key => Assert.Contains(key, integrationTestDeviceEnumeration.AllowedSerialNumbers));
        }

        [Fact]
        public async Task Constructor_ShouldLoadAllowlistedKeys_FromEnvironmentVariable()
        {
            // Arrange
            string customDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string allowlistFilePath = Path.Combine(customDir, "YUBIKEY_INTEGRATIONTEST_ALLOWEDKEYS.txt");
            var envAllowlistedKeys = "123456:7891011";

            _ = Directory.CreateDirectory(customDir);

            // Wait 500 ms for directory to be created
            await Task.Delay(500);
            await File.WriteAllTextAsync(allowlistFilePath, string.Empty);

            Environment.SetEnvironmentVariable("YUBIKEY_INTEGRATIONTEST_ALLOWEDKEYS", envAllowlistedKeys);

            // Act
            var integrationTestDeviceEnumeration = new IntegrationTestDeviceEnumeration(customDir);

            // Assert
            var expectedKeys = envAllowlistedKeys.Split(':');
            Assert.Equal(expectedKeys.Length, integrationTestDeviceEnumeration.AllowedSerialNumbers.Count);
            Assert.All(expectedKeys,
                key => Assert.Contains(key, integrationTestDeviceEnumeration.AllowedSerialNumbers));
        }
    }
}
