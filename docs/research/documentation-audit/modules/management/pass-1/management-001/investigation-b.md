# Management investigation B — configuration and wire claims

## Scope, baseline and limits

- Investigator: Sol 6.1 (`github-copilot/gpt-6.1-sol`), 2026-11-03.
- Source baseline verified: `da61d2e6d754b4c155cbb2579202e910986f4b3a`.
- Exclusive report ownership: this file. Semantic/edit ownership: all of
  `src/Management/src/DeviceConfig.cs` and `src/Management/src/SetDeviceConfigOptions.cs`.
  A owns session/backend/test comments; C owns guides/examples/references. Their source was read as
  evidence, not edited. Narrow Core dependencies are evidence, not an expansion into the Core audit.
- Read root `CLAUDE.md`, module README/CLAUDE, workflow, approved style, and run plan; reviewed
  `.claude/skills/`. Consulted the security guidelines for memory classification and the verification
  skill before reporting. No implementation/build/test/commit skill execution was needed or authorized.
- The checkout already contained campaign edits to the audit README/workflow, new campaign files,
  untracked audit-v2 reports and `mise.toml`. These are other work and were preserved. One untracked
  report contains a relevant historical timeout decision, identified explicitly below.
- No builds, tests, experiments, hardware operations, commits or delegation were performed. Assertions
  described below were **read**, not executed. No firmware, OS or transport was freshly qualified.
- All findings are candidates for coordinator triage, not accepted changes or independent review.
  Public documentation corrections can proceed in Pass 1; runtime/API/test changes wait for the
  campaign's second-sweep Pass 2. Device evidence remains not-run independently of doc status.

## Main conclusions

The encoder omits null configuration properties, writes full replacement masks, and already implements
the wire operations needed to set, replace and remove a configuration lock. The short property XML does
not explain those distinctions sufficiently. The NFC restriction summary is materially imprecise:
USB **power**, not touch or authenticated use, clears the shipping restriction; the feature is limited
to NFC-capable devices with firmware 5.7 and later. There is no field-specific SDK gate.

Do not reopen the timeout ceilings as an inferred implementation bug: the historical maintainer record
explicitly retains 3600/60 as SDK limits. They are not established firmware maxima. `NfcRestricted = false`
is encoded, but its device semantics remain unproven by the primary protocol reference. Preserve that
uncertainty rather than publishing a reboot-based explanation from test comments.

## Focused external evidence

All URLs below were accessed on **2026-11-03**. Quotes are focused passages retained from fetched pages,
not search snippets alone. The numbered evidence IDs are local to this report. These vendor pages are
not silently promoted to a versioned external standard.

### E1 — YubiKey 5 Series Configuration Reference Guide

URL: <https://developers.yubico.com/yubikey-manager/Config_Reference.html>

Status/revision: live official developer reference; no numbered revision or publication date identified
in the fetched document. Applicability passage: “The format described below is only available on
YubiKey 5 Series and above, and the Security Key by Yubico.” Device Information separately says its
read format expands the YubiKey 4 capabilities interface. Do not apply every feature to every family
merely because its firmware passes the session's generic 5.0.0 check.

Actor/layer/strength: vendor-defined device protocol and host command format, authoritative descriptive
requirements for this interface, not RFC-style capitalized MUST language or proof of SDK enforcement.

- **Capabilities / List of Capabilities:** “Each Application is defined as a single bit and is enabled
  if the bit is set and disabled if not.” “The Enabled Capabilities are always masked with the Supported
  Capabilities.” Supported capabilities “are set during manufacturing” and “cannot be modified.”
- **Writing Information:** “The format for providing the data to be written is a one Byte total length
  followed by a list of TAG-LENGTH-VALUE (TLV) triplets”. “Multiple information can be set as part of the
  same interface command simply by concatenating several TLVs.”
- **Enabled USB Capabilities:** “TAG = 0x03”; “set by supplying the complete list of packed bits. A set
  bit means that the related Application is enabled, a cleared bit means that the related Application
  is not available through the interface.”
- **Configuration Lock:** “The Configuration Lock is a 16 Byte value that can be set by the user or an
  administrator/crypto officer.” If set, changing the described user-configurable information is not
  allowed without supplying it in SET DEVICE INFORMATION. Default is sixteen `00` bytes, disabled.
