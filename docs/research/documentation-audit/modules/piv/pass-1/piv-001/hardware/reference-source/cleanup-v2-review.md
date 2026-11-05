# Versioned F9 comparison correction

## Observed failure and source diagnosis

The coordinator's initial control `cleanup-control-103.json` reports failure at
`Complete live F9 public metadata changed`, with `reset_completed: false`. Its event list
contains zero blocking-attempt, reference-blocked, reset-issued or reset-completed entries.
The fresh independent before-readback has zero baseline differences. This execution
establishes a failed preservation guard before mutation, rather than a reset failure.

The baseline F9 GET METADATA trace records `6117` with 256 payload bytes, then C0 records
`9000` with 23 payload bytes. Frozen `PublicTrace` stores raw public bytes for F7/F8/FD
instructions; it stores only length/status for C0. Installed
`yubikit.core.smartcard.ResponseChainingProcessor`, lines 231–250, assembles both fragments
before the public protocol returns. Thus `raw_metadata()` parses all 279 bytes, while the
baseline trace has no F7 record with final status `9000` and no complete raw TLV payload.

The baseline's decoded `slots/0xF9/metadata/value` used the complete assembled response.
Version 2 decodes the live complete fields with the same installed `SlotMetadata` shape
and frozen helper `slot_metadata()` function, then compares every captured decoded field:
algorithm ID/name, PIN and touch policy ID/name, represented origin, encoded-public-key
SHA-256, SPKI SHA-256 and length. Existing exact raw algorithm `07`, policies `02 01`,
origin `02`, public-key hash and certificate DER/SPKI comparisons remain unchanged.
The helper claims equality of captured public fields; full unknown-TLV equality remains
unestablished because the baseline lacks the complete raw chain.

The v1→v2 source diff contains only this F9 comparison block and its required public
metadata type imports. Blocking, RESET, factory authentication, serial/source guards,
timeouts, output retention and all other behavior remain byte-identical.

## Certificate GET shape confirmed from installed source

Installed `PivSession.get_object()`, lines 1227–1237, sends GET DATA with the object-selector
TLV and omits the optional Le argument. `SmartCardProtocol.send_apdu()`, lines 424–443,
defaults Le to zero and forwards it. `ShortApduFormatter`, lines 158–165, appends Le only
when nonzero; its extra zero for absent data does not apply to a nonempty object selector.
`ExtendedApduFormatter`, lines 174–182, likewise appends Le only when nonzero. Therefore
this certificate GET ends with `5C 03 5F FF 01` under either installed formatter. The
existing `apdu.endswith(...)` F9 certificate guard stays unchanged. Vendor response
chaining already handles `61xx`; this correction changes no protocol machinery.

## Retained artifact identities

All original artifacts remain unchanged. Exact copies use exclusive creation under this
directory:

| Artifact | SHA-256 |
| --- | --- |
| `cleanup-v1-vendor-cleanup.py` | `8e5aba11c7cb391b454aac9d1b4c478c69f0735671c701793f91d4f7f4bc6a7f` |
| `cleanup-v1-cleanup-control-103.json` | `d9fb566ed4572be7868f8361ddabc3ec83aeea2f3c5c029aa361701d7421d266` |
| `cleanup-v1-cleanup-control-103.events.jsonl` | `d785dfacc72e2118e42f58fda734f9d184449510703e8ad93ae57ff7bd018080` |
| `cleanup-v1-cleanup-control-103.before.json` | `533b244ecc0a9fae51356b399b9c379eee3e76ea4d64eaf50db96001fcf95a3b` |
| `../vendor-cleanup-v2.py` | `6042cef33b929136b3035c904255e3b18691f7cda96c789f2532fe2e7f3ad1a5` |

The baseline, public helper and installed-source manifest still match their pinned
SHA-256 values recorded in `cleanup-source-review.md`. Static `ast.parse` accepts v2;
byte comparisons confirm all four archived v1 artifacts match the originals. No cleanup
helper, device operation, build, test, formatter, subagent or commit ran in this correction
worker. The coordinator's repeated control establishes actual hardware behavior for v2.
