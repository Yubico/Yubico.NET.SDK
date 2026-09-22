# release-sign-relic-verify

A second opinion on the Authenticode signatures inside a signed NuGet package.

The release pipeline signs the assemblies in a package with
[`github.com/Yubico/nuget-sign`](https://github.com/Yubico/nuget-sign) and then
verifies the result with that same library. That is a useful check, but it is
not an independent one: a bug in the signer that the verifier shares will
confirm itself. This program re-verifies exactly the same assemblies with an
unrelated implementation,
[`github.com/sassoftware/relic`](https://github.com/sassoftware/relic), so that
the release is signed off by two codebases that do not share a mistake.

It is a temporary belt-and-braces measure. See "When to delete this" below.

## Usage

```
release-sign-relic-verify \
  --package OUTPUT.nupkg \
  --root ROOT.pem \
  --timestamp-root TIMESTAMP_ROOT.pem \
  --signer-fingerprint 9F2C...64hex \
  --assembly lib/netstandard2.0/Yubico.Core.dll \
  --assembly lib/netstandard2.0/Yubico.YubiKey.dll
```

All five flags are required and `--assembly` must be given at least once.
Each `--assembly` is the exact ZIP entry path of one assembly to check.

`--signer-fingerprint` is the SHA-256 of the expected signing certificate's DER
encoding. Case does not matter, and `:`, `-` and whitespace separators are
ignored, so the forms `openssl x509 -fingerprint -sha256` and Windows certmgr
produce can be pasted in unchanged. What is left after removing separators must
be exactly 64 hexadecimal digits — a SHA-1 fingerprint pasted by mistake is
rejected rather than silently never matched.

`--root` and `--timestamp-root` must contain self-signed CA certificates only.

Build it with:

```
go build ./cmd/release-sign-relic-verify
```

On success it prints one line per assembly and exits zero:

```
PASS lib/netstandard2.0/Yubico.Core.dll image=SHA-256 timestamp=2026-09-22T13:03:48Z signer=<sha256 of signer cert, matching --signer-fingerprint> tsa=<sha256 of TSA cert> subject=CN=...
```

Every assembly is checked even after one fails. Failures are written to
standard error as `FAIL <entry>: <reason>` and the exit status is non-zero.

## What it checks

For each requested entry, all of the following must hold:

- The entry name is a safe, exact, forward-slash ZIP path, requested only once,
  and matched by exactly one entry in the package. No other entry in the
  package may have an unsafe name or collide with another entry's name when
  compared case-insensitively.
- `authenticode.VerifyPE` accepts the image: the Authenticode digest recomputed
  over the image matches the digest in the `SpcIndirectDataContent`, and the
  CMS signature over the signed attributes verifies.
- There is exactly one signature.
- The image digest algorithm is SHA-256.
- There is an RFC 3161 timestamp; its CMS signature verifies, its message
  imprint really digests this signature's encrypted digest, and the time it
  attests to is not the zero time.
- The timestamp's message imprint algorithm is SHA-256. relic exposes it as
  `pkcs9.CounterSignature.Hash`, taken from the token's `TSTInfo`. An imprint
  relic does not recognise arrives as the zero `crypto.Hash` and fails the same
  comparison.
- The timestamp authority's certificate chains to a root in
  `--timestamp-root`, with the timestamping extended key usage, as of the time
  the token attests to.
- The signer's certificate chains to a root in `--root`, with the code-signing
  extended key usage, as of the time the timestamp attests to.
- The signer's certificate is the one named by `--signer-fingerprint`. This is
  checked last, after everything above has passed: chaining to the root only
  says the root issued the certificate, and every other certificate that root
  issues would chain just as well.

Every certificate in `--root` and `--timestamp-root` must be a self-signed CA:
`BasicConstraints` present with `CA:TRUE`, subject equal to issuer, and a
signature that verifies under its own key. A trust anchor file is a list of
things this program believes without further evidence, so a leaf in there would
let a signer vouch for itself and an intermediate would quietly widen trust to
whatever issued it. **If a release ever needs to pin an intermediate rather
than a root, this check is what will fail**, and the fix is a decision about
the trust model, not a tweak here.

`--root` and `--timestamp-root` are deliberately separate pools. Passing one
file for both would let a timestamp authority issue code-signing certificates
that this program would accept.

## What it does not check

- **The NuGet package signature.** The caller checks the container; this
  program only opens the archive to read the assemblies out of it.
- **Anything not named by `--assembly`.** Entries the caller did not ask about
  are not read.
- **Revocation.** No network calls are made, and none should be: this runs as
  part of a release and must give the same answer every time.

## When to delete this

This module exists because of a specific gap in
`github.com/Yubico/nuget-sign`'s `pkg/assembly` verifier as of
`v0.0.0-20260922130348-18234c6cd31f`. Its `checkTimestamp` chains the
certificate the timestamp token names, but it does not verify the token's own
CMS signature and it does not check that the token's `TSTInfo` message imprint
digests the signature it is attached to. A token issued for some other
signature, or one whose signature has been altered, would be accepted, and with
it whatever the timestamp is keeping alive after the signing certificate has
expired. That is why this program deliberately does not use
`pkg/assembly.Verify` — it would be verifying with the code under suspicion.

Delete this whole directory once upstream closes that gap, that is, once
`pkg/assembly` verifies the timestamp token's CMS signature and its
message imprint. At that point `nuget-sign` is checking what this program
checks, and a second implementation is upkeep without a payoff. The test suite
here is not worth keeping on its own: it tests this program, not the signer.

## Tests

The tests build everything in software — a code-signing root and leaf, a
timestamp root and leaf, an RFC 3161 timestamp authority that runs in-process,
and a minimal PE image — so there is no YubiKey, no network, and no checked-in
binary to go stale. Every negative case is produced by breaking one input:

```
go test -race ./...
```
