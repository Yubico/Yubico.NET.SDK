# OATH-004 setup and cross-document coverage

Read-only Sol investigation, baseline `ddf3cdc` plus validated OATH-001/002 edits. No SDK/test/hardware execution by the investigator. Public feed/index responses were inspected, not installed or consumer-built.

## Local units

- Module CLAUDE1–113 and package README1–25 complete; README1–191 read with setup1–59/constraints156–165/example-related176–191 investigated.
- OathSession1–145 and858–873: setup/state/disposal. IYubiKeyExtensions1–100 read, creation24–65 investigated. IOathBackend1–27/OathBackend1–71 complete.
- InfoCommand1–31, Program1–387, DeviceSelector1–188, OathDeviceSelector1–48, OutputHelpers1–50, ResetCommand1–52 complete (reset command is also OATH-003 evidence).
- Test CLAUDE1–12, integration helper1–67 complete. Unit OathSessionTests1–235/554–734. Integration OathSessionTests1–351 read with setup/fixture comments investigated. HashAlgorithmTests setup24–33 checked in1–176 read. Other operation/creation/authentication comments are covered by prior/parallel units.
- Library/example/unit/integration project metadata read for runner/reference/package claims.

## All 23 external reference files

Only OATH-relevant surrounding units were audited; unrelated claims remain outside pilot scope.

| File | Read ranges | Disposition |
| --- | --- | --- |
| 2026-09-21-yubikit-async-boundaries-ISA.md | 1–43,322–333,669–674,1377–1414,1447–1474 | OATH route supported; historical census/status qualified |
| CLAUDE.md | 1–635 | OATH description/wrapper command supported |
| README.md | 1–87 | Package row supported; F06 legacy docs link |
| docs/NATIVE-AOT.md | 1–164 | Link-verified versus unexercised protocol status correctly qualified |
| docs/TESTING.md | 1–565 | OATH-specific filtered command matches named tests |
| docs/architecture/applet-public-api.md | 1–106 | Ownership/output exception supported; F05 |
| docs/architecture/connection-ownership-and-contention.md | 57–76 | Sequential borrowed-session example supported |
| docs/architecture/physical-device-model.md | 153–239 | Ownership supported; F03 options/preference |
| docs/architecture/sdk-architecture-diagrams.md | 16–188,317–382 | OATH/Core edge and SmartCard route supported |
| docs/docs-inventory-report.md | 1–81 | Historical snapshot retained |
| docs/migration/v1-to-v2-changelog.md | 1–20,25–31,43–50,103–137,159–173,180–188,204–210 | Dated restoration/naming/prompting history consistent |
| docs/migration/v1-to-v2-gaps.md | 1–82,152–196,275–314,346–387 | Old missing-feature claims explicitly historical |
| docs/migration/v1-to-v2.md | 1–20,64–152,397–440,545–583,613–620,645–659,681–689 | Current factory/options/collections recipe supported; existing auth/creation followups still apply |
| docs/usage/user-interaction.md | 36–87,161–189 | OATH identifiers and known-touch policy supported |
| docs/v1-to-v2-comparison.md | 1–90,121–150,161–178 | Ownership/restoration/naming narrative supported; not fresh hardware parity proof |
| docs/v2-highlights.md | 1–24,88–104,138–153 | OATH package/presence inclusion supported |
| src/Core/README.md | 272–284 | OATH related link resolves |
| src/Management/CLAUDE.md | 13–32,661–698 | OATH capability examples supported |
| src/Management/README.md | 63–94,127–151,168–175 | Capability/link supported; F03 preference table |
| src/Tests.Shared/CLAUDE.md | 1–45,194–217,270–277 | Test-key warning appropriate; F02 obsolete factory argument |
| src/Tests.Shared/README.md | 183–204 | F02 duplicate obsolete example |
| src/YubiOtp/PACKAGE_README.md | 1–25 | Slot/app distinction clear |
| src/YubiOtp/README.md | 1–9,170–178 | Same distinction and cross-links supported |

## Supported units and limits

SmartCard includes USB CCID and NFC. Setup sequence, owned/borrowed connection distinction, readiness publication, SEND REMAINING0xA5, fresh-session InfoCommand state, test helper reset/disposal, package metadata and current touch/SHA512 qualifications are supported. Failure cleanup does not guarantee every failed physical exchange leaves a reusable connection. Historical docs remain historical.

Astra independently checked help/reset dispatch, positional option parsing, SELECT metadata defaults, helper output scope, the legacy documentation destination, shared selection prose, and actual Core SCP gates. It also checked the exact primary SCP URLs supplied by the investigator.

SCP gate provenance, NEO applet-versus-physical-firmware shorthand, malformed SELECT behavior, hardware compatibility, consumer installation, and full generated-reference rendering remain unverified. CRUD/calculation contracts belong to OATH-003. Previously recorded IMP/DEC issues remain open.
