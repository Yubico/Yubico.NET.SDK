# YubiOTP validation

## Current checkpoint

Independent content/citation/correction and hardware-impact reviews passed for all50authored paths.
Hardware16existing methods+4targeted probes passed; final slots empty. Final source preservation and
coverage reconciliation passed. Full final solution gates and complete-example compilation passed.
Fresh final evidence review passed in review-closeout.md; publication remains coordinator-owned and
will be recorded outside the frozen run.
status.md is the current progression authority; original phase records remain historical.

## Source and archive preservation

Unique AuditOtp001.Checker built warning-free using toolchain; proven strict Roslyn algorithms reused
with explicit baseline/run/output identity changes. Both initial and corrected pre-format comparisons
passed42C#paths. Formatting applies only to those42actual solution documents, including OtpTool.

Actual changes: EOF newline normalization, one import ordering/group-blank change in
YubiOtpSessionIntegrationTests.cs, and two unused ordinary imports in IYubiKeyExtensions.cs:
Yubico.YubiKit.Core.Protocols.SmartCard.Apdu and .Scp. No alias/global/static/conditional import removal.
Full pre-format sources, actual diagnostic/apply logs and exact transform hashes bind those changes;
final Roslyn comparison passed42files with one two-import exception. Declared API/executable body/
directive semantics remain preserved; do not claim literally identical syntax tokens for removed imports.

Initial transform recorder rejected the one import-group blank-line removal. Original recorder and
failure are retained; corrected recorder allows only that named change, not arbitrary whitespace/code
normalization. All20API snapshot bytes/pathsets match baseline. reviewed-files.json binds50accepted
content hashes through formatter changes; coverage-final.json reconciles63module paths,45edited and
18unchanged,24public types/193entries. Final clean sampling3/21remains valid.

Prior archives784/402/534/164 and four original user files passed initial checks; final checks will repeat
before publication. Three exact primary-source PDFs are deliberately retained with explicit identities;
all generated binaries/caches are excluded. Tooling preparation's text-only assumptions are superseded
by corrections.md and primary-binary-sources.json; no prior frozen archive/tool was modified.

## Hardware

See hardware/results.md and full validation integration/probe logs. The first suite was blocked before
tests by reader contention and preserved as0executed/15skipped, not silently treated as passing. User
released reader;15non-touch tests then passed, followed by four probes and human-coordinated successful
touch. All20successful method invocations reported zero warnings/skips/failures. Initial/pre-format
product behavior was baseline-identical; formatting changes are separately proved harmless. Device
metadata5.8.0.beta.0/rawOTP0.0.1, serial103, Linux, explicit HID/USBCCID scoped. No NFC readback, older
firmware, keyboard IMF output, access-code probe or all-platform qualification is implied.

## Final gate plan

After scoped final formatting: clean full solution build/unit tests, docs/package QA, architecture and
complexity through `dotnet toolchain.cs -- build test docs-qa docs-architecture complexity --clean`.
Existing integration behavior already executed; documentation/formatter-only changes do not require
repeating state mutation or human touch absent changed executable code or new unresolved concern.
Compile final full documentation blocks in genuine reference/analyzer context and actual OtpTool.
Retain commands/results, compiler membership and identities; compare final hashes/dependencies.

## Final repository result

`final-repository.log` and retained run20261104-113931-770 record the clean full-solution build and all
unit projects: **3998executed,0failed,5existing skips**, including **201YubiOTP unit cases**. Compiler/
analyzer warnings0; all13test projects passed. DocsQA73active documents/10packageREADMEs passed;
architecture6map entries/7image freshness checks passed; complexity changed-line scope19files/4methods,
all within thresholds. Static hardware-test and NativeAOT-contract gates passed. No diagram edits or
AOT publish claimed. Scoped final formatter exited0; workspace duplicateAPI source loading diagnostics
are separately retained and not compiler/analyzer build warnings.

Hardware probe's225actual compiler dependencies (205references,12analyzers,5sources,3configs) match
the final solution identities. Actual Csc membership and /warnaserror+ plus real pinned analyzer paths
verified; full source/config/asset snapshots retained in hardware-probe/evidence/final-inputs-v2.
First retention checker misclassified /embed:*.cs arguments as extra compilation units and failed closed;
original script/failure retained. Corrected check separately verifies embedded source paths are a subset
of actual compiler sources, with exact source membership still required. No hardware rerun was needed.

## Final examples

See snippet-validation.md:19documentation groups compiled in2genuinecontexts and full14source-file
OtpTool compiled/captured separately. All624reference/analyzer identities and full raw/wrapped bodies,
Csc membership/source/config/assets match final current inputs. No documentation/sample host executed.
Late-capture-only overlay resolvedMSB3540without shipping/toolchain edits or repeated accepted consumer
compilations. Original failed hook/preparation and corrective provenance retained.