- **Set Configuration Lock:** “TAG = 0x0a”; “If the payload is the Byte 00 repeated 16 times, the
  Configuration Lock is disabled”; otherwise “the value is stored as the Configuration Lock”.
- **Unlock Configuration Lock:** “TAG = 0x0b”; “The payload has to be the correct 16 Byte Configuration
  Lock.” “If the Configuration Lock is disabled, this TAG-LENGTH-VALUE (TLV) has no effect.” It “can be
  present anywhere in the list of TLVs” but “can not be part of a separate command.” “All interface
  commands requiring the Configuration Lock must contain an Unlock Configuration Lock TLV.” This is
  authorization of this command, not removal of the persistent code or session-wide authentication.
- **Auto-Eject Timeout:** “controls the period of inactivity (in seconds) after which the CCID card
  should be considered ejected.” “This is only meaningful if the Device Flag, MODE_FLAG_EJECT, is set.”
  Write section: “TAG = 0x06”; “a 2 Bytes Big-Endian value.”
- **Challenge-Response Timeout:** “controls the period of time (in seconds) after which the OTP module
  Challenge-Response should timeout. The default is 15 seconds.” Write section: “TAG = 0x07”; “a 1 Byte
  value.” Neither timeout passage establishes a 3600/60 device maximum.
- **Device Flags:** `MODE_FLAG_EJECT = 0x80`: “In pure CCID mode eject/inject the card with button”;
  `MODE_REMOTE_WAKEUP = 0x40`: “Enable remote wakeup”. Write section: “TAG = 0x08”; “a 1 Byte value.”
- **Reset:** “TAG = 0x0c”; “This TLV doesn’t require a payload (its length can be zero). If present as
  part of a list of TLVs forces the device to reboot if all the other TLVs have been correctly parsed.”
  “missing/wrong Configuration Lock will produce an error and not reboot”. This is reboot, not factory
  reset. It does not establish a universal re-enumeration delay.

**Reference defect:** the write subsection headed “Enabled NFC Capabilities” says “TAG = 0x0c” and then
“Enabled USB Capabilities”. The same page's read table assigns enabled NFC `0x0e`, and its Reset subsection
assigns reboot `0x0c`. Do not copy the contradictory NFC write tag. E5 corroborates SDK `0x0e`, but sibling
agreement is not independent normative firmware evidence. The page also labels `0x0100` unused whereas
current code models HsmAuth there; it is not an exhaustive current-feature inventory. NFC restriction
tag `0x17` is absent from this reference.

### E2 — YubiKey Technical Manual, “5.7 - 5.6 Firmware Specifics”, Restricted NFC

URL: <https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-firmware-5.7.html#restricted-nfc>

Status/revision: live official technical manual, section covering 5.7/5.6 and later additions including
5.7.4; no immutable manual revision identified. Actor/layer: device behavior and end-user activation,
descriptive product requirements, not a byte-level SET CONFIG definition.

- “Restricted NFC mode prevents wireless device manipulation before a YubiKey NFC with the 5.7 and
  later firmware is taken out of its blister pack or other packaging such as a tray.” It is “enabled
  by default on new NFC keys with the 5.7 and later firmware.”
- “the only permitted action via the NFC connection is reading the URL configured by Yubico on the
  NDEF tag set by Yubico.” Thus “will not respond over NFC” is too broad.
- “The end user is instructed to plug the key into USB power such as a USB charger or computer USB port
  for 3 seconds. This action is sufficient to disable Restricted NFC mode.”
- “The user can re-enable the restriction as often as they desire using ykman config nfc.”

### E3 — YubiKey Manager (ykman) CLI Guide, CONFIG Commands

URL: <https://docs.yubico.com/software/yubikey/tools/ykman/Config_Commands.html>

Status/revision: live official CLI guide without a fixed version identified in fetched prose. Actor/layer:
CLI user and command behavior; authoritative for that tool, corroborating product conditions, not a
promise that this SDK implements the CLI's extra policy.

- `config nfc --restrict`: “Disable NFC for transport. Re-enable by USB power. Available for YubiKeys
  with firmware version 5.7 and later.”
- `config usb --autoeject-timeout`: “When set the smartcard automatically ejects after the specified
  time. Implies --touch-eject.” Touch-eject is “CCID only”. **That implication is CLI behavior**; our
  `AutoEjectTimeout` setter does not set `DeviceFlags.TouchEject` automatically.
