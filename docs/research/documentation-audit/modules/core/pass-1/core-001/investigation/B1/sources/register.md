# B1 original-source register

Access date: November 11, 2026. `manifest.json` binds fetched bytes to URLs and SHA-256 values. The SDK source baseline is `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; external sources describe their own actors and revisions.

## PC/SC Workgroup

**Interoperability Specification for ICCs and Personal Computer Systems, Part 5, ICC Resource Manager Definition, revision 2.01.01, September 2005.** The revision history identifies 2.01.00 as Final Release and 2.01.01 as the September 29 name correction. Retained PDF SHA-256: `524c59e88e50ec0bb2171a7d4ad6369a80d9ab46eb31a77ba603d6d2fe7cb0ff`. The adjacent text is a `pdftotext -layout` derivative.

- §2.1, printed page 2: the resource manager controls shared/exclusive application access and provides transaction primitives for sequences of card commands. Actor: resource manager. This architectural explanation supplies no C# session-lifetime guarantee.
- §3.2.4, printed page 16, `SCARD_READERSTATE.CurrentState`: `SCARD_STATE_UNAWARE` has all bits zero and causes an **immediate return** from transition monitoring. `SCARD_READER_STATE.Create` sets zero on every listener iteration. The one-second timeout is therefore a maximum wait parameter, rather than a minimum quiet-loop delay.
- §3.2.4, printed pages 18–19: GetStatusChange blocks for a change or timeout, with zero meaning immediate; SCARDTRACK Cancel terminates blocked requests in that object context and is limited there to GetStatusChange.
- §3.2.5.2, printed pages 20–23: Connect returns error when requested sharing is unavailable; Disconnect takes a **desired** disposition; BeginTransaction blocks competing applications, while EndTransaction performs its disposition **if possible**. The EndTransaction paragraph says power-down/eject succeeds only with exclusive access. SCARDCOMM Cancel is described for a pending BeginTransaction. These object-interface passages are distinct from present OS C APIs.

## Microsoft Windows API reference

Retained original `MicrosoftDocs/sdk-api` Markdown pages describe WinSCard calls; each file retains its metadata and exact byte hash. They are an access-date snapshot of the `docs` branch, rather than a claim about a specific future Windows build.

- `SCardConnectA`, `dwShareMode`: willingness to share is a resource-manager application-access policy. Success/failure is returned as status. It supplies no exclusion over HID or raw custom transports.
- `SCardDisconnect`, `dwDisposition`: `SCARD_UNPOWER_CARD` requests power down. `SCardReleaseContext` closes the context and frees context resources. Native return statuses, SDK proof rules and observed physical state are separate evidence.
- `SCardGetStatusChangeA`, description and `dwTimeout`: comparisons use caller-provided `dwCurrentState`; zero timeout returns immediately. The PnP special reader is initialized with zero fields. The SDK currently recreates ordinary reader states too, losing the prior observed state.
- `SCardCancel`, description: cancellation is context-specific and applies to actions waiting for external card/user action, especially GetStatusChange. The portable connection owner never uses this call to detach a transmit; the listener uses a separate context.
- `SCardBeginTransaction`, remarks: Windows can reset a transaction held idle for more than five seconds. This is a Windows API constraint, rather than a portable SDK timeout or an atomicity promise for every public multi-command method.
- `SCardEndTransaction`, returns: current documented `SCARD_W_RESET_CARD` means the transaction was released but communication requires reconnect; older listed Windows versions differ. The SDK conservatively closes ordinary admission on every non-success and releases only after checked disconnect/context cleanup.

## pcsc-lite API reference

`https://pcsclite.apdu.fr/api/group__API.html`, retained HTML SHA-256 `5d18b385a1d839fcbd7d71a8b9a549cdf6da90b383b8625b673cc69ee565c334`. The page identifies Doxygen 1.15.0 but supplies no library release number; treat it as an access-date reference and ask B2 to bind consequential behavior to the deployed pcsc-lite source/version.

