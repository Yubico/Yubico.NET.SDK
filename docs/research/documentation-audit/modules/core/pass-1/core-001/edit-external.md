# External shared-surface documentation edit

## Outcome and scope

The external documentation batch contains **19 edited product files**, ready for independent review.
It changes Markdown guidance and C# comments/XML only. All runtime bodies, imports, directives,
declarations, attributes, executable help/output, assertions and guards remain unchanged.

Worktree: `/home/rupicola/Code/y/yubikit-core-docs-edit-external`. Base and unchanged HEAD:
`da66f5d01ba594993974ddd26723e8c300c0a454`. Product source pin:
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. The initial worktree was clean.

The exact shared ledger supplies **125 external paths** after excluding Core, the nine applet modules,
root README/CLAUDE, direct `docs/*.md`, `docs/usage/` and `docs/architecture/`. Of these, **66 active
C#/Markdown paths** are eligible: 19 edited and 47 retained. Another 22 historical paths and 37
script/configuration/other-format paths are held read-only. These are editing dispositions, not a
fresh clean audit or additional source-investigation coverage.

**No verification or benchmark path occurs in `inventory/shared-sections.json`.** Those primary
external files are therefore outside this exact allowlist. Their known comment needs remain explicit
referrals in `editing/external/outside-owner-followups.json`; none was edited. Benchmark qualifications
were integrated only into the eligible `TOOLCHAIN.md` surface.

Only this worktree's allowed product paths, this report and `editing/external/*` were written. Primary
and other worktrees were read-only. No SDK build/test, formatter, compiler, native/device/provider/crypto
execution, Git index/ref update, commit, push or subagent occurred. The small receipt script performs
static record/hash/text checks, not a new audit framework or runtime experiment.

## Transfer records

All paths below are relative to this run directory.

| Record | Purpose |
| --- | --- |
| `editing/external/source.patch` | Complete uncommitted 19-file product diff with conventional `a/` and `b/` prefixes |
| `editing/external/allowlist.json` | Exact derivation, all external ledger paths, active write allowlist and owned record paths |
| `editing/external/path-decisions.json` | All 125 edit/retain/hold reasons, local/proposal IDs, original shared unit/line/source anchors, base/source Git blobs and base/current raw/LF hashes |
| `editing/external/candidate-decisions.json` | All 32 directly associated family locals, original definitions/aliases/facets, exact evidence units/range hashes and independent remaining holds |
| `editing/external/supplemental-dependencies.json` | Four explicitly added adjacent dependencies: A C009/C010/C011 in test-rig guidance and C C017 in current migration guidance; not new findings |
| `editing/external/boundary-referrals.json` | Three exact YubiOTP index associations, with original records and no blanket boundary acceptance |
| `editing/external/example-bodies.json` | Complete original/current fenced/XML code bodies, coordinates, hashes, classifications and unrun compiler status |
| `editing/external/citations-and-handoff.md` | Exact source authority, assertion limits, historical exclusions and actionable out-of-scope comment needs |
| `editing/external/input-identities.json` | Exact completed family/boundary/map/release/referral input hashes; read-only use |
| `editing/external/static-preservation.json` | Fresh bounded static receipt and product patch hash |
| `editing/external/record.py` | Reproduces these owned receipts without SDK execution or source writes |

The JSON records retain original `CORE-*-C*`, boundary and `FPT-*` identities. They allocate no accepted
Core finding IDs. Hash identity and inherited investigator completion do not substitute for independent
full-unit/content/citation review.

## Applied corrections and retained paths

