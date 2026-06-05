# Swift WebAuthn Client Implementation Exploration

## 1. WebAuthn Client Module Location & File Tree

**Base Directory:** `/Users/Dennis.Dyall/Code/y/yubikit-swift/YubiKit/YubiKit/FIDO/WebAuthn/`

### Client Module File Structure
```
WebAuthn/
├── Client/
│   ├── Client.swift                          # Main Client actor
│   ├── ClientData.swift                      # Client data JSON construction & hashing
│   ├── ClientError.swift                     # Error types
│   ├── Origin.swift                          # Origin validation
│   ├── Registration/
│   │   ├── RegistrationOptions.swift         # WebAuthn.Registration.Options
│   │   ├── RegistrationResponse.swift        # WebAuthn.Registration.Response
│   │   └── Client+MakeCredential.swift       # makeCredential public API + implementation
│   ├── Authentication/
│   │   ├── AuthenticationOptions.swift       # WebAuthn.Authentication.Options
│   │   ├── AuthenticationResponse.swift      # MatchedCredential & Response types
│   │   └── Client+GetAssertion.swift         # getAssertion public API + implementation
│   ├── Shared/
│   │   ├── Client+UserVerification.swift     # PIN/UV token acquisition
│   │   ├── Client+CredentialMatching.swift   # Exclude/allow list matching logic
│   │   ├── Client+Validation.swift           # RP ID validation
│   │   └── Backend+Extensions.swift          # Extension input/output processing
│   └── Backends/
│       └── CTAP2Backend.swift                # Backend protocol definition
├── Extensions/
│   ├── WebAuthnPreviewSign.swift             # PreviewSign extension types
│   ├── WebAuthnCredBlob.swift                # CredBlob extension
│   ├── WebAuthnCredProtect.swift             # CredProtect extension
│   ├── WebAuthnLargeBlob.swift               # LargeBlob extension
│   ├── WebAuthnMinPinLength.swift            # MinPinLength extension
│   ├── WebAuthnCredProps.swift               # CredProps extension
│   ├── WebAuthnPRF.swift                     # PRF (hmac-secret) extension
│   ├── ExtensionInputs.swift                 # RegistrationInputs & AuthenticationInputs
│   ├── ExtensionOutputs.swift                # RegistrationOutputs & AuthenticationOutputs
│   └── Extensions+JSON.swift                 # JSON serialization for extensions
├── Attestation/
│   ├── AttestationObject.swift               # Attestation object structure
│   └── Attestation.swift                     # Attestation format & statement types
├── AuthenticatorData.swift                   # Authenticator data parsing
├── WebAuthn.swift                            # Core types, StatusStream, preferences
├── WebAuthn+JSON.swift                       # JSON serialization
└── WebAuthn+CBOR.swift                       # CBOR serialization

Supporting layers:
├── ../CBOR/
│   ├── CBOR.swift
│   ├── CBOR+Encode.swift
│   └── CBOR+Decode.swift
├── ../COSE/
│   └── COSE.swift                            # Algorithm & Key types
├── ../Session/
│   └── CTAP2.Session                         # Underlying session backend
└── ../Interfaces/
    └── CBORInterface.swift                   # Low-level APDU/CBOR bridge
```

---

## 2. Public API Surface

### Client Initialization
**File:** `Client.swift:49-96`

```swift
public actor Client {
    // Primary init - backed by CTAP2.Session
    public init(
        session: CTAP2.Session,
        origin: Origin,
        enterpriseRpIds: Set<String> = [],
        isPublicSuffix: @escaping PublicSuffixChecker
    )
}

public typealias PublicSuffixChecker = @Sendable (String) -> Bool
```

### Registration (makeCredential)
**Files:** `Registration/Client+MakeCredential.swift:19-61`, `Registration/RegistrationOptions.swift`

