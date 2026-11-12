"""Serialize explicit successor conclusions, never derive a verdict from a hash.

Checkpoint 1 is an immutable input. Scope-qualified dispositions are final for
this investigation, not clean passes or deferred unread work.
"""
import collections
import json
import subprocess
import record_checkpoint as old

HERE, RUN, ROOT, PIN = old.HERE, old.RUN, old.ROOT, old.PIN
sha, dump = old.sha, old.dump

NOTES = dict(old.NOTES)
NOTES.update({
    15: "Scope describes resource-manager/database authority, not physical access. pcsc-lite original marks USER/TERMINAL unused and SYSTEM local; qualify Windows-shaped USER comment, C009.",
    16: "Windows direct-reader exclusivity is not portable: pcsc-lite explicitly shares DIRECT mode. Shared/exclusive requests are separate from physical applet authentication; C009.",
    17: "All state comments read against original state-change API. UNAWARE returns immediately; ATRMATCH references Windows LocateCards (not imported here). No physical security-state proof; B1 owns loop cadence.",
    18: "Windows status0..6 matches official winsmcrd.h; Linux pcsc-lite status is flags. No active baseline SCardStatus import, so catalog portability concern only, C009.",
    24: "Encoding/CharSize are selected helpers, not every API ABI; Linux DwordSize8 is overbroad. IsElevated omits deterministic WindowsIdentity disposal; .NETv10 source confirms owned token and Dispose path, C005.",
    26: "All 27 DEVPKEY GUID/PID values match pinned official devpkey/hidclass headers; returned property type/size still requires actual per-call validation, C003 helper dependency.",
    28: "Full class catalog read and compared with pinned official headers. SmartCard990a2bd7 lacks original match after focused follow-up; only that unit is source-qualified, Q003; no claim it is wrong.",
    30: "Selector maps LocationInfo to LocationPaths in CmDevice; enum names are helper selection, not raw property types or dynamic refresh. Sparse public helper contract retained under C010.",
    31: "Active HID/keyboard/USB interface GUIDs match official hidclass/ntddkbd/usbiodef headers. Commented catalogs are historical reference, not exported API execution evidence.",
    40: "Entire shipped snapshot is #nullable enable only: no shipped member declarations. Administrative API preservation only, no runtime surface inference.",
    41: "Support entries and complete type inventory reconciled to B4 source. Single whole-file unit also contains unrelated crypto/device/protocol signatures: related context assigned to A/B1/B5/C, not an independent all-signature semantic or compile pass.",
    45: "Descriptor record carries interface metadata; HUT1.5 FIDO0xF1D0/1 and keyboard0x01/6 distinguish usages from physical identity/host characters. Signed VID/PID preserve16-bit patterns.",
    49: "Classifier uses exact usage pair; callers filter vendor separately. HUT1.5 anchors FIDO authenticator and keyboard collection values; pair alone establishes no native report-size/host-permission result.",
    50: "Usage pairs match HUT1.5; typed FIDO64/OTP8 profiles are distinct from arbitrary direct report sizes and application state. B5 owns framing/protocol semantic assurance.",
    51: "HID1.11 sections6.2.2.1–3: short size3 means4 bytes, reserved type3, long0xFE with no defined long tags. Parser safely stops truncated headers/payload; skip of complete long items is scoped parser behavior, not full descriptor validation.",
    52: "Report kind names/typical64/8 refer to SDK profiles. HID1.11/HUT define item/report and usage conventions, not universal direct-buffer size; platform caps and B5 profile own exact lengths.",
    56: "Full ModHex forward/reverse mapping read:16 lowercase plus16 uppercase non-whitespace symbols; shift marker distinct from universal host alphabet. CLI selection includes32, C010.",
    57: "Both constructors, conversion/indexer throw behavior and SupportedCharacters keys read. char-indexer self-reference and array/uppercase summaries inaccurate; shifted punctuation and canonical aliases matter, C010.",
    58: "German forward entries and independently decoded reverse97 entries: duplicate character assignments select final0x32; reverse0x31 aliases remain. Shift/punctuation follows table, not universal roundtrip, C010.",
    59: "UK full tables/reverse aliases read: indexer assignment overrides earlier slash/hash entries; final forward and reverse-only0x31 aliases mean nonidentity byte roundtrip, C010.",
    60: "US full forward/reverse entries and wrapper read; duplicate aliases around0x31/0x32/0x64 preserve canonical last forward assignment rather than bijection, C010.",
    61: "es_US full tables and wrapper read; US-shaped entries/aliases need locale applicability qualification, not a tested Spanish-layout OS failure, C010.",
    62: "French full tables/reverse read: AZERTY-specific letters and shifted digits/punctuation; repeated character assignments choose last usage and reverse aliases remain, C010.",
    63: "Italian full tables/reverse read: accented characters, shifted punctuation and duplicate aliases inspected; no host-layout acceptance or byte-roundtrip identity inferred, C010.",
    64: "sv_SE full tables/reverse read: US-shaped mapping lacks Swedish letters; locale label/application concern without tested host keyboard failure; aliases remain, C010.",
    65: "Eight layout selectors0..7 match translator dispatch including ModHex and seven locale maps; enum availability does not qualify every host keyboard layout.",
    74: "Child method serializes target runner args, collects stdout/stderr and bounds process wait; cancellation Kill does not await actual exit/output drain, C008. No child launched.",
    75: "RSA repro composes mismatched public modulus/private primes and zeroes sensitive test buffers; separate ASN.1/error assertions read. C owns crypto correctness; this is test-host/oracle context, unrun.",
    76: "Independent required route tuples, explicit operation/profile roles and reflection method resolution read; excluded Windows HID rows are not verified.43 operations76 links do not execute profiles.",
    77: "Positive required totals, role checks and deletion/mutation fixtures fail closed for absent links/symbols; reflection resolution says nothing about native completion. Tests read, not run.",
    78: "Complete scanner fixtures and semantic assertions distinguish blocking/scheduling/dispatch/callback sites, symbol targets and unsafe async shapes; compilation fixture diagnostics remain test-only source oracles.",
    79: "Manifest fingerprints complete site identity, resolves family evidence/owner/status and reports duplicates/missing/stale entries; explicit outstanding status never asserts async safety.",
    80: "Roslyn semantic scan resolves imports/exports/waits/scheduling/callbacks and dispatch gaps with explicit scope/exclusions; CoreSourceRoot search/source compilation does not inspect native binary behavior.",
    81: "FIDO full-frame cancel/drain/reuse and OTP recovery assertions read with fake bridges/prompt; early failure before continuation assignment can escape cleanup drain, separate C008 facet.",
    82: "Migration diagnostics source predicates and protocol-backed canaries read with logger/error/cleanup context; bounded diagnostic fixtures are not whole-SDK redaction, Windows or physical qualification.",
    83: "Independent23-entry public-sync registry resolves public contracts/owners with explicit method selection; source reachability is separate from task-return/native timing.",
    84: "Positive23-entry expectation, required Linux removal and dangling owner mutation assertions read; no registry reflection test establishes native wait behavior.",
    85: "README explicitly limits Core-only scanner to source-site classification;218 sites,43 adapter operations76 links,23 sync entries; Windows/native/device qualification remains outstanding.",
    86: "Held native fake probes separate caller invocation/native thread/terminal completion and release in finally. Five-second waits are watchdogs, not budgets; managed seams only.",
    87: "Probe records nested invocation/operation tasks and timestamps; Dispose releases native gate, joins caller and drains actual operation with bounded cleanup before disposing markers. No real native API.",
    88: "All43 JSON operations/76 profile links read against independent registry and named tests; route/profile/evidence fields link managed oracles without executing them or certifying Windows HID.",
    89: "All family entries/rationale/evidence/status read;218 source sites remain documented/outstanding classifications, not accepted runtime safety. Windows imports/callbacks stay qualified separately.",
    91: "Complete lexical scanner predicates/fixtures read: ignored-result loop, task/blocking patterns and exclusions; textual sensitivity is bounded, no live retry cadence/performance claim.",
})
assert set(NOTES) == set(range(106))

