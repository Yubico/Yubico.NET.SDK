# Cross-platform release signer

`release-sign` verifies build provenance, Authenticode-signs selected Windows assemblies, author-signs NuGet packages, verifies the results twice, and emits the layout consumed by the Release skill. Publishing is deliberately separate and is not performed by this tool.

## Prerequisites

- Go 1.27.1.
- GitHub CLI (`gh`) authenticated for `Yubico/Yubico.NET.SDK` attestations. Run `gh auth setup-git` so Go can authenticate to the private signing dependency without changing repository configuration.
- A PIV signing key and its leaf-first PEM certificate chain, or a Google Cloud KMS key location.
- Signer and timestamp-authority PEM trust roots. Every certificate in either trust-anchor file must be a certificate authority with valid basic constraints, be self-issued with byte-identical encoded subject and issuer names, and have a valid self-signature. The signer certificate-chain file remains leaf-first and contains only the leaf and any intermediates.
- An independent package verifier implementing the command shown below.
- Linux builds need PC/SC development headers (for example, `libpcsclite-dev`).

From the repository root, build without writing a binary into the repository:

```sh
export GOPRIVATE=github.com/Yubico/nuget-sign
gh auth setup-git
go -C build/release-sign build -o /tmp/release-sign .
go -C build/release-sign/relic-verify build -o /tmp/release-sign-relic-verify ./cmd/release-sign-relic-verify
```

The signer uses neither `signtool`, `nuget.exe`, nor `dotnet nuget` for signing.

## Core release

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
  --timestamp-root /secure/certs/digicert-timestamp-root.pem \
  --independent-verifier /secure/bin/relic-verify
```

## NativeShims release

```sh
/tmp/release-sign run \
  --component nativeshims \
  --working-directory /secure/release-work \
  --artifact nativeshims-build.zip \
  --manifest ./build/release-sign/manifests/nativeshims.json \
  --source-digest 0123456789abcdef0123456789abcdef01234567 \
  --key 'yubikey://9c?serial=12345678' \
  --certificate /secure/certs/code-signing-chain.pem \
  --root /secure/certs/code-signing-root.pem \
  --timestamp-root /secure/certs/digicert-timestamp-root.pem \
  --independent-verifier /secure/bin/relic-verify
```

`--timestamper` defaults to `http://timestamp.digicert.com`. Use `--clean` whenever intentionally replacing an existing `signed` directory. Existing output is preserved until every replacement package passes verification.

The flags `--source-digest`, `--key`, `--certificate`, `--root`, `--timestamp-root`, and `--independent-verifier` may instead come from `RELEASE_SIGN_SOURCE_DIGEST`, `RELEASE_SIGN_KEY`, `RELEASE_SIGN_CERTIFICATE`, `RELEASE_SIGN_ROOT`, `RELEASE_SIGN_TIMESTAMP_ROOT`, and `RELEASE_SIGN_INDEPENDENT_VERIFIER`. The source digest is the exact 40-character hexadecimal Git commit attested by the build workflow. For unattended PIV use, set `NUGET_SIGN_PIN`; never place a PIN in the key URI or command line.

## Output and safety

The working directory owns `scratch/unsigned/`, transient scratch directories, `signed/packages/`, and `signed/report.json`. Packages and the report appear only after every package passes internal and independent verification. Failed runs leave no partial package in `signed/packages`.

Stages 1–4 validate inputs, extract packages, verify GitHub attestations, and plan the complete package set before opening a signing-key session. Keep the YubiKey unplugged while preparing and validating release inputs; for an interactive run, connect it only when the process reaches key access. Unattended runs must have the device connected but still do not open it before stages 1–4 complete.

The external verifier is mandatory and is called once for each signed `.nupkg`:

```text
VERIFIER --package OUTPUT --root ROOT --timestamp-root TIMESTAMP_ROOT --signer-fingerprint SHA256HEX --assembly ENTRY [--assembly ENTRY...]
```

It is not called for `.snupkg` files. This independent check is intentionally required because verification by the signing implementation alone cannot detect every implementation-specific defect.
The bundled implementation is built from `relic-verify/cmd/release-sign-relic-verify`.
The parent requires one `PASS` record per selected assembly, including SHA-256
and the expected signer fingerprint; a zero exit status without that evidence
is rejected.

Publishing remains a separate release operation after review of `signed/report.json`. Retain the existing fallback signing scripts for one release, but do not mix their output into a `release-sign` run.

This module does not have a GitHub Actions test workflow because `github.com/Yubico/nuget-sign` is private and the repository workflow token is not known to have read access. Adding a workflow without an explicit credential would create a predictably broken required check. Run the verification commands locally or add continuous integration only after a suitable read-only credential is provisioned.

PEM means Privacy-Enhanced Mail certificate encoding. PIV means Personal Identity Verification. PIN means Personal Identification Number. PC/SC means Personal Computer/Smart Card. KMS means Key Management Service. CLI means command-line interface. DLL means Dynamic-Link Library. RFC 3161 is the Internet standard for trusted timestamps.
