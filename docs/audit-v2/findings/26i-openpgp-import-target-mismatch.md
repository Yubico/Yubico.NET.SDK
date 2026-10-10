# 26i OpenPGP import writes to the template slot, not the requested slot (YESDK-1634)

| | |
| --- | --- |
| Audit severity | MED |
| Our verdict | Confirmed |
| Our severity | MED |
| Root cause | SDK only (the API takes the slot twice: once as `keyRef`, once inside the template) |
| Fix group | B (small API decision: reject a mismatch, or derive the slot from `keyRef`) |
| Evidence | unit test (recorded APDU) |
| Since the audit | `IOpenPgpSession.cs`, `OpenPgpSession.Keys.cs` `PutKeyAsync` (lines 48-93) and `PrivateKeyTemplate.cs` are unchanged. `OpenPgpSession.Keys.cs` changed only in `AttestKeyAsync` (user-presence plumbing), which is not on this path. |

## What the audit says

`PutKeyAsync` takes a `keyRef` that is documented as the target slot. The private key is imported using a template that carries its own slot. If the two differ, the attributes go to one slot and the key to the other.

> "Passing SIG with an AUT template therefore routes attributes to SIG and the key import to AUT, potentially modifying two slots instead of the requested one."

Abbreviations: OpenPGP is the card application and message format. SIG, DEC and AUT are the signature, decryption and authentication key slots. CRT is control reference template, the tag that names a key slot in a command. APDU is application protocol data unit. SDK is software development kit.

## What is right

