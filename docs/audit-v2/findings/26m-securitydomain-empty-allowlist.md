# 26m Security Domain empty allowlist removes the restriction (YESDK-1634)

| | |
| --- | --- |
| Audit severity | MED |
| Our verdict | Confirmed, with a correction (the device semantics, and the section number in the specification) |
| Our severity | HIGH for callers who treat an empty list as deny-all. Removing the restriction is silent. |
| Root cause | SDK and Python share it (yubikey-manager and the Android SDK have no guard either) |
| Fix group | A (no-brainer: reject an empty list before any I/O; keep an explicit clear operation) |
| Evidence | unit test (recorded command); GlobalPlatform specification |
| Since the audit | `SecurityDomainSession.cs` and `ISecurityDomainSession.cs` are unchanged at current yubikit. |

## What the audit says

The SDK accepts an empty list in `StoreAllowListAsync`, and `ClearAllowListAsync` relies on that. An application that maps "no permitted certificates" to an empty list removes the serial-number restriction instead of denying everything.

> "An application translating 'no permitted certificates' into an empty list therefore removes the serial-number restriction rather than enforcing deny-all."

Abbreviations: SD is the Security Domain, the GlobalPlatform application that holds keys and trust anchors. SCP11 is Secure Channel Protocol 11, which authenticates the host with certificates. OCE is the off-card entity, the host. CA-KLOC is the GlobalPlatform certificate authority key that signs the OCE certificates. GlobalPlatform is the card specification body. TLV is tag-length-value. SDK is software development kit.

## What is right