- `config set-lock-code`: “The configuration lock code only applies to the management application.”
  Setting/deleting an OATH-TOTP credential does not trigger this prompt. The command was introduced with
  firmware 5.0. Do not confuse configuration locking with application PIN/access-key protection.
- `config reset`: “This command is only used with the YubiKey Bio Multi-protocol Edition.” Consequently
  a generic firmware threshold is not sufficient evidence for a lost-code recovery promise. A/C own
  broader reset prose and should reconcile it separately.

### E4 — official v1 .NET API reference (corroboration, not v2/firmware authority)

URLs:

- <https://docs.yubico.com/yesdk/yubikey-api/Yubico.YubiKey.IYubiKeyDevice.html>
- <https://docs.yubico.com/yesdk/yubikey-api/Yubico.YubiKey.Management.Commands.SetDeviceInfoBaseCommand.html>

Status/revision: live generated reference for `Yubico.YubiKey.dll` (v1 surface); fixed package version
not identified. Actor/layer: callers of those v1 methods. No v1 method postcondition is automatically a
v2 session or firmware contract.

- `SetAutoEjectTimeout`: “A value of 0 means that the timeout is disabled (the smart card will not be
  ejected automatically).” Range `ushort.MinValue` through `ushort.MaxValue`.
- `SetChallengeResponseTimeout`: “The value must be in the range 0-255, where 0 resets the value to its
  default.” E1 separately establishes a default of 15 seconds. This is corroboration for explaining
  zero, **not** a reason to expand the v2 accepted range.
- `SetDeviceFlags`: “A set flag means that the setting is enabled. Otherwise, the capability is
  disabled.” Supports full replacement rather than bitwise accumulation.
- `SetEnabledUsbCapabilities`: “At least one available USB capability must be enabled.”
- `ApplyLockCode`: “Temporarily unlocks the YubiKey's configuration by applying the lock code.”
- `SetLockCode`: “A value of all zeros (16 bytes) will clear the code.”
- `SetIsNfcRestricted`: “Set this value to true to restrict NFC, false to allow NFC.” This generated
  **sibling API** text does not settle exact firmware handling of our `17 01 00` request.

### E5 — official published sibling source, yubikey-manager 5.9.0

URL: <https://developers.yubico.com/yubikey-manager/API_Documentation/_modules/yubikit/management.html>

Status/revision: page explicitly headed `yubikey-manager 5.9.0 documentation`; source implementation,
not normative device specification. Actor/layer: Python host encoder/session. This is the single focused
follow-up for wire/NFC/range ambiguities after E1–E4; stop rather than infer an unproved device contract.

- Constants: `TAG_NFC_ENABLED = 0x0E`, `TAG_REBOOT = 0x0C`, `TAG_NFC_RESTRICTED = 0x17`.
- `DeviceConfig.get_bytes` order: reboot, current lock, USB, NFC, auto-eject, challenge-response, flags,
  new lock, restriction. It ends with `return int2bytes(len(buf)) + buf`.
- Restriction emission is specifically `if self.nfc_restricted: ... Tlv(TAG_NFC_RESTRICTED, b"\1")`.
  It does **not** corroborate emitting a false restriction byte. This difference is evidence of
  uncertainty, not proof our encoder is wrong.
- Python `write_device_config` checks lock-code length 16 and requires version 5.0.0. Its generic
  encoder does not establish our tighter timeout limits or device support for every value.

### E6 — .NET memory ownership, consulted through Context7

URL: <https://github.com/dotnet/docs/blob/main/docs/standard/memory-and-spans/memory-t-usage-guidelines.md>

Status/revision: current Microsoft .NET usage guidance, `main`, not a pinned runtime implementation.
Context7 library `/dotnet/docs`; query concerned borrowed `ReadOnlyMemory<T>` and asynchronous lifetimes.
Actor/layer: library/application memory consumers, usage guidance rather than device protocol.

- “The owner of a buffer instance is responsible for lifetime management, including destroying the
  buffer when it's no longer in use.”
- Rule 4: “If your method accepts a Memory<T> and returns a Task, you must not use the Memory<T> instance
  after the Task transitions to a terminal state”. This supports caller cleanup after completion,
  including failure/cancellation, not transfer of caller memory ownership to the session.

