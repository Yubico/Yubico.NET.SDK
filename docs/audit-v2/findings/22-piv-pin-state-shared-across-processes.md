# 22 PIV PIN state is shared across connections and processes (YESDK-1630)

| | |
| --- | --- |
| Audit severity | MED |
| Our verdict | Confirmed, with corrections. The attack also works while the victim's session is still open, and in-process. On macOS, sequential separate processes did not reproduce. |
| Our severity | MED. It needs a local process that can open the same reader, and a PIN ONCE key. TOUCH CACHED was not tested. |
| Root cause | Device behaviour: the PIV PIN state is card-wide. The SDK's `SHARED` open and `LEAVE_CARD` release decide how long it stays usable. Python shares the concurrent exposure in its shared fallback, but not the post-release one. |
| Fix group | C (policy decision). Decision 1 in the verification report. |
| Evidence | Hardware (both test keys, cross-process and in-process; poll variant on the 5.7.4 test key). Static (SDK source, pyscard, specs). |
| Since the audit | Structure changed, behaviour did not. The native layer moved to `PcscNativeResources.cs` and `PcscConnectionNativeState.cs`. It still opens `SHARED` by default and releases with `LEAVE_CARD`. The public `BeginTransaction` and `BeginTransactionAsync` also use `LEAVE_CARD`. The disposition is only a parameter on internal overloads. `VerifyPinAsync` is unchanged. |

## What the audit says

The audit says the "one live connection" protection is process-local, and that the handle settings let another process use the victim's authorization:

> "The SDK's "one live connection" protection is process-local, but it opens PC/SC handles using `SCARD_SHARE_SHARED` and closes them with `LEAVE_CARD`. An attacker process can open the genuine YubiKey before the victim, without knowing the PIN."

The audit also says the attacker can still sign after the victim disposes. It suggests exclusive mode plus reset on disposal.

Abbreviations: PIV is Personal Identity Verification, the smart-card application. PIN is personal identification number. PUK is PIN unblocking key. PC/SC is the Personal Computer/Smart Card API. SCard is the PC/SC function prefix. SP is NIST Special Publication. NIST is the National Institute of Standards and Technology. PIN ONCE and TOUCH CACHED are YubiKey PIV policy names.

## What is right

