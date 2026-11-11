# Pinned Python context for Core and boundary investigators

## Identity and evidence role

The remote `HEAD` and `main` both resolved to
`4ca60f706af930459138d8dc0f0f953480e1c7a4` on November 11, 2026. The existing
`/home/rupicola/Code/y/yubikey-manager` checkout is clean at that revision and shallow.
Its project version is `5.9.3-dev.0`. This is a fresh check of the same revision used by
earlier OATH/PIV work, rather than a new revision silently substituted into that evidence.
The installed 5.9.2 artifact and its earlier capture remain separate inputs. No Python
package was imported, and no CLI, tests, crypto computations, native calls or devices ran.

[manifest.json](manifest.json) records source hashes and immutable URL construction. The
`snapshot/` directory retains selected original Core source/tests and package/NEWS context.
Other listed files are available by `git show <pin>:<path>` in the clean local checkout or
the immutable URL. Their hash inventory establishes identity, not semantic audit coverage.

Use this sibling SDK to understand implementation and reference policy. Standards remain
independent authorities; matching Python behavior establishes neither conformance nor fresh
firmware/native qualification. Python immutable `bytes` ownership also differs substantially
from this SDK's clearable array/Memory owners.

## Source and assertion context actually read

| Pinned source | Read scope | Useful, bounded observation |
| --- | --- | --- |
| `yubikit/core/smartcard/__init__.py` | Complete 508-line file | Short/extended formatting, command/response chaining, touch workaround, reconfiguration, SELECT reset of processor state and SCP processor installation. `SmartCardProtocol.close` closes its connection, unlike C#'s borrowed-session ownership. Do not copy that lifetime contract. |
| `yubikit/core/smartcard/scp.py` | Complete 375-line file | Static/session keys use immutable byte tuples; SCP11 parameters borrow cryptography key objects and leaf-last certificate sequence. HKDF/X9.63 calls and fixed counter/chain calculations are implementation comparators. No GlobalPlatform conformance claim follows from the file's comments. |
| `yubikit/core/__init__.py` | Lines 240–423 | Development-version patching, integer conversions and complete TLV/OID helpers. Python TLV parses continuation tag octets, supports indefinite lengths, and verifies exact top-level item length. These are comparison points, rather than a requirement to broaden C#'s bounded smart-card codec. |
| `tests/test_core.py` | Complete 101-line file | OID vectors, including **legacy OpenPGP** 25519 OIDs; TLV encoding/parse/length vectors and invalid tag fixture. These legacy OIDs must stay out of Core RFC 8410 key metadata. |
| `tests/device/test_scp.py` | Complete 50-line file | Firmware 5.7.2 fixture with default SCP11b key/certificate availability gate; ECHO payload sizes 10/255/256/512/2048 over default, force-short and no-config profiles. Source inspection supplies no new executions. |

The source logs plaintext SCP request/response data at its `TRAFFIC` level. That behavior
is specific to Python's logging policy; it supplies no safe-redaction assurance for C#.
Likewise Python's APDU response chaining loop is not a proof of C# cancellation/drain or
bounded-response handling.

## Bounded history and firmware-policy origins

The first research pass queried GitHub's commit API at the pinned revision for exactly
four paths: `yubikit/core/smartcard/scp.py`, `yubikit/core/smartcard/__init__.py`,
`yubikit/oath.py`, and `yubikit/management.py`. Raw responses are in `history/`.
The Management response reached the selected 50-result bound; it is a useful index,
not complete Management history. Renames also limit path-local history. No deep clone,
fetch into the shared checkout, or assumption that shallow blame proves origins occurred.

The initial retained patch set is hashed in `history/patch-manifest.json`. One focused
follow-up fetched the three adjacent OATH-changing commits listed in
`history/focused-followup-manifest.json`. That follow-up located the floor addition.
These records keep exact commit identities; all dates below are patch author metadata.
Publication dates remain unestablished. Parent changes can have dates later than their
children, so chronological prose must not be inferred from dates alone.

| Commit | Patch context inspected | Result for later packets |
| --- | --- | --- |
| `a60ae832f895cfbffc2bb602b41cd6ba20cb2a0e` (2024-05-09) | Complete OATH diff | Adds SCP parameters to the OATH constructor; this patch itself has no 5.6.3 floor. |
| `f44c6d2e8ea70e88e2f00dcfcfe729a155f9c4d7` (2024-05-27) | Complete OATH diff | Retains SCP parameters for reset, reselects OATH, reinitializes SCP, and limits the NEO unlock workaround to plain sessions. This is implementation history, not earliest physical reset support. |
| `1b87964ba258e3ce39fd2959888dfe7cf81a6843` (2024-06-03) | Complete OATH diff | Adds rejection for `5.0.0 <= version < 5.6.3`. The patch establishes the source-policy introduction but gives no hardware/workaround rationale. Preserve inherited `DEC-007` and its source-run identity. |
| `04c8cda4b7c23686f840490203e5c515aa2fa61f` (2024-05-30) | Complete OATH diff | Replaces touch-workaround configuration with protocol configure; parent blob matches the preceding floor-containing file despite the author-date order. |
| `652e5825a2ecea8f9b4e3c55da6bf7b802594263` (2025-05-21) | Complete SmartCard diff and patch rationale | Adds short-APDU SCP and says NFC readers often lack extended support. Preserves whole-message MAC calculation across command chaining. B6 should independently compare exact wire behavior. |
| `e55c9c2f37c48b0c6d7eaa5eff7794d58adf9b8c` (2025-05-22) | Complete OATH diff; patch summary | Moves protocol configure before SCP setup in OATH. The complete large formatter patch is retained for B6's own read; its full diff has not been semantically audited here. |
| `c910e5d25ba675ac2bfc97d35f14a4cc0314864f` (2025-04-22) | Complete Core version-helper and OATH diffs | Moves override application into parsing for exactly version 0.0.1. This is not equivalent to a general C# major-zero permissive gate. A/boundary owners must retain raw/effective version provenance. |
| `13f8a21c34e2a8ef483c155d08eb730e7fa59fe9` (2026-05-04) | Complete patch | Treats `TAG_MORE_DATA` as remaining page count, with an explicit future-firmware rationale. A owns the comparison with C# DeviceInfoReader; C supplies no whole-A disposition. |

The retained `8706337...`, `7fd50da...`, and `a10d3b3...` patches provide SCP03
refactor, SCP11b prototype and SCP rename/refactor context. Their headers/summaries were
read to locate history; their complete wire changes remain for B6. Earlier PIV origin
patches remain available unchanged under the frozen PIV run and need not be downloaded again.

## Bounded gaps and handoff

- The OATH floor's source origin is located; its firmware rationale remains a precise
  vendor/maintainer question. This research used its initial pass and one focused follow-up.
- Short-APDU support has a retained implementation rationale. It does not settle every
  reader/firmware's physical support or GlobalPlatform profile requirements.
- No default branch movement, release installation, dependency update, native behavior,
  certificate trust policy or protocol support was inferred from this pin.
- A and B1 may use the pin now. They should read their own exact source/assertion ranges
  and record independent conclusions rather than inheriting Packet C's read scope.