Two guessed v1 guide URLs returned `CRAWL_NOT_FOUND`; no claim relies on them. Fetched originals and the
quotes above, rather than successful search appearance, are the citation evidence retained here.

## Candidate findings and dispositions

### B-01 — explain full replacement masks at each write property

- **Observed:** `UsbEnabled`/`NfcEnabled` XML says “Applications to enable”, which can be mistaken for
  additive enablement. `GetBytes` writes the entire two-byte mask. `DeviceFlags` likewise writes one
  replacement byte, not a toggle. Type-level example demonstrates subtraction but does not state the
  consequence for other applications/flags. Null omission is correctly implemented and documented.
- **Desired documentation:** say non-null values replace the enabled set/flags; omitted bits are
  disabled; null preserves the current setting. Derive a mask from `DeviceInfo` to change selected
  applications while preserving others. NFC `None` intentionally requests all NFC application bits
  cleared; unlike USB `None`, the SDK accepts it. This is distinct from temporary NFC restriction.
- **Evidence:** `DeviceConfig.cs:21–40,63–67,98–99,131–150`; E1/E4. Device capability support is not
  created by setting bits. Advise checking `UsbSupported`/`NfcSupported` rather than promising unsupported
  requests succeed or fail in one particular way.
- **Disposition:** `candidate` documentation correction, medium practical importance. No runtime change
  required. C should reconcile README “Enable or disable” and flags examples; A owns session operation
  XML. Validate observed limitations separately under B-05.

### B-02 — document current-code authorization versus replacing/removing the lock

- **Observed:** both option properties only say “optional borrowed 16-byte ... lock code”. The encoder
  already uses `0x0B` for current and `0x0A` for new, sends them in one command, and accepts an all-zero
  new code. The session enforces exactly 16 bytes for either present memory, not setters. Providing
  only the current code does not remove protection. README explains setting/replacing, not the explicit
  clear operation. The device, not this SDK, checks whether the current value is correct or necessary.
- **Desired documentation:** null current means no authorization TLV; supply the current code for
  **each** protected write, including changing/removing the code. Null new leaves the lock unchanged;
  nonzero 16-byte new sets/replaces it; sixteen zero bytes remove it, using the current code in the same
  operation if locked. `ReadOnlyMemory<byte>.Empty` is not the clear-code representation: it is present
  but invalid length. Do not call current-code authorization a session unlock. No code prompt, random
  generation, persistence or recovery is performed for callers.
- **Evidence:** `SetDeviceConfigOptions.cs:27–31`; `ManagementSession.cs:133–145`;
  `DeviceConfig.cs:128–129,146–147`; E1, corroborated by E4 and read-only inspection of
  `ManagementLockCodeTests.cs:35–86,97–135`. Those tests are not fresh firmware evidence.
- **Disposition:** `candidate` documentation correction, medium. Expand property-level XML; C owns
  lock/clear procedure and secret-storage/cleanup example, A owns operation exceptions. Avoid an
  unqualified reset-based lost-code recovery promise (E3).

### B-03 — NFC restriction needs firmware, power and NDEF qualifications

- **Observed:** XML says “restricted until the key has been used over USB.” This suggests an actual
  protocol operation and omits firmware/family. Encoder emits `17 01 01` for true, `17 01 00` for false,
  nothing for null. Session gates only Set Config at 5.0.0; it does not check NFC hardware or 5.7.0.
- **Desired documentation:** temporary shipping restriction on NFC-capable devices with firmware 5.7
  and later; USB power for three seconds is sufficient to remove it (no touch, PIN or successful host
  operation needed). The configured NDEF URL remains readable. Explain null omission separately from
  false, without claiming verified device behavior for false. Distinguish it from `NfcEnabled`.
- **Evidence:** `DeviceConfig.cs:101–102,149–150`; `ManagementSession.cs:35–36,129–145`; E2/E3.
  `ManagementSessionCapabilityTests.cs:81–124` asserts true readback only. Its comments claiming USB
  “touch”, total NFC nonresponse, false requiring reboot and queued restoration are not established by
  those assertions or E2. A owns correcting those comments; changing test behavior waits for Pass 2.
- **Disposition:** `candidate` doc correction; **separate** `deferred-pass-2` missing preflight gate/
  capability policy candidate, not an assertion that current device rejections are the intended API.
  Existing migration record `docs/migration/v1-to-v2-gaps.md:333–336` already flags this gap; deduplicate.

