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
using Yubico.Core.Tlv;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.TestUtilities;
using ECCurve = System.Security.Cryptography.ECCurve;
using ECPoint = System.Security.Cryptography.ECPoint;


namespace Yubico.YubiKey.Scp
{
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class Scp11Tests
    {
        private const byte OceKid = 0x010;
        private IYubiKeyDevice Device { get; set; }

        public Scp11Tests()
        {
            Device = IntegrationTestDeviceEnumeration.GetTestDevice(
                Transport.SmartCard,
                minimumFirmwareVersion: FirmwareVersion.V5_7_2);

            using var session = new SecurityDomainSession(Device);
            session.Reset();
        }

        [Fact]
        public void Scp11b_PutKey_WithPublicKey_Succeeds()
        {
            var keyReference = new KeyReference(0x10, 0x3);

            using var session = new SecurityDomainSession(Device, Scp03KeyParameters.DefaultKey);
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

            var publicKey = new ECPublicKeyParameters(ecdsa);
            session.PutKey(keyReference, publicKey, 0);

            var keyInformation = session.GetKeyInformation();
            Assert.True(keyInformation.ContainsKey(keyReference));
        }

        [Fact]
        public void Scp11b_Authenticate_Succeeds() // Works
        {
            IReadOnlyCollection<X509Certificate2> certificateList;
            var keyReference = new KeyReference(ScpKid.Scp11b, 0x1);
            using (var session = new SecurityDomainSession(Device))
            {
                certificateList = session.GetCertificates(keyReference);
            }

            var leaf = certificateList.Last();
            var ecDsaPublicKey = leaf.PublicKey.GetECDsaPublicKey()!.ExportParameters(false);
            var keyParams = new Scp11KeyParameters(keyReference, new ECPublicKeyParameters(ecDsaPublicKey));

            // Try to create authenticated session using key params and public key from yubikey
            using (var session = new SecurityDomainSession(Device, keyParams))
            {
                var result = session.GetKeyInformation();
                Assert.NotEmpty(result);
            }
        }

        [Fact]
        public void Scp11b_Import_Succeeds() // Works
        {
            var keyReference = new KeyReference(ScpKid.Scp11b, 0x2);
            var ecDsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

            // Start authenticated session with default key
            using var session = new SecurityDomainSession(Device, Scp03KeyParameters.DefaultKey);

            // Import private key
            var privateKey = new ECPrivateKeyParameters(ecDsa);
            session.PutKey(keyReference, privateKey, 0);

            var result = session.GetKeyInformation();
            Assert.NotEmpty(result);
        }

        [Fact]
        public void GetCertificates_IsNotEmpty() // Works
        {
            using var session = new SecurityDomainSession(Device);

            var keyReference = new KeyReference(ScpKid.Scp11b, 0x1);
            var certificateList = session.GetCertificates(keyReference);

            Assert.NotEmpty(certificateList);
        }

        [Fact]
        public void Scp11b_StoreCertificates_CanBeRetrieved()
        {
            var keyReference = new KeyReference(ScpKid.Scp11b, 0x1);

            using var session = new SecurityDomainSession(Device, Scp03KeyParameters.DefaultKey);
            var oceCertificates = GetOceCertificates(Scp11TestData.OceCerts.Span);

            session.StoreCertificates(keyReference, oceCertificates.Bundle);
            var result = session.GetCertificates(keyReference);

            // Assert that we can store and retrieve the off card entity certificate
            var oceThumbprint = oceCertificates.Bundle.Single().Thumbprint;
            Assert.Single(result);
            Assert.Equal(oceThumbprint, result[0].Thumbprint);
        }

        [Fact]
        public void Scp11a_GenerateEcKey_Succeeds() // Works
        {
            var keyReference = new KeyReference(ScpKid.Scp11a, 0x3);

            // Start authenticated session
            using var session = new SecurityDomainSession(Device, Scp03KeyParameters.DefaultKey);

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

        [Fact]
        public void Scp11a_WithAllowList_AllowsApprovedSerials()
        {
            const byte kvn = 0x05;
            var oceKeyRef = new KeyReference(OceKid, kvn);

            Scp11KeyParameters keyParams; // Means this is not containing the correct OCECerts?
            using (var session = new SecurityDomainSession(Device, Scp03KeyParameters.DefaultKey))
            {
                keyParams = LoadKeys(session, ScpKid.Scp11a, kvn);
            }

            using (var session = new SecurityDomainSession(Device, keyParams))
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

            using (var session = new SecurityDomainSession(Device, keyParams))
            {
                session.DeleteKeySet(new KeyReference(ScpKid.Scp11a, kvn));
            }
        }

        [Fact]
        public void Scp11a_WithAllowList_BlocksUnapprovedSerials() // Works
        {
            const byte kvn = 0x03;
            var oceKeyRef = new KeyReference(OceKid, kvn);

            Scp03KeyParameters scp03KeyParams;
            using (var session = new SecurityDomainSession(Device, Scp03KeyParameters.DefaultKey))
            {
                // Import SCP03 key and get key parameters
                scp03KeyParams = ImportScp03Key(session);
            }

            Scp11KeyParameters scp11KeyParams;
            using (var session = new SecurityDomainSession(Device, scp03KeyParams))
            {
                // Make space for new key
                session.DeleteKeySet(new KeyReference(ScpKid.Scp11b, 0x01), false);

                // Load SCP11a keys
                scp11KeyParams = LoadKeys(session, ScpKid.Scp11a, kvn);

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
                using (var session = new SecurityDomainSession(Device, scp11KeyParams))
                {
                    // ... Authenticated
                }
            });

            // Reset the allow list
            using (var session = new SecurityDomainSession(Device, scp03KeyParams))
            {
                session.ClearAllowList(oceKeyRef);
            }

            // Now, with the allowlist removed, authenticate with SCP11a should now succeed
            using (var session = new SecurityDomainSession(Device, scp11KeyParams))
            {
                // ... Authenticated
            }
        }

        [Fact]
        public void Scp11a_Authenticate_Succeeds() // Works
        {
            const byte kvn = 0x03;
            var keyRef = new KeyReference(ScpKid.Scp11a, kvn);

            // Start authenticated session with default key
            Scp11KeyParameters keyParams;
            using (var session = new SecurityDomainSession(Device, Scp03KeyParameters.DefaultKey))
            {
                keyParams = LoadKeys(session, ScpKid.Scp11a, kvn);
            }

            // Start authenticated session using new key params and public key from yubikey
            using (var session = new SecurityDomainSession(Device, keyParams))
            {
                session.DeleteKeySet(keyRef);
            }
        }

        [Fact]
        public void Scp11c_Authenticate_Succeeds()
        {
            const byte kvn = 0x03;
            var keyReference = new KeyReference(ScpKid.Scp11c, kvn);

            Scp11KeyParameters keyParams;
            using (var session = new SecurityDomainSession(Device, Scp03KeyParameters.DefaultKey))
            {
                keyParams = LoadKeys(session, ScpKid.Scp11c, kvn);
            }

            Assert.Throws<SecureChannelException>(() =>
            {
                using var session = new SecurityDomainSession(Device, keyParams);
                session.DeleteKeySet(keyReference);
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

            var (certChain, privateKey) = GetOceCertificateChainAndPrivateKey();

            // Now we have the EC private key parameters and cert chain
            return new Scp11KeyParameters(
                sessionRef,
                new ECPublicKeyParameters(newPublicKey.Parameters),
                oceRef,
                new ECPrivateKeyParameters(privateKey),
                certChain
            );
        }

        private static (List<X509Certificate2> certChain, ECParameters privateKey) GetOceCertificateChainAndPrivateKey()
        {
            // Load the OCE PKCS12 using Bouncy Castle
            using var pkcsStream = new MemoryStream(Scp11TestData.Oce.ToArray());
            var pkcs12Store = new Pkcs12Store(pkcsStream, Scp11TestData.OcePassword.ToArray());

            // Get the first alias (usually there's only one)
            var alias = pkcs12Store.Aliases.Cast<string>().FirstOrDefault();
            if (alias == null || !pkcs12Store.IsKeyEntry(alias))
            {
                throw new InvalidOperationException("No private key entry found in PKCS12");
            }

            // Get the private key
            var privateKeyEntry = pkcs12Store.GetKey(alias);
            if (!(privateKeyEntry.Key is Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters ecPrivateKey))
            {
                throw new InvalidOperationException("Private key is not an EC key");
            }

            var x509CertificateEntries = pkcs12Store.GetCertificateChain(alias);
            var x509Certs = x509CertificateEntries
                    .Select(certEntry =>
                    {
                        var cert = DotNetUtilities.ToX509Certificate(certEntry.Certificate);
                        return new X509Certificate2(
                            cert.Export(X509ContentType.Cert),
                            (string)null!, // no password needed for public cert
                            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet
                        );
                    });

            var certs = ScpCertificates.From(x509Certs); 
            var certChain = new List<X509Certificate2>(certs.Bundle);
            if (certs.Leaf != null)
            {
                certChain.Add(certs.Leaf);
            }

            return (certChain, ConvertToECParameters(ecPrivateKey));
        }

        static ECParameters ConvertToECParameters(
            Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters bcPrivateKey)
        {
            // Convert the BigInteger D to byte array
            byte[] dBytes = bcPrivateKey.D.ToByteArrayUnsigned();

            // Calculate public key point Q = d*G
            var Q = bcPrivateKey.Parameters.G.Multiply(bcPrivateKey.D);

            // Get X and Y coordinates as byte arrays
            byte[] xBytes = Q.XCoord.ToBigInteger().ToByteArrayUnsigned();
            byte[] yBytes = Q.YCoord.ToBigInteger().ToByteArrayUnsigned();

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
                string pemString = Encoding.UTF8.GetString(pem);

                // Split the PEM string into individual certificates
                string[] pemCerts = pemString.Split(
                    new[] { "-----BEGIN CERTIFICATE-----", "-----END CERTIFICATE-----" },
                    StringSplitOptions.RemoveEmptyEntries
                );

                foreach (string certString in pemCerts)
                {
                    if (!string.IsNullOrWhiteSpace(certString))
                    {
                        // Remove any whitespace and convert to byte array
                        byte[] certData = Convert.FromBase64String(certString.Trim());
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
    }


    public static class Scp11TestData
    {
        public readonly static ReadOnlyMemory<byte> OceCerts = Encoding.UTF8.GetBytes(
            "-----BEGIN CERTIFICATE-----\n" +
            "MIIB8DCCAZegAwIBAgIUf0lxsK1R+EydqZKLLV/vXhaykgowCgYIKoZIzj0EAwIw\n" +
            "KjEoMCYGA1UEAwwfRXhhbXBsZSBPQ0UgUm9vdCBDQSBDZXJ0aWZpY2F0ZTAeFw0y\n" +
            "NDA1MjgwOTIyMDlaFw0yNDA4MjYwOTIyMDlaMC8xLTArBgNVBAMMJEV4YW1wbGUg\n" +
            "T0NFIEludGVybWVkaWF0ZSBDZXJ0aWZpY2F0ZTBZMBMGByqGSM49AgEGCCqGSM49\n" +
            "AwEHA0IABMXbjb+Y33+GP8qUznrdZSJX9b2qC0VUS1WDhuTlQUfg/RBNFXb2/qWt\n" +
            "h/a+Ag406fV7wZW2e4PPH+Le7EwS1nyjgZUwgZIwHQYDVR0OBBYEFJzdQCINVBES\n" +
            "R4yZBN2l5CXyzlWsMB8GA1UdIwQYMBaAFDGqVWafYGfoHzPc/QT+3nPlcZ89MBIG\n" +
            "A1UdEwEB/wQIMAYBAf8CAQAwDgYDVR0PAQH/BAQDAgIEMCwGA1UdIAEB/wQiMCAw\n" +
            "DgYMKoZIhvxrZAAKAgEoMA4GDCqGSIb8a2QACgIBADAKBggqhkjOPQQDAgNHADBE\n" +
            "AiBE5SpNEKDW3OehDhvTKT9g1cuuIyPdaXGLZ3iX0x0VcwIgdnIirhlKocOKGXf9\n" +
            "ijkE8e+9dTazSPLf24lSIf0IGC8=\n" +
            "-----END CERTIFICATE-----\n" +
            "-----BEGIN CERTIFICATE-----\n" +
            "MIIB2zCCAYGgAwIBAgIUSf59wIpCKOrNGNc5FMPTD9zDGVAwCgYIKoZIzj0EAwIw\n" +
            "KjEoMCYGA1UEAwwfRXhhbXBsZSBPQ0UgUm9vdCBDQSBDZXJ0aWZpY2F0ZTAeFw0y\n" +
            "NDA1MjgwOTIyMDlaFw0yNDA2MjcwOTIyMDlaMCoxKDAmBgNVBAMMH0V4YW1wbGUg\n" +
            "T0NFIFJvb3QgQ0EgQ2VydGlmaWNhdGUwWTATBgcqhkjOPQIBBggqhkjOPQMBBwNC\n" +
            "AASPrxfpSB/AvuvLKaCz1YTx68Xbtx8S9xAMfRGwzp5cXMdF8c7AWpUfeM3BQ26M\n" +
            "h0WPvyBJKhCdeK8iVCaHyr5Jo4GEMIGBMB0GA1UdDgQWBBQxqlVmn2Bn6B8z3P0E\n" +
            "/t5z5XGfPTASBgNVHRMBAf8ECDAGAQH/AgEBMA4GA1UdDwEB/wQEAwIBBjA8BgNV\n" +
            "HSABAf8EMjAwMA4GDCqGSIb8a2QACgIBFDAOBgwqhkiG/GtkAAoCASgwDgYMKoZI\n" +
            "hvxrZAAKAgEAMAoGCCqGSM49BAMCA0gAMEUCIHv8cgOzxq2n1uZktL9gCXSR85mk\n" +
            "TieYeSoKZn6MM4rOAiEA1S/+7ez/gxDl01ztKeoHiUiW4FbEG4JUCzIITaGxVvM=\n" +
            "-----END CERTIFICATE-----").AsMemory();

        // PKCS12 certificate with a private key and full certificate chain
        public static readonly Memory<byte> Oce = Convert.FromBase64String(
            "MIIIfAIBAzCCCDIGCSqGSIb3DQEHAaCCCCMEgggfMIIIGzCCBtIGCSqGSIb3DQEHBqCCBsMwgga/" +
            "AgEAMIIGuAYJKoZIhvcNAQcBMFcGCSqGSIb3DQEFDTBKMCkGCSqGSIb3DQEFDDAcBAg8IcJO44iS" +
            "gAICCAAwDAYIKoZIhvcNAgkFADAdBglghkgBZQMEASoEEAllIHdoQx/USA3jmRMeciiAggZQAHCP" +
            "J5lzPV0Z5tnssXZZ1AWm8AcKEq28gWUTVqVxc+0EcbKQHig1Jx7rqC3q4G4sboIRw1vDH6q5O8eG" +
            "sbkeNuYBim8fZ08JrsjeJABJoEiJrPqplMWA7H6a7athg3YSu1v4OR3UKN5Gyzn3s0Yx5yMm/xzw" +
            "204TEK5/1LpK8AMcUliFSq7jw3Xl1RY0zjMSWyQjX0KmB9IdubqQCfhy8zkKluAQADtHsEYAn0F3" +
            "LoMETQytyUSkIvGMZoFemkCWV7zZ5n5IPhXL7gvnTu0WS8UxEnz/+FYdF43cjmwGfSb3OpaxOND4" +
            "PBCpwzbFfVCLa6mUBlwq1KQWRm1+PFm4LnL+3s2mxfjJAsVYP4U722/FHpW8rdTsyvdift9lsQja" +
            "s2jIjCu8PFClFZJLQldu5FxOhKzx2gsjYS/aeTdefwjlRiGtEFSrE1snKBbnBeRYFocBjhTD/sy3" +
            "Vj0i5sbWwTx7iq67joWydWAMp/lGSZ6akWRsyku/282jlwYsc3pR05qCHkbV0TzJcZofhXBwRgH5" +
            "NKfulnJ1gH+i3e3RT3TauAKlqCeAfvDvA3+jxEDy/puPncod7WH0m9P4OmXjZ0s5EI4U+v6bKPgL" +
            "7LlTCEI6yj15P7kxmruoxZlDAmhixVmlwJ8ZbVxD6Q+AOhXYPg+il3AYaRAS+VyJla0K+ac6hpYV" +
            "AnbZCPzgHVkKC6iq4a/azf2b4uq9ks109jjnryAChdBsGdmStpZaPW4koMSAIJf12vGRp5jNjSax" +
            "aIL5QxTn0WCO8FHi1oqTmlTSWvR8wwZLiBmqQtnNTpewiLL7C22lerUT7pYvKLCq/nnPYtb5UrST" +
            "HrmTNOUzEGVOSAGUWV293S4yiPGIwxT3dPE5/UaU/yKq1RonMRaPhOZEESZEwLKVCqyDVEbAt7Hd" +
            "ahp+Ex0FVrC5JQhpVQ0Wn6uCptF2Jup70u+P2kVWjxrGBuRrlgEkKuHcohWoO9EMX/bLK9KcY4s1" +
            "ofnfgSNagsAyX7N51Bmahgz1MCFOEcuFa375QYQhqkyLO2ZkNTpFQtjHjX0izZWO55LN3rNpcD9+" +
            "fZt6ldoZCpg+t6y5xqHy+7soH0BpxF1oGIHAUkYSuXpLY0M7Pt3qqvsJ4/ycmFUEyoGv8Ib/ieUB" +
            "bebPz0Uhn+jaTpjgtKCyym7nBxVCuUv39vZ31nhNr4WaFsjdB/FOJh1s4KI6kQgzCSObrIVXBcLC" +
            "TXPfZ3jWxspKIREHn+zNuW7jIkbugSRiNFfVArcc7cmU4av9JPSmFiZzeyA0gkrkESTg8DVPT16u" +
            "7W5HREX4CwmKu+12R6iYQ/po9Hcy6NJ8ShLdAzU0+q/BzgH7Cb8qimjgfGBA3Mesc+P98FlCzAjB" +
            "2EgucRuXuehM/FemmZyNl0qI1Mj9qOgx/HeYaJaYD+yXwojApmetFGtDtMJsDxwL0zK7eGXeHHa7" +
            "pd7OybKdSjDq25CCTOZvfR0DD55FDIGCy0FsJTcferzPFlkz/Q45vEwuGfEBnXXS9IhH4ySvJmDm" +
            "yfLMGiHW6t+9gjyEEg+dwSOq9yXYScfCsefRl7+o/9nDoNQ8s/XS7LKlJ72ZEBaKeAxcm6q4wVwU" +
            "WITNNl1R3EYAsFBWzYt4Ka9Ob3igVaNfeG9K4pfQqMWcPpqVp4FuIsEpDWZYuv71s+WMYCs1JMfH" +
            "bHDUczdRet1Ir2vLDGeWwvci70AzeKvvQ9OwBVESRec6cVrgt3EJWLey5sXY01WpMm526fwtLolS" +
            "MpCf+dNePT97nXemQCcr3QXimagHTSGPngG3577FPrSQJl+lCJDYxBFFtnd6hq4OcVr5HiNAbLnS" +
            "jBWbzqxhHMmgoojy4rwtHmrfyVYKXyl+98r+Lobitv2tpnBqmjL6dMPRBOJvQl8+Wp4MGBsi1gvT" +
            "gW/+pLlMXT++1iYyxBeK9/AN5hfjtrivewE3JY531jwkrl3rUl50MKwBJMMAtQQIYrDg7DAg/+Qc" +
            "Oi+2mgo9zJPzR2jIXF0wP+9FA4+MITa2v78QVXcesh63agcFJCayGAL1StnbSBvvDqK5vEei3uGZ" +
            "beJEpU1hikQx57w3UzS9O7OSQMFvRBOrFBQsYC4JzfF0soIweGNpJxpm+UNYz+hB9vCb8+3OHA06" +
            "9M0CAlJVOTF9uEpLVRzK+1kwggFBBgkqhkiG9w0BBwGgggEyBIIBLjCCASowggEmBgsqhkiG9w0B" +
            "DAoBAqCB7zCB7DBXBgkqhkiG9w0BBQ0wSjApBgkqhkiG9w0BBQwwHAQIexxrwNlHM34CAggAMAwG" +
            "CCqGSIb3DQIJBQAwHQYJYIZIAWUDBAEqBBAkK96h6gHJglyJl1/yEylvBIGQh62z7u5RoQ9y5wIX" +
            "bE3/oMQTKVfCSrtqGUmj38sxDY7yIoTVQq7sw0MPNeYHROgGUAzawU0DlXMGuOWrbgzYeURZs0/H" +
            "Z2Cqk8qhVnD8TgpB2n0U0NB7aJRHlkzTl5MLFAwn3NE49CSzb891lGwfLYXYCfNfqltD7xZ7uvz6" +
            "JAo/y6UtY8892wrRv4UdejyfMSUwIwYJKoZIhvcNAQkVMRYEFJBU0s1/6SLbIRbyeq65gLWqClWN" +
            "MEEwMTANBglghkgBZQMEAgEFAAQgqkOJRTcBlnx5yn57k23PH+qUXUGPEuYkrGy+DzEQiikECB0B" +
            "XjHOZZhuAgIIAA==");

        public static readonly ReadOnlyMemory<char> OcePassword = "password".AsMemory();
    }
}
