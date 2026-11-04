# YubiHSM Auth hardware results

## Device and scope

2026-11-04, Linux/Omarchy, serial **103**, USB-A YubiKey 5 NFC Enhanced PIN. Device metadata reports
**5.8.0.beta.0**; ykman 5.9.2 `hsmauth info` reports **5.8.0**; fresh SDK HsmAuthSession instances report
effective version **0.0.1**. Explicit SmartCard sessions and fixture serial checks bind the results.
Each version observation keeps its own meaning; inherited normalization remains HSM-DEP-001.

User approved YubiKey-only testing. Initial readback contained one asymmetric `test-asymmetric`
credential, touch off, retries eight; management retries eight. Authorized resets removed that initial
credential. Tests target the applet and host calculations; live HSM/connector interoperability remains
an outstanding verification dimension.

## Existing integration methods

`validation/integration-initial.log` and its retained full run record show **11 executed, 11 passed,
zero skipped, zero warnings**. The exact method/effect matrix is in hardware-test-plan.md. Password
change succeeded. Dummy asymmetric input passed a broad rejection assertion; its response cause and
HSM identity/handshake correctness are outside that assertion. Post-batch explicit ykman reset/LIST
confirmed empty applet state. During the human-ready batch, both UP-tagged metadata-only cases passed
(`integration-touch-metadata.log`): **all 13 existing methods passed**, zero skips/warnings. Their
stored-policy assertions are distinct from the actual CALCULATE touch probe below.

## Focused probes

Complete sources/provenance and actual compiler contexts are retained in validation/hardware-probe.
Astra read all three source files before execution. CMAC implements an independent AES-block oracle,
checking all four RFC 4493 vectors before device binding. Calculations compare all three 16-byte keys
constant-time; only operation metadata/status/comparison results are logged.

| Attempt | Result and actual assertions |
| --- | --- |
| v1 KDF/retries | **Failed.** Omitted-cryptogram calculation matched all three keys; one wrong password returned HsmAuthRetryException/63C7 and LIST seven. Correct password plus valid synthetic card cryptogram yielded 56 bytes, rejected by SessionKeys.Parse's 48-byte requirement. Retry-restoration assertion was not reached. Finally reset/fresh empty LIST/retries eight passed. |
| v2 KDF/retries | **Passed.** Cryptogram omitted for both successful calls; independent three-key matches, one typed 63C7 failure, credential LIST **8→7→8**, management retries eight, prompt silent, final cleanup passed. |
| v2 password actors | **Passed.** User and admin actors each used a fresh credential; each password-change call succeeded, and new-password calculation matched all three keys, retries eight. Credentials deleted and finally cleanup passed. |
| v2 cancellation | **Failed expectation.** Caller cancelled at 1,009 ms; operation drained at 14,308 ms with APDU 6982/Failed rather than the planned OCE/6985 alternatives. Recovery assertion was unreached; finally cleanup passed. |
| v3 measured cancellation outcome/recovery | **Passed for measured scope.** Cancelled at 1,011 ms; drained at 14,270 ms with 6982/Failed. Fresh owned session, before any reset, listed/deleted/recreated the credential and matched all three non-touch keys; final cleanup passed. |
| v3 successful touch | **Passed.** User confirmed readiness and touched serial 103; one PolicyRequires request/Completed resolution with identical context and token None, independent three-key equality, retries eight and finally cleanup. Cryptogram omitted. |

V2 isolates retry/password/presence behavior by omitting the optional cryptogram. The original
cryptogram-bearing failure remains **HSM-IMP-008** and its full original source is preserved. These
omission tests establish applet KDF results for laboratory input, while authenticated HSM operation
requires the separate real handshake and verification responsibilities documented for consumers.

## Current cleanup and limits

Each completed focused lane reset in finally, disposed and reopened a fresh session, then verified
empty LIST and management retries eight. Successful PUT using the default key after each starting
reset established default-key usability at those checkpoints. Final independent client readback and
default-key usability verification completed after the last operation using the independent installed
vendor Python client. Reset, new connection, default-key PUT/LIST/DELETE and another fresh connection
verified **zero credentials, eight management retries and usable default management key**. Direct Python
session version was **0.0.1**, agreeing with the SDK; ykman's earlier display remains separately recorded.
Final ykman device-info output matches initial serial/product/firmware/interface/application metadata.
All device jobs have drained and released their client connections.

The first cleanup helper passed an unpadded nine-byte password to Python, which requires exactly 16
bytes for byte input. It failed validation and reset in its exception handler; original source/log are
retained. Corrected explicit padding matched the documented API and completed the usability/readback
sequence. This is cleanup-helper setup correction, not a shipping SDK change or test success claim.

Public metadata readback establishes the reported applet state; the initial private credential was erased
under the approved reset scope. Laboratory secrets were fixed public test material in the vendor cleanup
helper; .NET probe-owned buffers were cleared/disposed. Host allocation erasure has its own scope.

Cancellation evidence establishes a terminal APDU error after the request and a usable fresh session.
Native admission, immediate interruption and a specific device touch-timeout cause remain unverified.
The original failed expectation is retained alongside the later measured-status recovery result.

Hardware lanes omitted for proportional scope: management wrong-key recovery, GET_CHALLENGE retry
mapping, derived-credential known answer, exhaustive deletion/lockout and natural touch timeout.
Older-firmware, NFC and other-OS qualification remain open. The existing metadata tests and host oracle
each retain their precise assertion scope.
