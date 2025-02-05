using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKey.Piv;

namespace Yubico.YubiKey.TestUtilities
{
    /// <summary>
    /// This class is used to load test keys and certificates from files.
    /// The files contain PEM-encoded data such as RSA keys, EC keys, and X.509 certificates.
    /// These keys and certificates are used in unit tests and have been generated through
    /// the script `generate-test-data.sh` which is checked into this repository under Yubico/YubiKey/utilities/TestData.
    /// </summary>
    public abstract class TestCrypto
    {
        protected readonly byte[] _bytes;
        protected readonly string _pemStringFull;

        protected TestCrypto(string filePath)
        {
            var pemString = File.ReadAllText(filePath);
            _pemStringFull = pemString.Replace("\n", "").Trim();
            _bytes = GetBytesFromPem(_pemStringFull);
        }

        public byte[] AsRawBytes() => _bytes;
        public string AsPem() => _pemStringFull;
        public string AsBase64() => StripPemHeaderFooter(_pemStringFull);

        private static byte[] GetBytesFromPem(string pemData)
        {
            var base64 = StripPemHeaderFooter(pemData);
            return Convert.FromBase64String(base64);
        }

        private static string StripPemHeaderFooter(string pemData)
        {
            var base64 = pemData
                .Replace("-----BEGIN PUBLIC KEY-----", "")
                .Replace("-----END PUBLIC KEY-----", "")
                .Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("-----BEGIN EC PRIVATE KEY-----", "")
                .Replace("-----END EC PRIVATE KEY-----", "")
                .Replace("-----BEGIN CERTIFICATE-----", "")
                .Replace("-----END CERTIFICATE-----", "")
                .Replace("-----BEGIN CERTIFICATE REQUEST-----", "")
                .Replace("-----END CERTIFICATE REQUEST-----", "")
                .Replace("\n", "")
                .Trim();
            return base64;
        }
    }

    public class TestKey : TestCrypto
    {
        private readonly string _curve;
        private readonly bool _isPrivate;

        private TestKey(string filePath, string curve, bool isPrivate) : base(filePath)
        {
            _curve = curve;
            _isPrivate = isPrivate;
        }

        public RSA AsRSA()
        {
            if (!_curve.StartsWith("rsa", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Not an RSA key");
            }

            var rsa = RSA.Create();
            rsa.ImportFromPem(_pemStringFull);

            return rsa;
        }

        public ECDsa AsECDsa()
        {
            if (_curve.StartsWith("rsa", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Not an EC key");
            }

            var ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(_pemStringFull);

            return ecdsa;
        }

        public static TestKey Load(string curve, bool isPrivate)
        {
            var fileName = $"{curve}_{(isPrivate ? "private" : "public")}.pem";
            var filePath = Path.Combine("TestData", fileName);
            return new TestKey(filePath, curve, isPrivate);
        }

        internal PivPrivateKey AsPrivateKey()
        {
            return new KeyConverter(_pemStringFull).GetPivPrivateKey();
        }

        internal PivPublicKey AsPublicKey()
        {
            return new KeyConverter(_pemStringFull).GetPivPublicKey();
        }
    }

    public class TestCertificate : TestCrypto
    {
        public readonly bool IsAttestation;

        private TestCertificate(string filePath, bool isAttestation) : base(filePath)
        {
            IsAttestation = isAttestation;
        }

        public X509Certificate2 AsX509Certificate2()
        {
            return new X509Certificate2(_bytes);
        }

        public static TestCertificate Load(string curve, bool isAttestation = false)
        {
            string fileName = $"{curve}_cert{(isAttestation ? "_attest" : "")}.pem";
            string filePath = Path.Combine("TestData", fileName);
            return new TestCertificate(filePath, isAttestation);
        }
    }

    public static class TestKeys
    {
        public static TestKey GetKey(string curve, bool isPrivate) => TestKey.Load(curve, isPrivate);
        public static TestCertificate GetCertificate(string curve, bool isAttestation = false) =>
            TestCertificate.Load(curve, isAttestation);
    }
}
