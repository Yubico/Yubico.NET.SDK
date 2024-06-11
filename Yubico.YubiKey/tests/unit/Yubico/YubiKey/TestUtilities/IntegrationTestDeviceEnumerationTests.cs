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
using Xunit;
using Xunit.Sdk;

namespace Yubico.YubiKey.TestUtilities
{
    public class IntegrationTestDeviceEnumerationTests
    {
        [Fact]
        public void Constructor_ShouldThrowException_And_CreateWhitelistFile_IfNotExists()
        {
            // Arrange
            string customDir = Path.Combine(Path.GetTempPath(), "CustomYubicoConfig");
            string whitelistFilePath = Path.Combine(customDir, "Yubico", "YUBIKEY_INTEGRATIONTEST_ALLOWEDKEYS.txt");

            if (Directory.Exists(customDir))
            {
                Directory.Delete(customDir, true);
            }

            Environment.SetEnvironmentVariable("YUBIKEY_INTEGRATIONTEST_ALLOWEDKEYS", null);

            // Act & Assert
            var exception = Assert.Throws<TestClassException>(() =>
            {
                _ = new IntegrationTestDeviceEnumeration(customDir);
            });

            // Assert
            Assert.True(File.Exists(whitelistFilePath));
            Assert.Contains("must add your whitelisted Yubikeys serial number", exception.Message);
        }

        [Fact]
        public void Constructor_ShouldThrowException_IfNoWhitelistedKeys()
        {
            // Arrange
            string customDir = Path.Combine(Path.GetTempPath(), "CustomYubicoConfig");
            string whitelistFilePath = Path.Combine(customDir, "Yubico", "YUBIKEY_INTEGRATIONTEST_ALLOWEDKEYS.txt");

            if (!Directory.Exists(customDir))
            {
                _ = Directory.CreateDirectory(customDir);
            }

            File.WriteAllText(whitelistFilePath, string.Empty);
            Environment.SetEnvironmentVariable("YUBIKEY_INTEGRATIONTEST_ALLOWEDKEYS", null);

            // Act & Assert
            var exception = Assert.Throws<TestClassException>(() =>
            {
                _ = new IntegrationTestDeviceEnumeration(customDir);
            });

            // Assert
            Assert.Contains("must add your whitelisted Yubikeys serial number", exception.Message);
        }

        [Fact]
        public void Constructor_ShouldLoadWhitelistedKeys_FromFile()
        {
            // Arrange
            string customDir = Path.Combine(Path.GetTempPath(), "CustomYubicoConfig");
            string whitelistFilePath = Path.Combine(customDir, "Yubico", "YUBIKEY_INTEGRATIONTEST_ALLOWEDKEYS.txt");
            var whitelistedKeys = new[] { "123456", "7891011" };

            if (!Directory.Exists(customDir))
            {
                _ = Directory.CreateDirectory(customDir);
            }

            File.WriteAllLines(whitelistFilePath, whitelistedKeys);
            Environment.SetEnvironmentVariable("YUBIKEY_INTEGRATIONTEST_ALLOWEDKEYS", null);

            // Act
            var integrationTestDeviceEnumeration = new IntegrationTestDeviceEnumeration(customDir);

            // Assert
            Assert.Equal(whitelistedKeys.Length, integrationTestDeviceEnumeration.AllowedSerialNumbers.Count);
            Assert.All(whitelistedKeys, key => Assert.Contains(key, integrationTestDeviceEnumeration.AllowedSerialNumbers));
        }

        [Fact]
        public void Constructor_ShouldLoadWhitelistedKeys_FromEnvironmentVariable()
        {
            // Arrange
            string customDir = Path.Combine(Path.GetTempPath(), "CustomYubicoConfig");
            string whitelistFilePath = Path.Combine(customDir, "Yubico", "YUBIKEY_INTEGRATIONTEST_ALLOWEDKEYS.txt");
            var envWhitelistedKeys = "123456:7891011";

            if (!Directory.Exists(customDir))
            {
                _ = Directory.CreateDirectory(customDir);
            }

            File.WriteAllText(whitelistFilePath, string.Empty); // Ensure the file exists but is empty
            Environment.SetEnvironmentVariable("YUBIKEY_INTEGRATIONTEST_ALLOWEDKEYS", envWhitelistedKeys);

            // Act
            var integrationTestDeviceEnumeration = new IntegrationTestDeviceEnumeration(customDir);

            // Assert
            var expectedKeys = envWhitelistedKeys.Split(':');
            Assert.Equal(expectedKeys.Length, integrationTestDeviceEnumeration.AllowedSerialNumbers.Count);
            Assert.All(expectedKeys,
                key => Assert.Contains(key, integrationTestDeviceEnumeration.AllowedSerialNumbers));
        }
    }
}