```swift
public func makeCredential(
    _ options: WebAuthn.Registration.Options
) async -> WebAuthn.StatusStream<WebAuthn.Registration.Response>

public func makeCredential(
    _ options: WebAuthn.Registration.Options,
    clientData: WebAuthn.ClientData
) async -> WebAuthn.StatusStream<WebAuthn.Registration.Response>

// Registration.Options fields (line 30-65)
public struct Options: Sendable {
    public let challenge: Data                           // Random bytes from RP
    public let rp: WebAuthn.RelyingParty                 // {id: String, name: String?}
    public let user: WebAuthn.User                       // {id, name, displayName}
    public let excludeCredentials: [WebAuthn.CredentialDescriptor]
    public let residentKey: WebAuthn.ResidentKeyPreference  // required|preferred|discouraged
    public let userVerification: WebAuthn.UserVerificationPreference
    public let attestation: WebAuthn.AttestationPreference  // none|indirect|direct|enterprise
    public let pubKeyCredParams: [COSE.Algorithm]        // [.es256, .edDSA, .rs256, ...]
    public let timeout: Duration?
    public let extensions: WebAuthn.Extension.RegistrationInputs?
}

// Registration.Response (RegistrationResponse.swift:22-47)
public struct Response: Sendable {
    public let credentialId: Data
    public let rawAttestationObject: Data               // CBOR-encoded
    public let rawAuthenticatorData: Data
    public let attestationStatement: WebAuthn.AttestationStatement
    public let transports: [WebAuthn.Transport]
    public let clientExtensionResults: WebAuthn.Extension.RegistrationOutputs
    public let publicKey: COSE.Key
    public let aaguid: WebAuthn.AAGUID
    public let signCount: UInt32
}
```

### Authentication (getAssertion)
**Files:** `Authentication/Client+GetAssertion.swift:31-69`, `Authentication/AuthenticationOptions.swift`

```swift
public func getAssertion(
    _ options: WebAuthn.Authentication.Options
) async -> WebAuthn.StatusStream<[WebAuthn.Authentication.MatchedCredential]>

public func getAssertion(
    _ options: WebAuthn.Authentication.Options,
    clientData: WebAuthn.ClientData
) async -> WebAuthn.StatusStream<[WebAuthn.Authentication.MatchedCredential]>

// Authentication.Options (AuthenticationOptions.swift:30-44)
public struct Options: Sendable {
    public let challenge: Data
    public let rpId: String?
    public let allowCredentials: [WebAuthn.CredentialDescriptor]
    public let userVerification: WebAuthn.UserVerificationPreference
    public let timeout: Duration?
    public let extensions: WebAuthn.Extension.AuthenticationInputs?
}

// MatchedCredential (AuthenticationResponse.swift:26-37)
public struct MatchedCredential: Sendable {
    public let id: Data
    public let user: WebAuthn.User?
    public let select: @Sendable () async throws(WebAuthn.ClientError) -> Response
}

// Authentication.Response (AuthenticationResponse.swift:47-65)
public struct Response: Sendable {
    public let credentialId: Data
    public let rawAuthenticatorData: Data
    public let signature: Data
    public let user: WebAuthn.User?
    public let clientExtensionResults: WebAuthn.Extension.AuthenticationOutputs
    public let signCount: UInt32
}
```

### Status Stream (StatusStream pattern)
**File:** `WebAuthn.swift:34-182`

```swift
public struct StatusStream<R: Sendable>: AsyncSequence {
    // Emitted states during operations
    public enum Status<Response> {
        case processing
        case waitingForUser(cancel: @Sendable () async -> Void)
        case requestingUV(useUV: @Sendable (Bool) -> Void)
        case requestingPIN(submitPIN: @Sendable (String?) -> Void)
        case finished(Response)
    }
    
    // Convenience accessors
    public func value() async throws(ClientError) -> R
    public func value(pin: String, useUV: Bool = true) async throws(ClientError) -> R
    
    // AsyncSequence conformance
    public func makeAsyncIterator() -> Iterator
}

// Example iteration:
for try await status in client.makeCredential(options) {
    switch status {
    case .processing: showSpinner()
    case .waitingForUser(let cancel): showTouchPrompt()
    case .requestingUV(let useUV): useUV(true)
    case .requestingPIN(let submitPIN): submitPIN(await askForPIN())
    case .finished(let response): return response
    }
}
```

### Core Data Model Types
**File:** `WebAuthn.swift:184-286`

