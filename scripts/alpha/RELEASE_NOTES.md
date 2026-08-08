# Yubico .NET SDK v2 — Alpha

> ## ⚠️ ALPHA — NOT FOR PRODUCTION
>
> This is a **pre-release alpha** of the Yubico .NET SDK v2. It is **subject to
> change** and has **not yet completed Yubico's formal security audit**.
>
> - **No security guarantees** are made until that audit is complete.
> - Packages are **unsigned**.
> - **Package names and namespaces may change** before the stable release.
> - Provided for **evaluation and hackathon use only**.

## Install (anonymous public feed — no authentication)

Add the alpha feed (keep nuget.org enabled so transitive dependencies such as
`Yubico.NativeShims` resolve):

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.Core --prerelease
```

The feed publishes a new alpha version on every push to `yubikit`, so
`--prerelease` always resolves the latest build. See the
[feed website](https://yubico.github.io/Yubico.NET.SDK/) for the current
package list and versions.

## Packages (10)

`Yubico.YubiKit.Core`, `.Management`, `.Piv`, `.Fido2`, `.WebAuthn`, `.Oath`,
`.YubiOtp`, `.OpenPgp`, `.SecurityDomain`, `.YubiHsm`.

## Updates

The feed is static and public; new alpha versions published to it are picked up
automatically by `dotnet restore`. No need to re-run the installer.

## Troubleshooting

- **Unsigned packages:** consumable under default NuGet configuration. Environments
  that enforce package signature validation (`signatureValidationMode=require`) will
  reject these alpha packages.
- **Package source mapping:** if you use source mapping, map `Yubico.NativeShims`
  (and any other transitive `Yubico.*` deps you don't get from this feed) to
  `nuget.org`, and the `Yubico.YubiKit.*` packages to this alpha feed.
- **Build provenance:** each package is attested via GitHub build provenance. Verify
  with `gh attestation verify <pkg>.nupkg --repo Yubico/Yubico.NET.SDK`.

## Teardown

```bash
dotnet nuget remove source yubikit-alpha
```
