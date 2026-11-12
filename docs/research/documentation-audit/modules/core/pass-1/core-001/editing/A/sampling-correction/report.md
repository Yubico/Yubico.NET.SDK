# SAMPLE-A001 / SAMPLE-A002 — bounded listener and discovery-admission documentation corrections

## Scope and verified baseline

Only `src/Core/src/Devices/HotplugSignals.cs` and
`src/Core/src/Devices/DiscoveryReadSkippedException.cs` are corrected. Before editing, HotplugSignals bytes matched
current primary at `/home/rupicola/Code/y/Yubico.Yubikit.NET/src/Core/src/Devices/HotplugSignals.cs`:
SHA-256 `e6191215527e0863f7548c4d53047696d9b85a24357f3f6aff8df13c8cda0561`.
The before-source snapshot and both comparison hashes are retained in this directory. No primary file
was edited. The 37 accepted revision-2 C# source hashes and all 25 predecessor editor/review files were
checked before correction and are checked again by the successor receipt. DiscoveryReadSkippedException
also matched current primary byte-for-byte before its correction: SHA-256
`0de5842734567dec85406471841150b7e44d8c29eeebab947c8d938faa8b8ac4`.
Its separate before-source snapshot and before identities are retained here. The accepted patches, reviews and
manifests remain byte-identical; no accepted full transfer patch is regenerated or replaced.

These are documentation corrections for the coordinator's **SAMPLE-A001** and **SAMPLE-A002** clean-sampling misses, not a
new runtime finding, policy change, experiment or implementation project. Sampling/coverage disposition,
denominator removal and expansion remain coordinator-owned and were not modified here.

## Exact selected unit and complete source evidence

Selected original unit: **`core-001:unit:711e47d18bab0742`**, original lines **80–98**, original character
range **3153..3900 exclusive**, range SHA-256
`3cb67fa588270fbe5d81dcabdbc82618a31a0dac0cb4081a923d14b909298c97`.
Original source pin: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; full-file SHA-256 is the before hash
above. The immutable investigation record calls this unit READ-CLEAN, with no candidate IDs. That
historical record is preserved; `annotation.json` records its corrective EDIT annotation separately.

The entire 114-line before file was read, including the constructor, TryStart, all three cleanup passes,
BestEffort and channel construction. The corrected file has 128 lines, with executable content unchanged.

| Complete unit/context | Original → current lines | Verified behavior and affected wording |
| --- | --- | --- |
| Constructor, `core-001:unit:f9c6bf50db8d00e7` | Constructor context 33–46 → 33–50; comment 35 → 35–39 | Calls TryStart sequentially. The catch-path warning logger can throw before partial-listener cleanup or a later constructor iteration. The all-listeners-unavailable warning can also throw. Replace the absolute promise that logging failures cannot prevent other startup with ordered attempts and the logging-exception qualification. |
| TryStart, `core-001:unit:5f278ed63d2f57d0` | Original unit 48–80; method 50–80 → 54–84 | Factory, callback assignment, Start, status and initial warning are inside try/catch. The catch's warning is outside another containment boundary. Failed-start cleanup then uses three BestEffort calls; a thrown cleanup-warning logger can interrupt them. Successful status adds the listener and returns. No universally successful startup or partial cleanup is established. |
| Dispose, selected `core-001:unit:711e47d18bab0742` | Original unit 80–98; comment 82 → 86–96; method 83–98 → 97–112 | Sets `_disposed` before three separate passes: detach every retained callback, stop every retained listener, dispose every retained listener. Only after those passes return does execution reach TryComplete; the final debug logger follows that call. Preserve this useful order, but describe attempts rather than confirmed cleanup. A cleanup-warning logger can escape before later passes or channel completion. A later Dispose returns on the already-set flag rather than retrying. Channel completion is not native-release evidence. |
| BestEffort, `core-001:unit:9edad7f2794d3e49` | Original unit 98–110; method 100–110 → 114–124 | Catches the cleanup action's exception and calls the warning logger. It neither proves that the failed cleanup succeeded nor contains an exception thrown by that logger. This is the direct source reason both absolute summaries need qualification. |

In HotplugSignals, only the constructor and Dispose XML blocks changed. No imports, directives, declarations, signature,
default, action ordering, callback, channel option, logging string, runtime body or assertion changed.
The patch tool also adds a final newline to the previously unterminated source, recorded in the manifest.
No example body occurs in either file, and no existing example or test was edited.

## SAMPLE-A002 — existing discovery lease is also an admission refusal

The two selected original units are **`core-001:unit:c085f1cd3632e7c2`** (InterfaceLeaseHeld,
original lines **26–31**, characters **1120..1294 exclusive**, SHA-256
`272daa5efb4dab19eb9a45a55f37d6e4e1371a2ca8e9ba1034e50b5362943a04`) and
**`core-001:unit:076b4d4328825902`** (enum type preamble, original lines **1–22**, characters
**0..958 exclusive**, SHA-256
`23290cb532f8ad35c9edc33f382085b9e00f55247a3ce3dc95d5e35cdfa2ca65`).
Both historical records remain READ-CLEAN with empty candidate IDs; separate miss annotations supersede
that reading disposition for these comments without rewriting the original records. Across both misses
there are two source files, two SAMPLE labels and three selected original unit IDs.

