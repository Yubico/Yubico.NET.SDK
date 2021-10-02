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
using Yubico.YubiKey.TestUtilities;
using Yubico.YubiKey.Piv.Commands;
using Yubico.Core.Tlv;
using Xunit;

namespace Yubico.YubiKey.Piv
{
    public class AttestTests
    {
        [Fact]
        public void Attest_EmptySlot_ThrowsException()
        {
            bool isValid = LoadAttestationPair(PivAlgorithm.EccP256, true);
            Assert.True(isValid);

            isValid = SelectSupport.TrySelectYubiKey(out IYubiKeyDevice yubiKey);
            Assert.True(isValid);
            Assert.True(yubiKey.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            using (var pivSession = new PivSession(yubiKey))
            {
                isValid = PivSupport.ResetPiv(pivSession);
                Assert.True(isValid);

                _ = Assert.Throws<InvalidOperationException>(() => pivSession.CreateAttestationStatement(PivSlot.Authentication));
            }
        }

        [Fact]
        public void Attest_Imported_ThrowsException()
        {
            bool isValid = LoadAttestationPair(PivAlgorithm.EccP384, true);
            Assert.True(isValid);

            isValid = SelectSupport.TrySelectYubiKey(out IYubiKeyDevice yubiKey);
            Assert.True(isValid);
            Assert.True(yubiKey.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            using (var pivSession = new PivSession(yubiKey))
            {
                Assert.NotNull(pivSession.Connection);

                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                isValid = PivSupport.ResetPiv(pivSession);
                Assert.True(isValid);

                isValid = PivSupport.ImportKey(pivSession, PivSlot.Authentication);
                Assert.True(isValid);

                _ = Assert.Throws<InvalidOperationException>(() => pivSession.CreateAttestationStatement(PivSlot.Authentication));
            }
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa2048)]
        [InlineData(PivAlgorithm.EccP256)]
        [InlineData(PivAlgorithm.EccP384)]
        public void AttestGenerated(PivAlgorithm algorithm)
        {
            byte[] slotNumbers = new byte[] {
                0x9A, 0x9C, 0x9D, 0x9E,
                0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F,
                0x90, 0x91, 0x92, 0x93, 0x94, 0x95
            };

            bool isValid = LoadAttestationPair(algorithm, true);
            Assert.True(isValid);

            isValid = SelectSupport.TrySelectYubiKey(out IYubiKeyDevice yubiKey);
            Assert.True(isValid);
            Assert.True(yubiKey.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            using (var pivSession = new PivSession(yubiKey))
            {
                Assert.NotNull(pivSession.Connection);

                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                isValid = PivSupport.ResetPiv(pivSession);
                Assert.True(isValid);

                for (int index = 0; index < slotNumbers.Length; index++)
                {
                    _ = pivSession.GenerateKeyPair(
                        slotNumbers[index], PivAlgorithm.EccP256, PivPinPolicy.Never, PivTouchPolicy.Never);
                    Assert.True(isValid);

                    X509Certificate2? cert = null;
                    try
                    {
                        cert = pivSession.CreateAttestationStatement(slotNumbers[index]);
                        Assert.NotEqual(1, cert.Version);
                    }
                    finally
                    {
                        cert?.Dispose();
                    }
                }
            }
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa2048)]
        [InlineData(PivAlgorithm.EccP256)]
        [InlineData(PivAlgorithm.EccP384)]
        public void LoadInvalidCert_Attest_ThrowsException(PivAlgorithm algorithm)
        {
            _ = Assert.Throws<ArgumentException>(() => LoadAttestationPair(algorithm, false));
        }

        [Fact]
        public void GetAttestationCert_ReturnsCert()
        {
            bool isValid = LoadAttestationPair(PivAlgorithm.EccP384, true);
            Assert.True(isValid);

            isValid = SelectSupport.TrySelectYubiKey(out IYubiKeyDevice yubiKey);
            Assert.True(isValid);
            Assert.True(yubiKey.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            using (var pivSession = new PivSession(yubiKey))
            {
                Assert.NotNull(pivSession.Connection);

                X509Certificate2? cert = null;
                try
                {
                    cert = pivSession.GetAttestationCertificate();
                    Assert.NotNull(cert);
                }
                finally
                {
                    cert?.Dispose();
                }
            }
        }

        // Don't test with whichPair values of 5 or 6. Those are bad pairs, but
        // the YubiKey will generate attestation statements with them nonetheless.
        [Theory]
        [InlineData(BadAttestationPairs.KeyRsa1024CertValid)]
        [InlineData(BadAttestationPairs.KeyRsa2048CertVersion1)]
        [InlineData(BadAttestationPairs.KeyEccP256CertVersion1)]
        [InlineData(BadAttestationPairs.KeyEccP384CertVersion1)]
        [InlineData(BadAttestationPairs.KeyRsa2048CertBigName)]
        public void UseBadAttestPair_CreateStatement_ThrowsInvalidOp(int whichPair)
        {
            BadAttestationPairs.GetPair(whichPair, out string privateKeyPem, out string certPem);

            var priKey = new KeyConverter(privateKeyPem.ToCharArray());

            char[] certChars = certPem.ToCharArray();
            byte[] certDer = Convert.FromBase64CharArray(certChars, 27, certChars.Length - 52);
            var certObj = new X509Certificate2(certDer);

            PivPrivateKey pivPrivateKey = priKey.GetPivPrivateKey();
            bool isValid = LoadAttestationPairCommands(pivPrivateKey, certObj);
            Assert.True(isValid);

            isValid = AttestationShouldFail(BadAttestationPairs.KeyRsa1024CertValid);
            Assert.True(isValid);
        }

        // Call this to attempt creating an attestation statement. It should
        // fail. So if the operation throws an exception, it fails, so return
        // true. If the operation does not throw an exception, it did not fail,
        // so return false.
        private static bool AttestationShouldFail(int whichPair)
        {
            if (SelectSupport.TrySelectYubiKey(out IYubiKeyDevice yubiKey) == false)
            {
                return false;
            }

            // version 4 YubiKeys accept 1024-bit RSA keys, so don't test that.
            if ((yubiKey.FirmwareVersion.Major < 5) && (whichPair == BadAttestationPairs.KeyRsa1024CertValid))
            {
                return true;
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                X509Certificate2? cert = null;
                try
                {
                    _ = pivSession.GenerateKeyPair(PivSlot.Authentication, PivAlgorithm.EccP256);
                    cert = pivSession.CreateAttestationStatement(PivSlot.Authentication);
                }
                catch (InvalidOperationException exc)
                {
                    if (!(exc.Source is null))
                    {
                        return exc.Source.CompareTo("Yubico.YubiKey") == 0;
                    }
                }
                finally
                {
                    cert?.Dispose();
                }
            }

            return false;
        }

        // Load a key and cert, but use the commands, so that the checks aren't
        // made and whatever key/cert pair is given is actually loaded.
        private static bool LoadAttestationPairCommands(PivPrivateKey privateKey, X509Certificate certObj)
        {
            if (SelectSupport.TrySelectYubiKey(out IYubiKeyDevice yubiKey) == false)
            {
                return false;
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ImportPrivateKey(0xF9, privateKey);

                byte[] certDer = certObj.GetRawCertData();
                var tlvWriter = new TlvWriter();
                using (tlvWriter.WriteNestedTlv(0x53))
                {
                    tlvWriter.WriteValue(0x70, certDer);
                    tlvWriter.WriteByte(0x71, 0);
                    tlvWriter.WriteValue(0xfe, null);
                }
                byte[] encodedCert = tlvWriter.Encode();

                var putCommand = new PutDataCommand(0x5FFF01, encodedCert);
                PutDataResponse putResponse = pivSession.Connection.SendCommand(putCommand);
                return putResponse.Status == ResponseStatus.Success;
            }
        }

        // Load a new attestation pair onto the YubiKey. If isValidCert is true,
        // load a pair with a cert that will work. Otherwise, load a pair with a
        // cert that won't work.
        private static bool LoadAttestationPair(PivAlgorithm algorithm, bool isValidCert)
        {
            if (SelectSupport.TrySelectYubiKey(out IYubiKeyDevice yubiKey) == false)
            {
                return false;
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                if (SampleKeyPairs.GetKeyAndCertPem(algorithm, isValidCert, out string certPem, out string privateKeyPem) == false)
                {
                    return false;
                }

                var cert = new CertConverter(certPem.ToCharArray());
                X509Certificate2 certObj = cert.GetCertObject();
                var privateKey = new KeyConverter(privateKeyPem.ToCharArray());
                PivPrivateKey pivPrivateKey = privateKey.GetPivPrivateKey();

                pivSession.ReplaceAttestationKeyAndCertificate(pivPrivateKey, certObj);
            }

            return true;
        }
    }
}
