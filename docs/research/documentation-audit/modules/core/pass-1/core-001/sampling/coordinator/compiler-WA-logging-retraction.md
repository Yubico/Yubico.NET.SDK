# Compiler finding supersedes the sampled WebAuthn logging verdict

**SAMPLE-WA-002 / FULL-032:** retract clean-content credit for original unit
`core-001:unit:4886f819fa316d28` (`src/WebAuthn/CLAUDE.md`, Logging). The earlier manual read
recognized the intended logging policy but missed its assembly-access restriction.

`examples-full-webauthn-first` emitted CS1929 for the example's call to
`YubiKitLogging.CreateLogger<PreviewSignProcessor>()`. Astra checked the actual Core declarations:
`CreateLogger<T>` is internal, and Core's explicit friend assemblies include neither WebAuthn nor
its integration-test assembly. A genuine WebAuthn context cannot compile that call.

The FIDO editor is correcting the guide to use the public `YubiKitLogging.LoggerFactory` with the
real Microsoft.Extensions.Logging extension. No new friend assembly, helper shim or API change is
authorized. Preserve the failed compile and earlier verdict as history; this unit now receives zero
clean credit. The changed body must pass independent review and actual compilation.

Expand the WebAuthn narrative category by its existing original quota and replace the lost credit,
without reducing the broad denominator or borrowing another category's surplus. This is a documentation
API-access error, not an observed runtime logging failure or a new unique Core implementation bug.
