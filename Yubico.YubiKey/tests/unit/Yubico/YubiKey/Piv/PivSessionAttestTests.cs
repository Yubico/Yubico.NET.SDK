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
using System.Security.Cryptography.X509Certificates;
using Xunit;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    public class PivSessionAttestationTests : PivSessionUnitTestBase
    {
        public PivSessionAttestationTests()
        {
            FirmwareVersion = new FirmwareVersion { Major = 4, Minor = 3, Patch = 0 };
            DeviceMock.AvailableUsbCapabilities = YubiKeyCapabilities.Piv;
        }

        [Theory]
        [InlineData(PivSlot.Pin)]
        [InlineData(PivSlot.Puk)]
        [InlineData(PivSlot.Management)]
        [InlineData(PivSlot.Attestation)]
        [InlineData(0x96)]
        public void CreateAttest_BadSlot_ThrowsArgException(byte slotNumber)
        {
            _ = Assert.Throws<ArgumentException>(() => PivSessionMock.CreateAttestationStatement(slotNumber));
        }

        [Fact]
        public void CreateAttest_BadVersion_ThrowsNotSupportedException()
        {
            // Override firmware version for this specific test
            FirmwareVersion = new FirmwareVersion { Major = 4, Minor = 2, Patch = 0 };

            _ = Assert.Throws<NotSupportedException>(() => PivSessionMock.CreateAttestationStatement(0x9A));
        }

        [Fact]
        public void GetAttest_BadVersion_ThrowsNotSupportedException()
        {
            // Override firmware version for this specific test
            FirmwareVersion = new FirmwareVersion { Major = 4, Minor = 2, Patch = 0 };

            _ = Assert.Throws<NotSupportedException>(() => PivSessionMock.GetAttestationCertificate());
        }

        [Fact]
        public void ReplaceAttest_BadVersion_ThrowsNotSupportedException()
        {
            // Override firmware version for this specific test
            FirmwareVersion = new FirmwareVersion { Major = 4, Minor = 2, Patch = 0 };

            var testKey = TestKeys.GetTestPrivateKey(KeyType.RSA2048);
            var privateKey = RSAPrivateKey.CreateFromPkcs8(testKey.EncodedKey);
            
#pragma warning disable SYSLIB0026
            var cert = new X509Certificate2();
#pragma warning restore SYSLIB0026

            _ = Assert.Throws<NotSupportedException>(() => PivSessionMock.ReplaceAttestationKeyAndCertificate(privateKey, cert));
        }

        [Fact]
        public void ReplaceAttest_NullKey_ThrowsException()
        {
            var cert = TestKeys.GetTestCertificate(KeyType.RSA2048).AsX509Certificate2();

            _ = Assert.Throws<ArgumentNullException>(() => PivSessionMock.ReplaceAttestationKeyAndCertificate((IPrivateKey)null!, cert!));
        }

        [Fact]
        public void ReplaceAttest_NullCert_ThrowsException()
        {
            var testKey = TestKeys.GetTestPrivateKey(KeyType.RSA2048);
            var privateKey = RSAPrivateKey.CreateFromPkcs8(testKey.EncodedKey);

            _ = Assert.Throws<ArgumentNullException>(() => PivSessionMock.ReplaceAttestationKeyAndCertificate(privateKey, null!));
        }

        [Fact]
        public void ReplaceAttest_Rsa1024_ThrowsException()
        {
            BadAttestationPairs.GetPair(BadAttestationPairs.KeyRsa1024CertValid, out var privateKeyPem, out var certPem);
            var badPrivateKey = RSAPrivateKey.CreateFromPkcs8(PemHelper.GetBytesFromPem(privateKeyPem));
            var badCert = X509CertificateLoader.LoadCertificate(PemHelper.GetBytesFromPem(certPem));

            _ = Assert.Throws<ArgumentException>(() => PivSessionMock.ReplaceAttestationKeyAndCertificate(badPrivateKey, badCert));
        }

        [Theory]
        [InlineData(BadAttestationPairs.KeyRsa2048CertVersion1)]
        [InlineData(BadAttestationPairs.KeyEccP256CertVersion1)]
        [InlineData(BadAttestationPairs.KeyEccP384CertVersion1)]
        public void ReplaceAttest_Version1Cert_ThrowsException(int whichPair)
        {
            BadAttestationPairs.GetPair(whichPair, out var privateKeyPem, out var certPem);
            var badPrivateKey = AsnPrivateKeyDecoder.CreatePrivateKey(PemHelper.GetBytesFromPem(privateKeyPem));
            var badCert = X509CertificateLoader.LoadCertificate(PemHelper.GetBytesFromPem(certPem));

            _ = Assert.Throws<ArgumentException>(() => PivSessionMock.ReplaceAttestationKeyAndCertificate(badPrivateKey, badCert));
        }

        [Fact]
        public void ReplaceAttest_BigName_ThrowsException()
        {
            BadAttestationPairs.GetPair(BadAttestationPairs.KeyRsa2048CertBigName, out var privateKeyPem, out var certPem);
            var badPrivateKey = RSAPrivateKey.CreateFromPkcs8(PemHelper.GetBytesFromPem(privateKeyPem));
            var badCert = X509CertificateLoader.LoadCertificate(PemHelper.GetBytesFromPem(certPem));

            _ = Assert.Throws<ArgumentException>(() => PivSessionMock.ReplaceAttestationKeyAndCertificate(badPrivateKey, badCert));
        }
    }
}
