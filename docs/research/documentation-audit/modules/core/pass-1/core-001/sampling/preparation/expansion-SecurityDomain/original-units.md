# Additional SD narrative whole units

**No content verdicts.** Pinned source `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; read complete units and relevant enclosing context.

## `core-001:unit:5f636711739239fe`
`src/SecurityDomain/README.md:1–11`; SecurityDomain narrative-examples-and-maps; **UNREAD / zero credit**.

````markdown
# Yubico.YubiKit.SecurityDomain

The Security Domain is the GlobalPlatform root security application on a YubiKey. It owns the Secure Channel
Protocol (SCP) keys that every other applet session can use to encrypt and authenticate its APDU traffic. This
package inspects, generates, imports, rotates, and deletes those keys, manages SCP11 certificates and
allowlists, and attempts a destructive Security Domain reset. Other modules consume the keys through
`SessionCreationOptions.ScpKeyParameters`.

> The v2 SDK is a pre-release alpha; see the [repository README](../../README.md) for the current status and
> constraints.


````

## `core-001:unit:0d64e57897a15be3`
`src/SecurityDomain/README.md:76–89`; SecurityDomain narrative-examples-and-maps; **UNREAD / zero credit**.

````markdown
### Authenticate with the default SCP03 keys

```csharp
using var scpKeyParameters = Scp03KeyParameters.Default;
await using var session = await device.CreateSecurityDomainSessionAsync(
    new SessionCreationOptions { ScpKeyParameters = scpKeyParameters });

IReadOnlyList<Yubico.YubiKit.SecurityDomain.KeyInfo> keys = await session.ListKeyInformationAsync();
```

`Scp03KeyParameters.Default` wraps the publicly documented factory key set. Use it only on a device you are
authorized to administer and whose factory set is still installed. The next two snippets continue on this
administrative `session`; each is a separate task, not a single combined program.


````

