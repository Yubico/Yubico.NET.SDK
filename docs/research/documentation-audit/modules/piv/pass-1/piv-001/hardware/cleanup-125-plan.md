# Restore serial 125 around unattended multi-key PIV tests

## Scope and identity

`vendor-cleanup-125.py` applies the proven serial-103 v2 algorithm to one separately guarded
target: **serial 125**, reader `Yubico YubiKey OTP+FIDO+CCID 01 00`, and the exact existing
`baseline-125.json`. The original serial-103 v2 helper and shared `vendor-state.py` remain
unchanged. This helper accepts no alternate serial or baseline identity.

The recorded baseline identifies Management firmware **5.8.0.alpha.2**, effective firmware
5.8.0 and direct PIV GET VERSION `000001`. It reports default PIN/PUK with 3/3 retries,
default AES192 management key (raw ID `0A`) with Never touch, 24 ordinary key/certificate
slots absent, and CHUID/CCC/key-history/ADMIN absent. These observations describe a
development alpha device; they supply no older-commercial-firmware qualification.

Serial 125 has its own existing RSA2048 F9 key and 793-byte certificate. Its identity is
distinct from serial 103:

| Preserved serial-125 value | SHA-256 |
| --- | --- |
| F9 certificate DER | `84d4b3407b5ca5743ae7f0ea46a58ccb493db357f877483e5c288790fe34fd1a` |
| F9 SPKI, metadata and certificate | `5fbb057994305ab5e98f70b6c17a582fee5ccff580aeb8c9d821cf52ff3d4c27` |
| F9 encoded public-key metadata | `f0eb02559c7875ee51e0dee8f22ecf9f68fef5e0f137e973e2042d40d352cd74` |

The inherited guards compare all captured decoded F9 metadata fields, exact RSA2048 /
PinOnce / TouchNever / imported-origin values, public-key encoding hash, certificate DER
and SPKI identity before retry mutation. The helper uses serial 125's own baseline for
every comparison. Its version check obtains the expected alpha qualifier dynamically
from that pinned baseline; it imports no beta version or F9 identity from serial 103.

## Run the initial serial-125 control

Finish the six serial-103 supplemental probes and their independent cleanup first. Then
run this control before the canonical multi-key tests mutate serial 125. Existing dedicated
key authorization covers its explicit RESET control; it requires no physical touch,
hotplug or additional user action. From the repository root:

```bash
timeout --signal=TERM --kill-after=5s 240s /usr/bin/python3 docs/research/documentation-audit/modules/piv/pass-1/piv-001/hardware/vendor-cleanup-125.py --serial 125 --authorize-reset-serial 125 --reader 'Yubico YubiKey OTP+FIDO+CCID 01 00' --baseline docs/research/documentation-audit/modules/piv/pass-1/piv-001/hardware/baseline-125.json --receipt docs/research/documentation-audit/modules/piv/pass-1/piv-001/hardware/cleanup-control-125.json
```

`/usr/bin/python3` matches the interpreter path recorded in this baseline. The helper
checks the exact Python/vendor/source provenance as well as the live device identity.
It exclusively creates the receipt, header/status journal and three public-state files.
Use a new receipt stem for a subsequent attempt; preserve any failed control intact.

The required result is `restored-and-factory-authenticated`, exit zero, and successful
fresh before/after-reset/after-auth public captures. The two final captures must equal
serial 125's baseline in every captured field except root `utc` and `mode`. Before reset,
identity and F9 must already match; ordinary state differences are recorded for cleanup.
If the initial control fails, keep multi-key mutation pending and investigate its exact
guard/status rather than relaxing the target checks.

## Existing three-method multi-key lane

The user deferred physical ceremonies only. The existing unattended multi-key tests remain
in scope after the six primary supplemental cases. P07, canonical touch and hotplug remain
deferred. The coordinator owns temporary shared allowlist edits and restores its preserved
original bytes; this preparation edits no configuration or test source.

After the serial-125 control passes, temporarily allow exactly `[103, 125]`, with both
already attached keys available over SmartCard. Device enumeration chooses A/B ordering;
record actual serial roles rather than assuming index zero is 103. Select the canonical
project explicitly to exclude the new audit host:

```bash
dotnet toolchain.cs -- test --integration --project Yubico.YubiKit.Piv.IntegrationTests --filter "FullyQualifiedName~PivMultiKeyContentionTests"
```

The coordinator uses the repository test skill and serializes this run against every
other toolchain/device operation. Keep the Slow case included; `--smoke` would remove it.
The expected source denominator is three facts, with three executed and zero skipped
when both captured devices bind successfully. Retain raw exits, RESULT counts, actual
roles, original failures and every cleanup outcome.

