# Astra clean sample — first 20 proposed units

Read the complete pinned units in `read-001.md` and relevant current enclosing implementations.
These are documentation/contract observations, pending final unchanged-unit mapping and eligibility
reconciliation. No runtime, hardware or provider result is inferred.

## Material misses

- **SAMPLE-A001 — `711e47d18bab0742`**, `HotplugSignals.Dispose`: the summary describes completed
  cleanup, but three `BestEffort` loops suppress cleanup failures; a throwing logger can also interrupt
  later work. Describe ordered attempts and the actual completion boundary. The complete current
  `HotplugSignals.cs` was read, including `TryStart` and `BestEffort`.
- **SAMPLE-A002 — `c085f1cd3632e7c2` and `076b4d4328825902`**, skip-cause enum/type explanation:
  `InterfaceLeaseHeld` also covers another discovery lease. `SharedDeviceInfoRead` maps failed
  availability/acquisition to this cause; `DeviceConnectionRegistry.TryAcquireDiscovery` checks
  `_discoveryActive` as well as connection holders/waiters. The existing explanation names only
  connections. Correct the actor scope while preserving the enum and runtime behavior.

The A editor owns these bounded comment corrections. Remove corrected overlaps from the clean
denominator and expand A's XML-contract and enum-value strata. These findings add documentation
corrections, not new independently demonstrated runtime defects.

## Other 17 units — content supported within sampled scope

| Original ID suffix | Observation |
| --- | --- |
| `d75a3dbb4ec57c42` | SmokeFilter composes exactly the Slow and RequiresUserPresence exclusions named in its summary. |
| `816c2fdd798610af` | Formatter names concrete flags and returns Unknown for no recognized bits; prose promises human-readable formatting only. |
| `6e353becfd918253` | IYubiKey forwards the documented concrete/group query to the available-connection helper; this is capability metadata, not a successful-open or no-contention oracle. |
| `1fc213103a564722` | HID expansion and unknown-bit masking match the complete helper and constants; illustrations agree. |
| `d231181d1329769b` | FIDO/OTP interface mapping and Unknown fallback match the switch. |
| `2671911eb64af1aa` | KnownPids matches every FromInterfaces output plus SkyPid; the complete parser and expected-interface map were read. No extra normative PID provenance is inferred. |
| `fde9854638dd24be` | Initial/interval/coalesced sequential triggers, cancellation checks and channel-closure termination agree with RunAsync, ReadAsync and CoalesceAsync. |
| `4a3cd5465f24843d` | Parameterless qualifier initializes FirmwareVersion and Final, with the unassigned numeric Iteration defaulting to zero. |
| `3f972c5207254bdd` | Count returns the list exposed by GetAll; enclosing GetAll identifies initialized authorized-device observations. |
| `36956745104ff731` | Unknown form factor bypasses the corresponding FilterDevices criterion. |
| `0f2b4a412a201ec2` | None bypasses capability filtering; the implementation consults enabled USB/NFC bits, separate from selected transport. |
| `3d71e6d3bd862995` | Attribute capability wording preserves the USB-or-NFC versus RequireUsb distinction. |
| `20c9c91498bfd689` | Placeholder criteria are retained for binding and serialized field-by-field; relevant assignments, binding and serialization anchors checked. |
| `2d7384815a34e691` | FirmwareVersion is exactly a DeviceInfo forwarding property; no fresh device read is claimed. |
| `019eae4efe42d6e5` | USB support is derived from nonempty metadata capabilities; it is not the current connection's transport. |
| `c20e4d6e55c8a0bb` | Cache key is the shown serial/type composite used by both AddDevice and GetDevice. |
| `35700b4303b7b14b` | Clear removes dictionary entries under the same lock used for cache access; it makes no connection-disposal claim. |

The source context read includes `ConnectionType`, its complete extensions, `ReaderNamePidParser`,
`RescanScheduler`, `YubiKeyTestInfrastructure.FilterDevices`, `AuthorizedDevices.GetAll`/Count,
`YubiKeyTestState.HasCapability` and the complete cache implementation. Final source mapping must
still exclude any subsequently edited sample. Other proposed samples remain unread.
