# OpenPGP documentation handoff

Pass 1 content, independent content/citation re-review and execution gates are complete as
[openpgp-001](modules/openpgp/pass-1/openpgp-001/plan.md), baseline
`fe180282a2d77b15eaaa5868d6c8b00e251d1f05` on yubikit-docs-audit.
See [current status](modules/openpgp/pass-1/openpgp-001/status.md).

Whole-module scope includes public contracts, guides, internal/example/test explanations and relevant
shared OpenPGP claims. Sol 6.1 investigates, edits and independently reviews; Astra coordinates and
validates. User authorized connected-key tests and configurations; Python YubiKey Manager and its history
are corroborating references. House style emphasizes affirmative task-oriented prose with useful context.
Implementation follow-ups accumulate for Pass 2; Core dependencies are reserved for the final Core cycle.

Current result:96 module paths,39 public types/423 API entries/40 operations,59 accepted authored paths;
49 C# paths preserve bodies/directives/imports and all20 API snapshots are unchanged. Expanded clean
sampling11/32 passed after corrected PinPolicy and timestamp misses. Clean repository gates passed
3,998 unit tests including186 OpenPGP, five existing skips and zero compiler/analyzer warnings/failures.
All17 complete example groups and the full19-source OpenPgpTool compiled with actual Csc/live evidence.

All49 existing integration methods passed. Nine supplemental methods retain five successful measured
outcomes and four contract failures; independent same-key X25519 and correct-CRT ATT references passed.
All original setup/mode/touch-window failures and the interrupted initial suite invocation remain
preserved. Final independent restoration matches every initial public-state field, including Att
certificate identity and allfour UIFs Off. Device scope:serial103, USB SmartCard/Linux, metadata5.8beta;
SDK/direct Python0.0.1 and CLI5.8.0 remain distinct observations.

[Findings](modules/openpgp/pass-1/openpgp-001/findings.md) preserve14 deferred engineering groups and
four Core boundary questions. [Closeout](modules/openpgp/pass-1/openpgp-001/closeout.md),
[hardware results](modules/openpgp/pass-1/openpgp-001/hardware/results.md) and
[example validation](modules/openpgp/pass-1/openpgp-001/snippet-validation.md) preserve exact scope.
Fresh final evidence review passed with zero required corrections. Manifest freeze and verified
publication remain pending.

## Publication staging evidence

The frozen run contains607 artifacts (606 text files and one original PDF),10,666,111 bytes excluding
its manifest. The first strict index-versus-working-byte check stopped at
`src/OpenPgp/src/Yubico.YubiKit.OpenPgp.csproj`: repository attributes specify `text` and `eol=crlf`,
so Git stages LF-normalized text. This is the sole publication byte-normalization exception:

| Representation | Bytes | SHA256 |
| --- | ---: | --- |
| Accepted/compiled working file (19 CRLF,20 LF) | 864 | `d150bc39c7c4545e4cf9b87ed2ad25e967c6116211d66f6026638306a0ac68af` |
| Staged Git text (0 CRLF,20 LF) | 845 | `95796651ce74b03ad736c901cc4dfbe993bccbd45528d97ffd83f96674f94540` |

Replacing CRLF with LF yields exact equality; the accepted Description remains the only semantic
project change. Full original working project bytes remain in compiler source/config snapshots within
the frozen archive. No product or archive bytes were changed to accommodate staging. The subsequent
index check permits exactly this named/path/hash/attribute normalization and still requires exact bytes
for every other staged file. Fresh-checkout validation must account explicitly for repository line-ending
conversion while retaining the recorded original compiler-input identity.
