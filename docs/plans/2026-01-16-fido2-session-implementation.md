# FidoSession Implementation Plan

**Goal:** Port the complete FIDO2/CTAP2 functionality from yubikit-android to C#, creating a fully-featured `FidoSession` that follows the SDK's modern session patterns, including YubiKey 5.7/5.8 features (PPUAT, encIdentifier, encCredStoreState).

**Architecture:** The FidoSession will derive from `ApplicationSession` (relying on base-class `IsInitialized` state) and support both SmartCard (CCID) and FIDO HID transports. It implements CTAP 2.3 protocol using `System.Formats.Cbor` with type-safe generic builders (avoiding `object?`). Integrates with existing `Yubico.YubiKit.Core.Cryptography.Cose.*` types and `KeyType`/`KeyDefinitions` infrastructure. WebAuthn/CTAP extensions (hmac-secret, hmac-secret-mc, credProtect, etc.) are first-class citizens.

**Tech Stack:** C# 14, .NET 10, System.Formats.Cbor, NSubstitute (tests), `Span<T>/Memory<T>` patterns, existing `CoseKey`/`CoseAlgorithmIdentifier` infrastructure

---

## Design Decisions & Rationale

### Addressing Feedback

| Concern | Resolution |
|---------|------------|
| **Moq → NSubstitute** | Use `NSubstitute` exclusively, per existing test projects |
| **ApplicationSession state** | Rely on base `IsInitialized`, `FirmwareVersion`, `Protocol` - no redundant caching |
| **No caching InfoData** | `GetInfoAsync()` always fetches fresh; no `_info` field; callers cache if needed |
| **Avoid `object?` in CBOR** | Use generic `CtapRequestBuilder<TKey>` with type constraints; strongly-typed model parsing |
| **Integrate KeyType/Cose** | Reuse `CoseAlgorithmIdentifier`, `CoseKeyType`, `KeyType.GetCoseKeyDefinition()` |
| **Fluent/Builder for CBOR** | `CtapRequestBuilder` with `.WithParameter(key, value)` fluent API |
| **`FromMap` naming** | Use `Parse(CborReader reader)` or `Decode(ReadOnlySpan<byte>)` C# idioms |
| **Extension methods for tests** | `YubiKeyTestState.WithFidoSessionAsync(...)` extension, not helper class |
| **Reuse WithYubiKeyAttribute** | Use existing `[WithYubiKey(Capability = DeviceCapabilities.Fido2)]` |
| **YubiKey 5.7/5.8 features** | Include `encIdentifier`, `encCredStoreState`, PPUAT decryption via HKDF |
| **WebAuthn/CTAP extensions** | Full support for hmac-secret, hmac-secret-mc, credProtect, credBlob, largeBlob, minPinLength, prf |

---

## Source Material Reference

