# Cross-platform release signer

`nuget-sign` owns all signing and signature verification: Authenticode for the selected assemblies, the NuGet author signature, certificate chains, timestamps, and signer pinning. This wrapper owns only what is specific to Yubico.NET.SDK releases: GitHub build provenance, the package and assembly allow-lists in `manifests/`, repacking signed assemblies into the CI-built packages, a check that nothing else changed, atomic output, and the release report.

## Prerequisites and build

- Go 1.27.1.
- GitHub CLI (`gh`), authenticated for `Yubico/Yubico.NET.SDK` attestations.
- `Yubico/nuget-sign` at commit `748f3b2bad581dbf3aae94b4266b0605fc38358c` ([companion PR #4](https://github.com/Yubico/nuget-sign/pull/4)), or a later version providing the same capabilities. This includes strict assembly verification and signer pinning, private stdin credentials, and batch signing. Merge/install the companion change before deploying this client.
- A Personal Identity Verification (PIV) signing key, a leaf-first Privacy-Enhanced Mail (PEM) certificate chain, and PEM roots for the signer and timestamp authority.

```sh
git clone git@github.com:Yubico/nuget-sign.git
cd nuget-sign
git checkout 748f3b2bad581dbf3aae94b4266b0605fc38358c
go install -tags nogcpkms .
nuget-sign capabilities
cd /path/to/Yubico.NET.SDK
go -C build/release-sign build -o /tmp/release-sign .
```

`--nuget-sign` defaults to `nuget-sign` on `PATH` and can also be set with `RELEASE_SIGN_NUGET_SIGN`.
Before prompting, the wrapper requires `capabilities` schema 1 with
`privateStdin` and `batchSign` both true. An older binary produces an upgrade
error; credentials are never sent through a legacy environment fallback.

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

With a `yubikey://` key, the signer checks provenance and package identities,
then asks for the Personal Identification Number (PIN) **once per `run`**, without
echo. No extra flags, environment setup, or credential files are needed locally.
A Core run signs both managed packages and their symbol packages with that one
prompt. NativeShims is a separate run and requires another prompt.

The PIN is held in an owned byte buffer and sent only to signing children through
anonymous stdin pipes using `--pin-stdin`. The wire format is raw bytes followed
by EOF (maximum 4096 bytes); no newline is appended. Each child may request the
PIN repeatedly within its session, respecting token authentication and touch
policies. A signing failure stops the run without automatic PIN retries.

Assembly signing and repacking happen in private staging. All prepared packages
then receive their author signatures in one `nuget-sign` batch. The parent PIN
buffer's full capacity is cleared before verification and on failure/cancellation.
Every output must pass signature and content-preservation checks before atomic
publication. The original attested packages are never modified.

Signing children do not inherit `NUGET_API_KEY`, GitHub tokens, or the shell's
unrelated credentials. GitHub tokens are passed only to attestation commands.
The environment allow-list retains OS paths, proxy/trust settings, and relevant
configuration directories. File/KMS signing retains its password/credential-path
compatibility inputs; PIV signing does not receive those either.

`NUGET_SIGN_PIN` remains a compatibility input to the wrapper, converted into the
same private transport. It is never forwarded as a child environment variable.
Supplying it still exposes the original immutable environment copy in the parent;
interactive input is preferred. PINs never appear in command arguments or reports.

The CLI disables core dumps on Linux, macOS and supported BSD platforms, and
Linux dumpability before collecting secrets. Windows dump/debugger policy is
host-managed. Byte-buffer clearing reduces exposure but cannot erase all copies
inside Go or string-based signing dependencies; memory is not locked, and these
protections do not defend against a compromised OS or privileged debugger.

The prompt needs an interactive terminal. When an agent shell has no terminal input on macOS, prefix the `release-sign run` command above with `zsh build/release-sign/launch-macos.zsh /secure/release-work/signing.status` (create the working directory first). It opens Terminal for the PIN prompt and writes the command's exit status to that file when finished. Confirm the status is `0` **and** review `signed/report.json` before publishing. On Windows or Linux, run the command in an attached interactive terminal.

Use `--clean` to replace existing output. Results are written to `WORKING_DIRECTORY/signed/`.

## Verification during development

```sh
go -C build/release-sign test -race ./...

# Exercise this client's actual process runner against the companion binary.
# Requires OpenSSL; uses a disposable encrypted PFX, never a real signing token.
RELEASE_SIGN_TEST_NUGET_SIGN=/absolute/path/to/nuget-sign \
  go -C build/release-sign test -race -run TestNugetSignPrivateInputInterop -v
```