# Explicit path-level conclusions for shared scopes. Each row additionally keeps
# its exact unit label/range, so a grouped note cannot expand assignment ownership.
SHARED = {}
def shared(paths, note, owner="B4 support editor; coordinator assigns compilation/execution"):
    for path in paths.splitlines():
        if path.strip():
            assert path not in SHARED, path
            SHARED[path] = {"note": note, "owner": owner}

shared(".github/workflows/build.yml\n.github/workflows/native-aot.yml",
       "Workflow jobs/configuration read with actual toolchain gate and AOT host. Source matrix/required output labels establish intended checks, not a hosted run or every native function; preserve exact historical artifact gap Q002.")
shared("Directory.Build.props\nDirectory.Build.targets\nDirectory.Packages.props\nsrc/Core/src/Yubico.YubiKit.Core.csproj",
       "net10/language/warnings and package1.18.1 opt-in, late AOT metadata/analyzer/API AdditionalFiles/friend declarations reviewed together. Configuration is not evaluated compiler input or binary producer evidence. Historical multi-target wording and current static-AOT publication need scoped correction.")
shared("global.json\nnuget.config",
       "SDK10.0.300 latestFeature and explicit public/private package sources/mapping read. Source selection/credential prohibition are declared intent; no restore, feed authentication, resolved SDK or package execution occurred.")
