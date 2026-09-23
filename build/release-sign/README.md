# Cross-platform release signer

`nuget-sign` owns all signing and signature verification: Authenticode for the selected assemblies, the NuGet author signature, certificate chains, timestamps, and signer pinning. This wrapper owns only what is specific to Yubico.NET.SDK releases: GitHub build provenance, the package and assembly allow-lists in `manifests/`, repacking signed assemblies into the CI-built packages, a check that nothing else changed, atomic output, and the release report.

## Prerequisites and build

- Go 1.27.1.
- GitHub CLI (`gh`), authenticated for `Yubico/Yubico.NET.SDK` attestations.
- `Yubico/nuget-sign` at commit `e6c54452834ff9fd6bf034b253d66758ca555556` (Yubico/nuget-sign#2) or later, which makes `verify --assemblies` fail on any unverified assembly and pins assembly signers with `--certificate-fingerprint`.
- A Personal Identity Verification (PIV) signing key, a leaf-first Privacy-Enhanced Mail (PEM) certificate chain, and PEM roots for the signer and timestamp authority.

```sh
git clone git@github.com:Yubico/nuget-sign.git
cd nuget-sign
git checkout e6c54452834ff9fd6bf034b253d66758ca555556
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

With a `yubikey://` key, the signer checks provenance and package identities, then asks for the Personal Identification Number (PIN) **once per `run`**, without echo. It passes the prompted PIN only to its `nuget-sign` signing subprocesses; it does not put it in command arguments, the process-wide environment, or the report. A Core run signs both managed packages and their symbol packages with that one prompt. A separately built NativeShims package is a separate run and requires another prompt. Set `NUGET_SIGN_PIN` yourself only for unattended use; never put the PIN on the command line.

The prompt needs an interactive terminal. When an agent shell has no terminal input on macOS, prefix the `release-sign run` command above with `zsh build/release-sign/launch-macos.zsh /secure/release-work/signing.status` (create the working directory first). It opens Terminal for the PIN prompt and writes the command's exit status to that file when finished. Confirm the status is `0` **and** review `signed/report.json` before publishing. On Windows or Linux, run the command in an attached interactive terminal.

Use `--clean` to replace existing output. Results are written to `WORKING_DIRECTORY/signed/`.
