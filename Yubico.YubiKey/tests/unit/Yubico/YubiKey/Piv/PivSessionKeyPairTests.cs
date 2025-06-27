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
using Xunit;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    public class PivSessionKeyPairTests : PivSessionUnitTestBase
    {
        [Fact]
        public void Generate_BadSlot_ThrowsArgException()
        {
            _ = Assert.Throws<ArgumentException>(() => PivSessionMock.GenerateKeyPair(0x81, KeyType.ECP256));
        }

        [Fact]
        public void Generate_NoCollector_ThrowsInvalidOpException()
        {
            KeyCollector = null;
            _ = Assert.Throws<InvalidOperationException>(() => PivSessionMock.GenerateKeyPair(0x9A, KeyType.ECP256));
        }

        [Fact]
        public void Generate_CollectorFalse_ThrowsCancelException()
        {
            KeyCollector = ReturnFalseKeyCollectorDelegate;
            _ = Assert.Throws<OperationCanceledException>(() => PivSessionMock.GenerateKeyPair(0x9A, KeyType.ECP256));
        }

        [Fact]
        public void ImportKey_BadSlot_ThrowsArgException()
        {
            var testKey = TestKeys.GetTestPrivateKey(KeyType.ECP256);
            var privateKey = ECPrivateKey.CreateFromValue(testKey.GetPrivateKeyValue(), KeyType.ECP256);
            
            _ = Assert.Throws<ArgumentException>(() => PivSessionMock.ImportPrivateKey(0x81, privateKey));
        }

        [Fact]
        public void ImportKey_NullKey_ThrowsNullArgException()
        {
            IPrivateKey privateKey = null!;
            _ = Assert.Throws<ArgumentNullException>(() => PivSessionMock.ImportPrivateKey(0x85, privateKey));
        }

        [Fact]
        public void ImportKey_EmptyKey_ThrowsArgException()
        {
            var privateKey = new EmptyPrivateKey();
            _ = Assert.Throws<ArgumentException>(() => PivSessionMock.ImportPrivateKey(0x85, privateKey));
        }

        [Fact]
        public void ImportKey_NoCollector_ThrowsInvalidOpException()
        {
            var testKey = TestKeys.GetTestPrivateKey(KeyType.ECP256);
            var privateKey = ECPrivateKey.CreateFromValue(testKey.GetPrivateKeyValue(), KeyType.ECP256);
            
            KeyCollector = null;
            _ = Assert.Throws<InvalidOperationException>(() => PivSessionMock.ImportPrivateKey(0x85, privateKey));
        }

        [Fact]
        public void ImportKey_CollectorFalse_ThrowsCancelException()
        {
            var testKey = TestKeys.GetTestPrivateKey(KeyType.ECP256);
            var privateKey = ECPrivateKey.CreateFromValue(testKey.GetPrivateKeyValue(), KeyType.ECP256);
            KeyCollector = ReturnFalseKeyCollectorDelegate;

            _ = Assert.Throws<OperationCanceledException>(() => PivSessionMock.ImportPrivateKey(0x85, privateKey));
        }

        [Fact]
        public void ImportCert_BadSlot_ThrowsArgException()
        {
            var cert = TestKeys.GetTestCertificate(KeyType.RSA2048).AsX509Certificate2();
            _ = Assert.Throws<ArgumentException>(() => PivSessionMock.ImportCertificate(0x81, cert!));
        }

        [Fact]
        public void ImportCert_NullCert_ThrowsNullArgException()
        {
            _ = Assert.Throws<ArgumentNullException>(() => PivSessionMock.ImportCertificate(0x85, null!));
        }

        [Fact]
        public void ImportCert_NoCollector_ThrowsInvalidOpException()
        {
            KeyCollector = null;
            var cert = TestKeys.GetTestCertificate(KeyType.RSA2048).AsX509Certificate2();
            _ = Assert.Throws<InvalidOperationException>(() => PivSessionMock.ImportCertificate(0x85, cert!));
        }

        [Fact]
        public void ImportCert_CollectorFalse_ThrowsCancelException()
        {
            KeyCollector = ReturnFalseKeyCollectorDelegate;
            var cert = TestKeys.GetTestCertificate(KeyType.RSA2048).AsX509Certificate2();

            _ = Assert.Throws<OperationCanceledException>(() => PivSessionMock.ImportCertificate(0x85, cert!));
        }

        [Fact]
        public void GetCert_BadSlot_ThrowsArgException()
        {
            _ = Assert.Throws<ArgumentException>(() => PivSessionMock.GetCertificate(0x81));
        }

        private static bool ReturnFalseKeyCollectorDelegate(KeyEntryData _) => false;
    }
}
