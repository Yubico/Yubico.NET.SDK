# Core B1–B4 documentation edit

## Outcome and boundary

The B1–B4 primary C# documentation edit is ready for coordinator integration and independent review.
The coherent source patch is retained in `editing/transports/source.patch`. This is a documentation
return, not runtime acceptance or publication of the whole Core campaign.

Worktree: `/home/rupicola/Code/y/yubikit-core-docs-edit-transports`; branch:
`yubikit-core-docs-edit-transports`. Base and unchanged HEAD:
`da66f5d01ba594993974ddd26723e8c300c0a454`. Product source pin:
`fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. Per-file Git blob IDs, base/current SHA-256
identities and explicit dispositions are in `editing/transports/file-dispositions.json`.
The hashes distinguish Git content, current worktree bytes and LF-normalized content.

Only primary `.cs` paths under `src/Core/` in the four named packet JSONs were edited.
The return covers all **213 eligible files: 62 edited, 135 retained and 16 held unchanged**.
All 122 candidate-associated eligible files have an explicit decision, including the editor's
explicitly linked KeyboardLayout reference dependency under B4-C010/FPT-057.
All executable lines, directives, imports, declarations, defaults, attributes, output/help strings,
assertions and API snapshots remain unchanged. The static receipt compares every nonblank,
non-full-line-`//` line verbatim, retaining inline comments and existing block comments in that
comparison. This is a narrowly sufficient preservation check for this patch, not a C# parser or
an independently qualified compiler check. Original BOM and final-newline policy are preserved.

The initial working tree was clean. No primary-checkout files, shared guides, external C# files,
README/CLAUDE files, applet documents, historical records, configuration or snapshots were written.
No build, test, formatter, native library, device, crypto operation, index/ref mutation, commit, push
or subagent was run. Record serialization, source reads and static Git/text/hash checks were the
only execution. Scratch and records stayed on home-backed storage.

## Record map

| Record | Purpose |
| --- | --- |
| `editing/transports/file-dispositions.json` | Every eligible file, edit/retain/hold decision, local/proposal IDs, evidence and before/after hashes |
| `editing/transports/proposal-dispositions.json` | All original B1–B4 candidate/proposal identities, inherited aliases, exact original unit/range/hash links, applied and held facets |
| `editing/transports/followups.json` | Exact shared/external paths and original unit identities, grouped by the original proposal and future owner |
| `editing/transports/example-bodies.json` | Complete original/current XML code bodies, classifications, hashes, scopes and unrun compilation status |
| `editing/transports/source-citations.json` | Retained originals reviewed for consequential edits, their hashes, passages and authority limits |
| `editing/transports/static-preservation.json` | Fresh bounded preservation result and counts; not runtime validation |
| `editing/transports/source.patch` | Complete primary C# diff against the named base |

The immutable full-family map supplies 37 local B1–B4 candidates in 36 proposals,
`FPT-022` through `FPT-057`. `CORE-B1-C010` and `CORE-B3-C006` remain one nonadditive
ambient-context policy question. No accepted finding IDs are allocated or historical IDs renumbered.
Every eligible file is dispositioned, including all candidate-associated unchanged files. A HOLD
retains the file while withholding the stronger claim identified in its record; an edited file can
also retain held runtime or qualification facets.

## Applied documentation

- **Portable admission and ownership:** registry remarks now describe sequential sorted acquisition
  and rollback, including an earlier discovery wait before a later held-member check. Registered
  SmartCard and Windows/custom HID failure policies are distinct. A shared disposal outcome is not
  generic native-release proof. The generic result factory explicitly leaves successful ownership
  to its result/caller, with a complete example returning the actual connection into an enclosing
  disposal scope before creating a borrowed raw session.
- **PC/SC state and cleanup:** sharing documentation explains resource-manager exclusion and shared
  application-state interference without a universal PIN=ONCE inference. Release requests UNPOWER
  and checks native statuses; neither a fake argument assertion nor that request measures physical
  power-off or PIN-state clearing. Transaction remarks separate same-connection workflow serialization,
  native end completion, scope diagnostics and later connection-disposal reporting. Post-open slot
  logging remains an explicit exceptional-path finding, not a repaired ownership path.
- **Enumeration and listeners:** finder XML describes one-shot interfaces and actual cancellation
  boundaries. PC/SC listener comments identify the maximum wait and immediate UNAWARE return, rather
  than a guaranteed polling delay. Timeout, finalization and restart wording no longer promises
  successful drain. Listener interface documentation also names the implementation differences after
  disposal instead of promising a uniform ObjectDisposedException.
