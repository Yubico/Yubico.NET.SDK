# Independent review — SHARED lifecycle sampling correction

**PASS for the exact focused patch.** No mandatory correction remains in the reviewed sampling delta.

- Patch SHA-256: `37074361c54bb2232ff8406ae37af482a73c51b029a3dfd8328e467f697e274e`.
- Accepted path: `docs/architecture/event-driven-device-discovery.md`.
- Accepted after SHA-256: `22a4d6e06ac319934bb541abc910286831a99f3159510c2e1b392ac22bf3c70c`.
- Accepted predecessor SHA-256: `4c841d4992e626de8d504ca8c776d696c3145fc818d1cc93e9082cd98bd2bc60`.

This focused review addresses SAMPLE-A001/A003 and Astra's actual sample
`core-001:unit:cf1e2081af8b8ac4`. The edited overlap receives **zero clean-sampling credit**;
sampling records and expansion remain coordinator-owned. The original full-26-path acceptance and
revision-2 patch remain preserved predecessor evidence, not identities relabeled for current bytes.
Only this review and [review-identities.json](review-identities.json) were written.

## Source and content decisions

The full before/after guide, complete focused patch and actual complete `HotplugSignals`,
`YubiKeyMonitor` and `DeviceChangeFeed` implementation files were read. Source evidence was checked
against actual files, not accepted from its observation table alone.

| Corrected claim | Actual source / focused decision |
| --- | --- |
| Listener startup and cleanup attempts, after-guide lines 129–134 | **PASS.** `HotplugSignals.cs:36–45,50–80` iterates listener sources, handles start/status failure and attempts cleanup for an acquired listener. `BestEffort:100–109` catches cleanup failure but its warning logger is unguarded. `Dispose:83–98` separately traverses callback clearing, stopping and disposing before channel completion and final logging. The guide now states attempts and explicitly conditions continuation on logging returning. It promises neither successful release nor that another listener always starts. |
| First-publication failure and cleanup, lines 139–143 | **PASS.** `YubiKeyMonitor.CreateAsync:109–124` constructs before its try, starts the loop, awaits the first-publication result and propagates its recorded failure; its catch awaits disposal before rethrow. An exception from that await replaces the earlier failure. The guide's conditional wording applies to the constructed monitor's failing/canceled first-publication wait, not every constructor failure, and no longer guarantees original-error preservation or complete partial cleanup. `RunAsync:136–170` supports initial failure reporting separately from later scan retry and terminal feed failure. |
| Ordered disposal versus guaranteed completion, lines 150–165 | **PASS.** `YubiKeyMonitor.DisposeAsync/DisposeCoreAsync:189–204` caches one task and calls signals disposal before cancellation, loop await, token-source disposal, discovery retirement and feed completion. No finally advances skipped stages. A listener cleanup logger throw can prevent later work; the final monitor log can fault disposal after feed completion. The corrected numbered path and adjacent conditions describe that ordering without guaranteeing every stage, bounded total disposal, retries or native release. No-publication-after-feed-completion is tied to actually reaching the post-loop stage. |
| Healthy completion versus pre-existing error, lines 108–115,166–171 | **PASS.** `DeviceChangeFeed.Subscribe:65–95` checks `_completed` before `_fault`, so later enumerations after Complete end empty. `Fault:98–109`, `Complete:112–122`, `Deliver:154–162` and subscription `Complete:164–168` distinguish feed failure, per-subscriber overflow and normal completion. Retained dotnet/runtime **v10.0.0** `BoundedChannel.TryComplete:305–360` returns false once `_doneWriting` is set and does not replace its prior error. Thus already-faulted channels retain their outcome after any replay/buffer drain; only healthy uncanceled streams can drain and end normally when disposal reaches feed completion. The guide now preserves those conditions. This source check is not installed-runtime or fault-injection evidence. |

The primary checkout's captured monitor XML differs from the guide worktree's XML, but the inspected
relevant implementation ordering agrees. The separate A C# sampling corrections are not transferred
or counted as work in this review; no duplicate source/XML edits were made.

## Independent static verification

Fresh read-only checks found:

- The before bytes match the predecessor's accepted path hash; after bytes match both the actual guide
  and the requested after hash. Reconstructing the conventional before-to-after unified diff produces
  the exact supplied focused patch and SHA-256.
- Exactly **one** of the predecessor's 26 accepted guide paths differs; all other **25** path hashes match.
- All **34** prior SHARED editor/reviewer/report preservation entries match their hashes, including
  the original and revision-2 product patches and full-26-path acceptance records.
- All **7** source-evidence entries match actual complete source text/hashes or the exact retained-runtime
  excerpt. The guide worktree's three implementation files were read in full; their primary counterparts
  were independently compared with the captured complete text. No runtime change is inferred from XML differences.
- The sole fence remains at lines **25–33**, with unchanged complete body SHA-256
  `63a69f9739206bd38ed7f95780af89d803f8a4408323d7b7870e3abf3cf48fbb`.
  No example code or compilation eligibility changes in this patch.
- `git diff --check` and read-only focused reverse-application check each exit **0**.
  HEAD remains `da66f5d01ba594993974ddd26723e8c300c0a454`; the Git index is unchanged.

The identity record binds the exact focused artifacts, source evidence and predecessor acceptance.
This PASS accepts the one-file sampling correction only. It adds no clean-sample credit, compiler/test
execution, throwing-logger reproduction, leak/release proof, platform/device qualification, or general
acceptance of unchanged lifecycle commentary elsewhere.

No product or prior-record edit, new tooling file, build/test/compiler/formatter, native/device/crypto
operation, Git mutation or subagent action occurred. Prior implementation, source/profile, provider/native,
Windows and PIV physical qualifications remain intact. Final sampling reconciliation, boundary cross-checks,
validation, publication and the precise deferred-owner handoff remain coordinator gates. Finish Core Pass 1,
then stop; no broader audit or implementation project is started here.
