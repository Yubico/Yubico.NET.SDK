# Cross-platform release signer

`nuget-sign` signs NuGet packages and their selected assemblies, then verifies the package signature and focused Authenticode runtime policy. This wrapper enforces manifest policy, build provenance, package preservation, atomic output, and reporting.

## Prerequisites and build

- Go 1.27.1.
- GitHub CLI (`gh`), authenticated for `Yubico/Yubico.NET.SDK` attestations.
- The Yubico `nuget-sign` fork at commit `25ef12b`.
- A Personal Identity Verification (PIV) signing key, a leaf-first Privacy-Enhanced Mail (PEM) certificate chain, and PEM roots for the signer and timestamp authority.

```sh
git clone git@github.com:Yubico/nuget-sign-verification-fix.git
cd nuget-sign-verification-fix
git switch fix/verify-authenticode-cryptography
git checkout 25ef12b
go install -tags nogcpkms .
cd /path/to/Yubico.NET.SDK
go -C build/release-sign build -o /tmp/release-sign .
```

`--nuget-sign` defaults to `nuget-sign` on `PATH` and can also be set with `RELEASE_SIGN_NUGET_SIGN`.

## Run

```sh
/tmp/release-sign run --component core \
  --working-directory /secure/release-work \
  --artifact core-build.zip --artifact yubikey-build.zip \
  --manifest ./build/release-sign/manifests/core.json \
  --source-digest 0123456789abcdef0123456789abcdef01234567 \
  --key 'yubikey://9c?serial=12345678' \
  --certificate /secure/certs/code-signing-chain.pem \
  --root /secure/certs/code-signing-root.pem \
  --timestamp-root /secure/certs/timestamp-root.pem
```

Set `NUGET_SIGN_PIN` for unattended use; never put the Personal Identification Number (PIN) on the command line. Use `--clean` to replace existing output. Results are written to `WORKING_DIRECTORY/signed/`.
