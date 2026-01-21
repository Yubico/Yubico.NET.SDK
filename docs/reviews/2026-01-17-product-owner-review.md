# Notes on the SDK.
Date: 2026-01-17

## Ralph Loop Iteration
The Ralph loop has been iterated upon implementing the Fido Session.
The architecture looks good. Im sure there will be bugs but overall looks good. I'm very happy with the learning output from the Ralph loop.

It reasons about the AI output within each iteration, and is able to self-correct and improve the output in subsequent iterations. This is a great feature of the Ralph loop.

The agent in the loop seemed to want to test the FidoSession, which started off as a sealed class, as was the intention, however when the agent wanted to test it, instead of making it unsealed temporarily to allow testing, it made a roundabout way to test it via an other, new interface. It worked, but it was a bit roundabout and would have been simpler to just make it unsealed for testing, and then sealed again afterwards when the entire implementation was done. It could have also added the intended interface to the sealed class, which should have also worked (depending on the intended use case and expected API features).

I think we also couldve been explicit when to end a Ralph loop iteration. The agent did multiple phases of the PRD before ending the iteration. It would have been better to end the iteration after the first PRD phase, and then start a new iteration for the next phase. This would have made it clearer when an iteration ends and a new one begins.

Overall, the Ralph loop is a great architecture for iterative development with AI assistance. It allows for self-correction and improvement over multiple iterations, leading to better outcomes.
### Ralph Loop Session Summary

==========================
**Session Stats:**
- Iterations: 9
- Duration: 3.8 hours (~25 min/iteration average)
- Outcome: COMPLETED - Full FIDO2/CTAP2 implementation delivered

**What Was Built:**
- 51 source files implementing complete FIDO2/CTAP2 protocol stack
- 38 test files with 265+ unit tests
- 13 phases completed covering: session foundation, PIN/UV auth protocols (V1+V2), MakeCredential/GetAssertion, credential management, all WebAuthn extensions (hmac-secret, credProtect, credBlob, largeBlob, minPinLength, PRF), bio enrollment, config commands, large blob storage, YK 5.7/5.8 encrypted metadata decryption, integration tests, DI setup, and documentation

## Fido Session Implementation Details
As previously stated, the agents strategy around testing looked like this:

Interface Extraction for Testability
When sealed classes blocked mocking, agent created interfaces:
```csharp
// Before: Can't mock FidoSession
public class FingerprintBioEnrollment(FidoSession session)

// After: Mockable via interface
public class FingerprintBioEnrollment(IBioEnrollmentCommands commands)
```

This allowed testing without changing the sealed nature of the original class. However, it added complexity by introducing additional interfaces and indirection.

The agent also forgot to remove these interfaces and patterns later, in favor of the original sealed classes with the IFidoSession interface, which would have been simpler overall.
The agent should reuse the IFidoSession interface throughout the codebase instead of using the implementation classes directly.

## New classes
New FidHidBackend : IFidoBackend was created. 
The naming could be confused with the ManagementSessions FidoBackend(IFidoHidProtocol hidProtocol) : IManagementBackend class. We should consider renaming these to be more distinct.

### FidoSession
It serves as the transport session for FIDO2/CTAP2 commands, encapsulating the protocol logic and command handling. It uses composition to include various command classes for different CTAP2 functionalities, such as PIN/UV auth, MakeCredential, GetAssertion, credential management, and extensions. This keeps the FidoSession class focused on session management while delegating specific command logic to dedicated classes. However, certain logic, e.g. MakeCrendentialAsync and GetAssertionAsync, is still implemented directly in FidoSession. We should consider refactoring this logic into separate command classes to further improve separation of concerns and maintainability, or at least have a criteria for what logic goes into FidoSession vs command classes. 

I noticed that it is implementing IAsyncDisposable, on top of inheriting our common IApplicationSession, which currently does not implement IAsyncDisposable. We should consider whether IApplicationSession should also implement IAsyncDisposable, to ensure consistency across session types.

