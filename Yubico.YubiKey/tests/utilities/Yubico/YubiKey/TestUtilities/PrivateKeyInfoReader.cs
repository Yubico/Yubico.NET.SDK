using System;
using System.Formats.Asn1;
using System.Security.Cryptography;

namespace Yubico.YubiKey.TestUtilities
{
    /// <summary>
    /// Common interface for all private key parsers
    /// </summary>
    public interface IPrivateKeyParser<out T> where T : IPrivateKeyInfo
    {
        /// <summary>
        /// Returns true if the parser can handle this key format
        /// </summary>
        bool CanParse(string algorithmOid);

        /// <summary>
        /// Parses the private key data and returns the raw key bytes
        /// </summary>
        byte[] ParseKeyData(byte[] keyData);

        T CreatePrivateKeyInfo(
            string algorithmOid,
            byte[] keyData,
            string? curveOid,
            byte[] privateKeyInfoAllBytes);
    }

    public class Ed25519KeyParser : IPrivateKeyParser<EdPrivateKeyInfo>
    {
        private const string Ed25519Oid = "1.3.101.112";

        public bool CanParse(
            string algorithmOid) =>
            algorithmOid == Ed25519Oid;

        public byte[] ParseKeyData(
            byte[] keyData)
        {
            var keyReader = new AsnReader(keyData, AsnEncodingRules.DER);
            return keyReader.ReadOctetString();
        }

        public EdPrivateKeyInfo CreatePrivateKeyInfo(
            string algorithmOid,
            byte[] keyData,
            string? curveOid,
            byte[] privateKeyInfoAllBytes)
        {
            var privateKey = ParseKeyData(keyData);
            return new EdPrivateKeyInfo { AlgorithmOid = algorithmOid, PrivateKey = privateKey };
        }
    }

    public class EcKeyParser : IPrivateKeyParser<EcPrivateKeyInfo>
    {
        private const string EcKeyAlgorithmOid = "1.2.840.10045.2.1";

        public bool CanParse(
            string algorithmOid)
        {
            return algorithmOid == EcKeyAlgorithmOid;
        }

        public byte[] ParseKeyData(
            byte[] keyData)
        {
            var keyReader = new AsnReader(keyData, AsnEncodingRules.DER);
            var ecPrivateKeySequence = keyReader.ReadSequence();
            _ = ecPrivateKeySequence.ReadInteger();

            return ecPrivateKeySequence.ReadOctetString();
        }

        public EcPrivateKeyInfo CreatePrivateKeyInfo(
            string algorithmOid,
            byte[] keyData,
            string? curveOid,
            byte[] privateKeyInfoAllBytes)
        {
            var privateKey = ParseKeyData(keyData);
            return new EcPrivateKeyInfo { AlgorithmOid = algorithmOid, CurveOid = curveOid, PrivateKey = privateKey };
        }
    }

    public class RsaKeyParser : IPrivateKeyParser<RsaPrivateKeyInfo>
    {
        private const string RsaOid = "1.2.840.113549.1.1.1";

        public bool CanParse(
            string algorithmOid) =>
            algorithmOid == RsaOid;

        public byte[] ParseKeyData(
            byte[] keyData)
        {
            throw new NotImplementedException();
        }

        public RsaPrivateKeyInfo CreatePrivateKeyInfo(
            string algorithmOid,
            byte[] keyData,
            string? curveOid,
            byte[] privateKeyInfoAllBytes)
        {
            var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(privateKeyInfoAllBytes, out _);
            var rsaParams = rsa.ExportParameters(true);

            var modulus = rsaParams.Modulus;
            var publicExponent = rsaParams.Exponent;
            var privateExponent = rsaParams.D;
            var prime1 = rsaParams.P;
            var prime2 = rsaParams.Q;
            var exponent1 = rsaParams.DP;
            var exponent2 = rsaParams.DQ;
            var coefficient = rsaParams.InverseQ;

            return new RsaPrivateKeyInfo
            {
                AlgorithmOid = algorithmOid,
                CurveOid = curveOid,
                PrivateKey = keyData,
                Modulus = modulus!,
                PublicExponent = publicExponent!,
                PrivateExponent = privateExponent!,
                Prime1 = prime1!,
                Prime2 = prime2!,
                Exponent1 = exponent1!,
                Exponent2 = exponent2!,
                Coefficient = coefficient!
            };
        }
    }

    public class PrivateKeyInfoParser
    {
        private readonly IPrivateKeyParser<IPrivateKeyInfo>[] _parsers;

        /// <summary>
        /// Create a parser with the default set of key parsers
        /// </summary>
        public PrivateKeyInfoParser() : this([new Ed25519KeyParser(), new EcKeyParser(), new RsaKeyParser()])
        {
        }

        /// <summary>
        /// Create a parser with a custom set of key parsers
        /// </summary>
        public PrivateKeyInfoParser(
            IPrivateKeyParser<IPrivateKeyInfo>[] parsers)
        {
            _parsers = parsers ?? throw new ArgumentNullException(nameof(parsers));
        }

        /// <summary>
        /// Parse a PKCS#8 private key and extract the raw key bytes
        /// </summary>
        public T ParsePrivateKey<T>(
            byte[] privateKeyInfoBytes) where T : IPrivateKeyInfo
        {
            (byte[] keyData, string algorithmOid, string? curveOid) = GetKeyComponents(privateKeyInfoBytes);

            foreach (var parser in _parsers)
            {
                if (!parser.CanParse(algorithmOid))
                {
                    continue;
                }

                return (T)parser.CreatePrivateKeyInfo(algorithmOid, keyData, curveOid, privateKeyInfoBytes);
            }

            throw new CryptographicException($"No parser available for algorithm OID {algorithmOid}");
        }

        private static (byte[] keyData, string algorithmOid, string? curveOid) GetKeyComponents(
            byte[] privateKeyInfoBytes)
        {
            var reader = new AsnReader(privateKeyInfoBytes, AsnEncodingRules.DER);

            var privateKeyInfoSequence = reader.ReadSequence();
            var version = privateKeyInfoSequence.ReadInteger();
            if (version != 0)
            {
                throw new CryptographicException("Unsupported PrivateKeyInfo version");
            }

            var algorithmIdentifierSequence = privateKeyInfoSequence.ReadSequence();
            string algorithmOid = algorithmIdentifierSequence.ReadObjectIdentifier();
            string? curveOid = null;
            if (algorithmIdentifierSequence.HasData)
            {
                if (algorithmIdentifierSequence.PeekTag().TagValue == (int)UniversalTagNumber.ObjectIdentifier)
                {
                    curveOid = algorithmIdentifierSequence.ReadObjectIdentifier();
                }
                else
                {
                    try { algorithmIdentifierSequence.ReadNull(); }
                    catch { algorithmIdentifierSequence.ReadEncodedValue(); }
                }
            }

            byte[] keyData = privateKeyInfoSequence.ReadOctetString();

            return (keyData, algorithmOid, curveOid);
        }
    }
}
