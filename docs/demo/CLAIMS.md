# Claims ledger

Every factual claim in the deck, mapped to the source that grounds it.

**Repository:** `Yubico.YubiKit.NET.SDK`, branch `yubikit`, `d04d59aae63981588f6dc047eb0d788b681a8b8d`

**Peer repositories:**

| Repo | Branch | SHA |
|---|---|---|
| `yubikey-manager` | `main` | `4ca60f706af930459138d8dc0f0f953480e1c7a4` |
| `yubikey-manager` | `experiment/rust` | `90940e9bb734e4daec04bdb17dae155ff3c85c57` |
| `yubikit-android` | `main` | `f46268563437ac52910001222a77741d229b9b99` |
| `yubikit-swift` | **`release/1.4.0`** | `c76ae973d6` |
| `python-fido2` | `main` | `5bc9d3a1c8c34a3c4ca408366e630b620db47faa` |

**Kinds:** `code` = verified against source. `doc` = rationale or recorded decision,
documentation is the authority. `peer` = peer SDK source. `measured` = measured on this
run, see slide 14 provenance. `quoted` = taken from a pull request, not re-measured.

---

## 00 — intro

| # | Claim | Kind | Anchor |
|---|---|---|---|
| ~~1.1~~ | ~~Async all the way down; no sync-over-async~~ | **WITHDRAWN** | **False against `src/`.** `YubiKeyManager.Shutdown()` is `GetAwaiter().GetResult()` (`src/Core/src/Devices/YubiKeyManager.cs:257`, public at `PublicAPI.Unshipped.txt:987`); `ISmartCardConnection.BeginTransaction` is sync and blocks (`:653`, `UsbSmartCardConnection.cs:102`). Superseded by R11. |
| 1.2 | Install only what you use; ten packages | doc | `docs/v2-highlights.md:48` |
| 1.3 | Native AOT, all ten libraries analyzer-checked and link-verified | doc | `docs/v2-highlights.md:65`; PR #578 body |
| 1.4 | v1 used a 500 ms polling timer | doc | `docs/architecture/event-driven-device-discovery.md` "BEFORE" |
| 1.5 | v2 breaks a lot on purpose | doc | `docs/v2-highlights.md:75` |

## 01 — pipeline

| # | Claim | Kind | Anchor |
|---|---|---|---|
| 2.1 | `YubiKeyManager.FindAllAsync()` returns `IReadOnlyList<IYubiKey>` | code | `src/Core/src/PublicAPI.Unshipped.txt:984-985` |
| 2.2 | `key.CreatePivSessionAsync()` exists and is an extension member | code | `src/Piv/src/IYubiKeyExtensions.cs:38` |
| 2.3 | `piv.GetCertificateAsync(PivSlot)` returns `Task<X509Certificate2?>` | code | `src/Piv/src/PublicAPI.Unshipped.txt:191` |
| 2.4 | `L2-layered-stack.svg` depicts the layer stack | doc | `docs/architecture/images/L2-layered-stack.svg` |

## 02 — transports

| # | Claim | Kind | Anchor |
|---|---|---|---|
| 3.1 | `ConnectionType` has `SmartCard`, `HidFido`, `HidOtp`, `All` | code | `src/Core/src/Devices/ConnectionType.cs` |
| 3.2 | Non-generic `ConnectAsync()` throws on multi-interface devices | code | `src/Core/src/Abstractions/IYubiKey.cs:172-188` |
| 3.3 | Device factory uses `PreferredConnectionType` for selection; direct factory treats it as an assertion | doc | `docs/architecture/applet-public-api.md:10-13` |
| 3.4 | PIV resolves its transport before creating the session | code | `src/Piv/src/IYubiKeyExtensions.cs:41-48` |
| ~~3.5~~ | ~~YubiOTP rides OTP-HID, not APDU~~ | **WITHDRAWN** | **Inverted and false. Superseded by T4** — order is `[SmartCard, HidOtp]` |

## 03 — discovery

| # | Claim | Kind | Anchor |
|---|---|---|---|
| 4.1 | `FindAllAsync(ConnectionType, bool forceRescan, CancellationToken)` | code | `src/Core/src/PublicAPI.Unshipped.txt:985` |
| 4.2 | Discovery is publish-first and degraded-state tolerant | doc | `docs/architecture/device-identity.md:93-96` |
| 4.3 | Canonical Rust withholds publication until metadata is read | doc | `docs/architecture/device-identity.md:93-96` |

## 04 — identity and merging

