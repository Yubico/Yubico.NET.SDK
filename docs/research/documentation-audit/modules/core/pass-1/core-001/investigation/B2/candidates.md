# B2 local candidates and reopening map

These six groups are investigator proposals at source `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`.
They remain **candidate**, with accepted IDs unassigned. They are not six accepted implementation bugs.
All named product and shared paths remain unchanged and require an explicit later editor/reviewer
disposition, including passages retained as accurate dependencies. SHARED exclusively integrates shared
documents; a fresh future editor owns any authorized product-comment work. Executable declarations,
assertions and console strings remain preserved in Pass 1.

## CORE-B2-C001 — lead Linux setup with scoped device access

**Documentation:** candidate correction, consequential security guidance. **Runtime:** unchanged.
**Decision:** choose documented desktop/session or administrator-managed service access, keeping the
application's deployment context explicit. **Qualification:** no rule was installed or evaluated here.

`docs/linux-setup.md:54–87` leads with `MODE="0666"` for every Yubico hidraw interface and every Yubico USB
device, then makes `0660` group access an alternative at 89–102. This grants access to every local user;
the USB rule is broader than the hidraw access these transports require. PC/SC uses its daemon/reader
driver path, described separately at 5–38. Existing package rules may already provide appropriate access.
The supported next step is to inspect the selected YubiKey node and distribution-supplied rules, then
grant access only to the intended user/group or active desktop session. A service needs an explicitly
managed group rather than an assumed interactive login ACL. Explain that `plugdev` is an example group
which the administrator must provide, and keep read/write requirements near the actual typed opens.

Original systemd 262 `udev(7)` defines OWNER/GROUP/MODE and rule matching; pinned Yubico Device Permissions
separates PC/SC, keyboard and FIDO access. The linked upstream `69-yubikey.rules` currently assigns
`ID_SECURITY_TOKEN`, rather than the guide's world-writable modes. That rule alone is not proof that
every distribution grants an ACL: the administrator must verify the resulting access in its deployment.
Those publications are configuration/API guidance, not a normative SDK permission mandate.

**Source → oracle → documentation:** owner `Open` uses O_RDWR; descriptor discovery can fall back to
O_RDONLY; `IsHidrawReady` only probes read access. Fake open flags and descriptor tests establish arguments,
not user ACL behavior. Reopen Linux setup, root/Core prerequisites, native-AOT deployment guidance and
direct-report examples together when permission guidance changes. Keep `LinuxHidInterface.cs`,
`LinuxHidEventSource.cs`, both native owners and `LinuxHidReportApi.cs` as associated unchanged dependencies.
**Trigger:** permission defaults, required access flags, distribution rules or PC/SC prerequisites change.

## CORE-B2-C002 — distinguish stage history from current integration ownership

**Documentation:** candidate current-context clarification. **Runtime:** unchanged. **Decision:** retain
stage records as historical evidence, with a precise successor pointer rather than rewriting old results.
**Qualification:** original execution identities remain attached to their own source revisions.

`linux-hid-routing.md:45–48,102–105` attributes discovery scheduling/lease transfer to ProtocolDeviceInfo.
The current split places it in `SharedDeviceInfoRead.ConnectAndReadAsync:248–292`; ProtocolDeviceInfo
borrows an open connection and owns only its matching protocol. The L5 text is useful history, but a
reader using it as current architecture needs the current actor and source pin nearby. Similarly,
`LinuxDescriptorLease.cs:19–21` and `LinuxHidEventSource.cs:192–193` speak of a future reactor even though
the production reactor now retains registration-long references. Replace active comments with the
present two reference scopes. Stage documents contain compressed forms such as `at most32`, `holds160`,
`exactly8` and `report length or64`; normal spacing improves those explanations without changing meaning.

The L0/L1/L2/L3/L4 and L6a statements excluding later-stage work are **historical slice scopes**. Preserve
them with that status. The latest `linux-native-acceptance.md` checkpoint at 12:39:27Z records serial 103
and passing read-only physical routes; its earlier blocked sections and original JSON fields stay intact.
Current PublicSyncBoundaryRegistry requires 23 entries, including eight Linux entries; L6a's recorded
24 describes its earlier source. Adapter links are reachability/runnable-test links rather than recursive
execution or native release attestation. Do not silently change historical counts to current counts.

