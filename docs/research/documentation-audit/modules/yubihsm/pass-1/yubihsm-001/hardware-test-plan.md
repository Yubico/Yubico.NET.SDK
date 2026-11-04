# YubiKey-only hardware verification plan

Source-inspected by Sol, 2026-11-04; **no execution by investigator**. Coordinator owns baseline,
serialized test/probe execution and final cleanup. User authorizes all YubiKey-only tests/probes and
mutations on the dedicated connected test key. Respect the existing allow-list boundary and coordinate
physical touch when an actual CALCULATE ceremony calls for it. No HSM2 or connector is used.

## Baseline and acceptance

1. Retain read-only identity: serial, OS/transport, product firmware and reported development status,
   applet version, available/enabled interfaces, LIST labels/algorithms/touch/retries and management retries.
   Bind SDK session observations to that same serial; a metadata filter is not applet qualification.
2. Record pre-mutation state without exposing secret values. Existing tests immediately reset the applet,
   so baseline must precede every test batch. Authorization already covers this destructive change.
3. Use repository `domain-test` skill and toolchain, never direct test runner commands. Save raw logs
   before retention rotation; record executed/failed/skipped counts and exact failed stage/status.
4. Reject false green: zero matched tests, all-skipped runs, wrong serial/transport, setup-only failure,
   feature-gate rejection and swallowed exceptions do not count as executed ceremony proof. A skipped
   5.8 test is firmware-inapplicable; a selected test failing INS0B/6D00 is **failed**, not silently skipped.

All13 have one SmartCard WithYubiKey filter, no InlineData, and resetBeforeUse:true. Runner configuration
disables assembly/collection parallelism and caps threads at1. Helper: open connection, create temporary
borrowed resetSession, RESET and re-SELECT, dispose resetSession, create body session over same connection,
await action, dispose session/connection. It has **no post-body reset**, no persistent-state finally, and
passes SCP parameters only to body creation. Test bodies have separate 30second tokens whose sources
are not disposed. Local data finally blocks clear selected keys/context; these do not erase applet state.

## All13 applicable-test matrix

Names are exact method filters; prepend the class namespace
`Yubico.YubiKit.YubiHsm.IntegrationTests.` for a FullyQualifiedName match. Default state after each setup:
empty applet, all-zero16byte management key, management retries8. Password `password` is test data.
Success leaves the following persistent state; a failure may leave partial state and needs final reset.

