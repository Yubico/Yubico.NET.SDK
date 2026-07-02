# Work Plan: TODO Backlog (YESDK-1559 to YESDK-1577)

**Created:** 2026-04-15
**Source:** Codebase-wide TODO scan + Jira issue creation
**Status:** Backlog — to be prioritized and worked in future sessions

---

## Priority 1: Correctness & Safety

| Jira | Summary | Module | File | Effort |
|------|---------|--------|------|--------|
| YESDK-1561 | Add try-catch to multi-page TLV retrieval | Management | ManagementSession.cs:112 | Small |
| YESDK-1563 | Verify ECPublicKey.CreateFromSubjectPublicKeyInfo | Core | ECPublicKey.cs:173 | Small (write test) |
| YESDK-1571 | Migrate GetBioMetadataAsync to TLV parsing | Piv | PivSession.Bio.cs:60 | Medium (needs Bio HW) |
| YESDK-1569 | Disambiguate PivSession.IsAuthenticated | Piv | PivSession.Authentication.cs:62 | Medium |
| YESDK-1574 | SCARD_W_RESET_CARD resilience | Core | SmartCard connections | Large |

## Priority 2: Tech Debt Cleanup

| Jira | Summary | Module | File | Effort |
|------|---------|--------|------|--------|
| YESDK-1559 | Incomplete TODO in DeviceInfo.cs | Management | DeviceInfo.cs:196 | Tiny |
| YESDK-1560 | Validate timeout max values | Management | DeviceConfig.cs:138,147 | Small |
| YESDK-1562 | ECPrivateKey: evaluate ECDH wrapping TODO | Core | ECPrivateKey.cs:27 | Small (may be stale) |
| YESDK-1573 | Make CapabilityMapper internal | Core | CapabilityMapper.cs | Small |
| YESDK-1570 | Check bio not configured before reset | Piv | PivSession.cs:255 | Medium |

## Priority 3: Architecture & Performance

| Jira | Summary | Module | File | Effort |
|------|---------|--------|------|--------|
| YESDK-1564 | FirmwareVersion on IApduProcessor interface | Core | ScpInitializer.cs, IApduProcessor.cs | Medium |
| YESDK-1565 | ChainedApduTransmitter composition refactor | Core | ChainedApduTransmitter.cs:18 | Medium |
| YESDK-1568 | Avoid allocation in ApduFormatterShort.Format | Core | ApduFormatterShort.cs:55 | Small |
| YESDK-1566 | Determine actual transport type | Core | UsbSmartCardConnection.cs:289 | Medium |
| YESDK-1567 | Extended APDU support per device | Core | UsbSmartCardConnection.cs:292 | Medium (see YESDK-1499) |

## Priority 4: Platform & Testing

| Jira | Summary | Module | File | Effort |
|------|---------|--------|------|--------|
| YESDK-1575 | HID: Windows platform support | Core | PlatformInterop/Windows | Large |
| YESDK-1576 | HID: Linux platform support | Core | PlatformInterop/Linux | Large |
| YESDK-1577 | Ed25519 signature verification tests | Piv | Integration tests | Medium |
| YESDK-1572 | Upgrade CodeAnalysis analyzers to 10.0.102 | Build | NuGet packages | Medium (189 errors) |

---

## Notes

- YESDK-1567 relates to existing YESDK-1499
- YESDK-1571 requires physical YubiKey Bio device for verification
- YESDK-1562 may be stale — `ToECDiffieHellman()` already exists
- YESDK-1572 is high-value but requires fixing 189 analyzer violations
- HID platform support (YESDK-1575/1576) are large feature work, not quick fixes
