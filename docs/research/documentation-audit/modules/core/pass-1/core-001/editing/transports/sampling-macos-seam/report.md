# SAMPLE-B3-001 — macOS native-lifetime seam overview

## Outcome and focused patch

Verified the complete `src/Core/src/Transports/Hid/MacOS/IIOKitDeviceLifetime.cs` file and corrected
only its interface and production-class overview XML. The source supports the finding; no
contradiction was found. The correction is ready for independent review, not independently accepted.

The before-file matched accepted primary bytes and the transport revision-2 identity:

- Before SHA-256: `d76be7afb306e3496eab0e4ea591f61ea58a69a9e9d9f91df3d606fa947ff87a`.
- After SHA-256: `18d3f8ef6d7d2b72d0bb668ed6301eee634a2659c20ed89e2d8b75e1a69dadd3`.
- **Additive focused patch:** `source.patch`, SHA-256
  **`a097122a13ba002f9126de2e00bbf8d7bd2d73fc2ae4a9d9d890d873083b1206`**.

Worktree: `/home/rupicola/Code/y/yubikit-core-docs-edit-transports`. The primary checkout was read only
for this one-file accepted-byte comparison. This patch applies on top of the accepted transport
source; it does not replace or regenerate any prior full or sampling patch.

## Verified rationale and correction

The interface now describes interceptable native-lifetime and OTP feature-report operations, rather
than a one-for-one native-call mirror with no policy. Its production-class summary likewise describes
native call forwarding with small managed composition and status handling.

The complete implementation makes those distinctions concrete:

- **CreateDevice, before lines 131–152:** combines registry-entry matching, service lookup and HID
  creation; checks missing service/device results; throws PlatformApiException; and releases the
  temporary service reference in finally.
- **OpenDevice and close operations, before lines 154–173:** OpenDevice translates a nonzero result
  from OpenDeviceResult into PlatformApiException. CloseDeviceChecked exposes success as a Boolean.
  These are managed status-handling choices, not merely identical signatures forwarded unchanged.
- **Remaining operations, before lines 175–197:** feature-report and callback operations forward native
  calls, while run-loop mode creation and integer-property lookup use managed helpers. Those native
  boundaries remain visible and interceptable.

The interface overview also assigns accepted-operation scheduling/drain and physical-claim release
ordering to the connection owners. Injected implementations allow tests to inspect that ordering;
the seam alone provides neither drain nor native-release proof. The separate FIDO report bridge and
the useful no-hardware acquisition-failure/report/cleanup testability rationale are preserved.

Narrow owner context confirms the assignment: MacOSOtpHidConnection's worker invokes admitted work
before Close; ShutdownAsync closes admission and returns the shared close task; Close checks native
close, releases its CF object and then releases the registration. Unproven release retains the rooted
owner/claim. This is source-level ordering evidence, not fresh execution or a portable release rule.

## Original unit mapping and records

- Finding: **SAMPLE-B3-001**.
- Actual selected unit: **`core-001:unit:f8412d69f0903d78`**, original source-context/interface overview,
  lines 1–43.
- Related production type anchor: `core-001:unit:573ce86308ec45e3`, original lines 120–121.
  The production overview is corrected under the same finding, not allocated as another sample.

`inputs.json` preserves the full before-file, accepted identities, original unit records and prior
record hashes. `file-disposition.json` records the one-file overview-only edit and unit mapping.
`source-evidence.json` identifies the complete-file and narrowly read owner/bridge evidence with
explicit source-versus-runtime limits. `static-preservation.json` records the fresh bounded checks.

All individual member documentation, native call boundaries, declarations, ABI, option flags, status
handling, resource ownership implementation and retained-state rationale remain unchanged. No
examples are changed. Existing native/runtime/producer/hardware qualification gaps and hold decisions
are neither reopened as implementation work nor closed by this wording.

## Preservation and review handoff

The following existing patches and all preceding editor/review records remain byte-exact:

- Original full transport patch: `86b0c6ed78d5d1c3cf2cd68ec4b529ad8903e9b0ad3258dfa8b2bd7cb1656ef6`.
- Accepted revision-2 full transport patch: `109bd7833b71d69f9b25b2a021ea27e460334535fe28e8fcdc8cd6bd160404de`.
- Linux sampling patch: `a994f3081f0bc7439221d2a6073b5bc4f0dec9149d9dfa4c3e254799cbab1049`.

Review the two corrected complete overview blocks alongside CreateDevice, OpenDevice, the status/
helper forwarders and the cited owner sequencing. Bind the focused patch hash above. Static checks
cover comment-only changes, unchanged executable/member-documentation text, BOM/final-newline state,
XML well-formedness, patch applicability and immutable prior records. They do not establish cref
binding, compiler/analyzer acceptance, native drain or device behavior.

No SDK build/tests, formatter, native/device/SDK-crypto execution, Git mutation, subagent, new framework
or broader semantic audit was performed. Only this source file and new records under
`editing/transports/sampling-macos-seam/` were written.

The sampler owns XML-stratum expansion and edited-overlap exclusion; this editor assigns no sampling
credit or campaign coverage changes. Independent content acceptance remains pending. Finish Core
Pass 1 publication and the deferred finding handoff, then stop; no implementation or new native/device
campaign is authorized by this correction.