### Java Source Files (yubikit-android/fido/src/main/java/com/yubico/yubikit/fido/)
- **ctap/Ctap2Session.java** - Main CTAP2 session with InfoData nested class
- **ctap/ClientPin.java** - PIN/UV token management
- **ctap/PinUvAuthProtocol.java** - Interface for PIN protocols
- **ctap/PinUvAuthProtocolV1.java** / **V2.java** - Protocol implementations
- **ctap/CredentialManagement.java** - Discoverable credential management
- **ctap/BioEnrollment.java** / **FingerprintBioEnrollment.java** - Bio enrollment
- **ctap/Config.java** - Authenticator configuration
- **ctap/Hkdf.java** - HKDF for PPUAT decryption
- **client/extensions/*.java** - WebAuthn extension implementations
- **Cbor.java** - CTAP canonical CBOR (reference only; use System.Formats.Cbor)
- **webauthn/*.java** - WebAuthn data models

### C# Reference Files (this repo)
- **Yubico.YubiKit.Management/src/ManagementSession.cs** - Session pattern template (preferred over ManagementSession)
- **Yubico.YubiKit.Management/src/IYubiKeyExtensions.cs** - C# 14 extension member pattern
- **Yubico.YubiKit.Core/src/YubiKey/ApplicationSession.cs** - Base class
- **Yubico.YubiKit.Core/src/Cryptography/Cose/** - Existing COSE infrastructure
- **Yubico.YubiKit.Core/src/Cryptography/KeyType.cs** / **KeyTypeExtensions.cs** - Key type utilities
- **Yubico.YubiKit.Tests.Shared/Infrastructure/WithYubiKeyAttribute.cs** - Test data attribute
- **docs/templates/session-package-checklist.md** / **session-test-infra-checklist.md** - Checklists

---

## Implementation Phases

| Phase | Name | Priority | Dependencies | Est. Effort |
|-------|------|----------|--------------|-------------|
| 1 | Foundation & Project Setup | P0 | None | 1 day |
| 2 | CTAP CBOR Infrastructure | P0 | Phase 1 | 1.5 days |
| 3 | Core FidoSession | P0 | Phase 2 | 2 days |
| 4 | PIN/UV Auth Protocols + PPUAT | P0 | Phase 3 | 2 days |
| 5 | MakeCredential & GetAssertion | P0 | Phase 4 | 3 days |
| 6 | WebAuthn/CTAP Extensions | P0 | Phase 5 | 2 days |
| 7 | CredentialManagement | P1 | Phase 5 | 2 days |
| 8 | BioEnrollment (FW 5.2+) | P1 | Phase 5 | 2 days |
| 9 | Config Commands | P1 | Phase 5 | 1 day |
| 10 | Large Blobs | P2 | Phase 5 | 1 day |
| 11 | YK 5.7/5.8 Features | P1 | Phase 4 | 1 day |
| 12 | Integration Tests | P0 | Phase 5+ | Ongoing |
| 13 | Documentation, DI & Extensions | P1 | All | 1 day |

### YubiKey Version Feature Matrix

| Feature | Min FW | Phase |
|---------|--------|-------|
| FIDO2/CTAP2 Core | 5.0 | 3-5 |
| hmac-secret extension | 5.0 | 6 |
| Bio Enrollment | 5.2 | 8 |
| credProtect extension | 5.2 | 6 |
| Credential Management | 5.2 | 7 |
| Large Blobs | 5.2 | 10 |
| hmac-secret-mc extension | 5.4 | 6 |
| credBlob extension | 5.5 | 6 |
| minPinLength extension | 5.4 | 6 |
| authenticatorConfig | 5.4 | 9 |
| encIdentifier / encCredStoreState | 5.7 | 11 |
| PPUAT (Persistent PIN/UV Auth Token) | 5.7 | 4, 11 |

---

## Phase 1: Foundation & Project Setup

### Task 1.1: Create Project Structure

**Files:**
- Create: `Yubico.YubiKit.Fido2/src/Yubico.YubiKit.Fido2.csproj`
- Create: `Yubico.YubiKit.Fido2/src/FidoSession.cs` (stub)
- Create: `Yubico.YubiKit.Fido2/src/IFidoSession.cs`
- Modify: `Yubico.YubiKit.sln`

**Step 1: Create project file**

```xml
<!-- Yubico.YubiKit.Fido2/src/Yubico.YubiKit.Fido2.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>14</LangVersion>
    <RootNamespace>Yubico.YubiKit.Fido2</RootNamespace>
    <AssemblyName>Yubico.YubiKit.Fido2</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\Yubico.YubiKit.Core\src\Yubico.YubiKit.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.0" />
    <PackageReference Include="System.Formats.Cbor" Version="9.0.0" />
  </ItemGroup>

</Project>
```

**Step 2: Create interface stub**

```csharp
// Yubico.YubiKit.Fido2/src/IFidoSession.cs
using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.Fido2;

/// <summary>
/// Interface for FIDO2/CTAP2 session operations.
/// </summary>
public interface IFidoSession : IApplicationSession
{
    /// <summary>
    /// Gets authenticator information. Always fetches fresh data from device.
    /// </summary>
    Task<AuthenticatorInfo> GetInfoAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Requests the user to select this authenticator (touch).
    /// </summary>
    Task SelectionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Resets the FIDO application. Requires touch within seconds of device insertion.
    /// </summary>
    Task ResetAsync(CancellationToken cancellationToken = default);
}
```

**Step 3: Create session stub following ManagementSession pattern**

```csharp
// Yubico.YubiKit.Fido2/src/FidoSession.cs
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Fido2;

/// <summary>
/// Provides FIDO2/CTAP2 session operations for YubiKey authenticators.
/// </summary>
/// <remarks>
/// <para>Implements CTAP 2.3 specification.</para>
/// <para>Supports SmartCard (CCID) and FIDO HID transports.</para>
/// <para>
/// See: https://fidoalliance.org/specs/fido-v2.3-rd-20251023/fido-client-to-authenticator-protocol-v2.3-rd-20251023.html
/// </para>
/// </remarks>
public sealed class FidoSession : ApplicationSession, IFidoSession
{
    // Feature constants
    public static readonly Feature FeatureFido2 = new("FIDO2", 5, 0, 0);
    public static readonly Feature FeatureBioEnrollment = new("Bio Enrollment", 5, 2, 0);
    public static readonly Feature FeatureCredentialManagement = new("Credential Management", 5, 2, 0);
    public static readonly Feature FeatureHmacSecretMc = new("hmac-secret-mc", 5, 4, 0);
    public static readonly Feature FeatureAuthenticatorConfig = new("Authenticator Config", 5, 4, 0);
    public static readonly Feature FeatureEncIdentifier = new("Encrypted Identifier", 5, 7, 0);
    
    private readonly IConnection _connection;
    private readonly ScpKeyParameters? _scpKeyParams;
    private readonly ILogger _logger;
    
    private IFidoBackend? _backend;
    
    private FidoSession(IConnection connection, ScpKeyParameters? scpKeyParams = null)
    {
        _connection = connection;
        _scpKeyParams = scpKeyParams;
        _logger = Logger;
    }
    
    /// <summary>
    /// Factory method that creates and initializes a FIDO session.
    /// </summary>
    public static async Task<FidoSession> CreateAsync(
        IConnection connection,
        ProtocolConfiguration? configuration = null,
        ScpKeyParameters? scpKeyParams = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        
        var session = new FidoSession(connection, scpKeyParams);
        await session.InitializeAsync(configuration, cancellationToken).ConfigureAwait(false);
        return session;
    }
    
    private async Task InitializeAsync(
        ProtocolConfiguration? configuration,
        CancellationToken cancellationToken)
    {
        if (IsInitialized)
            return;
        
        // Create backend based on connection type
        var (backend, protocol) = _connection switch
        {
            ISmartCardConnection sc => CreateSmartCardBackend(sc),
            IFidoHidConnection fido => CreateFidoHidBackend(fido),
            _ => throw new NotSupportedException(
                $"Connection type {_connection.GetType().Name} is not supported. " +
                "Use ISmartCardConnection or IFidoHidConnection.")
        };
        
        _backend = backend;
        
        // Get firmware version from authenticator info
        var info = await GetInfoCoreAsync(backend, cancellationToken).ConfigureAwait(false);
        var firmwareVersion = info.FirmwareVersion ?? new FirmwareVersion();
        
        // Initialize base class (handles SCP if requested)
        await InitializeCoreAsync(
                protocol,
                firmwareVersion,
                configuration,
                _scpKeyParams,
                cancellationToken)
            .ConfigureAwait(false);
        
        // If SCP was established, recreate backend with wrapped protocol
        if (IsAuthenticated && Protocol is ISmartCardProtocol scpProtocol)
        {
            _backend = new SmartCardFidoBackend(scpProtocol);
        }
        
        _logger.LogDebug(
            "FIDO session initialized. Firmware: {Version}, Versions: [{Versions}]",
            firmwareVersion,
            string.Join(", ", info.Versions));
    }
    
    /// <inheritdoc />
    public Task<AuthenticatorInfo> GetInfoAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        return GetInfoCoreAsync(_backend!, cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task SelectionAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        await SendCborAsync(CtapCommand.Selection, null, cancellationToken).ConfigureAwait(false);
    }
    
    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        await SendCborAsync(CtapCommand.Reset, null, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("FIDO application reset");
    }
    
    private static async Task<AuthenticatorInfo> GetInfoCoreAsync(
        IFidoBackend backend,
        CancellationToken cancellationToken)
    {
        var response = await backend.SendCborAsync(
            CtapRequestBuilder.Create(CtapCommand.GetInfo).Build(),
            cancellationToken).ConfigureAwait(false);
        
        return AuthenticatorInfo.Decode(response);
    }
    
    internal async Task<ReadOnlyMemory<byte>> SendCborAsync(
        byte command,
        ReadOnlyMemory<byte>? payload,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        
        var request = payload.HasValue
            ? CtapRequestBuilder.Create(command).WithPayload(payload.Value).Build()
            : CtapRequestBuilder.Create(command).Build();
        
        return await _backend!.SendCborAsync(request, cancellationToken).ConfigureAwait(false);
    }
    
    private void EnsureInitialized()
    {
        if (!IsInitialized)
            throw new InvalidOperationException("Session is not initialized. Call CreateAsync first.");
    }
    
    private static (IFidoBackend backend, IProtocol protocol) CreateSmartCardBackend(
        ISmartCardConnection connection)
    {
        var protocol = PcscProtocolFactory<ISmartCardConnection>
            .Create()
            .Create(connection);
        
        var backend = new SmartCardFidoBackend(
            protocol as ISmartCardProtocol ?? throw new InvalidOperationException());
        
        return (backend, protocol);
    }
    
    private static (IFidoBackend backend, IProtocol protocol) CreateFidoHidBackend(
        IFidoHidConnection connection)
    {
        var protocol = FidoProtocolFactory
            .Create()
            .Create(connection);
        
        var backend = new FidoHidBackend(protocol);
        return (backend, protocol);
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _backend?.Dispose();
            _backend = null;
        }
        base.Dispose(disposing);
    }
}
```

**Step 4: Add project to solution and verify build**

```bash
cd /home/dyallo/Code/y/Yubico.NET.SDK
dotnet sln add Yubico.YubiKit.Fido2/src/Yubico.YubiKit.Fido2.csproj
dotnet build Yubico.YubiKit.Fido2/src/Yubico.YubiKit.Fido2.csproj
```

**Step 5: Commit**

```bash
git add Yubico.YubiKit.Fido2/ Yubico.YubiKit.sln
git commit -m "feat(fido2): scaffold Yubico.YubiKit.Fido2 project"
```

---

### Task 1.2: Create Test Project Structure

**Files:**
- Create: `Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.Tests.csproj`
- Create: `Yubico.YubiKit.Fido2/tests/Unit/FidoSessionTests.cs`
- Create: `Yubico.YubiKit.Fido2/tests/Extensions/YubiKeyTestStateExtensions.cs`
- Modify: `Yubico.YubiKit.sln`

**Step 1: Create test project with NSubstitute**

```xml
<!-- Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>14</LangVersion>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="NSubstitute" Version="5.3.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\src\Yubico.YubiKit.Fido2.csproj" />
    <ProjectReference Include="..\..\Yubico.YubiKit.Tests.Shared\Yubico.YubiKit.Tests.Shared.csproj" />
  </ItemGroup>

</Project>
```

**Step 2: Create test extension methods for integration tests**

```csharp
// Yubico.YubiKit.Fido2/tests/Extensions/YubiKeyTestStateExtensions.cs
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Fido2.Tests.Extensions;

/// <summary>
/// Extension methods for FIDO2 integration tests.
/// </summary>
public static class YubiKeyTestStateExtensions
{
    /// <summary>
    /// Creates a FIDO session and executes an action.
    /// </summary>
    public static async Task WithFidoSessionAsync(
        this YubiKeyTestState device,
        Func<FidoSession, Task> action,
        CancellationToken cancellationToken = default)
    {
        // Prefer FIDO HID if available, fall back to SmartCard
        if (device.AvailableTransports.HasFlag(Transport.Usb))
        {
            await using var connection = await device.YubiKey
                .ConnectAsync<IFidoHidConnection>(cancellationToken)
                .ConfigureAwait(false);
            
            await using var session = await FidoSession.CreateAsync(
                    connection, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            
            await action(session).ConfigureAwait(false);
        }
        else
        {
            await using var connection = await device.YubiKey
                .ConnectAsync<ISmartCardConnection>(cancellationToken)
                .ConfigureAwait(false);
            
            await using var session = await FidoSession.CreateAsync(
                    connection, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            
            await action(session).ConfigureAwait(false);
        }
    }
    
    /// <summary>
    /// Creates an authenticated FIDO session with PIN token and executes an action.
    /// </summary>
    public static async Task WithAuthenticatedFidoSessionAsync(
        this YubiKeyTestState device,
        string pin,
        PinPermission permissions,
        Func<FidoSession, byte[], IPinUvAuthProtocol, Task> action,
        CancellationToken cancellationToken = default)
    {
        await device.WithFidoSessionAsync(async session =>
        {
            var protocol = new PinUvAuthProtocolV2();
            var clientPin = new ClientPin(session, protocol);
            var pinToken = await clientPin.GetPinTokenAsync(
                    pin, permissions, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            
            await action(session, pinToken, protocol).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }
}
```

**Step 3: Create initial unit test**

```csharp
// Yubico.YubiKit.Fido2/tests/Unit/FidoSessionTests.cs
using NSubstitute;
using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.Fido2.Tests.Unit;

public class FidoSessionTests
{
    [Fact]
    public async Task CreateAsync_NullConnection_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => FidoSession.CreateAsync(null!));
    }
    
    [Fact]
    public async Task CreateAsync_UnsupportedConnectionType_ThrowsNotSupportedException()
    {
        var unsupportedConnection = Substitute.For<IConnection>();
        
        await Assert.ThrowsAsync<NotSupportedException>(
            () => FidoSession.CreateAsync(unsupportedConnection));
    }
}
```

**Step 4: Add to solution and verify**

```bash
dotnet sln add Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.Tests.csproj
dotnet test Yubico.YubiKit.Fido2/tests/Yubico.YubiKit.Fido2.Tests.csproj
```

**Step 5: Commit**

```bash
git add Yubico.YubiKit.Fido2/tests/ Yubico.YubiKit.sln
git commit -m "test(fido2): add test project with NSubstitute"
```

---

## Phase 2: CTAP CBOR Infrastructure

### Task 2.1: Type-Safe CTAP Request Builder (Fluent API)

**Context:** Instead of using `object?` everywhere, we create a type-safe fluent builder for CTAP requests. This avoids the pitfalls of dynamic typing while maintaining flexibility.

**Files:**
- Create: `Yubico.YubiKit.Fido2/src/Cbor/CtapRequestBuilder.cs`
- Create: `Yubico.YubiKit.Fido2/src/Cbor/CtapResponseReader.cs`
- Create: `Yubico.YubiKit.Fido2/tests/Unit/Cbor/CtapRequestBuilderTests.cs`

**Step 1: Create CtapRequestBuilder with fluent API**

```csharp
// Yubico.YubiKit.Fido2/src/Cbor/CtapRequestBuilder.cs
using System.Formats.Cbor;
using System.Text;

namespace Yubico.YubiKit.Fido2.Cbor;

/// <summary>
/// Fluent builder for constructing CTAP2 requests with canonical CBOR encoding.
/// </summary>
/// <remarks>
/// CTAP2 requires deterministic CBOR encoding per:
/// https://fidoalliance.org/specs/fido-v2.0-ps-20190130/fido-client-to-authenticator-protocol-v2.0-ps-20190130.html#ctap2-canonical-cbor-encoding-form
/// </remarks>
public sealed class CtapRequestBuilder
{
    private readonly byte _command;
    private readonly SortedDictionary<int, Action<CborWriter>> _parameters = new();
    private ReadOnlyMemory<byte>? _rawPayload;
    
    private CtapRequestBuilder(byte command)
    {
        _command = command;
    }
    
    /// <summary>
    /// Creates a new builder for the specified CTAP command.
    /// </summary>
    public static CtapRequestBuilder Create(byte command) => new(command);
    
    /// <summary>
    /// Adds an integer parameter.
    /// </summary>
    public CtapRequestBuilder WithInt(int key, int value)
    {
        _parameters[key] = writer => writer.WriteInt32(value);
        return this;
    }
    
    /// <summary>
    /// Adds a byte array parameter.
    /// </summary>
    public CtapRequestBuilder WithBytes(int key, ReadOnlySpan<byte> value)
    {
        var copy = value.ToArray();
        _parameters[key] = writer => writer.WriteByteString(copy);
        return this;
    }
    
    /// <summary>
    /// Adds a string parameter.
    /// </summary>
    public CtapRequestBuilder WithString(int key, string value)
    {
        _parameters[key] = writer => writer.WriteTextString(value);
        return this;
    }
    
    /// <summary>
    /// Adds a boolean parameter.
    /// </summary>
    public CtapRequestBuilder WithBool(int key, bool value)
    {
        _parameters[key] = writer => writer.WriteBoolean(value);
        return this;
    }
    
    /// <summary>
    /// Adds a CBOR map parameter with integer keys (e.g., COSE key).
    /// </summary>
    public CtapRequestBuilder WithIntKeyMap(int key, IReadOnlyDictionary<int, byte[]> map)
    {
        _parameters[key] = writer =>
        {
            writer.WriteStartMap(map.Count);
            foreach (var (k, v) in map.OrderBy(kvp => kvp.Key))
            {
                writer.WriteInt32(k);
                writer.WriteByteString(v);
            }
            writer.WriteEndMap();
        };
        return this;
    }
    
    /// <summary>
    /// Adds a CBOR map parameter with string keys.
    /// </summary>
    public CtapRequestBuilder WithStringKeyMap(int key, IReadOnlyDictionary<string, byte[]> map)
    {
        _parameters[key] = writer =>
        {
            writer.WriteStartMap(map.Count);
            // CTAP canonical: sort by encoded length, then lexicographically
            foreach (var (k, v) in map.OrderBy(kvp => Encoding.UTF8.GetByteCount(kvp.Key))
                         .ThenBy(kvp => kvp.Key, StringComparer.Ordinal))
            {
                writer.WriteTextString(k);
                writer.WriteByteString(v);
            }
            writer.WriteEndMap();
        };
        return this;
    }
    
    /// <summary>
    /// Adds an array of byte arrays.
    /// </summary>
    public CtapRequestBuilder WithByteArrayList(int key, IReadOnlyList<byte[]> items)
    {
        _parameters[key] = writer =>
        {
            writer.WriteStartArray(items.Count);
            foreach (var item in items)
                writer.WriteByteString(item);
            writer.WriteEndArray();
        };
        return this;
    }
    
    /// <summary>
    /// Adds pre-encoded CBOR data as a parameter.
    /// </summary>
    public CtapRequestBuilder WithEncodedValue(int key, ReadOnlySpan<byte> encodedCbor)
    {
        var copy = encodedCbor.ToArray();
        _parameters[key] = writer => writer.WriteEncodedValue(copy);
        return this;
    }
    
    /// <summary>
    /// Sets a raw CBOR payload instead of building parameters.
    /// </summary>
    public CtapRequestBuilder WithPayload(ReadOnlyMemory<byte> payload)
    {
        _rawPayload = payload;
        return this;
    }
    
    /// <summary>
    /// Adds a parameter only if the value is not null.
    /// </summary>
    public CtapRequestBuilder WithBytesIfPresent(int key, byte[]? value)
    {
        if (value is not null)
            WithBytes(key, value);
        return this;
    }
    
    /// <summary>
    /// Adds a parameter only if the value is not null.
    /// </summary>
    public CtapRequestBuilder WithIntIfPresent(int key, int? value)
    {
        if (value.HasValue)
            WithInt(key, value.Value);
        return this;
    }
    
    /// <summary>
    /// Adds a parameter only if the value is not null or empty.
    /// </summary>
    public CtapRequestBuilder WithStringIfPresent(int key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
            WithString(key, value);
        return this;
    }
    
    /// <summary>
    /// Builds the CTAP request as a byte array.
    /// </summary>
    public byte[] Build()
    {
        if (_rawPayload.HasValue)
        {
            var result = new byte[1 + _rawPayload.Value.Length];
            result[0] = _command;
            _rawPayload.Value.Span.CopyTo(result.AsSpan(1));
            return result;
        }
        
        if (_parameters.Count == 0)
            return [_command];
        
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(_parameters.Count);
        
        foreach (var (key, writeValue) in _parameters)
        {
            writer.WriteInt32(key);
            writeValue(writer);
        }
        
        writer.WriteEndMap();
        
        var payload = writer.Encode();
        var request = new byte[1 + payload.Length];
        request[0] = _command;
        payload.CopyTo(request, 1);
        
        return request;
    }
}
```

**Step 2: Create CtapResponseReader for type-safe parsing**

```csharp
// Yubico.YubiKit.Fido2/src/Cbor/CtapResponseReader.cs
using System.Formats.Cbor;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Exceptions;

namespace Yubico.YubiKit.Fido2.Cbor;

/// <summary>
/// Reads and parses CTAP2 responses with type-safe accessors.
/// </summary>
public ref struct CtapResponseReader
{
    private readonly ReadOnlySpan<byte> _data;
    private readonly CborReader _reader;
    private readonly int _mapCount;
    private int _currentKey;
    
    /// <summary>
    /// Creates a reader from raw response data (after status byte).
    /// </summary>
    public static CtapResponseReader Create(ReadOnlySpan<byte> responseData)
    {
        if (responseData.IsEmpty)
            throw new ArgumentException("Response data is empty", nameof(responseData));
        
        var status = responseData[0];
        if (status != (byte)CtapError.Success)
            throw new CtapException(status);
        
        return new CtapResponseReader(responseData[1..]);
    }
    
    private CtapResponseReader(ReadOnlySpan<byte> data)
    {
        _data = data;
        
        if (_data.IsEmpty)
        {
            _reader = default;
            _mapCount = 0;
            _currentKey = -1;
            return;
        }
        
        _reader = new CborReader(_data.ToArray(), CborConformanceMode.Ctap2Canonical);
        _mapCount = _reader.ReadStartMap() ?? 0;
        _currentKey = -1;
    }
    
    /// <summary>
    /// Gets whether the response has more entries to read.
    /// </summary>
    public bool HasMore => _reader.PeekState() != CborReaderState.EndMap;
    
    /// <summary>
    /// Reads the next key.
    /// </summary>
    public int ReadKey()
    {
        _currentKey = _reader.ReadInt32();
        return _currentKey;
    }
    
    /// <summary>
    /// Reads an integer value.
    /// </summary>
    public int ReadInt32() => _reader.ReadInt32();
    
    /// <summary>
    /// Reads a byte string value.
    /// </summary>
    public byte[] ReadByteString() => _reader.ReadByteString();
    
    /// <summary>
    /// Reads a text string value.
    /// </summary>
    public string ReadTextString() => _reader.ReadTextString();
    
    /// <summary>
    /// Reads a boolean value.
    /// </summary>
    public bool ReadBoolean() => _reader.ReadBoolean();
    
    /// <summary>
    /// Reads a list of strings.
    /// </summary>
    public List<string> ReadStringList()
    {
        var count = _reader.ReadStartArray() ?? 0;
        var list = new List<string>(count);
        
        for (var i = 0; i < count; i++)
            list.Add(_reader.ReadTextString());
        
        _reader.ReadEndArray();
        return list;
    }
    
    /// <summary>
    /// Reads a list of integers.
    /// </summary>
    public List<int> ReadIntList()
    {
        var count = _reader.ReadStartArray() ?? 0;
        var list = new List<int>(count);
        
        for (var i = 0; i < count; i++)
            list.Add(_reader.ReadInt32());
        
        _reader.ReadEndArray();
        return list;
    }
    
    /// <summary>
    /// Reads a map with string keys and boolean values (options map).
    /// </summary>
    public Dictionary<string, bool> ReadOptionsMap()
    {
        var count = _reader.ReadStartMap() ?? 0;
        var map = new Dictionary<string, bool>(count);
        
        for (var i = 0; i < count; i++)
        {
            var key = _reader.ReadTextString();
            var value = _reader.ReadBoolean();
            map[key] = value;
        }
        
        _reader.ReadEndMap();
        return map;
    }
    
    /// <summary>
    /// Reads an encoded COSE key as raw bytes.
    /// </summary>
    public byte[] ReadEncodedCoseKey()
    {
        return _reader.ReadEncodedValue().ToArray();
    }
    
    /// <summary>
    /// Skips the current value.
    /// </summary>
    public void SkipValue() => _reader.SkipValue();
}
```

**Step 3: Write tests**

```csharp
// Yubico.YubiKit.Fido2/tests/Unit/Cbor/CtapRequestBuilderTests.cs
using System.Formats.Cbor;
using Yubico.YubiKit.Fido2.Cbor;
using Yubico.YubiKit.Fido2.Ctap;

namespace Yubico.YubiKit.Fido2.Tests.Unit.Cbor;

public class CtapRequestBuilderTests
{
    [Fact]
    public void Build_CommandOnly_ReturnsSingleByte()
    {
        var result = CtapRequestBuilder.Create(CtapCommand.GetInfo).Build();
        
        Assert.Single(result);
        Assert.Equal(CtapCommand.GetInfo, result[0]);
    }
    
    [Fact]
    public void Build_WithIntParameter_ProducesValidCbor()
    {
        var result = CtapRequestBuilder.Create(CtapCommand.ClientPin)
            .WithInt(1, 2)  // pinUvAuthProtocol = 2
            .WithInt(2, 1)  // subCommand = getRetries
            .Build();
        
        Assert.Equal(CtapCommand.ClientPin, result[0]);
        
        // Parse CBOR payload
        var reader = new CborReader(result.AsSpan(1).ToArray(), CborConformanceMode.Ctap2Canonical);
        var mapCount = reader.ReadStartMap();
        
        Assert.Equal(2, mapCount);
        Assert.Equal(1, reader.ReadInt32()); // key
        Assert.Equal(2, reader.ReadInt32()); // value
        Assert.Equal(2, reader.ReadInt32()); // key
        Assert.Equal(1, reader.ReadInt32()); // value
    }
    
    [Fact]
    public void Build_WithBytes_ProducesValidCbor()
    {
        var clientDataHash = new byte[32];
        
        var result = CtapRequestBuilder.Create(CtapCommand.MakeCredential)
            .WithBytes(1, clientDataHash)
            .Build();
        
        Assert.Equal(CtapCommand.MakeCredential, result[0]);
        
        var reader = new CborReader(result.AsSpan(1).ToArray(), CborConformanceMode.Ctap2Canonical);
        reader.ReadStartMap();
        Assert.Equal(1, reader.ReadInt32());
        Assert.Equal(32, reader.ReadByteString().Length);
    }
    
    [Fact]
    public void Build_ParameterOrderIsCanonical()
    {
        // Add parameters out of order
        var result = CtapRequestBuilder.Create(CtapCommand.MakeCredential)
            .WithInt(3, 3)
            .WithInt(1, 1)
            .WithInt(2, 2)
            .Build();
        
        var reader = new CborReader(result.AsSpan(1).ToArray(), CborConformanceMode.Ctap2Canonical);
        reader.ReadStartMap();
        
        // Keys should be in canonical order: 1, 2, 3
        Assert.Equal(1, reader.ReadInt32());
        reader.SkipValue();
        Assert.Equal(2, reader.ReadInt32());
        reader.SkipValue();
        Assert.Equal(3, reader.ReadInt32());
    }
    
    [Fact]
    public void WithIntIfPresent_NullValue_DoesNotAddParameter()
    {
        var result = CtapRequestBuilder.Create(CtapCommand.ClientPin)
            .WithInt(1, 2)
            .WithIntIfPresent(2, null)
            .Build();
        
        var reader = new CborReader(result.AsSpan(1).ToArray(), CborConformanceMode.Ctap2Canonical);
        Assert.Equal(1, reader.ReadStartMap()); // Only one parameter
    }
}
```

**Step 4: Run tests and commit**

```bash
dotnet test Yubico.YubiKit.Fido2/tests --filter "FullyQualifiedName~CtapRequestBuilderTests"
git add Yubico.YubiKit.Fido2/src/Cbor/ Yubico.YubiKit.Fido2/tests/Unit/Cbor/
git commit -m "feat(fido2): add type-safe CTAP request builder with fluent API"
```

---

### Task 2.2: CTAP Command Constants and Exceptions

**Files:**
- Create: `Yubico.YubiKit.Fido2/src/Ctap/CtapCommand.cs`
- Create: `Yubico.YubiKit.Fido2/src/Ctap/CtapError.cs`
- Create: `Yubico.YubiKit.Fido2/src/Exceptions/CtapException.cs`

**Reference:** `Ctap2Session.java` lines 63-76 for commands

```csharp
// Yubico.YubiKit.Fido2/src/Ctap/CtapCommand.cs
namespace Yubico.YubiKit.Fido2.Ctap;

/// <summary>
/// CTAP2 command codes per CTAP 2.3 specification.
/// </summary>
public static class CtapCommand
{
    public const byte MakeCredential = 0x01;
    public const byte GetAssertion = 0x02;
    public const byte GetInfo = 0x04;
    public const byte ClientPin = 0x06;
    public const byte Reset = 0x07;
    public const byte GetNextAssertion = 0x08;
    public const byte BioEnrollment = 0x09;
    public const byte CredentialManagement = 0x0A;
    public const byte Selection = 0x0B;
    public const byte LargeBlobs = 0x0C;
    public const byte Config = 0x0D;
    public const byte BioEnrollmentPreview = 0x40;
    public const byte CredentialManagementPreview = 0x41;
}
```

```csharp
// Yubico.YubiKit.Fido2/src/Ctap/CtapError.cs
namespace Yubico.YubiKit.Fido2.Ctap;

/// <summary>
/// CTAP2 error codes per CTAP 2.3 specification.
/// </summary>
public enum CtapError : byte
{
    Success = 0x00,
    InvalidCommand = 0x01,
    InvalidParameter = 0x02,
    InvalidLength = 0x03,
    InvalidSeq = 0x04,
    Timeout = 0x05,
    ChannelBusy = 0x06,
    LockRequired = 0x0A,
    InvalidChannel = 0x0B,
    CborUnexpectedType = 0x11,
    InvalidCbor = 0x12,
    MissingParameter = 0x14,
    LimitExceeded = 0x15,
    FpDatabaseFull = 0x17,
    LargeBlobStorageFull = 0x18,
    CredentialExcluded = 0x19,
    Processing = 0x21,
    InvalidCredential = 0x22,
    UserActionPending = 0x23,
    OperationPending = 0x24,
    NoOperations = 0x25,
    UnsupportedAlgorithm = 0x26,
    OperationDenied = 0x27,
    KeyStoreFull = 0x28,
    UnsupportedOption = 0x2B,
    InvalidOption = 0x2C,
    KeepaliveCancel = 0x2D,
    NoCredentials = 0x2E,
    UserActionTimeout = 0x2F,
    NotAllowed = 0x30,
    PinInvalid = 0x31,
    PinBlocked = 0x32,
    PinAuthInvalid = 0x33,
    PinAuthBlocked = 0x34,
    PinNotSet = 0x35,
    PuatRequired = 0x36,
    PinPolicyViolation = 0x37,
    RequestTooLarge = 0x39,
    ActionTimeout = 0x3A,
    UpRequired = 0x3B,
    UvBlocked = 0x3C,
    IntegrityFailure = 0x3D,
    InvalidSubcommand = 0x3E,
    UvInvalid = 0x3F,
    UnauthorizedPermission = 0x40,
    Other = 0x7F
}
```

```csharp
// Yubico.YubiKit.Fido2/src/Exceptions/CtapException.cs
using Yubico.YubiKit.Fido2.Ctap;

namespace Yubico.YubiKit.Fido2.Exceptions;

/// <summary>
/// Exception thrown when a CTAP command returns an error status.
/// </summary>
public class CtapException : Exception
{
    /// <summary>
    /// Gets the CTAP error code.
    /// </summary>
    public CtapError Error { get; }
    
    public CtapException(CtapError error)
        : base($"CTAP error: {error} (0x{(byte)error:X2})")
    {
        Error = error;
    }
    
    public CtapException(byte errorCode)
        : this((CtapError)errorCode)
    {
    }
    
    public CtapException(CtapError error, string message)
        : base(message)
    {
        Error = error;
    }
    
    /// <summary>
    /// Convenience check for PIN-related errors.
    /// </summary>
    public bool IsPinError => Error is 
        CtapError.PinInvalid or 
        CtapError.PinBlocked or 
        CtapError.PinAuthInvalid or 
        CtapError.PinAuthBlocked or 
        CtapError.PinNotSet or 
        CtapError.PinPolicyViolation;
}
```

**Commit:**

```bash
git add Yubico.YubiKit.Fido2/src/Ctap/ Yubico.YubiKit.Fido2/src/Exceptions/
git commit -m "feat(fido2): add CTAP commands, errors, and exception types"
```

---

### Task 3.2: AuthenticatorInfo Model (using Parse pattern)

**Context:** The `authenticatorGetInfo` response contains authenticator capabilities. This is a critical data model. We use the `Parse(CborReader)` pattern consistent with other SDK models.

**Design Decision:** Following user feedback, we use `Parse(CborReader)` and `Decode(ReadOnlySpan<byte>)` idioms instead of `FromMap(Dictionary)` to avoid the `object?` type and maintain type safety.

**Files:**
- Create: `Yubico.YubiKit.Fido2/src/Models/AuthenticatorInfo.cs`
- Create: `Yubico.YubiKit.Fido2/tests/Unit/Models/AuthenticatorInfoTests.cs`

**Reference:** `Ctap2Session.java` lines 621-900 (InfoData class)

**Step 1: Write tests using raw CBOR bytes**

```csharp
// Yubico.YubiKit.Fido2/tests/Unit/Models/AuthenticatorInfoTests.cs
using System.Formats.Cbor;
using Yubico.YubiKit.Fido2.Models;

namespace Yubico.YubiKit.Fido2.Tests.Unit.Models;

public class AuthenticatorInfoTests
{
    [Fact]
    public void Decode_ValidCborResponse_ParsesCorrectly()
    {
        // Build a minimal valid getInfo response
        var cborBytes = BuildGetInfoResponse();
        
        var info = AuthenticatorInfo.Decode(cborBytes);
        
        Assert.Contains("FIDO_2_0", info.Versions);
        Assert.Contains("FIDO_2_1", info.Versions);
        Assert.Contains("credProtect", info.Extensions);
        Assert.Equal(16, info.Aaguid.Length);
        Assert.True(info.Options.ResidentKey);
        Assert.True(info.Options.UserPresence);
        Assert.True(info.Options.ClientPinSupported);
        Assert.Equal(2048, info.MaxMsgSize);
        Assert.Contains(2, info.PinUvAuthProtocols);
    }
    
    [Fact]
    public void Options_ClientPinSet_ReturnsCorrectState()
    {
        var cborBytes = BuildGetInfoResponseWithOptions(clientPinSet: true);
        
        var info = AuthenticatorInfo.Decode(cborBytes);
        
        Assert.True(info.Options.ClientPinSupported);
        Assert.True(info.Options.ClientPinSet);
    }
    
    [Fact]
    public void Options_CredentialManagementSupported_ReturnsTrue()
    {
        var cborBytes = BuildGetInfoResponseWithOptions(credMgmt: true);
        
        var info = AuthenticatorInfo.Decode(cborBytes);
        
        Assert.True(info.Options.CredentialManagementSupported);
    }
    
    [Fact]
    public void Parse_FromCborReader_WorksCorrectly()
    {
        var cborBytes = BuildGetInfoResponse();
        var reader = new CborReader(cborBytes, CborConformanceMode.Ctap2Canonical);
        
        var info = AuthenticatorInfo.Parse(reader);
        
        Assert.NotNull(info);
        Assert.NotEmpty(info.Versions);
    }
    
    private static byte[] BuildGetInfoResponse()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(6);
        
        // 0x01: versions
        writer.WriteInt32(0x01);
        writer.WriteStartArray(2);
        writer.WriteTextString("FIDO_2_0");
        writer.WriteTextString("FIDO_2_1");
        writer.WriteEndArray();
        
        // 0x02: extensions
        writer.WriteInt32(0x02);
        writer.WriteStartArray(2);
        writer.WriteTextString("credProtect");
        writer.WriteTextString("hmac-secret");
        writer.WriteEndArray();
        
        // 0x03: aaguid
        writer.WriteInt32(0x03);
        writer.WriteByteString(new byte[16]);
        
        // 0x04: options
        writer.WriteInt32(0x04);
        writer.WriteStartMap(3);
        writer.WriteTextString("rk");
        writer.WriteBoolean(true);
        writer.WriteTextString("up");
        writer.WriteBoolean(true);
        writer.WriteTextString("clientPin");
        writer.WriteBoolean(true);
        writer.WriteEndMap();
        
        // 0x05: maxMsgSize
        writer.WriteInt32(0x05);
        writer.WriteInt32(2048);
        
        // 0x06: pinUvAuthProtocols
        writer.WriteInt32(0x06);
        writer.WriteStartArray(2);
        writer.WriteInt32(2);
        writer.WriteInt32(1);
        writer.WriteEndArray();
        
        writer.WriteEndMap();
        return writer.Encode();
    }
    
    private static byte[] BuildGetInfoResponseWithOptions(
        bool clientPinSet = false, 
        bool credMgmt = false)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(4);
        
        // 0x01: versions
        writer.WriteInt32(0x01);
        writer.WriteStartArray(1);
        writer.WriteTextString("FIDO_2_1");
        writer.WriteEndArray();
        
        // 0x03: aaguid
        writer.WriteInt32(0x03);
        writer.WriteByteString(new byte[16]);
        
        // 0x04: options
        writer.WriteInt32(0x04);
        var optionCount = 1 + (clientPinSet ? 1 : 0) + (credMgmt ? 1 : 0);
        writer.WriteStartMap(optionCount);
        writer.WriteTextString("clientPin");
        writer.WriteBoolean(clientPinSet);
        if (credMgmt)
        {
            writer.WriteTextString("credMgmt");
            writer.WriteBoolean(true);
        }
        writer.WriteEndMap();
        
        // 0x05: maxMsgSize
        writer.WriteInt32(0x05);
        writer.WriteInt32(1024);
        
        writer.WriteEndMap();
        return writer.Encode();
    }
}
```

**Step 2: Implement AuthenticatorInfo with Parse pattern**

```csharp
// Yubico.YubiKit.Fido2/src/Models/AuthenticatorInfo.cs
using System.Formats.Cbor;

namespace Yubico.YubiKit.Fido2.Models;

/// <summary>
/// Contains authenticator information from the CTAP2 getInfo command.
/// </summary>
/// <remarks>
/// See: https://fidoalliance.org/specs/fido-v2.3-rd-20251023/fido-client-to-authenticator-protocol-v2.3-rd-20251023.html#authenticatorGetInfo
/// </remarks>
public sealed class AuthenticatorInfo
{
    // Response map keys
    private const int KeyVersions = 0x01;
    private const int KeyExtensions = 0x02;
    private const int KeyAaguid = 0x03;
    private const int KeyOptions = 0x04;
    private const int KeyMaxMsgSize = 0x05;
    private const int KeyPinUvAuthProtocols = 0x06;
    private const int KeyMaxCredentialCountInList = 0x07;
    private const int KeyMaxCredentialIdLength = 0x08;
    private const int KeyTransports = 0x09;
    private const int KeyAlgorithms = 0x0A;
    private const int KeyMaxSerializedLargeBlobArray = 0x0B;
    private const int KeyForcePinChange = 0x0C;
    private const int KeyMinPinLength = 0x0D;
    private const int KeyFirmwareVersion = 0x0E;
    private const int KeyMaxCredBlobLength = 0x0F;
    private const int KeyMaxRpidsForSetMinPinLength = 0x10;
    private const int KeyPreferredPlatformUvAttempts = 0x11;
    private const int KeyUvModality = 0x12;
    private const int KeyCertifications = 0x13;
    private const int KeyRemainingDiscoverableCredentials = 0x14;
    private const int KeyVendorPrototypeConfigCommands = 0x15;
    private const int KeyAttestationFormats = 0x16;
    
    /// <summary>List of supported CTAP versions.</summary>
    public IReadOnlyList<string> Versions { get; private init; } = [];
    
    /// <summary>List of supported extensions.</summary>
    public IReadOnlyList<string> Extensions { get; private init; } = [];
    
    /// <summary>Authenticator AAGUID (16 bytes).</summary>
    public ReadOnlyMemory<byte> Aaguid { get; private init; }
    
    /// <summary>Authenticator options.</summary>
    public AuthenticatorOptions Options { get; private init; } = new();
    
    /// <summary>Maximum message size in bytes.</summary>
    public int MaxMsgSize { get; private init; } = 1024;
    
    /// <summary>List of supported PIN/UV auth protocol versions.</summary>
    public IReadOnlyList<int> PinUvAuthProtocols { get; private init; } = [];
    
    /// <summary>Maximum number of credentials in allowList or excludeList.</summary>
    public int? MaxCredentialCountInList { get; private init; }
    
    /// <summary>Maximum credential ID length in bytes.</summary>
    public int? MaxCredentialIdLength { get; private init; }
    
    /// <summary>List of supported transports.</summary>
    public IReadOnlyList<string> Transports { get; private init; } = [];
    
    /// <summary>Supported algorithms as (alg, type) pairs.</summary>
    public IReadOnlyList<PublicKeyCredentialParameters> Algorithms { get; private init; } = [];
    
    /// <summary>Maximum serialized large blob array size.</summary>
    public int MaxSerializedLargeBlobArray { get; private init; }
    
    /// <summary>Whether PIN change is required.</summary>
    public bool ForcePinChange { get; private init; }
    
    /// <summary>Minimum PIN length.</summary>
    public int MinPinLength { get; private init; } = 4;
    
    /// <summary>Authenticator firmware version (encoded).</summary>
    public int? FirmwareVersion { get; private init; }
    
    /// <summary>Maximum credBlob length.</summary>
    public int MaxCredBlobLength { get; private init; }
    
    /// <summary>Maximum RP IDs for setMinPinLength.</summary>
    public int MaxRpidsForSetMinPinLength { get; private init; }
    
    /// <summary>Remaining discoverable credentials capacity.</summary>
    public int? RemainingDiscoverableCredentials { get; private init; }
    
    /// <summary>List of supported attestation formats.</summary>
    public IReadOnlyList<string> AttestationFormats { get; private init; } = [];
    
    private AuthenticatorInfo() { }
    
    /// <summary>
    /// Decodes AuthenticatorInfo from CBOR bytes.
    /// </summary>
    public static AuthenticatorInfo Decode(ReadOnlySpan<byte> data)
    {
        var reader = new CborReader(data.ToArray(), CborConformanceMode.Ctap2Canonical);
        return Parse(reader);
    }
    
    /// <summary>
    /// Parses AuthenticatorInfo from a CborReader positioned at a map.
    /// </summary>
    public static AuthenticatorInfo Parse(CborReader reader)
    {
        var info = new AuthenticatorInfo();
        var mapCount = reader.ReadStartMap() ?? 0;
        
        List<string>? versions = null;
        List<string>? extensions = null;
        byte[]? aaguid = null;
        AuthenticatorOptions? options = null;
        int maxMsgSize = 1024;
        List<int>? pinProtocols = null;
        int? maxCredCount = null;
        int? maxCredIdLen = null;
        List<string>? transports = null;
        List<PublicKeyCredentialParameters>? algorithms = null;
        int maxLargeBlob = 0;
        bool forcePinChange = false;
        int minPinLen = 4;
        int? fwVersion = null;
        int maxCredBlob = 0;
        int maxRpids = 0;
        int? remaining = null;
        List<string>? attestFormats = null;
        
        for (var i = 0; i < mapCount; i++)
        {
            var key = reader.ReadInt32();
            
            switch (key)
            {
                case KeyVersions:
                    versions = ParseStringArray(reader);
                    break;
                case KeyExtensions:
                    extensions = ParseStringArray(reader);
                    break;
                case KeyAaguid:
                    aaguid = reader.ReadByteString();
                    break;
                case KeyOptions:
                    options = AuthenticatorOptions.Parse(reader);
                    break;
                case KeyMaxMsgSize:
                    maxMsgSize = reader.ReadInt32();
                    break;
                case KeyPinUvAuthProtocols:
                    pinProtocols = ParseIntArray(reader);
                    break;
                case KeyMaxCredentialCountInList:
                    maxCredCount = reader.ReadInt32();
                    break;
                case KeyMaxCredentialIdLength:
                    maxCredIdLen = reader.ReadInt32();
                    break;
                case KeyTransports:
                    transports = ParseStringArray(reader);
                    break;
                case KeyAlgorithms:
                    algorithms = ParseAlgorithms(reader);
                    break;
                case KeyMaxSerializedLargeBlobArray:
                    maxLargeBlob = reader.ReadInt32();
                    break;
                case KeyForcePinChange:
                    forcePinChange = reader.ReadBoolean();
                    break;
                case KeyMinPinLength:
                    minPinLen = reader.ReadInt32();
                    break;
                case KeyFirmwareVersion:
                    fwVersion = reader.ReadInt32();
                    break;
                case KeyMaxCredBlobLength:
                    maxCredBlob = reader.ReadInt32();
                    break;
                case KeyMaxRpidsForSetMinPinLength:
                    maxRpids = reader.ReadInt32();
                    break;
                case KeyRemainingDiscoverableCredentials:
                    remaining = reader.ReadInt32();
                    break;
                case KeyAttestationFormats:
                    attestFormats = ParseStringArray(reader);
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }
        
        reader.ReadEndMap();
        
        return new AuthenticatorInfo
        {
            Versions = versions ?? throw new InvalidOperationException("Missing versions"),
            Extensions = extensions ?? [],
            Aaguid = aaguid ?? throw new InvalidOperationException("Missing AAGUID"),
            Options = options ?? new AuthenticatorOptions(),
            MaxMsgSize = maxMsgSize,
            PinUvAuthProtocols = pinProtocols ?? [],
            MaxCredentialCountInList = maxCredCount,
            MaxCredentialIdLength = maxCredIdLen,
            Transports = transports ?? [],
            Algorithms = algorithms ?? [],
            MaxSerializedLargeBlobArray = maxLargeBlob,
            ForcePinChange = forcePinChange,
            MinPinLength = minPinLen,
            FirmwareVersion = fwVersion,
            MaxCredBlobLength = maxCredBlob,
            MaxRpidsForSetMinPinLength = maxRpids,
            RemainingDiscoverableCredentials = remaining,
            AttestationFormats = attestFormats ?? []
        };
    }
    
    private static List<string> ParseStringArray(CborReader reader)
    {
        var count = reader.ReadStartArray() ?? 0;
        var list = new List<string>(count);
        for (var i = 0; i < count; i++)
            list.Add(reader.ReadTextString());
        reader.ReadEndArray();
        return list;
    }
    
    private static List<int> ParseIntArray(CborReader reader)
    {
        var count = reader.ReadStartArray() ?? 0;
        var list = new List<int>(count);
        for (var i = 0; i < count; i++)
            list.Add(reader.ReadInt32());
        reader.ReadEndArray();
        return list;
    }
    
    private static List<PublicKeyCredentialParameters> ParseAlgorithms(CborReader reader)
    {
        var count = reader.ReadStartArray() ?? 0;
        var list = new List<PublicKeyCredentialParameters>(count);
        for (var i = 0; i < count; i++)
            list.Add(PublicKeyCredentialParameters.Parse(reader));
        reader.ReadEndArray();
        return list;
    }
}

/// <summary>
/// Authenticator options from getInfo response.
/// </summary>
public sealed class AuthenticatorOptions
{
    public bool PlatformDevice { get; private init; }
    public bool ResidentKey { get; private init; }
    public bool ClientPinSupported { get; private init; }
    public bool? ClientPinSet { get; private init; }
    public bool UserPresence { get; private init; } = true;
    public bool UserVerificationSupported { get; private init; }
    public bool? UserVerificationConfigured { get; private init; }
    public bool PinUvAuthToken { get; private init; }
    public bool NoMcGaPermissionsWithClientPin { get; private init; }
    public bool LargeBlobsSupported { get; private init; }
    public bool EnterpriseAttestation { get; private init; }
    public bool BioEnrollmentSupported { get; private init; }
    public bool? BioEnrollmentConfigured { get; private init; }
    public bool UserVerificationMgmtPreview { get; private init; }
    public bool UvBioEnroll { get; private init; }
    public bool AuthenticatorConfigSupported { get; private init; }
    public bool UvAcfgSupported { get; private init; }
    public bool CredentialManagementSupported { get; private init; }
    public bool CredentialManagementPreview { get; private init; }
    public bool SetMinPinLength { get; private init; }
    public bool MakeCredUvNotRqd { get; private init; }
    public bool AlwaysUv { get; private init; }
    
    internal AuthenticatorOptions() { }
    
    internal static AuthenticatorOptions Parse(CborReader reader)
    {
        var mapCount = reader.ReadStartMap() ?? 0;
        var opts = new AuthenticatorOptions();
        
        bool plat = false, rk = false, clientPinSupp = false;
        bool? clientPinSet = null;
        bool up = true, uvSupp = false;
        bool? uvConfig = null;
        bool pinUvToken = false, noMcGa = false, largeBlobs = false, ea = false;
        bool bioSupp = false;
        bool? bioConfig = null;
        bool uvMgmtPre = false, uvBio = false, authnrCfg = false, uvAcfg = false;
        bool credMgmt = false, credMgmtPre = false, setMinPin = false;
        bool mcUvNotRqd = false, alwaysUv = false;
        
        for (var i = 0; i < mapCount; i++)
        {
            var key = reader.ReadTextString();
            
            switch (key)
            {
                case "plat": plat = reader.ReadBoolean(); break;
                case "rk": rk = reader.ReadBoolean(); break;
                case "clientPin":
                    clientPinSupp = true;
                    clientPinSet = reader.ReadBoolean();
                    break;
                case "up": up = reader.ReadBoolean(); break;
                case "uv":
                    uvSupp = true;
                    uvConfig = reader.ReadBoolean();
                    break;
                case "pinUvAuthToken": pinUvToken = reader.ReadBoolean(); break;
                case "noMcGaPermissionsWithClientPin": noMcGa = reader.ReadBoolean(); break;
                case "largeBlobs": largeBlobs = reader.ReadBoolean(); break;
                case "ep": ea = reader.ReadBoolean(); break;
                case "bioEnroll":
                    bioSupp = true;
                    bioConfig = reader.ReadBoolean();
                    break;
                case "userVerificationMgmtPreview": uvMgmtPre = reader.ReadBoolean(); break;
                case "uvBioEnroll": uvBio = reader.ReadBoolean(); break;
                case "authnrCfg": authnrCfg = reader.ReadBoolean(); break;
                case "uvAcfg": uvAcfg = reader.ReadBoolean(); break;
                case "credMgmt": credMgmt = reader.ReadBoolean(); break;
                case "credentialMgmtPreview": credMgmtPre = reader.ReadBoolean(); break;
                case "setMinPINLength": setMinPin = reader.ReadBoolean(); break;
                case "makeCredUvNotRqd": mcUvNotRqd = reader.ReadBoolean(); break;
                case "alwaysUv": alwaysUv = reader.ReadBoolean(); break;
                default: reader.SkipValue(); break;
            }
        }
        
        reader.ReadEndMap();
        
        return new AuthenticatorOptions
        {
            PlatformDevice = plat,
            ResidentKey = rk,
            ClientPinSupported = clientPinSupp,
            ClientPinSet = clientPinSet,
            UserPresence = up,
            UserVerificationSupported = uvSupp,
            UserVerificationConfigured = uvConfig,
            PinUvAuthToken = pinUvToken,
            NoMcGaPermissionsWithClientPin = noMcGa,
            LargeBlobsSupported = largeBlobs,
            EnterpriseAttestation = ea,
            BioEnrollmentSupported = bioSupp,
            BioEnrollmentConfigured = bioConfig,
            UserVerificationMgmtPreview = uvMgmtPre,
            UvBioEnroll = uvBio,
            AuthenticatorConfigSupported = authnrCfg,
            UvAcfgSupported = uvAcfg,
            CredentialManagementSupported = credMgmt,
            CredentialManagementPreview = credMgmtPre,
            SetMinPinLength = setMinPin,
            MakeCredUvNotRqd = mcUvNotRqd,
            AlwaysUv = alwaysUv
        };
    }
}

/// <summary>
/// Public key credential parameters (algorithm type).
/// </summary>
public readonly record struct PublicKeyCredentialParameters(
    Yubico.YubiKit.Core.Cryptography.Cose.CoseAlgorithmIdentifier Algorithm,
    string Type = "public-key")
{
    internal static PublicKeyCredentialParameters Parse(CborReader reader)
    {
        reader.ReadStartMap();
        string type = "public-key";
        int alg = 0;
        
        while (reader.PeekState() != CborReaderState.EndMap)
        {
            var key = reader.ReadTextString();
            switch (key)
            {
                case "type": type = reader.ReadTextString(); break;
                case "alg": alg = reader.ReadInt32(); break;
                default: reader.SkipValue(); break;
            }
        }
        reader.ReadEndMap();
        
        return new PublicKeyCredentialParameters(
            (Yubico.YubiKit.Core.Cryptography.Cose.CoseAlgorithmIdentifier)alg, 
            type);
    }
}
```

**Step 3: Run tests**

```bash
dotnet test Yubico.YubiKit.Fido2/tests --filter "FullyQualifiedName~AuthenticatorInfoTests"
```
Expected: All PASS

**Step 4: Commit**

```bash
git add Yubico.YubiKit.Fido2/src/Models/
git commit -m "feat(fido2): add AuthenticatorInfo model with Parse(CborReader) pattern"
```

---

### Task 3.3: FidoSession Core Implementation (No Caching)

**Design Decision:** Per user feedback, FidoSession does NOT cache `AuthenticatorInfo`. The `GetInfoAsync()` method always fetches fresh data from the device. This ensures callers always get current state (e.g., after PIN changes, credential additions).

**Files:**
- Modify: `Yubico.YubiKit.Fido2/src/FidoSession.cs`
- Modify: `Yubico.YubiKit.Fido2/src/IFidoSession.cs`
- Create: `Yubico.YubiKit.Fido2/src/FidoBackend.cs`
- Create: `Yubico.YubiKit.Fido2/tests/Unit/FidoSessionTests.cs`

**Reference:** `Ctap2Session.java` constructor and sendCbor method

**Step 1: Create backend abstraction**

```csharp
// Yubico.YubiKit.Fido2/src/FidoBackend.cs
using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Fido2;

/// <summary>
/// Backend abstraction for FIDO communication over different transports.
/// </summary>
internal interface IFidoBackend : IDisposable
{
    /// <summary>
    /// Sends a CTAP2 CBOR command and returns the response.
    /// </summary>
    Task<byte[]> SendCborAsync(
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default);
}

internal sealed class SmartCardFidoBackend : IFidoBackend
{
    private readonly ISmartCardProtocol _protocol;
    private bool _disposed;
    
    public SmartCardFidoBackend(ISmartCardProtocol protocol)
    {
        _protocol = protocol;
    }
    
    public async Task<byte[]> SendCborAsync(
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        // FIDO2 over CCID uses NFCCTAP_MSG (0x10) INS
        var response = await _protocol.TransmitAsync(
            cla: 0x80,
            ins: 0x10,
            p1: 0x00,
            p2: 0x00,
            data: data,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        
        return response.ToArray();
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _protocol.Dispose();
    }
}

internal sealed class FidoHidBackend : IFidoBackend
{
    private readonly IFidoHidProtocol _protocol;
    private bool _disposed;
    
    public FidoHidBackend(IFidoHidProtocol protocol)
    {
        _protocol = protocol;
    }
    
    public async Task<byte[]> SendCborAsync(
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        var response = await _protocol.SendCborAsync(data, cancellationToken)
            .ConfigureAwait(false);
        return response.ToArray();
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _protocol.Dispose();
    }
}
```

**Step 2: Implement FidoSession without caching**

```csharp
// Yubico.YubiKit.Fido2/src/FidoSession.cs
using System.Formats.Cbor;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Fido2.Cbor;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Exceptions;
using Yubico.YubiKit.Fido2.Models;

namespace Yubico.YubiKit.Fido2;

/// <summary>
/// Provides FIDO2/CTAP2 session operations for YubiKey authenticators.
/// </summary>
/// <remarks>
/// <para>
/// Implements CTAP 2.3 specification.
/// See: https://fidoalliance.org/specs/fido-v2.3-rd-20251023/fido-client-to-authenticator-protocol-v2.3-rd-20251023.html
/// </para>
/// <para>
/// Note: This session does NOT cache authenticator info. Call <see cref="GetInfoAsync"/> 
/// to get current authenticator state. This ensures accurate state after operations
/// that modify authenticator configuration (PIN changes, credential additions, etc.).
/// </para>
/// </remarks>
public sealed class FidoSession : ApplicationSession, IFidoSession
{
    private readonly ILogger _logger;
    private readonly IFidoBackend _backend;
    private int _maxMsgSize = 1024;  // Only cache maxMsgSize for request validation
    
    private FidoSession(IFidoBackend backend)
    {
        _backend = backend;
        _logger = Logger;
    }
    
    /// <summary>
    /// Creates a new FIDO session from a connection.
    /// </summary>
    /// <param name="connection">The YubiKey connection (SmartCard or FIDO HID).</param>
    /// <param name="configuration">Optional protocol configuration.</param>
    /// <param name="scpKeyParams">Optional SCP key parameters (SmartCard only).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task<FidoSession> CreateAsync(
        IConnection connection,
        ProtocolConfiguration? configuration = null,
        ScpKeyParameters? scpKeyParams = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        
        var (backend, protocol) = connection switch
        {
            ISmartCardConnection sc => CreateSmartCardBackend(sc, scpKeyParams),
            IFidoHidConnection fido => CreateFidoHidBackend(fido),
            _ => throw new NotSupportedException(
                $"Connection type {connection.GetType().Name} is not supported. " +
                "Use ISmartCardConnection or IFidoHidConnection.")
        };
        
        var session = new FidoSession(backend);
        await session.InitializeAsync(protocol, configuration, scpKeyParams, cancellationToken)
            .ConfigureAwait(false);
        
        return session;
    }
    
    private async Task InitializeAsync(
        IProtocol protocol,
        ProtocolConfiguration? configuration,
        ScpKeyParameters? scpKeyParams,
        CancellationToken cancellationToken)
    {
        if (IsInitialized)
            return;
        
        // Fetch info once to get maxMsgSize and firmware version for session init
        var info = await GetInfoAsync(cancellationToken).ConfigureAwait(false);
        _maxMsgSize = info.MaxMsgSize;
        
        // Extract firmware version from info if available
        var firmwareVersion = info.FirmwareVersion.HasValue
            ? new FirmwareVersion(
                (info.FirmwareVersion.Value >> 16) & 0xFF,
                (info.FirmwareVersion.Value >> 8) & 0xFF,
                info.FirmwareVersion.Value & 0xFF)
            : new FirmwareVersion();
        
        await InitializeCoreAsync(protocol, firmwareVersion, configuration, scpKeyParams, cancellationToken)
            .ConfigureAwait(false);
        
        _logger.LogDebug("FIDO session initialized. Versions: {Versions}", 
            string.Join(", ", info.Versions));
    }
    
    /// <summary>
    /// Gets authenticator information. Always fetches fresh data from the device.
    /// </summary>
    public async Task<AuthenticatorInfo> GetInfoAsync(CancellationToken cancellationToken = default)
    {
        var request = CtapRequestBuilder.Create(CtapCommand.GetInfo).Build();
        var response = await SendRawAsync(request, cancellationToken).ConfigureAwait(false);
        return AuthenticatorInfo.Decode(response);
    }
    
    /// <summary>
    /// Requests the user to select this authenticator (touch).
    /// </summary>
    public async Task SelectionAsync(CancellationToken cancellationToken = default)
    {
        var request = CtapRequestBuilder.Create(CtapCommand.Selection).Build();
        await SendRawAsync(request, cancellationToken).ConfigureAwait(false);
    }
    
    /// <summary>
    /// Resets the FIDO application.
    /// </summary>
    /// <remarks>
    /// Over USB, this must be called within a few seconds of plugging in the YubiKey
    /// and requires touch confirmation.
    /// </remarks>
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        var request = CtapRequestBuilder.Create(CtapCommand.Reset).Build();
        await SendRawAsync(request, cancellationToken).ConfigureAwait(false);
        _logger.LogWarning("FIDO application reset - all credentials deleted");
    }
    
    /// <summary>
    /// Sends a raw CTAP2 command and returns the response (after status byte).
    /// </summary>
    /// <param name="request">The complete CTAP2 request (command byte + optional CBOR payload).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response data (without status byte), or empty if no data.</returns>
    internal async Task<ReadOnlyMemory<byte>> SendRawAsync(
        ReadOnlyMemory<byte> request,
        CancellationToken cancellationToken = default)
    {
        // Validate message size (except for GetInfo which uses default)
        var command = request.Span[0];
        var maxSize = command == CtapCommand.GetInfo ? 1024 : _maxMsgSize;
        if (request.Length > maxSize)
        {
            _logger.LogError("Message size {Size} exceeds max {Max}", request.Length, maxSize);
            throw new CtapException(CtapError.RequestTooLarge);
        }
        
        var response = await _backend.SendCborAsync(request, cancellationToken).ConfigureAwait(false);
        
        // First byte is status
        if (response.Length < 1)
            throw new CtapException(CtapError.Other, "Empty response");
        
        var status = response[0];
        if (status != (byte)CtapError.Success)
            throw new CtapException(status);
        
        // Return response without status byte
        return response.Length > 1 
            ? response.AsMemory(1) 
            : ReadOnlyMemory<byte>.Empty;
    }
    
    private static (IFidoBackend backend, IProtocol protocol) CreateSmartCardBackend(
        ISmartCardConnection connection,
        ScpKeyParameters? scpKeyParams)
    {
        var protocol = PcscProtocolFactory<ISmartCardConnection>
            .Create()
            .Create(connection);
        
        var backend = new SmartCardFidoBackend(
            protocol as ISmartCardProtocol ?? throw new InvalidOperationException());
        
        return (backend, protocol);
    }
    
    private static (IFidoBackend backend, IProtocol protocol) CreateFidoHidBackend(
        IFidoHidConnection connection)
    {
        var protocol = FidoProtocolFactory
            .Create()
            .Create(connection);
        
        var backend = new FidoHidBackend(protocol);
        return (backend, protocol);
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _backend.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

**Step 3: Update interface**

```csharp
// Yubico.YubiKit.Fido2/src/IFidoSession.cs
using Yubico.YubiKit.Fido2.Models;

namespace Yubico.YubiKit.Fido2;

/// <summary>
/// Interface for FIDO2/CTAP2 session operations.
/// </summary>
public interface IFidoSession : IDisposable
{
    /// <summary>
    /// Gets whether the session is initialized.
    /// </summary>
    bool IsInitialized { get; }
    
    /// <summary>
    /// Gets authenticator information. Always fetches fresh data from the device.
    /// </summary>
    Task<AuthenticatorInfo> GetInfoAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Requests the user to select this authenticator (touch).
    /// </summary>
    Task SelectionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Resets the FIDO application.
    /// </summary>
    Task ResetAsync(CancellationToken cancellationToken = default);
}
```

**Step 4: Commit**

```bash
git add Yubico.YubiKit.Fido2/src/
git commit -m "feat(fido2): implement FidoSession without info caching"
```

---

## Phase 4: PIN/UV Auth Protocols

### Task 4.1: PIN/UV Auth Protocol Interface and V2 Implementation

**Context:** CTAP2 uses PIN/UV auth protocols to authenticate commands. V2 is the current standard.

**Files:**
- Create: `Yubico.YubiKit.Fido2/src/Pin/IPinUvAuthProtocol.cs`
- Create: `Yubico.YubiKit.Fido2/src/Pin/PinUvAuthProtocolV2.cs`
- Create: `Yubico.YubiKit.Fido2/tests/Unit/Pin/PinUvAuthProtocolV2Tests.cs`

**Reference:** `PinUvAuthProtocol.java`, `PinUvAuthProtocolV2.java`

**Step 1: Create interface**

```csharp
// Yubico.YubiKit.Fido2/src/Pin/IPinUvAuthProtocol.cs
namespace Yubico.YubiKit.Fido2.Pin;

/// <summary>
/// PIN/UV authentication protocol for CTAP2.
/// </summary>
public interface IPinUvAuthProtocol
{
    /// <summary>
    /// Gets the protocol version number (1 or 2).
    /// </summary>
    int Version { get; }
    
    /// <summary>
    /// Generates key agreement and shared secret from authenticator's public key.
    /// </summary>
    /// <param name="peerCoseKey">The authenticator's COSE public key.</param>
    /// <returns>A tuple of (keyAgreement COSE map, sharedSecret).</returns>
    (Dictionary<int, object?> KeyAgreement, byte[] SharedSecret) Encapsulate(
        IDictionary<int, object?> peerCoseKey);
    
    /// <summary>
    /// Derives the shared secret using KDF.
    /// </summary>
    byte[] Kdf(ReadOnlySpan<byte> z);
    
    /// <summary>
    /// Encrypts plaintext using the shared secret.
    /// </summary>
    byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> plaintext);
    
    /// <summary>
    /// Decrypts ciphertext using the shared secret.
    /// </summary>
    byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> ciphertext);
    
    /// <summary>
    /// Computes authentication tag (MAC) for a message.
    /// </summary>
    byte[] Authenticate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message);
}
```

**Step 2: Implement V2 protocol**

```csharp
// Yubico.YubiKit.Fido2/src/Pin/PinUvAuthProtocolV2.cs
using System.Security.Cryptography;

namespace Yubico.YubiKit.Fido2.Pin;

/// <summary>
/// PIN/UV authentication protocol version 2 (HKDF-SHA-256, AES-256-CBC).
/// </summary>
public sealed class PinUvAuthProtocolV2 : IPinUvAuthProtocol
{
    private const int AesKeyLength = 32;
    private const int AesBlockSize = 16;
    private const int HmacKeyLength = 32;
    
    public int Version => 2;
    
    public (Dictionary<int, object?> KeyAgreement, byte[] SharedSecret) Encapsulate(
        IDictionary<int, object?> peerCoseKey)
    {
        ArgumentNullException.ThrowIfNull(peerCoseKey);
        
        // Generate ephemeral ECDH key pair
        using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var ourParams = ecdh.ExportParameters(true);
        
        // Import peer's public key
        var peerX = (byte[])peerCoseKey[-2]!;
        var peerY = (byte[])peerCoseKey[-3]!;
        
        using var peerEcdh = ECDiffieHellman.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = peerX, Y = peerY }
        });
        
        // Derive shared secret
        var z = ecdh.DeriveRawSecretAgreement(peerEcdh.PublicKey);
        var sharedSecret = Kdf(z);
        
        // Build our COSE key for key agreement
        var keyAgreement = new Dictionary<int, object?>
        {
            { 1, 2 },   // kty: EC2
            { 3, -25 }, // alg: ECDH-ES+HKDF-256
            { -1, 1 },  // crv: P-256
            { -2, ourParams.Q.X },
            { -3, ourParams.Q.Y }
        };
        
        return (keyAgreement, sharedSecret);
    }
    
    public byte[] Kdf(ReadOnlySpan<byte> z)
    {
        // HKDF-SHA-256 with no salt, info = "CTAP2 HMAC key" || "CTAP2 AES key"
        Span<byte> result = stackalloc byte[HmacKeyLength + AesKeyLength];
        
        // Derive HMAC key
        HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            z,
            result[..HmacKeyLength],
            salt: [],
            info: "CTAP2 HMAC key"u8);
        
        // Derive AES key
        HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            z,
            result[HmacKeyLength..],
            salt: [],
            info: "CTAP2 AES key"u8);
        
        return result.ToArray();
    }
    
    public byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> plaintext)
    {
        if (plaintext.Length % AesBlockSize != 0)
            throw new ArgumentException("Plaintext must be a multiple of AES block size");
        
        var aesKey = key[HmacKeyLength..];
        
        using var aes = Aes.Create();
        aes.Key = aesKey.ToArray();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;
        
        // Generate random IV
        var iv = RandomNumberGenerator.GetBytes(AesBlockSize);
        aes.IV = iv;
        
        using var encryptor = aes.CreateEncryptor();
        var ciphertext = new byte[plaintext.Length];
        encryptor.TransformBlock(plaintext.ToArray(), 0, plaintext.Length, ciphertext, 0);
        
        // Return IV || ciphertext
        var result = new byte[AesBlockSize + ciphertext.Length];
        iv.CopyTo(result, 0);
        ciphertext.CopyTo(result, AesBlockSize);
        
        return result;
    }
    
    public byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> ciphertext)
    {
        if (ciphertext.Length < AesBlockSize || ciphertext.Length % AesBlockSize != 0)
            throw new ArgumentException("Invalid ciphertext length");
        
        var aesKey = key[HmacKeyLength..];
        var iv = ciphertext[..AesBlockSize];
        var encrypted = ciphertext[AesBlockSize..];
        
        using var aes = Aes.Create();
        aes.Key = aesKey.ToArray();
        aes.IV = iv.ToArray();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;
        
        using var decryptor = aes.CreateDecryptor();
        var plaintext = new byte[encrypted.Length];
        decryptor.TransformBlock(encrypted.ToArray(), 0, encrypted.Length, plaintext, 0);
        
        return plaintext;
    }
    
    public byte[] Authenticate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message)
    {
        var hmacKey = key[..HmacKeyLength];
        
        Span<byte> hash = stackalloc byte[32];
        HMACSHA256.HashData(hmacKey, message, hash);
        
        return hash.ToArray();
    }
}
```

**Step 3: Write tests**

```csharp
// Yubico.YubiKit.Fido2/tests/Unit/Pin/PinUvAuthProtocolV2Tests.cs
using System.Security.Cryptography;
using Yubico.YubiKit.Fido2.Pin;

namespace Yubico.YubiKit.Fido2.Tests.Unit.Pin;

public class PinUvAuthProtocolV2Tests
{
    [Fact]
    public void Version_Returns2()
    {
        var protocol = new PinUvAuthProtocolV2();
        Assert.Equal(2, protocol.Version);
    }
    
    [Fact]
    public void Encapsulate_ValidPeerKey_ReturnsKeyAgreementAndSecret()
    {
        var protocol = new PinUvAuthProtocolV2();
        
        // Generate a peer key
        using var peerEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var peerParams = peerEcdh.ExportParameters(false);
        
        var peerCoseKey = new Dictionary<int, object?>
        {
            { 1, 2 },
            { -1, 1 },
            { -2, peerParams.Q.X },
            { -3, peerParams.Q.Y }
        };
        
        var (keyAgreement, sharedSecret) = protocol.Encapsulate(peerCoseKey);
        
        Assert.NotNull(keyAgreement);
        Assert.NotNull(sharedSecret);
        Assert.Equal(64, sharedSecret.Length); // HMAC key (32) + AES key (32)
        Assert.Equal(2, keyAgreement[1]); // kty = EC2
    }
    
    [Fact]
    public void EncryptDecrypt_RoundTrip_ReturnsOriginal()
    {
        var protocol = new PinUvAuthProtocolV2();
        var key = RandomNumberGenerator.GetBytes(64); // HMAC + AES key
        var plaintext = new byte[64]; // Must be multiple of 16
        RandomNumberGenerator.Fill(plaintext);
        
        var ciphertext = protocol.Encrypt(key, plaintext);
        var decrypted = protocol.Decrypt(key, ciphertext);
        
        Assert.Equal(plaintext, decrypted);
    }
    
    [Fact]
    public void Authenticate_ProducesConsistentMac()
    {
        var protocol = new PinUvAuthProtocolV2();
        var key = RandomNumberGenerator.GetBytes(64);
        var message = "test message"u8.ToArray();
        
        var mac1 = protocol.Authenticate(key, message);
        var mac2 = protocol.Authenticate(key, message);
        
        Assert.Equal(mac1, mac2);
        Assert.Equal(32, mac1.Length);
    }
}
```

**Step 4: Run tests and commit**

```bash
dotnet test Yubico.YubiKit.Fido2/tests --filter "FullyQualifiedName~PinUvAuthProtocol"
git add Yubico.YubiKit.Fido2/src/Pin/ Yubico.YubiKit.Fido2/tests/Unit/Pin/
git commit -m "feat(fido2): add PIN/UV auth protocol V2 implementation"
```

---

### Task 4.2: ClientPin Class

**Files:**
- Create: `Yubico.YubiKit.Fido2/src/Pin/ClientPin.cs`
- Create: `Yubico.YubiKit.Fido2/src/Pin/PinRetries.cs`
- Create: `Yubico.YubiKit.Fido2/src/Pin/PinPermission.cs`

**Reference:** `ClientPin.java`

```csharp
// Yubico.YubiKit.Fido2/src/Pin/PinPermission.cs
namespace Yubico.YubiKit.Fido2.Pin;

/// <summary>
/// PIN/UV auth token permissions.
/// </summary>
[Flags]
public enum PinPermission
{
    None = 0x00,
    MakeCredential = 0x01,
    GetAssertion = 0x02,
    CredentialManagement = 0x04,
    BioEnrollment = 0x08,
    LargeBlobWrite = 0x10,
    AuthenticatorConfig = 0x20,
    PerCredentialMgmtReadOnly = 0x40
}
```

```csharp
// Yubico.YubiKit.Fido2/src/Pin/PinRetries.cs
namespace Yubico.YubiKit.Fido2.Pin;

/// <summary>
/// PIN retry information.
/// </summary>
public readonly record struct PinRetries(int Count, bool? PowerCycleState);
```

```csharp
// Yubico.YubiKit.Fido2/src/Pin/ClientPin.cs
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Fido2.Ctap;

namespace Yubico.YubiKit.Fido2.Pin;

/// <summary>
/// Implements CTAP2 Client PIN commands.
/// </summary>
public sealed class ClientPin
{
    private const byte CmdGetRetries = 0x01;
    private const byte CmdGetKeyAgreement = 0x02;
    private const byte CmdSetPin = 0x03;
    private const byte CmdChangePin = 0x04;
    private const byte CmdGetPinToken = 0x05;
    private const byte CmdGetPinTokenUsingUvWithPermissions = 0x06;
    private const byte CmdGetUvRetries = 0x07;
    private const byte CmdGetPinTokenUsingPinWithPermissions = 0x09;
    
    private const int ResultKeyAgreement = 0x01;
    private const int ResultPinUvToken = 0x02;
    private const int ResultRetries = 0x03;
    private const int ResultPowerCycleState = 0x04;
    private const int ResultUvRetries = 0x05;
    
    private const int MinPinLength = 4;
    private const int MaxPinLength = 63;
    private const int PinBufferLength = 64;
    private const int PinHashLength = 16;
    
    private readonly FidoSession _session;
    private readonly IPinUvAuthProtocol _pinUvAuth;
    private readonly ILogger _logger;
    
    /// <summary>
    /// Creates a ClientPin instance.
    /// </summary>
    public ClientPin(FidoSession session, IPinUvAuthProtocol pinUvAuth)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _pinUvAuth = pinUvAuth ?? throw new ArgumentNullException(nameof(pinUvAuth));
        _logger = YubiKitLogging.CreateLogger<ClientPin>();
        
        if (!session.Info.SupportsClientPin)
            throw new InvalidOperationException("Client PIN not supported");
    }
    
    /// <summary>
    /// Gets the PIN/UV auth protocol in use.
    /// </summary>
    public IPinUvAuthProtocol PinUvAuth => _pinUvAuth;
    
    /// <summary>
    /// Gets PIN retry information.
    /// </summary>
    public async Task<PinRetries> GetPinRetriesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting PIN retries");
        
        var result = await _session.SendCborAsync(
            CtapCommand.ClientPin,
            new Dictionary<int, object?>
            {
                { 1, _pinUvAuth.Version },
                { 2, CmdGetRetries }
            },
            cancellationToken).ConfigureAwait(false);
        
        return new PinRetries(
            (int)result[ResultRetries]!,
            result.TryGetValue(ResultPowerCycleState, out var pcs) ? (bool?)pcs : null);
    }
    
    /// <summary>
    /// Gets UV retry count.
    /// </summary>
    public async Task<int> GetUvRetriesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting UV retries");
        
        var result = await _session.SendCborAsync(
            CtapCommand.ClientPin,
            new Dictionary<int, object?>
            {
                { 1, _pinUvAuth.Version },
                { 2, CmdGetUvRetries }
            },
            cancellationToken).ConfigureAwait(false);
        
        return (int)result[ResultUvRetries]!;
    }
    
    /// <summary>
    /// Gets a shared secret from the authenticator.
    /// </summary>
    public async Task<(Dictionary<int, object?> KeyAgreement, byte[] SharedSecret)> GetSharedSecretAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting shared secret");
        
        var result = await _session.SendCborAsync(
            CtapCommand.ClientPin,
            new Dictionary<int, object?>
            {
                { 1, _pinUvAuth.Version },
                { 2, CmdGetKeyAgreement }
            },
            cancellationToken).ConfigureAwait(false);
        
        var peerCoseKey = (IDictionary<int, object?>)result[ResultKeyAgreement]!;
        return _pinUvAuth.Encapsulate(peerCoseKey);
    }
    
    /// <summary>
    /// Sets the PIN on an authenticator with no PIN currently set.
    /// </summary>
    public async Task SetPinAsync(string pin, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(pin);
        ValidatePinLength(pin);
        
        var (keyAgreement, sharedSecret) = await GetSharedSecretAsync(cancellationToken)
            .ConfigureAwait(false);
        
        var pinBytes = PreparePin(pin, pad: true);
        var pinEnc = _pinUvAuth.Encrypt(sharedSecret, pinBytes);
        var pinUvAuthParam = _pinUvAuth.Authenticate(sharedSecret, pinEnc);
        
        _logger.LogDebug("Setting PIN");
        
        await _session.SendCborAsync(
            CtapCommand.ClientPin,
            new Dictionary<int, object?>
            {
                { 1, _pinUvAuth.Version },
                { 2, CmdSetPin },
                { 3, keyAgreement },
                { 4, pinUvAuthParam },
                { 5, pinEnc }
            },
            cancellationToken).ConfigureAwait(false);
        
        _logger.LogInformation("PIN set successfully");
    }
    
    /// <summary>
    /// Changes the PIN.
    /// </summary>
    public async Task ChangePinAsync(
        string currentPin,
        string newPin,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(currentPin);
        ArgumentException.ThrowIfNullOrEmpty(newPin);
        ValidatePinLength(newPin);
        
        var (keyAgreement, sharedSecret) = await GetSharedSecretAsync(cancellationToken)
            .ConfigureAwait(false);
        
        var currentPinHash = ComputePinHash(currentPin);
        var pinHashEnc = _pinUvAuth.Encrypt(sharedSecret, currentPinHash);
        
        var newPinBytes = PreparePin(newPin, pad: true);
        var newPinEnc = _pinUvAuth.Encrypt(sharedSecret, newPinBytes);
        
        // Authenticate over newPinEnc || pinHashEnc
        var authData = new byte[newPinEnc.Length + pinHashEnc.Length];
        newPinEnc.CopyTo(authData, 0);
        pinHashEnc.CopyTo(authData, newPinEnc.Length);
        var pinUvAuthParam = _pinUvAuth.Authenticate(sharedSecret, authData);
        
        _logger.LogDebug("Changing PIN");
        
        await _session.SendCborAsync(
            CtapCommand.ClientPin,
            new Dictionary<int, object?>
            {
                { 1, _pinUvAuth.Version },
                { 2, CmdChangePin },
                { 3, keyAgreement },
                { 4, pinUvAuthParam },
                { 5, newPinEnc },
                { 6, pinHashEnc }
            },
            cancellationToken).ConfigureAwait(false);
        
        _logger.LogInformation("PIN changed successfully");
    }
    
    /// <summary>
    /// Gets a PIN token for authenticating subsequent commands.
    /// </summary>
    public async Task<byte[]> GetPinTokenAsync(
        string pin,
        PinPermission permissions = PinPermission.None,
        string? rpId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(pin);
        
        var (keyAgreement, sharedSecret) = await GetSharedSecretAsync(cancellationToken)
            .ConfigureAwait(false);
        
        var pinHash = ComputePinHash(pin);
        var pinHashEnc = _pinUvAuth.Encrypt(sharedSecret, pinHash);
        
        var usePermissions = _session.Info.SupportsPinUvAuthToken;
        var subCommand = usePermissions
            ? CmdGetPinTokenUsingPinWithPermissions
            : CmdGetPinToken;
        
        _logger.LogDebug("Getting PIN token");
        
        var request = new Dictionary<int, object?>
        {
            { 1, _pinUvAuth.Version },
            { 2, subCommand },
            { 3, keyAgreement },
            { 6, pinHashEnc }
        };
        
        if (usePermissions && permissions != PinPermission.None)
            request[9] = (int)permissions;
        
        if (usePermissions && rpId is not null)
            request[10] = rpId;
        
        var result = await _session.SendCborAsync(CtapCommand.ClientPin, request, cancellationToken)
            .ConfigureAwait(false);
        
        var pinTokenEnc = (byte[])result[ResultPinUvToken]!;
        var pinToken = _pinUvAuth.Decrypt(sharedSecret, pinTokenEnc);
        
        _logger.LogDebug("Got PIN token for permissions: {Permissions}", permissions);
        return pinToken;
    }
    
    /// <summary>
    /// Gets a UV token using built-in user verification.
    /// </summary>
    public async Task<byte[]> GetUvTokenAsync(
        PinPermission permissions = PinPermission.None,
        string? rpId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_session.Info.SupportsPinUvAuthToken)
            throw new InvalidOperationException("PIN/UV auth token not supported");
        
        var (keyAgreement, sharedSecret) = await GetSharedSecretAsync(cancellationToken)
            .ConfigureAwait(false);
        
        _logger.LogDebug("Getting UV token");
        
        var request = new Dictionary<int, object?>
        {
            { 1, _pinUvAuth.Version },
            { 2, CmdGetPinTokenUsingUvWithPermissions },
            { 3, keyAgreement }
        };
        
        if (permissions != PinPermission.None)
            request[9] = (int)permissions;
        
        if (rpId is not null)
            request[10] = rpId;
        
        var result = await _session.SendCborAsync(CtapCommand.ClientPin, request, cancellationToken)
            .ConfigureAwait(false);
        
        var pinTokenEnc = (byte[])result[ResultPinUvToken]!;
        return _pinUvAuth.Decrypt(sharedSecret, pinTokenEnc);
    }
    
    private static void ValidatePinLength(string pin)
    {
        if (pin.Length < MinPinLength)
            throw new ArgumentException($"PIN must be at least {MinPinLength} characters", nameof(pin));
        
        var byteLength = Encoding.UTF8.GetByteCount(pin);
        if (byteLength > MaxPinLength)
            throw new ArgumentException($"PIN must be no more than {MaxPinLength} bytes", nameof(pin));
    }
    
    private static byte[] PreparePin(string pin, bool pad)
    {
        var bytes = Encoding.UTF8.GetBytes(pin);
        if (!pad) return bytes;
        
        var padded = new byte[PinBufferLength];
        bytes.CopyTo(padded, 0);
        return padded;
    }
    
    private static byte[] ComputePinHash(string pin)
    {
        var pinBytes = Encoding.UTF8.GetBytes(pin);
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(pinBytes, hash);
        return hash[..PinHashLength].ToArray();
    }
}
```

**Step 5: Commit**

```bash
git add Yubico.YubiKit.Fido2/src/Pin/
git commit -m "feat(fido2): add ClientPin for PIN management"
```

---

## Phase 5: MakeCredential & GetAssertion

### Task 5.1: Credential and Assertion Data Models

**Files:**
- Create: `Yubico.YubiKit.Fido2/src/Models/CredentialData.cs`
- Create: `Yubico.YubiKit.Fido2/src/Models/AssertionData.cs`
- Create: `Yubico.YubiKit.Fido2/src/Models/AuthenticatorData.cs`

*(Detailed implementation follows same TDD pattern - abbreviated for length)*

### Task 5.2: MakeCredential Implementation

Add to `FidoSession.cs`:

```csharp
/// <summary>
/// Creates a new credential.
/// </summary>
public async Task<CredentialData> MakeCredentialAsync(
    byte[] clientDataHash,
    Dictionary<string, object?> rp,
    Dictionary<string, object?> user,
    List<Dictionary<string, object?>> pubKeyCredParams,
    MakeCredentialOptions? options = null,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(clientDataHash);
    ArgumentNullException.ThrowIfNull(rp);
    ArgumentNullException.ThrowIfNull(user);
    ArgumentNullException.ThrowIfNull(pubKeyCredParams);
    
    var request = new Dictionary<int, object?>
    {
        { 1, clientDataHash },
        { 2, rp },
        { 3, user },
        { 4, pubKeyCredParams }
    };
    
    if (options?.ExcludeList is { Count: > 0 })
        request[5] = options.ExcludeList;
    
    if (options?.Extensions is { Count: > 0 })
        request[6] = options.Extensions;
    
    if (options?.Options is { Count: > 0 })
        request[7] = options.Options;
    
    if (options?.PinUvAuthParam is not null)
    {
        request[8] = options.PinUvAuthParam;
        request[9] = options.PinUvAuthProtocol;
    }
    
    if (options?.EnterpriseAttestation.HasValue == true)
        request[10] = options.EnterpriseAttestation.Value;
    
    _logger.LogDebug("Making credential for RP: {RpId}", rp.GetValueOrDefault("id"));
    
    var result = await SendCborAsync(CtapCommand.MakeCredential, request, cancellationToken)
        .ConfigureAwait(false);
    
    _logger.LogInformation("Credential created");
    return CredentialData.FromMap(result);
}
```

### Task 5.3: GetAssertion Implementation

```csharp
/// <summary>
/// Gets assertions for a credential.
/// </summary>
public async Task<List<AssertionData>> GetAssertionsAsync(
    string rpId,
    byte[] clientDataHash,
    GetAssertionOptions? options = null,
    CancellationToken cancellationToken = default)
{
    ArgumentException.ThrowIfNullOrEmpty(rpId);
    ArgumentNullException.ThrowIfNull(clientDataHash);
    
    var request = new Dictionary<int, object?>
    {
        { 1, rpId },
        { 2, clientDataHash }
    };
    
    if (options?.AllowList is { Count: > 0 })
        request[3] = options.AllowList;
    
    if (options?.Extensions is { Count: > 0 })
        request[4] = options.Extensions;
    
    if (options?.Options is { Count: > 0 })
        request[5] = options.Options;
    
    if (options?.PinUvAuthParam is not null)
    {
        request[6] = options.PinUvAuthParam;
        request[7] = options.PinUvAuthProtocol;
    }
    
    _logger.LogDebug("Getting assertions for RP: {RpId}", rpId);
    
    var result = await SendCborAsync(CtapCommand.GetAssertion, request, cancellationToken)
        .ConfigureAwait(false);
    
    var assertions = new List<AssertionData> { AssertionData.FromMap(result) };
    
    // Get additional assertions if available
    if (result.TryGetValue(5, out var nCreds) && nCreds is int credCount && credCount > 1)
    {
        for (var i = 1; i < credCount; i++)
        {
            var nextResult = await SendCborAsync(CtapCommand.GetNextAssertion, null, cancellationToken)
                .ConfigureAwait(false);
            assertions.Add(AssertionData.FromMap(nextResult));
        }
    }
    
    _logger.LogInformation("Got {Count} assertions", assertions.Count);
    return assertions;
}
```

---

## Phase 6-10: Additional Features (Summarized)

### Phase 6: WebAuthn Data Models
- `PublicKeyCredentialDescriptor`
- `PublicKeyCredentialParameters`
- `PublicKeyCredentialUserEntity`
- `PublicKeyCredentialRpEntity`
- High-level creation/assertion request builders

### Phase 7: CredentialManagement
- `CredentialManagement.cs` - enumerate/delete discoverable credentials
- Port from `CredentialManagement.java`

### Phase 8: BioEnrollment
- `BioEnrollment.cs` - base class
- `FingerprintBioEnrollment.cs` - fingerprint enrollment
- Port from `BioEnrollment.java`, `FingerprintBioEnrollment.java`

### Phase 9: Config Commands
- `FidoConfig.cs` - authenticator configuration
- `enableEnterpriseAttestation`, `toggleAlwaysUv`, `setMinPinLength`
- Port from `Config.java`

### Phase 10: Large Blobs
- `LargeBlobs.cs` - large blob storage
- Read/write operations with chunking

---

## Phase 10A: YubiKey 5.7/5.8 Advanced Features

**Context:** YubiKey firmware 5.7+ introduces advanced FIDO2 features including encrypted credential management metadata and PPUAT (Persistent PIN/UV Auth Token) support.

### Task 10A.1: Encrypted Identifier Support (YK 5.7+)

**Files:**
- Create: `Yubico.YubiKit.Fido2/src/Crypto/EncryptedMetadataDecryptor.cs`
- Update: `Yubico.YubiKit.Fido2/src/CredentialManagement.cs`

**Reference:** `Ctap2Session.java` lines 1170-1222

```csharp
// Yubico.YubiKit.Fido2/src/Crypto/EncryptedMetadataDecryptor.cs
using System.Security.Cryptography;

namespace Yubico.YubiKit.Fido2.Crypto;

/// <summary>
/// Decrypts encrypted credential metadata (encIdentifier, encCredStoreState) 
/// using PPUAT-derived keys. Requires YubiKey 5.7+.
/// </summary>
/// <remarks>
/// These fields allow the authenticator to return encrypted metadata that can
/// be decrypted by the client using a key derived from the PPUAT via HKDF.
/// 
/// HKDF parameters:
/// - Algorithm: SHA-256
/// - Secret: PPUAT (PIN/UV Auth Token)
/// - Salt: 32 zero bytes
/// - Info: "encIdentifier" or "encCredStoreState"
/// - Output length: 16 bytes (AES-128 key)
/// </remarks>
public static class EncryptedMetadataDecryptor
{
    private static readonly byte[] ZeroSalt = new byte[32];
    
    /// <summary>
    /// Decrypts the encIdentifier field using PPUAT-derived key.
    /// </summary>
    /// <param name="ppuat">The Persistent PIN/UV Auth Token.</param>
    /// <param name="encIdentifier">The encrypted identifier bytes.</param>
    /// <returns>The decrypted identifier, or null if decryption fails.</returns>
    public static byte[]? DecryptIdentifier(ReadOnlySpan<byte> ppuat, ReadOnlySpan<byte> encIdentifier)
    {
        return DecryptWithInfo(ppuat, encIdentifier, "encIdentifier"u8);
    }
    
    /// <summary>
    /// Decrypts the encCredStoreState field using PPUAT-derived key.
    /// </summary>
    /// <param name="ppuat">The Persistent PIN/UV Auth Token.</param>
    /// <param name="encCredStoreState">The encrypted credential store state bytes.</param>
    /// <returns>The decrypted state, or null if decryption fails.</returns>
    public static byte[]? DecryptCredStoreState(ReadOnlySpan<byte> ppuat, ReadOnlySpan<byte> encCredStoreState)
    {
        return DecryptWithInfo(ppuat, encCredStoreState, "encCredStoreState"u8);
    }
    
    private static byte[]? DecryptWithInfo(
        ReadOnlySpan<byte> ppuat, 
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> info)
    {
        if (ppuat.IsEmpty || ciphertext.IsEmpty)
            return null;
        
        // Derive AES-128 key using HKDF-SHA256
        Span<byte> key = stackalloc byte[16];
        HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            ppuat,
            key,
            salt: ZeroSalt,
            info: info);
        
        try
        {
            // Decrypt using AES-128 (mode depends on ciphertext format)
            // YubiKey uses AES-128-ECB for these small values
            using var aes = Aes.Create();
            aes.Key = key.ToArray();
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;
            
            var plaintext = new byte[ciphertext.Length];
            aes.DecryptEcb(ciphertext, plaintext, PaddingMode.None);
            
            return plaintext;
        }
        catch
        {
            return null;
        }
    }
}
```

### Task 10A.2: PPUAT (Persistent PIN/UV Auth Token) Support

**Context:** PPUAT is a PIN/UV auth token that persists across certain operations. It's used to derive keys for decrypting encrypted metadata.

```csharp
// Addition to ClientPin.cs - GetPersistentPinToken support
/// <summary>
/// Gets a persistent PIN/UV auth token (PPUAT) for credential management operations.
/// Requires YubiKey 5.7+ with pinUvAuthToken option.
/// </summary>
/// <remarks>
/// The PPUAT is used to decrypt encrypted credential metadata (encIdentifier, 
/// encCredStoreState) returned by CredentialManagement commands on YK 5.7+.
/// </remarks>
public async Task<byte[]> GetPersistentPinTokenAsync(
    string pin,
    PinPermission permissions,
    string? rpId = null,
    CancellationToken cancellationToken = default)
{
    // This is essentially the same as GetPinTokenAsync but the returned token
    // is kept for use with EncryptedMetadataDecryptor
    return await GetPinTokenAsync(pin, permissions, rpId, cancellationToken)
        .ConfigureAwait(false);
}
```

---

## Phase 10B: WebAuthn/CTAP Extensions

**Context:** CTAP2 extensions provide additional functionality for credentials. YubiKey supports various extensions depending on firmware version.

### Task 10B.1: Extension Framework

**Files:**
- Create: `Yubico.YubiKit.Fido2/src/Extensions/ICtapExtension.cs`
- Create: `Yubico.YubiKit.Fido2/src/Extensions/ExtensionRegistry.cs`

```csharp
// Yubico.YubiKit.Fido2/src/Extensions/ICtapExtension.cs
using System.Formats.Cbor;

namespace Yubico.YubiKit.Fido2.Extensions;

/// <summary>
/// Interface for CTAP2 extensions.
/// </summary>
public interface ICtapExtension
{
    /// <summary>
    /// Gets the extension identifier (e.g., "hmac-secret", "credProtect").
    /// </summary>
    string Identifier { get; }
    
    /// <summary>
    /// Encodes the extension input for MakeCredential.
    /// </summary>
    void EncodeMakeCredentialInput(CborWriter writer);
    
    /// <summary>
    /// Encodes the extension input for GetAssertion.
    /// </summary>
    void EncodeGetAssertionInput(CborWriter writer);
    
    /// <summary>
    /// Decodes the extension output from authenticator response.
    /// </summary>
    void DecodeOutput(CborReader reader);
}
```

### Task 10B.2: hmac-secret Extension (YK 5.0+)

**Reference:** `HmacSecretExtension.java`

```csharp
// Yubico.YubiKit.Fido2/src/Extensions/HmacSecretExtension.cs
using System.Formats.Cbor;
using System.Security.Cryptography;
using Yubico.YubiKit.Fido2.Pin;

namespace Yubico.YubiKit.Fido2.Extensions;

/// <summary>
/// HMAC secret extension for PRF (Pseudo-Random Function) support.
/// Available on YubiKey 5.0+.
/// </summary>
/// <remarks>
/// During MakeCredential: Signals that the credential supports hmac-secret.
/// During GetAssertion: Provides salts that are processed with a credential-bound secret.
/// 
/// This extension powers the WebAuthn PRF extension.
/// </remarks>
public sealed class HmacSecretExtension : ICtapExtension
{
    public string Identifier => "hmac-secret";
    
    private readonly IPinUvAuthProtocol _protocol;
    private readonly byte[] _sharedSecret;
    private byte[]? _salt1;
    private byte[]? _salt2;
    private byte[]? _output1;
    private byte[]? _output2;
    
    /// <summary>
    /// Gets the first PRF output (32 bytes), available after GetAssertion.
    /// </summary>
    public ReadOnlySpan<byte> Output1 => _output1 ?? throw new InvalidOperationException("No output available");
    
    /// <summary>
    /// Gets the second PRF output (32 bytes), available if salt2 was provided.
    /// </summary>
    public ReadOnlySpan<byte> Output2 => _output2 ?? throw new InvalidOperationException("No output2 available");
    
    /// <summary>
    /// Creates extension for MakeCredential (just signals support).
    /// </summary>
    public HmacSecretExtension()
    {
        _protocol = null!;
        _sharedSecret = [];
    }
    
    /// <summary>
    /// Creates extension for GetAssertion with salt values.
    /// </summary>
    /// <param name="protocol">The PIN/UV auth protocol for encryption.</param>
    /// <param name="sharedSecret">The shared secret from key agreement.</param>
    /// <param name="salt1">First 32-byte salt (required).</param>
    /// <param name="salt2">Optional second 32-byte salt.</param>
    public HmacSecretExtension(
        IPinUvAuthProtocol protocol,
        byte[] sharedSecret,
        byte[] salt1,
        byte[]? salt2 = null)
    {
        ArgumentNullException.ThrowIfNull(protocol);
        ArgumentNullException.ThrowIfNull(sharedSecret);
        ArgumentNullException.ThrowIfNull(salt1);
        
        if (salt1.Length != 32)
            throw new ArgumentException("Salt1 must be 32 bytes", nameof(salt1));
        if (salt2 is { Length: not 32 })
            throw new ArgumentException("Salt2 must be 32 bytes if provided", nameof(salt2));
        
        _protocol = protocol;
        _sharedSecret = sharedSecret;
        _salt1 = salt1;
        _salt2 = salt2;
    }
    
    public void EncodeMakeCredentialInput(CborWriter writer)
    {
        // For MakeCredential, just signal support
        writer.WriteBoolean(true);
    }
    
    public void EncodeGetAssertionInput(CborWriter writer)
    {
        // For GetAssertion, provide encrypted salts
        // Structure: { 1: keyAgreement, 2: saltEnc, 3: saltAuth, 4: pinUvAuthProtocol }
        
        // Prepare salt buffer (32 or 64 bytes)
        var saltBuffer = _salt2 is not null 
            ? new byte[64] 
            : new byte[32];
        _salt1!.CopyTo(saltBuffer, 0);
        _salt2?.CopyTo(saltBuffer, 32);
        
        // Encrypt salts
        var saltEnc = _protocol.Encrypt(_sharedSecret, saltBuffer);
        var saltAuth = _protocol.Authenticate(_sharedSecret, saltEnc);
        
        writer.WriteStartMap(_salt2 is null ? 3 : 4);
        
        // Note: keyAgreement comes from ClientPin.GetSharedSecretAsync
        // which the caller needs to include
        
        writer.WriteInt32(2);
        writer.WriteByteString(saltEnc);
        
        writer.WriteInt32(3);
        writer.WriteByteString(saltAuth[..16]); // First 16 bytes
        
        writer.WriteInt32(4);
        writer.WriteInt32(_protocol.Version);
        
        writer.WriteEndMap();
    }
    
    public void DecodeOutput(CborReader reader)
    {
        // Output is encrypted: decrypt with shared secret
        var encryptedOutput = reader.ReadByteString();
        var decrypted = _protocol.Decrypt(_sharedSecret, encryptedOutput);
        
        _output1 = decrypted[..32];
        if (decrypted.Length >= 64)
            _output2 = decrypted[32..64];
    }
}
```

### Task 10B.3: credProtect Extension (YK 5.2+)

```csharp
// Yubico.YubiKit.Fido2/src/Extensions/CredProtectExtension.cs
using System.Formats.Cbor;

namespace Yubico.YubiKit.Fido2.Extensions;

/// <summary>
/// Credential protection levels.
/// </summary>
public enum CredProtectLevel
{
    /// <summary>UV optional (default).</summary>
    UserVerificationOptional = 1,
    
    /// <summary>UV optional with credential ID list.</summary>
    UserVerificationOptionalWithCredentialIdList = 2,
    
    /// <summary>UV required.</summary>
    UserVerificationRequired = 3
}

/// <summary>
/// Credential protection extension for controlling when credentials can be used.
/// Available on YubiKey 5.2+.
/// </summary>
public sealed class CredProtectExtension : ICtapExtension
{
    public string Identifier => "credProtect";
    
    private readonly CredProtectLevel _level;
    private CredProtectLevel? _actualLevel;
    
    /// <summary>
    /// Gets the actual credential protection level applied (after MakeCredential).
    /// </summary>
    public CredProtectLevel ActualLevel => _actualLevel ?? _level;
    
    public CredProtectExtension(CredProtectLevel level)
    {
        _level = level;
    }
    
    public void EncodeMakeCredentialInput(CborWriter writer)
    {
        writer.WriteInt32((int)_level);
    }
    
    public void EncodeGetAssertionInput(CborWriter writer)
    {
        // Not used for GetAssertion
        throw new NotSupportedException("credProtect is only for MakeCredential");
    }
    
    public void DecodeOutput(CborReader reader)
    {
        _actualLevel = (CredProtectLevel)reader.ReadInt32();
    }
}
```

### Task 10B.4: credBlob Extension (YK 5.5+)

**Reference:** `yubikit-android/fido/src/main/java/com/yubico/yubikit/fido/client/extensions/CredBlobExtension.java`

The credBlob extension stores a small per-credential blob (up to `maxCredBlobLength` bytes, typically 32) that is returned during authentication.

```csharp
// Yubico.YubiKit.Fido2/src/Extensions/CredBlobExtension.cs
using System.Formats.Cbor;

namespace Yubico.YubiKit.Fido2.Extensions;

/// <summary>
/// Per-credential blob storage extension.
/// Stores a small blob with the credential that is returned during authentication.
/// Available on YubiKey 5.5+.
/// </summary>
/// <remarks>
/// Maximum blob size is determined by authenticator's maxCredBlobLength (typically 32 bytes).
/// The blob is stored with discoverable (resident) credentials only.
/// </remarks>
public sealed class CredBlobExtension : ICtapExtension
{
    public string Identifier => "credBlob";
    
    private readonly byte[]? _blobToStore;
    private byte[]? _retrievedBlob;
    private bool? _stored;
    
    /// <summary>
    /// Gets the blob retrieved during GetAssertion (null if not present).
    /// </summary>
    public byte[]? RetrievedBlob => _retrievedBlob;
    
    /// <summary>
    /// Gets whether the blob was successfully stored (for MakeCredential output).
    /// </summary>
    public bool? WasStored => _stored;
    
    /// <summary>
    /// Creates extension for storing a blob during MakeCredential.
    /// </summary>
    /// <param name="blob">Blob to store. Must not exceed maxCredBlobLength.</param>
    public CredBlobExtension(byte[] blob)
    {
        ArgumentNullException.ThrowIfNull(blob);
        _blobToStore = blob;
    }
    
    /// <summary>
    /// Creates extension for retrieving a blob during GetAssertion.
    /// </summary>
    public CredBlobExtension()
    {
        _blobToStore = null;
    }
    
    public bool IsSupported(AuthenticatorInfo info) =>
        info.Extensions?.Contains("credBlob") == true &&
        info.MaxCredBlobLength > 0;
    
    public void EncodeMakeCredentialInput(CborWriter writer)
    {
        // Input is the blob bytes directly
        if (_blobToStore is null)
            throw new InvalidOperationException("No blob set for storage");
        
        writer.WriteByteString(_blobToStore);
    }
    
    public void EncodeGetAssertionInput(CborWriter writer)
    {
        // Input is boolean true to request blob retrieval
        writer.WriteBoolean(true);
    }
    
    public void DecodeMakeCredentialOutput(CborReader reader)
    {
        // Output is boolean indicating success
        _stored = reader.ReadBoolean();
    }
    
    public void DecodeGetAssertionOutput(CborReader reader)
    {
        // Output is the blob bytes
        _retrievedBlob = reader.ReadByteString().ToArray();
    }
}
```

**Unit Tests:**
```csharp
// Yubico.YubiKit.Fido2/tests/Extensions/CredBlobExtensionTests.cs
using System.Formats.Cbor;
using NUnit.Framework;
using Yubico.YubiKit.Fido2.Extensions;

namespace Yubico.YubiKit.Fido2.Tests.Extensions;

[TestFixture]
public class CredBlobExtensionTests
{
    [Test]
    public void Identifier_ReturnsCredBlob()
    {
        var ext = new CredBlobExtension(new byte[] { 1, 2, 3 });
        Assert.That(ext.Identifier, Is.EqualTo("credBlob"));
    }
    
    [Test]
    public void EncodeMakeCredentialInput_WritesBlob()
    {
        var blob = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var ext = new CredBlobExtension(blob);
        
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        ext.EncodeMakeCredentialInput(writer);
        
        var reader = new CborReader(writer.Encode(), CborConformanceMode.Ctap2Canonical);
        Assert.That(reader.ReadByteString(), Is.EqualTo(blob));
    }
    
    [Test]
    public void EncodeGetAssertionInput_WritesTrue()
    {
        var ext = new CredBlobExtension();
        
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        ext.EncodeGetAssertionInput(writer);
        
        var reader = new CborReader(writer.Encode(), CborConformanceMode.Ctap2Canonical);
        Assert.That(reader.ReadBoolean(), Is.True);
    }
    
    [Test]
    public void DecodeMakeCredentialOutput_SetsWasStored()
    {
        var ext = new CredBlobExtension(new byte[] { 1 });
        
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteBoolean(true);
        
        var reader = new CborReader(writer.Encode(), CborConformanceMode.Ctap2Canonical);
        ext.DecodeMakeCredentialOutput(reader);
        
        Assert.That(ext.WasStored, Is.True);
    }
    
    [Test]
    public void DecodeGetAssertionOutput_SetsRetrievedBlob()
    {
        var ext = new CredBlobExtension();
        var expectedBlob = new byte[] { 0xCA, 0xFE };
        
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteByteString(expectedBlob);
        
        var reader = new CborReader(writer.Encode(), CborConformanceMode.Ctap2Canonical);
        ext.DecodeGetAssertionOutput(reader);
        
        Assert.That(ext.RetrievedBlob, Is.EqualTo(expectedBlob));
    }
    
    [Test]
    public void IsSupported_ReturnsTrueWhenExtensionAndMaxLength()
    {
        var info = CreateInfoWithCredBlob(maxLength: 32);
        var ext = new CredBlobExtension(new byte[] { 1 });
        
        Assert.That(ext.IsSupported(info), Is.True);
    }
    
    [Test]
    public void IsSupported_ReturnsFalseWhenZeroMaxLength()
    {
        var info = CreateInfoWithCredBlob(maxLength: 0);
        var ext = new CredBlobExtension(new byte[] { 1 });
        
        Assert.That(ext.IsSupported(info), Is.False);
    }
    
    private static AuthenticatorInfo CreateInfoWithCredBlob(int maxLength)
    {
        // Create minimal AuthenticatorInfo with credBlob support
        var builder = new CborWriter(CborConformanceMode.Ctap2Canonical);
        builder.WriteStartMap(3);
        
        // versions (required)
        builder.WriteInt32(0x01);
        builder.WriteStartArray(1);
        builder.WriteTextString("FIDO_2_0");
        builder.WriteEndArray();
        
        // extensions
        builder.WriteInt32(0x02);
        builder.WriteStartArray(1);
        builder.WriteTextString("credBlob");
        builder.WriteEndArray();
        
        // maxCredBlobLength
        builder.WriteInt32(0x0C);
        builder.WriteInt32(maxLength);
        
        builder.WriteEndMap();
        
        return AuthenticatorInfo.Parse(new CborReader(builder.Encode()));
    }
}
```

### Task 10B.5: credProps Extension (Output Only)

**Reference:** `yubikit-android/fido/src/main/java/com/yubico/yubikit/fido/client/extensions/CredPropsExtension.java`

The credProps extension is output-only - it reports properties about the created credential (whether it's a resident key).

```csharp
// Yubico.YubiKit.Fido2/src/Extensions/CredPropsExtension.cs
using System.Formats.Cbor;

namespace Yubico.YubiKit.Fido2.Extensions;

/// <summary>
/// Credential properties extension (output-only).
/// Reports properties about the created credential.
/// </summary>
/// <remarks>
/// This extension has no authenticator input - it only requests output.
/// The output indicates whether the credential was created as a discoverable (resident) credential.
/// </remarks>
public sealed class CredPropsExtension : ICtapExtension
{
    public string Identifier => "credProps";
    
    private bool? _isResidentKey;
    
    /// <summary>
    /// Gets whether the credential is a resident key (discoverable credential).
    /// Null if output not yet received.
    /// </summary>
    public bool? IsResidentKey => _isResidentKey;
    
    public bool IsSupported(AuthenticatorInfo info) =>
        // credProps is handled by the client, not the authenticator
        // Always supported for MakeCredential
        true;
    
    public void EncodeMakeCredentialInput(CborWriter writer)
    {
        // Input is just boolean true
        writer.WriteBoolean(true);
    }
    
    public void EncodeGetAssertionInput(CborWriter writer)
    {
        throw new NotSupportedException("credProps is only for MakeCredential");
    }
    
    public void DecodeMakeCredentialOutput(CborReader reader)
    {
        // Output is a map: { "rk": bool }
        var count = reader.ReadStartMap();
        
        while (reader.PeekState() != CborReaderState.EndMap)
        {
            var key = reader.ReadTextString();
            
            if (key == "rk")
            {
                _isResidentKey = reader.ReadBoolean();
            }
            else
            {
                reader.SkipValue(); // Unknown property
            }
        }
        
        reader.ReadEndMap();
    }
    
    public void DecodeGetAssertionOutput(CborReader reader)
    {
        throw new NotSupportedException("credProps is only for MakeCredential");
    }
}
```

### Task 10B.6: largeBlob Extension (YK 5.5+)

**Reference:** `yubikit-android/fido/src/main/java/com/yubico/yubikit/fido/client/extensions/LargeBlobExtension.java`

The largeBlob extension is complex - it involves reading/writing large blobs through a separate CTAP command (LargeBlobs 0x0C) and associating them with credentials via a largeBlobKey.

```csharp
// Yubico.YubiKit.Fido2/src/Extensions/LargeBlobExtension.cs
using System.Formats.Cbor;

namespace Yubico.YubiKit.Fido2.Extensions;

/// <summary>
/// Large blob storage extension.
/// Enables storing large amounts of data associated with credentials.
/// Available on YubiKey 5.5+.
/// </summary>
/// <remarks>
/// The largeBlob extension works in two modes:
/// 1. During MakeCredential: request a largeBlobKey to be generated
/// 2. During GetAssertion: retrieve the largeBlobKey for the credential
/// 
/// Actual blob read/write is done via the separate LargeBlobs CTAP command (0x0C),
/// using the largeBlobKey to encrypt/decrypt the blob.
/// </remarks>
public sealed class LargeBlobExtension : ICtapExtension
{
    public string Identifier => "largeBlob";
    
    private readonly LargeBlobSupport _support;
    private byte[]? _largeBlobKey;
    private byte[]? _blob;
    private bool? _written;
    
    /// <summary>
    /// Gets the large blob key (for encrypting/decrypting blobs).
    /// Available after successful MakeCredential or GetAssertion.
    /// </summary>
    public byte[]? LargeBlobKey => _largeBlobKey;
    
    /// <summary>
    /// Gets the decrypted blob content (when Read was specified).
    /// </summary>
    public byte[]? Blob => _blob;
    
    /// <summary>
    /// Gets whether the write was successful (when Write was specified).
    /// </summary>
    public bool? Written => _written;
    
    /// <summary>
    /// Creates extension for MakeCredential to generate a largeBlobKey.
    /// </summary>
    /// <param name="support">Whether the blob key is preferred or required.</param>
    public LargeBlobExtension(LargeBlobSupport support = LargeBlobSupport.Preferred)
    {
        _support = support;
    }
    
    public bool IsSupported(AuthenticatorInfo info) =>
        info.Extensions?.Contains("largeBlob") == true ||
        info.Extensions?.Contains("largeBlobKey") == true;
    
    public void EncodeMakeCredentialInput(CborWriter writer)
    {
        // Input: { "support": "preferred" | "required" }
        writer.WriteStartMap(1);
        writer.WriteTextString("support");
        writer.WriteTextString(_support == LargeBlobSupport.Required ? "required" : "preferred");
        writer.WriteEndMap();
    }
    
    public void EncodeGetAssertionInput(CborWriter writer)
    {
        // Input: { "read": true } or { "write": <blob> }
        // For now, only read support
        writer.WriteStartMap(1);
        writer.WriteTextString("read");
        writer.WriteBoolean(true);
        writer.WriteEndMap();
    }
    
    public void DecodeMakeCredentialOutput(CborReader reader)
    {
        // Output: { "supported": bool }
        var count = reader.ReadStartMap();
        
        while (reader.PeekState() != CborReaderState.EndMap)
        {
            var key = reader.ReadTextString();
            
            if (key == "supported")
            {
                var supported = reader.ReadBoolean();
                // supported=true means largeBlobKey was generated
            }
            else
            {
                reader.SkipValue();
            }
        }
        
        reader.ReadEndMap();
    }
    
    public void DecodeGetAssertionOutput(CborReader reader)
    {
        // Output: { "blob": bytes } or { "written": bool }
        var count = reader.ReadStartMap();
        
        while (reader.PeekState() != CborReaderState.EndMap)
        {
            var key = reader.ReadTextString();
            
            switch (key)
            {
                case "blob":
                    _blob = reader.ReadByteString().ToArray();
                    break;
                case "written":
                    _written = reader.ReadBoolean();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }
        
        reader.ReadEndMap();
    }
}

/// <summary>
/// Large blob support level for MakeCredential.
/// </summary>
public enum LargeBlobSupport
{
    /// <summary>Prefer largeBlob support but don't fail if unavailable.</summary>
    Preferred,
    
    /// <summary>Require largeBlob support - fail if unavailable.</summary>
    Required
}
```

**Large Blob Operations (via LargeBlobs command):**
```csharp
// Yubico.YubiKit.Fido2/src/LargeBlobs/LargeBlobsOperations.cs
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Buffers;

namespace Yubico.YubiKit.Fido2.LargeBlobs;

/// <summary>
/// Operations for reading and writing large blobs.
/// </summary>
public static class LargeBlobsOperations
{
    private const int MaxFragmentLength = 1024;
    
    /// <summary>
    /// Reads the large blob array from the authenticator.
    /// </summary>
    public static async Task<byte[]> ReadLargeBlobArrayAsync(
        FidoSession session,
        CancellationToken cancellationToken = default)
    {
        var fullData = new List<byte>();
        int offset = 0;
        
        while (true)
        {
            var request = new CtapRequestBuilder(CtapCommand.LargeBlobs)
                .Add(0x01, MaxFragmentLength) // get (length)
                .Add(0x02, offset)             // offset
                .Build();
            
            var response = await session.SendRawAsync(request, cancellationToken);
            
            // Parse response: { 1: config (bytes) }
            var reader = new CborReader(response, CborConformanceMode.Ctap2Canonical);
            reader.ReadStartMap();
            
            byte[]? fragment = null;
            while (reader.PeekState() != CborReaderState.EndMap)
            {
                var key = reader.ReadInt32();
                if (key == 0x01)
                {
                    fragment = reader.ReadByteString().ToArray();
                }
                else
                {
                    reader.SkipValue();
                }
            }
            
            if (fragment is null || fragment.Length == 0)
                break;
            
            fullData.AddRange(fragment);
            offset += fragment.Length;
            
            if (fragment.Length < MaxFragmentLength)
                break;
        }
        
        return fullData.ToArray();
    }
    
    /// <summary>
    /// Finds and decrypts a blob for a specific credential.
    /// </summary>
    public static byte[]? FindBlobForCredential(
        byte[] largeBlobArray,
        byte[] largeBlobKey,
        byte[] credentialId)
    {
        // Large blob array structure:
        // CBOR array of entries, each encrypted with largeBlobKey
        // SHA256 hash of array appended at end (16 bytes)
        
        if (largeBlobArray.Length < 17) // Minimum: empty array + hash
            return null;
        
        // Verify integrity (last 16 bytes are SHA256 truncated)
        var expectedHash = largeBlobArray[^16..];
        var actualHash = SHA256.HashData(largeBlobArray[..^16])[..16];
        
        if (!CryptographicOperations.FixedTimeEquals(expectedHash, actualHash))
            return null;
        
        // Parse CBOR array
        var reader = new CborReader(largeBlobArray[..^16], CborConformanceMode.Ctap2Canonical);
        
        if (reader.PeekState() != CborReaderState.StartArray)
            return null;
        
        var count = reader.ReadStartArray();
        
        // Each entry is: { ciphertext: bytes, nonce: bytes, origSize: int }
        // Try to decrypt each with our largeBlobKey
        while (reader.PeekState() != CborReaderState.EndArray)
        {
            var entry = ParseBlobEntry(reader);
            
            var decrypted = TryDecrypt(entry, largeBlobKey);
            if (decrypted is not null)
            {
                // Found our blob
                return decrypted;
            }
        }
        
        return null;
    }
    
    private static BlobEntry ParseBlobEntry(CborReader reader)
    {
        byte[]? ciphertext = null;
        byte[]? nonce = null;
        int origSize = 0;
        
        reader.ReadStartMap();
        
        while (reader.PeekState() != CborReaderState.EndMap)
        {
            var key = reader.ReadInt32();
            
            switch (key)
            {
                case 1:
                    ciphertext = reader.ReadByteString().ToArray();
                    break;
                case 2:
                    nonce = reader.ReadByteString().ToArray();
                    break;
                case 3:
                    origSize = reader.ReadInt32();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }
        
        reader.ReadEndMap();
        
        return new BlobEntry(ciphertext!, nonce!, origSize);
    }
    
    private static byte[]? TryDecrypt(BlobEntry entry, byte[] key)
    {
        try
        {
            // Decrypt using AES-GCM
            using var aes = new AesGcm(key, 16);
            var plaintext = new byte[entry.OrigSize];
            
            aes.Decrypt(
                entry.Nonce,
                entry.Ciphertext[..^16], // Ciphertext without tag
                entry.Ciphertext[^16..], // Tag
                plaintext);
            
            return plaintext;
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
    
    private readonly record struct BlobEntry(byte[] Ciphertext, byte[] Nonce, int OrigSize);
}
```

### Task 10B.7: minPinLength Extension (YK 5.4+)

**Reference:** `yubikit-android/fido/src/main/java/com/yubico/yubikit/fido/client/extensions/MinPinLengthExtension.java`

```csharp
// Yubico.YubiKit.Fido2/src/Extensions/MinPinLengthExtension.cs
using System.Formats.Cbor;

namespace Yubico.YubiKit.Fido2.Extensions;

/// <summary>
/// Minimum PIN length extension.
/// Requests the authenticator's minimum PIN length in attestation.
/// Available on YubiKey 5.4+.
/// </summary>
/// <remarks>
/// This extension is used during MakeCredential to request that the
/// authenticator include its minimum PIN length in the attestation.
/// The relying party can use this to enforce PIN policies.
/// </remarks>
public sealed class MinPinLengthExtension : ICtapExtension
{
    public string Identifier => "minPinLength";
    
    private int? _minPinLength;
    
    /// <summary>
    /// Gets the minimum PIN length from attestation output.
    /// </summary>
    public int? MinPinLength => _minPinLength;
    
    public bool IsSupported(AuthenticatorInfo info)
    {
        // Check if the authenticator supports minPinLength extension
        // and if it's configured to return minPinLength (setMinPINLength was called)
        return info.Extensions?.Contains("minPinLength") == true &&
               info.MinPinLength.HasValue;
    }
    
    public void EncodeMakeCredentialInput(CborWriter writer)
    {
        // Input is boolean true to request minPinLength in output
        writer.WriteBoolean(true);
    }
    
    public void EncodeGetAssertionInput(CborWriter writer)
    {
        throw new NotSupportedException("minPinLength is only for MakeCredential");
    }
    
    public void DecodeMakeCredentialOutput(CborReader reader)
    {
        // Output is the minimum PIN length as unsigned integer
        _minPinLength = reader.ReadInt32();
    }
    
    public void DecodeGetAssertionOutput(CborReader reader)
    {
        throw new NotSupportedException("minPinLength is only for MakeCredential");
    }
}
```

### Task 10B.8: Sign Extension (Payment/Signing)

**Reference:** `yubikit-android/fido/src/main/java/com/yubico/yubikit/fido/client/extensions/SignExtension.java`

The Sign extension enables cryptographic signing operations, supporting key generation and signing.

```csharp
// Yubico.YubiKit.Fido2/src/Extensions/SignExtension.cs
using System.Formats.Cbor;
using Yubico.YubiKit.Core.Cryptography.Cose;

namespace Yubico.YubiKit.Fido2.Extensions;

/// <summary>
/// Cryptographic signing extension.
/// Enables key generation and signing operations.
/// </summary>
/// <remarks>
/// This extension supports two operations:
/// 1. generateKey: Generate a new key pair and return the public key
/// 2. sign: Sign data with a previously generated key
/// 
/// Used for payment authentication and other cryptographic protocols.
/// </remarks>
public sealed class SignExtension : ICtapExtension
{
    public string Identifier => "sign";
    
    private readonly SignAction _action;
    private readonly CoseAlgorithmIdentifier? _algorithm;
    private readonly byte[]? _keyToSign;
    private readonly byte[]? _dataToSign;
    
    private byte[]? _publicKey;
    private byte[]? _signature;
    
    /// <summary>
    /// Gets the generated public key (COSE_Key format).
    /// Available after generateKey operation.
    /// </summary>
    public byte[]? PublicKey => _publicKey;
    
    /// <summary>
    /// Gets the signature.
    /// Available after sign operation.
    /// </summary>
    public byte[]? Signature => _signature;
    
    /// <summary>
    /// Creates extension for key generation.
    /// </summary>
    public static SignExtension GenerateKey(CoseAlgorithmIdentifier algorithm = CoseAlgorithmIdentifier.ES256)
    {
        return new SignExtension(SignAction.GenerateKey, algorithm, null, null);
    }
    
    /// <summary>
    /// Creates extension for signing.
    /// </summary>
    public static SignExtension Sign(byte[] publicKey, byte[] data)
    {
        return new SignExtension(SignAction.Sign, null, publicKey, data);
    }
    
    private SignExtension(
        SignAction action,
        CoseAlgorithmIdentifier? algorithm,
        byte[]? keyToSign,
        byte[]? dataToSign)
    {
        _action = action;
        _algorithm = algorithm;
        _keyToSign = keyToSign;
        _dataToSign = dataToSign;
    }
    
    public bool IsSupported(AuthenticatorInfo info) =>
        info.Extensions?.Contains("sign") == true;
    
    public void EncodeMakeCredentialInput(CborWriter writer)
    {
        // Sign extension is typically used with GetAssertion
        throw new NotSupportedException("sign is typically for GetAssertion");
    }
    
    public void EncodeGetAssertionInput(CborWriter writer)
    {
        // Input: { "generateKey": { "algorithms": [alg] } }
        // or:    { "sign": { "publicKey": bytes, "data": bytes } }
        
        writer.WriteStartMap(1);
        
        switch (_action)
        {
            case SignAction.GenerateKey:
                writer.WriteTextString("generateKey");
                writer.WriteStartMap(1);
                writer.WriteTextString("algorithms");
                writer.WriteStartArray(1);
                writer.WriteInt32((int)_algorithm!);
                writer.WriteEndArray();
                writer.WriteEndMap();
                break;
                
            case SignAction.Sign:
                writer.WriteTextString("sign");
                writer.WriteStartMap(2);
                writer.WriteTextString("publicKey");
                writer.WriteByteString(_keyToSign!);
                writer.WriteTextString("data");
                writer.WriteByteString(_dataToSign!);
                writer.WriteEndMap();
                break;
        }
        
        writer.WriteEndMap();
    }
    
    public void DecodeMakeCredentialOutput(CborReader reader)
    {
        throw new NotSupportedException("sign is typically for GetAssertion");
    }
    
    public void DecodeGetAssertionOutput(CborReader reader)
    {
        // Output: { "generatedKey": { "publicKey": COSE_Key } }
        // or:     { "signature": bytes }
        
        reader.ReadStartMap();
        
        while (reader.PeekState() != CborReaderState.EndMap)
        {
            var key = reader.ReadTextString();
            
            switch (key)
            {
                case "generatedKey":
                    ParseGeneratedKey(reader);
                    break;
                case "signature":
                    _signature = reader.ReadByteString().ToArray();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }
        
        reader.ReadEndMap();
    }
    
    private void ParseGeneratedKey(CborReader reader)
    {
        reader.ReadStartMap();
        
        while (reader.PeekState() != CborReaderState.EndMap)
        {
            var key = reader.ReadTextString();
            
            if (key == "publicKey")
            {
                // Store the raw COSE_Key bytes
                _publicKey = reader.ReadByteString().ToArray();
            }
            else
            {
                reader.SkipValue();
            }
        }
        
        reader.ReadEndMap();
    }
    
    private enum SignAction
    {
        GenerateKey,
        Sign
    }
}
```

### Task 10B.9: Third Party Payment Extension

**Reference:** `yubikit-android/fido/src/main/java/com/yubico/yubikit/fido/client/extensions/ThirdPartyPaymentExtension.java`

```csharp
// Yubico.YubiKit.Fido2/src/Extensions/ThirdPartyPaymentExtension.cs
using System.Formats.Cbor;

namespace Yubico.YubiKit.Fido2.Extensions;

/// <summary>
/// Third-party payment extension.
/// Indicates the credential is for payment authentication.
/// </summary>
/// <remarks>
/// This extension is used during MakeCredential to mark a credential
/// as being used for third-party payment authentication.
/// 
/// The authenticator will indicate in its output whether payment
/// support is available.
/// </remarks>
public sealed class ThirdPartyPaymentExtension : ICtapExtension
{
    public string Identifier => "thirdPartyPayment";
    
    private bool? _isPayment;
    
    /// <summary>
    /// Gets whether payment support is confirmed.
    /// </summary>
    public bool? IsPayment => _isPayment;
    
    public bool IsSupported(AuthenticatorInfo info) =>
        info.Extensions?.Contains("thirdPartyPayment") == true;
    
    public void EncodeMakeCredentialInput(CborWriter writer)
    {
        // Input is boolean true to request payment credential
        writer.WriteBoolean(true);
    }
    
    public void EncodeGetAssertionInput(CborWriter writer)
    {
        // For GetAssertion, also send true to indicate this is a payment assertion
        writer.WriteBoolean(true);
    }
    
    public void DecodeMakeCredentialOutput(CborReader reader)
    {
        // Output is boolean indicating payment support
        _isPayment = reader.ReadBoolean();
    }
    
    public void DecodeGetAssertionOutput(CborReader reader)
    {
        // Output is boolean indicating payment was processed
        _isPayment = reader.ReadBoolean();
    }
}
```

### Task 10B.10: Extension Framework and Registration

```csharp
// Yubico.YubiKit.Fido2/src/Extensions/ExtensionRegistry.cs
namespace Yubico.YubiKit.Fido2.Extensions;

/// <summary>
/// Registry of built-in CTAP extensions.
/// </summary>
public static class ExtensionRegistry
{
    /// <summary>
    /// All available extension identifiers.
    /// </summary>
    public static readonly IReadOnlyList<string> KnownExtensions = new[]
    {
        "credBlob",
        "credProps",
        "credProtect",
        "hmac-secret",
        "largeBlob",
        "minPinLength",
        "prf",
        "sign",
        "thirdPartyPayment"
    };
    
    /// <summary>
    /// Checks if an extension identifier is known.
    /// </summary>
    public static bool IsKnown(string identifier) =>
        KnownExtensions.Contains(identifier);
    
    /// <summary>
    /// Gets the minimum YubiKey version for an extension.
    /// </summary>
    public static FirmwareVersion? GetMinimumVersion(string identifier) => identifier switch
    {
        "hmac-secret" => new FirmwareVersion(5, 0, 0),
        "credProtect" => new FirmwareVersion(5, 2, 0),
        "minPinLength" => new FirmwareVersion(5, 4, 0),
        "credBlob" => new FirmwareVersion(5, 5, 0),
        "largeBlob" => new FirmwareVersion(5, 5, 0),
        "prf" => new FirmwareVersion(5, 4, 0),
        _ => null
    };
}

// Yubico.YubiKit.Fido2/src/Extensions/ICtapExtensionProcessor.cs

/// <summary>
/// Processor for handling extension input/output during operations.
/// </summary>
public interface IExtensionProcessor
{
    /// <summary>
    /// Encodes extension inputs into the extensions map.
    /// </summary>
    void EncodeInputs(CborWriter extensionsWriter, IReadOnlyList<ICtapExtension> extensions);
    
    /// <summary>
    /// Decodes extension outputs from the response.
    /// </summary>
    void DecodeOutputs(CborReader extensionsReader, IReadOnlyList<ICtapExtension> extensions);
}

/// <summary>
/// Processor for MakeCredential extensions.
/// </summary>
public sealed class MakeCredentialExtensionProcessor : IExtensionProcessor
{
    public void EncodeInputs(CborWriter extensionsWriter, IReadOnlyList<ICtapExtension> extensions)
    {
        extensionsWriter.WriteStartMap(extensions.Count);
        
        foreach (var ext in extensions)
        {
            extensionsWriter.WriteTextString(ext.Identifier);
            ext.EncodeMakeCredentialInput(extensionsWriter);
        }
        
        extensionsWriter.WriteEndMap();
    }
    
    public void DecodeOutputs(CborReader extensionsReader, IReadOnlyList<ICtapExtension> extensions)
    {
        var lookup = extensions.ToDictionary(e => e.Identifier);
        
        var count = extensionsReader.ReadStartMap();
        
        while (extensionsReader.PeekState() != CborReaderState.EndMap)
        {
            var id = extensionsReader.ReadTextString();
            
            if (lookup.TryGetValue(id, out var ext))
            {
                ext.DecodeMakeCredentialOutput(extensionsReader);
            }
            else
            {
                extensionsReader.SkipValue();
            }
        }
        
        extensionsReader.ReadEndMap();
    }
}

/// <summary>
/// Processor for GetAssertion extensions.
/// </summary>
public sealed class GetAssertionExtensionProcessor : IExtensionProcessor
{
    public void EncodeInputs(CborWriter extensionsWriter, IReadOnlyList<ICtapExtension> extensions)
    {
        extensionsWriter.WriteStartMap(extensions.Count);
        
        foreach (var ext in extensions)
        {
            extensionsWriter.WriteTextString(ext.Identifier);
            ext.EncodeGetAssertionInput(extensionsWriter);
        }
        
        extensionsWriter.WriteEndMap();
    }
    
    public void DecodeOutputs(CborReader extensionsReader, IReadOnlyList<ICtapExtension> extensions)
    {
        var lookup = extensions.ToDictionary(e => e.Identifier);
        
        var count = extensionsReader.ReadStartMap();
        
        while (extensionsReader.PeekState() != CborReaderState.EndMap)
        {
            var id = extensionsReader.ReadTextString();
            
            if (lookup.TryGetValue(id, out var ext))
            {
                ext.DecodeGetAssertionOutput(extensionsReader);
            }
            else
            {
                extensionsReader.SkipValue();
            }
        }
        
        extensionsReader.ReadEndMap();
    }
}
```

### Task 10B.11: Extension Version Matrix

| Extension | Identifier | YK Version | MakeCredential | GetAssertion | Notes |
|-----------|------------|------------|----------------|--------------|-------|
| HMAC Secret | hmac-secret | 5.0+ | ✅ (via hmac-secret-mc) | ✅ | PRF capability |
| PRF | prf | 5.4+ | ✅ | ✅ | WebAuthn PRF extension |
| Cred Protect | credProtect | 5.2+ | ✅ | ❌ | Protection levels 1-3 |
| Cred Blob | credBlob | 5.5+ | ✅ (store) | ✅ (retrieve) | Max 32 bytes |
| Cred Props | credProps | All | ✅ | ❌ | Output only |
| Large Blob | largeBlob | 5.5+ | ✅ (key gen) | ✅ (read/write) | Requires LargeBlobs cmd |
| Min PIN Length | minPinLength | 5.4+ | ✅ | ❌ | Attestation output |
| Sign | sign | Varies | ❌ | ✅ | generateKey/sign ops |
| Third Party Payment | thirdPartyPayment | Varies | ✅ | ✅ | Payment authentication |

---

## Phase 11: Integration Tests (Proper Test Patterns)

**Design Decision:** Per user feedback, we use extension methods on `YubiKeyTestState` and `WithYubiKeyAttribute` instead of helper classes and custom attributes.

### Task 11.1: Test Extension Methods on YubiKeyTestState

**Files:**
- Create: `Yubico.YubiKit.Fido2/tests/Extensions/YubiKeyTestStateExtensions.cs`

```csharp
// Yubico.YubiKit.Fido2/tests/Extensions/YubiKeyTestStateExtensions.cs
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Fido2.Tests.Extensions;

/// <summary>
/// Extension methods for FIDO2 integration tests.
/// </summary>
public static class YubiKeyTestStateExtensions
{
    /// <summary>
    /// Creates a FIDO session and executes an action.
    /// </summary>
    public static async Task WithFidoSessionAsync(
        this YubiKeyTestState device,
        Func<FidoSession, Task> action,
        CancellationToken cancellationToken = default)
    {
        // Prefer FIDO HID if available, fall back to SmartCard
        if (device.AvailableTransports.HasFlag(Transport.Usb))
        {
            await using var connection = await device.YubiKey
                .ConnectAsync<IFidoHidConnection>(cancellationToken)
                .ConfigureAwait(false);
            
            await using var session = await FidoSession.CreateAsync(
                    connection, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            
            await action(session).ConfigureAwait(false);
        }
        else
        {
            await using var connection = await device.YubiKey
                .ConnectAsync<ISmartCardConnection>(cancellationToken)
                .ConfigureAwait(false);
            
            await using var session = await FidoSession.CreateAsync(
                    connection, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            
            await action(session).ConfigureAwait(false);
        }
    }
    
    /// <summary>
    /// Creates an authenticated FIDO session with PIN token and executes an action.
    /// </summary>
    public static async Task WithAuthenticatedFidoSessionAsync(
        this YubiKeyTestState device,
        string pin,
        PinPermission permissions,
        Func<FidoSession, byte[], IPinUvAuthProtocol, Task> action,
        CancellationToken cancellationToken = default)
    {
        await device.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync(cancellationToken).ConfigureAwait(false);
            var protocolVersion = info.PinUvAuthProtocols.Contains(2) ? 2 : 1;
            
            IPinUvAuthProtocol protocol = protocolVersion == 2
                ? new PinUvAuthProtocolV2()
                : new PinUvAuthProtocolV1();
            
            var clientPin = new ClientPin(session, protocol);
            var pinToken = await clientPin.GetPinTokenAsync(
                    pin, permissions, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            
            await action(session, pinToken, protocol).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }
}
```

### Task 11.2: Integration Tests with WithYubiKeyAttribute

**Files:**
- Create: `Yubico.YubiKit.Fido2/tests/Integration/FidoSessionIntegrationTests.cs`

**Note:** Uses `WithYubiKeyAttribute` with `Capability = DeviceCapabilities.Fido2` filter.

```csharp
// Yubico.YubiKit.Fido2/tests/Integration/FidoSessionIntegrationTests.cs
using Yubico.YubiKit.Core.Device;
using Yubico.YubiKit.Fido2.Tests.Extensions;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Fido2.Tests.Integration;

public class FidoSessionIntegrationTests
{
    [Theory]
    [WithYubiKey(Capability = DeviceCapabilities.Fido2, MinFirmware = "5.2.0")]
    public async Task GetInfo_ReturnsValidData(YubiKeyTestState device)
    {
        await device.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            
            Assert.NotEmpty(info.Versions);
            Assert.Contains("FIDO_2_0", info.Versions);
            Assert.Equal(16, info.Aaguid.Length);
        });
    }
    
    [Theory]
    [WithYubiKey(Capability = DeviceCapabilities.Fido2, MinFirmware = "5.2.0")]
    public async Task GetInfo_ContainsExpectedOptions(YubiKeyTestState device)
    {
        await device.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            
            // All FIDO2 YubiKeys support resident keys
            Assert.True(info.Options.ResidentKey);
            // User presence is always supported
            Assert.True(info.Options.UserPresence);
        });
    }
    
    [Theory]
    [WithYubiKey(Capability = DeviceCapabilities.Fido2, MinFirmware = "5.2.0")]
    public async Task GetInfo_FetchesFreshData(YubiKeyTestState device)
    {
        await device.WithFidoSessionAsync(async session =>
        {
            var info1 = await session.GetInfoAsync();
            var info2 = await session.GetInfoAsync();
            
            // Both should have same versions (demonstrating fetch works)
            Assert.Equal(info1.Versions, info2.Versions);
        });
    }
    
    [Theory]
    [WithYubiKey(Capability = DeviceCapabilities.Fido2, MinFirmware = "5.4.0")]
    public async Task GetInfo_YK54Plus_SupportsPinUvAuthToken(YubiKeyTestState device)
    {
        await device.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            
            Assert.True(info.Options.PinUvAuthToken, "YubiKey 5.4+ should support pinUvAuthToken");
        });
    }
    
    [Theory]
    [WithYubiKey(Capability = DeviceCapabilities.Fido2, MinFirmware = "5.7.0")]
    public async Task GetInfo_YK57Plus_SupportsAlwaysUv(YubiKeyTestState device)
    {
        await device.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            
            // YubiKey 5.7+ has alwaysUv option
            Assert.True(info.Options.AlwaysUv is not null, "YubiKey 5.7+ should report alwaysUv");
        });
    }
}
```

### Task 11.3: PIN Integration Tests

```csharp
// Yubico.YubiKit.Fido2/tests/Integration/ClientPinIntegrationTests.cs
using Yubico.YubiKit.Core.Device;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.Fido2.Tests.Extensions;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Fido2.Tests.Integration;

public class ClientPinIntegrationTests
{
    private const string TestPin = "123456";
    
    [Theory]
    [WithYubiKey(Capability = DeviceCapabilities.Fido2, MinFirmware = "5.2.0")]
    public async Task GetPinRetries_ReturnsValidCount(YubiKeyTestState device)
    {
        await device.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            if (!info.Options.ClientPinSupported)
            {
                // Skip if clientPin not supported
                return;
            }
            
            var protocol = new PinUvAuthProtocolV2();
            var clientPin = new ClientPin(session, protocol);
            
            var retries = await clientPin.GetPinRetriesAsync();
            
            // Retries should be between 0 and 8
            Assert.InRange(retries.Count, 0, 8);
        });
    }
    
    [Theory]
    [WithYubiKey(Capability = DeviceCapabilities.Fido2, MinFirmware = "5.2.0")]
    public async Task GetSharedSecret_ReturnsValidKeyAndSecret(YubiKeyTestState device)
    {
        await device.WithFidoSessionAsync(async session =>
        {
            var info = await session.GetInfoAsync();
            if (!info.Options.ClientPinSupported)
            {
                return;
            }
            
            var protocol = new PinUvAuthProtocolV2();
            var clientPin = new ClientPin(session, protocol);
            
            var (keyAgreement, sharedSecret) = await clientPin.GetSharedSecretAsync();
            
            Assert.NotNull(keyAgreement);
            Assert.NotEmpty(sharedSecret);
            Assert.Equal(64, sharedSecret.Length); // HMAC key (32) + AES key (32)
        });
    }
}
```

---

## Phase 12: Documentation & DI

### Task 12.1: README

**Files:**
- Create: `Yubico.YubiKit.Fido2/README.md`

### Task 12.2: DI Registration

**Files:**
- Create: `Yubico.YubiKit.Fido2/src/DependencyInjection.cs`

### Task 12.3: CLAUDE.md for Subdirectory

**Files:**
- Create: `Yubico.YubiKit.Fido2/CLAUDE.md`

---

## Checklist Summary

### Session Package Checklist
- [ ] Public entry point: `IYubiKey.CreateFidoSessionAsync()` extension (C# 14 syntax)
- [ ] Transport docs: SmartCard and FIDO HID supported
- [ ] 3-tier examples in README
- [ ] Model types: `readonly record struct` for small, `sealed class` with `Parse`/`Decode` for larger
- [ ] Session derives from `ApplicationSession`
- [ ] Uses `InitializeCoreAsync()`, no info caching
- [ ] DI registration in `DependencyInjection.cs`
- [ ] Logging via `YubiKitLogging.LoggerFactory`
- [ ] Integrates with existing Core COSE types

### Test Infrastructure Checklist
- [ ] `WithFidoSessionAsync` extension method on `YubiKeyTestState`
- [ ] `WithAuthenticatedFidoSessionAsync` for PIN-protected operations
- [ ] Unit tests with NSubstitute (not Moq)
- [ ] Integration tests use `[WithYubiKey(Capability = DeviceCapabilities.Fido2)]`
- [ ] Firmware minimums via `MinFirmware` attribute parameter

### Design Decisions Summary
| Decision | Choice | Rationale |
|----------|--------|-----------|
| Mocking framework | NSubstitute | Matches existing codebase |
| Info caching | None | `GetInfoAsync()` always fetches fresh |
| CBOR parsing | `Parse(CborReader)`/`Decode(ReadOnlySpan<byte>)` | Type-safe, no `object?` |
| Request building | `CtapRequestBuilder` fluent API | Type-safe, canonical ordering |
| Test attributes | `WithYubiKeyAttribute` | Existing infrastructure |
| Test helpers | Extension methods on `YubiKeyTestState` | Existing pattern |
| COSE types | Reuse from Core | `CoseAlgorithmIdentifier`, `CoseKeyType` |

---

## Execution Order

### Priority 1: Foundation (Phases 1-3)
1. **Phase 1** - Project structure, test project, initial FidoSession stub
2. **Phase 2** - CTAP CBOR infrastructure (CtapRequestBuilder, commands, errors)
3. **Phase 3** - AuthenticatorInfo model, FidoSession core with GetInfo

### Priority 2: Core FIDO2 (Phases 4-5)
4. **Phase 4** - PIN/UV auth protocols (V1, V2), ClientPin
5. **Phase 5** - MakeCredential/GetAssertion with proper request/response models

### Priority 3: Validation (Phase 11)
6. **Phase 11** - Integration tests to validate with real hardware

### Priority 4: Advanced Features (Phases 6-10)
7. **Phase 6** - WebAuthn models
8. **Phase 7** - CredentialManagement
9. **Phase 8** - BioEnrollment
10. **Phase 9** - Config commands
11. **Phase 10** - Large blobs

### Priority 5: YK 5.7+ Features (Phases 10A-10B)
12. **Phase 10A** - Encrypted metadata (encIdentifier, encCredStoreState, PPUAT)
13. **Phase 10B** - WebAuthn/CTAP extensions:
    - 10B.1: Extension Framework (ICtapExtension interface)
    - 10B.2: hmac-secret extension (PRF)
    - 10B.3: credProtect extension
    - 10B.4: credBlob extension
    - 10B.5: credProps extension
    - 10B.6: largeBlob extension
    - 10B.7: minPinLength extension
    - 10B.8: Sign extension
    - 10B.9: Third Party Payment extension
    - 10B.10: Extension Registry and Processors
    - 10B.11: Extension Tests

### Priority 6: Polish (Phase 12)
14. **Phase 12** - Documentation, DI registration, CLAUDE.md

---

## YubiKey Version Feature Matrix

| Feature | Min Version | Phase | Task |
|---------|-------------|-------|------|
| Basic FIDO2 (MakeCredential, GetAssertion) | 5.0 | 5 | - |
| hmac-secret extension | 5.0 | 10B | 10B.2 |
| credProtect extension | 5.2 | 10B | 10B.3 |
| Credential Management | 5.2 | 7 | - |
| Bio Enrollment | 5.2 | 8 | - |
| pinUvAuthToken | 5.4 | 4 | - |
| prf extension | 5.4 | 10B | 10B.2 |
| minPinLength extension | 5.4 | 10B | 10B.7 |
| credBlob extension | 5.5 | 10B | 10B.4 |
| largeBlob extension | 5.5 | 10B | 10B.6 |
| alwaysUv option | 5.7 | 9 | - |
| encIdentifier | 5.7 | 10A | 10A.2 |
| encCredStoreState | 5.8 | 10A | 10A.3 |

---

## All Extensions Summary Table

| Extension | Java Source | C# Task | Priority | Complexity |
|-----------|-------------|---------|----------|------------|
| hmac-secret | HmacSecretExtension.java (527 lines) | 10B.2 | High | High (encryption, protocols) |
| credProtect | CredProtectExtension.java (88 lines) | 10B.3 | High | Low |
| credBlob | CredBlobExtension.java (86 lines) | 10B.4 | Medium | Low |
| credProps | CredPropsExtension.java (70 lines) | 10B.5 | Low | Low (output-only) |
| largeBlob | LargeBlobExtension.java (207 lines) | 10B.6 | Medium | Medium (LargeBlobs cmd) |
| minPinLength | MinPinLengthExtension.java (68 lines) | 10B.7 | Low | Low |
| sign | SignExtension.java (379 lines) | 10B.8 | Low | Medium (key ops) |
| thirdPartyPayment | ThirdPartyPaymentExtension.java (110 lines) | 10B.9 | Low | Low |

---

## Post-Implementation Review (2026-01-17)

### Ralph Loop Session Summary

**Session Stats:**
- Iterations: 9
- Duration: 3.8 hours (~25 min/iteration average)
- Outcome: COMPLETED - Full FIDO2/CTAP2 implementation delivered

**What Was Built:**
- 51 source files implementing complete FIDO2/CTAP2 protocol stack
- 38 test files with 265+ unit tests
- 13 phases completed covering: session foundation, PIN/UV auth protocols (V1+V2), MakeCredential/GetAssertion, credential management, all WebAuthn extensions (hmac-secret, credProtect, credBlob, largeBlob, minPinLength, PRF), bio enrollment, config commands, large blob storage, YK 5.7/5.8 encrypted metadata decryption, integration tests, DI setup, and documentation

### Product Owner Feedback

#### Architecture Assessment

The architecture looks good overall. The Ralph loop demonstrated good self-correction and improvement over iterations.

#### Issues Identified

##### MUST FIX: SmartCard Connection Runtime Errors

Instantiating FidoSession with a SmartCardConnection causes runtime errors. Currently only the HidFidoConnection works.

**Failing tests:**
- `CreateFidoSession_With_SmartCard_CreateAsync` - fails at `SelectAsync()`: `Yubico.YubiKit.Core.SmartCard.ApduException: SELECT command failed: File or application not found (SW=0x6A82)`
- `CreateFidoSession_With_FactoryInstance` - fails at same `SelectAsync()` call

**Note:** All other FidoSession integration tests pass.

##### Interface Extraction Complexity

When sealed classes blocked mocking, agent created additional interfaces:

```csharp
// Before: Can't mock FidoSession
public class FingerprintBioEnrollment(FidoSession session)

// After: Mockable via interface
public class FingerprintBioEnrollment(IBioEnrollmentCommands commands)
```

**Action needed:** The agent forgot to remove these interfaces and patterns later. Should reuse the `IFidoSession` interface throughout the codebase instead of using implementation classes directly.

##### Naming Confusion: FidoHidBackend

New `FidoHidBackend : IFidoBackend` was created. The naming could be confused with ManagementSession's `FidoBackend(IFidoHidProtocol hidProtocol) : IManagementBackend` class.

**Action needed:** Consider renaming these to be more distinct.

##### FidoSession Design

- Implements `IAsyncDisposable` on top of inheriting `IApplicationSession`, which does not implement `IAsyncDisposable`. Consider whether `IApplicationSession` should also implement `IAsyncDisposable` for consistency.
- `MakeCredentialAsync` and `GetAssertionAsync` logic is implemented directly in FidoSession. Consider refactoring into separate command classes, or establish criteria for what logic goes into FidoSession vs command classes.

##### Missing WebAuthn/CTAP Extensions

Extensions that may be missing or need clarification:
- `CredProtectExtensions`
- `HmacSecretExtensions` (possibly named `PrfExtensions` - clarify intended usage, unclear if `hmac-secret-mc` is implemented)
- `ThirdPartyPaymentsExtensions`
- `SignExtensions`

**Action needed:** If not implemented, implement in next run. If implemented, document intended usage.

##### CtapRequestBuilder Inconsistency

`CtapRequestBuilder` is a nice utility class but not used consistently. Some places still manually build requests.

**Action needed:** Refactor to use `CtapRequestBuilder` everywhere for consistency.

##### CredentialManagementModels Deserialization Duplication

CBOR deserialization logic is duplicated across models. Consider extracting common logic into helper methods or a new `CtapResponseParser` static class (analogous to `CtapRequestBuilder`).

##### Testing Infrastructure

Fido tests need to implement existing test infrastructure following patterns from `SecurityDomainSessionTests` and `ManagementSessionTests`, using `[WithYubiKey()]` attributes.

##### IYubiKeyExtensions Validation

Methods accepting `ScpKeyParameters` only work if the underlying connection is a smart card connection. Review these methods to validate connection type before accepting `ScpKeyParameters`.

##### Dead Code

Some unused code exists, e.g., `public int? GetKeyType()` in `AttestedCredentialData` is never used. Either write tests for intended public API usage or remove unused code.

#### SDK-Wide Decisions Needed

1. **Property syntax:** What object setter/getter syntax to use? `get`, `init`, `private set`, `public set`? How to provide validation - on setting properties or separate validation methods?

2. **Logging:** Since there's a static, default, settable `LoggingFactory`, no class should accept an `ILogger` or `ILoggerFactory` in its constructor. Each class should get its logger from the `LoggingFactory`.

**Action needed:** Document conclusions in `CLAUDE.md` and/or `CONTRIBUTING.md`. Consider absorbing `DEV-GUIDE.md` into `CONTRIBUTING.md` to reduce documentation maintenance burden.

#### Ralph Loop Process Improvements

1. The agent made the class unsealed temporarily to test it via an additional interface when it would have been simpler to just make it unsealed for testing, then sealed again afterwards.

2. Should be more explicit when to end a Ralph loop iteration. The agent did multiple phases of the PRD before ending the iteration. Better to end after each PRD phase for clearer iteration boundaries.

---

**Plan complete and saved to `docs/plans/2026-01-16-fido2-session-implementation.md`.**

Ready to execute? I recommend starting with Phase 1, Task 1.1 (Create Project Structure).
