# Astra OATH boundary sample verdicts

Read all 18 complete pinned units in `read-boundary-Oath.md`. These are source-content verdicts;
final current-range mapping, edited overlap and quota reconciliation remain separate. No device
operation was executed. Historical OATH implementation resolutions retain their recorded scope.

| Unit suffix | Verdict and scope |
| --- | --- |
| `1bf9d5c2d8ded722` | Supported: delete-by-name workflow owns the returned session, confirms unless forced, and excludes cancellation from the ordinary error conversion. |
| `ab98f777e0e5a2e5` | Supported: delegates to the shared Base32 decoder; returned independent bytes are caller-owned. Decoder permissiveness is separate from OATH nonempty-secret validation. |
| `1da1115a6a2d8624` | Supported: empty key TLV is sent before the local protection flag changes; no transaction rollback or device-state readback promised. |
| `054ed96fe3237098` | Supported: cancellation observed after opening disposes the returned session before throwing; cleanup failure can supersede cancellation. |
| `d8dd51ea527f6add` | Supported: exact backend forwarding preserves command, throw-on-error and token arguments. |
| `1b7bc942bb2b181f` | Supported within source scope: SmartCard path, effective-version gates, creation pattern and individual-versus-bulk presence behavior retain their qualifications. |
| `bd9fbdc6508adac7` | Supported: current URI task guidance distinguishes parsing bounds, ignored fields, permissive unused Base32 bits and 14-byte storage padding from entropy. Example scopes credential and session. Firmware statement remains vendor-profile evidence, not a new physical result. |
| `55b07354d429a6be` | Supported: test asserts a non-null six-digit result for one stored SHA-512 credential; it does not independently verify the computed OTP against a reference. |
| `61c7599de7ec9de1` | Supported: fake checks preceding connection disposal and records the later cleanup token. |
| `d4ac23565d565892` | Supported: fake distinguishes operation-token cancellation and later cleanup connections; no physical token-state recovery inferred. |
| `74a5f93a6223fb56` | Supported: scripted cancellation occurs after the inner SET CODE response. |
| `adb4dcb90b9853b3` | Supported: explicit serial/version assertions describe the selected historical device lane; no current device baseline inferred. |
| `a9579efc62584f84` | Supported: SCP03 session test checks metadata presence and salt; no additional authentication/reset guarantee inferred. |
| `039b97880290cbc2` | Supported: two non-touch TOTP entries yield count/type/non-null six-digit assertions, not an independently calculated OTP oracle. |
| `d7db7354164023a4` | Supported: cleanup visits captured command arrays and named reflected byte arrays before disposing the fake; no all-copy erasure claim. |
| `9ab2dca35fa7c063` | Supported: recorded presence-result tuple declaration. |
| `b4961ad59e422291` | Supported: cancellation propagates with exactly one connection attempt and no waiting prompt start in the fake. |
| `868041ae1b1ae700` | Supported: initialization value carries version and array references; declaration makes no independent-owner or deep-immutability promise. |

All IDs have the prefix `core-001:unit:`. No material documentation miss was found in these units.
