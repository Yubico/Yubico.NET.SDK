# B4 Windows and support — completed pinned investigation

**Outcome:** **106/106 primary files** and **2,350 exact assigned IDs** now have
final investigator dispositions: 1,456 primary units plus 894 shared units, with
**zero unread or await-context outcomes**. The 1,462 outstanding checkpoint IDs
are explicitly transitioned. Ten stable local candidate groups remain provisional;
no accepted IDs or product fixes were allocated.

Coverage is deliberately qualified: 105 primary files received complete reads.
The single whole-file Unshipped API snapshot received B4 support-entry and complete
type-inventory reconciliation, with an explicit related-context disposition for
non-B4 signatures. That outcome is neither clean nor an independent semantic
review of every A/B1/B5/C member. Historical, related-context and source-qualified
outcomes retain evidence limits and concrete owners rather than hiding as clean.

Source stays `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. Startup HEAD was
`01a114d4e251e0f52b92f73393fbe1dca5d5c24e`; completion observes metadata HEAD
`bb94f0884f63f260156227957d4cc112d984e300`. All selected product/shared file and
range hashes match the frozen inventory; source paths present in the pin match
after explicit LF normalization. Records-only HEAD advances leave the source pin
unchanged. No unfinished branch or other worktree was inspected.

## Coverage and record map

The exact JSON packet contains **106 files / 14,842 lines**, **1,456 primary units**
and **894 shared units**, for **2,350 assigned IDs**. Every ID appears once in the
successor ledger. The original checkpoint report, ledger and eleven companion
records are preserved byte-for-byte under `investigation/B4/history/checkpoint-1/`.

The exact primary/shared breakdown is in the coverage summary. Combined outcomes:

| Disposition | Primary | Shared | Total | Meaning |
| --- | ---: | ---: | ---: | --- |
| READ-CLEAN | 1,053 | 193 | 1,246 | Scoped source/documentation conclusion only; independent review and validation pending |
| READ-CANDIDATE | 397 | 177 | 574 | Candidate or explicitly retained reopening context, not an accepted independent bug |
| READ-RELATED-CONTEXT | 5 | 366 | 371 | Assigned caller/policy/API context; underlying semantic assurance remains with named primary owners |
| READ-HISTORICAL-QUALIFIED | 0 | 158 | 158 | Dated evidence/proposal/source claim, not rerun or current artifact attestation |
| READ-SOURCE-QUALIFIED | 1 | 0 | 1 | SmartCard class GUID provenance absent after bounded original-source follow-up; concrete owner Q003 |
| UNREAD / await-context | 0 | 0 | 0 | No remaining pinned investigator work |

The provisional clean units supply no claim of implementation acceptance,
compiler shape, all-platform behavior, or fresh native/device execution. The
transition record preserves all **1,462 previously outstanding IDs** and reasons.
Candidate-associated unchanged paths remain an explicit later editor/reviewer
responsibility, rather than silently becoming accepted fixes.

| Record | Purpose |
| --- | --- |
| [Unit dispositions](investigation/B4/unit-dispositions.json) | Every original ID/range/hash, final scope/owner, candidate and later gate |
| [Coverage summary](investigation/B4/coverage-summary.json) | Exact counts and all 106 files reconciled into 15 coherent subfamilies |
| [File evidence](investigation/B4/file-evidence.md) | All 106 primary-file conclusions and every assigned shared path's substantive context |
| [Candidates](investigation/B4/candidates.md) | CORE-B4-C001–C010; separate documentation/runtime/policy/qualification facets and reopening maps |
| [Candidate index](investigation/B4/candidate-index.json) | Exact associated IDs and unchanged paths; narrow dependencies retain their original owner |
| [Original sources](investigation/B4/sources/register.md) | Exact Microsoft/.NET/shim/PCSC passages, versions, actors, strengths and conditions |
| [Capture manifest](investigation/B4/sources/captures.json) | URL, UTC access, original bytes and SHA-256 |
| [File identities](investigation/B4/file-identities.json) | Raw inventory and pinned Git identities, separately from semantic status |
| [Transition record](investigation/B4/completion-transition.json) | Exact 1,209 unread + 253 pending-context successor outcomes |
| [Continuation state](investigation/B4/continuation.json) | Empty remaining ID list; external gates are not unread units |
| [Qualification and reopening](investigation/B4/continuation-and-verification.md) | Native/test matrix, inherited IDs, clean nominees, future proposals and focused late Windows delta |
| [External gaps](investigation/B4/qualification-gaps.json) | Five bounded external qualifications, owners and exact reopening evidence |
| [Clean nominees](investigation/B4/clean-nominees.json) | Seven checked eligible IDs, not independent stratified sampling |
| [Diagram reconciliation](investigation/B4/diagram-reconciliation.json) | Five visually inspected PNGs, SVG labels and exact source/artifact digest correspondence |
| [Final verification](investigation/B4/completion-verification.json) | Fresh ID/range/source/history/capture/link/frozen-record checks; eleven published native source blobs |
| [Checkpoint history](investigation/B4/history/checkpoint-1/manifest.json) | Thirteen immutable original records and hashes; original verification failure retained separately |

## Reconciled subfamilies

| Primary family | Files | Complete file dispositions |
| --- | ---: | ---: |
| Benchmarks | 4 | 4 |
| Public API snapshots and support exceptions/service locator | 4 | 4 (one scoped snapshot) |
| Desktop PC/SC imports, handles, structs and carriers | 14 | 14 |
| Native loading/platform support | 7 | 7 |
| Windows native declarations, property access and report owner | 14 | 14 |
| Portable HID contracts, parsing/classification and listener | 14 | 14 |
| Keyboard translators/layout | 10 | 10 |
| Windows HID listener/facades/enumeration | 4 | 4 |
| Integration test oracles | 3 | 3 |
| Audit child hosts | 2 | 2 |
| Boundary inventory/contract/migration/responsiveness oracles | 14 | 14 |
| Windows native registration test | 1 | 1 |
| Resilience static oracle | 1 | 1 |
| HID unit-test oracles | 5 | 5 |
| Native AOT/compatibility hosts and fixtures | 9 | 9 |

Complete file reading and scoped final disposition differ. The snapshot limit,
dated evidence and unavailable external provenance stay outside the clean sample
denominator. All 894 shared IDs have final outcomes from assigned body/assertion/
policy review or an explicit historical/context qualification; orientation or
hash equality alone supplied no verdict.

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
  prose and delegate/library lifetime need a focused correction. IsElevated also
  omits deterministic disposal of a WindowsIdentity that owns a native token.
- **C006:** shared PC/SC reader names mix ASCII, UTF-8 and Windows ANSI allocation.
  Published shim source forwards the native PCI pointer directly; the inspected
  Linux LP64 PCI is sixteen bytes versus the managed eight-byte carrier. Keep the
  producer/header condition and installed-binary gap explicit. The built-in
  Transmit/default-provider path actually forwards this carrier; Linux build
  recipes select distro target pcsc-lite headers via pkg-config. Windows uses
  two 32-bit DWORDs; macOS framework selection is separate, not a Linux-width
  mismatch conclusion. Exact installed producer/header correspondence is Q001.
- **C007:** nested Windows report-ID staging survives typed-adapter copy clearing;
  cleanup requirements depend on actual payload meaning. Successful short native
  transfers also need their own error path, rather than a last-error value from a
  successful call. RunLog also persists complete argv, including a supplied
  --nuget-api-key, outside Exec.Mask's command-display path. C/B5 retain semantic
  secret/framing ownership; no real key was used.
- **C008:** benchmarks and verification hosts have narrow, useful oracles; preserve
  invocation/terminal/censoring distinctions and historical Mac-only compatibility.
  Current Windows runtime qualification still lacks an exact matching artifact.
  Test gates reject all-no-tests, not each no-test project; exit-zero/null-count
  can pass. Child kill and early continuation cleanup are not proven drain.
  OperatorCue speech/console guarantees and highlights' shared-AOT-shim claim
  exceed their implementations. Current registry totals are 218/43/76/23, not
  every earlier plan count. Preserve historical native-only versus physical runs.
- **C009:** BROKEN_PIPE literal differs from Win32; shared Windows PC/SC symbolic
  mapping also mislabels pcsc-lite UNSUPPORTED_FEATURE. Preserve native raw-code
  provenance and B3's separate Mac findings. DIRECT sharing, USER scope and status
  catalogs need backend conditions; ordinary SCardException contention text omits
  strict PC/SC ConnectionInUseException and Windows report error distinctions.
- **C010:** direct HID factory/public support reference needs caller ownership,
  synchronous execution and ungrouped-claim context. Detailed IHidConnection
  borrowed-input/result language is already useful and should be retained.
  Keyboard tables have aliases, locale-applicability and shifted-character limits;
  CLI ModHex selection includes 32 cases versus the 16-letter description. Stale
  helper/examples and diagram teaching labels need focused correction. All27
  DEVPKEY values/active interface GUIDs reconcile; one class GUID stays Q003.

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

## Completion boundaries and remaining external qualifications

The pinned source investigation is complete, not the campaign's editing/review/
validation phase. Five external qualifications remain: **Q001** deployed native
producer/header identity; **Q002** exact historical Windows runtime artifacts;
**Q003** one SmartCard class-GUID provenance; **Q004** unspecified HIDAPI revision
behind comparative comments; **Q005** historical serial9681620 failure cause.
Each has a concrete owner and exact evidence needed in qualification-gaps.json.
Fresh compilation/native/device qualification remains unrun, not an extra source
failure or permission to start hardware work.

The inherited migration `oath-pilot-and-fixes-wave-002:DOC-002` parse failure was
reproduced statically at 274:488; its original ID is preserved. Host-selector
collision remains a scoped substring-selection warning, not a newly executed
host result. C-C008/C011–C013/C017–C019 and A/B1/B2/B3 aliases are nonadditive.
HID 1.11/HUT 1.5, pinned Microsoft/.NET source, published NativeShims 1.18.1 targets
and the original SourceLink advisory now have explicit title/version/status/
actor/strength/conditions in the source register. All 42 captures are hashed.

Only `investigation/B4/*` and this report were written. Product code, XML,
assertions, executable output, central inventory/coverage, original registries,
index and refs remain untouched by B4. No build, test, formatter, native-library
load, workstation service, VM, remote hardware, device command, commit, push or
subagent ran. Source HTTP requests and Python/Git static record work were the
only execution; scratch stayed out of exhausted `/tmp`.

The focused late-delta map awaits the user's ready Windows CCID/HID handoff;
that work did not block this baseline and no unfinished checkout was inspected.
Retain PIV physical deferral. Inherited 38 JIT/38 AOT native-only checks are not new
Windows or physical evidence; historical 43 physical checks on serial 103 remain
separate. Frozen 5,366/8,325/114/4 evidence populations and baseline 57 product
newline representations are preservation inputs, not new product changes.

## Fresh static verification result

`verify_completion.py` passed with zero failures: 2,350 exact unit identities,
230 assigned source paths, 13 preserved checkpoint records, 42 captured originals,
11 immutable published native source blobs, five assigned diagram pairs and seven
eligible clean nominees. It also checked the frozen 5,366 prior artifacts, 8,325
integration records, 114 prior PIV files and four user files without rewriting
them. The Git product working diff is empty after normal attribute handling;
13 assigned source paths differ from pinned raw bytes only by LF/CRLF, matching
their checkpoint/inventory identities. The earlier baseline's 57-path newline
qualification is not relabeled as new code or a new semantic source failure.

The successor verifier's first attempt used the wrong root for the top-level
frozen manifest. That exact failure is preserved in
`investigation/B4/completion-verification-attempt-1.txt`; the corrected fresh
verification passed. These checks verify record preservation, not semantic
quality, independent acceptance or runtime behavior.