### B-04 — false NFC encoding remains an explicitly bounded evidence gap

- **Observed:** our `17 01 00` is certain from source. E1 does not specify tag `0x17`; E2/E3 specify the
  USB-power recovery route but not the effect of this byte. E4's sibling API claims false allows NFC;
  E5's sibling encoder sends only true. Local integration tests do not assert false readback or
  restoration, and were not executed here.
- **Desired behavior:** not adjudicated. Do not change the bool API, omit false or assert false is
  ineffective based solely on sibling code. Equally, do not claim a reboot is required or that a false
  write queues restoration without primary firmware evidence.
- **Disposition:** `needs-decision` / `deferred-pass-2` investigation, medium uncertainty. Initial
  technical/API reference check plus one focused published-source follow-up exhausted the bound.
  Missing evidence: firmware-owner definition of `0x17` values and application timing, or authorized
  hardware observations covering true/false/null across NFC-capable firmware and USB power/reboot
  conditions. A coordinator decision is needed for how prominently to qualify false in Pass-1 XML;
  this report does not silently convert either hypothesis into a contract.

### B-05 — validation documentation overstates what is checked

- **Observed:** type remarks and `Validate` describe “rules that involve ... more than one property”.
  Actual `Validate` checks exactly one condition: `UsbEnabled == DeviceCapabilities.None`. It does not
  inspect a second property, known flag masks, supported device applications, firmware or current
  configuration. Setters check timeout ceilings only. Nonzero unsupported/unknown USB bits can pass;
  values are cast to ushort for capabilities and byte for flags, discarding higher bits in the usual
  unchecked build context. Consequently “at least one USB application” is not a guarantee of effective
  hardware reachability for arbitrary enum casts or applications absent on the selected key.
- **Desired documentation:** state the actual USB-None check and timeout validation timing. Tell
  callers to preserve at least one **supported** USB application, rather than promise comprehensive
  preflight enforcement. Avoid treating the SDK's inability to verify support as intentional permission
  to send nonsensical masks. Do not add a closed-enum restriction in this pass.
- **Evidence:** `DeviceConfig.cs:37–40,63,78–80,91–93,104–115,131–144`; Core capability/flag enum
  declarations and E1's supported-mask rule. Concrete source-derived example: USB
  `(DeviceCapabilities)0x10000` is not None at validation, but the unchecked `(ushort)` conversion emits
  zero; no execution/device outcome claimed. No `CheckForOverflowUnderflow` setting was found in the
  repository's `.props` files; a future checked-build policy would change narrowing failure behavior.
- **Disposition:** `candidate` doc correction. Mask-width validation and device-aware safety are
  `deferred-pass-2` / `needs-decision`, medium: determine intended request extensibility and supported
  capability enforcement first. Source-only observation, not a confirmed hardware lockout. A's audit
  test summary (“rule must hold ... however constructed”) should be scoped to what assertions cover.

### B-06 — timeout meaning and SDK range must not be conflated

- **Observed:** XML and setters agree on 0–3600 auto-eject and 0–60 challenge-response. They omit the
  special meaning of zero; auto-eject XML also omits **inactivity**, despite correct CCID-only/TouchEject
  conditions. E1 specifies two-byte/one-byte wire values, not those maxima. E4 describes wider v1 ranges.
- **Historical decision:** `docs/research/audit-v2-closeout-review.md:61` (existing untracked user work)
  records: “A approved ... Retain AutoEjectTimeout 0–3600 seconds and ChallengeResponseTimeout 0–60
  seconds.” It explicitly says this is documentation/validation consistency, “not independent proof of
  firmware maximums.” Treat as recorded maintainer intent, not newly issued authorization or normative
  protocol evidence; preserve the file untouched.
- **Desired documentation:** retain these **SDK-accepted** ranges, with assignment-time
  `ArgumentOutOfRangeException`. Auto-eject is an inactivity timeout and does not implicitly enable
  TouchEject. Explain zero as disabled auto-eject/default challenge-response, supported by E4 with E1's
  15-second default; an independent reviewer should recheck those consequential zero clauses against
  the original API passages. Do not expand ranges or advertise 3600/60 as firmware maxima.