shared("PACKAGE_README.md\nREADME.md\nsrc/Core/PACKAGE_README.md",
       "Assigned installation/requirements/package guidance read against project graph and current toolchain. Core transitive dependency is distinct from direct Core use; no feed/network install or all-platform acceptance inferred.")
shared("TOOLCHAIN.md\ntoolchain.cs",
       "Examples/options/dispatch/result semantics traced to parsed args and target registration. Bare invocation prints help; named default tests/publishes. RunLog stores raw argv (C007); every-project executed assertion exceeds gate (C008); substring selectors do not imply unique target.")
shared("docs/CRYPTO-APIS.md\ndocs/CSHARP-PATTERNS.md\ndocs/MEMORY-MANAGEMENT.md\ndocs/LOGGING.md\ndocs/SDK-HOUSE-STYLE.md\ndocs/DEV-GUIDE.md",
       "Assigned policy/examples read: Span/Memory scope, borrowed input/result ownership, explicit zeroing and provider/logging responsibilities. Guidance is not proof every implementation cleans/redacts; C-C008/C011–C013/C017–C019 retain logging/provider/stale-example ownership. B4 nested Windows copies/raw argv are separate concrete facets.",
       "SHARED documentation editor with C primary security/provider owners and B4 carrier context")
shared("docs/TESTING.md\ndocs/TESTING_PLATFORM_FINDINGS.md",
       "Trait/runner/allow-list/filtering/transport/presence guidance compared with actual helpers. Filtering is not transport pinning or every-device iteration; old paths/examples and historical Windows result need qualification. Keep physical authorization, attended work and no-runtime boundary distinct.")
shared("docs/NATIVE-AOT.md",
       "Current support envelope read against ten-type host, source contract discovery and published static targets. IsAotCompatible/link discovery is not all-function/native driver acceptance. Linux native-only checks retain B2 scope; Windows exact retained artifact/producer gap Q001/Q002 remains.")
shared("docs/architecture/.state.yml\ndocs/migration/.state.yml",
       "Watermarks are automation provenance a5881533, not current product source pins or proof automation ran in this audit. Preserve unchanged state; no migration/architecture gate executed.")
shared("docs/architecture/architecture-docs-automation.md\ndocs/architecture/images/image-manifest.txt",
       "Automation contract and source/image manifest read with actual scripts. Five assigned source-block/SVG/PNG digests reconcile independently and PNG labels visually match; freshness does not verify teaching claims (C010). No render/formatter/workflow ran.")
shared("docs/architecture/sdk-architecture-diagrams.md\ndocs/architecture/sdk-architecture-map.yml",
       "Assigned diagram/appendix/map sections compared with graph and meaningful SVG/PNG labels: WebAuthn is indirect through Ctap2, ApduResponse is sealed class not record struct. Pedagogical pipelines are not native timing/wire conformance. C010 preserves exact source versus teaching-label distinctions.")
shared("docs/architecture/applet-public-api.md\ndocs/architecture/webauthn-extension-processing.md\ndocs/usage/user-interaction.md",
       "Assigned creation/prompt/extension lifecycle read: snapshot options, sole retained caller-owned prompt, owned versus borrowed connection, per-attempt extension state and token lifetime. B4 caller-support context only; A/B1/B5 and applet owners retain actual session/policy/wire assurance; no full WebAuthn conformance inferred.",
       "SHARED editor with A/B1/B5 and WebAuthn primary implementation owners")
shared("docs/migration/v1-to-v2.md\ndocs/migration/v1-to-v2-map.yml\ndocs/migration/v1-to-v2-gaps.md\ndocs/migration/v1-to-v2-changelog.md",
       "Every assigned section/map entry read as dated assisted/manual migration advice, not an automatic migration or implementation pass. PIV scalar line274 reproduces inherited DOC-002 ScannerError column488; assigned later entries still reviewed. B4 native/support claims use actual route/owner/error scopes; applet-specific parity stays with its primary owner.",
       "SHARED migration editor; inherited DOC-002 owner; relevant A/B1/B5/C/applet investigators")
