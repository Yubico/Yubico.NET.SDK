using System;
using System.IO;
using Xunit;

namespace Yubico.YubiKey.TestUtilities
{
    public class TestKeysTests
    {
        [Fact]
        public void TestKey_LoadRSA_CanReadPublicKey()
        {
            var key = TestKeys.GetPublicKey("rsa4096");
            
            var rsaKey = key.AsRSA();
            Assert.NotNull(rsaKey);
            Assert.Equal(4096, rsaKey.KeySize);
        }

        [Fact]
        public void TestKey_LoadRSA_CanReadPrivateKey()
        {
            var key = TestKeys.GetPrivateKey("rsa4096");
            
            var rsaKey = key.AsRSA();
            Assert.NotNull(rsaKey);
            Assert.Equal(4096, rsaKey.KeySize);
        }

        [Fact]
        public void TestKey_LoadECDsa_CanReadPublicKey()
        {
            var key = TestKeys.GetPublicKey("p384");
            
            var ecKey = key.AsECDsa();
            Assert.NotNull(ecKey);
        }

        [Fact]
        public void TestKey_LoadECDsa_ThrowsOnRSAKey()
        {
            var key = TestKeys.GetPublicKey("rsa4096");
            
            Assert.Throws<InvalidOperationException>(() => key.AsECDsa());
        }

        [Fact]
        public void TestKey_LoadRSA_ThrowsOnECKey()
        {
            var key = TestKeys.GetPublicKey("p384");
            
            Assert.Throws<InvalidOperationException>(() => key.AsRSA());
        }

        [Fact]
        public void TestKey_AsBase64_StripsHeaders()
        {
            var key = TestKeys.GetPublicKey("rsa4096");
            
            string base64 = key.AsBase64();
            Assert.DoesNotContain("-----BEGIN", base64);
            Assert.DoesNotContain("-----END", base64);
            Assert.DoesNotContain("\n", base64);
        }

        [Fact]
        public void TestKey_AsPemBase64_PreservesFormat()
        {
            var key = TestKeys.GetPublicKey("rsa4096");
            
            string pem = key.AsPemString();
            Assert.Contains("-----BEGIN PUBLIC KEY-----", pem);
            Assert.Contains("-----END PUBLIC KEY-----", pem);
        }

        [Fact]
        public void TestCertificate_Load_CanReadCertificate()
        {
            var cert = TestKeys.GetCertificate("rsa4096");
            
            var x509 = cert.AsX509Certificate2();
            Assert.NotNull(x509);
        }

        [Fact]
        public void TestCertificate_Load_CanReadAttestationCertificate()
        {
            var cert = TestKeys.GetCertificate("rsa4096", true);
            Assert.True(cert.IsAttestation);
            
            var x509 = cert.AsX509Certificate2();
            Assert.NotNull(x509);
        }

        [Fact]
        public void TestKey_Load_ThrowsOnMissingFile()
        {
            Assert.Throws<FileNotFoundException>(() => TestKeys.GetPublicKey("invalid"));
        }

        [Fact]
        public void TestCertificate_Load_ThrowsOnMissingFile()
        {
            Assert.Throws<FileNotFoundException>(() => TestKeys.GetCertificate("invalid"));
        }
    }
}
