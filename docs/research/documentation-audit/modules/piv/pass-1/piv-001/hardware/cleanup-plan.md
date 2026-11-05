# Restore serial 103 after coordinated PIV work

`vendor-cleanup-v2.py` restores the exact successful `baseline-103.json` public state through
an independent vendor path. The coordinator runs it after test/probe observations and
same-key controls have finished. The existing baseline helper and source references remain
frozen at their captured hashes.

## Execute the authorized cleanup

From the repository root, run:

```bash
timeout --signal=TERM --kill-after=5s 240s python docs/research/documentation-audit/modules/piv/pass-1/piv-001/hardware/vendor-cleanup-v2.py --serial 103 --authorize-reset-serial 103 --reader 'Yubico YubiKey OTP+FIDO+CCID 00 00' --baseline docs/research/documentation-audit/modules/piv/pass-1/piv-001/hardware/baseline-103.json --receipt docs/research/documentation-audit/modules/piv/pass-1/piv-001/hardware/cleanup-control-v2-103.json
```

Both serial arguments explicitly authorize the destructive PIV reset. The helper also
requires the exact captured reader, baseline SHA-256, frozen helper/manifest hashes,
installed source hashes and Python runtime provenance. Serial 125 remains outside its
connection scope. The helper enumerates reader descriptors, then opens only the selected
reader. Every opened card connection checks Management and direct PIV serial/version
before proceeding.

The command above runs the coordinator's repeated initial control before test mutation.
After coordinated tests, use the same command with a fresh receipt stem such as
`cleanup-final-v2-103.json`. The helper
exclusively creates its receipt, event journal and three public-state files. Existing
evidence stays intact. A pre-existing path, guard mismatch or public-read failure returns
failure; a pre-reset guard failure leaves reset unissued. The helper has no test hooks,
alternate-target mode or automatic destructive retry.

The coordinator temporarily narrows the existing integration allowlist to 103 using its
separate tooling assignment, preserves the tracked original, and restores it before final
verification. This helper reads and writes no allowlist or product/test configuration.

## Version-2 correction and preserved failed control

The original control stopped before PIN/PUK blocking or RESET with
`Complete live F9 public metadata changed`. Its independent before-readback matched the
baseline with zero differences. The original trace carries F7 F9 status `6117` and 256
response bytes, followed by GET RESPONSE (`C0`) status `9000` and 23 bytes. The frozen
public trace retains the first F7 payload but omits C0 payload bytes. Its higher-level
decoder receives the complete 279-byte response through vendor response chaining.

Version 1 incorrectly selected an F7 entry with final status `9000` and tried to compare
the complete live TLV map with that nonexistent complete baseline entry. Version 2
instead decodes the complete live response into vendor `SlotMetadata` and compares
`slot_metadata()`'s complete captured value: algorithm, PIN/touch policy, represented
origin, encoded-public-key SHA-256, SPKI SHA-256 and length. The existing exact raw
algorithm/policy/origin checks and certificate identity checks remain. Unknown raw TLV
equality stays outside the claim because the baseline never retained the full raw chain.
All other cleanup behavior remains byte-for-byte unchanged.

Original `vendor-cleanup.py`, baseline and public helper remain unchanged. Exact v1 source,
failed receipt, journal and before-readback copies are preserved under
`reference-source/cleanup-v1-*`. The versioned correction, hashes and source-confirmed
certificate-APDU shape are recorded in `reference-source/cleanup-v2-review.md`.

## Operation sequence and receipts

The artifact names below use the original illustrative stem `cleanup-103-01`. The
repeated-control command above produces the corresponding `cleanup-control-v2-103` files.

1. **Fresh before-reset readback.** The helper invokes frozen `vendor-state.py` in a new
   process and retains `cleanup-103-01.before.json`. It requires a successful capture,
   exact source/runtime provenance, unchanged device/PIV identity and unchanged F9 public
   metadata/certificate. It records all other differences from baseline: ordinary keys,
   certificates, retry state and management/admin changes are expected cleanup inputs.
   The receipt's comparison contains every actual difference rather than a blanket
   “restored” label.
2. **Bounded reset on a fresh connection.** Live Management DeviceInfo checks serial 103,
   firmware `5.8.0.beta.0`, USB-A, USB transport, non-FIPS and reset-blocked mask zero.
   Direct PIV reads recheck serial and the captured `0.0.1` sentinel. GET METADATA `96`
   must return the captured non-Bio status `6A88`. F9 metadata and certificate are reread
   immediately before retry mutation and compared with the original identity.
3. **Block PIN and PUK, then RESET.** Installed `PivSession.reset()` supplies the sequence,
   but its unbounded loops and sentinel-triggered cached retry/type fallbacks are replaced
   locally with explicit bounds. Raw public GET METADATA supplies each retry count.
   Public `verify_pin("")` blocks PIN, and public `unblock_pin("", "")` supplies the
   same invalid-PUK RESET RETRY payload as the installed reset implementation. Each
   invalid attempt must lower actual metadata by exactly one. Each loop has at most its
   initial remaining count, capped by the one-byte metadata range of 255. Already-blocked
   references need zero attempts. The helper sends `00 FB 00 00` only after both counters
   reach zero, then closes the connection.