shared("docs/v1-to-v2-comparison.md\ndocs/v2-highlights.md",
       "Assigned comparison/support claims read: async signature versus retained sync access, process claim versus cross-process sharing, sparse low-level surface. Shared-shim AOT copy and ordinary-SCardException contention text conflict with current targets/strict-sharing map; C008/C009/C010, not an all-product parity judgment.")
shared("2026-09-21-yubikit-async-boundaries-ISA.md\ndocs/plans/yubikit-async-boundaries/00-status.md\ndocs/plans/yubikit-async-boundaries/01-product.md\ndocs/plans/yubikit-async-boundaries/02-architecture.md\ndocs/plans/yubikit-async-boundaries/03-program-design.md\ndocs/plans/yubikit-async-boundaries/addenda/earlier-multi-key-architecture.md\ndocs/plans/yubikit-async-boundaries/addenda/earlier-multi-key-program-design.md\ndocs/plans/yubikit-async-boundaries/addenda/multi-key-capacity.md",
       "Complete assigned historical/current plan ranges read, including assertions/pseudocode/decisions. Criteria/approvals/proposals and historical run reports are not current implementation acceptance. Preserve unimplemented multi-key scope, stale206/219/24 versus current218/23 counts, separate38JIT+38AOT native-only and43 serial103 physical checks, and unready Windows lane. No old command was rerun.",
       "Async-program evidence owner/coordinator; B2/B3 platform owners; ready Windows handoff owner")
shared("docs/research/native-aot-readiness.md\ndocs/research/native-aot-readiness-data.md\ndocs/research/documentation-audit/core.md",
       "Assigned retained history read with current host/targets. Dated dynamic-loader/default-provider assertions and listed output do not prove every function ran. Integration4442/197 and38+38 retain exact historical populations/strict-live failures; B4 contributes zero runs or archive edits. Q001/Q002 preserve source/producer/artifact limits.",
       "Coordinator and historical evidence owners; A-C020/B3-C008 aliases")
shared("scripts/architecture/architecture-images.env\nscripts/architecture/build-architecture-context.sh\nscripts/architecture/check-architecture-images.sh\nscripts/architecture/check-architecture-map.sh\nscripts/architecture/lib-architecture-blocks.sh\nscripts/architecture/mermaid.config.json\nscripts/architecture/render-architecture.sh",
       "Complete assigned script/config text read: ordered fenced blocks, pinned CLI version, source/artifact digests, path/symbol/project-edge and forbidden-edge checks, render versus check-only distinction. These lexical/XML/source gates cannot prove diagram semantics or runtime ownership. Scripts not executed.")
shared("scripts/toolchain/BenchmarkTargets.cs\nscripts/toolchain/BuildTargets.cs\nscripts/toolchain/Context.cs\nscripts/toolchain/Conventions.cs\nscripts/toolchain/CoverageTargets.cs\nscripts/toolchain/DocsTargets.cs\nscripts/toolchain/Options.cs\nscripts/toolchain/Output.cs\nscripts/toolchain/PackageTargets.cs\nscripts/toolchain/Process.cs\nscripts/toolchain/Results.cs\nscripts/toolchain/RunLog.cs\nscripts/toolchain/TargetRegistry.cs\nscripts/toolchain/TestTargets.cs\nscripts/toolchain/Usage.cs",
       "All assigned implementation ranges read with omitted AOT discovery/validation and test execution/classification predicates. Runner-specific filters/count parsing, named targets, package key, artifact logs and exclusions traced. Exit-zero/null-count can pass; only all-no-tests is rejected; raw invocation is outside Exec.Mask. C007/C008; no target executed.")
shared("src/Cli.Commands/src/Fido/FidoCommands.cs\nsrc/Cli.Commands/src/Infrastructure/GlobalSettings.cs\nsrc/Cli.Commands/src/Infrastructure/YkCommandBase.cs\nsrc/Cli.Commands/src/Management/ManagementResetCommand.cs\nsrc/Cli.Commands/src/OpenPgp/OpenPgpAccessCommands.cs\nsrc/Cli.Commands/src/OpenPgp/OpenPgpCertificatesCommands.cs\nsrc/Cli.Commands/src/OpenPgp/OpenPgpHelpers.cs\nsrc/Cli.Commands/src/OpenPgp/OpenPgpInfoCommand.cs\nsrc/Cli.Commands/src/OpenPgp/OpenPgpKeysCommands.cs\nsrc/Cli.Commands/src/OpenPgp/OpenPgpResetCommand.cs\nsrc/Cli.Commands/src/Otp/OtpCommands.cs\nsrc/Cli.Commands/src/Piv/PivCommands.cs\nsrc/Cli.Commands/tests/Yubico.YubiKit.Cli.Commands.UnitTests/Oath/OathHelpersTests.cs",
       "Assigned caller bodies/exception-output/settings/helpers read: user input/owned session scopes, raw status versus friendly output and destructive/interactive prerequisites stay operation-specific. OTP password picks translator's32 non-whitespace ModHex cases (C010). No command, secret prompt, reset or device operation ran; applet wire semantics belong to respective primary owner.",
       "CLI/support editor with relevant applet/C/B5 primary owners")
