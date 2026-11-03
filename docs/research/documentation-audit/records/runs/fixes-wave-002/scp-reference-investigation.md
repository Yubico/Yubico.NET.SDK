# SCP03 initialization reference investigation

## Source-confirmed comparison

- C# `Scp03KeyParameters.Default` uses `KeyReference.Default`: KID1/KVNFF (`Scp03KeyParameters.cs:45`, `KeyReference.cs:34`).
- C# `ScpState.Scp03.cs:50–55` builds INITIALIZE UPDATE as CLA80, INS50, P1=KVN, P2=00, with an eight-byte generated host challenge.
- Pinned Python `yubikit/core/smartcard/scp.py:175–177` defaults to KID1/KVN00; lines255–258 build the same header except P1=00. P2 agrees; the suspected P2 discrepancy is ruled out by source.
- Thus the default request headers differ only in key-version selection: C# `80 50 FF 00`, Python `80 50 00 00` (excluding length/data/Le).
- Official Yubico legacy documentation identifies FF as the factory SCP03 key-set version and states it is removed when the first custom key set is installed: https://docs.yubico.com/yesdk/users-manual/application-security-domain/security-domain-keys.html and https://docs.yubico.com/yesdk/users-manual/sdk-programming-guide/secure-channel-protocol.html . This corroborates an intentional factory-key reference; it does not establish what is installed on serial125.

## Current limits and next experiment

- Previous6A88 happened during INITIALIZE UPDATE, before external authentication or OATH RESET. No successful reset under SCP has been demonstrated.
- A different P1 does not itself establish a bug in C# Default or availability of matching secret keys. Do not globally replace FF with00 on this source comparison alone.
- Next bounded device experiment: compare INITIALIZE UPDATE acceptance forFF and00 on serial125, without EXTERNAL AUTHENTICATE, RESET, key writes, or logging cryptograms. A successful response can identify the selected KVN and distinguish reference lookup from authentication failure; it cannot certify a complete secure channel.
- Read-only inventory on resumption confirms only125/5.8.0.alpha.2 attached. Independent reviewers have no hardware access.

## Probe preflight changed

The probe worker subsequently observed four connected devices (103/5.8.0.beta.0,9681620/5.4.3,125/5.8.0.alpha.2,31683481/5.7.4). It stopped before test/build execution under its explicit125-only operation scope. No INITIALIZE UPDATE probe observations exist. The new probe source is uncommitted and uncompiled; evidence `/tmp/opencode/oath-scp-reference-probe-evidence/` records this limitation. Hardware ownership is released. No conclusion aboutFF/00 device acceptance follows.

## Python reset behavior — requested by user

Pinned Python revision4ca60f7 allows continued use of the same OathSession and physical SmartCardConnection after successful reset:

1. `oath.py:273` creates one SmartCardProtocol over the caller's connection; `:283` retains the supplied SCP parameters in `_scp_params`.
2. `oath.py:316–324` sends RESET, reselects OATH, reinitializes SCP with those retained parameters if present, then updates password state and device ID.
3. `core/smartcard/__init__.py:450–458` rebuilds the base APDU processor before SELECT; `:471–508` builds fresh SCP state/processor and performs the handshake. The same `self.connection` is used throughout (`:331,337–348`). Reset never invokes `close()`.

Thus Python preserves the session/connection objects but replaces secure-channel state. Its implementation is not precedent for reusing obsolete derived SCP state. The caller does not pass fresh parameters at reset because the session retained them at creation. C# currently documents initialization-only borrowing; matching Python transparently would require an explicit changed lifetime contract for disposable/opaque key objects.

User is leaning toward terminal reset followed by caller close/reopen, but requested this Python comparison before deciding. No new reset API policy is approved yet. Source inspection establishes intended successful flow, not complete failure-atomicity or live SCP verification.

**Decision update:** User subsequently approved always-terminal OATH reset for both plain/SCP sessions. See `reset-decision.md`. This closes the design choice, not live SCP verification.

## Completed bounded probe

After clarifying that ordinary read-only discovery is allowed while test APDUs must bind only125, the worker ran the probe through the shared harness. Each version used a fresh caller-owned connection and raw session:

| Header | Result | Response length |
| --- | --- | --- |
| SELECT OATH `00 A4 04 00` |9000|15|
| INITIALIZE UPDATE `80 50 FF 00` |6A88|0|
| SELECT OATH `00 A4 04 00` |9000|15|
| INITIALIZE UPDATE `80 50 00 00` |6A88|0|

Serial125 bound successfully; SDK reports5.8.0, physical inventory5.8.0.alpha.2. Older9681620 was excluded during discovery by the existing unrecovered-connection guard. Probe build passed warning-free; the single diagnostic test **failed** its acceptance assertions after collecting both rows,0skips/0warnings. Do not describe this as passing integration evidence. No EXTERNAL AUTHENTICATE, RESET, key writes, or other probe values were used.

Conclusion: changingFFto00 does not solve this observed OATH-selected failure. No change to Default is justified; device-wide key inventory and exact failure cause remain unproven. The bounded investigation stops here under the documentation-first scope. Deeper SCP investigation belongs to a separate implementation/debugging pass unless needed for a separately approved task.

User follow-up: the failure could be specific to this YubiKey. In the deferred pass, repeat the bounded initialization check on different SCP-capable YubiKeys and record serial, firmware, key reference, and result for each. Current observations concern only serial125 and must not be generalized across devices. No additional hardware testing is assigned now.

Durable evidence: `scp-probe-evidence/`, including actual step log at `probe-toolchain/runs/20261103-124643-513-test/01-test-Oath.IntegrationTests.log`. Probe source remains uncommitted in its isolated diagnostic worktree; it is not part of integration. Hardware ownership released.