- **Linux:** current reactor reference scope replaces future-tense comments. Udev text conversion,
  borrowed parent lifetime and pointer-returning native unref are documented without altering imports.
  The unversioned `libudev.so` deployment requirement is qualified by the retained Ubuntu noble file
  lists. Direct factories state synchronous waiting, caller disposal and no grouped claim; descriptor
  parsing describes first-pair extraction followed by classification, not whole-descriptor validation.
- **macOS:** nullable-property XML names InvalidOperationException and distinguishes absence, stored
  zero and lossy/out-of-range conversion. Direct IO explains descriptor metadata versus fixed 64-byte
  FIDO normalization; expert feature GET explains eight-byte padded storage versus typed short-result
  rejection. Callback documentation separates rooted generation context, persistent native buffers,
  normal late drain and no-worker partial startup. Native enum gaps and CFNumberType width remain
  explicit implementation findings, with raw native values and no changed constants/signatures.
- **Windows/support:** comments describe synchronous native invocation, temporary SafeHandle borrows,
  registry-release limits, separate staging arrays, operation-specific access and environmental ACL/
  collection/driver conditions. Historical runs are not promoted to current acceptance. Native ABI
  commentary names LPGUID, 32-bit PULONG, BOOLEAN versus BOOL and signed managed caps without claiming
  an observed architecture failure. Loader comments distinguish dormant support code from active shim
  resolution and explain delegate/library lifetime. Helper, exception and keyboard XML now describes
  actual return types, raw-status provenance, table aliases, shift markers and locale limits.
- **PC/SC ABI:** the managed PCI remains eight bytes. The published 1.18.1 pointer-forwarding path and
  selected Linux LP64 pcsc-lite two-unsigned-long layout are a conditional discrepancy, not a universal
  mismatch. Windows' two-DWORD layout and the separately selected macOS framework stay distinct.
  Reader-state packing follows the published wrapper; string boundaries remain implementation findings.

## Test commentary and examples

Existing assertions and executable test names/output are retained. The corrected commentary identifies
the complete local oracle rather than suggesting the test was strengthened:

- `SmartCardSharingTests` checks mode selection, retry policy, snapshots and requested disconnect
  disposition with fakes, not competing-process access or card state.
- `PcscConnectionLifetimeTests` holds transmit before scope end, observes end-call counts and sequential
  synchronous begin, but does not hold native EndTransaction itself. Its failed-end vector observes
  normal scope return, ordinary-call refusal, later SCardException and cleanup call counts separately.
- `DesktopSmartCardDeviceListenerDisposalTests` observes the calling thread around abandonment and GC;
  it neither observes finalizer entry nor catches a finalizer-thread failure.
- HID disposal timing tests compare Task.WhenAny without awaiting the selected disposal result. A fast
  fault wins too. HID quiet-period counting can pass after failed Start; PC/SC quiet counting measures
  callbacks, not native cadence. The PC/SC integration disposal test actually awaits its task and is
  retained; it is not incorrectly grouped with the HID race-only oracle.
- Session/SCP tests retain their local ownership rationale and assertions while removing unversioned
  upstream, Python and Rust authority claims. No comparator research or runtime interpretation was added.

There are three current XML `<code>` bodies in the eligible C# scope: one new generic-result example
and two updated factory examples. The first factory example is a complete top-level startup policy/
discovery snippet with no owned disposable result. The two composable methods take actual typed
device/reader and cancellation prerequisites and dispose raw session before connection. They contain
no fake helpers. Compilation remains **VALID-NOT-CHECKED**; the coordinator should reuse its existing
qualified consumer tools. The inventory preserves the two original bodies as well as all three current
bodies. Native C signatures in ordinary comments remain reference declarations, not runnable consumer
examples; the complete commented-out LinuxUdevScan remains inactive history, unchanged.

## Held claims and deferred implementation

The precise per-proposal reopening conditions remain in the JSON return and original candidates.
Important limits for publication are:

1. **NIST versus PC/SC originals:** B1's NIST SP 800-73-5 Part 2 §2.4.2 text was retrieved through Exa
   after direct PDF HTTP 403. No NIST PDF bytes were independently reviewed here. The unsupported
   inference was removed without publishing revised normative PIV wording. Independent original-PDF
   review remains held. PC/SC Part 5 revision 2.01.01 is a separately retained original PDF with a
   hashed text derivative; its desired-disposition and if-possible language was reviewed separately.
   Preserve `audit-v2-20261022-20261031:#22`, `piv-001:PIVCORE-001` and the PIV physical-test deferral.
