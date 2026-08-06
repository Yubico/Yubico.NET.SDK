# Yubico .NET SDK v2 — `2.0.0-alpha.2`

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
dotnet add package Yubico.YubiKit.Core --version 2.0.0-alpha.2
```

### Optional: bootstrap script

Prefer the manual command above. If you want the script, **download, review, then
run it** — do not pipe it straight into your shell:

```bash
# macOS / Linux
curl -fsSLO https://github.com/Yubico/Yubico.NET.SDK/releases/download/v2.0.0-alpha.2/install-yubikit-alpha.sh
# review install-yubikit-alpha.sh, then:
bash install-yubikit-alpha.sh
```
```powershell
# Windows
iwr https://github.com/Yubico/Yubico.NET.SDK/releases/download/v2.0.0-alpha.2/install-yubikit-alpha.ps1 -OutFile install-yubikit-alpha.ps1
# review install-yubikit-alpha.ps1, then:
./install-yubikit-alpha.ps1
```

The scripts refuse to run when piped (they require a terminal for the confirmation
prompt), so download-and-run is the supported path.

## Packages (10)

`Yubico.YubiKit.Core`, `.Management`, `.Piv`, `.Fido2`, `.WebAuthn`, `.Oath`,
`.YubiOtp`, `.OpenPgp`, `.SecurityDomain`, `.YubiHsm` — all `2.0.0-alpha.2`.

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