The complete 62-line before exception/cause file and complete relevant acquisition methods were read:

| Actual source context (unchanged by this correction) | Evidence |
| --- | --- |
| `DeviceConnectionRegistry.cs:144–145,181–191` | The precheck treats an unregistered interface as available. For registered ownership it takes the lock, calls ThrowIfUnrecovered, then requires `!_discoveryActive && _connectionCount == 0 && _waitingConnections == 0`. |
| `DeviceConnectionRegistry.cs:281–293` | Atomic acquisition takes the same lock, calls ThrowIfUnrecovered, and returns null for `_discoveryActive || _connectionCount > 0 || _waitingConnections > 0`. Otherwise it records discovery ownership and returns the registration. The predicates are complementary; the precheck is not an acquisition or a reservation. |
| `SharedDeviceInfoRead.cs:98–132` | When PID/cohort gates exist, StartSharedRead calls IsDiscoveryAvailable before gate acquisition; a false result throws DiscoveryReadSkippedException with InterfaceLeaseHeld. The surrounding finally retains the existing admission-transfer cleanup. |
| `SharedDeviceInfoRead.cs:245–290` | ConnectAndReadAsync validates the provider, calls TryAcquireDiscovery and translates a null lease into InterfaceLeaseHeld before native open. This atomically checks admission even after a successful earlier precheck. |
| `DeviceConnectionRegistry.cs:193–231,295–304` | Connection waiters are tracked separately while discovery is active. Unrecovered native ownership throws UnrecoveredConnectionException before either availability predicate returns a refusal; it is not silently relabeled InterfaceLeaseHeld. |

The enum preamble, InterfaceLeaseHeld member XML and adjacent exception-type summary
(`core-001:unit:79340850e7d0f3c2`, original lines 45–54, an adjacent unit rather than another selected sample) now include
another discovery lease alongside active and waiting connections. Enum ordering/values, constructor
defaults, exception messages and all runtime behavior are preserved. No registry or shared-read source
was edited. This corrects the cause vocabulary, not a runtime admission defect or its policy.

## Existing qualification facets, not new runtime allocation

- **CORE-A-C008 / FPT-008:** the existing monitor/lifetime/logging qualification and held **A-E03**
  logger-failure investigation remain the direct handoff. This correction acknowledges visible exception
  propagation without claiming the held experiment ran or deciding a logging/cleanup policy.
- **CORE-C-C008 / FPT-090:** the existing logging callback/exception-containment qualification is related
  context; no global redaction, logging-factory change or new logging finding follows.
- **CORE-B1-C004 / FPT-025** and **CORE-B4-C002 / FPT-049:** existing listener partial-startup,
  unregister/drain and native-lifetime qualifications remain separate mechanisms. Ordered managed
  attempts here do not close them or prove platform-native release.
- **SAMPLE-A002 → CORE-A-C008 / FPT-008:** the existing discovery admission/ownership qualification
  supplies the direct context for the cause wording. **CORE-B1-C012 / FPT-033** remains a related but
  distinct connection-acquisition precedence/rollback question; this correction changes or closes none
  of that question's implementation/experiment facets.

These are facet/dependency links only. Original IDs, inherited provenance and evidence populations are
unchanged. No additional candidate, implementation finding or acceptance count is allocated.

## Focused transfer and reviewer handoff

- `product.patch` contains **only these two files' documentation deltas relative to the byte-verified current
  primary before-source**, with conventional `a/` and `b/` prefixes. It is not the cumulative A patch.
- `file-identities.json` records exact primary-before/local-before/current hashes, final-newline state,
  affected comment boundaries and non-comment preservation.
- `annotation.json` retains the three exact selected and adjacent original unit records, candidate/facet links
  and focused-review status. Original investigation and sampling records are not overwritten.
- `verification.json` records fresh static XML/diff checks, exact patch hash, preserved 37 accepted source
  identities and 25 predecessor files, and the unchanged-primary check. `output-identities.json` hashes
  these successor artifacts except itself.

Review only the five affected XML blocks against the complete code described above, particularly the warning
logger inside BestEffort and TryStart's catch, the position of TryComplete before the final debug log,
and the matching discovery/live-connection/waiter predicates in precheck and atomic acquisition.
Content acceptance and primary transfer remain pending focused review. All experiments, including
FPX-01/A-E01 and A-E02 through A-E06, remain unexecuted. No build/test, formatter, compiler,
native/device/crypto execution, Git mutation or subagent ran. Complete Core Pass 1 and its agreed handoff,
then stop; this correction introduces no later-pass planning or implementation.