```swift
public struct RelyingParty: Sendable {
    public let id: String                      // RP ID, e.g., "example.com"
    public let name: String?
}

public struct User: Sendable {
    public let id: Data                        // Opaque user handle (bytes)
    public let name: String?                   // e.g., "alice@example.com"
    public let displayName: String?            // e.g., "Alice Smith"
}

public struct CredentialDescriptor: Sendable, Hashable {
    public let type: String                    // Always "public-key" for FIDO2
    public let id: Data                        // Credential ID
    public let transports: Set<Transport>?     // Hint: usb|nfc|ble|smart-card|hybrid|internal
}

public enum ResidentKeyPreference: String {
    case required      // Must be discoverable
    case preferred     // Discoverable if possible
    case discouraged   // Non-discoverable preferred
}

public enum UserVerificationPreference: String {
    case required      // PIN or biometric mandatory
    case preferred     // Use if available
    case discouraged   // Skip if possible
}

public enum AttestationPreference: String {
    case none          // No attestation
    case indirect      // Client may replace direct attestation
    case direct        // Return unmodified attestation
    case enterprise    // Platform/vendor-facilitated enterprise mode
}

public enum Transport: Sendable, Hashable {
    case usb
    case nfc
    case ble
    case smartCard
    case hybrid
    case `internal`
    case unknown(String)
}
```

---

## 3. Internal Architecture: Session/Connection Abstraction

### Backend Protocol (abstracting CTAP2.Session)
**File:** `Backends/CTAP2Backend.swift:25-93`

```swift
protocol Backend: Actor {
    // Info
    var cachedInfo: CTAP2.GetInfo.ImmutableView { get async throws(CTAP2.SessionError) }
    func getInfo() async throws(CTAP2.SessionError) -> CTAP2.GetInfo.Response
    
    // PIN/UV
    func getUVRetries() async throws(CTAP2.SessionError) -> Int
    func getPinRetries() async throws(CTAP2.SessionError) -> CTAP2.ClientPin.GetRetries.Response
    func getPinUVToken(
        using method: CTAP2.ClientPin.Method,
        permissions: CTAP2.ClientPin.Permission,
        rpId: String?
    ) async throws(CTAP2.SessionError) -> CTAP2.Token
    
    // Core CTAP2 operations
    func makeCredential(
        parameters: CTAP2.MakeCredential.Parameters,
        token: CTAP2.Token?
    ) async -> CTAP2.StatusStream<CTAP2.MakeCredential.Response>
    
    func getAssertion(
        parameters: CTAP2.GetAssertion.Parameters,
        token: CTAP2.Token?
    ) async -> CTAP2.StatusStream<CTAP2.GetAssertion.Response>
    
    func getNextAssertion() async -> CTAP2.StatusStream<CTAP2.GetAssertion.Response>
}
```

### Flow: WebAuthn → CTAP2 Bridging
**File:** `Client+MakeCredential.swift:69-203`

1. **Validate** RP ID against origin (lines 44-46)
2. **Build client data JSON** with proper key ordering (lines 30-35)
3. **Hash challenge** (SHA-256) to get `clientDataHash` (ClientData.swift:64)
4. **Retry loop** for PIN/UV errors (lines 98-203)
5. **Acquire auth token** via PIN/UV (lines 107-116)
6. **Build extensions** from WebAuthn inputs → CTAP2 inputs (lines 126-130)
7. **Create CTAP2.MakeCredential.Parameters** from WebAuthn options (lines 132-142)
8. **Send to backend** (makeCredential), forward status updates (lines 146-160)
9. **Parse response** and convert back to WebAuthn types (lines 173-202)

### Extension Processing Bridge
**File:** `Shared/Backend+Extensions.swift:23-231`

