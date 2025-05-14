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
using System.Security.Cryptography.X509Certificates;
using Xunit;
using Yubico.Core.Tlv;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class AttestTests : PivSessionIntegrationTestBase
    {
        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void Attest_EmptySlot_ThrowsException(
            StandardTestDevice deviceType)
        {
            TestDeviceType = deviceType;

            _ = Assert.Throws<InvalidOperationException>(() =>
                Session.CreateAttestationStatement(PivSlot.Authentication));
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void Attest_Imported_ThrowsException(
            StandardTestDevice deviceType)
        {
            TestDeviceType = deviceType;
            var privateKey = TestKeys.GetTestPrivateKey(KeyType.ECP384).AsPrivateKey();

            Session.ImportPrivateKey(PivSlot.Retired1, privateKey);

            // Cannot attest to an imported key.
            _ = Assert.Throws<InvalidOperationException>(() =>
                Session.CreateAttestationStatement(PivSlot.Retired1));
        }

        [Theory]
        [InlineData(KeyType.RSA2048, StandardTestDevice.Fw5)]
        [InlineData(KeyType.RSA3072, StandardTestDevice.Fw5)]
        [InlineData(KeyType.RSA4096, StandardTestDevice.Fw5)]
        [InlineData(KeyType.ECP256, StandardTestDevice.Fw5)]
        [InlineData(KeyType.ECP384, StandardTestDevice.Fw5)]
        public void AttestGenerated(
            KeyType keyType,
            StandardTestDevice deviceType)
        {
            TestDeviceType = deviceType;

            const byte slotNumber = PivSlot.Retired1;
            _ = Session.GenerateKeyPair(
                slotNumber, keyType, PivPinPolicy.Never, PivTouchPolicy.Never);

            X509Certificate2? cert = null;
            try
            {
                cert = Session.CreateAttestationStatement(slotNumber);
                Assert.NotEqual(1, cert.Version);
            }
            finally
            {
                cert?.Dispose();
            }
        }

        [Theory]
        [InlineData(KeyType.RSA2048, StandardTestDevice.Fw5)]
        [InlineData(KeyType.RSA3072, StandardTestDevice.Fw5)]
        [InlineData(KeyType.RSA4096, StandardTestDevice.Fw5)]
        [InlineData(KeyType.ECP256, StandardTestDevice.Fw5)]
        [InlineData(KeyType.ECP384, StandardTestDevice.Fw5)]
        public void LoadInvalidCert_Attest_ThrowsException(
            KeyType keyType,
            StandardTestDevice deviceType)
        {
            TestDeviceType = deviceType;

            _ = Assert.Throws<ArgumentException>(() => LoadAttestationPair(keyType, false));
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void GetAttestationCert_ReturnsCert(
            StandardTestDevice deviceType)
        {
            TestDeviceType = deviceType;

            LoadAttestationPair(KeyType.ECP384, true);

            X509Certificate2? cert = null;
            try
            {
                cert = Session.GetAttestationCertificate();
                Assert.NotNull(cert);
            }
            finally
            {
                cert?.Dispose();
            }
        }

        [Theory]
        [InlineData(BadAttestationPairs.KeyRsa1024CertValid, StandardTestDevice.Fw5)]
        [InlineData(BadAttestationPairs.KeyRsa2048CertVersion1, StandardTestDevice.Fw5)]
        [InlineData(BadAttestationPairs.KeyEccP256CertVersion1, StandardTestDevice.Fw5)]
        [InlineData(BadAttestationPairs.KeyEccP384CertVersion1, StandardTestDevice.Fw5)]
        [InlineData(BadAttestationPairs.KeyRsa2048CertBigName, StandardTestDevice.Fw5)]
        public void UseBadAttestPair_CreateStatement_ThrowsInvalidOp(
            int whichPair,
            StandardTestDevice deviceType)
        {
            TestDeviceType = deviceType;
            BadAttestationPairs.GetPair(whichPair, out var privateKeyPem, out var certPem);

            var certObj = X509CertificateLoader.LoadCertificate(PemHelper.GetBytesFromPem(certPem));
            var privateKey = AsnPrivateKeyDecoder.CreatePrivateKey(PemHelper.GetBytesFromPem(privateKeyPem));
            var isValid = LoadAttestationPairCommands(privateKey, certObj);
            Assert.True(isValid);

            isValid = AttestationShouldFail(BadAttestationPairs.KeyRsa1024CertValid);
            Assert.True(isValid);
        }

        // Call this to attempt creating an attestation statement. It should
        // fail. So if the operation throws an exception, it fails, so return
        // true. If the operation does not throw an exception, it did not fail,
        // so return false.6
        private bool AttestationShouldFail(
            int whichPair)
        {
            // version 4 YubiKeys accept 1024-bit RSA keys, so don't test that.
            if (Device.FirmwareVersion.Major < 5 && whichPair == BadAttestationPairs.KeyRsa1024CertValid)
            {
                return true;
            }

            X509Certificate2? cert = null;
            try
            {
                _ = Session.GenerateKeyPair(PivSlot.Authentication, KeyType.ECP256);
                cert = Session.CreateAttestationStatement(PivSlot.Authentication);
            }
            catch (InvalidOperationException exc)
            {
                if (exc.Source is not null)
                {
                    return string.Compare(exc.Source, "Yubico.YubiKey", StringComparison.Ordinal) == 0;
                }
            }
            finally
            {
                cert?.Dispose();
            }

            return false;
        }

        // Load a key and cert, but use the commands, so that the checks aren't
        // made and whatever key/cert pair is given is actually loaded.
        private bool LoadAttestationPairCommands(
            IPrivateKey privateKey,
            X509Certificate certObj)
        {
            Session.ImportPrivateKey(0xF9, privateKey);

            var certDer = certObj.GetRawCertData();
            var tlvWriter = new TlvWriter();
            using (tlvWriter.WriteNestedTlv(0x53))
            {
                tlvWriter.WriteValue(0x70, certDer);
                tlvWriter.WriteByte(0x71, 0);
                tlvWriter.WriteValue(0xfe, null);
            }

            var encodedCert = tlvWriter.Encode();
            var putCommand = new PutDataCommand(0x5FFF01, encodedCert);
            var putResponse = Session.Connection.SendCommand(putCommand);
            return putResponse.Status == ResponseStatus.Success;
        }

        // Load a new attestation pair onto the YubiKey. If isValidCert is true,
        // load a pair with a cert that will work. Otherwise, load a pair with a
        // cert that won't work.
        private void LoadAttestationPair(
            KeyType keyType,
            bool isValidCert)
        {
            var testCert = TestKeys.GetTestCertificate(keyType, isValidCert);
            var testPrivKey = TestKeys.GetTestPrivateKey(keyType);
            var privateKey = testPrivKey.AsPrivateKey();

            Session.ReplaceAttestationKeyAndCertificate(privateKey, testCert.AsX509Certificate2());
        }
    }
}
