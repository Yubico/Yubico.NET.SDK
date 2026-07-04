# GitHub Fork V2 Readiness Audit

Date: 2026-07-04

Branch context: `yubikit`

Purpose: identify unreported user expectations for YubiKit V2 by reviewing recent forks of `Yubico/Yubico.NET.SDK` and looking for meaningful custom changes.

## Summary

The GitHub fork signal is sparse but useful. Most forks are mirrors with no custom changes, but the few meaningful forks cluster around the same product expectations: partner or YubiKey-compatible device support, less narrow device identity modeling, low-level transport escape hatches, FIDO-over-smart-card behavior, and target framework adoption risk.

## Method

- Queried GitHub for recent forks of `Yubico/Yubico.NET.SDK`.
- Reviewed all 64 forks visible from GitHub at investigation time.
- Filtered out forks with no custom commits or only stale mirror state.
- Focused on forks that were ahead of or meaningfully diverged from upstream.
- Cross-checked findings against the V2 `yubikit` branch where relevant.

## Meaningful Forks

Most forks had no useful signal. The forks that appeared to contain custom or divergent work were:

- `Logos-Parthenos-AI/Yubico.NET.SDK`
- `janlii/Yubico.NET.SDK`
- `gvigroux/Yubico.NET.SDK`
- `markeytos/Yubico.NET.SDK`
- `darrenjrobinson/Yubico.NET.SDK`
- `obliduty/Yubico.NET.SDK`

Strongest signal: `gvigroux/Yubico.NET.SDK`.

## Findings

### 1. Partner And Compatible Device Support

The strongest fork signal is support for Thales/eToken-style devices and other YubiKey-compatible or partner authenticators. The fork appears to add or adjust:

- Vendor/product detection.
- Device naming and recognition.
- Serial handling.
- Capability assumptions.
- FIDO/CTAP details needed for non-standard integration.

V2 readiness question: is YubiKit V2 intentionally YubiKey-only, or should it support partner/compatible devices through an explicit extension model?

Recommendation: decide this product boundary explicitly. If partner devices are in scope, avoid hard-coding assumptions that only hold for YubiKey hardware. If out of scope, document that boundary so users do not infer unsupported compatibility.

### 2. Serial Number Shape May Be Too Narrow

One fork changed serial numbers from `int?` to `string?`. This likely reflects devices whose identifiers do not fit YubiKey's numeric serial model.

V2 readiness question: should `DeviceInfo.SerialNumber` remain numeric, or should V2 model device identity separately from YubiKey serial identity?

Recommendation: re-evaluate whether `int?` is the right public API shape for V2. A possible split is:

- YubiKey serial number: numeric, when the device is a YubiKey and exposes one.
- Device identifier: string or structured value, when broader device identity is needed.

### 3. Low-Level Transport Escape Hatches Are Desired

At least one fork exposed direct smart-card device access by making `GetSmartCardDevice()` public. That indicates some users want to bypass high-level session APIs and work directly with transports.

V2 appears to solve this more cleanly through connection-based APIs such as `IYubiKey.ConnectAsync<ISmartCardConnection>()` rather than exposing older internal device handles.

Recommendation: document the V2 low-level transport escape hatches clearly, with examples for smart-card/APDU and FIDO/HID use cases. If the intended escape hatch is connection-based, make that discoverable in docs and migration guidance.

### 4. FIDO Over Smart Card/NFC Pain Is Real

Fork changes around CTAP/FIDO behavior suggest that FIDO over smart card or NFC has caused real integration pain.

V2 already addresses much of this structurally:

- HID FIDO uses the HID/CTAPHID path.
- Smart-card FIDO wraps CTAP CBOR in APDUs.
- `SmartCardBackend` uses the smart-card protocol path and sends CTAP via `CLA=0x80`, `INS=0x10`.

Recommendation: keep this as a V2 teaching point. The design should make it clear that smart-card FIDO is not a duplicate stack; it reuses the APDU pipeline where appropriate, while HID remains the separate CTAPHID route.

### 5. Target Framework May Be An Adoption Risk

One fork moved to `.NET 6`. V2 currently targets `net10.0`.

This may be intentional for the V2 rewrite, but it is a real adoption risk for users integrating into conservative enterprise environments.

Recommendation: make the target framework policy explicit. If `net10.0` only is intentional, document why. If adoption friction matters more than platform feature usage, consider whether multi-targeting or a lower LTS target is worth evaluating.

### 6. Devcontainer And Build Ergonomics Are Weaker But Real Signal

Some fork changes relate to local development setup, containerization, or build ergonomics.

Recommendation: lower priority than API shape issues, but worth tracking. A clean contributor/devcontainer story may reduce fork-only fixes and make community debugging easier.

## V2 Readiness Checklist

- Decide whether V2 supports only YubiKey devices or also YubiKey-compatible/partner devices.
- Re-evaluate `DeviceInfo.SerialNumber int?` against non-YubiKey-compatible devices and enterprise inventory use cases.
- Make low-level transport access obvious in docs and examples.
- Document the FIDO transport split: HID CTAPHID versus smart-card APDU-wrapped CTAP.
- Revisit the `net10.0`-only target decision before broad V2 adoption messaging.
- Consider a small contributor ergonomics pass after the API boundary questions are settled.

## Recommended Next Step

Turn this into a short V2 product/API review focused on three decisions:

1. Device scope: YubiKey-only or compatible/partner-device extensible.
2. Device identity model: numeric YubiKey serial versus broader string/structured identifiers.
3. Adoption target: `net10.0` only versus a broader target framework policy.