- The SDK opens handles shared by default and releases them with `LEAVE_CARD`:
  - Shared unless the compatibility switch is set ([UsbSmartCardConnection.cs#L127-L130](../../../src/Core/src/Transports/SmartCard/UsbSmartCardConnection.cs#L127-L130)), then connect ([L132-L138](../../../src/Core/src/Transports/SmartCard/UsbSmartCardConnection.cs#L132-L138)).
  - The card handle releases with `LEAVE_CARD` ([SCardCardHandle.cs#L35-L38](../../../src/Core/src/Native/Desktop/SCard/SCardCardHandle.cs#L35-L38)).
- The "one live connection" guard is process-local. The Core design notes say: "In-process only — cross-process contention is not covered." ([src/Core/CLAUDE.md#L439](../../../src/Core/CLAUDE.md#L439)).
- The PIV PIN is scoped to the application in the card. SP 800-73-5 Part 2 §2.4.2 lists the PIV PIN among the "application security status indicators". Those are set to FALSE when another application is selected. See [Specification](#specification).
- The hardware record confirms the concurrent attack on both test keys. An attacker that opened its session before the victim verified signed without the PIN after the victim disposed and exited. The signature verified against the slot 9A public key ([hardware-results.md](../evidence/hardware-results.md), item 3).
- The poll variant shows the attacker can act during the victim's session. On the 5.7.4 test key, the attacker signed about 1 s after the victim verified. The victim was still holding its session open for 15 s (item 6).
- The in-process integration test shows the state survives disposal of the earlier SDK sessions and their PC/SC contexts. It also survives a fresh SELECT of the PIV application (item 5).
- The `OpenSmartCardHandlesExclusively` switch does block a connected attacker. The victim's connect fails with a sharing violation, and the attacker's later signature is rejected with `6982` (item 4). The switch exists ([CoreCompatSwitches.cs#L21-L27](../../../src/Core/src/CoreCompatSwitches.cs#L21-L27)).

## What is wrong or imprecise

- "After disposal" understates the problem. The attacker does not need the victim to finish. A reset on disposal alone does not close the window during the session (item 6).
- The audit's suggested switch is not a complete fix. Only the attacker-first ordering was tested: the victim could not connect. Per the PC/SC share-mode definition (`SCARD_SHARE_EXCLUSIVE`, quoted in [Specification](#specification)), exclusive access also keeps a later ordinary connection out while the victim holds the card. We did not test that ordering. Post-release authorization reuse was not tested with exclusive mode on.
- The hardware record's explanation is not supported. Item 2 says the card is "powered down/reset by macOS PC/SC stack when last handle closes". Item 5 contradicts that. In one process, the SDK released every handle and context, and the PIN state was still usable. Process exit clears the state, but in-process disposal does not. The mechanism is unknown, so do not rely on a power-down explanation.
- Sequential processes are not a mitigation. Only macOS was tested. Microsoft documents that an application that exits without calling `SCardDisconnect` causes the card to reset ([SCardDisconnect remarks](https://learn.microsoft.com/en-us/windows/win32/api/winscard/nf-winscard-scarddisconnect)). Windows and Linux behaviour for this case is not known.
- The SDK's contract does not say the state is card-wide. `VerifyPinAsync` says "Verify PIN to enable PIN-protected operations." ([IPivSession.cs#L95](../../../src/Piv/src/IPivSession.cs#L95)). The SDK tracks management-key authentication per session ([PivSession.cs#L54](../../../src/Piv/src/PivSession.cs#L54)). Nothing tracks PIN verification. A per-session flag would not match card-wide state anyway.
- Yubico's policy wording does not define its scope. The technical manual says PIN policy 02 means "require PIN once per session" ([YubiKey Technical Manual](https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-apps-piv.html), PIV metadata). It does not define "session". The hardware shows the verified state outlives an SDK session and a fresh SELECT.
- Python does not fully avoid this. ykman tries exclusive access first, then falls back to shared. The fallback has the same concurrent exposure. Python's stronger post-release behaviour comes from powering the card down on close. See [Canonical Python reference](#canonical-python-reference).
- Not tested, so not established:
  - TOUCH CACHED keys. The audit says they are exploitable during the touch cache.
  - Windows and Linux.
  - PIN ALWAYS keys. The Yubico docs say the PIN is required for every operation.
  - Whether a second handle can run management-key operations after another handle authenticated. SP 800-73-5 §2.4.2 lists the PIV Card Application Administration Key with the PIN among the application security status indicators. If the management-key state is shared the same way, #22 covers more than signing.

## Why it matters

- Who can trigger it: a local process that can open the same PC/SC reader. Nothing here is remote.
- Preconditions:
  - The victim verifies the PIN on a PIN ONCE key.
  - The attacker holds an open handle before the victim verifies, or uses the card while the victim's session is open.
  - Sequential use after the victim exits did not reproduce on macOS.
- Impact: the attacker signs with a registered, non-exportable key without the PIN. The key is not extracted. But the attacker gets signatures. That is enough to defeat a login or signing flow that relies on the PIN gate.
- Exclusive mode turns the attack into a denial of service for the victim. The victim cannot connect while the attacker holds the reader.
- PIN ALWAYS keys are not affected in the same way, because the device asks for the PIN each time (Yubico docs; not tested here).

## Specification

Quotes below are verbatim. We checked each one against the cached text or the cited page.

- NIST SP 800-73-5 Part 2 (July 2024), §2.4.2 (Security Status). Source: [NIST SP 800-73-5 Part 2](https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-73pt2-5.pdf).
  > "A security status indicator is said to be an application security status indicator if it is set to FALSE when the currently selected application changes from one application to another."

  > "The security status indicators associated with the PIV Card Application PIN, the PIN Unblocking Key (PUK), OCC, pairing code, and the PIV Card Application Administration Key are application security status indicators for the PIV Card Application, whereas the security status indicator associated with the Global PIN is a global security status indicator."

- NIST SP 800-73-5 Part 2, §3.1.1 (SELECT command). Same source.
  > "If the currently selected application is the PIV Card Application when the SELECT command is given and the AID in the data field of the SELECT command is either the AID of the PIV Card Application or the right-truncated version thereof, then the PIV Card Application SHALL continue to be the currently selected card application, and the setting of all security status indicators in the PIV Card Application SHALL be unchanged."

  > "... and all the PIV Card Application security status indicators in the PIV Card Application SHALL be set to FALSE." (the case where a different application is selected)

- YubiKey Technical Manual, "Smart Card - PIV Compatible Specifics", slot 9a. Source: [YubiKey Technical Manual](https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-apps-piv.html).
  > "To perform any private key operations, the end user PIN is required. Once the correct PIN has been provided, multiple private key operations may be performed without additional cardholder consent."

- YubiKey Technical Manual, PIV metadata, PIN policy values:
  > "02 - require PIN once per session"

- Microsoft `SCardDisconnect`. Source: [SCardDisconnect function](https://learn.microsoft.com/en-us/windows/win32/api/winscard/nf-winscard-scarddisconnect).
  - `SCARD_LEAVE_CARD`: "Do not do anything special."
  - `SCARD_RESET_CARD`: "Reset the card."
  - `SCARD_UNPOWER_CARD`: "Power down the card."
  - Remarks: "If an application (which previously called SCardConnect) exits without calling SCardDisconnect, the card is automatically reset."

- Microsoft `SCardConnect`, `dwShareMode`. Source: [SCardConnect function](https://learn.microsoft.com/en-us/windows/win32/api/winscard/nf-winscard-scardconnecta).
  - `SCARD_SHARE_SHARED`: "This application is willing to share the card with other applications."
  - `SCARD_SHARE_EXCLUSIVE`: "This application is not willing to share the card with other applications."

- pcsc-lite API, `SCardDisconnect` (version 2.4.1 docs). Source: [pcsc-lite API](https://pcsclite.apdu.fr/api/group__API.html).
  - `SCARD_LEAVE_CARD`: "Do nothing."
  - `SCARD_RESET_CARD`: "Reset the card (warm reset)."
  - `SCARD_UNPOWER_CARD`: "Power down the card (cold reset)."

- SDK contract. The compatibility switch says: "If set to true, Yubico.Core will attempt to open smart card handles exclusively. False will open shared. Default is false / shared." ([CoreCompatSwitches.cs#L23-L26](../../../src/Core/src/CoreCompatSwitches.cs#L23-L26)).

Disposition summary:

| Disposition | Meaning (Microsoft and pcsc-lite) | SDK v2 (`df1ec06d`) | SDK v1 (legacy) | pyscard 2.3.1 (used by ykman) |
| --- | --- | --- | --- | --- |
| `LEAVE_CARD` | Do nothing | Release ([PcscNativeResources.cs#L106](https://github.com/Yubico/Yubico.NET.SDK/blob/df1ec06d1a03c18c0b697ad424a40e4b0f9b46d1/src/Core/src/Transports/SmartCard/PcscNativeResources.cs#L106)). Also the end disposition of public transactions ([UsbSmartCardConnection.cs#L103-L104](https://github.com/Yubico/Yubico.NET.SDK/blob/df1ec06d1a03c18c0b697ad424a40e4b0f9b46d1/src/Core/src/Transports/SmartCard/UsbSmartCardConnection.cs#L103-L104)) | Not the default | Used once, when the exclusive path releases its shared handle ([ExclusiveConnectCardConnection.py#L59](https://github.com/LudovicRousseau/pyscard/blob/2.3.1/src/smartcard/ExclusiveConnectCardConnection.py#L59)) |
| `RESET_CARD` | Warm reset | Not used | Default on release ([SCardCardHandle.cs#L27](https://github.com/Yubico/Yubico.NET.SDK/blob/941874e91a77616f5d7063f2952a0291c7e7c8f8/Yubico.Core/src/Yubico/PlatformInterop/Desktop/SCard/SCardCardHandle.cs#L27)) | Default for `reconnect()` only |
| `UNPOWER_CARD` | Cold reset (power down) | Not used | Not used | Default on `connect()`, stored, and used on `disconnect()` ([PCSCCardConnection.py#L154-L156](https://github.com/LudovicRousseau/pyscard/blob/2.3.1/src/smartcard/pcsc/PCSCCardConnection.py#L154-L156), [L257](https://github.com/LudovicRousseau/pyscard/blob/2.3.1/src/smartcard/pcsc/PCSCCardConnection.py#L257)) |

Note: the table reflects the code at the cited lines. The SDK v2 column is the current yubikit at `df1ec06d`.

## Canonical Python reference

Python has the same issue in its shared fallback (candidate for the divergence ledger). It limits the post-release window by powering the card down on close, which the SDK does not do.

- ykman tries an exclusive connection first, then a shared one ([ykman/pcsc/__init__.py#L130-L150](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/ykman/pcsc/__init__.py#L130-L150)):
  ```python
  if os.environ.get(_YKMAN_NO_EXCLUSIVE) is None:
      excl_connection = ExclusiveConnectCardConnection(connection)
      try:
          scard_conn = ScardSmartCardConnection(excl_connection)
          ...
  # Try a shared connection
  return ScardSmartCardConnection(connection)
  ```
- Closing calls `disconnect()` with no disposition ([ykman/pcsc/__init__.py#L91-L93](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/ykman/pcsc/__init__.py#L91-L93)).
- pyscard 2.3.1 defaults. `connect()` uses shared mode unless told otherwise. It stores `SCARD_UNPOWER_CARD` as the disposition, and `disconnect()` uses it ([PCSCCardConnection.py#L137-L157](https://github.com/LudovicRousseau/pyscard/blob/2.3.1/src/smartcard/pcsc/PCSCCardConnection.py#L137-L157), [L244-L257](https://github.com/LudovicRousseau/pyscard/blob/2.3.1/src/smartcard/pcsc/PCSCCardConnection.py#L244-L257)).
- The exclusive path is not atomic. It connects shared, releases that handle with `SCARD_LEAVE_CARD`, then connects with `SCARD_SHARE_EXCLUSIVE` ([ExclusiveConnectCardConnection.py#L46-L76](https://github.com/LudovicRousseau/pyscard/blob/2.3.1/src/smartcard/ExclusiveConnectCardConnection.py#L46-L76)). The stored disposition stays `UNPOWER`.
- In short: exclusive when ykman can get it, otherwise shared. Both paths power the card down on close.
- Deliberate or not: the exclusive-first behaviour looks deliberate. The commits are "Use exclusive card access" (`20d94d72`, 2025-02-26) and "Fallback to non-exclusive connection if needed" (`60bd2f2c`, 2025-03-17). The commit messages do not mention PIN state. The `UNPOWER` behaviour comes from pyscard's default, not from ykman code.
- Pinned dependency: the pinned SHA's `uv.lock` pins pyscard 2.3.1 ([uv.lock#L675-L677](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/uv.lock#L675-L677)). `pyproject.toml` only constrains it to `>=2.0, <3`.

## Sibling SDKs (context only)

- Legacy .NET SDK v1 defaults to shared and uses the same exclusive switch ([DesktopSmartCardDevice.cs#L134-L140](https://github.com/Yubico/Yubico.NET.SDK/blob/941874e91a77616f5d7063f2952a0291c7e7c8f8/Yubico.Core/src/Yubico/Core/Devices/SmartCard/DesktopSmartCardDevice.cs#L134-L140)). It releases with `RESET_CARD`. That should close the post-release window, if a warm reset clears the PIN state. We did not test that. It does not close the concurrent window.
- yubikit-android desktop holds `beginExclusive()` for the whole connection, and calls `disconnect(true)` on close ([PcscSmartCardConnection.java#L37-L73](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/desktop/src/main/java/com/yubico/yubikit/desktop/pcsc/PcscSmartCardConnection.java#L37-L73)). That is exclusive plus reset for the session. It is the strictest option, and it blocks other PC/SC clients while open.

## Reproduction

- Hardware. The source is [hardware-results.md](../evidence/hardware-results.md). This page uses that record. The per-area report ([E.md](../evidence/agent-reports/E.md)) lists these runs as not executed. The hardware record supersedes it.
  - Test keys: 5.7.4 and 5.8.0. Setup: P-256 in slot 9A, PIN ONCE, TOUCH NEVER.
  - Item 2, sequential processes on macOS: an attacker signs without the PIN and gets `6982`. A victim process verifies and exits. A new process still gets `6982`. Fast sequential runs (about 0.6 s) and runs with a 5 s gap also gave `6982`. This did not reproduce.
  - Item 3, concurrent attacker, 5.7.4 and 5.8.0: the attacker opened its session first. The victim verified and exited. The attacker signed without the PIN, and the signature verified against the slot 9A public key.
  - Item 4, exclusive switch: the victim fails to connect with a sharing violation. The attacker's later signature is rejected with `6982`.
  - Item 5, in-process: the integration test [YESDK1630_PinOnceStateSurvivesConnectionDisposal](../../../src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/AuditV2/PivAuditHardwareReproTests.cs#L24-L70) uses three SDK sessions in one process. In the recorded run it failed with "Unverified handle produced a signature without PIN (verifies against slot key: True)." The record does not say which test key ran it. The same item shows the state survives a fresh SELECT of the PIV application.
  - Item 6, poll variant, 5.7.4 only: the attacker polled once per second. The victim verified and kept its session open for 15 s. The attacker signed about 1 s after the verify, while the victim was still open.
  - Not run: TOUCH CACHED, Windows, Linux, management-key scope, and exclusive mode with reset on release.
- Unit: none. A fake transport cannot model card state.
- Integration repro: [PivAuditHardwareReproTests.cs#L24-L70](../../../src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/AuditV2/PivAuditHardwareReproTests.cs#L24-L70). It asserts a security expectation: a handle that never verified the PIN cannot sign after another handle's session has ended. No cited specification clause requires that. It failed in the recorded run.
- Cross-process script: [app.cs](../evidence/scripts/Yesdk1630CrossProcess/app.cs), with modes `setup`, `hold`, `verify`, `verify-wait`, `poll`, and `sign`.

## Proposed fix

- Recommendation (group C). Ship the documentation first, then add an opt-in isolation policy. Keep the 2.0 default unchanged unless the maintainer chooses option (c).
  1. Document now. State that the PIN state is card-wide, that `VerifyPinAsync` does not bind it to the SDK session, and that other local processes can use PIN ONCE keys while the session is open, and after it closes until the card resets. Put this on `IPivSession.VerifyPinAsync` and on the PIV facade.
  2. Opt-in policy. A session option that holds exclusive access, or a PC/SC transaction, from before `VerifyPinAsync` until the last private-key operation. On release, end with `RESET_CARD` (or `UNPOWER_CARD`). Surface acquisition failure as a specific exception. The transport already has the hook: `BeginTransaction(SCARD_DISPOSITION endDisposition, ...)` ([UsbSmartCardConnection.cs#L106-L112](https://github.com/Yubico/Yubico.NET.SDK/blob/df1ec06d1a03c18c0b697ad424a40e4b0f9b46d1/src/Core/src/Transports/SmartCard/UsbSmartCardConnection.cs#L106-L112) at `df1ec06d`).
  3. Keep the disposition explicit. Shutdown with an active transaction ends it with `LEAVE_CARD` ([PcscConnectionNativeState.cs#L146-L147](https://github.com/Yubico/Yubico.NET.SDK/blob/df1ec06d1a03c18c0b697ad424a40e4b0f9b46d1/src/Core/src/Transports/SmartCard/PcscConnectionNativeState.cs#L146-L147), [L350-L351](https://github.com/Yubico/Yubico.NET.SDK/blob/df1ec06d1a03c18c0b697ad424a40e4b0f9b46d1/src/Core/src/Transports/SmartCard/PcscConnectionNativeState.cs#L350-L351)). The policy must end its transaction before shutdown. Otherwise the reset is lost.
- Options considered:
  - (a) Document only. Pros: no code change and no breaking change. It can ship now. Cons: the attack stays open for anyone who does not opt in, and users get no tool. Keep it as step 1.
  - (b) Opt-in isolation policy (recommended). Pros: additive, and the default is unchanged. It closes both windows for users who choose it. Cons: while active, other PC/SC clients are blocked (exclusive), or their commands are blocked (transaction). Reset disrupts other state on the card. A victim that crashes never sends the disposition. macOS behaviour for RESET and for transaction blocking is not established. On Windows, a transaction with no card operations for more than five seconds resets the card ([SCardBeginTransaction remarks](https://learn.microsoft.com/en-us/windows/win32/api/winscard/nf-winscard-scardbegintransaction)). That reset may also discard the verified PIN state. We did not test this. Sub-choice: exclusive is simpler. A transaction permits other handles to remain connected, but blocks their commands throughout the protected sequence ([pcsc-lite API, SCardBeginTransaction](https://pcsclite.apdu.fr/api/group__API.html)). Acquire it before PIN verification and retain it through the final protected operation and reset. Releasing it between those operations reopens the exposure. The verification report leaves the choice open.
  - (c) Python-style default: exclusive, with power-off on close. Pros: the strongest default, and closest to ykman's intent. Cons: breaking for every application that needs concurrent PC/SC access, such as gpg-agent, browsers, and OS smart-card services. ykman's own shared fallback shows exclusivity can fail. It still needs cross-platform tests.
- Why (b): it is the only option that closes both windows without changing the default. (c) breaks coexistence for everyone. (a) alone leaves the attack open. The Android desktop design uses exclusive plus reset for the whole session. For PC/SC, that is the same trade-off as (c).
- API impact: (a) none. (b) additive, a new option or session parameter. (c) breaking, because the default changes.
- Proving test:
  - Hardware on macOS, Windows, and Linux. With the policy on, a second handle cannot sign during the victim's session (it is rejected or cannot connect). After release with reset, the second handle gets `6982`. Acquisition failure raises the new exception.
  - Keep `YESDK1630_PinOnceStateSurvivesConnectionDisposal` as a regression test for the selected isolation and release policy, not as proof of a PIV specification violation. If isolation stays opt-in, enable that policy in the test. It failed in the recorded run.
- Depends on / interacts with: #26e (same documentation surface), management-key scope (open question), and the transaction shutdown behaviour described above.
- Open questions:
  1. Which option for 2.0: (a), (b), or (c)? The verification report recommends (b).
  2. For (b), exclusive or transaction?
  3. Should management-key operations get the same policy? Not tested yet.
  4. What does "session" mean in the PIN policy wording? The SDK docs need a definition that matches the card.

## Check it yourself

- Read the spec sections: NIST SP 800-73-5 Part 2 §2.4.2 and §3.1.1 (link in [Specification](#specification)).
- Read the disposition definitions: [Microsoft SCardDisconnect](https://learn.microsoft.com/en-us/windows/win32/api/winscard/nf-winscard-scarddisconnect) and [pcsc-lite SCardDisconnect](https://pcsclite.apdu.fr/api/group__API.html).
- Read the Python path: [ykman/pcsc/__init__.py#L130-L150](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/ykman/pcsc/__init__.py#L130-L150), [pyscard PCSCCardConnection.py#L137-L157](https://github.com/LudovicRousseau/pyscard/blob/2.3.1/src/smartcard/pcsc/PCSCCardConnection.py#L137-L157), and [ExclusiveConnectCardConnection.py#L46-L76](https://github.com/LudovicRousseau/pyscard/blob/2.3.1/src/smartcard/ExclusiveConnectCardConnection.py#L46-L76).
- In-process hardware repro (test keys only; it resets the PIV application):
  ```bash
  dotnet toolchain.cs -- test --integration --project Piv.IntegrationTests --filter "FullyQualifiedName~AuditV2"
  ```
  This also runs the #24 hardware repro.
- Cross-process repro. Each mode runs as its own process. Scenario 1 is the concurrent attacker with default shared handles. Scenario 2 is the exclusive switch with the attacker connected first. Each scenario starts with `setup`, which resets PIV. Start the attacker, then wait for its `waiting for victim` line before the victim runs. If the attacker's log shows an error instead, stop and read it, because the wait loop would never end.
  ```bash
  cd docs/audit-v2/evidence/scripts/Yesdk1630CrossProcess

  # Scenario 1: concurrent attacker, default shared handles
  dotnet run app.cs -- <serial> setup                        # reset PIV; P-256 in 9A, PIN ONCE, TOUCH NEVER
  dotnet run app.cs -- <serial> hold > attacker-1.log 2>&1 &  # attacker: opens PIV, no PIN, waits for the go file
  until grep -q "waiting for victim" attacker-1.log; do sleep 1; done
  dotnet run app.cs -- <serial> verify                       # victim: verifies PIN, disposes, exits
  touch "$TMPDIR/yesdk1630-<serial>.go"; wait                # attacker signs without PIN (succeeded in the recorded run)
  grep -E "SIGNED WITHOUT PIN|rejected" attacker-1.log

  # Scenario 2: exclusive switch, attacker connected first
  dotnet run app.cs -- <serial> setup
  dotnet run app.cs -- <serial> hold > attacker-2.log 2>&1 &
  until grep -q "waiting for victim" attacker-2.log; do sleep 1; done
  EXCL=1 dotnet run app.cs -- <serial> verify                # expected: victim cannot connect (sharing violation)
  touch "$TMPDIR/yesdk1630-<serial>.go"; wait                # expected: attacker's signature is rejected (6982)
  grep -E "SIGNED WITHOUT PIN|rejected" attacker-2.log
  ```
  Replace `<serial>` with the test key's serial number.