**Source → oracle → documentation:** SharedDeviceInfoRead transfers before invoking open and releases
only an untransferred lease. LinuxDiscoveryAdmissionTests holds PID admission through actual owner close;
LinuxHidRoutingTests supplies direct/composite/epoch cases. Read-only hardware checkpoint and integration
native-only output have distinct envelopes. Reopen the L5 handoff, active Core guides, native lifetime
comments, architecture ownership/raw tiers and migration notes after a responsibility split or new current
qualification. Associated unchanged paths include all L0–L6 evidence documents/JSON, the actual slot,
SharedDeviceInfoRead, ProtocolDeviceInfo, both owners and all 14 verifier files. Preserve old source/hash
identities and explanatory limits. B1-C007 supplies the separate active ownership-guide Linux wake correction.

## CORE-B2-C003 — describe libudev string and unref contracts accurately

**Documentation:** candidate correction. **Runtime:** current return is deliberately ignored; no observed
ABI failure or authorized declaration change. **Decision:** any executable return-type alignment is Pass 2.
**Qualification:** source contracts checked against systemd v257, not every deployed libudev build.

`Udev.Interop.cs:123–129,164–171,192–199` describes returned native strings as ASCII, while the managed
Linux `PtrToStringAnsi` conversion uses UTF-8. These are native NUL-terminated strings copied while their
udev/enumerate/device owner remains live. Name that representation and lifetime instead of promising
ASCII-only paths. `238–246` states the C signature of `udev_monitor_unref` as `void`; the original v257
implementation returns a monitor pointer, documented always NULL. The managed `void` import ignores that
return, so a comment fix can state the actual C contract while preserving executable tokens.

Parent lookup is borrowed from the child, as v257 `libudev-device.c:388–415` explicitly states. Current
GetList/ReceiveEvent scopes retain the child while traversing/copying. The overloads using DangerousGetHandle
have narrower caller-owned lifetime obligations than SafeHandle-marshalled calls; no parent SafeHandle or
extra unref should be invented. Release of a monitor reference owns its socket cleanup, distinct from
checking a hidraw physical connection claim.

**Source → oracle → documentation:** LinuxUdevHidEventSourceTests checks the signed get_fd delegate shape
and scripted wake policy, not the native unref signature or non-ASCII path round trip. Reopen Udev.Interop,
the four udev SafeHandles, LinuxHidInterface/EventSource, native rationale and relevant Linux setup text if
encoding or reference ownership changes. Add a non-ASCII scoped path-copy probe only in a later assigned
native environment. All those paths remain unchanged here. B4 retains general loading/marshalling claims.

## CORE-B2-C004 — unversioned libudev name adds an undeclared runtime prerequisite

**Documentation:** candidate installation/troubleshooting qualification. **Runtime:** source/package-backed
deployment candidate. **Decision:** select a stable soname or an explicit resolver in separately authorized
implementation work; avoid ad hoc user-created library symlinks. **Qualification:** no clean Ubuntu host
or container was executed in B2.

`Native/Linux/Linux.Libraries.cs:20` imports `libudev.so`. Canonical's Ubuntu noble amd64 file lists place
`libudev.so.1` and `.1.7.8` in `libudev1`, while the unversioned `libudev.so` is in `libudev-dev`. .NET's
documented Linux name variations add prefixes/suffixes; they do not infer `.so.1` from `.so`.
`FindHidInterfaces.FindAllLinux:81–91` catches DllNotFoundException, logs a warning and returns no HID
interfaces. A runtime-only installation can therefore look like empty discovery despite valid device
permissions. Existing setup prerequisites describe PC/SC packages and udev rules without this loader-name
condition. The conditional conclusion applies when the unversioned entry/resolver is absent; the package
lists do not prove every Ubuntu machine has that condition.

**Source → oracle → documentation:** injected report/reactor tests bypass libudev resolution; the retained
Omarchy native/selected-key captures qualify that recorded host, not a minimal Ubuntu package set. Reopen
Linux setup, Core/root requirements, native-AOT packaging, empty-discovery troubleshooting and native
library-name comments after the selected loading contract changes. Keep Linux.Libraries, Udev.Interop,
FindHidInterfaces and verifier publish/project contracts as associated unchanged dependencies. B4 receives
the loader/package boundary, while A receives the warning-versus-empty-discovery consequence. A meaningful
future test uses an isolated minimal deployment with recorded installed files and library resolution;
it should assert discovery's classified outcome independently of attached hardware.

