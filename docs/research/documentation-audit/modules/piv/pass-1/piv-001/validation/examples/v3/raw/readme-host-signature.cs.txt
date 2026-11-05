await using var session = await device.CreatePivSessionAsync();

byte[] pin = Encoding.UTF8.GetBytes("123456");
byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes("message to sign"));
Memory<byte> signature = Memory<byte>.Empty;
try
{
    await session.VerifyPinAsync(pin);
    var metadata = await session.GetSlotMetadataAsync(Slot.Authentication)
        ?? throw new InvalidOperationException("Generate a P-256 key first.");
    using var verifier = metadata.GetECDsaPublicKey();
    signature = await session.SignOrDecryptAsync(Slot.Authentication, KeyType.P256, digest);
    if (!verifier.VerifyHash(digest, signature.Span, DSASignatureFormat.Rfc3279DerSequence))
        throw new CryptographicException("The slot signature failed host verification.");
}
finally
{
    CryptographicOperations.ZeroMemory(pin);
    CryptographicOperations.ZeroMemory(digest);
    CryptographicOperations.ZeroMemory(signature.Span);
}
