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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Xunit;
using Yubico.Core.Devices.Hid;
using Yubico.Core.Tlv;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Oath;
using Yubico.YubiKey.Otp;
using Yubico.YubiKey.Piv;
using Yubico.YubiKey.TestUtilities;
using Yubico.YubiKey.YubiHsmAuth;
using ECCurve = System.Security.Cryptography.ECCurve;
using ECPoint = System.Security.Cryptography.ECPoint;


namespace Yubico.YubiKey.Scp
{
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class Scp11Tests
    {
        private const byte OceKid = 0x010;

        public Scp11Tests()
        {
            ResetAllowedDevices();
        }

        private IYubiKeyDevice GetDevice(
            StandardTestDevice desiredDeviceType,
            Transport transport = Transport.SmartCard,
            FirmwareVersion? minimumFirmwareVersion = null) =>
            IntegrationTestDeviceEnumeration.GetTestDevice(desiredDeviceType, transport,
                minimumFirmwareVersion ?? FirmwareVersion.V5_7_2);
        
        private static void ResetAllowedDevices()
        {
            // Reset all attached allowed devices
            foreach (var availableDevice in IntegrationTestDeviceEnumeration.GetTestDevices())
            {
                using var session = new SecurityDomainSession(availableDevice);
                session.Reset();
            }
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp11b_PivSession_Operations_Succeeds(
            StandardTestDevice desiredDeviceType)
        {
            var testDevice = GetDevice(desiredDeviceType);
            var keyReference = new KeyReference(ScpKeyIds.Scp11B, 0x1);
            var keyParams = Get_Scp11b_EncryptedChannel_Parameters(testDevice, keyReference);

            using var session = new PivSession(testDevice, keyParams);
            session.ResetApplication();

            session.KeyCollector = new Simple39KeyCollector().Simple39KeyCollectorDelegate;
            var isVerified = session.TryVerifyPin();
            Assert.True(isVerified);

            var result = session.GenerateKeyPair(PivSlot.Retired12, PivAlgorithm.EccP256);
            Assert.Equal(PivAlgorithm.EccP256, result.Algorithm);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp11b_OathSession_Operations_Succeeds(
            StandardTestDevice desiredDeviceType)
        {
            var testDevice = GetDevice(desiredDeviceType);
            var keyReference = new KeyReference(ScpKeyIds.Scp11B, 0x1);
            var keyParams = Get_Scp11b_EncryptedChannel_Parameters(testDevice, keyReference);

            using (var resetSession = new OathSession(testDevice, keyParams))
            {
                resetSession.ResetApplication();
            }

            using var session = new OathSession(testDevice, keyParams);
            session.KeyCollector = new SimpleOathKeyCollector().SimpleKeyCollectorDelegate;

            session.SetPassword();
            Assert.True(session.IsPasswordProtected);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp11b_OtpSession_Operations_Succeeds(
            StandardTestDevice desiredDeviceType)
        {
            var testDevice = GetDevice(desiredDeviceType);
            var keyReference = new KeyReference(ScpKeyIds.Scp11B, 0x1);
            var keyParams = Get_Scp11b_EncryptedChannel_Parameters(testDevice, keyReference);

            using var session = new OtpSession(testDevice, keyParams);
            if (session.IsLongPressConfigured)
            {
                session.DeleteSlot(Slot.LongPress);
            }

            var configObj = session.ConfigureStaticPassword(Slot.LongPress);
            var generatedPassword = new Memory<char>(new char[16]);
            configObj = configObj.WithKeyboard(KeyboardLayout.en_US);
            configObj = configObj.GeneratePassword(generatedPassword);

            configObj.Execute();
        }
        
        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp11b_YubiHsmSession_Operations_Succeeds(
            StandardTestDevice desiredDeviceType)
        {
            var testDevice = YhaTestUtilities.GetCleanDevice(desiredDeviceType);
            var keyReference = new KeyReference(ScpKeyIds.Scp11B, 0x1);
            var keyParams = Get_Scp11b_EncryptedChannel_Parameters(testDevice, keyReference);

            using var session = new YubiHsmAuthSession(testDevice, keyParams);
            session.AddCredential(YhaTestUtilities.DefaultMgmtKey, YhaTestUtilities.DefaultAes128Cred);
            
            var result = session.ListCredentials();
            Assert.Single(result);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp11b_Establish_Connection_Succeeds(
            StandardTestDevice desiredDeviceType)
        {
            var testDevice = GetDevice(desiredDeviceType);
            var keyReference = new KeyReference(ScpKeyIds.Scp11B, 0x1);

            IReadOnlyCollection<X509Certificate2> certificateList;
            using (var session = new SecurityDomainSession(testDevice))
            {
                certificateList = session.GetCertificates(keyReference);
            }

            var leaf = certificateList.Last();
            var ecDsaPublicKey = leaf.PublicKey.GetECDsaPublicKey()!.ExportParameters(false);
            var keyParams = new Scp11KeyParameters(keyReference, new ECPublicKeyParameters(ecDsaPublicKey));

            using (var session = new SecurityDomainSession(testDevice, keyParams))
            {
                var result = session.GetKeyInformation();
                Assert.NotEmpty(result);
            }
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp11b_Import_Succeeds(
            StandardTestDevice desiredDeviceType)
        {
            var testDevice = GetDevice(desiredDeviceType);
            var keyReference = new KeyReference(ScpKeyIds.Scp11B, 0x2);

            // Start authenticated session with default key
            using var session = new SecurityDomainSession(testDevice, Scp03KeyParameters.DefaultKey);

            // Import private key
            var ecDsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var privateKey = new ECPrivateKeyParameters(ecDsa);
            session.PutKey(keyReference, privateKey, 0);

            var result = session.GetKeyInformation();
            Assert.NotEmpty(result);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp11b_GetCertificates_IsNotEmpty(
            StandardTestDevice desiredDeviceType)
        {
            var testDevice = GetDevice(desiredDeviceType);
            using var session = new SecurityDomainSession(testDevice);

            var keyReference = new KeyReference(ScpKeyIds.Scp11B, 0x1);
            var certificateList = session.GetCertificates(keyReference);

            Assert.NotEmpty(certificateList);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp11b_StoreCertificates_CanBeRetrieved(
            StandardTestDevice desiredDeviceType)
        {
            var testDevice = GetDevice(desiredDeviceType);
            var keyReference = new KeyReference(ScpKeyIds.Scp11B, 0x1);

            using var session = new SecurityDomainSession(testDevice, Scp03KeyParameters.DefaultKey);
            var oceCertificates = GetOceCertificates(Scp11TestData.OceCerts.Span);

            session.StoreCertificates(keyReference, oceCertificates.Bundle);
            var result = session.GetCertificates(keyReference);

            // Assert that we can store and retrieve the off card entity certificate
            var oceThumbprint = oceCertificates.Bundle.Single().Thumbprint;
            Assert.Single(result);
            Assert.Equal(oceThumbprint, result[0].Thumbprint);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp11a_GenerateEcKey_Succeeds(
            StandardTestDevice desiredDeviceType)
        {
            var testDevice = GetDevice(desiredDeviceType);
            var keyReference = new KeyReference(ScpKeyIds.Scp11A, 0x3);

            // Start authenticated session
            using var session = new SecurityDomainSession(testDevice, Scp03KeyParameters.DefaultKey);

            // Generate a new EC key
            var generatedKey = session.GenerateEcKey(keyReference, 0);

            // Verify the generated key
            Assert.NotNull(generatedKey.Parameters.Q.X);
            Assert.NotNull(generatedKey.Parameters.Q.Y);
            Assert.Equal(32, generatedKey.Parameters.Q.X.Length);
            Assert.Equal(32, generatedKey.Parameters.Q.Y.Length);
            Assert.Equal(ECCurve.NamedCurves.nistP256.Oid.Value, generatedKey.Parameters.Curve.Oid.Value);

            using var ecdsa = ECDsa.Create(generatedKey.Parameters);
            Assert.NotNull(ecdsa);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp11a_WithAllowList_AllowsApprovedSerials(
            StandardTestDevice desiredDeviceType)
        {
            var testDevice = GetDevice(desiredDeviceType);
            const byte kvn = 0x05;
            var oceKeyRef = new KeyReference(OceKid, kvn);

            Scp11KeyParameters keyParams;
            using (var session = new SecurityDomainSession(testDevice, Scp03KeyParameters.DefaultKey))
            {
                keyParams = LoadKeys(session, ScpKeyIds.Scp11A, kvn);
            }

            using (var session = new SecurityDomainSession(testDevice, keyParams))
            {
                var serials = new List<string>
                {
                    // Serial numbers from oce certs
                    "7F4971B0AD51F84C9DA9928B2D5FEF5E16B2920A",
                    "6B90028800909F9FFCD641346933242748FBE9AD"
                };

                // Only the above serials shall work. 
                session.StoreAllowlist(oceKeyRef, serials);
            }

            using (var session = new SecurityDomainSession(testDevice, keyParams))
            {
                session.DeleteKey(new KeyReference(ScpKeyIds.Scp11A, kvn));
            }
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp11a_WithAllowList_BlocksUnapprovedSerials(
            StandardTestDevice desiredDeviceType)
        {
            var testDevice = GetDevice(desiredDeviceType);
            const byte kvn = 0x03;
            var oceKeyRef = new KeyReference(OceKid, kvn);

            Scp03KeyParameters scp03KeyParams;
            using (var session = new SecurityDomainSession(testDevice, Scp03KeyParameters.DefaultKey))
            {
                // Import SCP03 key and get key parameters
                scp03KeyParams = ImportScp03Key(session);
            }

            Scp11KeyParameters scp11KeyParams;
            using (var session = new SecurityDomainSession(testDevice, scp03KeyParams))
            {
                // Make space for new key
                session.DeleteKey(new KeyReference(ScpKeyIds.Scp11B, 0x01), false);

                // Load SCP11a keys
                scp11KeyParams = LoadKeys(session, ScpKeyIds.Scp11A, kvn);

                // Create list of serial numbers
                var serials = new List<string>
                {
                    "01",
                    "02",
                    "03"
                };

                // Store the allow list
                session.StoreAllowlist(oceKeyRef, serials);
            }

            // This is the test. Authenticate with SCP11a should throw.
            Assert.Throws<SecureChannelException>(() =>
            {
                using (var session = new SecurityDomainSession(testDevice, scp11KeyParams))
                {
                    // ... Authenticated
                }
            });

            // Reset the allow list
            using (var session = new SecurityDomainSession(testDevice, scp03KeyParams))
            {
                session.ClearAllowList(oceKeyRef);
            }

            // Now, with the allowlist removed, authenticate with SCP11a should now succeed
            using (var session = new SecurityDomainSession(testDevice, scp11KeyParams))
            {
                // ... Authenticated
            }
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp11a_Authenticate_Succeeds(
            StandardTestDevice desiredDeviceType)
        {
            var testDevice = GetDevice(desiredDeviceType);
            const byte kvn = 0x03;
            var keyRef = new KeyReference(ScpKeyIds.Scp11A, kvn);

            // Start authenticated session with default key
            Scp11KeyParameters keyParams;
            using (var session = new SecurityDomainSession(testDevice, Scp03KeyParameters.DefaultKey))
            {
                keyParams = LoadKeys(session, ScpKeyIds.Scp11A, kvn);
            }

            // Start authenticated session using new key params and public key from yubikey
            using (var session = new SecurityDomainSession(testDevice, keyParams))
            {
                session.DeleteKey(keyRef);
            }
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5)]
        [InlineData(StandardTestDevice.Fw5Fips)]
        public void Scp11c_Authenticate_Succeeds(
            StandardTestDevice desiredDeviceType)
        {
            var testDevice = GetDevice(desiredDeviceType);
            const byte kvn = 0x03;
            var keyReference = new KeyReference(ScpKeyIds.Scp11C, kvn);

            Scp11KeyParameters keyParams;
            using (var session = new SecurityDomainSession(testDevice, Scp03KeyParameters.DefaultKey))
            {
                keyParams = LoadKeys(session, ScpKeyIds.Scp11C, kvn);
            }

            Assert.Throws<SecureChannelException>(() =>
            {
                using var session = new SecurityDomainSession(testDevice, keyParams);
                session.DeleteKey(keyReference);
            });
        }

        private Scp11KeyParameters LoadKeys(
            SecurityDomainSession session,
            byte scpKid,
            byte kvn)
        {
            var sessionRef = new KeyReference(scpKid, kvn);
            var oceRef = new KeyReference(OceKid, kvn);

            // Generate new key pair on YubiKey and store public key for later use
            var newPublicKey = session.GenerateEcKey(sessionRef, 0);

            var oceCerts = GetOceCertificates(Scp11TestData.OceCerts.Span);
            if (oceCerts.Ca == null)
            {
                throw new InvalidOperationException("Missing CA certificate");
            }

            // Put Oce Keys
            var ocePublicKey = new ECPublicKeyParameters(oceCerts.Ca.PublicKey.GetECDsaPublicKey()!);
            session.PutKey(oceRef, ocePublicKey, 0);

            // Get Oce subject key identifier
            var ski = GetSki(oceCerts.Ca);
            if (ski.IsEmpty)
            {
                throw new InvalidOperationException("CA certificate missing Subject Key Identifier");
            }

            // Store the key identifier with the referenced off card entity on the Yubikey
            session.StoreCaIssuer(oceRef, ski);

            var (certChain, privateKey) =
                GetOceCertificateChainAndPrivateKey(Scp11TestData.Oce, Scp11TestData.OcePassword);

            // Now we have the EC private key parameters and cert chain
            return new Scp11KeyParameters(
                sessionRef,
                new ECPublicKeyParameters(newPublicKey.Parameters),
                oceRef,
                new ECPrivateKeyParameters(privateKey),
                certChain
            );
        }

        private static (List<X509Certificate2> certChain, ECParameters privateKey) GetOceCertificateChainAndPrivateKey(
            ReadOnlyMemory<byte> ocePkcs12,
            ReadOnlyMemory<char> ocePassword)
        {
            // Load the OCE PKCS12 using Bouncy Castle Pkcs12 Store
            using var pkcsStream = new MemoryStream(ocePkcs12.ToArray());
            var pkcs12Store = new Pkcs12Store(pkcsStream, ocePassword.ToArray());

            // Get the first alias (usually there's only one)
            var alias = pkcs12Store.Aliases.Cast<string>().FirstOrDefault();
            if (alias == null || !pkcs12Store.IsKeyEntry(alias))
            {
                throw new InvalidOperationException("No private key entry found in PKCS12");
            }

            // Get the certificate chain
            var x509CertificateEntries = pkcs12Store.GetCertificateChain(alias);
            var x509Certs = x509CertificateEntries
                .Select(certEntry =>
                {
                    var cert = DotNetUtilities.ToX509Certificate(certEntry.Certificate);
                    return new X509Certificate2(
                        cert.Export(X509ContentType.Cert)
                    );
                });

            var certs = ScpCertificates.From(x509Certs);
            var certChain = new List<X509Certificate2>(certs.Bundle);
            if (certs.Leaf != null)
            {
                certChain.Add(certs.Leaf);
            }

            // Get the private key
            var privateKeyEntry = pkcs12Store.GetKey(alias);
            if (!(privateKeyEntry.Key is Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters ecPrivateKey))
            {
                throw new InvalidOperationException("Private key is not an EC key");
            }

            return (certChain, ConvertToECParameters(ecPrivateKey));
        }

        static ECParameters ConvertToECParameters(
            Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters bcPrivateKey)
        {
            // Convert the BigInteger D to byte array
            var dBytes = bcPrivateKey.D.ToByteArrayUnsigned();

            // Calculate public key point Q = d*G
            var Q = bcPrivateKey.Parameters.G.Multiply(bcPrivateKey.D);

            // Get X and Y coordinates as byte arrays
            var xBytes = Q.XCoord.ToBigInteger().ToByteArrayUnsigned();
            var yBytes = Q.YCoord.ToBigInteger().ToByteArrayUnsigned();

            // Create ECParameters with P-256 curve
            return new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                D = dBytes,
                Q = new ECPoint
                {
                    X = xBytes,
                    Y = yBytes
                }
            };
        }

        private ScpCertificates GetOceCertificates(
            ReadOnlySpan<byte> pem)
        {
            try
            {
                var certificates = new List<X509Certificate2>();

                // Convert PEM to a string
                var pemString = Encoding.UTF8.GetString(pem);

                // Split the PEM string into individual certificates
                var pemCerts = pemString.Split(
                    new[] { "-----BEGIN CERTIFICATE-----", "-----END CERTIFICATE-----" },
                    StringSplitOptions.RemoveEmptyEntries
                );

                foreach (var certString in pemCerts)
                {
                    if (!string.IsNullOrWhiteSpace(certString))
                    {
                        // Remove any whitespace and convert to byte array
                        var certData = Convert.FromBase64String(certString.Trim());
                        var cert = new X509Certificate2(certData);
                        certificates.Add(cert);
                    }
                }

                return ScpCertificates.From(certificates);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to parse PEM certificates", ex);
            }
        }

        private Memory<byte> GetSki(
            X509Certificate2 certificate)
        {
            var extension = certificate.Extensions["2.5.29.14"];
            if (!(extension is X509SubjectKeyIdentifierExtension skiExtension))
            {
                throw new InvalidOperationException("Invalid Subject Key Identifier extension");
            }

            var rawData = skiExtension.RawData;
            if (rawData == null || rawData.Length == 0)
            {
                throw new InvalidOperationException("Missing Subject Key Identifier");
            }

            var tlv = TlvObject.Parse(skiExtension.RawData);
            return tlv.Value;
        }

        private static Scp03KeyParameters ImportScp03Key(
            SecurityDomainSession session)
        {
            // assumeFalse("SCP03 management not supported over NFC on FIPS capable devices",
            //     state.getDeviceInfo().getFipsCapable() != 0 && !state.isUsbTransport()); // todo


            var scp03Ref = new KeyReference(0x01, 0x01);
            var staticKeys = new StaticKeys(
                GetRandomBytes(16),
                GetRandomBytes(16),
                GetRandomBytes(16)
            );

            session.PutKey(scp03Ref, staticKeys, 0);

            return new Scp03KeyParameters(scp03Ref, staticKeys);
        }

        private static Memory<byte> GetRandomBytes(
            byte length)
        {
            using var rng = CryptographyProviders.RngCreator();
            Span<byte> hostChallenge = stackalloc byte[length];
            rng.GetBytes(hostChallenge);

            return hostChallenge.ToArray();
        }

        /// <summary>
        /// This is a copy of Scp11b_Authenticate_Succeeds test
        /// </summary>
        /// <param name="testDevice"></param>
        /// <param name="keyReference"></param>
        /// <returns></returns>
        private static Scp11KeyParameters Get_Scp11b_EncryptedChannel_Parameters(
            IYubiKeyDevice testDevice,
            KeyReference keyReference)
        {
            IReadOnlyCollection<X509Certificate2> certificateList;
            using (var session = new SecurityDomainSession(testDevice))
            {
                certificateList = session.GetCertificates(keyReference);
            }

            var leaf = certificateList.Last();
            var ecDsaPublicKey = leaf.PublicKey.GetECDsaPublicKey()!.ExportParameters(false);
            var keyParams = new Scp11KeyParameters(keyReference, new ECPublicKeyParameters(ecDsaPublicKey));

            return keyParams;
        }
    }
}