| # | Claim | Kind | Anchor |
|---|---|---|---|
| 5.1 | Merge inputs are `Device`, `Connection`, `IsUsb`, `Pid`, `Serial`, `DeviceInfo`, `TopologyKey`, `IdentityReadBudgetConsumed` (all eight record members) | code | `src/Core/src/Devices/CompositeDeviceMerger.cs:40-48` (record), doc `:19-39` |
| 5.2 | NFC and unknown-kind PC/SC readers never merge | code | `src/Core/src/Devices/CompositeDeviceMerger.cs:24-27` |
| 5.3 | Repository correlates on an internal interface-set key | code | `src/Core/src/Devices/YubiKeyDevice.cs:123`; `YubiKeyDeviceRepository.cs:106,112` |
| 5.4 | Interface-set key stays internal; `DeviceId` is diagnostic, not durable identity | doc | `docs/architecture/device-identity.md:61-65` (D1) |
| 5.5 | `SerialNumber` is latched: once non-null it never reverts | doc | `docs/architecture/device-identity.md:76-77` (D2) |
| 5.6 | `SerialNumber` may be null forever; Security Key series reports none | doc | `docs/architecture/device-identity.md:73-75` (D2) |
| 5.7 | Serial may arrive late, after publication, with no device event | doc | `docs/architecture/device-identity.md:80-82` (D2) |
| 5.8 | A republished object inherits nothing from its predecessor | doc | `docs/architecture/device-identity.md:82-84` (D2) |
| 5.9 | `Equals`/`GetHashCode` remain referential | doc | `docs/architecture/device-identity.md:161` (D6) |
| 5.10 | Full `DeviceInfo` stays internal because its fields can go stale | doc | `docs/architecture/device-identity.md:88-98` (D2) |
| 5.11 | `key.GetDeviceInfoAsync()` exists without opening a session | code | `src/Management/src/PublicAPI.Unshipped.txt:33` |

## 05 — observability, events

| # | Claim | Kind | Anchor |
|---|---|---|---|
| 6.1 | Monitoring **control** surface is five members: `StartMonitoring()`, `StartMonitoring(TimeSpan)`, `StopMonitoring()`, `WatchAsync(ct)`, `IsMonitoring`. `Shutdown`/`ShutdownAsync` additionally stop monitoring during teardown. | code | `src/Core/src/PublicAPI.Unshipped.txt:986-992`; teardown remark `src/Core/src/Devices/YubiKeyManager.cs:216` |
| 6.2 | `WatchAsync` returns `IAsyncEnumerable<DeviceEvent>` | code | `src/Core/src/PublicAPI.Unshipped.txt:992` |
| 6.3 | `DeviceAction` has exactly `Added` and `Removed` | code | `src/Core/src/DeviceEvent.cs:19-23`; `PublicAPI.Unshipped.txt:203-204` |
| 6.4 | `DeviceEvent` carries `Device`, `Action`, `Timestamp` | code | `src/Core/src/DeviceEvent.cs:25-30` |
| 6.5 | **No `IObservable`, no Rx dependency** | code | `rg DeviceChanges src/` returns zero hits; no `IObservable` in `PublicAPI.Unshipped.txt` |
| 6.6 | Events do not flow until `StartMonitoring()` | doc | `docs/usage/device-discovery.md:71-72` |
| 6.7 | ykman Python is poll-only via `scan_devices()` | peer | `yubikey-manager@4ca60f7:ykman/device.py:108`; `doc/Library_Usage.adoc:75` |
| 6.8 | ykman rust has `monitor_yubikeys` with OS push events | peer | `yubikey-manager@90940e9:crates/yubikit/src/platform/monitor.rs:1113` |
| 6.9 | rust has a third `Changed` variant | peer | `crates/yubikit/examples/monitor_yubikeys.rs:87` |
| 6.10 | Android attach via `Callback<T>.invoke`, detach via `setOnClosed` | peer | `yubikit-android@f462685:.../YubiKitManager.java:82-84`; `UsbYubiKeyDevice.java:154` |
| 6.11 | Swift is connect-on-demand; `makeConnection()` polls, `waitUntilClosed()` observes removal | peer | `swift@1.4.0 USBSmartCardConnection.swift:53-55`; `Connection.swift:32` |
| 6.12 | python-fido2 has no watcher; `list_devices()` enumerates on call | peer | `python-fido2@5bc9d3a:fido2/hid/__init__.py:275-278`; `fido2/ctap.py:62,69-70` |

## 06 — observability, architecture and contracts

