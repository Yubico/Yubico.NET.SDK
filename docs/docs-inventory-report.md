# Active Documentation Inventory

> Report-only snapshot. This file does not make staleness claims by itself; it lists signals for maintainer review.

- Generated: `2026-07-02T15:03:27Z`
- Active-doc boundary source: `dotnet toolchain.cs -- docs-list-active`
- Active documentation files: `54`
- Excluded by docs-qa boundary: `docs/archive/**`, `docs/completed/**`, `docs/plans/**`, `docs/research/**`, `docs/reviews/**`, `docs/specs/**`, `docs/templates/**`

## Summary

| Lane | Count |
| --- | ---: |
| architecture | 4 |
| module | 29 |
| other | 2 |
| root | 4 |
| top-level-docs | 13 |
| troubleshooting | 1 |
| usage | 1 |

## Inventory

| File | Lane | Lines | Last commit | Age (days) | Signals |
| --- | --- | ---: | --- | ---: | --- |
| `AGENTS.md` | root | 63 | 87bba114 (2026-07-01) | 1 | none |
| `CLAUDE.md` | root | 588 | 87bba114 (2026-07-01) | 1 | none |
| `docs/AI-DOCS-GUIDE.md` | top-level-docs | 520 | 3b44f755 (2026-06-07) | 25 | none |
| `docs/architecture/architecture-docs-automation.md` | architecture | 77 | 90b918d3 (2026-07-02) | 0 | architecture lane |
| `docs/architecture/event-driven-device-discovery.md` | architecture | 337 | 1fb0ac4e (2026-02-09) | 142 | architecture lane |
| `docs/architecture/physical-device-model.md` | architecture | 191 | db41b6a9 (2026-06-23) | 8 | architecture lane |
| `docs/architecture/sdk-architecture-diagrams.md` | architecture | 406 | 5ecf1266 (2026-07-02) | 0 | architecture lane |
| `docs/COMMIT_GUIDELINES.md` | top-level-docs | 66 | 184ae24e (2026-01-17) | 166 | none |
| `docs/CRYPTO-APIS.md` | top-level-docs | 81 | d4b95038 (2026-04-27) | 66 | none |
| `docs/CSHARP-PATTERNS.md` | top-level-docs | 274 | d4b95038 (2026-04-27) | 66 | none |
| `docs/DEV-GUIDE.md` | top-level-docs | 32 | 594dd849 (2026-04-20) | 72 | none |
| `docs/docs-inventory-report.md` | top-level-docs | 79 | 7dddc1a3 (2026-07-02) | 0 | none |
| `docs/linux-setup.md` | top-level-docs | 93 | 767dae35 (2026-01-16) | 167 | none |
| `docs/live-documentation-governance.md` | top-level-docs | 47 | 5d285e66 (2026-07-02) | 0 | none |
| `docs/LOGGING.md` | top-level-docs | 258 | 752b34ef (2026-06-15) | 17 | none |
| `docs/MEMORY-MANAGEMENT.md` | top-level-docs | 204 | d4b95038 (2026-04-27) | 66 | none |
| `docs/SDK-HOUSE-STYLE.md` | top-level-docs | 230 | 44d61a9b (2026-06-05) | 26 | none |
| `docs/TESTING.md` | top-level-docs | 482 | ab8d9364 (2026-06-07) | 25 | none |
| `docs/TESTING_PLATFORM_FINDINGS.md` | top-level-docs | 128 | 631e3b07 (2026-06-05) | 27 | none |
| `docs/troubleshooting/testdiscovery-findings.md` | troubleshooting | 271 | 2e6b1fbf (2026-01-20) | 163 | none |
| `docs/usage/device-discovery.md` | usage | 205 | 752b34ef (2026-06-15) | 17 | none |
| `GEMINI.md` | other | 5 | f6d1416c (2026-01-18) | 165 | none |
| `README.md` | root | 123 | 3b44f755 (2026-06-07) | 25 | none |
| `src/Core/CLAUDE.md` | module | 377 | 2a4c67bc (2026-06-17) | 15 | none |
| `src/Core/README.md` | module | 277 | 752b34ef (2026-06-15) | 17 | none |
| `src/Core/tests/CLAUDE.md` | module | 90 | 752b34ef (2026-06-15) | 17 | none |
| `src/Fido2/CLAUDE.md` | module | 398 | 476d9769 (2026-07-02) | 0 | none |
| `src/Fido2/examples/FidoTool/README.md` | module | 149 | 4bf40a10 (2026-06-06) | 26 | none |
| `src/Fido2/README.md` | module | 370 | f1e1f4fe (2026-06-11) | 21 | none |
| `src/Fido2/tests/CLAUDE.md` | module | 159 | 3b44f755 (2026-06-07) | 25 | none |
| `src/Management/CLAUDE.md` | module | 899 | 752b34ef (2026-06-15) | 17 | none |
| `src/Management/examples/ManagementTool/README.md` | module | 80 | a20afb55 (2026-06-06) | 26 | none |
| `src/Management/README.md` | module | 392 | 752b34ef (2026-06-15) | 17 | none |
| `src/Management/tests/CLAUDE.md` | module | 77 | 3b44f755 (2026-06-07) | 25 | none |
| `src/Oath/CLAUDE.md` | module | 81 | a20afb55 (2026-06-06) | 26 | none |
| `src/Oath/tests/CLAUDE.md` | module | 12 | 3b44f755 (2026-06-07) | 25 | none |
| `src/OpenPgp/CLAUDE.md` | module | 114 | a20afb55 (2026-06-06) | 26 | none |
| `src/OpenPgp/tests/CLAUDE.md` | module | 12 | 3b44f755 (2026-06-07) | 25 | none |
| `src/Piv/CLAUDE.md` | module | 164 | a20afb55 (2026-06-06) | 26 | none |
| `src/Piv/examples/PivTool/README.md` | module | 149 | a20afb55 (2026-06-06) | 26 | none |
| `src/Piv/README.md` | module | 139 | a20afb55 (2026-06-06) | 26 | none |
| `src/SecurityDomain/CLAUDE.md` | module | 329 | 4bce5858 (2026-06-05) | 26 | none |
| `src/SecurityDomain/README.md` | module | 208 | 752b34ef (2026-06-15) | 17 | none |
| `src/SecurityDomain/tests/CLAUDE.md` | module | 196 | bad01416 (2026-06-08) | 24 | none |
| `src/SecurityDomain/tests/README.md` | module | 155 | bad01416 (2026-06-08) | 24 | none |
| `src/Tests.Shared/CLAUDE.md` | module | 267 | 5092c3e2 (2026-06-08) | 24 | none |
| `src/Tests.Shared/README.md` | module | 240 | 5092c3e2 (2026-06-08) | 24 | none |
| `src/WebAuthn/CLAUDE.md` | module | 310 | 476d9769 (2026-07-02) | 0 | none |
| `src/YubiHsm/CLAUDE.md` | module | 140 | a20afb55 (2026-06-06) | 26 | none |
| `src/YubiHsm/tests/CLAUDE.md` | module | 12 | 3b44f755 (2026-06-07) | 25 | none |
| `src/YubiOtp/CLAUDE.md` | module | 125 | 24f1c4b8 (2026-04-02) | 91 | none |
| `src/YubiOtp/tests/CLAUDE.md` | module | 12 | 3b44f755 (2026-06-07) | 25 | none |
| `SWIFT_WEBAUTHN_CLIENT_EXPLORATION.md` | other | 802 | 46e52334 (2026-04-22) | 70 | none |
| `TOOLCHAIN.md` | root | 221 | 5d285e66 (2026-07-02) | 0 | none |
