# SHARED source and citation check

This is editor checking, not independent review or execution. Original registers/captures remain
unchanged. No current source was fetched beyond one Context7 orientation query for .NET/C# span
lifetimes. Consequential claims use the retained original revisions and actual pinned SDK sources.

| Original / exact status and checked passage | Actor, strength, applicability and edited use |
| --- | --- |
| Apple XNU `xnu-12377.1.9`, retained `B3/sources/apple-IOReturn.h:150–190` | Numeric ABI definitions: NotOpen 0xE00002CD versus NotPermitted 0xE00002E2. Correct current troubleshooting name without rewriting the historical numeric failure or diagnosing its cause. |
| Apple IOHIDFamily `777ccd9698845aadf711e32d843c8c9b777431d9`, retained `apple-device-class.m:496–645` | Implementation's initConnect/open/close: TCC-denied path assigns NotPermitted; success/exclusive-open state and close are separate. Conditional host-access advice only, not universal macOS permission or physical release proof. |
| .NET runtime v10.0.0, `C/sources/dotnet-v10.0.0-AsnWriter.cs:79–99,185–209` | Encode copies the written span; Reset clears the written prefix. Writer and returned encoding have separate owners. Current SDK private encoders omit Reset. No claim that these bytes identify the installed runtime/provider or all exceptional cleanup. |
| .NET runtime v10.0.0, retained EccSecurityTransforms.cs:184–232 and .macOS.cs:52–79 | Missing Q invokes extraction; supplied X/Y skips that route. Extraction exports/reimports EC parameters. Narrow provider warning to that inspected route; native key internals and erasure remain unqualified. |
| RFC 5915, June 2010, Informational, retained rfc5915.txt:96–149, §3 | Schema OPTIONAL differs from conforming implementation's MUST include parameters. Current encoder's inner omission is a target-profile limitation; no automatic fix, general DER validation, or importer interoperability promise. |
| SCP11 v1.3.0.13 Public Review, August 2023, retained derivative:1076–1137, §4.3/4.4 | a/c OCE authenticates to SD with certificate and first correct-MAC post-establishment APDU; b authenticates SD to OCE only. Draft role/phase distinction, not final 1.4 or exact vendor authorization proof. Original PDF hash stays 5db47b3df3c4e565a5ad91257395604f3482ed5db70c64853544c3b0e289fde0. |
| SCP03 v1.1.2 Public Release, March 2019, retained original/derivative and final B6 register, §§6.2.5/6.2.6 | Card status-only errors and off-card absent-data rule remain different from the synthetic model. Preserve conformance gaps and exact edition; code-cited 1.1.2.6 Public Review clauses are uninspected, not interchangeable section numbers. Original PDF hash stays 973a10834f7fe8befd98da9af65393622234b80745092bd85248804c57b648dd. |
| CTAP 2.3 Proposed Standard, February 26, 2026, B5 retained register / protocol editor citations, §11.2.9.1.5 | Client MAY cancel active CBOR; authenticator MUST NOT respond to CANCEL itself and returns the original canceled request's terminal status. Guide distinguishes that protocol rule from keep-alive-triggered signaling and late-success SDK policy. Vendor-command scripts supply no broader CBOR mandate. |
| systemd udev(7) 262 retained HTML; Ubuntu noble amd64 libudev1/libudev-dev retained file lists; B2 original register | Administrator MODE/GROUP semantics and exact package `.so.1` versus `.so` layout. Group example is local deployment guidance with account prerequisites, not a measured ACL result or all-distribution statement. No system configuration changed. |
| NativeShims 1.18.1 source `dedba95b94946907112a7953cd82df8a15c22655`, B4 retained shim-pcsc.c:181–205, target-header/source register | Native_SCardTransmit directly forwards the PCI pointer. Inspected Linux LP64 header has two unsigned longs versus current managed two four-byte fields. Conditional selected-route discrepancy and producer gap retained; Windows DWORD/macOS framework and actual installed behavior are separate. |
| ARKG draft-bradleylundberg-cfrg-arkg-09 title/status and C source register | July 7, 2025, intended Informational, expired January 8, 2026. Preserve target/domain separation, not a final standard, injectivity, immutable-generator provenance, native erasure or whole-WebAuthn acceptance. |

The latest B1 clarification keeps independent original NIST PDF review outstanding. No normative PIV
PIN-state rewrite is based on the attributed excerpt. Requested unpower, actual release, applet
deselection, PIN state and cross-process authorization remain distinct.

## Direct SDK source checks

- `DeviceInfo.cs`: `ChallengeResponseTimeout` is ReadOnlyMemory backed by ReadFixedField/Tlv.Value;
  AutoEjectTimeout is a scalar ushort. Snapshot replacement and alias stability are distinguished.
- `YubiKitLogging.cs`: public factory/Configure/UseTemporary versus internal CreateLogger; temporary
  scope captures/restores a process-global factory. PcscProtocol ordinary trace and SELECT templates
  were read directly; exception provenance is separate.
- `RawSmartCardSession.cs`, PcscProtocol.cs, SmartCardMaxApduSizes.cs: real borrowed factory overloads,
  Configure before SCP, plain SELECT graph, bool/token argument positions, 1390/2038/3062 complete-command
  constants and 255-byte short chunks. ApduException exposes nullable signed-short SW, not StatusWord.
- StaticKeys and Scp03/11 parameter types: source clones versus retained contained owners, certificate
  list copy versus object aliases, disposal responsibility and shallow record clones.
- ConsoleCredentialReader/options: synchronous interactive UTF-16 pool, redirected ReadLine string,
  retained first confirmation owner, filtering/hex/positive-length allocation and waiting-key-only
  cancellation checks. PIV VerifyPinAsync takes ReadOnlyMemory and token; the new method borrows IPivSession.
- MacOSHidDeviceListener: static unmanaged callback addresses, generation GCHandle context and actual
  lifetime, replacing stale per-instance delegate instruction only.
- FakeSmartCardConnection: EnqueueResponse/TransmittedCommands, synchronous disposal observation and
  current empty async disposal. Correct examples do not manufacture a counter assertion.
- TestTargets source: separate switches per `&` term. Runner repeated-switch semantics remain unrun;
  no AND/OR conclusion or library migration is inferred.
- AES-CBC key/IV copies precede the cleanup try. Source confirms the exceptional allocation scope;
  no allocation failure, provider, or cryptographic operation was executed.

Context7 `/dotnet/docs` C# 13/ref-struct documentation confirms locals may occupy async segments
without crossing await/yield suspension boundaries. The selected repository language is C# 14. This
orientation supplies no compiler run. Every current fence/XML body is captured and classified separately
in `example-bodies.json`; none is called compiler-verified or runtime-qualified.