- **Disposition:** `candidate` doc improvement; range mismatch bug recommendation **rejected for this
  cycle** in light of the recorded decision. No runtime change selected. Device-specific range/default
  qualification remains not-run, not a claim that every family accepts all encoded values. No further
  ambiguity research was attempted after the published-source follow-up failed to establish maxima.

### B-07 — make reboot and secret lifetime options actionable

- **Observed:** `Reboot` summary says whether the key “reboots after applying”, not that false is the
  default or that this is a reboot request contingent on accepted parsing/authorization. Borrowed-memory
  remarks correctly retain caller ownership and cleanup duty. They do not explicitly say to keep input
  valid/unmodified until completion, including the extension's session-creation awaits.
- **Desired documentation:** default false means no reboot TLV; true requests reboot after successful
  configuration parsing/authorization, not a factory reset. A resulting disconnect invalidates existing
  sessions; caller must rediscover/reopen as appropriate (A/C own detailed lifecycle wording). Keep
  borrowed buffers valid and unchanged until the operation completes; clear owned secrets in `finally`
  on success, failure and cancellation. The options object does not own/clear the source buffer; a
  caller-retained options object still references it. No promise of zeroing every transport-layer copy.
- **Evidence:** `SetDeviceConfigOptions.cs:17–31`; `DeviceConfig.cs:125–129,146–172`;
  `ManagementSession.cs:144–160`; `IYubiKeyExtensions.cs:72–81`; E1 Reset and E6.
- **Disposition:** `candidate` doc clarification, low/medium. Existing ownership/zeroing assertions are
  locally correct; this is not a newly discovered leak. A owns operation/error/cancellation wording.

### B-08 — distinguish local wire order from a protocol ordering requirement

- **Observed:** internal `GetBytes` XML accurately says fixed order, reboot/unlock first when present,
  not sorted by tag. Audit test cites sibling revision `4ca60f7`, not a specification. E1 explicitly
  allows unlock anywhere in the same command and does not require sorted tags.
- **Desired documentation:** retain useful local implementation description without elevating sibling
  order to a protocol requirement. Optionally add that returned mutable memory is caller-owned and
  must be cleared after transmission, and intermediate lock-code copies are disposed/cleared.
- **Evidence:** `DeviceConfig.cs:45–58,117–172`; E1/E5; Core `Tlv.Encode` and `Tlv.Dispose`,
  `TlvHelper.EncodeList`. No correction to actual tag values/order is justified by E1's NFC typo.
- **Disposition:** clean implementation explanation with optional documentation improvement; reference
  caveat to A/C, not an independent runtime bug. Do not change encoder ordering in Pass 1.

## Per-file / semantic-unit coverage

All units below were read in full at the baseline. “Clean” means no required correction in the stated
dimension, not runtime/hardware verification. Copyright/license boilerplate is retained, not rewritten.
Compiler-generated record constructors/equality/clone/ToString members are excluded from invented prose;
the record contains no lock-code fields and no caller secret to dispose.