| Original identities | External disposition |
| --- | --- |
| A C004/C005/C006/C013/C014; FPT-004/005/006/013/014 | Correct live identification before authorization, run-local fixture metadata, filtered nonmatches/read failures, empty-result binding skips and first-match rows in Tests.Shared. Replace nonexistent filter APIs. Explain exact form-factor matching and FIPS-mask limits without fixing decoding or filters. Retain `WithYubiKeyAttribute.cs`, whose existing qualifications are accurate. |
| A C009/C010/C011 adjacent dependencies | Test-rig guidance distinguishes the SDK's null topology resolver from absence of OS topology APIs and avoids a one-scan split/convergence guarantee. No merger policy or assertion changes. |
| A C018/C019; B4 C007/C008; FPT-018/019/054/055 | Correct toolchain recursive selection, same-kind translation limits, aggregate no-tests gate, full-argv exposure and compile/link versus execution evidence. Qualify benchmark fixture/censoring/provenance and OperatorCue outcomes. Qualify PermanentDeviceState reset/reinsertion. Retain root `toolchain.cs`; script/configuration changes remain held. |
| B1 C008; FPT-029 | `YkCommandBase.cs` and `DeviceSelector.cs` explain device preference separately from an explicit applet transport choice. Actual options/selection behavior is preserved. |
| B2 C002/C006; B3 C007; B5 C001/C006/C009; FPT-035/039/045/058/063/066 | Current migration explains Find/monitor ownership, synchronous ungrouped direct report access, macOS normalized input/expert feature storage, public direct borrowing factories, whole-workflow/input lifetime, firmware provenance and authentication limits. PublicApi comments describe finite reflection grammar and actual RP-validation option names, not runtime cancellation evidence. |
| B6 C007/C009/C010/C015; FPT-073/075/076/081 | Current migration explains contained SCP owners/shared record clones and plain SELECT retaining the old graph. The raw SCP method has typed prerequisites and an enclosing owned session/connection lifetime. Dated gap/changelog records remain byte-identical; no final/draft clause upgrade or profile acceptance is implied. |
| C C003/C006/C008/C014; FPT-085/088/090/096 | CLI reader XML distinguishes UTF-8 bytes, interactive validation, raw redirected bytes, discarded CR, normalization/profile responsibilities and synchronous input. Recorder XML distinguishes copied commands, retained responses and counter-only disposal. Credential test commentary states exact view/null/pool observations. Logger exceptions and separate copy owners remain qualified. |
| C C012/C017; FPT-094/099 | Current migration explains public RNG/SHA/AES factories versus internal primitive seams, direct-runtime bypass, current shared Base32, span suspension boundaries and shared array-bearing struct ownership. Historical gap/YAML bytes remain held. |
| C C013/C020; FPT-095/102 | Retain `HsmAuthCommands.cs`: the existing comments correctly scope management-key clearing around password resolution. They promise no all-provider/SCP-copy erasure. Pre-try allocations, other owners and behavior/output remain separate concerns. |
| CORE-OTP-X005/X007/X008; OATH X006 referral | Preserve working unified CLI scan-code translation, distinct from standalone OtpTool's known encoding defect. Correct ModHex XML only and retain the actual encoding regression assertions. The migration password-retry method now owns/clears its input in an enclosing finally, with the actual OATH callback API. Historical/YAML relocation records stay held. |

The detailed path ledger gives explicit decisions for every candidate-associated unchanged path,
including `WithYubiKeyAttribute.cs`, `HsmAuthCommands.cs`, the unified CLI encoding test and
`toolchain.cs`. A retained or edited file does not close its independent M/P/E/Q/profile facets.

## Example and verification boundaries

The three principal repaired examples are the raw SCP composable method, the OATH retry composable
method and the recorder test fragment. The filter XML contains complete filter/test types; the README
filter also has an enclosing test type. All use actual source APIs. Borrowed parameters remain with
their named caller; owned sessions/connections and transferred password storage have complete scopes.
The OATH callback does not invent a password loader or incorrectly rely on the retry helper to clear
its returned password memory.

All 243 original/current example bodies are classified and retained in the JSON record, including
historical v1/alpha/proposal material, implementation sketches, command/output/configuration fragments
and active composable examples. **No example was compiled or executed.** The compiler status is
NOT-RUN, not a passing consumer or all-platform result. Reuse the coordinator's existing qualified
consumer tools when that validation is assigned.

The static receipt checks the exact allowlist, unchanged HEAD, every edited C# non-comment line
(including preserved code on the two edited inline return comments), well-formed XML, Markdown fence
balance and `git diff --check`. It records raw/LF/BOM/final-newline identities separately. Some patch
edits add a final newline to formerly unterminated files; this is recorded, not hidden as a code change.
These are not Roslyn token/API/cref checks, builds, tests, native release proofs or independent review.

The first receipt attempt stopped because the later audit handoff `docs/research/documentation-audit/core.md`
does not exist at product pin `fe342fb0`. It wrote no receipts or product files. The corrected receipt
records a null source-pin blob for such later metadata, while retaining its exact base/current identity.
This was a record-identity assumption, not an SDK failure. No historical input was rewritten.
The first final scope check also encountered Git's collapsed untracked `editing/` directory; its
successor checks individual untracked files. Both preparation failures are retained in
`editing/external/static-attempts.md`. `final-checks.json` adds current hash, local-link target, fence
and exact status-scope checks; `output-identities.json` binds the final owned record bytes.

## Next owner and stop

The coordinator transfers the exact uncommitted product diff and these records, obtains independent
full-unit/citation review, reconciles held surfaces and performs only separately assigned existing
validation/publication gates. Windows concurrency input remains **UNREADY / UNREVIEWED**; PIV physical
touch/hotplug and touch-dependent cancellation/recovery remain user-deferred. Stronger experiments,
installed-producer identity, profile decisions and historical physical causes remain held.

Finish and publish Core Pass 1, deliver the deferred-finding handoff, then stop. This return starts no
Pass-2 implementation, planning project, broader audit or new platform/hardware campaign.