- **An empty list is sent as an empty allowlist.** `StoreAllowListAsync` builds one serial TLV per entry, so an empty collection writes nothing, and then wraps the (empty) list in tag `0x70` ([SecurityDomainSession.cs:765-774](../../../src/SecurityDomain/src/SecurityDomainSession.cs#L765-L774)). It is then sent with `StoreDataAsync` ([SecurityDomainSession.cs:776](../../../src/SecurityDomain/src/SecurityDomainSession.cs#L776)). There is no check of the input before I/O.
- **Clearing uses the same bytes, and the specification says that is how to clear.** `ClearAllowListAsync` calls `StoreAllowListAsync` with an empty list ([SecurityDomainSession.cs:787-788](../../../src/SecurityDomain/src/SecurityDomainSession.cs#L787-L788)). Section 7.9 of the specification says a zero-length allowlist TLV removes the allowlist (see Specification). The wire bytes for clearing are right.
- **The remark about "no allowlist" is accurate.** The method says: "If an allowlist is not stored, any certificate signed by the CA can be used." ([SecurityDomainSession.cs:745-748](../../../src/SecurityDomain/src/SecurityDomainSession.cs#L745-L748)). That matches the specification.
- **The unit test reproduces the problem.** [YESDK1634_EmptyAllowListDoesNotIssueRestrictionRemovingStoreData](../../../src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/AuditV2/SecurityDomainAuditReproTests.cs#L8-L21) expects `ArgumentException` for an empty list and no STORE DATA. **Result today (2026-10-10): fails, "No exception was thrown".**

## What is wrong or imprecise

- **No device setting means deny-all through an allowlist.** The specification requires that an allowlist, once created, always contains at least one certificate. It also says that once an allowlist is removed, all certificates are accepted (section 4.3). An empty list is therefore not a deny-all. The verification report reaches the same conclusion.
- **The section number is different.** The verification report cites section 3.3 of Amendment F, SCP11 v1.3. In the public-review copy (v1.3.0.13, August 2023) the authentication rule is in section 4.3, page 25. The removal rule is in section 7.9, page 65. The final v1.3 text was not checked.
- **The audit's "deny-all" is not expressible.** The audit's example application expects an empty list to mean deny-all. Nothing on the device does that. The verification report suggests removing or disabling the CA trust key instead. The specification text checked here does not describe another device-level deny-all. Whether this SDK can remove the CA-KLOC key was not checked.
- **Adjacent, not in the audit, not tested.** Section 7.9 requires a tag `92` (allowlist counter) in SCP11c sessions and forbids it otherwise. `StoreAllowListAsync` never sends tag `92` ([SecurityDomainSession.cs:771-776](../../../src/SecurityDomain/src/SecurityDomainSession.cs#L771-L776)), and the SDK supports SCP11c ([Scp11KeyParameters.cs:21](../../../src/Core/src/Protocols/SmartCard/Scp/Scp11KeyParameters.cs#L21)). Whether an SCP11c allowlist update fails today needs a device test. Track it separately.

## Why it matters

- **Who triggers it.** An application that uses the allowlist as its revocation mechanism. Section 4.3 recommends that: "It is recommended to use the allowlist also as a revocation mechanism for OCE certificates." An app that revokes everything by storing an empty list silently re-opens access to every certificate signed by the CA.
- **Impact.** Any certificate signed by the CA can authenticate as an OCE, for SCP11a and SCP11c sessions. The CA-signed chain is still checked, so the attacker needs a certificate from the CA. The serial-number restriction is what disappears.
- **Silent.** The fake accepts the command. Under the specification, a successfully authorized and accepted zero-length allowlist removes the restriction. Hardware acceptance was not tested; SCP11c requires a counter that this implementation omits.
- **No secret exposure.** The problem is the removed restriction.
- **Preconditions.** An application that clears or empties the allowlist by mistake or by design, with the OCE authenticated to the SD.

## Specification

> "If an allowlist with one or more Certificate Serial Number entries exists in the SD for the CA-KLOC’s public key, the SD also verifies that the certificate is contained in an allowlist. Else the SD accepts all certificates signed by the CA-KLOC." (GlobalPlatform Card Specification v2.3 Amendment F, SCP11, v1.3.0.13 public review, section 4.3, "Authentication", page 25)

> "...special care shall be taken never to empty/remove the allowlist (i.e. if created, the allowlist shall always contain at least one certificate) because no restrictions apply (i.e. all certificates are accepted) once an allowlist is removed." (same document, section 4.3)

> "To remove an allowlist, the allowlist TLV of the command shall have a length of zero." (same document, section 7.9, "STORE DATA (Allowlist) Command", page 65)

Source: [GlobalPlatform SCP11 Amendment F, public review v1.3.0.13](https://globalplatform.org/wp-content/uploads/2023/08/GPC_2.3_F_SCP11_v1.3.0.13_PublicRvw.pdf). The text was checked in this copy only.

| Clause | Amendment F v1.3.0.13 (public review) | Notes |
| --- | --- | --- |
| Authentication rule | 4.3 (page 25) | With an allowlist, only listed serials pass. Without one, any CA-signed certificate passes. |
| Revocation advice | 4.3 (page 25) | The allowlist is recommended as the revocation mechanism, and must never be emptied. |
| STORE DATA (Allowlist) | 7.9 (page 65) | Replaces the allowlist. A zero-length `70` TLV removes it. Tag `92` is required in SCP11c sessions. |

## Canonical Python reference

- **Python has the same issue.** `store_allowlist` builds `Tlv(0x70, b"".join(...))` with no guard ([yubikit/securitydomain.py:251-263](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/securitydomain.py#L251-L263)). An empty `serials` sequence sends a zero-length `70` TLV. This is a candidate for the divergence ledger.
- **The ykman CLI has the same footgun.** The `set-allowlist` command takes serials as `nargs=-1`. With no serials it clears the allowlist and then prints "SCP serial number allowlist set for ..." ([ykman/_cli/securitydomain.py:428-447](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/ykman/_cli/securitydomain.py#L428-L447)). The message misstates what happened.
- Deliberate? The docstring documents the semantics ("If no allowlist is stored, any certificate signed by the CA can be used."), but nothing guards the empty case. The function dates from 2024 according to `git blame`.

## Sibling SDKs (context only)

- The Android SDK's `storeAllowlist` also has no guard for an empty list ([SecurityDomainSession.java:256-268](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/core/src/main/java/com/yubico/yubikit/core/smartcard/scp/SecurityDomainSession.java#L256-L268)). It has no separate clear operation.
- The Swift SDK's security domain code was not found to implement an allowlist in the places searched.

## Reproduction

- Unit: [YESDK1634_EmptyAllowListDoesNotIssueRestrictionRemovingStoreData](../../../src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/AuditV2/SecurityDomainAuditReproTests.cs#L8-L21). Asserts `ArgumentException` for `StoreAllowListAsync(..., [])` and no new commands. **Result today: fails, "No exception was thrown".**
- Hardware: not run. The fake cannot show what a real card does with the empty list; the specification says it removes the allowlist.

## Proposed fix

- **Recommendation.**
  1. In `StoreAllowListAsync`, reject an empty collection with `ArgumentException` before any I/O ([SecurityDomainSession.cs:757-760](../../../src/SecurityDomain/src/SecurityDomainSession.cs#L757-L760)).
  2. Keep `ClearAllowListAsync` as its own method, with its own explicit wire implementation: a zero-length `70` TLV, built directly rather than through `StoreAllowListAsync` ([SecurityDomainSession.cs:787-788](../../../src/SecurityDomain/src/SecurityDomainSession.cs#L787-L788)). This keeps the clear operation intentional.
  3. Correct the remark at [SecurityDomainSession.cs:745-748](../../../src/SecurityDomain/src/SecurityDomainSession.cs#L745-L748) to say that clearing is done only with `ClearAllowListAsync`, and to point to the deny-all guidance.
  4. Update the two SD README passages that describe these methods ([src/SecurityDomain/README.md:112](../../../src/SecurityDomain/README.md#L112) and [line 140](../../../src/SecurityDomain/README.md#L140)).
- **Options considered.**
  - Option 1 (recommended): reject empty input in `StoreAllowListAsync`, keep `ClearAllowListAsync`. Breaking for every caller that passes an empty list, including intentional clearing. Such callers must migrate to `ClearAllowListAsync`.
  - Option 2: treat an empty list as "clear". Matches the device but keeps the trap: a caller who means deny-all gets a silent clear. Not recommended.
  - Option 3: add a separate deny-all operation that disables the CA trust. Useful, but a larger change, and it needs its own spec review.
- **API impact.** Empty input now throws. `ClearAllowListAsync` keeps its signature.
- **Proving test.** The existing test (rejection, no STORE DATA). Add a test that `ClearAllowListAsync` sends exactly one STORE DATA with a zero-length `70` TLV.
- **Depends on / interacts with.** The tag `92` question for SCP11c is separate. The deny-all policy question needs a decision before any deny-all API is added.
- **Open questions for the maintainer.** Is a deny-all operation wanted, and should it remove the CA key or the CA's trust? Should the tag `92` gap be filed as its own finding?

## Check it yourself

From the worktree root:

```bash
dotnet toolchain.cs -- test --project SecurityDomain --filter "FullyQualifiedName~YESDK1634_EmptyAllowList"
```

Read in this order:

- [SecurityDomainSession.cs:741-788](../../../src/SecurityDomain/src/SecurityDomainSession.cs#L741-L788) (the two methods and their remarks).
- Amendment F v1.3.0.13, section 4.3 (page 25) and section 7.9 (page 65). The public-review PDF is linked above.
- `store_allowlist` in `yubikit/securitydomain.py` (lines 251-263) and the `set-allowlist` command in `ykman/_cli/securitydomain.py` (lines 428-447) in the Python reference.
