# OATH/Core dedicated boundary investigation

**Outcome:** all **239 assigned units across 42 selected paths** received complete source/assertion
dispositions. The complete **56 candidate files / 11,227 lines** were read, including relevant callers,
test helpers, assertions and enclosing cleanup. Six local candidate groups are ready for coordinator
triage. This is a static boundary return, not a second whole-OATH audit, accepted Core IDs, product
editing, independent content review or runtime qualification.

Source remains `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; record preparation observed metadata HEAD
`9d72160375862dfd9d09793538f64a7d4f96e398`. Every assigned raw file/range hash matches the inventory;
all 56 disposition files match pinned source with explicit BOM/CRLF handling. Metadata HEAD supplies no
new implementation baseline. Existing user/coordinator records and other boundaries were left intact.

## Records

| Record | Purpose / limit |
| --- | --- |
| [Analysis](boundaries/Oath/analysis.md) | Actual factory call graph; creation/resource/state/reset/failure/cancellation/version tables; operation-specific B1/B5/B6/C handoffs |
| [Candidates](boundaries/Oath/candidates.md), [index](boundaries/Oath/candidate-index.json) | CORE-OATH-X001–X006 with documentation/runtime/decision/qualification facets and exact XML/guide/sample/helper/migration/comment reopening locations |
| [Unit dispositions](boundaries/Oath/unit-dispositions.json) | All original IDs, exact coordinates, source/range hashes, actual context, localized candidate/clean/context and pending review/validation |
| [File dispositions](boundaries/Oath/file-dispositions.json), [authored observations](boundaries/Oath/file-notes.json) | Every complete candidate file explicitly unchanged, with substantive ownership/assertion/context reasoning |
| [Coverage](boundaries/Oath/coverage-summary.json), [clean nominees](boundaries/Oath/clean-nominees.json) | 158 read-clean, 28 read-candidate, 53 read-context; zero unread; 16 substantive unsampled nominees |
| [Inherited outcomes](boundaries/Oath/inherited-dispositions.json) | All 13 assigned source-run keys in eight nonadditive groups; authoritative registry remains unchanged |
| [Sources](boundaries/Oath/sources.md), [manifest](boundaries/Oath/source-manifest.json) | Exact original status/section/actor/strength/profile and hashes; shared Python/B6/C reuse; bounded external gaps |
| [Static verification](boundaries/Oath/verification.json) | Fresh ID/raw-range/pin/record/source checks; no SDK or physical execution credit |

Root/Core/OATH/nested test instructions, campaign plan/dispatch/workflow/style/SDK house style,
inherited registry/routing/seeds, completed A/B1–B6/C reports and relevant original OATH closeouts were
read. Narrow shared/Core/SecurityDomain/Tests.Shared/CLI dependencies retain their primary owners;
reading them adds no second coverage credit. The linked SCP handshake fake was read completely.

## Principal conclusions

1. **Creation selects SmartCard once and awaits full OATH setup.** The device extension opens a hidden
   connection, calls the direct borrowing factory and transfers ownership after success. Core binds before
   SELECT, registers protocol disposal, configures optional SCP after SELECT/version discovery, and
   publishes readiness only after the complete initializer. OATH rebinds its backend to the SCP wrapper.
   A failed direct factory detaches without closing the caller's connection; convenience failure cleanup
   closes the hidden connection while preserving the original initializer exception.
2. **SCP authentication and OATH unlock are separate.** A protected applet can be initialized over an
   authenticated channel while IsLocked remains true. VALIDATE consumes the OATH challenge after host
   HMAC verification; SET/UNSET update password protection. Salt aliases owned session storage and clears
   on disposal, while cached applet metadata remains historical. X001 proposes useful local lifetime/state
   XML, with no changed getter policy.
3. **Successful RESET remains terminal for both plain and SCP sessions.** It sends RESET, then awaits
   disposal without a caller token. No SELECT/refresh/rehandshake occurs. A borrowed connection remains
   the caller's to close/reopen; owned teardown closes the hidden connection. A teardown error follows an
   already successful device mutation. Pre-admission cancel/overlap refusal leaves a healthy session intact.
   A transport/device failure leaves mutation uncertain. No token-after-reset API or new symbol is invented.
4. **Per-exchange drain is narrower than an OATH public workflow.** CalculateAll's custom-period
   followups, AuthenticateAndRetry and user-presence callbacks extend beyond one guarded exchange.
   Await full workflows and keep callback services/input owners alive before disposal. X002 applies
   B5-C006 to these actual callers, without duplicate bug credit or a new operation lease.
5. **Existing buffer fixes retain exact source resolution.** Base32 shape/admission/failure cleanup,
   credential seed isolation/retry, raw VALIDATE and calculation response cleanup, HMAC-only output,
   structural parsing, numeric/identity validation and terminal reset remain their fourteen historical
   IMP scopes. Returned HMAC and immutable Code strings have different caller cleanup capabilities.
   B6's lower-level response/decryption/fragment copies remain separate from OATH's cleared owner.
6. **OATH reaches consequential Core SCP branches.** LIST and RESET have empty application data;
   configured A5 continuation calls reach protected followup handling. B6-C011/C012 own empty-command,
   phase/status/chaining qualification; the OATH fake checks actual key destruction and command order
   while accepting unverified outbound encryption/MAC. X003 proposes assertion-accurate test commentary.
   No live SCP terminal-reset proof follows from that synthetic test or historical plain reset.

X004 supplies actual UTF-16 reader-option/UI-policy scope and borrowed prompt/password ownership,
nonadditively with C. X005 localizes weaker test descriptions and separately scoped harness cleanup debt;
it does not reopen fixed product behavior. X006 clarifies selection/known-member process ownership and
complete example prerequisites, including migration callback-returned password cleanup. All candidate
product files remain unchanged for a fresh editor/reviewer.

## Inherited decisions and qualification

- `oath-pilot-and-fixes-wave-002:DEC-001`: error-provenance correction remains resolved; broader
  post-device-verification-failure session-usability policy remains open.
- `oath-pilot-and-fixes-wave-002:DEC-002`: normal derivation returns 16 bytes while raw APIs accept
  nonempty keys. Supported broader raw-key scope remains a maintainer/device-profile decision. This key
  is unrelated to CTAP's native selected-user DEC-002.
- `oath-pilot-and-fixes-wave-002:DEC-007`: effective OATH 5.6.3 any-SCP gate and Core 5.3.0/5.7.2 gates
  are confirmed. Shared Python pin `4ca60f706af930459138d8dc0f0f953480e1c7a4` and introduction commit
  `1b87964ba258e3ce39fd2959888dfe7cf81a6843` establish source policy; hardware/workaround rationale
  remains unresolved after the existing bounded research.
- `fixes-wave-001:CORE-001` and historical multi-key-exclusions retain their original identities.
  SmartCard-scoped Find avoids unrelated HID probes only for that selected call; the default OathTool
  still discovers All. Serial 9681620's old two-key HID discovery failure remains unexplained.
- Serial 125 returned `6A88` for both FF and 00 INITIALIZE UPDATE, before EXTERNAL AUTHENTICATE/RESET.
  No new device inventory, secure reset, key changes, PIN/password input or native/crypto operation ran.
- Final SCP03 **1.1.2 Public Release** clauses were reread; code-cited **1.1.2.6 Public Review** remains
  a different edition. Retained SCP11 is **1.3.0.13 Public Review**; final 1.4, full YubiKey profile and
  exact ISO APDU clauses retain B6's explicit gaps. Sibling implementation agreement is not conformance.
- DOC-002's existing YAML parse failure remains a separate shared migration issue. OpenPGP, Security
  Domain, HSM and YubiOTP version aliases receive the OATH comparison only; their own boundaries govern
  caller conclusions. The 21 PGP documentation labels remain outside new bug counts.

## Oracles and next gates

The 16 nominees include actual retained-seed/view/copy assertions, held reset teardown with finally
drain, first-failure-aware fresh reset/body helpers, actual raw-response clearing, exact encoded time
challenges and real-selector/fake-discovery cancellation. They are substantive eligible units, not
completed independent sampling. The coordinator removes any later edited overlap before selection.

Recommended later managed checks target full OATH Reset/ScpReset/Response/Authentication/Input/Time and
ToolCancellation/DeviceSelection classes, Core Base32 and selected PC/SC/session ownership classes, plus
offline lifecycle regressions in the actual xUnit-v2 integration project. Require nonzero execution
counts and inspect real toolchain selection; no SDK command ran in this packet. A bounded new workflow
experiment could hold a request/resolution callback or aggregate-followup gap, attempt disposal, then
release and drain everything in finally. That is a proposal, not execution or authorization.

Live SCP qualification needs an authorized identified device/firmware/reference and distinct successful
INITIALIZE UPDATE, external/internal authentication, protected OATH operation, RESET, teardown and fresh
reopen/handshake observations. Preserve failed setup and independent restoration results. Do not repair
or reset Security Domain keys just to make this lane green. Native/OS acceptance must identify loaded
artifacts and actual release evidence; compiler/fake tests cannot supply it.

Only `boundaries/Oath/*` and this report were written. No product/central ledger/assertion/API edit,
formatter, SDK build/test, crypto/native/device execution, subagent, Git index/ref change, commit or push
occurred. Static Python performed record/hash joins and original-source capture; the first record-tool
syntax failure is retained. No scratch or cleanup operation used `/tmp`; any later assigned scratch uses
the approved home-backed `BOUNDARY-Oath` directory. Existing archives and user files were preserved.

Independent content/citation review, product editing, sampling, compilation and runtime qualification
remain pending. Continue the pin while waiting for the user's Windows CCID/HID ready handoff; review its
eventual delta separately. PIV physical deferrals remain in force.
