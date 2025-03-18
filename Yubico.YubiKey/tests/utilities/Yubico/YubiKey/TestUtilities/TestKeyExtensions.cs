using System;
using Yubico.YubiKey.Piv;

namespace Yubico.YubiKey.TestUtilities
{
    public static class TestKeyExtensions
    {
        /// <summary>
        /// Converts the key to a PIV private key format.
        /// </summary>
        /// <returns>PivPrivateKey instance</returns>
        public static PivPrivateKey AsPivPrivateKey(this TestKey key)
        {
            var parser = new PrivateKeyInfoParser();
            switch (key._curve)
            {
                case "p256":
                    {
                        var keyInfo = parser.ParsePrivateKey<EcPrivateKeyInfo>(key.EncodedKey);
                        return new PivEccPrivateKey(keyInfo.PrivateKey, PivAlgorithm.EccP256);
                    }
                case "p384":
                    {
                        var keyInfo = parser.ParsePrivateKey<EcPrivateKeyInfo>(key.EncodedKey);
                        return new PivEccPrivateKey(keyInfo.PrivateKey, PivAlgorithm.EccP384);
                    }
                case "ed25519":
                    {
                        var keyInfo = parser.ParsePrivateKey<EdPrivateKeyInfo>(key.EncodedKey);
                        return new PivEccPrivateKey(keyInfo.PrivateKey, PivAlgorithm.EccEd25519);
                    }
                case "x25519":
                    {
                        var keyInfo = parser.ParsePrivateKey<EdPrivateKeyInfo>(key.EncodedKey);
                        return new PivEccPrivateKey(keyInfo.PrivateKey, PivAlgorithm.EccX25519);
                    }
                case "rsa1024":
                case "rsa2048":
                case "rsa3072":
                case "rsa4096":
                    {
                        var keyInfo = parser.ParsePrivateKey<RsaPrivateKeyInfo>(key.EncodedKey);
                        return new PivRsaPrivateKey(keyInfo.Prime1, keyInfo.Prime2, keyInfo.Exponent1,
                            keyInfo.Exponent2, keyInfo.Coefficient);
                    }
                default: throw new ArgumentException("Unknown curve");
            }
        }

        /// <summary>
        /// Converts the key to a PIV public key format.
        /// </summary>
        /// <returns>PivPublicKey instance</returns>
        public static PivPublicKey AsPivPublicKey(this TestKey key)
        {
            var keyConverter = new KeyConverter(key.AsPemString()); 
            var pivKey = keyConverter.GetPivPublicKey(); 
            return pivKey;
        }
    }
}

