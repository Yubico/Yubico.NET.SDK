# OATH-003 operation coverage

Source/test/spec reading only during investigation. Baseline is `ddf3cdc` plus validated OATH-001/002 documentation.

| Source | Semantic reading |
| --- | --- |
| OathSession.cs | 147–224, 323–628: device binding, LIST, delete, rename, full/truncated/aggregate calculations, parsing isolation, notifications, reset |
| IOathSession.cs | 62–136: public operation contracts |
| Code.cs | Entire 1–103 |
| IYubiKeyExtensions.cs | 67–98: LIST/aggregate wrappers |
| README.md / CLAUDE.md | README 1–92, 123–174; complete module guidance 1–113 |
| Unit CodeTests.cs | Entire 1–188 |
| Unit OathSessionTests.cs | 189–462, 630–689 |
| Unit AuditV2/OathAuditReproTests.cs | 26–172 |
| Integration OathSessionTests.cs | 25–139, 191–343 |
| Integration OathHashAlgorithmTests.cs | 24–175 |
| Integration AuditV2/OathAuditReproTests.cs | Entire 1–69 |
| TestExtensions/OathTestStateExtensions.cs | 20–67, reset-before-test interpretation |
| AccountsCommand.cs | 15–47, 124–269, 331–430 |
| ResetCommand.cs / OutputHelpers.cs | Entire 1–52 / 1–50 |
| Supporting OATH source | Credential incoming parsing 133–188; constants 20–60; backend 22–70 |
| Supporting Core source | TlvHelper25–69; Tlv25–89,197–258; ApplicationSession115–199; UserPresenceNotification19–205; ExchangeGuard17–140; PcscProtocol115–159; PcscProtocolScp63–109 |

Required root/module/test instructions and style guidance were read. Reading an entire file does not establish full test/spec verification for all its claims.

## Supported claims

Foreign-device guards run before transmission; truncated response shape/digit validation and decimal formatting follow the relevant RFC steps; custom periods are individually recalculated after the default-period batch; explicit touch metadata controls notifications; bulk stays silent. The callback lifecycle/cleanup rationale and individual-operation cancellation admission description are supported. Delete/reset command scope and ordinary rename metadata preservation/gate are supported. Output helper comments and pipe-oriented rationale are useful and accurate. Routine test narration was not selected for preference-only rewriting.

## Coordinator checks

Astra independently inspected full/truncated calculation, aggregate parsing/cleanup, reset sequencing, Code window construction, and public operation contracts. The original full YKOATH page already fetched in this session corroborates the digit header and response markers. The sensitivity/ownership conclusion follows the independently read ApduResponse copy boundary from OATH-001.

## Gaps

No device behavior was executed. Rename collisions/name limits, HOTP touch-marker precedence, full/truncated counter consumption on success/failure, exact algorithm vectors, post-reset refresh/SCP recovery, malformed aggregate pairing, output cleanup, and timestamp extremes remain unresolved or deferred. Setup/external claims belong to the parallel OATH-004 investigator. These gaps are not silently closed by passing existing repository unit tests.