```swift
// Input processing for makeCredential
func buildMakeCredentialExtensions(
    _ inputs: WebAuthn.Extension.RegistrationInputs?,
    userVerification: WebAuthn.UserVerificationPreference
) async throws(WebAuthn.ClientError) -> (
    ctapInputs: [CTAP2.Extension.MakeCredential.Input],
    prf: WebAuthn.Extension.PRF?,
    previewSign: CTAP2.Extension.PreviewSign?,
    largeBlobRequested: Bool
)

// Output processing for makeCredential response
func parseRegistrationOutputs(
    from response: CTAP2.MakeCredential.Response,
    prf: WebAuthn.Extension.PRF?,
    previewSign: CTAP2.Extension.PreviewSign?,
    largeBlobRequested: Bool,
    credPropsRk: Bool?
) throws(WebAuthn.ClientError) -> WebAuthn.Extension.RegistrationOutputs
```

---

## 4. Data Model Types (Complete Enumeration)

### Attestation Types
**File:** `Attestation/Attestation.swift`

```swift
public enum AttestationFormat: Sendable, Hashable {
    case packed           // FIDO2 standard format
    case tpm              // TPM format
    case androidKey
    case androidSafetynet
    case fidoU2F
    case apple
    case none
    case unknown(String)
}

public enum AttestationStatement: Sendable {
    case packed(Packed)                    // sig, alg, x5c?, ecdaaKeyId?
    case fidoU2F(FIDOU2F)                  // sig, x5c
    case apple(Apple)                      // x5c
    case none
    case unknown(format: String)
}

// Packed format details
public struct Packed: Sendable {
    public let sig: Data                   // Attestation signature
    public let alg: Int                    // COSE algorithm ID
    public let x5c: [Data]?                // Cert chain (optional)
    public let ecdaaKeyId: Data?           // ECDAA issuer key (rare)
}
```

### Authenticator Data
**File:** `AuthenticatorData.swift:25-140`

```swift
public struct AuthenticatorData: Sendable {
    public let rawData: Data               // Full parsed structure
    public let rpIdHash: Data              // SHA-256(rpId) [32 bytes]
    public let flags: Flags                // UP, UV, BE, BS, AT, ED
    public let signCount: UInt32           // 32-bit big-endian counter
    public let attestedCredentialData: AttestedCredentialData?
    // internal:
    internal let extensions: [Extension.Identifier: CBOR.Value]?
}

public struct Flags: OptionSet {
    public static let userPresent = Flags(rawValue: 1 << 0)
    public static let userVerified = Flags(rawValue: 1 << 2)
    public static let backupEligibility = Flags(rawValue: 1 << 3)
    public static let backupState = Flags(rawValue: 1 << 4)
    public static let attestedCredentialData = Flags(rawValue: 1 << 6)
    public static let extensionData = Flags(rawValue: 1 << 7)
}

public struct AttestedCredentialData: Sendable {
    public let aaguid: AAGUID              // 128-bit unique ID
    public let credentialId: Data          // Variable-length credential ID
    public let credentialPublicKey: COSE.Key
}
```

### COSE Key Types
**File:** `../COSE/COSE.swift:97-160`

```swift
public enum Key: Sendable, Equatable {
    case ec2(
        alg: Algorithm,
        kid: Data?,
        crv: Int,      // 1: P-256, 2: P-384
        x: Data,
        y: Data
    )
    
    case okp(
        alg: Algorithm,
        kid: Data?,
        crv: Int,      // 6: Ed25519, 4: X25519
        x: Data
    )
    
    case rsa(
        alg: Algorithm,
        kid: Data?,
        n: Data,       // Modulus
        e: Data        // Public exponent
    )
    
    case other(Unsupported)
}

public enum Algorithm: Sendable, Equatable {
    case es256                              // -7: ECDSA P-256
    case edDSA                              // -8: Ed25519
    case esp256                             // -9: ECDSA P-256 pre-hashed
    case es384                              // -35: ECDSA P-384
    case rs256                              // -257: RSA PKCS#1 v1.5
    case esp256SplitARKGPlaceholder         // -65539: previewSign
    case other(Int)
    
    public var rawValue: Int { /* maps above */ }
}
```

### Client Data
**File:** `Client/ClientData.swift:25-101`

