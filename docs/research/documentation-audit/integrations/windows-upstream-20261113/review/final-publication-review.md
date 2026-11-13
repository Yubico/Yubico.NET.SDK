# Independent final evidence and publication review

**Not ready for publication: one Important evidence-retention blocker. No Critical issue found.**

The retained software results support the final documentation at its stated scope. However, the
exact staged publication packet omits 40 receipt-listed validation logs. Those logs exist locally
and match their captured hashes; they are not in the reviewed tree or publication manifest. Resolve
that bounded publication omission before committing. No build, test, formatter or platform rerun is
requested.

## Reviewer and exact checkpoint

- Reviewer: fresh independent GPT-6.1 Sol (`github-copilot/gpt-6.1-sol`), separate from the resolvers,
  source/documentation reviewers and execution lead; reviewed on 2026-11-13.
- Worktree: `/home/rupicola/Code/y/yubikit-windows-audit-integration-20261113`.
- Branch: `yubikit-windows-audit-integration-20261113`.
- Reviewed staged tree: `7e8b4b40111b65231470a06207cf9700f12f7015`.
- Audited `HEAD`: `d5d01ef99f1478e4d49911194c17667b2450d2fd`.
- Incoming `MERGE_HEAD`: `e322712d609aaf03b9bd1cf2964a99326728866a`.
- Natural base: `5746e72ed9a848d08b5782ddde025aa4a4b522ef`.
- Software-tested product tree: `05d4409729dd81c7717c99a15a2697d49eb10906`.
- Final-documentation capture tree: `5dcd089144393515641dd94130b1ecdac92bfeca`.
- Publication manifest blob: `3f0d3bbc5c71551f6dcf93883fb33c111c42ca7a`.
- Publication manifest SHA-256:
  `2e0651bca9cb2696929ba239ed250ffc940c0b4d043fa132dc9d97bc2df0f5a1`.
- Active source: 1,363 C# files under `src`, `verification` and `scripts`, excluding `bin`/`obj`;
  fingerprint `c05c4010789edd3424ec62dc143c68bb3fa3cdc36cacc2eef4069c27d6e82b02`.

The source fingerprint was freshly reproduced using the prior review's sorted relative path + NUL +
raw SHA-256 digest construction. It is unchanged from independent source review. Root instructions,
approved `records/style.md`, the final plan snapshot, coordinator decisions, prior reviews, manifest,
intended message, handoff, status, successor map and retained results supplied the review inputs.
Product and index remained read-only. Only this new review record was created. No build, test,
formatter, native/device/credential/cryptographic exercise, commit, push or subagent was run.
Static file hashing and artifact inspection are not runtime acceptance.

## Important blocker: receipt logs are absent from publication

`execution/prepare-publication.py:28` enumerates packet paths with `git ls-files --cached --others
--exclude-standard`. The repository's `.gitignore:468` matches `*.log`. As a result, all 40 locally
retained toolchain logs below are excluded from its manifest and staged tree. The script's final
untracked-file assertion uses the same ignore-aware selection and does not detect this omission.
The packet's `.gitattributes` preserves bytes but does not override ignore rules.

All paths in this table are relative to `RUN/validation/`; each name is an exact receipt-listed file.

| Lane | Omitted files under `toolchain/` | Count |
| --- | --- | ---: |
| `clean-solution-build` | `01-clean.log`, `02-build.log`, `run.log` | 3 |
| `complexity-from-audit` | `run.log` | 1 |
| `complexity-from-incoming` | `run.log` | 1 |
| `device-event-lifecycle` | `01-test-Core.IntegrationTests.log`, `run.log` | 2 |
| `docs-inclusive-pack` | `01-build.log`, `02-pack-Core.log`, `03-pack-Ctap2.log`, `04-pack-Management.log`, `05-pack-Oath.log`, `06-pack-OpenPgp.log`, `07-pack-Piv.log`, `08-pack-SecurityDomain.log`, `09-pack-WebAuthn.log`, `10-pack-YubiHsm.log`, `11-pack-YubiOtp.log`, `run.log` | 12 |
| `final-documentation` | `run.log` | 1 |
| `ordinary-unit-suite` | `01-test-Cli.Commands.UnitTests.log`, `02-test-Cli.Shared.UnitTests.log`, `03-test-Core.UnitTests.log`, `04-test-Ctap2.UnitTests.log`, `05-test-Management.UnitTests.log`, `06-test-Oath.UnitTests.log`, `07-test-OpenPgp.UnitTests.log`, `08-test-Piv.UnitTests.log`, `09-test-PublicApi.UnitTests.log`, `10-test-SecurityDomain.UnitTests.log`, `11-test-WebAuthn.UnitTests.log`, `12-test-YubiHsm.UnitTests.log`, `13-test-YubiOtp.UnitTests.log`, `run.log` | 14 |
| `portable-evidence` | `01-windows-hid-verification-build.log`, `02-windows-hid-evidence-test.log`, `run.log` | 3 |
| `verifier-build` | `01-clean-WindowsMerge20261113.VerifierCompile.log`, `02-build-WindowsMerge20261113.VerifierCompile.log`, `run.log` | 3 |

