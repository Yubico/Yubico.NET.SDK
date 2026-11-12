# Coordinator-only bounded verification proposal

All proposals below are **unrun**. This packet prepared records only. Use existing
validation foundation and current test/build skills if the coordinator authorizes
execution; preserve actual failures and nonzero executed counts. No test assertions
or production fixes are authorized here.

1. **Wire grammar / empty SCP:** capture short/extended empty and data-bearing
   commands with API Le 0, 255, 256, 65536; compare exact byte layouts and complete
   payload ceilings. Independently derive the inspected SCP03 empty-command
   counter/MAC expectation without adding a ciphertext block. Compare current
   wire, not only successful fake callback completion. Include external-authenticate
   absence-of-Le discrepancy as a separate observation.
2. **Response chaining oracle:** define an independently produced protected response
   split inside a ciphertext block, inside the trailing R-MAC, and at whole blocks.
   Compare the actual C# graph with its declared target profile. Include status-only
   6A82, bare61xx, signed9000 and custom remaining instruction. Distinguish number of
   commands/state advances from full wire validity. Existing fake's signed error
   oracle is not sufficient. Run no hardware until exact target/firmware and recovery
   plan are approved.
3. **Failure boundaries:** fake a first plain chained-fragment transmission that
   records receipt then throws; observe local recovery refusal or reuse, without
   claiming physical delivery. Separately inject failure after counter increment
   during IV/encryption through an appropriate managed seam, with no native crypto
   probe. Check first-failure identity, no replay, next-call admission and disposal
   independently. Test later raw INS82 bare9000 to distinguish handshake-phase gating.
4. **Cleanup:** retain explicit formatted/transport/response/decrypted-temporary
   owners and observe each separately after success/failure, including response
   continuation failure. Do not equate disposal invocation with byte erasure or
   use casts to write through read-only consumer views. Existing source owners may
   require an approved observation seam; do not broaden the harness as a shortcut.
5. **Parameter/profile shape:** compile KeyReference with-clone/default/equality
   consumers and record properties versus actual AsMemory bytes. Check concrete
   parameter containment disposal separately from caller source clearing. Synthetic
   SCP11 vectors should cover a/b/c key roles, optional OCE-reference default,
   non-P256 admission, malformed point prefix/width, receipt widths and conditionally
   admitted extension TLVs. Never mutate borrowed key/certificate storage as a consumer workaround.
6. **Fixture reachability:** run the exact TestGetSharedSecret test with its current
   32-byte static scalar; distinguish its length-only assertion from independent
   agreement correctness and keep the original assertion unchanged. Compile meaningful secure-channel/credential examples with explicit
   typed inputs and complete parameter/session/finally scopes, and classify secure
   storage loader fragments as application-dependent.
7. **Hardware qualification later:** one approved isolated device/firmware/transport
   lane per SCP03/11a/11b/11c and short/extended profile. Record trusted-key/certificate
   provisioning and initial state, actual command reached, expected actor/authority,
   success/failure/terminal/reopen and independently observed restoration. Plain reset
   success never closes an unreached SCP RESET. Preserve PIV physical deferral and
   Windows readiness gate. No native/provider experiment belongs to this investigator.

The generic Core suite's historical counts (including integration's 4,442 unit and
197 resilience executions) retain their original scope; they are zero new B6 executions.
