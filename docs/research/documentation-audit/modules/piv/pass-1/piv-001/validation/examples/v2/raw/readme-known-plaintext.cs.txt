await using var session = await device.CreatePivSessionAsync();
using var certificate = await session.GetCertificateAsync(Slot.KeyManagement)
    ?? throw new InvalidOperationException("Provision a matching RSA certificate first.");
using var rsa = certificate.GetRSAPublicKey()
    ?? throw new InvalidOperationException("The certificate must contain an RSA public key.");
byte[] pin = Encoding.UTF8.GetBytes("123456");
byte[] expected = Encoding.UTF8.GetBytes("known plaintext");
Memory<byte> plaintext = Memory<byte>.Empty;
try
{
    byte[] ciphertext = rsa.Encrypt(expected, RSAEncryptionPadding.OaepSHA256);
    await session.VerifyPinAsync(pin);
    plaintext = await session.DecryptAsync(Slot.KeyManagement, ciphertext, RSAEncryptionPadding.OaepSHA256);
    if (!CryptographicOperations.FixedTimeEquals(expected, plaintext.Span))
        throw new CryptographicException("Decryption returned unexpected plaintext.");
}
finally
{
    CryptographicOperations.ZeroMemory(pin);
    CryptographicOperations.ZeroMemory(expected);
    CryptographicOperations.ZeroMemory(plaintext.Span);
}
