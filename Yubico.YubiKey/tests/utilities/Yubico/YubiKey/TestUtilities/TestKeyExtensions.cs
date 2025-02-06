using Yubico.YubiKey.Piv;

namespace Yubico.YubiKey.TestUtilities
{
    public static class TestKeyExtensions
    {
        /// <summary>
        /// Converts the key to a PIV private key format.
        /// </summary>
        /// <returns>PivPrivateKey instance</returns>
        public static PivPrivateKey AsPrivateKey(this TestKey key)
        {
            return new KeyConverter(key.AsPemString()).GetPivPrivateKey();
        }

        /// <summary>
        /// Converts the key to a PIV public key format.
        /// </summary>
        /// <returns>PivPublicKey instance</returns>
        public static PivPublicKey AsPublicKey(this TestKey key)

        {
            return new KeyConverter(key.AsPemString()).GetPivPublicKey();
        }
    }
}