| # / class | Method | Device filter minimum | Body/assertion and successful residual state |
|---|---|---|---|
| 1 Session | ResetAsync_ThenList_ReturnsEmptyList | 5.4.3 | LIST empty. No credentials; default management key. RESET is in helper, not a second body call. |
| 2 Session | PutSymmetric_ListDeleteList_RoundTrip | 5.4.3 | PUT AES credential, LIST label/algorithm, DELETE then LIST empty. Default management key; random host keys cleared in finally. |
| 3 Session | PutDerived_VerifyAlgorithm_IsAes128 | 5.4.3 | Derive/store from `my-derivation-password`, LIST one AES credential. Leaves `test-credential`, password `password`, touchfalse. This proves metadata, not derived-key correctness. |
| 4 Session | PutManagementKey_RoundTrip_CanUseNewKey | 5.4.3 | Rotate default to fixed bytes `01..08 || 01..08`, PUT with new key, LIST one. Leaves **rotated management key**, `test-credential` password `password`; test clears host copy but never restores applet key. |
| 5 Session | GetManagementKeyRetries_AfterReset_Returns8 | 5.4.3 | Reads/asserts8. Empty applet/default key. No failure/success retry transition exercised. |
| 6 Session | CalculateSessionKeysSymmetric_Returns48Bytes | 5.4.3 | Store AES, random16byte context, CALCULATE without cryptogram, assert3×16bytes. Leaves AES `test-credential` password `password`, touchfalse. No HSM authentication oracle. |
| 7 Session | GenerateAsymmetric_GetPublicKey_Returns65Bytes | 5.6.0 | On-device generation, LIST EC, GetPublicKey length65/prefix04. Leaves EC `test-credential`, password `password`, touchfalse. No matching HSM enrollment. |
| 8 Session | ChangeCredentialPassword_ThenCalculate_Succeeds | 5.8.0 | Store AES with `password`, CHANGE user path to `new-password`, CALCULATE with new password; checks SEnc length16. Leaves AES `test-credential`, new password **if change succeeded**; on0x6D00 leaves original password. No old-password rejection or admin path. |
| 9 Session | PutSymmetric_WithTouch_CredentialListShowsTouchRequired | 5.4.3 + UP trait | PUT touchtrue and LIST/assertflag. Leaves AES `test-credential`, password `password`, touchtrue. **No CALCULATE/touch wait**, despite RequiresUserPresence trait. |
| 10 Asymmetric | PutCredentialAsymmetric_ThenList_ShowsEcP256 | 5.6.0 | Generate host P256, import private scalar, LIST exact label/EC. Leaves `test-asymmetric`, password `password`, touchfalse; scalar cleared. Does not compare returned credential public key to imported point. |
| 11 Asymmetric | GetChallenge_WithAsymmetricCredential_ReturnsEpk | 5.6.0 | Generate EC, GET_CHALLENGE with TestPassword (sent only on effective5.7.1+), assert nonempty. Leaves `test-asymmetric`, password `password`, touchfalse; ephemeral state may persist. Does not assert65bytes/curve. |
| 12 Asymmetric | CalculateSessionKeysAsymmetric_WithDummyCryptogram_ThrowsApduException | 5.6.0 | Generate EC, obtain PK-OCE and EPK-OCE, synthesize EPK-SD; pass **PK-OCE in HSM-public-key parameter** and random8byte cryptogram. Exact ApduException/nonzeroSW is broad rejection evidence only. Leaves `test-asymmetric`, password `password`, retry state depends on response. A derived HsmAuthRetryException can fail exact Throws assertion. |
| 13 Asymmetric | PutCredentialAsymmetric_WithTouch_ShowsTouchRequired | 5.6.0 + UP trait | Host private import touchtrue, LIST/assertflag. Leaves EC `test-asymmetric`, password `password`, touchtrue. **No CALCULATE/touch wait**. |