2. **Portable runtime work:** early enumeration context exits, listener partial allocation/startup and
   timeout/restart behavior, post-open logging cleanup, Windows/custom wrapper release, held native-end
   coverage and ambient ExecutionContext policy remain unfixed. Source-supported qualifications do not
   prove deterministic cleanup on those paths or create a bounded native deadline.
3. **Linux harness/deployment:** assertion-failure cleanup in FIDO/reactor/routing tests remains separate
   test-harness work. Existing successful-path oracles are retained, not relabeled complete failure-path
   cleanup. Runtime package loading, node/session/service permissions and induced close errors were not
   exercised. Linux checked-close semantics do not become portable POSIX/macOS/Windows guarantees.
4. **macOS:** nullable manager/set handling, pre-generation loss, ambiguous registration without a
   started worker, enum numeric repairs and CFNumberType signature repairs remain implementation work.
   Static enum/ABI findings are not intended design or measured corruption. Published native source
   availability does not bind an installed dylib to its exact producer or qualify fresh-host permissions.
5. **Windows/support:** notification failure retention/restart, native declarations, short successful
   transfer handling, nested sensitive-staging cleanup, deterministic WindowsIdentity disposal and
   dormant loader repair remain unfixed. Q001 deployed producer/headers, Q002 historical Windows
   artifact, Q003 SmartCard class-GUID provenance and Q004 HIDAPI revision remain held. No source
   comment asserts cross-architecture execution or universal elevation.
6. **History and late delta:** `fixes-wave-001:CORE-001` and B4 Q005 retain the unresolved original
   serial-9681620 failure cause. The user's upcoming Windows CCID/HID concurrency work is a later
   source/evidence delta; no unfinished branch was inspected. Existing native-only Linux counts,
   historical physical counts, macOS `.8` observations and stable 1.18.1 source remain distinct.

## Exact shared/external handoff

`editing/transports/followups.json` enumerates exact paths and original assigned units for each
proposal, including unchanged candidate surfaces. It is the path-level handoff rather than permission
to edit those files. In particular:

| Later owner | Required integration |
| --- | --- |
| SHARED: `docs/architecture/connection-ownership-and-contention.md`, `docs/architecture/raw-access-tiers.md`, Core README/CLAUDE | Integrate route-specific admission, wrapper failure release, Linux terminal wake, borrowed session lifetime, transaction outcome and direct synchronous access. Qualify Windows elevation and macOS raw error history without rewriting historical observations. |
| SHARED: Core/test CLAUDE fake examples | Use actual EnqueueResponse/TransmittedCommands, complete protocol/connection disposal and current Device state. B1-C011 stays linked to B6's read-only fake/protocol dependencies; no absent helper is introduced here. |
| SHARED: Linux setup, AOT/package and migration guidance | Lead with scoped node/session/service permissions and actual distribution deployment checks; do not make world-writable access the default. Explain unversioned libudev loading and keep historical L0–L6 hashes/counts as historical. Use SharedDeviceInfoRead for current discovery ownership. |
| SHARED: platform interop/native AOT/highlights/toolchain guidance | Replace stale delegate-field callback instructions with static unmanaged callbacks plus rooted generation context. Explain JIT shared versus AOT static shim selection, current registry totals and actual gate/child-drain limits. Preserve later producer/native qualification. |
| Boundary/CLI owners | Correct device preference versus applet transport choice and the 32-case ModHex selection commentary. PIV application/PIN/cache assertions require their boundary and deferred physical scope; no new PIV normative text is supplied by this edit. |
| External verification/benchmark owners | Preserve invocation versus task/native completion, censoring, real native-only versus physical scope, compiled versus executed AOT coverage and source/artifact identities. Child kill is OS cleanup, not cooperative SDK drain. Fix OperatorCue outcome/speech guarantees and RunLog complete-argv exposure only in separately assigned scope. |

A/C and B5/B6 primary sources remain other editors' disjoint scopes. Where a proposal needs only
shared/external edits, that disposition is explicit instead of creating artificial primary-source
changes. Accurate buffer ownership, checked Linux close, actual quiescence, native-owner cancellation
and result-transfer explanations are retained with their investigator evidence links.

## Coordinator next gate and stopping point

Review full affected units and retained original citations independently, reconcile the unchanged/held
files and shared/boundary returns, then run only the already-qualified static/compiler/documentation
checks assigned by the coordinator. This return claims no build or test result. Complete Core Pass 1
publication and deliver the deferred finding handoff, then stop; it starts no Pass 2 implementation,
new native/device campaign or additional planning project.