## CORE-B2-C005 — successful-path oracles leave some assertion-failure lifetimes undrained

**Documentation:** candidate qualification of test-coverage descriptions. **Runtime:** test-harness cleanup
candidate, separate from shipping owner correctness. **Decision:** repair cleanup only under an explicit
test-edit scope, preserving existing assertions. **Qualification:** source traces only, no injected red run.

`LinuxFidoHidConnectionTests.Open_ReturnsInvocationWhileNativeIsHeld:23–36` drains the invocation in
finally, then awaits/disposes the returned connection outside it. If AssertResponsive throws, release can
finish a successful open but control skips disposal of that result. The rooted native owner/worker can
remain live. `HeldWrite...:39–70` similarly drains output in finally but only requests disposal inside the
successful assertion body. A failure before that request leaves the connection owner unclosed. In
`CancelDuringPreparation:95–117`, a pre-cancellation assertion failure can make finally's expected-cancel
assertion replace the primary error after a successful open. The OTP held-native helper at 23–40 instead
captures and disposes the opened result within finally and is a useful local pattern.

`LinuxHidRoutingTests.DiscoveryTimeout...LateOpenAndCleanup:154–190` releases its gate in finally but does
not independently await late work/recovery on every failing branch. Some reactor tests create/publish a
held poll token and make setup assertions before a cleanup finally; `PublishedFailedAdmission:38–80` is a
concrete example. A harness watchdog bounding a child limits test-process damage, while actual resource
drain must still be observed for any cleanup claim. Later worker-failure setup and direct synchronous
disposal witness already have stronger finally/independent-join cleanup; preserve those positive oracles.

**Source → oracle → documentation:** stage descriptions say tests cover named outcomes, which remains
true for their successful paths. Avoid upgrading those descriptions into arbitrary setup/assertion-failure
or all-race guarantees. Reopen these complete test methods, their fakes/WithheldNativeProbe, L0 helper
contract, L3/L5/L6a evidence summaries, boundary registry links and Core test guidance when cleanup changes.
LinuxNativeAbiTests and Python gate tests also prefer literal `/tmp/opencode` when it exists; the current
quota-constrained coordinator must prepare a home-backed adaptation before executing those selections.
Their temporary-path behavior is a separate harness constraint, not an owner leak diagnosis.

## CORE-B2-C006 — expose direct Linux report ownership and parser scope at the entry point

**Documentation:** candidate XML usefulness/precision improvement. **Runtime:** unchanged.
**Decision:** retain public synchronous compatibility signatures and expert responsibility.
**Qualification:** examples remain source-checked, `VALID-NOT-CHECKED` for later consumer compilation.

`LinuxHidInterface.ConnectToFeatureReports/ConnectToIOReports:126–142` only promises an active connection.
The implementation synchronously waits for a new native owner; the caller owns the returned connection,
these direct opens carry no grouped registry claim, input IO waits indefinitely, and feature calls wait
for native return. Keep this concise ownership/blocking context near these implementation comments and
the public IHidInterface contract (B4/B1 dependency), with the typed `IYubiKey.ConnectAsync<T>` alternative
and explicit `await using` ownership. Detailed facade XML already accurately distinguishes metadata,
actual input length, typed64/8 versus bounded direct SET and checked close.

`ParseHidDescriptorBytes:328–367` extracts the first Usage Page/Usage independently and returns the pair;
`GetList:111–115` applies supported vendor/interface classification. The prominent validation comment at
330–331 should name this downstream validation rather than imply a general HID parser or in-method
combination validator. The complete descriptor vectors explicitly characterize truncation, long-item
progress, first-pair selection and narrowed values; LinuxHidReportSizes separately rejects numbered
reports and bounds widened report arithmetic. Preserve those deliberate limits.

**Source → oracle → documentation:** direct facade tests assert supported caller lengths and actual
read count; routing tests prove typed opens select owners; raw factory initialization/recovery belongs
B5. Reopen LinuxHidInterface, IHidInterface/IHidConnection XML, raw-access architecture, Core README direct
examples and migration EOF/no-timeout notes if direct access or descriptor classification changes.
LinuxHidIOReportConnection, LinuxHidFeatureReportConnection and both typed connections are useful
associated unchanged paths. B5 retains CTAPHID/OTP wire semantics and applet-specific recovery policy.