- SCardCancel explicitly cancels a specific GetStatusChange with the same context. This narrower description prevents promoting Windows' wording to a portable native-abort guarantee.
- GetStatusChange lists zero `SCARD_STATE_UNAWARE` as immediate return.
- EstablishContext says each application thread shall use its own context, with the cancellation exception. The SDK's single connection worker and separate listener context give useful ownership separation; native transfer/concurrency details require the platform stage.
- Disconnect describes `SCARD_UNPOWER_CARD` as power down/cold reset; API status still needs checking. No physical PIN-state result was collected.

## .NET 10 lifetime implementation

Context7 `/dotnet/docs` was queried for SafeHandle release and wait-only cancellation orientation. Its results included .NET Framework reliability/MDA material; those historical guarantees are **not** used as modern process-shutdown promises. Consequential implementation is pinned to `dotnet/runtime` **v10.0.0**:

- `SafeHandle.cs`, `Dispose`, `SetHandleAsInvalid`, `InternalRelease`: reference counting defers release until the last reference; setting invalid suppresses finalization; `InternalRelease` invokes `ReleaseHandle` and discards its Boolean return. `IsClosed` alone establishes managed closure, rather than successful PC/SC release. The portable owner instead explicitly checks native statuses before invalidating/disposal and releasing its registration.
- `Task.cs`, `WaitAsync` and `CancellationPromise`: timeout/cancellation completes the promise and removes its continuation from the original task. It leaves original work running. Applicability: discovery wait budgets and external callers layering WaitAsync over a raw operation.
- `Thread.cs`, `Start(..., captureContext)` captures `ExecutionContext` for normal Start. `PcscConnectionNativeState` uses normal Start without suppressing flow. The resulting ambient-state retention is a source-backed question for the maintainer; this audit supplies no secret-retention measurement.
- Context7's EventWaitHandle/WaitHandle orientation identifies SafeWaitHandle as the synchronization-object wrapper and recommends disposal after use. Pinned `SafeWaitHandle.cs` v10.0.0 delegates ownership to SafeHandle; `WaitHandle.cs:87–98,107–205` closes that wrapper on Dispose and retains a DangerousAddRef through WaitOne's finally. These framework mechanics preserve a native borrow; they do not make arbitrary Set/Dispose races an application shutdown protocol. The PC/SC owner separately serializes SignalWorker with its terminated state under `_sync`, and its worker retires the event after leaving the loop. Its start-failure path marks termination before event disposal. Later platform owners must prove their own setter/retirement ordering.

## NIST SP 800-73-5 Part 2 citation

Original: *Interfaces for Personal Identity Verification, Part 2 — PIV Card Application Card Command Interface*, July 2024 final, §2.4.2. Original URL: `https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-73pt2-5.pdf`; publication record: `https://csrc.nist.gov/pubs/sp/800/73/pt2/5/final`.

The initial direct PDF request returned HTTP 403. One focused follow-up through Exa returned original-PDF passage text and the NIST final-publication record. The consequential passage is retained in `nist-security-status-excerpt.md`. No local PDF identity is claimed. Independent review should obtain the original PDF before accepting a revised normative citation.

§2.4.2 requires successful/failed authentication to update security status, defines application status as becoming FALSE when selection changes to another application, and identifies the PIV PIN as application status. It defines Global PIN status separately. Actor: PIV card application/authenticable entity, normative SHALL statements. It gives no PC/SC per-handle or per-process isolation rule and no YubiKey-specific PIN=ONCE policy definition. Consequently, the current “any handle … until the card is reset” inference exceeds this clause and overlooks deselection. Physical cross-process PIV qualification remains open under the original inherited IDs.

## Scope and research limits

ISO 7816 logical-channel and application-selection details belong to B5/B6 and the PIV boundary. The B1 correction rests on the original cited NIST passage rather than an invented ISO clause. macOS framework implementation, Windows native concurrency changes and Linux ABI/release details remain their later packet owners' work. Python implementation comparisons reuse C's forthcoming immutable source register; B1 creates no competing checkout or speculative session-history citation. Existing comparisons in test comments are editorial candidates until that exact version is identified.