| Unit | Coverage / disposition | Evidence or finding |
| --- | --- | --- |
| `DeviceConfig.cs` type summary: optional/null values | Clean semantic statement | Every configuration field is guarded by a non-null pattern before TLV emission; null does not encode zero/false. |
| Type remarks: DeviceInfo correspondence, derivation example | Read; useful composable fragment, not a complete program | B-01/B-05. Session is an implicit typed prerequisite. Example may reach None if OTP is the only USB app; validation then rejects it, not silently leaves USB unchanged. |
| Type remarks: validation timing/extent | Finding | B-05/B-06; limits checked by timeout setters, USB None checked at Validate/GetBytes. |
| USB enabled property | Finding | B-01/B-05: full replacement, null omission, supported application responsibility. |
| NFC enabled property | Finding | B-01/B-03: replacement, None allowed, distinct from restriction; check hardware support. |
| Auto-eject property, getter/init/backing field | Finding plus clean range enforcement | B-06. Nullable assignment accepted; >3600 rejected; zero transmitted as two zero bytes. |
| Challenge-response property, getter/init/backing field | Finding plus clean range enforcement | B-06. Nullable assignment accepted; >60 rejected; zero transmitted, not omitted. |
| Device flags property | Finding | B-01; whole byte replaces current flags. TouchEject requires CCID-only per E1; setting a timeout does not set the flag. |
| NFC restricted property | Finding / evidence gap | B-03/B-04; null versus explicit false/true encodings separately checked. |
| Public `Validate`, XML summary/exception | Finding; exact None rejection clean | B-05. No device I/O; no capability-support or firmware validation. |
| Internal tag constants comment and values | Clean local encoding, external citation caveat | B-08/E1/E5. 03/06/07/08/0A/0B/0C/0E/17 traced individually. |
| Max length constant/comment | Clean | One-byte prefix excludes itself; SDK rejects body length >255. Not a claim that each transport accepts a 255-byte body. |
| Timeout constants/backing fields | Clean under recorded SDK policy | B-06; do not convert into firmware constants. |
| Internal `GetBytes` summary | Accurate local order, optional ownership detail | B-08. |
| `GetBytes`: Validate and conditional TLV assembly | Read, null/false/zero distinctions verified from source | B-01–B-05. Reboot/current/new also conditionally added; no lock side effect for null options. |
| `GetBytes`: serialization/prefix/size | Clean for modeled ordinary values | USB/NFC/auto-eject two-byte big endian; challenge/flags/restriction one byte; current/new 16 through public session; empty config yields `[00]`. No device no-op guarantee inferred. |
| `GetBytes`: intermediate/final cleanup explanatory unit | Clean at this layer | TLVs own encoded copies, disposed in outer finally; encoded intermediate cleared in inner finally; returned buffer deliberately escapes and session clears it in its own finally. |
| Private `UInt16BigEndian` helper | Clean, no explanatory prose needed | High byte then low byte, used for USB/NFC/auto-eject only. |
| `SetDeviceConfigOptions.cs` type summary | Broad but not false | B-07 optional direct task wording; options carry command controls rather than a persistent policy engine. |
| Type remarks: borrowed memory, ownership, retention | Clean core contract; practical expansion | B-07/E6. No private clone/IDisposable requirement for this borrowed-memory holder. |
| Public `Reboot` | Finding / clarification | B-07; default false, successful command condition, not factory reset. |
| Public `CurrentLockCode` | Finding | B-02/B-07; exactly 16 if present, per-command authorization, does not remove lock. |
| Public `NewLockCode` | Finding | B-02/B-07; null/no change, nonzero/set-replace, all zero/remove. |
| Both owned files: TODO/specification/development-residue sweep | Clean | No TODO/FIXME/HACK, historical implementation narration or substantive explicit spec URL inside these two files. Internal TLV/zeroing comments are useful present-tense context. |

Wire sanity by static inspection: all nine modeled TLVs together have a 59-byte body (2 reboot + 18
current + 4 USB + 4 NFC + 4 auto-eject + 3 challenge + 3 flags + 18 new + 3 restriction), hence 60 bytes
including the prefix when public-session lock-code checks have passed. Current widths therefore use
one-byte TLV lengths even though Core's generic encoder supports BER long-form lengths. This arithmetic
is not a transport execution result or an argument for deleting the generic size guard.

## Supporting source and assertion evidence (not owned edits)

- Read complete `ManagementSession.cs`, `IManagementSession.cs`, `IYubiKeyExtensions.cs` and the three
  concrete backend files. Each write backend forwards the configuration bytes without changing their
  modeled TLVs. No layer shown here adds a per-field NFC gate. No attempt to prove all lower protocol
  allocations/zeroing was made; public cleanup claims should name the **encoded configuration buffer**.
- Read complete `ManagementAuditReproTests.cs`: record derivation independence, exact USB None
  rejection, null USB omission in one NFC-only packet, one above-limit case per timeout, and one
  multi-field packet with current lock and true restriction. It does not establish all enum masks,
  new-lock/all-zero encoding, false restriction semantics or actual firmware ordering requirements.
- Read `ManagementSessionTests.cs:1–260`, including complete success-path encoded-buffer zeroing test
  and capturing backend. The test checks a retained view is zero after a successful fake backend write;
  it does not assert caller lock-code clearing or failure/cancellation cleanup. Session `finally` provides
  static evidence for final-buffer cleanup on those paths, not a newly executed regression result.
- Read complete `ManagementLockCodeTests.cs`: set/read locked, missing-code failure, current-code write,
  clear/read unlocked; separate auto-eject change/readback. Both are SmartCard/minimum-5.0 tests, not
  all-family/transport evidence. Their cleanup code can skip `ZeroMemory(lockCode)` when a preceding
  restoration write/read throws. This is a **test implementation** cleanup gap for A/Pass 2, not evidence
  that the production encoded buffer is leaked. Do not duplicate it if A records the same finding.