MUST FIX: Instantiating FidoSession with a SmartCardConnection causes runtime errors. Currently only the HidFidoConnection seems to work. Failing tests: 
- CreateFidoSession_With_SmartCard_CreateAsync (fails at SelectAsync(): Yubico.YubiKit.Core.SmartCard.ApduException: SELECT command failed: File or application not found (SW=0x6A82)
)
- CreateFidoSession_With_FactoryInstance (fails at: same SelectAsync() call)

Note: All other FidoSession integration tests pass - great job on those! 

## IYubiKeyExtensions
This pattern is used for the ManagementSession and SecurityDomainSession as well. It is intended to provide a simple API for users. However, with ManagementSession and SecurityDomainSession, we accept ScpKeyParameters, which only work if the underlying connection is a smart card connection. We should review these IYubiKeyExtensions methods to ensure they validate the connection type before accepting ScpKeyParameters, to prevent runtime errors.

## WebAuthn/CtapExtensions
Missing extensions that I expected to see here:
- CredProtectExtensions
- HmacSecretExtensions (but perhaps the agent named it PrfExtensions, which indeed a class that was created), also. It is not clear how these extensions are intended to be used. I'm not sure if Hmac-Secret-Mc is implemented. 
- ThirdPartyPaymentsExtensions
- SignExtensions
If these extensions are not implemented, we need to implment them in the next run.
If they are implemented, I need a better understanding of how they are intended to be used.

## CtapRequestBuilder
This is a nice utility class for building CTAP requests. It simplifies the process of constructing requests with the correct structure and encoding using fluent methods.
However, it is not used consistently throughout the codebase. Some places still manually build requests without using this builder. We should refactor the codebase to use CtapRequestBuilder everywhere for consistency and maintainability.

## CredentialManagementModels
The CredentialManagementModels class contains data models for credential management operations. Each of the models has methods for deserialization from CBOR format. Each model corresponds to a specific credential management command or response. They use the CborReader with a loop, reading tags and values to populate the model properties. This is a good pattern for handling CBOR data in a structured way. But I wonder if we could refactor the deserialization logic to reduce duplication across models. Many of them have similar patterns for reading CBOR data. We could extract common logic into helper methods or base classes to improve maintainability. The only thing that differs between models is the specific tags and properties being read, which could be parameterized in shared methods. Perhaps by a new static class, since we have we CtapRequestBuilder for building requests, we could have a CtapResponseParser for parsing responses, having common methods for reading CBOR data into models, and each model could just specify the CborConformanceMode, tags and properties it needs.

## Testing
The Fido tests need to be implementing our existing TestInfrastructure, following both patterns from SecurityDomainSessionTests and ManagementSessionTests, using [WithYubiKey()] attributes.

## Misses
There is some dead code. E.g. public int? GetKeyType() in AttestedCredentialData is never used. We should either write tests that use them, if they are intended to be used by the public space, or remove unused code to keep the codebase clean. This class could also reuse the CtapRequestParser idea mentioned above for its deserialization logic.

## Overall
Not related to the refactor, but the SDK should decide:
1. What object setter/getter syntax to use? Get, init, private set, public set? How to provide validation? Should we validate on setting properties, or have separate validation methods?
2. Logging: Since we have a static, default, settable LoggingFactory, no class should ever have to accept an ILogger or ILoggerFactory in its constructor. Instead, each class should get its logger from the LoggingFactory. This will simplify constructors and ensure consistent logging across the codebase.

The conclusion of these points will help ensure consistency and maintainability as we continue to build out the SDK and should be noted in the CLAUDE.md and/or CONTRIBUTING.md files and/or DEV-GUIDE.md files. Perhaps the DEV-GUIDE.md should be absorbed into CONTRIBUTING.md to reduce the number of docs we have to maintain, preferring a standard CONTRIBUTING.md file for all contribution guidelines.