“Session” means HsmAuthSessionTests; “Asymmetric” means HsmAuthAsymmetricTests. For production5.4.3–5.5:
seven base cases (#1–6,9) apply; for5.6–5.7:12 except#8;5.8+:13 nominally apply. Effective applet feature
gates and real instruction support may differ from device metadata on development firmware; report actual
result instead of inferring support from version text. User authorization covers#9/#13 metadata bodies;
their UP labels do not turn them into touch ceremonies. A whole-module run with no --smoke selects them.

Suggested coordinator commands after loading skill (commands planned, not run here):

```bash
dotnet toolchain.cs -- test --integration --project YubiHsm
dotnet toolchain.cs -- test --integration --project YubiHsm --filter "FullyQualifiedName~ChangeCredentialPassword_ThenCalculate_Succeeds"
```

For isolation, choose one exact method filter from the table; do not confuse Category!=RequiresUserPresence
or --smoke (11 nominal methods) with the full13. The full suite's last method/order is not guaranteed;
always normalize and read back explicitly after it, including on failure.

## Meaningful independent symmetric key proof

Useful bounded temporary probe, outside product/test changes, coordinator-owned. Retain source and output.
Use the existing authorized dedicated key, unique labels and normal SDK calls; no connector. Start reset
or use the known baseline default state. Keep every generated secret owned/cleared in finally.

1. Choose independent deterministic test-only K-ENC/K-MAC/context bytes, with distinct keys and a16byte
   context. Import AES credential with known credential password and touchfalse. Compute expected S-ENC,
   S-MAC and S-RMAC independently of HsmAuthSession/SessionKeys. S5's vendor host implementation supplies
   the oracle: AES-CMAC of `00×11 || type || 00 || L[2 big-endian] || 01 || context` where L=0080;
   type04 with K-ENC gives S-ENC, type06/07 with K-MAC give S-MAC/S-RMAC. Verify CMAC engine against an
   independent published AES-CMAC test vector before using it as oracle. Avoid calling the SDK's own
   implementation as expected output.
2. CALCULATE symmetric and constant-time compare **all48bytes**, report booleans/lengths only. A second
   distinct context and/or independently derived PBKDF2 enrollment case adds binding evidence. For the
   derived case independently compute PBKDF2-HMAC-SHA256(password, UTF8`Yubico`,10000,32), split16/16,
   then use the CMAC oracle; LIST algorithm alone is insufficient.
3. Optionally independently compute card cryptogram from expected S-MAC/type00/L=0040 and supply it
   to a separate CALCULATE; compare all keys again. A modified cryptogram should reject, but exact
   device SW and retry behavior must be recorded rather than assumed. Do not mask an unexpected response.
4. Scope outcome to applet symmetric KDF/validation on recorded firmware. Synthetic context/cryptogram
   is an isolated applet probe; it establishes no genuine HSM challenge provenance, server authentication,
   connector handshake completion or cross-platform interoperability. S5 is corroboration, not normative
   conformance evidence; GAP-SPEC-1 remains explicit.

## Retry/password probes

- Credential success reset: PUT fresh test AES credential, LIST retries8, one wrong-password CALCULATE,
  record typed exception/SW and LIST7, correct-password CALCULATE with known context, compare keys and
  LIST8. One deliberate failure is sufficient; exhaustive deletion/management lockout is unnecessary.
- Management success reset: with known current key issue exactly one wrong-key management operation,
  record retries7, perform valid management operation with current key, then read retries8. Keep the
  wrong and valid operations distinct, with enough retries left. No new mutation approval gate is added.
- GET_CHALLENGE consistency on5.7.1+: use asymmetric credential with known password; wrong supplied
  password once, record concrete exception type and SW, LIST count, then correct supplied password and
  exact65byte/point check. This investigates generic-vs-typed behavior; do not demand a typed exception
  the current code never constructs. Firmware can give further command-specific evidence.
- Password changes on effective5.8+: user change old→new, prove new CALCULATE and old rejection once;
  admin change using known management key to third password and prove new CALCULATE. At each stage record
  password state symbolically, including failures. On INS0B unsupported, record the test/probe failed at
  that stage and retain original credential state; no override may manufacture hardware capability.

## Actual touch/cancellation probes (separate from#9/#13)

Use symmetric known test keys/context to obtain a meaningful successful CALCULATE without an HSM.
Store touchtrue and supply a recording prompt through SessionCreationOptions.UserPresencePrompt.

- Human-coordinated positive ceremony: record LIST-derived PolicyRequires request, application/operation/
  scope correlation, actual physical touch, completion, exactly one Completed resolution and all48byte
  oracle equality. Request callback returns promptly; session is kept alive throughout.
- Pre-send cancellation: cancel in the prompt's successful request callback. Expect OperationCanceledException,
  Cancelled resolution and no CALCULATE; this matches existing fake ordering but can verify device-side
  retry/state readback. It proves cancellation **before send**, not an interrupted native call.
- Bounded in-flight cancel if useful: signal after prompt returned and CALCULATE is known to have started,
  withhold touch, cancel once; retain elapsed time, exception, resolution and subsequent session usability.
  Keep lifetime until call actually drains. A caller-side timeout alone must not dispose live resources
  or be reported as native cancellation. Arrange a bounded recovery/touch to release the operation if
  necessary; record whether cancellation or eventual hardware timeout ended it. Do not infer TimedOut
  notification from elapsed time alone. Human coordination is timing, not a new mutation approval gate.
- touchfalse comparison verifies silent success with a prompt supplied. Missing notification for missing
  credentials or LIST failure remains no-hardware evidence unless coordinator separately probes it.

## Final cleanup/readback — required regardless of result

After all operations have completed/drained, dispose sessions, open fresh authorized SmartCard session,
RESET applet explicitly, and read LIST empty plus management retries8. To establish default key usability
rather than infer it from empty LIST, PUT a unique test credential with all-zero management key and DELETE
it, then verify LIST empty/retries8 again (or final reset/readback). Record precise operations/results.
The management-key round-trip may be last and leaves rotation; RESET is independent of knowing that key.
Verify no operation is still pending before final reset. Readback proves YubiHSM Auth applet cleanup only;
it makes no claim about other applets, host terminal history, immutable strings or HSM state.