- Read complete `ManagementSessionCapabilityTests.cs` and `ManagementSessionAdvancedTests.cs` as
  corroboration. Capability test checks persisted bit readback without reboot, not activation across
  re-enumeration. Restriction test reads true only; no verified restoration postcondition. A owns comment
  accuracy/cleanup candidates. Advanced tests do not add range, false-restriction or lock evidence.
- Read complete Core `DeviceCapabilities.cs`, `DeviceFlags.cs`, `Tlv.cs`, `TlvHelper.cs`, and
  `DeviceInfo.cs:1–190`. The read-side timeout is `ReadOnlyMemory<byte>` (not byte), restriction property
  is `IsNfcRestricted` (not `NfcRestricted`), and absent optional fields can default to zero/None. Thus
  “write-side counterpart” is conceptual, not a lossless typed round-trip. No automatic copy-all example
  should be invented. Core representation changes remain outside Pass 1/this ownership.
- Read the active migration Management section `docs/migration/v1-to-v2-gaps.md:320–342`: it already
  records the NFC gate and read-timeout-shape gap; its `DeviceConfig.Builder` language is stale. C owns
  active migration edits. The historical timeout decision was read but not edited or re-authorized.

## Claim-to-document dependencies and Pass-2 re-review triggers

Paths below are rooted at the repository; module shorthand `Management/` means `src/Management/`.
All implementation items remain unimplemented/unverified in this investigation.

| Candidate | Implementation / decision dependency | Documentation and explanatory consumers to re-review |
| --- | --- | --- |
| B-01/B-05 | `Management/src/DeviceConfig.cs` mask encoding, Validate, potential device-supported-mask policy; Core capability/flag carriers | Both property XML and type example; Management README enable/disable and constraints; CLAUDE configuration/flags patterns and USB gotcha; A's session XML/test claims; C's ManagementTool masks; active migration configuration coverage. Reopen when mask validation, width, replacement or support policy changes. |
| B-02/B-07 | `DeviceConfig.GetBytes`, `ManagementSession.SetDeviceConfigAsync`, options borrowing and extension awaits | Options property/type XML; README lock/security/clear instructions; CLAUDE lock gotcha; IManagementSession and extension docs; lock-code tests/examples. Reopen if authorization lifetime, code length, all-zero semantics or code ownership changes. |
| B-03/B-04 | Per-field gate, NFC hardware policy, authoritative tag-17 true/false/timing behavior | Restriction property XML; README requirements/configuration/security; CLAUDE NFC gotcha; migration per-field gate; capability test explanations and restoration claims; ManagementTool restriction UI. Reopen after firmware evidence or implementation changes; remove qualifications only when evidence supports it. |
| B-06 | Existing retained SDK limits; any future explicit maintainer range decision, default/zero device evidence | Timeout XML/constants explanations; README property correspondence; CLAUDE config example; ManagementTool input ranges and prompts; above-limit test comments and migration. No automatic change to 65535/255 merely because wire fields fit. |
| B-08 | Generic TLV helper ownership/order/length, transport encoder contract, vendor reference erratum | Internal GetBytes XML/tag comment/cleanup rationale; A's wire-order test citation; C's config-reference link and TLV narrative. Reopen if order, tags, buffer ownership or length framing changes. |

## Handoff and verification status

Eight B-* candidates are recorded, not eight independent bugs: B-03/B-04 intentionally separate a
documented product condition from unresolved false-byte behavior; B-01/B-05 separate replacement
semantics from incomplete enforcement. B-08's observed order is clean, with an optional explanation
and citation caveat. B-06 retains the existing maintainer-selected SDK limits rather than reopening
a settled range choice. Coordinator should assign canonical MGMT IDs and deduplicate A/C overlaps.

This report covers every manually authored public member and internal explanatory unit in the two
owned files. Independent review, clean-unit sampling, source/doc edits, builds/tests, example compilation,
hardware work, commits and remote backup were not performed by B. The only authored artifact is this
report. Initial research plus one focused follow-up left the tag-17 false behavior, exact firmware
range maxima and device-aware mask policy explicitly unresolved; missing evidence is preserved above.
