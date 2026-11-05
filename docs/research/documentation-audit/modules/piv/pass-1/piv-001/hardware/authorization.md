# PIV device authorization and initial observation

User authorizes dedicated-key integration tests, resets, PIN/PUK/management-key changes, configuration
and bounded ad hoc/vendor probes. Physical touch/hotplug timing remains coordinated. The final state
must be independently read back, with existing attestation material preserved and reset exceptions
handled explicitly. No PIV mutation has run in this audit yet.

Initial CLI discovery reports two attached serials: **103 and125**. Primary audit target is103; bind
mutation tests explicitly through the canonical harness configuration or a reviewed audit filter.
First-match discovery alone is insufficient to identify the tested device. Before any multi-key case,
record the other device's state and the case's effects separately.

Serial103 read-only CLI identity: YubiKey5 NFC Enhanced PIN, USB-A, metadata5.8.0.beta.0, OTP/FIDO/CCID
enabled. PIV CLI version5.8.0, PIN3/3, PUK3/3, **AES192** management key, default PIN/PUK/management-key
flags, CHUID andCCC absent. Fuller slot/certificate/attestation baseline is being prepared.

An initial preparer packet incorrectly suggested AES256 as a possible recent-firmware default.
The actual CLI metadata says AES192; coordinator immediately corrected that hypothesis. Cleanup must
derive its default-key algorithm from source-checked device metadata rather than that planning guess.
Context7 was consulted for vendor PIV metadata/serial-selection API orientation; original installed
source and recorded actual device observations govern the helper's behavior.

Narrow shared-harness reads establish first-match placeholder binding and allow-list configuration;
this is execution setup, separate from a Core/cross-module audit. Existing historical claims about
serial125 or prior PR745 test/reset success do not substitute for this run's measured baseline.

## Full serial103 baseline

`baseline-103.json` captured successfully through reader `Yubico YubiKey OTP+FIDO+CCID 00 00`, with
live Management and PIV serial guards. All25 slots were read:24 normal slots returned6A88 for metadata
and6A82 for certificates. F9 reports RSA2048, PINOnce, touchNever, origin imported-or-other. Its793-byte
certificate DER SHA256 is`fb2e0abe368a14f434bdf76d398bb62daca62cfa8478cc6c19fc2b48e4aa446a`; SPKI
SHA256`4937e8476bc5ce737220992930cde1a74fe7c031d04032338c3da520e0b94ad4` matches slot public metadata.
Existing F9 private-key possession is not established by that read; preserve it throughout tests.

Raw PIV GET VERSION returned000001; direct Python version0.0.1 is retained separately from CLI5.8.0
and Management metadata5.8.0.beta.0. Source-checked public metadata/serial commands supplied readback
after the direct client's development-version gate. No global/private version override was used.
PIN/PUK metadata confirms default3/3; management raw algorithm0A identifiesAES192 with default/Never.
CHUID,CCC,KeyHistory andAdmin objects are absent. Bio API is unsupported in this observed context;
this key supplies no Bio ceremony qualification. Protected management-key data was excluded.

The harness allowlist is **tracked**, despite an earlier ignored-file assumption. That assumption
failed before any write. Its exact original bytes/hash are now preserved in test-allowlist-original
records; no configuration edit has been made yet. Any temporary serial narrowing must be restored
before final source/API/publication checks.

## Secondary serial125 read-only baseline

To prepare existing multi-key cases without assuming another device's state, coordinator mapped reader
`Yubico YubiKey OTP+FIDO+CCID 01 00` to125 and captured `baseline-125.json` successfully. Metadata is
5.8.0.alpha.2 Enhanced PIN USB-A; raw PIV000001. Its24 normal slots are empty, PIN/PUKdefault3/3,
managementAES192default/Never, andCHUID/CCC/KeyHistory/Admin objects absent. F9 is RSA2048/Once/Never;
its793-byte certificate DER SHA256 is`84d4b3407b5ca5743ae7f0ea46a58ccb493db357f877483e5c288790fe34fd1a`
and matching metadata/certificate SPKI hash is`5fbb057994305ab5e98f70b6c17a582fee5ccff580aeb8c9d821cf52ff3d4c27`.

No mutation has run on either device. Primary single-key tests remain scoped to103; any later multi-key
case receives separate effect/readback accounting for125. Both keys are development5.8 hardware, so
their availability supplies no older-commercial-firmware qualification.
