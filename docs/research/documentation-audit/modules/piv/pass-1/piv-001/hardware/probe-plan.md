# PIV public-state capture and cleanup handoff

## Baseline ready for coordinator execution

`vendor-state.py` captures public state using installed YubiKey Manager 5.9.2. The helper
checks the reviewed source hashes before opening a card. Its baseline and final modes share
the same read-only path. The coordinator owns hardware access and execution evidence.

The coordinator's fresh CLI observation identifies serial **103** as the primary target:
YubiKey 5 NFC Enhanced PIN, USB-A, Management version `5.8.0.beta.0`, PIV version `5.8.0`,
PIN/PUK retries `3/3`, management algorithm **AES192**, default flags reported true, CHUID
and CCC absent. These are coordinator-reported observations pending the independent JSON
capture. Serial **125** is also attached and stays outside this helper's connection scope.
The earlier AES256-at-5.7 planning expectation was incorrect for the observed device.
The helper records actual management metadata and encodes no firmware-to-algorithm guess.

The explicit PC/SC reader selector keeps serial 125 unopened. The vendor's
`list_all_devices([SmartCardConnection])` opens each key to identify it; this helper instead
enumerates reader descriptors, selects one exact reader, and reads its Management serial
on the connection used for PIV. A mismatch fails before selecting PIV. The coordinator
maps the reader to 103 using existing device evidence or coordinated hotplug. Reader names
alone are routing hints; the live serial establishes identity.

From the repository root, list reader descriptors without opening cards:

```bash
python -c 'from ykman.pcsc import list_devices; print("\n".join(str(d.fingerprint) for d in list_devices()))'
```

After the coordinator establishes the exact reader name for 103, assign it verbatim to
`READER_103`. Run the following baseline command before any test mutation:

```bash
python docs/research/documentation-audit/modules/piv/pass-1/piv-001/hardware/vendor-state.py baseline --serial 103 --reader "$READER_103" --output docs/research/documentation-audit/modules/piv/pass-1/piv-001/hardware/baseline-103.json
```

The helper reserves the output with exclusive creation, prints the result path/status,
and exits nonzero after a read, parsing, identity or source-guard failure. Preserve failed
captures and use a new suffix for subsequent attempts. Resolve baseline failures before
mutation. The helper has no reset or authentication path.

Capture the final public state in a fresh process:

```bash
python docs/research/documentation-audit/modules/piv/pass-1/piv-001/hardware/vendor-state.py final --serial 103 --reader "$READER_103" --output docs/research/documentation-audit/modules/piv/pass-1/piv-001/hardware/final-103.json
```

Compare device identity/versions, `pin`, `puk`, `management_key`, `bio`, all `slots`,
`objects`, and `admin` between the JSON files. Timestamps, APDU sequencing, mode and helper
hash identify each execution and belong in provenance rather than restoration equality.
Record every public-state difference and its intended cleanup disposition. A successful
capture establishes that its reads completed; restoration needs the separate comparison.

## Captured public evidence

- Fresh Management DeviceInfo serial, effective firmware, version qualifier, applet version,
  transport, form factor, FIPS flag and reset-blocked capability mask. DeviceInfo's version
  qualifier can replace its effective version on beta hardware; the capture retains both.
- PIV GET VERSION bytes, public session version, and PIV serial on applicable firmware.
- PIN/PUK retry totals, remaining retries and default flags where metadata is available.
  Legacy firmware produces an explicit unsupported result; baseline sends no VERIFY,
  including the vendor's no-data retry fallback.
- Actual management-key algorithm ID, default flag and touch policy. These flags describe
  metadata; authentication remains a separate post-reset observation.
- Every asymmetric slot: `9A`, `9C`, `9D`, `9E`, `82`–`95`, and `F9`. Each has independent
  key-metadata and certificate outcomes, including empty slots. Public-key SPKI hashes,
  algorithm, PIN/touch policy and origin accompany present keys; present certificates get
  DER and SPKI hashes. Raw metadata retains origin IDs even when the vendor's generated
  boolean loses that distinction.
- CHUID, CCC and key-history existence, length, empty state and SHA-256. The capture avoids
  rendering their personal fields or certificate subject/issuer/extension strings.
- Public ADMIN flags: blocked-PUK, stored/protected management key, derived-key presence and
  timestamp presence. Salt and timestamp values stay out of JSON. PRINTED (`5FC109`) and
  the protected management secret are excluded from all reads. Protected-object existence
  itself remains unobserved; the capture labels the public flag's provenance.
