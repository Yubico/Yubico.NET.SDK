# Astra B2/B3/B4 expansion and replacement readings

Read all 27 whole units in `expansion-B2/original-units.md`, both new B3 units and all five B4
units. The seven repeated B3 activation excerpts are the same IDs already read in the B2 file;
they receive no additional read or sample count. Final changed-span mapping remains pending.

## B2 XML expansion and three eligibility replacements

Content supported in the stated source scope for:
`9d8102989bf52740`, `f00b1d2526f62fe3`, `e5b5533e4df67ea7`, `727ad82dd732b613`,
`1dcf2e836efee1d5`, `9e1f0228fabdcb5d`, `f82d55d29d5e7e42`, `fb2be78aaf8f6f25`,
`e9d68bf588ab00e4`, `1f646b31d8d5f48a`, `dd179ffbb8315a43`, `d5ffab3651d9f944`,
`e954bf0af6c49fba`, `fb09fe35ad8505f1`, `418fbc28a73c4bd5`,
`9b2517c252b41c26`, `21cfb83cdc2b4263`, `1dc08e7d03f341ef`.

The complete send/receive implementations preserve admitted-copy ownership and separate caller
buffers. Reactor completion and quiescence remain distinct from owner/device close. Udev events
are copied from a scoped native device. Readiness is an instantaneous openability check, not a
reservation against later changes. Initialization failure leaves cleanup with the owning listener
session; no all-stage cleanup or wake acknowledgment is inferred.

- `cf330b6a639970cd`: **edited overlap**, zero clean credit. Its “Each Start” wording was already
  corrected by the accepted Linux sampling patch to account for the started guard.
- `82f001f6db22d507`: **source-qualified**, zero clean credit. Null receive and possible event loss
  are supported; “most likely” event-storm frequency is not independently established. Preserve
  the uncertainty rather than treating the likelihood claim as verified native evidence.

## Seven activated historical-scope replacements

| Unit suffix | Verdict |
| --- | --- |
| `4c48c7d8d8d2e37a` | Supported concrete/group flag semantics; “cache” does not introduce a new public cache API. |
| `4c955b22a2de68df` | Historical build/result receipt and specific artifact-count claims: read as qualified evidence, zero clean contract credit. |
| `0b2135f2f301e2c1` | Historical test/inventory accounting: qualified evidence, zero clean quota credit. |
| `d8f176c067aa0fd3` | Supported current source mechanism, despite research-file location: original-worker drain, registration release, checked close and retained failure are independently distinguishable. No physical result claimed. |
| `b45b5794aa92008c` | Supported source mechanism: one poller, eventfd-only retirement, pending admissions, bounded wake retries and independent owner release. The explicit local-source classification is not a hard syscall deadline. |
| `4330e60d07cd5417` | Historical physical checkpoint and measurements: qualified evidence only, zero clean quota credit. |
| `457a4879daaf1b55` | Supported source mechanism: shared owner completion, original-worker cleanup and finalizer request-only behavior; known native interruption/release limits are explicit. |

This distinction follows the content of each unit: a current control-flow explanation can be checked
against source, while a historical test run, inventory count or measurement requires its own evidence.

## B3: two new XML units

- `e3d8ada85fd182e2`: supported synchronous report-connection factory; native open can fail, and
  this API supplies no asynchronous drain guarantee.
- `76b664da02352c87`: supported registration/clear contract; callback registration is not proof
  of quiescence or checked native release.

## B4: four narrative additions and one protected-output replacement

- `f5a358ec89b4c4ce`: administrative documentation-maintenance policy; zero clean contract credit.
- `9a180bff18d16922`: installation/feed/package recipe read; current-feed availability is a
  publication prerequisite, not inferred from these commands alone. Source-content supported,
  pending the final package/document checks.
- `7070cc291aec3e7e`: administrative module/router preamble; zero clean contract credit.
- `db781bee61b48612`: project programming-policy guidance read; recommendations are not performance
  measurements or promises that init/read-only views deep-freeze mutable storage. Supported at that scope.
- `7dc8f2fc14f0186c`: package-source mapping and credential-location comment agree with the XML;
  restored package selection/publication remain separate checks.

All suffixes use `core-001:unit:`. No compiler or native/device operation was run by these readings.
Replace the explicit exclusions/qualifications without silently narrowing the broad denominator.