shared("src/Cli.Commands/src/Yubico.YubiKit.Cli.Commands.csproj\nsrc/Cli.Shared/src/Yubico.YubiKit.Cli.Shared.csproj\nsrc/PublicApi/tests/Yubico.YubiKit.PublicApi.UnitTests/Yubico.YubiKit.PublicApi.UnitTests.csproj\nsrc/Tests.Shared/Yubico.YubiKit.Tests.Shared.csproj\nsrc/Core/tests/Yubico.YubiKit.Core.IntegrationTests/Yubico.YubiKit.Core.IntegrationTests.csproj\nsrc/Core/tests/Yubico.YubiKit.Core.IntegrationTests/xunit.runner.json\nsrc/Core/tests/Yubico.YubiKit.Core.UnitTests/Yubico.YubiKit.Core.UnitTests.csproj\nsrc/Core/tests/Yubico.YubiKit.Core.UnitTests/xunit.runner.json",
       "Project/runner declarations read: nonpackable tooling, xUnitv2 shared/integration versus v3MTP executable units, direct xUnit-free retry source link, per-reference generated shipping metadata and disabled integration parallelism. No evaluated build or actual test selection/runtime qualification inferred.")
shared("CLAUDE.md\nsrc/Core/CLAUDE.md\nsrc/Core/README.md\nsrc/Core/tests/CLAUDE.md\nsrc/PublicApi/CLAUDE.md\nsrc/Tests.Shared/CLAUDE.md\nsrc/Tests.Shared/README.md",
       "All assigned support/instruction/example ranges read separately from obeying orientation. Scoped discovery/session/direct ownership, logging/security and test helper contracts compared with current bodies. Stale state.YubiKey/scanner/SafeLibraryHandle examples and runner commands are C010; C-C017/C018 and A/B1/B5 retain semantic aliases. No examples compiled or instruction commands run.",
       "SHARED/Core/test-support editors with A/B1/B5/C primary owners")
shared("src/PublicApi/tests/Yubico.YubiKit.PublicApi.UnitTests/AsyncSurfaceConventionTests.cs\nsrc/PublicApi/tests/Yubico.YubiKit.PublicApi.UnitTests/FactoryShapeTests.cs\nsrc/PublicApi/tests/Yubico.YubiKit.PublicApi.UnitTests/PublicReturnContractScanner.cs\nsrc/PublicApi/tests/Yubico.YubiKit.PublicApi.UnitTests/PublicReturnContractScannerTests.cs\nsrc/PublicApi/tests/Yubico.YubiKit.PublicApi.UnitTests/PublicReturnContractTests.cs",
       "Assigned assertion/scanner bodies plus full selected allowlist, factory predicate and shipping-assembly loader context read. Recursive inheritance/member/generic/array/task shapes, cycle guards and negative fixtures provide bounded API-shape oracles, not disposal/native timing/semantic conformance. Reflection tests remain unrun.",
       "PublicApi test owner and relevant primary implementation owners; B4 support context")
shared("src/Tests.Shared/Infrastructure/AllowList.cs\nsrc/Tests.Shared/Infrastructure/AppSettingsAllowListProvider.cs\nsrc/Tests.Shared/Infrastructure/IAllowListProvider.cs\nsrc/Tests.Shared/Infrastructure/TestCategories.cs\nsrc/Tests.Shared/Infrastructure/TransientScanRetry.cs\nsrc/Tests.Shared/Infrastructure/WithYubiKeyAttribute.cs\nsrc/Tests.Shared/Infrastructure/YubiKeyTestInfrastructure.cs\nsrc/Tests.Shared/YubiKeyTestState.cs\nsrc/Tests.Shared/YubiKeyTestStateExtensions.cs\nsrc/Tests.Shared/appsettings.json",
       "Assigned configuration/state/helper/filter bodies and exact retry saturation predicate read. Output appsettings is sole allow-list source; unknown serials false; placeholders bind first eligible cached key, filter does not pin transport. Retry is finite exact type/message, not arbitrary failure replay. Management helper owns hidden session and passes cached metadata. No discovery, authorization mutation or test executed.",
       "Tests.Shared support owner; A metadata/filter and applet helper primary owners")
