# Astra YubiOTP boundary sample verdicts

Read all 28 pinned units in `read-boundary-YubiOtp.md`, including complete test bodies and the
current NDEF command/payload construction. Final current-range and quota reconciliation remain
pending. No hardware, touch, OTP calculation or configuration operation was executed.

| Unit suffix | Verdict and scope |
| --- | --- |
| `eb4d4df0bb3c6233` | Supported: `options.Prefix` is the command's content argument; null disables, distinct from no URI compression match. URI overhead is one byte and English text overhead three in the 54-byte area. No record readback claimed. |
| `53f86b1ba80074a2` | Supported for noninteractive selection: ordered transport preference and stderr console routing are explicit. Interactive selection remains interactive. |
| `dddf82740266d31e` | Supported: substantive human/JSON output formatting summary. |
| `4a803dcc4d6bd607` | Supported with preserved legacy/manual/tagged-personalization qualifications; setters do not claim full firmware validation. |
| `dccbd1f9dd9d13eb` | Supported: raw identity/key layout, input copies and separate caller cleanup are distinguished from verifier operations. |
| `544cb91fcb796dfc` | Supported: session-scoped status output consumes GetConfigState; no new persisted metadata read promised. |
| `1d9a1e2ddc9a4a96` | Supported: dispatcher and exact exception-to-exit mapping; cancellation returns zero by current code, not an inferred rollback. |
| `a97df01d328e9157` | Supported: static logger initialization declaration; no provider-lifetime guarantee inferred. |
| `5fa8e291ce0e50fa` | Supported: source NDEF feature descriptor is 3.0.0, distinct from NFC hardware suitability. |
| `f02eac5f2126a9c4` | Supported: fragment scopes owned challenge and successfully returned response; an existing session is explicitly borrowed. No unseen intermediate-copy erasure claim. |
| `6a99ceaa75ce028b` | Supported within retained legacy ticket evidence: configuration/session and acquired arrays are scoped, verifier provisioning and freshness/CRC policy remain application responsibilities. |
| `bd29ead0f592421f` | Supported as an unexecuted physical-test definition: explicit empty-slot prerequisite, acknowledged ownership before deletion, independent HMAC comparison and single notification assertions. No current touch qualification. |
| `864bf94e1240f012` | Supported: explicit length-only comment limits the known-key test's oracle; cleanup starts after configuration succeeds. |
| `45ba1288acf4038a` | Supported within successful-path assertions: caller-created connection is independently scoped; configured/deleted status is checked. Registry-lifetime rationale is not proof of permanent leakage or physical teardown. |
| `c3dfa3ac3526a31a` | Supported: full result plus retained CRC reconstruction checks the returned bytes, beyond endpoint assertions. |
| `805d70e10d397c4c` | Supported: helper copies report data and composes response-pending/sequence bits. |
| `d64fbe6746b8ec24` | Supported: throwing-prompt fixture declaration. |
| `a323bdc19346c4b1` | Supported: fake available-connection forwarding. |
| `e79d924e62408464` | Supported: three encoded scan-code bytes and shift flag are asserted. |
| `1bebb9d5f7f0877b` | Supported: invalid initialization clears the exact injected owned scan-code array. |
| `c34204e2d27aaf15` | Supported: fake status payload includes the stated six data bytes and SW9000. |
| `3843b52721516f81` | Supported: throwing-prompt fixture declaration. |
| `5c54ff22c326b0b3` | Supported: fake records the request then cancels the optional token source. |
| `b429d29004cc107f` | Supported: synthetic handle/close callback is a fake, not a live descriptor. |
| `84ede3c0f0fbbe78` | Supported: SmartCard touch-policy notification fields, object identity and Completed outcome are asserted. |
| `a8517209eac277b9` | Supported: scripted TimeoutException maps to Failed, not the presence TimedOut classification. |
| `0356e60bb431d88f` | Supported: probe inherits ApplicationSession. |
| `159b455a45df887f` | Supported: version and read-only status view are carried by value; no deep-copy ownership claim. |

All IDs use prefix `core-001:unit:`. No new material documentation miss was found in this packet.