| # | Claim | Kind | Anchor |
|---|---|---|---|
| 7.1 | v1: `PeriodicTimer` 500 ms, `Task.Run` async-over-sync, full enumeration each cycle, Windows HID unimplemented | doc | `docs/architecture/event-driven-device-discovery.md` "BEFORE" |
| 7.2 | v2 listeners: `SCardGetStatusChange(1000ms)` plus per-platform HID | doc | `docs/architecture/event-driven-device-discovery.md` "AFTER" |
| 7.3 | Capacity-one signal, single reader | doc | `docs/architecture/event-driven-device-discovery.md` "AFTER" |
| 7.4 | Quiet period is 200 ms | code | `src/Core/src/Devices/YubiKeyDeviceMonitorService.cs:61` |
| 7.5 | Coalescing capped at 5 × throttle | code | `src/Core/src/Devices/YubiKeyDeviceMonitorService.cs:66` |
| 7.6 | Interval polling is fallback only | code | `src/Core/src/Devices/YubiKeyDeviceMonitorService.cs:579-580` |
| 7.7 | `WatchAsync` subscribes on first iteration, not on call | doc | `docs/usage/device-discovery.md:90-92` |
| 7.8 | Per-enumeration buffer is 256 events | code | `src/Core/src/Devices/DeviceEventHub.cs:51` |
| 7.9 | Overflow throws rather than dropping; events are deltas | code | `src/Core/src/Devices/DeviceEventHub.cs:32,138,242` |
| 7.10 | Each enumeration has an independent buffer | doc | `docs/usage/device-discovery.md:118` |
| 7.11 | `StartMonitoring(interval)` while running is a silent no-op | code | `src/Core/src/Devices/YubiKeyDeviceMonitorService.cs:296-303` |
| 7.12 | Logging is opt-in, one line, static `YubiKitLogging.Configure` | doc | `docs/LOGGING.md:5-14` |
| 7.13 | Never inject `ILogger`; use static `YubiKitLogging` | doc | `docs/LOGGING.md:219` |
| 7.14 | Six documented configuration methods | doc | `docs/LOGGING.md:22-137` |
| 7.15 | Sensitive-data logging policy exists | doc | `docs/LOGGING.md:201,244` |

## 08 — raw access tiers

| # | Claim | Kind | Anchor |
|---|---|---|---|
| 8.1 | Tier 0 applet sessions are the golden path | doc | `docs/architecture/raw-access-tiers.md:6-16` |
| 8.2 | Tier 1 raw sessions give framing/chaining/overlap refusal/SCP but no applet selection or feature gates | doc | `docs/architecture/raw-access-tiers.md:19-33` |
| 8.3 | `CreateRawSmartCardSessionAsync`, `CreateRawFidoHidSessionAsync`, `CreateRawOtpHidSessionAsync` exist | code | `src/Core/src/Devices/YubiKeyConnectionExtensions.cs:37,90,108` |
| 8.4 | Tier 2 bypasses `ApplicationSession`, `ConnectionSessionGuard`, `ExchangeGuard` | doc | `docs/architecture/raw-access-tiers.md:138-140` |
| 8.5 | At Tier 2 the caller owns APDU formatting, chaining, correlation, CRC, keep-alive, sequencing, recovery | doc | `docs/architecture/raw-access-tiers.md:141-145` |
| 8.6 | Raw sessions still obey one-connection-per-key and one-session-per-connection | doc | `docs/architecture/raw-access-tiers.md:30-33` |

## 09 — session model

| # | Claim | Kind | Anchor |
|---|---|---|---|
| 9.1 | Each applet has a sealed session, an interface, a device factory and a direct factory | doc | `docs/architecture/applet-public-api.md:3-9` |
| 9.2 | Both factories take `SessionCreationOptions?` then a defaulted `CancellationToken` | code | `src/Piv/src/IYubiKeyExtensions.cs:38-40`; `src/Piv/src/PivSession.cs:130-133` |
| 9.3 | `SessionCreationOptions` is consumed at creation and not retained | doc | `docs/architecture/applet-public-api.md:10-13` |
| 9.4 | `SessionCreationOptions` carries protocol config, SCP params, preferred connection type, firmware override | doc | `docs/architecture/applet-public-api.md:10-13` |
| 9.5 | Convenience factory owns the connection via `OwnConnection()` | code | `src/Piv/src/IYubiKeyExtensions.cs:59` |
| 9.6 | Real call site: connect then direct factory | code | `src/Management/tests/Yubico.YubiKit.Management.IntegrationTests/ManagementSessionSimpleTests.cs:40-43` |
| 9.7 | One physical key admits one live connection | doc | `docs/architecture/raw-access-tiers.md:148` |
| 9.8 | One connection admits one live session; second attach throws `ConnectionInUseException` | code | `src/Core/src/Sessions/ConnectionSessionGuard.cs:44-52` |
| 9.9 | Dispose session N before creating N+1 over the same connection | doc | `docs/architecture/raw-access-tiers.md:151` |
| 9.10 | Prefer `DisposeAsync`; sync `Dispose` blocks to drain | doc | `docs/architecture/raw-access-tiers.md:158-160` |
| 9.11 | Shape enforced by convention tests | code | `src/PublicApi/tests/Yubico.YubiKit.PublicApi.UnitTests/AppletSessionShapeTests.cs`, `AsyncSurfaceConventionTests.cs`, `FactoryShapeTests.cs`, `MemoryAndCollectionConventionTests.cs` |

