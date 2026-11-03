# OATH-001 investigation checkpoint

Baseline: `ddf3cdc24984f598678b48f2a0f98d6be1a58bf9` on `yubikit-docs-audit`.

## Result

Sol 6.1 returned eight supported findings. Astra checked the consequential implementation paths against the original code and official YKOATH protocol, and refined two editorial dispositions to avoid overstating the problem or adding unnecessary warnings.

| ID | Finding | Proposed next action |
| --- | --- | --- |
| F01 | Missing authentication prerequisites and borrowed-buffer contracts | Document verified workflow; hold nonstandard-key contract open |
| F02 | DeriveKey XML contains development-transition narration | Replace with present-tense contract |
| F03 | Host-side password derivation called hardware-dictated | Distinguish interoperability convention from device enforcement |
| F04 | Password changes grouped with irreversible credential deletion | Describe specific effects instead of a blanket label |
| F05 | Sample comment lists absent remember/forget commands | Correct the comment to match current CLI |
| F06 | Sample password paths drop caller cancellation | Separate implementation follow-up, including exception handling |
| F07 | VALIDATE raw response array is not explicitly cleared | Separate sensitive-buffer cleanup fix with regression verification |
| F08 | WrongPassword combines user-key rejection, device-HMAC mismatch, and status failures | Resolve intended error contract and obtain firmware evidence |

The detailed ledger is `findings.json`. E1 tracks accepted key lengths, E2 tracks authentication-failure provenance, and E3 tracks the response-cleanup follow-up. Owners are proposed roles, not assigned people or created issues.

## Recommendations at the checkpoint

1. Proceed next with the five documentation findings and the approved IsLocked/DeriveKey prose examples, after user feedback on this checkpoint.
2. Keep implementation changes in separate work. Prioritize F07's explicit cleanup omission; address sample cancellation with focused evidence.
3. Keep F08's error-contract claims open until protocol/firmware evidence and intended semantics are reconciled. Do not label the discrepancy a deliberate deviation without a decision.
4. Document the normal 16-byte derived-key workflow without claiming that all other lengths are supported or changing accepted inputs.

## Pilot observations

- The bounded task produced actionable stale-text and API-contract findings, plus implementation issues that prose alone cannot resolve.
- Required repository guidance and necessary Core dependencies consumed substantial context. Future packets should retain mandatory instructions while distinguishing primary scope from evidence expansion.
- Sol supplied original-source references, test-read evidence, and explicit unknowns. One batch does not establish a model-quality or cost benchmark.
- A code-read checkpoint is not a runtime verification result. No clean-sample independent review has been claimed; that remains a later cycle stage.

## Coverage and validation

- Inventory: 46 tracked OATH files; 38 Markdown/C# surfaces (4 Markdown, 34 C#); 23 external reference files discovered lexically.
- This batch reviewed authentication/password semantics, with reading ranges recorded in `investigation-coverage.json`. Other claims in shared files remain open.
- `dotnet toolchain.cs -- docs-qa`: passed, 73 active docs and 10 package readmes, 0 warnings.
- No SDK build, tests, or hardware verification performed.
- No source/documentation edits, commits, pushes, or pull requests made. Existing untracked work remains untouched.
- Actual per-task model usage/cost is unavailable from the dispatch tool.

## Records

- `baseline.json`: revision, initial working-tree state, model/session, inventory counts.
- `active-docs.txt`: canonical active-doc list.
- `coverage.json`: full pilot inventory and pending file-level coverage.
- `mechanical-signals.json`: 15 unverified pattern signals, not additional confirmed findings.
- `dispatch.md`: bounded role packet and prospective remaining units.
- `investigation-coverage.json`: semantic reading, supported claims, gaps, and coordinator checks.
- `findings.json`: evidence, dispositions, and unresolved decisions.
- `validation.json`: commands/results and limitations.

## Subsequent authorization

The user approved proceeding with documentation editing and requested implementation concerns be tracked for the end-of-pilot review. See [implementation follow-ups](../../implementation-followups.md). The user also requested check-ins between phases: report after editing/independent review and after validation, before expanding to the next coverage batch.

The pre-edit checkpoint above remains a record of the initial investigation, not the current completion state of the batch. Subsequent edit/review/validation records will record their actual outcomes.
