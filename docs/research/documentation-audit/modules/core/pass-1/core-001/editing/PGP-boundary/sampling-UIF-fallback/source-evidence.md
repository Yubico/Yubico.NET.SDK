# UIF fallback — source evidence and limits

## Actual caller path

The accepted-before `OpenPgpSession.cs` method is at lines 327–377. Its catch sequence is:

1. `catch (OperationCanceledException)` rethrows at 359–362.
2. The other-error catch calls `_logger.LogDebug(...)` at 366.
3. Only after that call returns does it assign `PolicyMayRequire` at 367.
4. No local catch protects diagnostic logging. An exception there exits context acquisition
   before the crypto caller reaches its private-key APDU.

The declaration-order evidence is the implementation, not the old comment's advisory intent.
`OpenPgpSession.Crypto.cs` awaits context acquisition at lines 42/80/124 before transmits at
50/88/132 for Sign/Decrypt/Authenticate. This bounded read establishes that logging failure
can prevent the operation; it does not assert that every configuration/provider throws.

The session copies its inherited logger into `_logger` in its constructor. Core
`ApplicationSession.cs:109` obtains that logger through `YubiKitLogging.CreateLogger`.
`YubiKitLogging.cs:24–47` permits an application-supplied logger factory and otherwise defaults
to `NullLoggerFactory`. There is no universal exception-swallowing wrapper at that seam.
Filtering or no-op logging can complete normally, preserving the intended advisory fallback.

## Logging contract / exact implementation

The isolated checkout's `Directory.Packages.props:12–14` requests
Microsoft.Extensions.Logging/Abstractions/Console **10.0.2**. The following retained sources
are from the .NET runtime **v10.0.2** tag; URLs, SHA-256 and sizes are in source-manifest.json.
These are implementation authorities for the inspected library version, not normative protocol
requirements, proof of loaded binaries or a runtime experiment.

- `sources/LoggerExtensions-v10.0.2.cs:64–67` forwards the exception-bearing `LogDebug`
  overload to `Log`; the final extension call at 484–486 invokes `ILogger.Log` directly.
  These forwarding methods supply no exception suppression.
- `sources/Logger-v10.0.2.cs:27–64` checks configured message loggers, invokes enabled
  providers synchronously, collects exceptions from provider `Log` calls and calls
  `ThrowLoggingError`. Lines 155–159 throw an `AggregateException` containing those errors.
  Thus even the standard factory does not guarantee a nonthrowing diagnostic call.
  A custom factory/logger is separately application-controlled.

Bounded Context7 lookup: `/dotnet/docs`, queried during this focused correction for custom
Microsoft.Extensions.Logging provider synchronization/exception behavior. It returned
`docs/core/extensions/logging/providers.md`, documenting synchronous `ILogger.Log`, and a
custom-provider example. That current-main documentation is orientation with no immutable
revision returned; the v10.0.2 source supplies the exception-propagation evidence. A separate
Microsoft Testing Platform logger result uses a different contract and was not relied on.
The first library-resolution query returned a different extensions collection and was not used;
the subsequent .NET resolution supplied `/dotnet/docs`. No broader lookup or experiment followed.

## Wording consequence and handoff

Keep UIF advisory and retain the normal fallback path, but qualify it on diagnostic logging
success. Preserve the explicit cancellation rethrow. The two edited passages now state those
conditions without changing logging, fallback or cryptographic behavior.

The source-supported miss is mapped to SAMPLE-PGP-001 and its two exact sampled units. Existing
logging-related engineering/qualification ownership remains with the owner/sampler; this focused
documentation correction creates no new unique Core defect, exception policy or test obligation.
PIN/KDF identity, X25519/ATT defects, PIVIMP-005, native/provider/SCP qualifications, PIV physical
deferrals and the unready future Windows handoff remain unchanged.
