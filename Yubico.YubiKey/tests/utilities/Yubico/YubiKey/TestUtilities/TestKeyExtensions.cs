using System;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Piv;

namespace Yubico.YubiKey.TestUtilities
{
    public static class TestKeyExtensions
    {
        /// <summary>
        /// Converts the key to a PIV private key format.
        /// </summary>
        /// <returns>PivPrivateKey instance</returns>
        public static PivPrivateKey AsPivPrivateKey(
            this TestKey key)
        {
            var keyDefinition = key.KeyDefinition;
            if (keyDefinition.IsRsaKey)
            {
                var rsaPrivateKey = RSAPrivateKeyParameters.CreateFromPkcs8(key.EncodedKey);
                var rsaPivEncodedKey = rsaPrivateKey.ToPivEncodedPrivateKey();
                return PivPrivateKey.Create(rsaPivEncodedKey, keyDefinition.KeyType.GetPivAlgorithm());
            }

            if (keyDefinition is { IsEcKey: true, AlgorithmOid: KeyDefinitions.Oids.ECDSA })
            {
                var ecPrivateKey = ECPrivateKeyParameters.CreateFromPkcs8(key.EncodedKey);
                var ecPivEncodedKey = ecPrivateKey.ToPivEncodedPrivateKey();
                return PivPrivateKey.Create(ecPivEncodedKey, keyDefinition.KeyType.GetPivAlgorithm());
            }

            // Curve25519
            var cvPrivateKey = Curve25519PrivateKeyParameters.CreateFromPkcs8(key.EncodedKey);
            var cvPivEncodedKey = cvPrivateKey.ToPivEncodedPrivateKey();
            return PivPrivateKey.Create(cvPivEncodedKey, keyDefinition.KeyType.GetPivAlgorithm());
        }

        /// <summary>
        /// Converts the key to a PIV public key format.
        /// </summary>
        /// <returns>PivPublicKey instance</returns>
        public static PivPublicKey AsPivPublicKey(
            this TestKey key)
        {
            var keyDefinition = key.KeyDefinition;
            if (keyDefinition.IsRsaKey)
            {
                var rsaPublicKey = RSAPublicKeyParameters.CreateFromPkcs8(key.EncodedKey);
                var rsaPivEncodedKey = rsaPublicKey.ToPivEncodedPublicKey();
                return PivPublicKey.Create(rsaPivEncodedKey, key.KeyType.GetPivAlgorithm());
            }

            if (keyDefinition is { IsEcKey: true, AlgorithmOid: KeyDefinitions.Oids.ECDSA })
            {
                var ecPublicKey = ECPublicKeyParameters.CreateFromPkcs8(key.EncodedKey);
                var ecPivEncodedKey = ecPublicKey.ToPivEncodedPublicKey();
                return PivPublicKey.Create(ecPivEncodedKey, key.KeyType.GetPivAlgorithm());
            }

            // Curve25519
            var cvPublicKey = Curve25519PublicKeyParameters.CreateFromPkcs8(key.EncodedKey);
            var cvPivEncodedKey = cvPublicKey.ToPivEncodedPublicKey();
            return PivPublicKey.Create(cvPivEncodedKey, keyDefinition.KeyType.GetPivAlgorithm());
        }
    }
}