This matters because the receipts identify these files as retained evidence, the plan requires complete
logs, and `validation/summary.md:34–35` specifically relies on the omitted Core log for the actual-source
scan observation. The omitted portable detail log is also the direct evidence for 21 child/coordinator
and 19 publication guards; its RESULT has no project execution rows. A clone of the proposed commit
would not contain these evidence inputs or reproduce the complete receipt-file verification. This is
not a claim that the local software gates failed or ran zero cases.

**Required closure:** retain these exact, already-captured logs in the publication packet with explicit
ignored-path handling, verify their receipt byte counts and SHA-256 values against the staged blobs,
and refresh the manifest, staged scope and final checkpoint. Do not broadly force-add ignored files,
artifacts, `bin`/`obj`, binaries or private evidence. If the execution lead chooses a renamed retained
representation instead, provide an explicit original-path/hash mapping and reconcile receipt access;
silently dropping the files or relabeling hashes is not closure. A bounded additive follow-up review
can close this finding without replaying source review or suites.

## Checks supported by actual artifacts

I freshly checked all 67 file entries across the nine toolchain receipts for existence, byte count and
SHA-256. All match locally; 40 are absent from the index. Each retained toolchain RESULT has passed
targets, zero warnings and exit-zero receipt status. Empty `projects` arrays in build, pack, docs,
complexity and portable results are not interpreted as test execution evidence.

| Evidence | Independent observation |
| --- | --- |
| Clean solution build | Actual clean/build logs and RESULT agree: zero warnings/errors; fresh `--no-incremental` compilation, not a zero-document formatter gate. |
| Ordinary unit suite | Summing the 13 distinct project rows yields 4,620 executed, zero failed and five skipped. Core is 2,236/0/4; PublicApi is 25/0/0. These are subsets, not additional totals. Core's detailed log explicitly shows the actual-source inventory test running and its final successful aggregate. |
| Portable evidence | Detail log records 21 child/coordinator guards and 19 publication guards with zero failures. Two metadata-default checks are separately visible and are not added to the 40-guard claim. No native API or real publication fixture acceptance is inferred. |
| Lifecycle | RESULT and selected integration log show exactly three executed, zero failed/skipped. These are separate from the ordinary unit suite; they are not physical hotplug/listener acceptance. |
| Inventory | Freshly counted 245 unique path/entry pairs, 59 operations, 104 links/104 unique profile methods, and 31 independent required entries/31 public rows. Every family retains `outstanding`, with no `verified` promotion. Links are not execution totals. |
| API | All 20 actual API snapshot files match audited HEAD byte-for-byte. |
| Packages | All ten actual local package hashes match reconciliation. Each archive has one library XML document and one README; its README bytes equal the module's `PACKAGE_README.md`. Local packages are not proposed as staged artifacts or remote package publication. |
| Formatting | Both actual JSON reports are empty and receipts exit zero. Includes are 15 separate Core paths and one Linux verifier path. Retained diagnostic logs show analyzers running on those projects; source/project layout supports their membership. The MSBuild membership reconciliation is retained evidence, not a new evaluation by this reviewer. File-based toolchain inputs and unselected incoming files remain outside the claim. No repository-wide formatting acceptance is inferred. |
| Final docs | Actual console and RESULT show 74 active documents, ten package READMEs, six mapped diagram entries and seven fresh diagram images; both docs targets passed with zero warnings. |

The aggregate verifier project intentionally has no source of its own, but its real Mac/Linux project
references are present and their build log supports the claimed compilation. It is not treated as an
empty source compilation that validates examples or runs platform entry points. No replay of the
unchanged Core001 example campaign is needed for this final narrative delta.

## Final documentation, complexity and protected evidence

Against the prior reviewed/tested product tree, the product-only delta is confined to four Markdown
files: `docs/architecture/raw-access-tiers.md`, `docs/research/documentation-audit/core.md`,
`src/Core/CLAUDE.md` and `src/Core/README.md`. They replace pending inventory/software wording with
observed bounded results; no C# or package README/XML input changes. The additive handoff, status,
validation summary and successor completion section preserve the publication gate and do not
convert managed prerequisites into platform closure.

