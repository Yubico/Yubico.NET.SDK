# F steering follow-up — GetInfo absence and ARKG partial agreement

**2026-11-04.** Fresh-editor checkout `/tmp/opencode/ctap2-001-edit-f`; baseline/HEAD remains
`5b09a9b6942ded3913b5c9340c3e126617e61a82`. Status: **prose corrected and stable for fresh F review/snippet
extraction**, not independently reviewed or compiled. No build, test, hardware, commit, delegation or
integration. Loaded review-receive and verified feedback against original evidence/source before editing.
One patch attempt was rejected for an invalid hunk before any file modification; the corrected patch
then applied. No failed compiler/test attempt or runtime operation occurred.

## COORD-001 / expanded A-REV-001

Read PRIMARY `correction-a.md` completely and independently traced baseline
`src/Ctap2/src/AuthenticatorInfo.cs`: nullable memory properties at 217/268, typed null byte-array
locals at 327/332, byte-string reads at 438–461 and direct array assignments at 507/512. Missing keys
leave the arrays null; the initializer has no explicit nullable-preserving conversion. RawData stores
the complete original response map at 478. Required missing AAGUID separately becomes empty at 481.

Independently fetched original pinned framework source on 2026-11-04:
<https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/ReadOnlyMemory.cs>.
The `ReadOnlyMemory(T[]? array)` constructor returns default when array is null; its implicit array
operator calls that constructor, and Length returns `_length`. Assigning that nonnullable default
memory to the nullable property produces a present zero-length value. Context7's focused `/dotnet/docs`
query corroborated the general value-to-nullable conversion, but did not return this exact memory-array
case; pinned source and initializer inspection, not that generic result, establish the mechanism here.
A's prior archived language-probe provenance is retained, not counted as a new executed CTAP experiment.

**Narrow corrections:**

- `src/Ctap2/README.md`, **Getting started**, paragraph immediately after the one-shot helper explanation:
  absent decoded EncIdentifier/EncCredStoreState become present empty nullable memory; HasValue is not
  wire presence/capability, and nullable presence/length does not distinguish absent versus encoded empty.
  `info.RawData` is the encoded-presence inspection path.
- `src/Ctap2/CLAUDE.md`, **GetInfo (No User Presence Required)**, paragraph immediately after the code fence:
  restrict preserved absent nulls to optional numeric/Boolean/text fields, explain the encrypted-field
  conversion and missing-required-AAGUID fallback, and direct wire-presence inspection to RawData.

The existing F guides did not explicitly promise encrypted-field null preservation, but their general
GetInfo explanations needed this local qualification after the expanded finding. Checked
`src/Ctap2/tests/CLAUDE.md`, FidoTool README, `FidoExamples/GetAuthenticatorInfo.cs` and
`Cli/Menus/InfoMenu.cs` relevant complete explanatory units. They make no blanket optional-null promise
or encrypted HasValue-based availability claim. InfoMenu displays nullable numeric/Boolean metadata,
not these encrypted fields; those different typed guards are not alleged faulty by this finding.
No extra warning churn or executable UI/guard change was made in those files. WebAuthn requirements,
shared diagrams/migration and existing callback/firmware/PIN-normalization qualifications are unchanged.

This remains the A-006/CTAP-IMP-001 decoder family, not an implemented repair or new counted storage bug.
E metadata integration HasValue assertions/guards need its owning review; these F corrections do not
validate token/decryption prerequisites or repair the missing-versus-empty distinction.

## RC-R1 — ARKG-10 seed placeholder agrees

Read PRIMARY `review-c.md` RC-R1 and its complete evidence interpretation. Independently fetched
<https://www.ietf.org/archive/id/draft-bradleylundberg-cfrg-arkg-10.html> on 2026-11-04 and read original
§§5.1/5.2/8.3 plus memo/publication status:

- Published 27 February 2026, intended Informational, Internet-Draft/work in progress; stated expiry
  31 August 2026 remains before this review.
- §5.1 example explicitly contains `1: -65537`, `3: -65700`, direct BL/KEM maps at -1/-2 and
  `-3: -9`. Its parameter table makes dkalg **optional int/tstr**.
- §5.2 Table2 and §8.3 explicitly identify ARKG-P256 placeholder **-65700**. The separate
  ESP256-split-ARKG signing placeholder is **-65539**, with ESP256 verification algorithm -9.

Independently inspected baseline `src/Ctap2/src/Cose/CoseArkgP256SeedKey.cs`: Decode requires integer
derived algorithm at 90–93; Encode writes integer -3 at 160–161 and byte-string-wrapped constituent
COSE maps at 163–167, then KeyType/Algorithm at 169–173. These are actual profile/model restrictions,
not proof that the draft lacks the seed marker. No C-owned file was edited.

**Correction:** `src/Ctap2/CLAUDE.md`, **Available Extensions**, previewSign/ARKG paragraph now explicitly
states seed algorithm placeholder -65700 and BL=-1/KEM=-2 labels agree, as does the example's derived
ESP256=-9. It then distinguishes SDK outer kty=2 versus draft -65537, wrapped versus direct maps and
required integer derived-algorithm modeling versus optional int/text. The experimental/non-production
warning, stated expiry, separate signing/derived identifiers and limited verifier remain attached.
Partial agreement is not whole-profile conformance, vendor/device interoperability, validated points
or production cryptographic authority. The prior investigation's unsupported subclaim remains immutable
provenance, superseded by this explicit reconciliation; C/E own their source/test follow-ups.

README's generic actual-profile/no-full-conformance paragraph does not name -65700 as unsupported and
needed no ARKG change. No extra ARKG warning or existing E verification interpretation was rewritten.

## Final bounded checks and handoff

- Follow-up changes **only two existing F guide paths**, prose outside fences: README Getting started;
  CLAUDE GetInfo and previewSign/ARKG paragraphs. No new source edit path, C# change, executable string,
  assertion, trait, API/project-reference change or shared-document change in this correction.
- Fresh Python baseline comparison of all changed C# after excluding only whole-line comments/blank
  lines: unchanged non-comment lines. Final module Markdown count remains **39 fences**; no fence
  content changed in this follow-up. Five A/B/E-owned XML dependencies remain as classified in edit-f.
- `git diff --check`: exit0, no whitespace errors; only the already recorded two csproj LF→CRLF
  checkout warnings. No compilation, SDK test, probe execution or hardware qualification inferred.
- Corrected checkout hashes:
  - `src/Ctap2/README.md`: `a4ce9582931bf8304888a74691e6683fe2cb4977b6ed72f51a384c809fa683e7`
  - `src/Ctap2/CLAUDE.md`: `ea814d3ad1ceac8dbd13a51042205c940e1854444f8c82be1043f793edd5cccd`

PRIMARY `edit-f.md` now links this correction and adds precise decoder/profile re-review dependencies.
Its original exact-path/fence-ordinal/typed-context map remains usable without re-extracting changed
snippet bodies. F is stable for the requested fresh module/shared/boundary reviews and snippet extraction;
the initial diagram image-regeneration and coordinator integrated gates remain outstanding.