## 10-12 — applets

All nine device factories, verified to exist:

| Applet | Factory | Anchor |
|---|---|---|
| Management | `CreateManagementSessionAsync` | `src/Management/src/IYubiKeyExtensions.cs:101` |
| PIV | `CreatePivSessionAsync` | `src/Piv/src/IYubiKeyExtensions.cs:38` |
| OATH | `CreateOathSessionAsync` | `src/Oath/src/IYubiKeyExtensions.cs:39` |
| OpenPGP | `CreateOpenPgpSessionAsync` | `src/OpenPgp/src/IYubiKeyExtensions.cs:37` |
| FIDO2 | `CreateFidoSessionAsync` | `src/Fido2/src/IYubiKeyExtensions.cs:124` |
| WebAuthn | `CreateWebAuthnClientAsync` | `src/WebAuthn/src/IYubiKeyExtensions.cs:57` |
| SecurityDomain | `CreateSecurityDomainSessionAsync` | `src/SecurityDomain/src/IYubiKeyExtensions.cs:45` |
| YubiHSM Auth | `CreateHsmAuthSessionAsync` | `src/YubiHsm/src/IYubiKeyExtensions.cs:41` |
| YubiOTP | `CreateYubiOtpSessionAsync` | `src/YubiOtp/src/IYubiKeyExtensions.cs:102` |

Representative operations, verified against the public API baselines:

| # | Claim | Kind | Anchor |
|---|---|---|---|
| 10.1 | `GetDeviceInfoAsync` on `IYubiKey` and on the session | code | `src/Management/src/PublicAPI.Unshipped.txt:33,36` |
| 10.2 | `GetCertificateAsync(PivSlot)` returns `X509Certificate2?` | code | `src/Piv/src/PublicAPI.Unshipped.txt:191` |
| 10.3 | `CalculateAllAsync` returns `IReadOnlyDictionary<Credential, Code?>` | code | `src/Oath/src/PublicAPI.Unshipped.txt:50,87` |
| 10.4 | `GetApplicationRelatedDataAsync` | code | `src/OpenPgp/src/PublicAPI.Unshipped.txt:181` |
| 10.5 | `FidoSession.GetInfoAsync` returns `AuthenticatorInfo` | code | `src/Fido2/src/PublicAPI.Unshipped.txt:609` |
| 10.6 | `WebAuthnClient.MakeCredentialAsync(options, pinBytes, ct)` | code | `src/WebAuthn/src/PublicAPI.Unshipped.txt:112` |
| 10.7 | `GetAssertionAsync` returns `IReadOnlyList<MatchedCredential>` | code | `src/WebAuthn/src/PublicAPI.Unshipped.txt:111` |
| 10.8 | `GetCertificatesAsync(KeyReference)` on SecurityDomain | code | `src/SecurityDomain/src/PublicAPI.Unshipped.txt:22` |
| 10.9 | `HsmAuthSession.ListCredentialsAsync` | code | `src/YubiHsm/src/PublicAPI.Unshipped.txt:36` |
| 10.10 | `YubiOtpSession.GetSerialNumberAsync` | code | `src/YubiOtp/src/PublicAPI.Unshipped.txt:166` |
| 10.11 | SCP can be passed to any session via `SessionCreationOptions.ScpKeyParameters` | code | `src/Management/tests/.../ManagementSessionSimpleTests.cs:215-217` |

Peer snippets:

