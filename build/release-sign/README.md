# Cross-platform release signer

`release-sign` verifies build provenance, Authenticode-signs the assemblies explicitly selected by a manifest, author-signs NuGet packages, verifies package and assembly signatures internally, and verifies each selected assembly independently with `osslsigncode`. Publishing is a separate operation.

## Build and prerequisites

Prerequisites:

- Go 1.27.1.
- GitHub CLI (`gh`), authenticated for `Yubico/Yubico.NET.SDK` attestations.
- `osslsigncode` 2.13 or newer, available through Homebrew (`brew install osslsigncode`), apt (`apt install osslsigncode`), or Chocolatey (`choco install osslsigncode`). This integration was developed with 2.14; older versions are rejected because of verify-path security defects and missing required flags.
- A PIV signing key and its leaf-first PEM certificate chain.
- PEM trust roots for the signer and timestamp authority. Every trust-anchor certificate must be a certificate authority with valid basic constraints, be self-issued with byte-identical encoded subject and issuer names, and have a valid self-signature. The signer chain contains only the leaf followed by any intermediates.
- Linux builds need PC/SC development headers, such as `libpcsclite-dev`.

Build outside the repository:

```sh
export GOPRIVATE=github.com/Yubico/nuget-sign
gh auth setup-git
go -C build/release-sign build -o /tmp/release-sign .
```

The signer does not use `signtool`, `nuget.exe`, or `dotnet nuget` for signing. `osslsigncode` is a GPL-licensed tool executed as a separate process; it is not linked into the signer or included as a Go dependency.

This pull request lands the signing tool first and does not wire the Release skill. The existing release signer remains in use until a follow-up changes the release workflow.

## Run

```sh
/tmp/release-sign run \
  --component core \
  --working-directory /secure/release-work \
  --artifact core-build.zip \
  --artifact yubikey-build.zip \
  --manifest ./build/release-sign/manifests/core.json \
  --source-digest 0123456789abcdef0123456789abcdef01234567 \
  --key 'yubikey://9c?serial=12345678' \
  --certificate /secure/certs/code-signing-chain.pem \
  --root /secure/certs/code-signing-root.pem \
  --timestamp-root /secure/certs/digicert-timestamp-root.pem
```

For NativeShims, use `--component nativeshims`, its build artifact, and `manifests/nativeshims.json`. Manifests remain explicit so the package and assembly scope can be audited independently of the executable location.

`--osslsigncode` defaults to `osslsigncode` on `PATH`. It can also be set with `RELEASE_SIGN_OSSLSIGNCODE`. The source digest, key, certificate, signer root, and timestamp root can come from `RELEASE_SIGN_SOURCE_DIGEST`, `RELEASE_SIGN_KEY`, `RELEASE_SIGN_CERTIFICATE`, `RELEASE_SIGN_ROOT`, and `RELEASE_SIGN_TIMESTAMP_ROOT`. `--timestamper` defaults to `http://timestamp.digicert.com`. For unattended PIV use, set `NUGET_SIGN_PIN`; never place a PIN in the key URI or command line.

Use `--clean` to replace an existing `signed` directory. Existing output remains untouched until all replacement packages pass verification.

## Verification and output

Before key acquisition, the signer validates inputs, resolves the exact `gh` and `osslsigncode` executable paths, requires `osslsigncode` 2.13 or newer, extracts packages, verifies GitHub attestations, and plans the complete package set.

After signing, the signer verifies NuGet package signatures and selected assembly signatures internally. It then extracts the exact bytes of each selected assembly from the signed package into a temporary file and runs:

```sh
osslsigncode verify -in TEMP -CAfile ROOT -TSA-CAfile TIMESTAMP_ROOT -require-leaf-hash sha256:FINGERPRINT -ignore-cdp -ignore-crl
```

Each selected assembly gets exactly one call in sorted entry-name order. Temporary assembly files stay beside the staged package and are removed after verification. A zero exit status is insufficient: output must confirm a SHA-256 message digest, the required leaf hash, the timestamp-server signature, the assembly signature, and exactly one verified signature. Symbol packages are not passed to `osslsigncode`.

For an optional live check of an extracted signed assembly, run the same command with real file and certificate paths:

```sh
osslsigncode verify -in ./Yubico.Core.dll -CAfile ./code-signing-root.pem -TSA-CAfile ./timestamp-root.pem -require-leaf-hash sha256:HEX_FINGERPRINT -ignore-cdp -ignore-crl
```

The working directory owns `scratch/unsigned/`, transient scratch directories, `signed/packages/`, and `signed/report.json`. Packages and the report are published there only after every package passes provenance, internal, and `osslsigncode` verification. Failed runs leave no partial package in `signed/packages`.

This module has no GitHub Actions test workflow because `github.com/Yubico/nuget-sign` is private and the repository workflow token is not known to have read access. Run its checks locally until a suitable read-only credential is provisioned.

Cloud KMS support remains deferred because its provider dependency currently pulls a gRPC version covered by `GHSA-2v4p-qf9q-27wj`. Reconsider it after the upstream dependency is patched and dependency review passes.

PIV means Personal Identity Verification. PEM means Privacy-Enhanced Mail certificate encoding. PC/SC means Personal Computer/Smart Card. PIN means Personal Identification Number. GPL means GNU General Public License. SHA-256 means Secure Hash Algorithm 256-bit. URI means Uniform Resource Identifier. CLI means command-line interface. CI means continuous integration. KMS means Key Management Service. gRPC means Google Remote Procedure Call. apt means Advanced Package Tool.
