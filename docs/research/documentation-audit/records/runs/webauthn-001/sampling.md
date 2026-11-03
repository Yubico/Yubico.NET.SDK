# Astra quality sampling

Baseline `e353b49a`, 2026-11-03. Semantic sampling units are the file/assigned external section rows already enumerated by the investigation manifests. The manifests contain 69 rows explicitly assessed clean (34 A, 12 B, 23 C); some share findings and are only locally clean. Eight selected units exceed the initial 10% minimum and span public XML, internal explanations, tests and external user prose. This is a process check, not statistical certification.

| Selected unit | Independent assessment |
| --- | --- |
| `src/WebAuthn/src/Extensions/Inputs/CredPropsInput.cs` | Clean; presence requests client-side credential properties, no extra carrier parameters |
| `src/WebAuthn/src/Extensions/Outputs/CredPropsOutput.cs` | Clean; nullable discoverable-property carrier. Processor supplies effective requireResidentKey; dated W3C L3 REC §10.1.3 independently fetched and matches |
| `src/WebAuthn/src/Extensions/Processing/RegistrationExtensionOutputsBuilder.cs` | Clean; Build copies all seven fields to typed record; mutable-sink explanation accurate |
| `src/WebAuthn/src/Extensions/Processing/AuthenticationExtensionOutputsBuilder.cs` | Clean; Build copies all four fields; no unsupported public/plugin claims |
| `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnLargeBlobAuditReproTests.cs` | Clean scoped key-location test: decoder gets top-level 0x05 and processor reports it for effective discoverable request. CTAP2.3 PS response table independently fetched, confirms key. Synthetic response is not device/conformance proof |
| `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Extensions/PreviewSign/PreviewSignProcessorTests.cs` | Clean selected-entry/no-match/map-shape explanations; source processor compared and vendor draft v4 selected-credential text independently fetched. Internal comparer is valid within test assembly, unlike the public example finding |
| `docs/usage/user-interaction.md`, WebAuthn/intent examples and surrounding contract | **Required miss WA-S-001:** line 98 passes cancellationToken as second positional argument to WebAuthn GetAssertionAsync; second argument is nullable PIN bytes. Must use named `cancellationToken: cancellationToken`. Other sampled distinction between presence evidence and authorization remains useful |
| `docs/architecture/physical-device-model.md`, WebAuthn transport-selection row and surrounding override/SCP text | Clean assigned WebAuthn scope; explicit SCP-before-default qualification present; matches factory forwarding evidence. Adjacent other-module examples/platform claims not recertified |

## Material-miss response

WA-S-001 expands review to **all external WebAuthn consumer snippets**, including code blocks with implicit `client` variable names and no WebAuthn keyword on the call line. Fresh Sol reviewer dispatched as `ses_e5e683301ffe5WFjxqQ1sQt8UE`; output is `sample-expansion.md`. This avoids relying only on keyword-selected passages for example coverage. Editor C must incorporate required findings before independent final review. Compile the corrected shared snippet along with the guide's example shape.

Expansion completed: confirmed the guide mismatch and found its identical companion XML example in
`src/Core/src/Credentials/UserPresenceIntent.cs`. Both corrections are explicitly assigned to editor C.
All 25 external files plus four migration files screened; no further consumer-snippet mismatch established.
The newly discovered companion raises the supplemental external coverage count to 30.

## Consequential checks

Astra separately inspected current credProtect processor, PRF decoder, Aaguid default/equality, authentication eager outputs, and retry/mapping boundaries; primary PRF and credProtect clauses were independently fetched. See triage for dispositions. More final-diff checks follow editing/review. No hardware or test execution is claimed here.

After B review, Astra read the complete edited public XML in `PrfInput`, `PrfOutput`, `CredProtectInput`,
`LargeBlobInput`, and `PreviewSignSigningParams`. Required UV is clearly a workaround for missing
enforcement, cleanup distinguishes read-only SDK-created originals from caller buffers, credential
protection does not promise prevention, blob cardinality/support limitations remain local, and signing
arguments distinguish draft requirements from local constructor restrictions. No additional correction
found in those five consequential changed units. B-R1 was separately verified against the two-credential
test's actual key-selection expression and assigned to editor C.