| # | Claim | Kind | Anchor |
|---|---|---|---|
| 11.1 | ykman Management | peer | `yubikit/support.py:83`; `yubikit/management.py:598` |
| 11.2 | ykman PIV | peer | `yubikit/piv.py:697`; `doc/Library_Usage.adoc:66` |
| 11.3 | ykman OATH | peer | `yubikit/oath.py:265,447` |
| 11.4 | ykman OpenPGP | peer | `yubikit/openpgp.py:987,1088` |
| 11.5 | ykman SecurityDomain | peer | `yubikit/securitydomain.py:100,122` |
| 11.6 | ykman YubiHSM Auth | peer | `yubikit/hsmauth.py:223,250` |
| 11.7 | ykman YubiOTP | peer | `yubikit/yubiotp.py:708,779` |
| 11.8 | rust YubiHSM Auth | peer | `crates/yubikit/src/hsmauth.rs:32,383` |
| 11.9 | Android Management | peer | `management/.../ManagementSession.java:116,361` |
| 11.10 | Android PIV | peer | `piv/.../PivSession.java:208,530` |
| 11.11 | Android OATH | peer | `oath/.../OathSession.java:129,391` |
| 11.12 | Android YubiOTP | peer | `yubiotp/.../YubiOtpSession.java:253,447` |
| 11.13 | Swift Management | peer | `swift@1.4.0 ManagementSession.swift:142` (SmartCard overload), `:159` (FIDO), `:72` |
| 11.14 | Swift PIV | peer | `swift@1.4.0 PIVSession.swift:67,498` |
| 11.15 | Swift CTAP2 | peer | `swift@1.4.0 CTAPSession.swift:49`; `CTAPSession+Creation.swift:25` |
| 11.16 | Swift WebAuthn client | peer | `swift@1.4.0 FIDO/WebAuthn/Client/Client.swift:34-41` (usage doc), `:95` (init). Also present on `origin/main`, at different line numbers |
| 11.17 | Swift SecurityDomain | peer | `swift@1.4.0 SecurityDomainSession.swift:55,124` |
| 11.18 | python-fido2 `Ctap2` runs `GET_INFO` in the constructor | peer | `fido2/ctap2/base.py:246-262` (get_info at :252), `:304-309` |
| 11.19 | python-fido2 `Fido2Client.make_credential` / `get_assertion` | peer | `fido2/client/__init__.py:1066-1179` |
| 11.20 | python-fido2 ships `Fido2Server` (relying-party side) | peer | `fido2/server.py:125` (class), `:158` (`register_begin`) |

Absences:

| # | Claim | Kind | Anchor |
|---|---|---|---|
| 12.1 | Android has **no** YubiHSM Auth module | peer | `yubikit-android@f462685:settings.gradle.kts:35-39` |
| 12.2 | Swift has **no** OpenPGP session | peer | `swift@1.4.0 Capability.swift:24` (capability bit only) |
| 12.3 | Swift has **no** YubiHSM Auth session | peer | `swift@1.4.0 Capability.swift:30` |
| 12.4 | Swift has **no** YubiOTP slot session | peer | `swift@1.4.0 Capability.swift:20` |
| 12.5 | ykman Python delegates FIDO2 to `python-fido2` | peer | `ykman/diagnostics.py:207`; `ykman/_cli/fido.py:116` |

Ownership comparison:

| # | Claim | Kind | Anchor |
|---|---|---|---|
| 13.1 | Python: caller owns via `with dev.open_connection(...)` | peer | `yubikit/core/__init__.py:182` |
| 13.2 | rust: session consumes the connection, returns `(Error, Connection)` on failure | peer | `crates/yubikit/src/management.rs:34` |
| 13.3 | Android: `session.close()` closes the underlying connection | peer | `core/.../SmartCardProtocol.java:125-127`; `piv/.../PivSession.java:252-254` |
| 13.4 | Swift: caller owns; session never closes it | peer | `swift@1.4.0 PIVSession.swift:67` |

## 14 — footprint

| # | Claim | Kind | Detail |
|---|---|---|---|
| ~~14.1~~ | ~~ten libraries, 1.37 MB total~~ — **WITHDRAWN, superseded by F1: 1,441 KiB = 1.41 MiB** | measured | `stat` on `src/*/src/bin/Release/net10.0/Yubico.YubiKit.*.dll` |
| 14.2 | AOT executable 3.11 MiB (3,261,000 bytes) | measured | `dotnet publish -c Release -r osx-arm64 -p:PublishAot=true` |
| 14.3 | NativeShims sidecar 3.71 MiB (3,895,192 bytes) | measured | same publish output |
| 14.4 | AOT peak RSS 11.73 MiB (12,304,384 bytes) | measured | `/usr/bin/time -l` |
| 14.5 | AOT CPU 20 ms (0.01 user + 0.01 sys) | measured | `/usr/bin/time -l` |
| 14.6 | AOT wall 528 ms median of 10 | measured | 10 runs after 3 warm-ups |
| 14.7 | Framework-dependent RSS 51.53 MiB (54,034,432 bytes) | measured | `-p:PublishAot=false --self-contained false` |
| 14.8 | Framework-dependent CPU 170 ms (0.14 + 0.03) | measured | `/usr/bin/time -l` |
| 14.9 | Framework-dependent wall 628 ms median of 10 | measured | 10 runs after 3 warm-ups |
| 14.10 | Verification host links all ten libraries | code | `verification/NativeAotVerification/Yubico.YubiKit.NativeAotVerification.csproj` |
| 14.11 | Six YubiKeys were attached during measurement | measured | host printed `Found 6 YubiKey(s).` |
| 14.12 | NativeShims released package uses shared libraries | doc | PR #578 body, `docs/NATIVE-AOT.md` |
| 14.13 | Static-shim package growth figures | quoted | PR #586 body — **not re-measured** |
| 14.14 | BenchmarkDotNet numbers not collected; project does not compile | code | `DEFERRED.md` #1 |

