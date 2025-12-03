namespace Yubico.YubiKey.Fido2;

/// <summary>
/// Contains constant strings for FIDO2 extension identifiers.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// The credential protection extension identifier.
    /// </summary>
    public const string CredProtect = "credProtect";

    /// <summary>
    /// The credential blob extension identifier.
    /// </summary>
    public const string CredBlob = "credBlob";

    /// <summary>
    /// The large blob key extension identifier.
    /// </summary>
    public const string LargeBlobKey = "largeBlobKey";

    /// <summary>
    /// The minimum PIN length extension identifier.
    /// </summary>
    public const string MinPinLength = "minPinLength";

    /// <summary>
    /// The HMAC secret extension identifier.
    /// </summary>
    public const string HmacSecret = "hmac-secret";

    /// <summary>
    /// The HMAC secret multi-credential extension identifier.
    /// </summary>
    public const string HmacSecretMc = "hmac-secret-mc";

    /// <summary>
    /// The third party payment extension identifier.
    /// </summary>
    public const string ThirdPartyPayment = "thirdPartyPayment";
}
