# Coordinator probe review

Astra read the complete prepared source before execution: HardwareProbeTests.cs (248 lines),
ProbeScope.cs (208), SymmetricOracle.cs (177), and probe-plan.md. Unique host builds warning-free
through toolchain; actual hardware outcomes are recorded separately.

The independent CMAC engine checks all four RFC 4493 vectors before device binding. Its key derivation
uses explicit vendor mapping, separate K-ENC/K-MAC and constant-time comparison of all three keys.
Valid laboratory cryptogram and omitted-cryptogram calculations exercise distinct applet paths.
One deliberate wrong password tests 8→7→8 credential retries; user/admin change attempts have distinct
credentials and actor-specific status checks. A blocked change produces an explicit skipped lane after
cleanup; successful return must also pass a new-password three-key comparison.

Every lane uses a serial-103 guard and explicit SmartCard sessions; authorized reset precedes the body.
Fresh reset/session readback in finally checks empty LIST and management retries eight. Operation tasks
are awaited through completion before session disposal. Cooperative budgets and the process-level limit
are distinct; a host termination would require independent coordinator cleanup.

Touch success requires a real calculation, independent key comparison, one PolicyRequires request and
one Completed resolution, correlated by context. Cancellation allows an admission opportunity after
the policy callback and records native admission as unobserved. Caller cancellation versus device 6985
are separately classified with their actual terminal notification; fresh-session recovery occurs before
any reset. These checks establish observed public behavior on this key rather than native internals.

Owned fixture/oracle/session-key material is cleared/disposed; outputs contain metadata/statuses/results.
The probe omits exhaustive lockout/deletion, management wrong-key transition, GET_CHALLENGE wrong-password,
derived credential KDF and natural timeout lanes to keep the scope proportional. Source/vendor evidence
supports those public explanations; any unresolved hardware dimension stays explicit in the handoff.

## v2 isolation after the first executed probe

Original v1 failed specifically on the supplied-valid-cryptogram response (56 bytes versus the SDK's
48-byte parser), after the omission branch matched all keys. Its source is frozen in the v1 input
snapshots and its failure remains HSM-IMP-008. For v2, five optional-card arguments are omitted so that
retry restoration, password transitions and presence/recovery can be evaluated independently. All
three key comparisons and cleanup assertions remain. The change manifest binds original/current hashes.
This scopes v2 to applet KDF behavior with laboratory context; authenticated HSM operation remains open.