## 15 — takeaways

| # | Claim | Kind | Anchor |
|---|---|---|---|
| 15.1 | Version is `2.0.0-alpha.2` | code | `Directory.Packages.props:6` |
| 15.2 | Public API still in `PublicAPI.Unshipped.txt` | code | `src/*/src/PublicAPI.Unshipped.txt` |
| 15.3 | No `Fido2Server` equivalent; v2 is client-side only | code | no server type in `src/WebAuthn/src/PublicAPI.Unshipped.txt` |
| 15.4 | AOT runtime evidence limited to Core discovery, Management reads, a PIV session | doc | PR #578 "Evidence boundaries" |
| 15.5 | Recurring AOT CI is macOS arm64 without hardware | doc | PR #578; `.github/workflows/native-aot.yml` |

---

## Claims deliberately cut

- **"WatchAsync events may be delivered on any thread."** Documented at
  `docs/usage/device-discovery.md:254`, but not verified in source during this pass.
  Cut rather than asserted.
- **A worked example of two sequential sessions over one caller-owned connection.**
  The *rule* is anchored (9.8, 9.9), but no real call site doing exactly this was found.
  The slide states the rule and shows a single-session call site instead of inventing
  a snippet.

---

# Round-2 additions and corrections

Added after the first independent review. Verified against `d04d59aa` and the
repinned peer SHAs (Swift now `release/1.4.0` @ `c76ae973`).

## Transports — the corrected multi-transport claims

| # | Claim | Kind | Anchor |
|---|---|---|---|
| T1 | Management order is `[SmartCard, HidFido, HidOtp]` — three transports | code | `src/Management/src/IYubiKeyExtensions.cs:143-144` |
| T2 | FIDO2 order is `[HidFido, SmartCard]` | code | `src/Fido2/src/IYubiKeyExtensions.cs:189-190` |
| T3 | FIDO2 SmartCard path is NFC, or USB-CCID on **firmware 5.8.0+** | doc | `src/Fido2/src/IYubiKeyExtensions.cs:95` (XML doc in source) |
| T4 | YubiOTP order is `[SmartCard, HidOtp]` — **SmartCard preferred** | code | `src/YubiOtp/src/IYubiKeyExtensions.cs:144-145` |
| T5 | WebAuthn inherits FIDO2's transport resolution | code | `src/WebAuthn/tests/.../IYubiKeyExtensionsTransportTests.cs:47` (`DefaultBothTransports_PicksHidFido`) |
| T6 | PIV, OATH, OpenPGP, SecurityDomain, YubiHSM are SmartCard-only | code | `IYubiKeyExtensions.cs` `:46`, `:47`, `:45`, `:53`, `:49` respectively |
| T7 | SCP key parameters force SmartCard absent an explicit preference | code | `src/Management/src/IYubiKeyExtensions.cs:109`; `src/Fido2/src/IYubiKeyExtensions.cs:132`; `src/YubiOtp/src/IYubiKeyExtensions.cs:110` |

**Supersedes** the withdrawn claim 3.5 ("YubiOTP rides OTP-HID, not APDU"), which was
inverted.

## Session shape — eight, not nine

| # | Claim | Kind | Anchor |
|---|---|---|---|
| S1 | `AppletSessionShapeTests` enumerates **eight** sessions | code | `src/PublicApi/tests/.../AppletSessionShapeTests.cs:16-26` |
| S2 | WebAuthn is deliberately excluded from the applet-session shape | code | `src/PublicApi/tests/.../FactoryShapeTests.cs:56` (comment, verbatim) |
| S3 | `CreateWebAuthnClientAsync` requires `WebAuthnOrigin` and `PublicSuffixChecker` positionally | code | `src/WebAuthn/src/IYubiKeyExtensions.cs:57-62`; null-checked `:64-65` |
| S4 | Real call site for the WebAuthn factory | code | `src/WebAuthn/tests/.../WebAuthnClientFactoryTests.cs:42-45` |