```swift
public struct ClientData: Sendable {
    // internal:
    internal let clientDataJSON: Data?     // Full JSON for webauthn flow (nil for hash-only)
    internal let clientDataHash: Data      // SHA-256 hash [32 bytes]
    internal let origin: Origin
    internal let rpId: String
}

// Factory methods (lines 56-80)
public static func webauthn(
    type: String,                          // "webauthn.create" or "webauthn.get"
    challenge: Data,
    origin: WebAuthn.Origin,
    rpId: String,
    crossOrigin: Bool? = nil
) -> WebAuthn.ClientData

public static func hash(
    _ hash: Data,                          // Pre-computed SHA-256
    origin: WebAuthn.Origin,
    rpId: String
) -> WebAuthn.ClientData

// JSON structure (lines 87-101):
// {"type": "...", "challenge": "...", "origin": "...", "crossOrigin": true|false}
```

---

## 5. Extensions Architecture

### Extension Inputs Structure
**File:** `Extensions/ExtensionInputs.swift`

```swift
public struct RegistrationInputs: Sendable, Equatable {
    public let prf: PRF.Registration.Input?
    public let credProtect: CredProtect.Registration.Input?
    public let credBlob: CredBlob.Registration.Input?
    public let minPinLength: MinPinLength.Registration.Input?
    public let largeBlob: LargeBlob.Registration.Input?
    public let credProps: CredProps.Registration.Input?
    public let previewSign: PreviewSign.Registration.Input?
}

public struct AuthenticationInputs: Sendable, Equatable {
    public let prf: PRF.Authentication.Input?
    public let getCredBlob: CredBlob.Authentication.Input?
    public let largeBlob: LargeBlob.Authentication.Input?
    public let previewSign: PreviewSign.Authentication.Input?
}
```

### Extension Outputs Structure
**File:** `Extensions/ExtensionOutputs.swift`

```swift
public struct RegistrationOutputs: Sendable, Equatable {
    public let prf: PRF.Registration.Output?
    public let credProtect: CredProtect.Registration.Output?
    public let credBlob: CredBlob.Registration.Output?
    public let minPinLength: MinPinLength.Registration.Output?
    public let largeBlob: LargeBlob.Registration.Output?
    public let credProps: CredProps.Registration.Output?
    public let previewSign: PreviewSign.Registration.Output?
}

public struct AuthenticationOutputs: Sendable, Equatable {
    public let prf: PRF.Authentication.Output?
    public let credBlob: CredBlob.Authentication.Output?
    public let largeBlob: LargeBlob.Authentication.Output?
    public let previewSign: PreviewSign.Authentication.Output?
}
```

### Extension Dispatch Logic
**File:** `Shared/Backend+Extensions.swift:23-111` (registration), **115-231** (authentication)

**Registration dispatch (lines 42-108):**
1. Check if input is nil → return early
2. Iterate through each extension (prf, credProtect, credBlob, largeBlob, previewSign, minPinLength)
3. Build CTAP2 inputs, collect references for post-processing
4. Return tuple: `(ctapInputs: [CTAP2.Extension.MakeCredential.Input], prf, previewSign, largeBlobRequested)`

**Authentication dispatch (lines 115-231):**
1. Similar pattern for getAssertion
2. PRF: validate evalByCredential against allowCredentials (lines 134-178)
3. CredBlob: request retrieval (lines 181-185)
4. LargeBlob: handle read/write, propagate write errors (lines 187-199)
5. PreviewSign: validate allowCredentials non-empty, map selected credential (lines 201-227)

---

## 6. PreviewSign Extension (Delegated Signing)

### Types Definition
**File:** `Extensions/WebAuthnPreviewSign.swift:19-116`

```swift
public enum PreviewSign {}

// Signing parameters
public struct SigningParams: Sendable, Equatable {
    public let keyHandle: Data              // From generated key output
    public let tbs: Data                    // "To be signed" data (typically hash)
    public let additionalArgs: Data?        // Optional CBOR-encoded args (e.g., ARKG derivation)
}

// Registration Input/Output
extension PreviewSign {
    public enum Registration {
        public struct Input: Sendable, Equatable {
            public let algorithms: [COSE.Algorithm]
            public static func generateKey(algorithms: [COSE.Algorithm]) -> Input
        }
        
        public struct Output: Sendable, Equatable {
            public let generatedKey: CTAP2.Extension.PreviewSign.GeneratedKey
        }
    }
}

// Authentication Input/Output
extension PreviewSign {
    public enum Authentication {
        public struct Input: Sendable, Equatable {
            public let signByCredential: [Data: SigningParams]  // credentialId → SigningParams
        }
        
        public struct Output: Sendable, Equatable {
            public let signature: Data
        }
    }
}
```

