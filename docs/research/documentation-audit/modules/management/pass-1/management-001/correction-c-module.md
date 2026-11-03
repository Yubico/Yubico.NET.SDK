# Management C module-review corrections

## Status and scope

Editor: GPT-6.1 Sol (`github-copilot/gpt-6.1-sol`), 2026-11-03. Verified the five required findings in
[review-c-module.md](review-c-module.md) against the current source, then corrected only these worktree
files in `/tmp/opencode/management-001-edit-c`:

- `src/Management/CLAUDE.md` — C-MOD-R1.
- `src/Management/README.md` — C-MOD-R2/R3.
- `src/Management/examples/ManagementTool/README.md` — C-MOD-R4.
- `src/Management/tests/CLAUDE.md` — C-MOD-R5.

Also updated PRIMARY `edit-c.md` mapping/classification and wrote this report. No outside-module working
file was edited during this correction round; C2 review remains separate. No C# executable tokens,
strings, directives, API shapes, tests or runtime behavior were changed. No build, test, hardware
operation, example execution, commit or delegation occurred. Changes are **ready for the same independent
reviewer's focused re-review**, not independently accepted yet.

Root/module/test/shared instructions and the workflow/style were already loaded for this editor task;
loaded `review-receive` for source-first review handling. No compile source or no-op scaffolding was
created. Build/test/commit operations remain prohibited, so their execution skills were not actioned.

## Coordinator erratum request — C-10 and triage

The original C-10 investigation and triage IMP-014 incorrectly omit recognized top-level help tokens.
Please add a leading, dated erratum to those records rather than silently rewriting their original
evidence. The verified dispatcher recognizes `info`, `config`, **`-h`, `--help`, and `help`** as the
first token (`Program.cs:34–42`). The real limitation is that additional arguments are ignored: command
handlers receive no remaining arguments, so `info --help` performs the information task rather than
command-specific help. NFC restriction remains unreachable through current menus/dispatch. Monitoring
starts before dispatch (`Program.cs:11–19`), so top-level usage dispatch is not a no-device-activity claim.

This corrects the investigation's dispatch description without cancelling the command-specific-help
defect or changing MGMT-IMP-014's deferred status. The tool README and updated edit manifest now use the
correct distinction. Original investigation, triage and independent-review files were not edited here.

## Original required findings and evidence — preserved verbatim

The following is the original required-findings section from `review-c-module.md`, including the
reviewer's source/reference evidence and scope limits. Line locations refer to the originally reviewed
bytes, not the corrected files. The original reviewer report remains unchanged.