- **The API names the slot in two places.** The `keyRef` parameter is documented as "The target key slot" ([IOpenPgpSession.cs:215](../../../src/OpenPgp/src/IOpenPgpSession.cs#L215)). The template also states that its CRT identifies the target slot ([PrivateKeyTemplate.cs:35-37](../../../src/OpenPgp/src/PrivateKeyTemplate.cs#L35-L37)), and the template sets that CRT in its constructor from the slot it is given ([PrivateKeyTemplate.cs:43-46](../../../src/OpenPgp/src/PrivateKeyTemplate.cs#L43-L46)).
- **The import uses the template's slot.** `PutKeyAsync` serializes the template as is ([OpenPgpSession.Keys.cs:68](../../../src/OpenPgp/src/OpenPgpSession.Keys.cs#L68)). The CRT is copied from the template's `CrtBytes` ([PrivateKeyTemplate.cs:101-105](../../../src/OpenPgp/src/PrivateKeyTemplate.cs#L101-L105)), and the command is a PUT DATA odd (INS `0xDB`) at [OpenPgpSession.Keys.cs:70-77](../../../src/OpenPgp/src/OpenPgpSession.Keys.cs#L70-L77).
- **The attributes use `keyRef`.** When attributes are passed, `SetAlgorithmAttributesAsync(keyRef, ...)` runs first ([OpenPgpSession.Keys.cs:59-63](../../../src/OpenPgp/src/OpenPgpSession.Keys.cs#L59-L63)). That is the slot the caller asked for.
- **The unit test reproduces the mismatch.** [YESDK1634_ImportDoesNotTargetTemplateSlotInsteadOfRequestedSlot](../../../src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/AuditV2/OpenPgpAuditReproTests.cs#L9-L28) passes `KeyRef.Sig` with an `Aut` template and asserts `B6 00`. It fails today with `[A4, 00]` in the recorded import command.

## What is wrong or imprecise

- **Two slots are touched only when attributes are passed.** Without attributes, the import alone goes to the template's slot, which is AUT in the audit's example. With attributes, the SIG algorithm attributes are written as well. The audit's "potentially modifying two slots" is accurate for that case only.
- **The attribute write can delete the SIG key.** The card specification says that when the algorithm attributes of an existing key change and no longer match the stored key, "the card should delete this key internally or prevent it from usage" (section 4.4.3.9). The SDK's own `DeleteKeyAsync` relies on this: it deletes a key by changing its attributes twice ([OpenPgpSession.Keys.cs:104-115](../../../src/OpenPgp/src/OpenPgpSession.Keys.cs#L104-L115)). So the SIG write in the example can remove an existing SIG key. The card decides; the spec says "should", not "must".
- **Only the template is validated, nothing is.** Neither `PutKeyAsync` nor the template constructor compares the two references. The check belongs in `PutKeyAsync`, before any command is sent.

## Why it matters

- **Who triggers it.** An application developer, through a copy-paste or refactoring error, or a UI that lets a user pick a slot separately from the key. No remote attacker is involved.
- **Impact.** The key lands in a different slot from the one the caller named, and that slot is overwritten. AUT is the slot OpenPGP uses for SSH authentication, so replacing it can break logins. With attributes, an existing SIG key can also be deleted. The caller gets no error.
- **Preconditions.** A template whose slot differs from `keyRef`. The problem exists with or without attributes.

## Specification

> "The DOs in the Extended Header list start with a Control Reference Template (CRT) of the referenced key in short or extended format with Key-Ref." (OpenPGP card application 3.4.1, section 4.4.3.12)

Source: [OpenPGP card application 3.4.1](https://gnupg.org/ftp/specs/OpenPGP-smart-card-application-3.4.pdf), section 4.4.3.12 ("Private Key Template"). The table in the same section gives the CRTs: "Digital signature: B6 00 or B6 03 84 01 01", "Confidentiality: B8 00 or B8 03 84 01 02", "Authentication: A4 00 or A4 03 84 01 03".

> "If the attributes of an existing key are changed and do no longer match with the stored key, the card should delete this key internally or prevent it from usage." (OpenPGP card application 3.4.1, section 4.4.3.9)

The spec defines the import through one referenced key: the CRT names it, and the attributes describe that same key. The SDK sends attributes for one key and the import for another.

| Clause | OpenPGP card 3.4.1 | Notes |
| --- | --- | --- |
| Private key template | 4.4.3.12 | CRT of "the referenced key" |
| Algorithm attributes | 4.4.3.9 | Attribute change may delete the stored key ("should") |

## Canonical Python reference

- **Python is correct here.** `put_key` takes one `key_ref`, and the attributes and the template both use it ([yubikit/openpgp.py:1581-1605](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/openpgp.py#L1581-L1605)). The template's CRT comes from that same `key_ref` ([openpgp.py:900-929](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/openpgp.py#L900-L929)). A mismatch cannot be expressed.

## Sibling SDKs (context only)

- Android also takes one key reference: `putKey(KeyRef keyRef, PrivateKeyValues privateKey)` ([OpenPgpSession.java:1107](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/openpgp/src/main/java/com/yubico/yubikit/openpgp/OpenPgpSession.java#L1107)). It builds the slot from the key reference, so the mismatch cannot happen.

## Reproduction

- Unit: [YESDK1634_ImportDoesNotTargetTemplateSlotInsteadOfRequestedSlot](../../../src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/AuditV2/OpenPgpAuditReproTests.cs#L9-L28). Asserts that the import CRT is `B6 00`, the signature slot. **Result today (2026-10-10): fails, "Collections differ", expected `[182, 0]`, actual `[164, 0]`.**
- Hardware: not applicable. The recorded APDU is enough to show the slot. A hardware run would change keys on the test card.

## Proposed fix

- **Recommendation.** At the top of `PutKeyAsync` ([OpenPgpSession.Keys.cs:48-93](../../../src/OpenPgp/src/OpenPgpSession.Keys.cs#L48-L93)), compare the template's `CrtBytes` with `keyRef.GetCrt()` and throw `ArgumentException` if they differ. Do this before `SetAlgorithmAttributesAsync`, so that a rejected call sends no commands. The CRT is public data, so an ordinary comparison is fine.
- **Options considered.**
  - Option 1 (recommended): reject a mismatch before any command is sent. Consistent callers see no change. Mismatched callers get an exception instead of a silent write to another slot.
  - Option 2: ignore the template's CRT and use `keyRef`. Silent. A caller who built an AUT template and passed SIG would get the key in SIG with no warning. Not recommended.
  - Option 3: remove the slot from the template, so `keyRef` is the only input. Removes the ambiguity completely. Breaking for `RsaKeyTemplate` and the other template types. Later removal of the template's slot would be breaking; doing it before a release would not be.
- **API impact.** Option 1 adds an exception for mismatched input. No signature change.
- **Proving test.** Replace the current assertion with: passing `KeyRef.Sig` with an `Aut` template throws `ArgumentException`, and no APDU is recorded. Add a positive control: matching references still send the attributes and the import.
- **Depends on / interacts with.** Independent of the other OpenPGP findings. The attestation change in `OpenPgpSession.Keys.cs` is in the same file.
- **Open questions for the maintainer.** Option 1 now, with Option 3 later? Or Option 3 now, if the template API has not shipped?

## Check it yourself

From the worktree root:

```bash
dotnet toolchain.cs -- test --project OpenPgp --filter "FullyQualifiedName~YESDK1634_ImportDoesNotTargetTemplateSlotInsteadOfRequestedSlot"
```

Read in this order:

- [OpenPgpSession.Keys.cs:48-93](../../../src/OpenPgp/src/OpenPgpSession.Keys.cs#L48-L93) (`PutKeyAsync`), in particular lines 61 and 68.
- [PrivateKeyTemplate.cs:43-46](../../../src/OpenPgp/src/PrivateKeyTemplate.cs#L43-L46) and [Crt.cs](../../../src/OpenPgp/src/Crt.cs) (the CRT bytes).
- Card specification 3.4.1, sections 4.4.3.9 and 4.4.3.12.
- `put_key` in `yubikit/openpgp.py` lines 1581-1605 in the Python reference.