### PreviewSign in Extension Processing
**File:** `Shared/Backend+Extensions.swift`

**Registration (lines 83-94):**
```swift
if let previewSignInput = inputs.previewSign {
    if let ps = try? await makePreviewSign() {
        let flags: UInt8 = userVerification == .required ? 0b101 : 0b001
        ctapInputs.append(
            ps.makeCredential.input(
                algorithms: previewSignInput.algorithms,
                flags: flags
            )
        )
        previewSign = ps
    }
}
```

**Authentication (lines 201-227):**
```swift
if let previewSignInput = inputs.previewSign {
    if allowCredentials.isEmpty {
        throw .invalidRequest("sign requires allowCredentials", source: .here())
    }
    let allowedIds = Set(allowCredentials.map(\.id))
    guard allowedIds.isSubset(of: previewSignInput.signByCredential.keys) else {
        throw .invalidRequest("signByCredential not valid", source: .here())
    }
    
    if let ps = try? await makePreviewSign(), let selectedCredentialId {
        if let params = previewSignInput.signByCredential[selectedCredentialId] {
            ctapInputs.append(
                ps.getAssertion.input(
                    keyHandle: params.keyHandle,
                    tbs: params.tbs,
                    additionalArgs: params.additionalArgs
                )
            )
            previewSign = ps
        }
    }
}
```

**Output parsing (lines 254-263, 318-323):**
```swift
// Registration
var previewSignOutput: WebAuthn.Extension.PreviewSign.Registration.Output?
if let previewSign {
    do throws(CTAP2.SessionError) {
        if let generatedKey = try previewSign.makeCredential.output(from: response) {
            previewSignOutput = .init(generatedKey: generatedKey)
        }
    } catch {
        throw WebAuthn.ClientError(error)
    }
}

// Authentication
var previewSignOutput: WebAuthn.Extension.PreviewSign.Authentication.Output?
if let previewSign {
    if let signature = previewSign.getAssertion.output(from: response) {
        previewSignOutput = .init(signature: signature)
    }
}
```

### PreviewSign Tests
**File:** `FullStackTests/Tests/WebAuthn/Extensions/PreviewSignTests.swift:20-109`

```swift
@Suite("WebAuthn PreviewSign Extension Tests", .serialized)
struct WebAuthnPreviewSignExtensionTests {
    
    // Supported algorithms: esp256, esp256SplitARKGPlaceholder, es256
    private let generateKeyAlgorithms = [.esp256, .esp256SplitARKGPlaceholder, .es256]
    
    @Test("PreviewSign - GenerateKey")
    func testGenerateKey(discoverable: Bool) async throws {
        // 1. Create credential with previewSign.generateKey(algorithms:)
        let response = try await client.makeCredential(createOptions)
            .value(pin: defaultTestPin)
        
        // 2. Assert generatedKey output present
        let generatedKey = response.clientExtensionResults.previewSign?.generatedKey
        
        // 3. Validate fields
        // - keyHandle: non-empty Data
        // - publicKey: non-empty Data (COSE key)
        // - attestationObject: non-empty Data
        // - algorithm: one of the requested algorithms
    }
}
```

---

## 7. CBOR & Encoding Layer

### CBOR Implementation
**File:** `../CBOR/CBOR.swift` and encoding/decoding modules

```swift
public enum CBOR {
    enum MajorType: UInt8 {
        case unsignedInt = 0
        case negativeInt = 1
        case byteString = 2
        case textString = 3
        case array = 4
        case map = 5
        case simpleOrFloat = 7
    }
    
    enum SimpleValue: UInt8 {
        case `false` = 20
        case `true` = 21
        case null = 22
    }
    
    protocol Encodable {
        func cbor() -> CBOR.Value
    }
    
    protocol Decodable {
        init?(cbor: CBOR.Value)
    }
}
```