```text
### C-MOD-R1 — dispose the SCP parameters created by the contributor example

- **Location:** `src/Management/CLAUDE.md:242–258`, complete “Low-Level: CreateManagementSessionAsync” block.
- **Problem:** `ScpKeyParameters = Scp03KeyParameters.Default` creates a disposable parameter object and
  owned key arrays, but no variable or disposal scope retains responsibility for them. Disposing the
  returned session does not dispose these borrowed initialization parameters. The block currently teaches
  connection/session cleanup while omitting another resource it creates.
- **Independent source evidence:** `Core/src/Protocols/SmartCard/Scp/Scp03KeyParameters.cs:45–57` makes
  `Default` a fresh `new(...)`, with `Dispose()` calling `Keys.Dispose()`;
  `StaticKeys.cs:52–61,83–99` clones three key arrays and zeroes them only on disposal.
  `ManagementSession.InitializeAsync` passes parameters to `ConfigureProtocolAsync` without taking
  ownership; `ApplicationSession.cs:359–397,480–565` establishes SCP and later disposes protocol/owned
  connection, not the input parameters. `SessionCreationOptions.cs:35–39` corroborates caller ownership.
- **Required edit:** introduce `using var scpParameters = Scp03KeyParameters.Default;` before session
  creation, assign that variable in options, and keep disposal order explicit (session ends before its
  outer parameter scope). State that the parameters are caller-owned and borrowed for initialization.
  Preserve the real factory and both awaited reads. Do not remove SCP from the example or substitute
  a fake/no-op parameter source to make the lifetime problem disappear.
- **Scope/limit:** this is a documentation-example ownership defect, not a finding of an SDK session-key
  leak. The demonstrated keys are public defaults; their sensitivity does not change the type's ownership
  contract or the need for a correct reusable example pattern. Related group: MGMT-DOC-006/011.

### C-MOD-R2 — distinguish Management command credentials from optional SCP keys

- **Location:** `src/Management/README.md:205–209`, complete User interaction opening paragraph.
- **Problem:** “The only secret it handles is the 16-byte configuration lock code” is too broad for a
  module whose factories accept optional SCP key parameters. It can also make “no management key” sound
  like no cryptographic key material is ever involved in initialization.
- **Independent source evidence:** `ManagementSession.cs:79–108` accepts and forwards `ScpKeyParameters`;
  `ApplicationSession.cs:382–388` uses them to establish SCP; `Scp03KeyParameters.cs:18–43` contains
  static ENC/MAC/DEK material. The ordinary configuration operation separately reads current/new lock
  memory at `ManagementSession.cs:133–145`. The constructor at `:47–49` uses the base overload with no
  prompt, so the adjacent not-retained/invoked/disposed claim is accurate and should remain.
- **Required edit:** narrow the claim to the command-level configuration credential, and explicitly
  distinguish optional caller-owned SCP initialization keys. For example, ordinary Management commands
  do not need a PIN/PIV management key; configuration protection uses the 16-byte lock code, while optional
  SCP uses separately supplied key parameters. Do not imply ownership transfer or prompt retention.
- **Scope:** factual contract qualification, not a new secret-input feature. MGMT-DOC-006/011.

### C-MOD-R3 — qualify the reboot effect by whether the requested reboot occurs

- **Location:** `src/Management/README.md:244–246`, Constraints reboot bullet.
- **Problem:** “SetDeviceConfigAsync with Reboot = true terminates every session” states an unconditional
  outcome from a request flag. A validation, authorization or device failure can prevent the reboot.
  Nor does this source mark all managed session objects disposed when encoding that flag.
- **Independent source/reference evidence:** `DeviceConfig.GetBytes:125–126` adds the reboot TLV;
  `ManagementSession.SetDeviceConfigAsync:127–159` validates, encodes, sends and clears the request with
  no global session-state transition. Original reference E1, Writing Information / Reset, says the TLV
  forces reboot **if all other TLVs have been correctly parsed**, and a missing/wrong configuration lock
  produces an error and does not reboot.
- **Required edit:** describe `Reboot = true` as a request; **if the requested reboot occurs**, existing
  sessions/connections must no longer be reused. Retain the dispose → rediscover intended key → fresh
  scopes procedure and capability-change blast-radius warning. Do not promise that managed session
  objects are automatically invalidated/disposed.
- **Scope:** align the guide with the request/result distinction already correctly explained by
  `ConfigResult` and the tool guide. MGMT-DOC-002/006.

### C-MOD-R4 — document real top-level help separately from ignored command-specific help

- **Location:** `src/Management/examples/ManagementTool/README.md:78–80`.
- **Problem:** “Program recognizes only info and config as the first command token” and unqualified
  “help flags are not parsed” contradict the actual dispatcher. The warning about `info --help` itself
  is correct and useful.
- **Independent source evidence:** `Program.cs:34–42` switches on the first token and recognizes
  `info`, `config`, **`-h`, `--help`, and `help`**. Only the first token is used; command handlers get no
  remaining arguments. `InfoCommand.ExecuteAsync` selects a device, opens an owned session and queries
  metadata; thus `info --help` really can perform the read task instead of showing command help.
- **Required edit:** state that the data commands are `info`/`config`, top-level `-h`/`--help`/`help`
  print usage, and extra command arguments are ignored. Preserve the command-specific-help warning and
  menu route for writes. No executable parsing/help-string changes are requested in Pass 1.
- **Limit:** top-level usage dispatch is not a claim of zero device activity: monitoring starts before
  dispatch. MGMT-DOC-012; MGMT-IMP-014 remains deferred.

### C-MOD-R5 — do not label a unit-plus-integration command “integration tests only”

- **Location:** `src/Management/tests/CLAUDE.md:81–82`.
- **Problem:** `dotnet toolchain.cs -- test --integration --project Management` includes Management unit
  projects as well as integration projects. The updated command reaches the hardware tests, but its label
  still misstates the selected suite.
- **Independent source evidence:** `scripts/toolchain/Context.cs:44–50` concatenates unit and integration
  project lists when `--integration` is present; `TestTargets.cs:21–24` selects from that combined list;
  `Discovery.cs:113–118` applies the project matcher rather than filtering to integration projects.
- **Required edit:** simplest correction: label it “Run unit and integration tests (requires YubiKey).”
  If integration-only execution is desired instead, use and source-check a specific integration-project
  selector. No runner command was executed by this reviewer.
- **Scope:** task usability/command description, MGMT-DOC-008/011. The separately filtered real-class
  command and unit-only command are not implicated by this finding.
```

## Fresh verification and corrections

All five findings were accepted only after source inspection; no speculative runtime fix was selected.

