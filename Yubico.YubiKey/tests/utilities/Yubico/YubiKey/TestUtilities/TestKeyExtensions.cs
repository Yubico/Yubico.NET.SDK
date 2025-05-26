using System;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Piv;
using Yubico.YubiKey.Piv.Converters;

namespace Yubico.YubiKey.TestUtilities
{
    public static class TestKeyExtensions
    {
        /// <summary>
        /// Converts the key to a PIV private key format.
        /// </summary>
        /// <returns>PivPrivateKey instance</returns>
        [Obsolete("Usage of PivEccPublic/PivEccPrivateKey PivRsaPublic/PivRsaPrivateKey is deprecated. Use implementations of ECPublicKey, ECPrivateKey and RSAPublicKey, RSAPrivateKey instead", false)]
        public static PivPrivateKey AsPivPrivateKey(
            this TestKey key)
        {
            var keyDefinition = key.KeyDefinition;
            if (keyDefinition.IsRSA)
            {
                var rsaPrivateKey = RSAPrivateKey.CreateFromPkcs8(key.EncodedKey);
                var rsaPivEncodedKey = rsaPrivateKey.EncodeAsPiv();
                return PivPrivateKey.Create(rsaPivEncodedKey, keyDefinition.KeyType.GetPivAlgorithm());
            }

            if (keyDefinition is { IsEllipticCurve: true, AlgorithmOid: Oids.ECDSA })
            {
                var ecPrivateKey = ECPrivateKey.CreateFromPkcs8(key.EncodedKey);
                var ecPivEncodedKey = ecPrivateKey.EncodeAsPiv();
                return PivPrivateKey.Create(ecPivEncodedKey, keyDefinition.KeyType.GetPivAlgorithm());
            }

            var cvPrivateKey = Curve25519PrivateKey.CreateFromPkcs8(key.EncodedKey);
            var cvPivEncodedKey = cvPrivateKey.EncodeAsPiv();
            return PivPrivateKey.Create(cvPivEncodedKey, keyDefinition.KeyType.GetPivAlgorithm());
        }

        /// <summary>
        /// Converts the key to a PIV public key format.
        /// </summary>
        /// <returns>PivPublicKey instance</returns>
        [Obsolete("Usage of PivEccPublic/PivEccPrivateKey PivRsaPublic/PivRsaPrivateKey is deprecated. Use implementations of ECPublicKey, ECPrivateKey and RSAPublicKey, RSAPrivateKey instead", false)]
        public static PivPublicKey AsPivPublicKey(
            this TestKey key)
        {
            var keyDefinition = key.KeyDefinition;
            if (keyDefinition.IsRSA)
            {
                var rsaPublicKey = RSAPublicKey.CreateFromPkcs8(key.EncodedKey);
                var rsaPivEncodedKey = rsaPublicKey.EncodeAsPiv();
                return PivPublicKey.Create(rsaPivEncodedKey, key.KeyType.GetPivAlgorithm());
            }

            if (keyDefinition is { IsEllipticCurve: true, AlgorithmOid: Oids.ECDSA })
            {
                var ecPublicKey = ECPublicKey.CreateFromPkcs8(key.EncodedKey);
                var ecPivEncodedKey = ecPublicKey.EncodeAsPiv();
                return PivPublicKey.Create(ecPivEncodedKey, key.KeyType.GetPivAlgorithm());
            }

            var cvPublicKey = Curve25519PublicKey.CreateFromPkcs8(key.EncodedKey);
            var cvPivEncodedKey = cvPublicKey.EncodeAsPiv();
            return PivPublicKey.Create(cvPivEncodedKey, keyDefinition.KeyType.GetPivAlgorithm());
        }
    }
}