4. **Fresh after-reset readback.** Frozen `vendor-state.py` captures
   `cleanup-103-01.after-reset.json` in a new process. Every captured stable field must
   equal the original baseline: PIN/PUK defaults and `3/3` retries; actual AES192 management
   metadata with default flag and Never touch; all 24 ordinary key slots and certificates
   absent; CHUID/CCC/key-history/ADMIN absent; unchanged F9; unchanged device identity and
   provenance. Raw public APDU trace and fallback records also participate in equality.
5. **Factory authentication on another fresh connection.** The helper rechecks identity
   and actual PIN/PUK default/retry metadata. It reads actual management metadata, requires
   algorithm ID `0A` (AES192), default flag and Never touch, then verifies the public
   factory PIN once and calls vendor `authenticate(DEFAULT_MANAGEMENT_KEY)` once. The
   installed public default constant is 24 bytes, matching actual AES192 key width.
   The receipt records operation success, serial, algorithm and constant provenance.
   PUK's default metadata flag supplies its check; the helper sends no PUK authentication
   experiment. The authentication connection closes before the next observation.
6. **Fresh after-authentication readback.** Frozen `vendor-state.py` captures
   `cleanup-103-01.after-auth.json` in a new process. The same complete comparison must
   match baseline. This verifies public restoration after disconnecting the authenticated
   session. Success is `restored-and-factory-authenticated`; failure exits nonzero and
   preserves its exact stage, public status words and prior artifacts.

Comparison excludes exactly the root `utc` timestamp and `mode` label (`baseline` versus
`final`). Every other captured field is compared, including helper/source identity,
selected reader, all slot outcomes and raw metadata/status observations. There are no
public-state restoration exceptions. The helper's readbacks use the original public helper;
its internal `PublicTrace` instruction guard stays unchanged.

The receipt records exact invocation, cleanup-helper SHA-256, original baseline hash,
installed-source/runtime provenance, readback file hashes, full public differences and
whether RESET returned successfully. The append-only `.events.jsonl` journal flushes an
entry before each cleanup APDU and its header/status after return. It carries no request
or response payload bytes, lengths, payload hashes, PIN text, keys, witnesses, challenges
or authentication responses. The independent public-state files retain their established
public-only trace policy.

## Firmware handling and preservation boundary

The observed physical device reports Management `5.8.0.beta.0` and PIV GET VERSION
`000001`. Installed vendor session construction therefore caches a TDES fallback, despite
raw management metadata reporting AES192. The cleanup helper leaves session version,
private caches and the global private version override untouched. Its small local
`MetadataTypedSession` overrides the **public** `management_key_type` property used by
installed `authenticate()` and supplies the actual, freshly checked metadata type. The
installed vendor implementation still performs the full mutual authentication exchange.
Other firmware/default variants fail this bounded serial-103 scope rather than triggering
an algorithm guess. AES256-at-5.7 remains the rejected planning hypothesis.

F9 stays read-only throughout. The APDU guard permits only F9's public certificate GET,
public metadata/identity reads, the two bounded blocking commands, RESET and post-reset
factory authentication. It provides no import/generate/move/delete/set-policy path and
requests no F9 private-key operation. The vendor's documented reset exception preserves
the existing F9 key and certificate. Exact metadata, public-key encoding hash, certificate
DER hash and SPKI hash are compared before/after; these establish public identity.
Private-key possession requires the coordinator's separately retained attestation-signature
oracle. The helper makes no factory-CA or Bio hardware qualification claim.

Factory authentication uses only published test defaults on the dedicated reset key.
Python/vendor immutable strings and byte buffers, and native cryptography intermediates,
prevent a guarantee of prompt zeroization. The helper accepts no production credential
input, reads no protected management secret and disables vendor logging. Its process ends
after the operation, and its payload-free journal bounds diagnostic exposure.

## Timing and failures

Each public capture subprocess has at most 45 seconds. The helper applies a 180-second
process alarm; the outer `timeout` sends TERM at 240 seconds and KILL five seconds later
to bound a blocked native PC/SC call. Python signal delivery can wait for a native call to
return, so the external limit remains part of the execution command. The helper catches
TERM/interrupt/alarm for a best-effort terminal receipt. If KILL is needed, retain the
journal and partial files as interrupted evidence; an empty/partial receipt is a failed
execution, never a restoration claim.

A failure during blocking may leave PIN and/or PUK blocked. A reset APDU transport failure
can leave reset completion uncertain even when `reset_completed` is false. Preserve the
last `apdu-start`/`apdu-finish` records and run a separately named read-only public capture
before selecting the next authorized action. A fresh explicit cleanup invocation can
handle zero retry counters. Identity/source/F9 mismatches require investigation; bypassing
their guards would invalidate the preservation claim.

Preparation consists of source inspection and static checks. Hardware execution, builds,
tests, formatters and commits remain coordinator-owned or outside this assignment.