| Existing method | Actual effects and oracle | State left for independent restoration |
| --- | --- | --- |
| `FindAllAsync_WithOpenSessionOnOneKey_IdentifiesOtherKeysAndPreservesSession` | Resets A, authenticates defaults, provisions 9A P256/PinOnce, signs before and after a scan, identifies B and checks both serials after closing A. Signatures are checked for nonempty output. | A's ordinary 9A key remains. B receives discovery/Management reads. |
| `ConcurrentPivSessions_OnTwoKeys_OperateIndependently` | Resets both, authenticates defaults, provisions 9A P256/PinOnce on both, verifies PIN and runs ten signing operations per key concurrently. Checks completion counts and nonempty output. | Both ordinary 9A keys remain. |
| `Rsa4096Keygen_OnOneKey_DoesNotDelayPivOperationsOnAnotherKey` | Resets/provisions both 9A P256/Once/Never, then tests both busy/responsive directions using RSA4096 generation in Retired1 and timed signing on the other key. Attempts both SDK resets after the body. | Its own resets need independent readback. A responsive signing task can outlive the four-second `WaitAsync` timeout; key generation is explicitly drained. Preserve any timeout and establish process/task drainage before independent reset traffic. |

These are the existing assertions, not a new cryptographic cross-key-binding oracle.
Counts/completion/timing and nonempty signatures qualify those exact observations.
The test bodies use ordinary slots and introduce no F9 replacement, fixed policy, Bio
enrollment or required presence ceremony. Independent cleanup remains a separate result
dimension even when the SDK's own reset paths return success.

## Restore each key independently after terminal execution

Await complete runner/process termination and resolve any outstanding native/task-drain
question before opening cleanup connections. A timeout is a recorded failure boundary;
RESET must not conceal incomplete same-key/recovery evidence. Preserve the source test's
known responsive-task timeout limitation with the corresponding execution result.

Run the unchanged serial-103 v2 helper against `baseline-103.json` and reader `00 00`,
using the coordinator's established command and a fresh receipt stem. Then run serial 125's
helper independently with a new final receipt:

```bash
timeout --signal=TERM --kill-after=5s 240s /usr/bin/python3 docs/research/documentation-audit/modules/piv/pass-1/piv-001/hardware/vendor-cleanup-125.py --serial 125 --authorize-reset-serial 125 --reader 'Yubico YubiKey OTP+FIDO+CCID 01 00' --baseline docs/research/documentation-audit/modules/piv/pass-1/piv-001/hardware/baseline-125.json --receipt docs/research/documentation-audit/modules/piv/pass-1/piv-001/hardware/cleanup-multikey-final-125.json
```

Retain both independent receipts and exact final public differences. Restore the original
tracked allowlist bytes after the two-key lane. Scope final hardware conclusions to these
two development 5.8 USB devices and the actual executed methods.

## Inherited algorithm and transformation proof

`reference-source/cleanup-125-provenance.json` records complete source/target hashes,
baseline-125 identity, seven allowlisted substitution rules with eleven occurrences,
and the full side-by-side unified diff. The transformation changes only the target
baseline digest, reader selector and serial-specific guards/labels. Forward and inverse
byte checks establish exact derivation from canonical v2. The canonical v2 source remains
SHA-256 `6042cef33b929136b3035c904255e3b18691f7cda96c789f2532fe2e7f3ad1a5`.

The derivative retains bounded PIN/PUK blocking with exact metadata decrement checks,
RESET only after both references block, fresh-session factory PIN and actual AES192
default-key authentication, payload-free header/status tracing, protected-secret exclusion,
three independent public readbacks and complete stable-field comparison. It retains the
180-second alarm, 45-second per-capture bound and external TERM/KILL envelope. It changes
neither applet/global version state nor shared response-chaining/public-helper behavior.
F9 receives public identity reads only; its existing private key, certificate and policy
remain preserved by the same reset exception and identity guards as proven v2.

| Prepared artifact | SHA-256 |
| --- | --- |
| `vendor-cleanup-125.py` | `9c079d681750ec8a3aef903d9b6f5e70ae11fb0050d9914130786709ca3714d3` |
| `baseline-125.json` | `1bac12341dfa823dfc5106a7b4ce78dee4497c2d14edcfd0cf3f112d9fd92705` |

Preparation verified the captured serial/reader/alpha version, empty ordinary slots,
default retry/management metadata, F9 public binding, exact transformation and all nine
installed vendor source/snapshot pairs. Static Python AST parsing accepted the derivative
without executing it. No identity-mixing issue was found for this exact captured baseline;
runtime serial, firmware, reader, source and F9 guards still establish fresh identity before
mutation. A changed reader mapping or runtime/source identity fails the control instead
of selecting another key. Actual serial-125 reset/authentication/control evidence remains
coordinator-owned and pending. This worker ran no helper, device operation, build, test,
formatter, commit or subagent and edited only the three assigned preparation files.