The two historical Windows JIT bundles were freshly checked at their ledger-pinned summary hashes:
each independently has ten distinct scenarios, 44 scenario checks plus 21 coordinator checks, all
true, and identical 207-file maps in all 11 documents. Each on-disk published-file membership and hash
matches its map. Source pins remain `bdac2f4f4224fa0ccf92006971af8659736aa2b1` and
`0f10775cecfb61f74d20851417746a5816863bb7`. They are unsealed JIT kernel/BCL synthetic-pipe evidence,
not sealed Native AOT or HID-driver evidence. The seven named Windows job-report hashes and the
captured incoming source/PR hashes also match the intake ledger. The first-FIDO-open failure and
original attended 17-pass/1-fail aggregate are preserved, not replaced by later focused passes.
Mac raw artifacts remain unavailable at the recorded location; FIDO/PCSC claims remain PR-summary-only
with OTP permission/expert-topology, loaded-image, later-source, AOT and listener/touch/removal limits.

PIV production matches audited HEAD; the candidate documentation-bearing blob is
`eed0152966b89f8627c4749daa359d2c65b92f59`, while the incoming restored rollback blob is
`526c5da214546c1655ab8f36e2f7e95831cfdbf0`. The receipt explicitly distinguishes these, so the incoming
pin is not misrepresented as the candidate's complete blob. Response-only testing is not a
pre-return pooled-command clearing oracle. F9 attestation preservation and physical PIN isolation,
power-cycle, separate-process access/release, touch/hotplug/cancellation deferrals remain intact.

The actual complexity report against audited HEAD contains precisely three actionable findings:
`CompleteRequest` 15/18 (new), `Advance` 14/14 (new), and `OpenBuiltInConnectionAsync` 12/1 (worse).
Against incoming there are 17 unchanged violations and zero actionable new/worse findings. The
affected production files retain incoming executable lines after ignoring whole-line comments and
blank lines. The intended message contains exactly the three incoming justification lines below,
with actual rationale rather than placeholder waivers:

```text
feat(core): integrate Windows native owners with the audited SDK

Merge the received Windows HID and adapter-consistency delivery with the
published audit history. Preserve audited runtime corrections and frozen
records; reconcile current ownership guidance and source-bound software evidence.

Complexity-Justification: HidConnectionSlot.OpenBuiltInConnectionAsync: Keep the six platform/transport routes and their claim transfer visible in one flat switch; cyclomatic 12, cognitive 1, with no additional lifecycle state machine.
Complexity-Justification: WindowsFidoHidNativeOwner.CompleteRequest: Keep cancellation/error reconciliation, exact retirement, owned-buffer clearing, and actual public task publication together so their ordering remains auditable.
Complexity-Justification: WindowsFidoHidNativeOwner.Advance: Keep the submit-return/completion join and once-only worker cancellation in one state transition method; these branches encode the frozen independent native observations.
```

## Scope, preservation and publication boundary

The manifest exactly covers the current 256 staged no-rename path spellings: 135 product/incoming
tooling paths plus 121 RUN records including the manifest. Every surviving working-file hash and
filtered Git blob OID matches its manifest row; deletions remain absent. Two `.csproj` working files
have CRLF that Git normalizes to LF; the manifest's raw-file hashes and separate filtered blob OIDs
correctly identify both representations. This is not corruption. The manifest explains its own
self-exclusion, and its reviewed hash/blob/tree above supply the external identity.

There are no unmerged entries or tracked unstaged changes. Fresh staged diff checking passes.
No staged `bin`, `obj`, `__pycache__`, package binaries, secret-key files or unrelated path was found.
The index's clean scope does not cure the ignored-log omission.

Fresh on-disk preservation reproduces 13,666 frozen files across the three protected roots:
Core Pass-1 (5,226), prior Core integration (8,326), and prior PIV integration (114), with their pinned
fingerprints unchanged. All four original user-file hashes/byte counts match. The original checkout
remains at audited HEAD with no tracked or index delta. All 33 preparation records other than the
authorized status update remain hash-identical to `execution/pre-merge.json`.

After the Important blocker is closed and the revised checkpoint accepted, execution belongs to Sol;
Astra orchestrates only. Publication target is **only**
`origin/yubikit-windows-audit-integration-20261113`: a normal two-parent commit with the two pinned
parents and intended message, no force push, no audit/upstream branch promotion, no original-checkout
change and no PR. Fresh remote OID readback remains a post-push execution gate, not a fact established
by this static review.

This record is intentionally additive and unstaged at reviewer handoff. The execution lead may stage
it afterward and retain a successor closure record. Its addition changes the final tree but does not
require the record to hash itself, the manifest to include its own hash, or a commit to predict its
future OID. Review-record closure must not silently alter the already reviewed product/source.