shared("src/Tests.Shared/Infrastructure/OperatorCue.cs",
       "Announce console/echo lie outside speech catch; WaitForExit15s result ignored. Outcome-neutral/finished-speech wording exceeds actual failure/timeout behavior, C008. No child/speech launched.",
       "Tests.Shared support editor; separately assigned test implementation reviewer")

for name in ["L0-context", "L1-assembly-deps", "L2-layered-stack", "L3-apdu-sequence", "L3b-fido2-ctap-sequence"]:
    shared(f"docs/architecture/images/{name}.svg\ndocs/architecture/images/png/{name}.png",
           f"{name}: PNG visually inspected and SVG meaningful text labels independently extracted/compared with Mermaid. All three source/SVG/PNG manifest digests match. Binary unit character ranges are administrative identities, not visual semantics. C010 teaching scope still applies.")

# New exact primary spans extend the stable checkpoint groups.
SPANS = {cid: list(spans) for cid, spans in old.SPANS.items()}
SPANS["CORE-B4-C005"] += [(24, 1, 200)]
SPANS["CORE-B4-C009"] += [(15, 17, 34), (16, 17, 100), (18, 17, 200)]
SPANS["CORE-B4-C008"] += [(74, 19, 88), (81, 1, 193)]
SPANS["CORE-B4-C010"] += [(57, 19, 214)] + [(i, 17, 10000) for i in [56, 58, 59, 60, 61, 62, 63, 64]]

# Shared path/span map. Broad reopen entries intentionally include retained,
# unchanged context: a candidate does not imply every member of that file is wrong.
SHARED_SPANS = {
    "CORE-B4-C001": [("src/Core/README.md", 593, 631), ("src/Core/CLAUDE.md", 877, 892)],
    "CORE-B4-C002": [("src/Core/CLAUDE.md", 652, 683)],
    "CORE-B4-C003": [("src/Core/CLAUDE.md", 423, 499)],
    "CORE-B4-C004": [("docs/TESTING_PLATFORM_FINDINGS.md", 1, 10000)],
    "CORE-B4-C005": [("src/Core/CLAUDE.md", 399, 422)],
    "CORE-B4-C006": [("docs/NATIVE-AOT.md", 1, 10000)],
    "CORE-B4-C007": [(p, 1, 10000) for p in ["toolchain.cs", "scripts/toolchain/RunLog.cs", "scripts/toolchain/Options.cs", "scripts/toolchain/PackageTargets.cs", "scripts/toolchain/Process.cs", "docs/LOGGING.md", "TOOLCHAIN.md"]],
    "CORE-B4-C008": [(p, 1, 10000) for p in ["TOOLCHAIN.md", "scripts/toolchain/Results.cs", "scripts/toolchain/TestTargets.cs", "scripts/toolchain/RunLog.cs", "scripts/toolchain/BuildTargets.cs", "docs/NATIVE-AOT.md", "docs/v2-highlights.md", "src/Core/src/Yubico.YubiKit.Core.csproj", "src/Tests.Shared/Infrastructure/OperatorCue.cs", ".github/workflows/build.yml", ".github/workflows/native-aot.yml", "Directory.Build.props", "global.json"]],
    "CORE-B4-C009": [("docs/v2-highlights.md", 1, 10000)],
    "CORE-B4-C010": [(p, 1, 10000) for p in ["docs/architecture/sdk-architecture-diagrams.md", "docs/architecture/sdk-architecture-map.yml", "src/Core/tests/CLAUDE.md", "docs/TESTING.md", "src/Cli.Commands/src/Otp/OtpCommands.cs"]] + [("src/Core/CLAUDE.md", 399, 422), ("src/Core/CLAUDE.md", 778, 795), ("CLAUDE.md", 156, 170), ("CLAUDE.md", 499, 535)],
}

HISTORICAL = {p for p in SHARED if p.startswith("docs/plans/") or p.startswith("docs/research/") or p.startswith("2026-")}
RELATED = {"src/Core/src/PublicAPI.Unshipped.txt"}
RELATED_SHARED = {p for p in SHARED if p.startswith(("src/Cli.Commands/", "src/PublicApi/", "src/Tests.Shared/"))}
RELATED_SHARED |= {p for p in SHARED if p in ["docs/CRYPTO-APIS.md", "docs/CSHARP-PATTERNS.md", "docs/MEMORY-MANAGEMENT.md", "docs/SDK-HOUSE-STYLE.md", "docs/DEV-GUIDE.md", "docs/architecture/applet-public-api.md", "docs/architecture/webauthn-extension-processing.md", "docs/usage/user-interaction.md", "docs/migration/v1-to-v2.md", "docs/migration/v1-to-v2-map.yml", "docs/migration/v1-to-v2-gaps.md", "docs/migration/v1-to-v2-changelog.md"]}