**Supersedes** the withdrawn "nine applets, enforced by tests" claim.

## Applet delta corrections

| # | Claim | Kind | Anchor |
|---|---|---|---|
| D1 | ykman returns `x509.Certificate` from `get_certificate` | peer | `yubikey-manager@4ca60f7:yubikit/piv.py:1258` |
| D2 | Android returns `java.security.cert.X509Certificate` | peer | `yubikit-android@f462685:piv/.../PivSession.java:882` |
| D3 | .NET returns `X509Certificate2?` — nullable for an empty slot | code | `src/Piv/src/PublicAPI.Unshipped.txt:191` |
| D4 | Android OATH is `Map<Credential, @Nullable Code>` | peer | `yubikit-android@f462685:oath/.../OathSession.java:391` |
| D5 | Android `Slot.AUTHENTICATION` exists | peer | `yubikit-android@f462685:piv/.../Slot.java:23` |
| D6 | Swift WebAuthn usage is `makeCredential(options, authorization: .pin(...)).value` | peer | `swift@1.4.0 FIDO/WebAuthn/Client/Client.swift:34-41` |
| D7 | Swift Management has SmartCard **and** FIDO `makeSession` overloads | peer | `swift@1.4.0 ManagementSession.swift:142, :159` |

**Supersedes** the withdrawn PIV delta ("peers hand back raw bytes") and the withdrawn
OATH delta.

## Coverage matrix — per-cell anchors

| # | Claim | Kind | Anchor |
|---|---|---|---|
| C1 | rust implements all nine | peer | `yubikey-manager@90940e9:crates/yubikit/src/` — `management.rs`, `piv.rs`, `oath.rs`, `openpgp.rs`, `ctap2/mod.rs`, `webauthn/client.rs`, `securitydomain.rs`, `hsmauth.rs`, `yubiotp.rs` all present |
| C2 | Android has SecurityDomain | peer | `yubikit-android@f462685:core/.../smartcard/scp/SecurityDomainSession.java` |
| C3 | Android has a WebAuthn client | peer | `yubikit-android@f462685:fido/.../client/WebAuthnClient.java` |
| C4 | Swift lacks OpenPGP, YubiHSM Auth, YubiOTP sessions | peer | `swift@1.4.0` — no such files; `Capability.swift:24,30,20` carry only bits |

## Footprint corrections

| # | Claim | Kind | Anchor |
|---|---|---|---|
| F1 | Ten assemblies total **1,441 KiB = 1.41 MiB** | measured | sum of the ten `stat` values, expressed in **KiB** (1024 B). Supersedes 14.1's 1.37, which was the same bytes mislabelled as SI MB. |
| F2 | AOT executable is exactly 3,261,000 bytes | measured | `stat -f%z /tmp/aotpub/Yubico.YubiKit.NativeAotVerification` — genuinely round, not rounded |
| F3 | Recurring AOT CI run on `yubikit` | CI | `native-aot.yml` run `34121554932`, 2026-09-07, `success` |
| F4 | Recurring AOT CI is hardware-free | doc | `.github/workflows/native-aot.yml` asserts `Found 0 YubiKey(s)` |
| F5 | `/usr/bin/time -l` quantises CPU to 10 ms, so the CPU ratio is order-of-magnitude only | measured | inherent to the tool; stated on the slide |
| F6 | Version is `2.0.0-alpha.2` | code | `Directory.Packages.props:6` |

## Claims withdrawn in round 2

- **3.5** — "YubiOTP rides OTP-HID, not APDU". Inverted; replaced by T4.
- **"Nine applets enforced by tests"** — the cited test covers eight; replaced by S1/S2.
- **PIV delta "peers hand back raw bytes or their own wrapper"** — false for both peers
  shown; replaced by D1–D3.
- **OATH delta** — was not a delta; replaced by D4.
- **Static-shim package-growth provenance row** — the row advertised a number that
  appeared on no slide. Row removed rather than a number invented.
- **All Swift anchors against `main`** — repinned to `release/1.4.0` because it is the
  development tip, 58 commits ahead of `origin/main` and contemporaneous with the .NET
  pin, and because the cited line numbers differ between the two refs. Every Swift line
  number re-verified. **Note:** the original wording of this bullet claimed `main` was
  89 commits behind and lacked `WebAuthn/Client/`. Both were false — see R1-R4.

## Round-3 corrections (second review)