- APDU headers, response lengths and raw status words. PIV metadata/version/serial response
  bytes are public and retained as hex. Object and certificate response payloads stay out
  of the transport trace. Vendor logging is disabled before hardware access.

The helper uses the public `ScardSmartCardConnection` adapter directly for the selected
reader. This keeps the vendor convenience opener's local agent-termination recovery out
of the capture. The real connection context owns disposal.

### Development-firmware metadata fallback

The public APIs remain the first path. If an installed public metadata API raises
`NotSupportedError`, the helper permits a direct public `protocol.send_apdu` GET METADATA
only when PIV reports the exact development sentinel `0.0.1` and fresh Management DeviceInfo
reports firmware at least `5.3.0`. The fallback follows the pinned installed encodings and
records its reference, reason, both versions and source lines. GET SERIAL has the analogous
sentinel fallback for device firmware at least `5.0.3`.

The helper leaves the session version and vendor global override untouched. Other firmware
versions keep the public API's unsupported result. APDU errors and parsing failures retain
their distinct outcomes. Expected absence is operation-specific: metadata `6A88`, object
or certificate `6A82`; unsupported instructions `6D00`/`6A81` stay separate from absence.
Unexpected status words produce failures, with partial evidence retained.

## Proposed authorized cleanup scope

Baseline delivery comes first. This revision exposes only baseline/final modes; the
coordinator can capture state before a separately prepared cleanup implementation exists.
The following defines the safe scope for that implementation, rather than a runnable
cleanup command in this revision.

1. A future `cleanup` mode requires both `--serial 103` and
   `--authorize-reset-serial 103`, the exact selected reader, and the successful baseline
   file. Check both serials, live identity, reviewed source, baseline identity and captured
   F9 state before mutation. Existing dedicated-key authorization covers routine reset and
   configuration; these arguments bind the destructive action to the intended key.
2. Read the current public state and retain same-key crypto controls before cleanup. Finish
   cancellation/recovery observations in fresh sessions before resetting, so reset does
   not conceal an outstanding task or a recovery failure.
3. Check DeviceInfo reset-blocked capabilities and Bio metadata first. Installed vendor
   `reset()` rejects configured biometrics, then deliberately blocks PIN and PUK before
   issuing RESET. A failure during this sequence can leave either retry counter blocked.
   Preserve the exact failure stage and public readback; use an explicit subsequent cleanup
   invocation instead of silently retrying destructive work.
4. Preserve F9's existing private key, certificate and policy. The vendor documents that
   PIV reset retains the attestation key/certificate. The helper never imports, generates,
   moves, deletes or changes policy in F9. Overwriting the factory attestation key loses
   material that a reset cannot restore. Capture F9 DER/SPKI and metadata hashes before and
   after reset, and retain an independent attestation-signature oracle for possession when
   the coordinator has a suitable generated slot. Hash equality establishes identity;
   the oracle separately establishes continued private-key use.
5. After successful reset, reopen a fresh connection, re-establish identity, and read actual
   management metadata. Select its returned algorithm for factory-key authentication.
   AES192 is the coordinator's current observation; readback after reset decides the actual
   default variant. If metadata is unavailable on older firmware, a source-verified,
   firmware-qualified fallback needs its own explicit record. This revision offers none.
6. Factory PIN verification and management-key authentication belong exclusively after the
   explicitly authorized successful reset. Log only operation identity, algorithm and
   status. Keep PIN, PUK, management-key bytes and authentication payloads out of traces.
   PUK default metadata supplies the public post-reset check; any active PUK check must be
   separately documented as a mutation and bounded to the authorized cleanup path.
7. Fresh-session final capture verifies default/retry flags, actual management algorithm,
   public data objects and every slot/certificate. Compare with the baseline and an explicit
   expected-default disposition. Existing ordinary private keys are non-restorable unless
   their import source was separately retained; a reset restores factory applet state,
   while F9 remains the preserved exception. Record that distinction before test mutation.

Touch and hotplug operations stay coordinated with the human. Irreversible fixed
attestation policy and F9 private-key replacement are outside coverage scope. The historical
PR745 “83+1” hardware claim remains historical evidence and supplies no fresh baseline.
Compiler probes and integration-harness selection belong to the coordinator's tooling
assignment; this preparation changes no product, test or harness files.

## Source and preparation status

`reference-source/installed-manifest.json` records installed paths, SHA-256 and exact API
signatures. The adjacent snapshots preserve complete reviewed modules and license headers.
`reference-source/review-notes.md` maps consequential behavior to source and vendor guidance.
This worker inspected source and prepared files without device access, builds, tests,
formatters, commits or delegation. Hardware results remain coordinator-owned.