if __name__ == "__main__":
    packet_path = RUN / "inventory/packets/B4-windows-support.json"
    packet = json.loads(packet_path.read_text())
    inventory = {u["id"]: u for u in json.loads((RUN / "inventory/semantic-units.json").read_text())}
    primary = set(packet["unit_ids"])
    paths = packet["paths"]
    shared_paths = {inventory[uid]["path"] for uid in packet["shared_read_unit_ids"]}
    assert shared_paths == set(SHARED), (shared_paths - set(SHARED), set(SHARED) - shared_paths)
    frozen = json.loads((HERE / "history/checkpoint-1/unit-dispositions.json").read_text())
    assert {u["id"] for u in frozen} == set(packet["unit_ids"] + packet["shared_read_unit_ids"])
    identities = json.loads((HERE / "history/checkpoint-1/file-identities.json").read_text())
    failures = []
    for row in identities:
        data = (ROOT / row["path"]).read_bytes()
        if sha(data) != row["current_raw_sha256"]:
            failures.append({"path": row["path"], "failure": "changed-since-checkpoint"})
    rows = []
    for previous in frozen:
        u = inventory[previous["id"]]
        path = u["path"]
        is_primary = u["id"] in primary
        index = paths.index(path) if is_primary else None
        note = NOTES[index] if is_primary else SHARED[path]["note"]
        candidates = []
        for cid in SPANS:
            spans = [(a, b) for i, a, b in SPANS[cid] if i == index] if is_primary else [(a, b) for p, a, b in SHARED_SPANS[cid] if p == path]
            if any(u["first_line"] <= b and u["last_line"] >= a for a, b in spans):
                candidates.append(cid)
        related = path in RELATED or (not is_primary and path in RELATED_SHARED) or index == 75
        historical = path in HISTORICAL
        source_qualified = u["id"] == "core-001:unit:d6efcc57eba79156"
        if source_qualified:
            candidates.append("CORE-B4-C010")
        status = ("READ-SOURCE-QUALIFIED" if source_qualified else "READ-CANDIDATE" if candidates else
                  "READ-HISTORICAL-QUALIFIED" if historical else "READ-RELATED-CONTEXT" if related else "READ-CLEAN")
        scope = ("B4 support entries/type inventory; non-B4 signatures not independently reaccepted" if path in RELATED else
                 "Dated source/proposal/evidence report, not rerun or current artifact attestation" if historical else
                 "Assigned caller/oracle/policy context; underlying semantic assurance remains with named primary owner" if related else
                 "Exact assigned implementation/comment/assertion or artifact content")
        owner = ("Core Windows maintainers: original SmartCard GUID provenance (Q003)" if source_qualified else
                 "A/B1/B5/C primary implementation owners; B4 support snapshot editor" if path in RELATED else
                 SHARED[path]["owner"] if not is_primary else "B4 documentation editor; Pass2 implementation reviewer separately assigned")
        data = (ROOT / path).read_bytes()
        text = data.decode("utf-8-sig", errors="replace")
        actual = sha(text[u["start_character"]:u["end_character_exclusive"]].encode())
        if actual != u["range_sha256"]:
            failures.append({"id": u["id"], "failure": "range-hash"})
        row = dict(previous)
        row.update(status=status, reason=f"{note} Exact unit: {u['label']} ({u['first_line']}–{u['last_line']}).",
                   actual_range_sha256=actual, complete_context_read=path not in RELATED,
                   assigned_scope_disposition_complete=True, scope=scope, disposition_owner=owner,
                   qualification=("Original authority absent after initial pass and focused follow-up; not clean, Q003" if source_qualified else scope),
                   pending_context=None, candidate_ids=candidates, sample_eligible=status == "READ-CLEAN",
                   documentation_status="LOCAL-CANDIDATE" if candidates else status,
                   implementation_status="NOT-ACCEPTED-BY-THIS-AUDIT", review_status="INDEPENDENT-REVIEW-PENDING",
                   validation_status="VALID-NOT-CHECKED", platform_execution="UNRUN",
                   previous_checkpoint_status=previous["status"], evidence_record="file-evidence.md; candidates.md; sources/register.md")
        rows.append(row)
    dump("unit-dispositions.json", rows)
    dump("shared-conclusions.json", SHARED)
    families = []
    for family in dict.fromkeys(old.family(i) for i in range(106)):
        indices = [i for i in range(106) if old.family(i) == family]
        families.append({"family": family, "files": len(indices), "lines": sum(len((ROOT / paths[i]).read_bytes().splitlines()) for i in indices),
                         "complete_file_dispositions": len(indices), "full_file_reads": sum(i != 41 for i in indices), "unread_paths": []})
    summary = {"status": "COMPLETE-PINNED-INVESTIGATION", "source_pin": PIN,
               "observed_head": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(),
               "packet_sha256": sha(packet_path.read_bytes()), "primary_files": 106, "primary_lines": 14842,
               "complete_primary_file_dispositions": 106, "complete_primary_file_reads": 105,
               "scoped_related_context_snapshot_files": 1, "unread_primary_files": 0,
               "primary_unit_count": 1456, "shared_unit_count": 894, "total_unit_count": 2350,
               "primary_status_counts": dict(collections.Counter(r["status"] for r in rows if r["ownership"] == "primary")),
               "shared_status_counts": dict(collections.Counter(r["status"] for r in rows if r["ownership"] == "assigned-shared")),
               "combined_status_counts": dict(collections.Counter(r["status"] for r in rows)),
               "resolved_checkpoint_outstanding": dict(collections.Counter(r["previous_checkpoint_status"] for r in rows if r["previous_checkpoint_status"] in {"UNREAD", "READ-PENDING-CONTEXT"})),
               "families": families, "local_candidate_count": 10,
               "sdk_builds_tests_native_device_formatter_executions": 0, "identity_failures": failures,
               "note": "106/106 primary files dispositioned; 105 full reads and one whole-file API snapshot reconciled for B4 support/type inventory with explicit non-B4 signature scope. Qualified/context rows are not clean or await-context. Hashes never establish semantic verdicts."}
    dump("coverage-summary.json", summary)
    dump("continuation.json", {"status": "PINNED-INVESTIGATION-COMPLETE", "source_pin": PIN, "remaining_unit_ids": [], "remaining": [],
                               "next_candidate_number": "CORE-B4-C011", "external_qualification_record": "qualification-gaps.json",
                               "late_windows_delta": "Ready handoff only; no unfinished branch/worktree inspected; focused reopening map in continuation-and-verification.md."})
    candidate_index = []
    for cid in SPANS:
        affected = [r for r in rows if cid in r["candidate_ids"]]
        candidate_index.append({"id": cid, "status": "LOCAL-CANDIDATE", "unit_ids": [r["id"] for r in affected],
                                "unchanged_primary_or_shared_paths": sorted({r["path"] for r in affected}),
                                "facets": ["documentation", "runtime-source-concern", "policy-decision", "external-qualification"],
                                "additional_dependencies": "candidates.md and continuation-and-verification.md preserve narrow A/B1/B2/B3/B5/C/applet aliases and reopening paths without extra primary dispositions.",
                                "final_disposition_recommendation": "Explicitly retain/correct each listed unchanged path during editing/review; no automatic Pass2 selection or accepted ID."})
    dump("candidate-index.json", candidate_index)
    evidence = ["# B4 final file and shared-scope evidence", "", summary["note"], "", "## Primary files", "", "| Index / path | Substantive conclusion |", "| --- | --- |"]
    evidence += [f"| {i}: `{paths[i]}` | {note} |" for i, note in sorted(NOTES.items())]
    evidence += ["", "## Assigned shared ranges", "", "Each exact ID/range appears in unit-dispositions.json; related/historical qualifications never enter clean sampling.", "", "| Path | Conclusion | Responsible scope |", "| --- | --- | --- |"]
    evidence += [f"| `{p}` | {v['note']} | {v['owner']} |" for p, v in SHARED.items()]
    (HERE / "file-evidence.md").write_text("\n".join(evidence) + "\n")
    dump("completion-transition.json", [{"id": r["id"], "before": r["previous_checkpoint_status"], "after": r["status"], "reason": r["reason"], "owner": r["disposition_owner"]} for r in rows if r["previous_checkpoint_status"] in {"UNREAD", "READ-PENDING-CONTEXT"}])
    print(json.dumps({k: summary[k] for k in ["status", "complete_primary_file_dispositions", "complete_primary_file_reads", "combined_status_counts", "identity_failures"]}, indent=2))
    if failures:
        raise SystemExit(1)