### ClientDataJSON Construction
**File:** `Client/ClientData.swift:87-101`

```swift
private static func buildJSON(
    type: String,
    challenge: Data,
    origin: WebAuthn.Origin,
    crossOrigin: Bool?
) -> Data {
    // Key ordering per WebAuthn spec: type, challenge, origin, crossOrigin
    var json = "{" + #""type":"# + type.asJSONString()
    json += #","challenge":"# + challenge.base64URLEncodedString().asJSONString()
    json += #","origin":"# + origin.stringValue.asJSONString()
    json += #","crossOrigin":"# + (crossOrigin == true ? "true" : "false")
    json += "}"
    return Data(json.utf8)
}

// Hashing
let hash = Crypto.Hash.sha256(json)
```

### Attestation Object Construction
**File:** `Attestation/AttestationObject.swift:43-56`

```swift
internal init(format: String, statementCBOR: CBOR.Value, authenticatorData: AuthenticatorData) {
    // Build CBOR map
    let map: [CBOR.Value: CBOR.Value] = [
        "fmt": format.cbor(),
        "attStmt": statementCBOR,
        "authData": authenticatorData.rawData.cbor(),
    ]
    
    // Encode to CBOR bytes
    self.rawData = map.cbor().encode()
}
```

---

## 8. Tests

### Test Location
- **Unit Tests:** `/Users/Dennis.Dyall/Code/y/yubikit-swift/YubiKit/UnitTests/FIDO/WebAuthn/`
- **Integration Tests:** `/Users/Dennis.Dyall/Code/y/yubikit-swift/FullStackTests/Tests/WebAuthn/`
- **PreviewSign Tests:** `/Users/Dennis.Dyall/Code/y/yubikit-swift/FullStackTests/Tests/WebAuthn/Extensions/PreviewSignTests.swift`

### Test Files Structure
```
YubiKit/UnitTests/FIDO/WebAuthn/
├── StatusStreamTests.swift
├── SerializationTests.swift
├── SerializationTests+JSON.swift
├── ResponseParsingTests.swift
├── OriginTests.swift
├── CredentialPreprocessingTests.swift
└── MockWebAuthnBackend.swift

FullStackTests/Tests/WebAuthn/
├── WebAuthnClientTests.swift
└── Extensions/
    ├── PreviewSignTests.swift
    ├── PRFTests.swift
    ├── CredBlobTests.swift
    ├── CredPropsTests.swift
    ├── CredProtectTests.swift
    ├── LargeBlobsTests.swift
    └── MinPinLengthTests.swift
```

### Test Coverage
- **StatusStream:** Deduplication, timeout handling, error propagation
- **Serialization:** JSON, CBOR, Base64URL encoding/decoding
- **Response Parsing:** Authenticator data, attestation object structure
- **Origin Validation:** RP ID matching, public suffix handling
- **Credential Matching:** Exclude list, allow list probing
- **Mock Backend:** TestableClient with injected Backend protocol for isolated testing
- **Integration Tests:** Full flow with real YubiKey device (requires device + PIN)
- **PreviewSign:** GenerateKey output validation, algorithm selection, signing parameter passing

---

## Summary

The Swift WebAuthn Client is a sophisticated, well-architected wrapper over CTAP2 that:

1. **Abstracts the WebAuthn spec** (PublicKeyCredentialCreationOptions → CTAP2.MakeCredential)
2. **Manages session lifecycle** via a Backend protocol (testable, mockable)
3. **Handles PIN/UV flows** with automatic retry logic and status callbacks
4. **Processes extensions** via a bidirectional bridge (WebAuthn ↔ CTAP2)
5. **Constructs proper JSON/CBOR** for client data, attestation objects, authenticator data
6. **Defers extension processing** (e.g., PRF decryption, largeBlob read/write) until credential selection
7. **Supports emerging specs** like previewSign with first-class integration

The implementation is production-grade with comprehensive testing, clear separation of concerns, and extensive type safety via Swift's enum-based design.

