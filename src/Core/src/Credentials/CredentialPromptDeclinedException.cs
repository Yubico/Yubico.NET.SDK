namespace Yubico.YubiKit.Core.Credentials;

/// <summary>Indicates the credential provider declined an input request (not a device rejection).</summary>
public sealed class CredentialPromptDeclinedException : Exception
{
    /// <summary>Gets the kind of credential that was declined.</summary>
    public CredentialKind Kind { get; }

    /// <summary>Creates a decline exception for the requested kind.</summary>
    public CredentialPromptDeclinedException(CredentialKind kind)
        : base($"Credential request for {kind} was declined.") => Kind = kind;
}
