# B4 Windows and support investigation — continuation checkpoint 1

**Outcome:** a durable partial investigation, with complete source/assertion reads
of **77 of the 106 primary files**. Ten local candidate groups are recorded.
**B4 remains incomplete.** The 29 unread primary files, 878 unread shared units
and 253 read units needing further context are explicitly retained for continuation.
No inventory/hash check has been promoted into a semantic pass.

Source stays `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. Startup HEAD was
`01a114d4e251e0f52b92f73393fbe1dca5d5c24e`; record serialization observed
`c7ff868c6f1b89e2d72eaba526bf98d9c28adb05`. All selected product/shared file and
range hashes match the frozen inventory; source paths present in the pin match
after explicit LF normalization. Records-only HEAD advances leave the source pin
unchanged. No unfinished branch or other worktree was inspected.

## Coverage and record map

The exact JSON packet contains **106 files / 14,842 lines**, **1,456 primary units**
and **894 shared units**, for **2,350 assigned IDs**. Every ID appears once in the
checkpoint ledger, including the ones still unread.

| Disposition | Primary | Shared | Meaning |
| --- | ---: | ---: | --- |
| READ-CLEAN | 590 | 1 | Scoped source/documentation disposition; independent review and validation pending |
| READ-CANDIDATE | 286 | 11 | Read in implementation/assertion context; candidate triage pending |
| READ-PENDING-CONTEXT | 249 | 4 | Complete text read, with exact original/API/dependency work still outstanding |
| UNREAD | 331 | 878 | Complete semantic/context review remains pending |

The 591 provisional clean units supply no claim of implementation acceptance,
compiler shape, all-platform behavior, or fresh native/device execution. The
continuation contains **1,462 outstanding IDs** (unread plus context-pending).
Candidate-associated unchanged paths remain an explicit later editor/reviewer
responsibility, rather than silently becoming accepted fixes.

| Record | Purpose |
| --- | --- |
| [Unit dispositions](investigation/B4/unit-dispositions.json) | Every original ID/range/hash, read context, candidates and pending gates |
| [Coverage summary](investigation/B4/coverage-summary.json) | Exact counts and all 106 files reconciled into 15 coherent subfamilies |
| [File evidence](investigation/B4/file-evidence.md) | Distinct implementation/assertion/lifetime context for each of the 77 complete reads |
| [Candidates](investigation/B4/candidates.md) | CORE-B4-C001–C010; separate documentation/runtime/policy/qualification facets and reopening maps |
| [Candidate index](investigation/B4/candidate-index.json) | Exact associated IDs and unchanged paths; narrow dependencies retain their original owner |
| [Original sources](investigation/B4/sources/register.md) | Exact Microsoft/.NET/shim/PCSC passages, versions, actors, strengths and conditions |
| [Capture manifest](investigation/B4/sources/captures.json) | URL, UTC access, original bytes and SHA-256 |
| [File identities](investigation/B4/file-identities.json) | Raw inventory and pinned Git identities, separately from semantic status |
| [Continuation IDs](investigation/B4/continuation.json) | Exact remaining 1,462 IDs/ranges/hashes, including 29 unread primary files |
| [Continuation and qualification](investigation/B4/continuation-and-verification.md) | Ordered remaining work, native/test matrix, inherited IDs and late Windows delta |
| [Preservation check](investigation/B4/preservation-verification.json) | Source/range/ID reconciliation and protected-input hashes |
| [Checkpoint verification](investigation/B4/checkpoint-verification.json) | Fresh read-only ID/hash/count/link checks and eight published-shim source Git-blob comparisons |

## Reconciled subfamilies

| Primary family | Files | Complete file reads |
| --- | ---: | ---: |
| Benchmarks | 4 | 4 |
| Public API snapshots and support exceptions/service locator | 4 | 2 |
| Desktop PC/SC imports, handles, structs and carriers | 14 | 14 |
| Native loading/platform support | 7 | 7 |
| Windows native declarations, property access and report owner | 14 | 14 |
| Portable HID contracts, parsing/classification and listener | 14 | 14 |
| Keyboard translators/layout | 10 | 0 |
| Windows HID listener/facades/enumeration | 4 | 4 |
| Integration test oracles | 3 | 3 |
| Audit child hosts | 2 | 0 |
| Boundary inventory/contract/migration/responsiveness oracles | 14 | 0 |
| Windows native registration test | 1 | 1 |
| Resilience static oracle | 1 | 0 |
| HID unit-test oracles | 5 | 5 |
| Native AOT/compatibility hosts and fixtures | 9 | 9 |

Complete file reading and completed disposition differ: selected carrier/usage
constants and shared build claims still need exact source/context checks. The
ledger keeps those units out of the clean sample denominator. The 894 shared
ranges are a substantial support/documentation population in their own right;
instruction reading alone has not marked them semantically clean.

## Principal source findings

- **C001:** Windows opens report handles synchronously with FILE_FLAG.NORMAL and
  null OVERLAPPED. Its typed adapters can block before returning a Task. The
  SafeHandle native borrow can outlive concurrent managed Dispose, while the
  registered compatibility wrapper releases its claim after that Dispose returns.
  Extend B1-C005 nonadditively; distinguish built-in deferred release from an
  arbitrary custom adapter's throwing/uncertain teardown.
- **C002:** Windows listener frees callback state after ignoring unregister status;
  Start lacks the disposed guard promised by IDeviceChangeListener. Several timing
  tests compare Task.WhenAny without awaiting the selected disposal task. Their
  source oracle supplies no successful native unregister/finalizer proof.
- **C003:** exact native carriers differ from several declarations: LPGUID versus
  by-value Guid, PULONG versus IntPtr storage, and HidD BOOLEAN versus BOOL.
  Windows x64's indirect 16-byte aggregate convention can hide one mismatch;
  x86/arm64 remain distinct. Declaration mismatch is separate from captured failure.
- **C004:** preserve the feature/IO desired-access split, while replacing universal
  elevation/access claims with their host, collection, ACL and driver conditions.
  The exact B1 platform-notes secondary parent is retained without another primary
  count.
- **C005:** dormant LoadLibraryEx import lacks the exact W entry point and captured
  last-error setting. Present NativeShims loading follows a different route; no
  active failure is attributed to the dormant loader. Legacy Framework/Cleanup
  prose and delegate/library lifetime need a focused correction.
- **C006:** shared PC/SC reader names mix ASCII, UTF-8 and Windows ANSI allocation.
  Published shim source forwards the native PCI pointer directly; the inspected
  Linux LP64 PCI is sixteen bytes versus the managed eight-byte carrier. Keep the
  producer/header condition and installed-binary gap explicit.
- **C007:** nested Windows report-ID staging survives typed-adapter copy clearing;
  cleanup requirements depend on actual payload meaning. Successful short native
  transfers also need their own error path, rather than a last-error value from a
  successful call. C/B5 retain semantic secret/framing ownership.
- **C008:** benchmarks and verification hosts have narrow, useful oracles; preserve
  invocation/terminal/censoring distinctions and historical Mac-only compatibility.
  Current Windows runtime qualification still lacks an exact matching artifact.
- **C009:** BROKEN_PIPE literal differs from Win32; shared Windows PC/SC symbolic
  mapping also mislabels pcsc-lite UNSUPPORTED_FEATURE. Preserve native raw-code
  provenance and B3's separate Mac findings.
- **C010:** direct HID factory/public support reference needs caller ownership,
  synchronous execution and ungrouped-claim context. Detailed IHidConnection
  borrowed-input/result language is already useful and should be retained.

These are candidates for fresh independent review, rather than ten accepted
independent bugs. C001 extends a B1 group; C008 carries A/B3 evidence dependencies;
C006/C009 carry generic support effects without reopening B2's primary source
denominator.

## Current ownership and access boundaries

| Resource / state | Baseline owner and observation | Consequential bound |
| --- | --- | --- |
| Report metadata | Temporary/retained SafeFileHandle and scoped preparsed pointer | Caps query success establishes no feature or FIDO transfer |
| Windows IO/feature handle | WindowsHidReportAccess replaces metadata handle at report open | Open and native I/O run synchronously; tokens in typed compatibility adapters are unused |
| Borrowed native buffer/handle | Generated synchronous import retains handle/buffer through native return | Another thread's Dispose return may precede actual deferred native close |
| Typed report result | Windows owner allocates independent payload array; adapter returns it | Caller handles successful result; nested staging has a different lifetime |
| Grouped-key registration | YubiKeyDevice/registered wrapper owns known-member process token | OS sharing/elevation and cross-process/physical applet state are separate |
| Native notification | CM registration, delegate field and weak GCHandle | Successful unregister drains callbacks; an ignored failure supplies no proof |
| Topology context | Separate scoped PC/SC context in WindowsTopologyNativeOps | Shared native resource manager is not the active connection's context; synchronous work has no SDK latency budget |
| Container identity | ConfigMgr ContainerId feeds A's evidence hierarchy | OS container equality supplies grouping evidence, not fresh hardware/app authentication attestation |

Use B1's **admitted**, **wait canceled**, **native pending**, **terminal**,
**drained**, **release proven**, and **uncertain claim retained** vocabulary.
The baseline Windows compatibility path has fewer explicit state observations
than the built-in Mac/Linux owners. Common IConnection signatures add none of
those missing native observations automatically. A future CancelIoEx success
would request cancellation; actual completion/error and storage/reference drain
must still be checked before release or reuse.

## Preserved scope and next action

Root/Core/nested test instructions, Core README, campaign workflow/style, approved
plan/inventory/dispatch/source ownership, SDK house style, A/B1/B2/B3 completed
returns and relevant inherited routing were read. No additional nested
AGENTS/CLAUDE files were found in verification or benchmarks. External host source
and project declarations were read before oracle dispositions. Remaining shared
Tests.Shared/PublicApi/toolchain caller bodies are explicit continuation work.

Only `investigation/B4/*` and this report were written. Product code, XML,
assertions, executable output, central inventory/coverage, original registries,
index and refs remain untouched by B4. No build, test, formatter, native-library
load, workstation service, VM, remote hardware, device command, commit, push or
subagent ran. Source HTTP requests and Python/Git static record work were the
only execution; scratch stayed out of exhausted `/tmp`.

Continue the existing B4 assignment from its exact remaining IDs, without new
agents or a wholesale baseline reread. Finish all 29 unread primary files, pending
original/context checks and remaining shared ranges before requesting packet
completion. Keep the user's Windows readiness gate separate; preserve the serial
9681620 historical uncertainty and deferred PIV physical lane.