## Reviewed input SHA-256 identities

Paths below are RUN-relative unless shown as root/voice inputs. The manifest pins all remaining
reviewed packet contents; the receipt identities pin their individual local files, including the
40 omitted logs identified above.

| Input | SHA-256 |
| --- | --- |
| Root `CLAUDE.md` | `93b24692c2c9ad11e13f9c349a30eff650e6285430fb80d3b1aeb14f47050e5d` |
| `docs/research/documentation-audit/records/style.md` | `2b8882adb75b18d67adcd71d38a6c70fb4aa7c44ec3211c0d45455ab94e9ca15` |
| `coordinator-decisions.md` | `9e01a1c47c4556418f7898e4752d79688eb800742f6b54b4335c328a2a0023c5` |
| `execution/final-plan-snapshot.md` | `8327b1b05f4a891d73641c04775f1e8eaf5643efb14e196eb46e38ebb8b8770f` |
| `execution/publication-manifest.json` | `2e0651bca9cb2696929ba239ed250ffc940c0b4d043fa132dc9d97bc2df0f5a1` |
| `execution/intended-commit-message.txt` | `0bb4e6e88c95e1466f48396c3eadec3b0f35e69a71a51d753c1cfe65531f1b5b` |
| `execution/pre-merge.json` | `6112b7a517ff42f97e7903df35fc0849bb0da25e86cd0fa7d76ce67f6d37fc54` |
| `execution/final-candidate-preservation.json` | `feec539a45537ad8f02838e9239e116cccfa7429651a7659eda46862ba51410f` |
| `review/source-review.md` | `82490bbe7ef6374cdca67fb41523ab7120dbd25c06b3b97dd11a9770c37ea003` |
| `review/documentation-review.md` | `edb2e46f03baa1fa28b663c377f7aa5ad3548580c4d36988d6e19c1243b40aec` |
| `handoff.md` | `7a64cd4a0ba415136d00d9343ac92cfc21851639b202c61eb25cba1a8ad70e9c` |
| `status.md` | `7ec80ea446c872b349f77a51498b1697e75a2c891050ee2f3bf5c04a61ff249c` |
| `deferred-findings-successor.md` | `7af060c11d92a253a9bc12e98845a3942d382cf89ddb77a8e5c5620068948a59` |
| `validation/summary.md` | `0c17a3b8c5adea46546c8f259451a2e28b30adc24ff54195202ae48887a50358` |
| `validation/evidence-reconciliation.json` | `677443d245f70cb56233aa439073f86d8e18d4b670c71a65da24e628b39fd993` |
| `intake-review/evidence-identities.json` | `00d82d8e1912ec984d6c028be72da2a2812cb89aa92c047ef4c43c1e79b3df87` |
| `validation/clean-solution-build/receipt.json` | `db4a8cb70967cbff04216f16ada6bcdc89da21fbd32a179162c02b87ce309fb7` |
| `validation/complexity-from-audit/receipt.json` | `9e14160cc17118befa6d46cea58b5a677a6b8c8c7cd1d9fbd9914806deaf72da` |
| `validation/complexity-from-incoming/receipt.json` | `255339f5c1a663c6196ec024750e0f2db1ff9b5e13d1464a0da25bc91567b427` |
| `validation/device-event-lifecycle/receipt.json` | `38e54b42af832e08dfa2b7db0f3fb941bcfe9364af0edab5bffff88a68539d63` |
| `validation/docs-inclusive-pack/receipt.json` | `1ba6f0d3a5b5826465b5416e2329da19809e71ea28b22100f090543e3d7aa21c` |
| `validation/final-documentation/receipt.json` | `f1cf856526487bcfd406689947f412672d27133be4c38a926e0231e95406db30` |
| `validation/format-core/receipt.json` | `a0f757b753ff2ec78be75cf7bb49212e2b8cd7fa746653694ea91af118b658e4` |
| `validation/format-linux-verifier/receipt.json` | `8e18bd4fcca8e874837452fa888338e6d57405520ed1b77606bfbdc911cc4fd2` |
| `validation/ordinary-unit-suite/receipt.json` | `27977a65bc02af6494a73887d2d6530459b374beb1dc2ad9e8a07d7c60af6499` |
| `validation/portable-evidence/receipt.json` | `fb074c7e85d418364ba3649f65647a77c94a32a35d7583b31f9dbb57446ff74e` |
| `validation/verifier-build/receipt.json` | `f9460c1d201d7bded315c2433cb9a96770db87d5b8b10e33d6a72a4fa10b530f` |
