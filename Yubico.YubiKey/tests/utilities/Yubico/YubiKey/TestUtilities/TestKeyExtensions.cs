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
            var keyConverter = new KeyConverter(key.AsPemString()); 
            var pivKey = keyConverter.GetPivPrivateKey();
            return pivKey;
        }

        /// <summary>
        /// Converts the key to a PIV public key format.
        /// </summary>
        /// <returns>PivPublicKey instance</returns>
        public static PivPublicKey AsPivPublicKey(this TestKey key)
        {
            var keyConverter = new KeyConverter(key.AsPemString()); // works
            var pivKey = keyConverter.GetPivPublicKey(); // doesnt work
            return pivKey;
        }
    }
}

