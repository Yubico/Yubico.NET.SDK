# Supplemental OpenPGP hardware probes

## Readiness and scope

Prepared for coordinator execution against the dedicated allow-listed SmartCard key, serial **103**.
Preparation used installed YubiKey Manager **5.9.2** source and Context7 vendor API documentation.
Focused API references were the vendor's
[`yubikit.openpgp`](https://github.com/Yubico/yubikey-manager/blob/main/_autodocs/api-reference/yubikit.openpgp.md)
and [`ykman.openpgp.safe_reset`](https://github.com/Yubico/yubikey-manager/blob/main/_autodocs/api-reference/ykman.openpgp.md)
documentation, checked against `/usr/lib/python3.14/site-packages/yubikit/openpgp.py`,
`ykman/openpgp.py`, and `ykman/device.py` through source/signature introspection. Execution JSON
retains the installed OpenPGP implementation/helper source hashes; current documentation is API
guidance rather than independent firmware evidence. Context7's .NET documentation was consulted
for host cryptographic verification APIs.
The coordinator reported YubiKey 5 NFC Enhanced PIN, firmware `5.8.0.beta.0`, OpenPGP card `3.4`,
application `5.8.0`, retries `3/0/3`, signature PIN policy `Once`, and KDF disabled. Fresh helper
evidence establishes the actual pre-mutation state. This packet contains proposed effects and
unexecuted fixtures; it makes no build, test, touch, cancellation, recovery, or cleanup success claim.

Owned surfaces are this plan, `hardware/vendor-state.py`, and `validation/hardware-probe/`.
Shipping implementation, existing assertions, API snapshots, and other applet state stay outside
these probes. The existing 49-method suite remains the main integration coverage; these cases add
PIN-identity and physical-presence evidence rather than repeating its import and ECDH coverage.

## First read-only baseline

### V1 observation and narrow V2 read-path correction

The coordinator's first baseline is preserved unchanged as `hardware/vendor-baseline.json`.
It reported direct `OpenPgpSession.version` **0.0.1**, alongside management metadata
**5.8.0.beta.0**. Its SIG/DEC/ATT certificate reads explicitly failed with `NotSupportedError`;
AUT used the vendor's legacy default-selection path and returned empty certificate data.
Those outcomes are failed reads, not evidence that the first three certificates were absent.
The coordinator's attempted `ykman.cli` introspection failed with `ModuleNotFoundError`;
direct introspection of the installed `yubikit.openpgp`/`ykman.openpgp` modules succeeded.
No CLI-equivalence claim relies on that failed import.

Before editing the helper, preparation retained its exact v1 bytes and the installed vendor
OpenPGP implementation/helper source under
`validation/hardware-probe/evidence/vendor-helper-v2-preparation/`. That directory's manifest
records source hashes and the failed baseline's unchanged identity. V2 additionally records
its own source SHA-256 in execution JSON. C# probes remain coordinator-owned and unchanged
by this correction.

V2 leaves the direct application version unchanged. Only when fresh serial-guarded management
metadata reports matching alpha/beta version components at least 5.2.0 and the direct session
reports below 5.2.0 does it select an explicit public-APDU certificate path. This path mirrors
the installed vendor source's commands, using management metadata solely for SELECT DATA framing:

| Slot | Public command sequence |
| --- | --- |
| ATT | GET DATA header `00 CA 00 FC`, empty command data. |
| AUT | SELECT DATA `00 A5 00 04`, data `60 04 5C 02 7F 21`; GET DATA `00 CA 7F 21`. |
| DEC | SELECT DATA `00 A5 01 04`, same selection data; GET DATA `00 CA 7F 21`. |
| SIG | SELECT DATA `00 A5 02 04`, same selection data; GET DATA `00 CA 7F 21`. |

These are logical APDU header/data inputs to the installed `SmartCardProtocol`, which owns wire
framing and response chaining. The installed source prepends `06` to selection data for firmware
through 5.4.3; fresh 5.8 beta metadata chooses the six-octet selection data above. Every explicit
command records header/data, successful `9000` status and returned length, or the actual APDU error
status/type. A successful zero-octet certificate response means `absent`; nonempty bytes must parse
as a DER certificate before public hashes are recorded. Parser or APDU errors remain errors.
The JSON names this path `explicit-public-apdu`; it makes no claim that vendor `get_certificate`
accepted the low reported version.

Installed `get_uif` also synthesizes `Off` below 4.2.0. Under the same development-version mismatch,
V2 therefore reads each slot's public UIF DO and uses the installed `UIF.parse` implementation,
retaining command/status provenance. This prevents a synthesized default from masquerading as
the final observed touch policy. Neither path authenticates or writes persistent configuration;
no private `_version` field is overridden. Att certificate presence remains an observation for
the next baseline. Cleanup compares either observed certificate identity or observed absence;
it accepts only a successful original baseline, never the preserved v1 failed baseline.

Run from the repository root, before any reset or configuration change:

```bash
python docs/research/documentation-audit/modules/openpgp/pass-1/openpgp-001/hardware/vendor-state.py baseline --output docs/research/documentation-audit/modules/openpgp/pass-1/openpgp-001/hardware/vendor-baseline-v2.json
```

The helper enumerates SmartCard devices and opens only the management-identified serial 103.
It also checks the OpenPGP AID serial. A SELECT preflight refuses a terminated applet before the
vendor session constructor can auto-activate it. Baseline mode performs public reads without PIN
verification or configuration writes; certificate SELECT DATA changes only the volatile read cursor.
The JSON records device/application versions, capability flags, PIN policy/limits/retries, KDF
algorithm/hash/count and salt shape, key information, current/supported algorithms, all UIFs,
signature counter, fingerprints, generation times, and certificate DER/SPKI SHA-256 identities.
The observed Att certificate identity or explicit absence supplies subsequent preservation checks.

Each read has an explicit `ok`, `absent`, or `error` status. `absent` is reserved for a successful
zero-octet certificate response or the vendor API's exact empty-certificate result. APDU errors retain their status word and exception type;
parser/transport errors remain errors. No generic exception becomes an empty key/certificate result.
The helper omits PINs, private keys, KDF initial hashes, derived credentials, and exception messages.
Factory verification uses published fixed test credentials only. Python's immutable factory strings
and vendor-returned objects carry no secure-erasure guarantee; this helper acquires no user secrets.

Keep the original baseline JSON unchanged. Output files use exclusive creation. Choose a new path
for a repeated observation. A nonzero exit requests inspection of explicit JSON error records.

## Compiler preparation and retained provenance

The unique host is `AuditPgp001.Hardware.IntegrationTests`. It uses real OpenPgp/Core/TestShared
project references, the canonical TestShared allow list, centrally versioned xUnit v2/VSTest
packages, and inherited warnings-as-errors/.NET/xUnit analyzers. Its project lives at:

`validation/hardware-probe/tests/Yubico.YubiKit.AuditPgp001.Hardware.IntegrationTests/src/`

Outputs stay under `/tmp/opencode/openpgp-001-hardware`. The host/capture hooks derive from the
latest frozen HSM-001 tooling. `record-provenance.py` validates those original files against the
HSM manifest recorded in this run's `prior-archive-baseline.json`, then records their identities and
all authored probe inputs. `retain-inputs.py` reuses that verified HSM source with one explicit
workspace substitution; it preserves actual-Csc source/reference/analyzer/config set reconciliation,
warnings-as-errors checks, dependency identities, and retained source/configuration/assets text.

Coordinator commands, in this order:

```bash
python docs/research/documentation-audit/modules/openpgp/pass-1/openpgp-001/validation/hardware-probe/record-provenance.py
dotnet toolchain.cs -- build --project AuditPgp001.Hardware.IntegrationTests --clean
python docs/research/documentation-audit/modules/openpgp/pass-1/openpgp-001/validation/hardware-probe/capture-compiler.py v1-build
python docs/research/documentation-audit/modules/openpgp/pass-1/openpgp-001/validation/hardware-probe/retain-inputs.py v1-build final-inputs-v1
```

Capture **immediately after the real compilation, before any test, incremental build, formatter, or
other command that could replace generated compiler evidence**. The pre-CoreCompile hook records
membership; the post-CoreCompile hook records actual `CscCommandLineArgs`. An empty argument list
fails capture. Retention reconciles actual arguments, rather than treating an evaluated MSBuild item
list as proof of compilation. Preserve the toolchain RESULT block, zero-warning result, actual Csc
evidence, and full test step logs. Source corrections require a fresh provenance/evidence identity
and a new real build/capture; preserve each failed attempt.

## Bounded hypotheses and proposed effects

Each case independently checks serial 103, opens SmartCard, resets OpenPGP before its body, and
disposes all .NET sessions before external cleanup. The KDF case first checks the applet's KDF
capability in freshly read application-related data, **before its first mutation**. An absent flag
fails explicitly instead of assuming that device-wide firmware proves application support.
The managed body has a cooperative 90-second token; VSTest has a 120-second session watchdog.
These are execution budgets, not proof that a native command stops at token cancellation.

| Method | One hypothesis / oracle | Expected mutation or ceremony |
| --- | --- | --- |
| `KdfDistinctSalts_ExtendedVerifyUsesUserIdentity` | Three distinct User/Reset/Admin salts retain User identity for both ordinary and extended VERIFY. A host-verified ordinary P-256 signature is the control; extended VERIFY must then accept the same User credential. | OpenPGP reset; generate Sig P-256; set S2K SHA-256, 100000 octets, three distinct public fixture salts, correct initial factory hashes. Fresh session verifies admin/ordinary User, signs, unverifies, then extended-verifies. No compensating PIN change or salt equalization. |
| `ExtendedWrongPin_ReportsUserRetryCounter` | One deliberately wrong extended User VERIFY decrements User retries, leaves Reset Code retries unchanged, and reports the observed User count in the typed exception. | OpenPGP reset, one wrong User verification. Fresh status and typed retry count are logged before assertions. |
| `TouchSign_P256_HostVerifies` | On-policy Sign pairs one request/completed notification and yields a host-verifiable P-256/SHA-256 signature. | OpenPGP reset; generate Sig; set Sig UIF `On`; verify ordinary User; **one touch**. |
| `TouchAuthenticate_P256_OrdinaryVerifyIsInsufficient` | Ordinary User verification yields security-status denial for Authenticate; extended verification plus On policy produces a host-verifiable signature. | OpenPGP reset; generate Aut; silent Off-policy denial control with exact `6982`; unverify; set Aut `On`; extended verification; **one touch**. |
| `TouchDecrypt_Rsa_KnownPlaintextAndModeBoundary` | Ordinary User verification yields `6982`; extended verification plus On policy decrypts the host-encrypted known plaintext. | OpenPGP reset; generate Dec RSA-2048; host PKCS#1 encryption; silent denial control; unverify; set Dec `On`; extended verification; **one touch**. Returned plaintext is compared in constant time and cleared. |
| `TouchAttest_PreservedIssuerSignsGeneratedKeyCertificate` | Existing Att key signs a certificate whose signature verifies under the preserved issuer public key and whose SPKI matches the generated Sig public key. | OpenPGP reset; read/hash Att certificate; generate Sig P-256; set **Att** UIF `On`; extended User verification; **one touch**; create Sig certificate. Preserve Att key/certificate and compare issuer bytes afterward. No Att private-key import/generation. |
| `CancelSign_NoTouch_ObserveTerminalThenFreshRecoveryBeforeReset` | Observe actual no-touch Sign cancellation terminal status, then prove a fresh connection can read/authenticate/sign **before reset**. | OpenPGP reset; generate Sig P-256; set Sig `On`; verify User; schedule cancellation after 1500 ms; **withhold touch**. Await the operation itself to terminal completion; dispose; fresh session reads retries/counter/UIF, verifies admin, turns Sig UIF Off, verifies User, and signs a host-verified recovery message. |

The first two cases intentionally retain assertions that may expose the current implementation's
extended-PIN salt/counter identity mapping. A failing result is evidence, not a reason to weaken the
oracle. If the KDF case fails at extended verification, report that exact point: its later extended
crypto path was unreached. Real Authenticate/Decrypt mode boundaries are separately exercised by
their ordinary, KDF-disabled touch cases. The cache-owned KDF object is observed without caller
disposal; this packet ships no cache-ownership fix or separate speculative cache test.

All key generation occurs on the device. Managed credential arrays, salt fixtures, initial hashes,
returned sensitive plaintext, and owned operation results have explicit cleanup. Each authored KDF
configuration is disposed, and the session retains ownership of its cache-returned configuration.
Attestation verification establishes the certificate signature under the preserved issuer and key
binding; it makes no independent manufacturer-root trust-chain claim.

## Coordinator execution and one human touch batch

Run each case through `run-probe.py`. It invokes the canonical toolchain, preserves the exact command
and exit code, then performs an independent fresh public read and vendor reset/readback in nested
`finally` blocks. A failing test remains a failing result even when cleanup succeeds. Inspect cleanup
before starting another case. Replace the case/name below with each exact table entry and a fresh
evidence name:

```bash
python docs/research/documentation-audit/modules/openpgp/pass-1/openpgp-001/validation/hardware-probe/run-probe.py KdfDistinctSalts_ExtendedVerifyUsesUserIdentity --name v1-kdf --baseline docs/research/documentation-audit/modules/openpgp/pass-1/openpgp-001/hardware/vendor-baseline-v2.json
python docs/research/documentation-audit/modules/openpgp/pass-1/openpgp-001/validation/hardware-probe/run-probe.py ExtendedWrongPin_ReportsUserRetryCounter --name v1-retries --baseline docs/research/documentation-audit/modules/openpgp/pass-1/openpgp-001/hardware/vendor-baseline-v2.json
```

After warning-free compilation and unattended observations, ask the human once for a coordinated
batch: touch Sign, Authenticate, Decrypt, and Attest when each operation's LED requests it; then
withhold touch throughout the cancellation/recovery case. Each touch method carries
`RequiresUserPresence` and `Slow`; cancellation carries both because it needs coordinated withholding
and may wait for a native response. Run these one at a time with the same runner syntax and fresh
names, in that order. The presence callback announces the operation and returns promptly; it never
waits for the human or claims that a touch occurred.

For cancellation, retain requested-delay, callback time, actual terminal exception/status word,
resolution outcome, total observed time, fresh-session readback, and cryptographic recovery result.
The test accepts an observed cancellation exception or a device/APDU failure as distinct terminal
observations and checks their corresponding notification outcome. Unexpected successful signing
fails after recovery is attempted. A watchdog kill or missing recovery assertion is **unqualified
recovery**, even if the vendor read afterward succeeds. The wrapper's pre-reset public read
complements rather than replaces the .NET recovery assertion. Keep the key untouched until the
coordinator reports recovery and cleanup finished.

## Independent cleanup and actual reset exceptions

The runner invokes this helper after each case. The coordinator can also invoke it after the main
suite or an interrupted probe, using a fresh output name:

```bash
python docs/research/documentation-audit/modules/openpgp/pass-1/openpgp-001/hardware/vendor-state.py cleanup --authorize-reset-serial-103 --baseline docs/research/documentation-audit/modules/openpgp/pass-1/openpgp-001/hardware/vendor-baseline-v2.json --output docs/research/documentation-audit/modules/openpgp/pass-1/openpgp-001/validation/hardware-probe/final-vendor-state.json
```

Cleanup explicitly authorizes OpenPGP factory reset through installed `ykman.openpgp.safe_reset`.
That vendor helper blocks User/Admin verification and sends TERMINATE/ACTIVATE; its return value
alone proves no complete reset scope. This helper closes that connection, opens a new guarded
connection for full public readback, closes it, opens another for fixed factory User-sign,
User-extended and Admin verification, and finally opens a fourth for independent full readback.

The JSON checks default retry counts `3/0/3`, Once policy, KDF disabled, zero signature counter,
empty normal-slot key status/certificates/fingerprints/generation times, normal-slot UIF Off,
and preserved Att certificate DER/SPKI identity (or preserved absence when actually observed).
It retains baseline/final algorithm attributes,
UIFs, key information, fingerprints, generation times and all certificate identities with explicit
unchanged/different observations. Inspect those observations to report actual reset exceptions
rather than claiming all algorithms/configuration were erased. The baseline file SHA-256 binds
the preservation comparison. Public certificate identity preservation alone makes no claim that
this helper exercised the Att private key; the touch-attestation signature oracle supplies that
separate observation if it completes.

A read, reset, factory verification, or required check failure yields a nonzero exit and explicit JSON.
Stop and inspect that evidence before another mutating operation. Do not equate transport recovery,
factory-PIN acceptance, Att preservation, full default restoration, and shipping implementation
correctness: each has its own observations above.

## Prepared additions: X25519 Decipher and read-only Att public-key CRT

The coordinator reported a complete v2 vendor baseline, 49/49 existing integration methods passing,
and independent cleanup with preserved Att certificate identity. The seven initial supplemental
methods remain in their compiled source set while these two additions are prepared. These reported
results establish neither X25519 Decipher nor Att public-key CRT correctness: the existing vector
import method compares the X25519 public point without exercising agreement.

### Source and ownership boundary

`validation/hardware-probe/v2-prepared/DecipherAndAttReadProbes.cs.snapshot` contains the **one new
partial file**, with two focused methods. It is intentionally outside the host's implicit Compile
scope while the coordinator runs the initial probes. Preparation leaves the original three C# files,
project, previous compiler evidence and retained full source snapshots untouched. The coordinator
installs the prepared file only after those executions finish. Original evidence remains bound to
its original membership; the next real compilation must show four authored C# sources.

`validation/hardware-probe/vendor-reference-v2.py` is the single companion helper, with `x25519` and
`att` modes. It reuses serial/AID guards and public error records from `hardware/vendor-state.py`,
has no reset/import/generation/configuration-write path, and creates fresh evidence exclusively.
Its source and installed vendor method identities are recorded in output JSON. Preparation provenance
is retained separately under `v2-prepared/`; the earlier compiler/provenance records stay unchanged.

Focused references reuse the investigator's `evidence/source-analysis.md` and original source
manifest: Card application **3.4.1 §7.2.11** specifies ECDH `A6{7F49{86 external point}}` and
§7.2.14 specifies read-existing-public-key; **RFC 7748 §6.1** supplies Alice/Bob's fixture and shared
secret. The vendor [Attestation extension](https://developers.yubico.com/PGP/Attestation.html) and
pinned Python revision `4ca60f706af930459138d8dc0f0f953480e1c7a4` specify Att CRT
`B6 03 84 01 81`. Installed Python 5.9.2 source independently confirms `CRT.ATT` and that
`OpenPgpSession.decrypt` wraps EC peer data before PSO DECIPHER, without a session-version gate.
Context7 API documentation was checked against that exact installed source; no broad firmware
investigation or Core implementation inspection is part of these additions.

### Cases and effects

| Method / reference | Hypothesis and oracle | Effects |
| --- | --- | --- |
| `X25519Decipher_Rfc7748AliceBob_MatchesSharedSecret` | Imported Alice key first reports the exact RFC public point. SDK `DecryptAsync(BobPublic)` must then match the RFC shared secret in constant time. A thrown card status remains a failure; returning bytes of the right length is insufficient. | Uses the existing bounded reset-before-body scope; requires KDF disabled and Dec UIF Off; imports only published Alice fixture into Dec through the existing canonical `Curve25519PrivateKey.CreateFromValue(..., KeyType.X25519)` API; verifies factory User in extended mode. Owned private/derived buffers and the disposable key are cleared/disposed. Leaves Dec key available for the companion, including after failure. |
| Python `x25519` | A new guarded connection verifies current Dec is X25519, KDF disabled, actual UIF Off, and its public point is Alice's. It extended-verifies the fixed factory test User PIN, then calls installed `OpenPgpSession.decrypt(BobPublic)` on **that same card key** and compares the RFC shared secret in constant time. | Performs authentication and agreement only. No reset, key import or configuration change. Output contains metadata/status/match booleans, never shared-secret bytes or hashes. Caller-owned mutable shared/expected buffers are cleared. The vendor API returns immutable Python bytes, whose secure erasure cannot be guaranteed; this limitation is recorded explicitly. |
| `AttPublicKey_ReadOnly_MatchesPreservedIssuerSpki` | SDK `GetPublicKeyAsync(KeyRef.Att)` must return a parsable RSA public-key template whose SPKI matches the preserved current issuer certificate. Issuer certificate SHA-256 binds the observation back to the independent baseline. | **Bypasses reset-before-body**. Direct guarded SmartCard session, finite 15-second token, public certificate/key reads only. No PIN verification, generation, import, reset or key/configuration replacement. |
| Python `att` | Correct installed/vendor Att CRT public-key query parses as `7F49{81 modulus,82 exponent}`, and its RSA SPKI matches both current certificate and baseline SPKI. | Explicit public GET DATA `00 CA 00 FC`, then read-existing-public-key `00 47 81 00` with data `B6 03 84 01 81`. Records actual command/status/length and certificate/SPKI identities. It neither generates a key nor queries private key material. No PIN or reset. |

The investigator identified raw unwrapped X25519 point input in the SDK and SDK Att CRT
`B6 01 81`, versus the wrapper and CRT above. The new SDK cases retain the intended cryptographic
and key-identity contracts: preserve failures and actual status words rather than rewriting the
probe to pass around either suspected defect. These probes change no shipping implementation.

### Coordinator install and actual-Csc capture

After the original source-bound case executions finish, install the exact prepared bytes:

```bash
cp --no-clobber docs/research/documentation-audit/modules/openpgp/pass-1/openpgp-001/validation/hardware-probe/v2-prepared/DecipherAndAttReadProbes.cs.snapshot docs/research/documentation-audit/modules/openpgp/pass-1/openpgp-001/validation/hardware-probe/tests/Yubico.YubiKit.AuditPgp001.Hardware.IntegrationTests/src/DecipherAndAttReadProbes.cs
dotnet toolchain.cs -- build --project AuditPgp001.Hardware.IntegrationTests --clean
python docs/research/documentation-audit/modules/openpgp/pass-1/openpgp-001/validation/hardware-probe/capture-compiler.py v3-decipher-att-build
python docs/research/documentation-audit/modules/openpgp/pass-1/openpgp-001/validation/hardware-probe/retain-inputs.py v3-decipher-att-build final-inputs-v3-decipher-att
```

The fresh evidence names above avoid existing `v1-build`/`v2-build` records. Capture immediately
after the real compile, before any test/incremental build. Retention must show all four authored
sources in actual Csc membership and preserve source/configuration identities. Review the warning
count and full failed attempts normally; no project/analyzer/runner gate changes are proposed.

### Same-key X25519 sequence: SDK → companion → cleanup

**Use the sequence below rather than `run-probe.py` for this new case:** that runner's existing
case list remains unchanged, and running cleanup before the reference would destroy same-key
evidence. The coordinator runs this bounded sequence from the repository root. Nested `finally`
blocks invoke the reference even after the expected SDK failure and independently clean up afterward.
Each command/log/exit remains separate; a successful companion or cleanup does not turn an SDK
failure into a passing SDK case.

```python
from pathlib import Path
import json
import shlex
import subprocess
import sys

run = Path('docs/research/documentation-audit/modules/openpgp/pass-1/openpgp-001')
probe = run / 'validation/hardware-probe'
baseline = run / 'hardware/vendor-baseline-v2.json'
evidence = probe / 'evidence/v3-x25519-same-key'
evidence.mkdir(parents=True, exist_ok=False)
results = {}

def command(label, arguments):
    with (evidence / (label + '.log')).open('x') as output:
        output.write('$ ' + shlex.join(arguments) + '\n')
        output.flush()
        completed = subprocess.run(arguments, stdout=output, stderr=subprocess.STDOUT)
        output.write('\nEXIT=' + str(completed.returncode) + '\n')
    results[label] = completed.returncode

try:
    command('sdk', ['dotnet', 'toolchain.cs', '--', 'test', '--integration',
        '--project', 'AuditPgp001.Hardware.IntegrationTests', '--filter',
        'FullyQualifiedName~X25519Decipher_Rfc7748AliceBob_MatchesSharedSecret'])
finally:
    try:
        command('vendor-reference', [sys.executable, str(probe / 'vendor-reference-v2.py'),
            'x25519', '--baseline', str(baseline), '--output', str(evidence / 'vendor-reference.json')])
    finally:
        try:
            command('cleanup', [sys.executable, str(run / 'hardware/vendor-state.py'),
                'cleanup', '--authorize-reset-serial-103', '--baseline', str(baseline),
                '--output', str(evidence / 'cleanup.json')])
        finally:
            with (evidence / 'result.json').open('x') as output:
                json.dump(results, output, indent=2)
                output.write('\n')
```

Retain the SDK toolchain RESULT block and full step logs immediately, before the five-run retention
window rotates. The companion's phase/preconditions distinguish an unreached SDK import from a
same-key formatting result. Inspect cleanup before starting another mutation.

### Read-only Att query and correct-CRT companion

After cleanup, these exact commands exercise the SDK read and its independent public-CRT reference.
Preserve the SDK failure if observed, then run the companion regardless of that exit code. Neither
command requests reset or key replacement:

```bash
dotnet toolchain.cs -- test --integration --project AuditPgp001.Hardware.IntegrationTests --filter FullyQualifiedName~AttPublicKey_ReadOnly_MatchesPreservedIssuerSpki
python docs/research/documentation-audit/modules/openpgp/pass-1/openpgp-001/validation/hardware-probe/vendor-reference-v2.py att --baseline docs/research/documentation-audit/modules/openpgp/pass-1/openpgp-001/hardware/vendor-baseline-v2.json --output docs/research/documentation-audit/modules/openpgp/pass-1/openpgp-001/validation/hardware-probe/att-correct-crt-reference-v3.json
```

This preparation executes no build, test, hardware operation, formatter, agent, or commit. The new
partial remains a noncompiled snapshot until coordinator installation; all outcomes await execution.
