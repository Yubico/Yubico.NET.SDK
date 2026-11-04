# Coordinator integration review

Astra read all six integration classes (49 methods), the 75-line local session helper and runner JSON
before execution. Shared harness instructions/README/allow list were read narrowly for device execution;
serial 103 is already authorized. No shared implementation or Core investigation was needed.

All 49 methods request reset-before. Four RSA-related methods return before reset/callback on firmware
4.2.0–4.3.5; actual serial-103 metadata is 5.8.0.beta.0, outside that branch. Two larger-RSA generation
methods have Slow traits. No method requests physical-touch coordination; the sole UIF mutation sets
the signature slot On and reads it back, without a subsequent cryptographic operation in that method.
Separate real-operation probes will cover presence rather than infer it from policy readback.

The helper resets using one session, disposes it, then creates the callback session on the borrowed
connection; callback/session cleanup closes resources without post-test applet reset. Persistent effects
include changed PINs/reset code/retries/KDF, generated/imported keys, cardholder certificates and UIF.
Tests restore selected values on their successful paths; final coordinator cleanup is mandatory.

The KDF case will be isolated from the other 48 methods. It sets a KDF object with distinct salts and
initial hashes, then changes user/admin PINs through that session before ordinary signing-mode VERIFY.
Its comments about a raw old PIN are hypotheses for investigation; current implementation and actual
device outcomes establish what executes. Both failed and successful assertions will retain exact scope.

Independent host checks already present: RSA/P256/P384 imported-key signature verification, imported
P256 authentication verification, RSA plaintext recovery and P256 ECDH raw-secret equality. Ed25519
and X25519 import tests compare public points with RFC vectors. Other generate/sign/attest cases often
check only metadata or nonempty outputs; those remain narrower evidence. The attestation replacement
test compares stored certificates before/after and rejects Admin-only authorization, without verifying
the certificate chain/signature. Neither test replaces the attestation private key.

Execution order: complete vendor-client baseline including attestation public identity, run 48 non-KDF
methods (including slow cases), explicit independent cleanup/readback, isolated KDF case after focused
triage, then bounded supplements and human-ready touch batch. Reconcile each failure before broadening
probes. State and source evidence remain versioned; no shipping runtime/assertion correction is included.

## Supplemental probe review

Astra read all three probe source files (HardwareProbeTests 283 lines, CancellationProbe 89,
ProbeSupport 193), the vendor-state helper and per-case runner. Warning-free v1 build and immediate
Csc capture/retention passed all 227 dependency identities and 22 source/config/assets snapshots.
All seven cases explicitly bind serial 103 and SmartCard; external vendor cleanup runs after the
test process completes, captures fresh pre-reset state and preserves both test and cleanup exits.

KDF probe tests distinct logical salt identities, using ordinary-PIN host-verified signing as control
before extended VERIFY. Initial hashes use the SDK KDF, so this is a credential-identity/transition
probe rather than an independent KDF algorithm oracle. Retry probe compares actual User/Reset counters
and typed exception. Touch probes verify generated-key signatures, known plaintext or certificate
signature/SPKI binding. The attestation check trusts the preserved issuer for that comparison, without
claiming an independently validated manufacturer-root chain. On policy is used; no fixed UIF or
attestation private-key replacement is planned.

Cancellation awaits the native-backed operation task to completion, records exception/status/prompt
outcome, then opens a fresh connection and host-verifies a recovery signature before reset. Its scope
is observed terminal behavior and recovery, rather than guaranteed cancellation latency or causal
proof of native interruption. Per-case budgets and the process watchdog are distinct. Failed test
expectations remain failed even if independent cleanup succeeds.
