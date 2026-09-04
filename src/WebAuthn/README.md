# Yubico.YubiKit.WebAuthn

This package provides a high-level WebAuthn client backed by the FIDO2 package.

Create a client from a YubiKey with the required WebAuthn origin and public-suffix checker. Cross-cutting
session settings use `SessionCreationOptions` and are forwarded directly to the underlying FIDO2 session:

```csharp
await using var client = await yubiKey.CreateWebAuthnClientAsync(
    origin,
    isPublicSuffix: domain => publicSuffixList.Contains(domain),
    options: new SessionCreationOptions
    {
        PreferredConnectionType = ConnectionType.SmartCard
    },
    cancellationToken: cancellationToken);
```

The client owns and asynchronously disposes the FIDO2 session it creates. The public-suffix checker should be
backed by Public Suffix List data.