| # | Claim | Kind | Anchor |
|---|---|---|---|
| R1 | `origin/main` @ `f5a01653` **does** contain `FIDO/WebAuthn/Client/Client.swift` | peer | `git cat-file -e origin/main:YubiKit/YubiKit/FIDO/WebAuthn/Client/Client.swift` succeeds. Corrects the false A1 evidence. |
| R2 | `release/1.3.0` is **fully merged** into `origin/main` (0 commits ahead) | peer | `git rev-list --count origin/main..origin/release/1.3.0` = 0 |
| R3 | `release/1.4.0` is **58 commits ahead** of `origin/main` and dated 2026-09-07 | peer | `git rev-list --count origin/main..origin/release/1.4.0` = 58 |
| R4 | Swift anchors are branch-sensitive — `CTAPSession.getInfo` is `:42` on `origin/main`, `:49` on `release/1.4.0` | peer | both refs read directly |
| R5 | Non-CPU wall time is 508 ms (AOT) and 458 ms (framework-dependent), ~11 % apart | measured | 528−20 and 628−170 from the measured table |
| R6 | Tier 2 `ISmartCardConnection.TransmitAndReceiveAsync` takes `ReadOnlyMemory<byte>` | code | `src/Core/src/PublicAPI.Unshipped.txt:655`; Tier 1 `RawSmartCardSession` takes `ApduCommand` at `:535` |
| R7 | `DeviceInterfaceDescriptor` includes `TopologyKey` (Windows Container ID, `null` elsewhere) and `IdentityReadBudgetConsumed` | code | `src/Core/src/Devices/CompositeDeviceMerger.cs:40-48`, doc `:31-35` |
| R8 | ykman declares a hard dependency on `python-fido2` | peer | `yubikey-manager@4ca60f7:pyproject.toml:21` — `"fido2 (>=2.0, <3)"` |
| R9 | `python-fido2` supplies `Fido2Client` as well as `Ctap2` | peer | `python-fido2@5bc9d3a:fido2/client/__init__.py:1066` |
| R10 | v1 pattern was **async-over-sync**, not sync-over-async | doc | `docs/architecture/event-driven-device-discovery.md:14,31` |

### Withdrawn in round 3

- **A1's original evidence** — the 89-commit count, the is-ancestor result, and
  "`Client.swift` not present on `main`" were all produced against a **stale local
  `main` ref** (`4833bb7dc9`) instead of `origin/main`. All three are false. The repin
  to `release/1.4.0` stands on R3 and R4 instead.
- **Claim 3.5** and **claim 14.1** are now struck through in place rather than merely
  superseded further down the file.


## Round-4 corrections (third review)

| # | Claim | Kind | Anchor |
|---|---|---|---|
| R11 | v2 is async **on the golden path**, but sync members exist by design | code | `YubiKeyManager.Shutdown()` = `ShutdownAsync().GetAwaiter().GetResult()` (`src/Core/src/Devices/YubiKeyManager.cs:257`, public `PublicAPI.Unshipped.txt:987`); `ISmartCardConnection.BeginTransaction(ct)` is synchronous (`:653`) and blocks on a `Task` (`src/Core/src/Transports/SmartCard/UsbSmartCardConnection.cs:102`) |
| R12 | `ShutdownAsync` stops monitoring as part of teardown, so it is monitoring lifecycle | doc | `src/Core/src/Devices/YubiKeyManager.cs:216` — "stops monitoring if active"; `<seealso cref="StopMonitoring"/>` at `:229` |
| R13 | `Fido2Server` class is at `server.py:125` | peer | `python-fido2@5bc9d3a1:fido2/server.py:125` |
| R14 | `DeviceInterfaceDescriptor` has **eight** members, first is `Device` | code | `src/Core/src/Devices/CompositeDeviceMerger.cs:40-48` |
| R15 | CI run `34121554932` head_sha **equals the deck's pin** `d04d59aa` | CI | GitHub REST `actions/runs/34121554932` — run 189, `native-aot.yml`, branch `yubikit`, `success`, 2026-09-07 |
| R16 | Footprint column is **KiB** (1024 B), not SI KB | measured | 1,441 KiB = 1.407 MiB; the same bytes as SI KB would be 1.374 MiB |

### Withdrawn in round 4

- **Claim 1.1 / intro bullet "no sync-over-async"** — false against `src/`. Replaced by
  R11 and the qualified slide wording "async on the golden path".
- **Claim 6.1's "exactly" four members** — the monitoring surface is five control
  members plus two teardown members. Anchor widened from `:989-992` to `:986-992`, which
  was previously narrowed in a way that made the exhaustiveness claim read true against
  its own citation.