| Finding | Fresh editor verification | Applied correction / review locator |
| --- | --- | --- |
| R1 | Read all of `Scp03KeyParameters.cs`: Default constructs a new parameter object; Dispose calls Keys.Dispose. Read `StaticKeys.cs:52–99`: constructor clones ENC/MAC/DEK, disposal zeroes arrays, default factory creates a new set. Read `SessionCreationOptions.cs:35–39`, `ManagementSession.cs:79–108`, and `ApplicationSession.cs:359–397,480–565`: initialization borrows parameters, disposal tears down protocol/owned connection, not inputs. | Contributor “3. Low-Level: CreateManagementSessionAsync,” fence 10: real `using var scpParameters = Scp03KeyParameters.Default;` precedes `await using var mgmtSession`; options reference that variable. Both original awaited reads remain. Adjacent prose/comment explicitly states caller ownership, initialization borrowing and reverse disposal order (session before parameters). Factory failure also exits the parameter scope. |
| R2 | Read Management initialization and write paths: optional SCP parameters reach InitializeScpAsync, while per-command current/new lock memory is separate. No credential ownership transfer is present. Existing constructor path remains the previously verified unused-prompt behavior. | README “User interaction”: no touch/PIN/**PIV** management-key requirement for commands; configuration protection uses the 16-byte lock code; optional SCP uses separate caller-owned initialization parameters with caller disposal. Not-retained/invoked/disposed prompt statement remains unchanged in meaning. |
| R3 | Read `DeviceConfig.cs:118–150` and `ManagementSession.cs:122–160`: flag encodes a TLV, with validation/transmission/cleanup and no global managed-session disposal transition. Independently fetched original vendor reference below and read the complete Reset subsection. | README “Constraints” reboot bullet: request, not automatic managed-object disposal; **if the requested reboot occurs**, dispose existing scopes, rediscover the intended key and create fresh ones. Capability-change blast radius and Bio MPE reset distinction retained. |
| R4 | Read complete `Program.cs` and `InfoCommand.cs`: monitoring precedes dispatch; first-token switch recognizes info/config and all three top-level help forms; remaining args are not forwarded; info selects/query/displays via an owned session. | Tool README “Usage,” prose after the unchanged command fence: real data commands versus top-level help, ignored extra arguments, retained `info --help` warning, menu route for writes and monitoring-before-dispatch qualification. No parsing/help strings changed. |
| R5 | Read `Context.cs:37–50`, `TestTargets.cs:9–24`, `Discovery.cs:98–118`: --integration combines unit/integration candidates before project matching; Management matches both. | Test guide “Running Tests,” same command fence: only the label changes to “Run unit and integration tests (requires YubiKey).” All command tokens remain identical. |

Original reference fetched 2026-11-03:
<https://developers.yubico.com/yubikey-manager/Config_Reference.html>, **YubiKey 5 Series Configuration
Reference Guide**, live unversioned manufacturer reference, **Writing Information → Reset**. No immutable
revision is exposed. Actor/layer: device handling of host-supplied configuration TLV, not managed object
lifetime. Exact focused passages: “forces the device to reboot if all the other TLVs have been correctly
parsed”; “Reboot is determined by the Configuration Lock (missing/wrong Configuration Lock will produce
an error and not reboot).” This supports conditional request effects, not automatic disposal or observed
reboot completion.

## Final snippet/classification update

The module still has 43 Markdown fences; this round adds/removes none and changes no XML snippet.
`edit-c.md` now locates contributor fence 10 by its new first statement, `using var scpParameters`, and
retains its **fragment** classification. Typed prerequisites remain M + D +
`ProtocolConfiguration customProtocolConfig`; the actual SmartCard key must be configured for the supplied
defaults if executed. The snippet creates the real `Scp03KeyParameters` itself; it must not be supplied
by a fake stub or stripped to avoid disposal checking. Keep the using declaration, real session factory,
both awaited reads and disposal order together during compile extraction.

The tool command fence is unchanged and remains complete commands. Its following help prose is corrected.
The test command fence is unchanged except for its shell-comment label and remains complete commands;
the --integration line selects both Management suites. No build/test execution is authorized by those labels.

## Correction hashes and limited verification

| Corrected path | SHA-256 after correction |
| --- | --- |
| `src/Management/CLAUDE.md` | `42eae875aabe688b4856ecb19252c9edb4454fd4551977549a9a48f3da6c3d74` |
| `src/Management/README.md` | `e5a631ff57ad934ef294e9805ea6039fdace475dbc63613c92d98534bc5c878d` |
| `src/Management/examples/ManagementTool/README.md` | `3143005aa7f28a9a5843dfa7b0acefff8836b7f9aebb1fdbe7299162a973e50f` |
| `src/Management/tests/CLAUDE.md` | `081220c3f7d0e64c076aff8e93a11cf72eb2e97fa201d3cec7592b0c21d00449` |

`git diff --check da61d2e6 -- src/Management` completed with exit 0 and no output after the corrections.
This is whitespace validation only. Source/evidence review, not runtime execution, supports the changes.
Compiled-snippet, executable-token/directive/API checks and full authorized gates remain coordinator work.
All MGMT-IMP/MGMT-XREF behavior and device-evidence items remain deferred; C-MOD-R1 is an example resource
ownership correction, not a newly demonstrated SDK session-key leak. Original accepted module units and
outside-module changes are not reopened or silently marked PASS by this correction report.
