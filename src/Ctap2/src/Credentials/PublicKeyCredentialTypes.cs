// Copyright 2025 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Formats.Cbor;

namespace Yubico.YubiKit.Ctap2.Credentials;

/// <summary>
/// Represents a public key credential descriptor used in exclude/allow lists.
/// </summary>
/// <remarks>
/// <para>
/// Per WebAuthn/CTAP specification, a credential descriptor identifies a credential
/// by its type and ID, with optional transport hints.
/// </para>
/// <para>
/// Constructors borrow ID memory and the transport collection; keep them stable during use and
/// through an awaited request. Parse materializes new ID/transport storage. Transport hints are
/// metadata, not proof that an interface is reachable, and Encode writes only modeled fields.
/// </para>
/// <para>
/// See: https://www.w3.org/TR/webauthn-2/#dictdef-publickeycredentialdescriptor
/// </para>
/// </remarks>
public sealed class PublicKeyCredentialDescriptor
{
    /// <summary>
    /// The credential type (always "public-key" for WebAuthn).
    /// </summary>
    public const string PublicKeyType = "public-key";

    /// <summary>
    /// Gets the credential type.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets the credential ID.
    /// </summary>
    public ReadOnlyMemory<byte> Id { get; }

    /// <summary>
    /// Gets the optional transport hints.
    /// </summary>
    public IReadOnlyList<string>? Transports { get; }

    /// <summary>
    /// Creates a new credential descriptor.
    /// </summary>
    /// <param name="id">The nonempty credential ID; its storage is borrowed without copying.</param>
    /// <param name="type">The credential type (default: "public-key").</param>
    /// <param name="transports">Optional borrowed transport hints; no immutable snapshot is made.</param>
    public PublicKeyCredentialDescriptor(
        ReadOnlyMemory<byte> id,
        string type = PublicKeyType,
        IReadOnlyList<string>? transports = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(type);
        if (id.IsEmpty)
        {
            throw new ArgumentException("Credential ID cannot be empty.", nameof(id));
        }

        Type = type;
        Id = id;
        Transports = transports;
    }

    /// <summary>
    /// Creates a credential descriptor from a credential ID.
    /// </summary>
    /// <param name="credentialId">The credential ID bytes.</param>
    /// <returns>A new credential descriptor.</returns>
    public static PublicKeyCredentialDescriptor FromCredentialId(ReadOnlyMemory<byte> credentialId) =>
        new(credentialId);

    /// <summary>
    /// Parses a credential descriptor from CBOR.
    /// </summary>
    /// <param name="reader">The CBOR reader positioned at the descriptor.</param>
    /// <returns>The parsed credential descriptor.</returns>
    public static PublicKeyCredentialDescriptor Parse(CborReader reader)
    {
        var mapLength = reader.ReadStartMap();

        string? type = null;
        byte[]? id = null;
        List<string>? transports = null;

        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadTextString();
            switch (key)
            {
                case "type":
                    type = reader.ReadTextString();
                    break;
                case "id":
                    id = reader.ReadByteString();
                    break;
                case "transports":
                    transports = [];
                    var arrayLength = reader.ReadStartArray();
                    for (var j = 0; j < arrayLength; j++)
                    {
                        transports.Add(reader.ReadTextString());
                    }
                    reader.ReadEndArray();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }

        reader.ReadEndMap();

        if (type is null || id is null)
        {
            throw new InvalidOperationException("Credential descriptor missing required fields.");
        }

        return new PublicKeyCredentialDescriptor(id, type, transports);
    }

    /// <summary>
    /// Encodes this descriptor as CBOR.
    /// </summary>
    /// <param name="writer">The CBOR writer.</param>
    public void Encode(CborWriter writer)
    {
        var mapSize = Transports is { Count: > 0 } ? 3 : 2;
        writer.WriteStartMap(mapSize);

        writer.WriteTextString("type");
        writer.WriteTextString(Type);

        writer.WriteTextString("id");
        writer.WriteByteString(Id.Span);

        if (Transports is { Count: > 0 })
        {
            writer.WriteTextString("transports");
            writer.WriteStartArray(Transports.Count);
            foreach (var transport in Transports)
            {
                writer.WriteTextString(transport);
            }
            writer.WriteEndArray();
        }

        writer.WriteEndMap();
    }
}

/// <summary>
/// Represents a relying party entity.
/// </summary>
/// <remarks>
/// <para>
/// Per WebAuthn/CTAP specification, the relying party entity describes the
/// relying party responsible for the request.
/// </para>
/// <para>
/// The constructor checks only nonempty ID text, not domain syntax or authorization against an origin.
/// The client must establish a valid RP ID and client-data binding; the RP verifies the ceremony.
/// </para>
/// <para>
/// See: https://www.w3.org/TR/webauthn-2/#dictdef-publickeycredentialrpentity
/// </para>
/// </remarks>
public sealed class PublicKeyCredentialRpEntity
{
    /// <summary>
    /// Gets the relying party identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the relying party name for display.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Creates a new RP entity.
    /// </summary>
    /// <param name="id">The RP identifier (typically a domain name).</param>
    /// <param name="name">Optional display name.</param>
    public PublicKeyCredentialRpEntity(string id, string? name = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        Id = id;
        Name = name;
    }

    /// <summary>
    /// Parses an RP entity from CBOR.
    /// </summary>
    /// <param name="reader">The CBOR reader.</param>
    /// <returns>The parsed RP entity.</returns>
    public static PublicKeyCredentialRpEntity Parse(CborReader reader)
    {
        var mapLength = reader.ReadStartMap();

        string? id = null;
        string? name = null;

        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadTextString();
            switch (key)
            {
                case "id":
                    id = reader.ReadTextString();
                    break;
                case "name":
                    name = reader.ReadTextString();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }

        reader.ReadEndMap();

        if (id is null)
        {
            throw new InvalidOperationException("RP entity missing required 'id' field.");
        }

        return new PublicKeyCredentialRpEntity(id, name);
    }

    /// <summary>
    /// Encodes this RP entity as CBOR.
    /// </summary>
    /// <param name="writer">The CBOR writer.</param>
    public void Encode(CborWriter writer)
    {
        var mapSize = Name is not null ? 2 : 1;
        writer.WriteStartMap(mapSize);

        writer.WriteTextString("id");
        writer.WriteTextString(Id);

        if (Name is not null)
        {
            writer.WriteTextString("name");
            writer.WriteTextString(Name);
        }

        writer.WriteEndMap();
    }
}

/// <summary>
/// Represents account information for credential creation or an authenticator response.
/// </summary>
/// <remarks>
/// <para>
/// Id is an opaque RP-scoped account handle, not display text. WebAuthn L2 §5.4.3 requires RP
/// authentication/authorization decisions to use the ID rather than name/displayName; the handle
/// must not contain personally identifying information. Response identifying fields can be absent
/// and are prohibited without authenticator UV under CTAP 2.1/2.3 §6.2.2.
/// </para>
/// <para>
/// The public constructor requires a nonempty ID of at most 64 bytes and nonempty name/displayName.
/// These are current SDK restrictions: CTAP permits omitted display inputs for privacy and permits
/// an empty account ID (though it recommends avoiding it); WebAuthn's nonempty handle rule is narrower.
/// Parse permits absent display fields but still rejects empty/oversized IDs. Constructors borrow ID
/// memory; Parse creates an independent ID array. Keep borrowed bytes stable during use/transmission.
/// </para>
/// <para>
/// See: https://www.w3.org/TR/webauthn-2/#dictdef-publickeycredentialuserentity
/// </para>
/// </remarks>
public sealed class PublicKeyCredentialUserEntity
{
    /// <summary>
    /// Gets the opaque RP-scoped user handle used for account binding, not a human-readable name.
    /// </summary>
    public ReadOnlyMemory<byte> Id { get; }

    /// <summary>
    /// Gets the human-readable user name.
    /// </summary>
    /// <remarks>
    /// This may be null when parsing a GetAssertion response, as only the user ID is required.
    /// </remarks>
    public string? Name { get; }

    /// <summary>
    /// Gets the human-readable display name.
    /// </summary>
    /// <remarks>
    /// This may be null when parsing a GetAssertion response, as only the user ID is required.
    /// </remarks>
    public string? DisplayName { get; }

    /// <summary>
    /// Creates a new user entity.
    /// </summary>
    /// <param name="id">The nonempty user handle (up to 64 bytes); byte storage is borrowed.</param>
    /// <param name="name">The nonempty name required by this SDK constructor, not an authorization identifier.</param>
    /// <param name="displayName">The nonempty display name required by this SDK constructor.</param>
    public PublicKeyCredentialUserEntity(
        ReadOnlyMemory<byte> id,
        string name,
        string displayName)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(id));
        }
        if (id.Length > 64)
        {
            throw new ArgumentException("User ID cannot exceed 64 bytes.", nameof(id));
        }
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(displayName);

        Id = id;
        Name = name;
        DisplayName = displayName;
    }

    /// <summary>
    /// Internal constructor for parsing responses where name/displayName may be absent.
    /// </summary>
    private PublicKeyCredentialUserEntity(
        ReadOnlyMemory<byte> id,
        string? name,
        string? displayName,
        bool fromParse)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(id));
        }
        if (id.Length > 64)
        {
            throw new ArgumentException("User ID cannot exceed 64 bytes.", nameof(id));
        }

        Id = id;
        Name = name;
        DisplayName = displayName;
    }

    /// <summary>
    /// Parses a user entity from CBOR.
    /// </summary>
    /// <param name="reader">The CBOR reader.</param>
    /// <returns>The parsed user entity.</returns>
    /// <remarks>
    /// Requires an ID and copies its bytes; name/displayName are optional and unknown fields are skipped.
    /// This parser does not filter identifying fields based on UV or complete RP account verification.
    /// </remarks>
    public static PublicKeyCredentialUserEntity Parse(CborReader reader)
    {
        var mapLength = reader.ReadStartMap();

        byte[]? id = null;
        string? name = null;
        string? displayName = null;

        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadTextString();
            switch (key)
            {
                case "id":
                    id = reader.ReadByteString();
                    break;
                case "name":
                    name = reader.ReadTextString();
                    break;
                case "displayName":
                    displayName = reader.ReadTextString();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }

        reader.ReadEndMap();

        if (id is null)
        {
            throw new InvalidOperationException("User entity missing required 'id' field.");
        }

        // Use private constructor since name/displayName are optional in GetAssertion responses
        return new PublicKeyCredentialUserEntity(id, name, displayName, fromParse: true);
    }

    /// <summary>
    /// Encodes this user entity as CBOR.
    /// </summary>
    /// <param name="writer">The CBOR writer.</param>
    public void Encode(CborWriter writer)
    {
        // Count non-null fields
        var fieldCount = 1; // id is always present
        if (Name is not null) fieldCount++;
        if (DisplayName is not null) fieldCount++;

        writer.WriteStartMap(fieldCount);

        writer.WriteTextString("id");
        writer.WriteByteString(Id.Span);

        if (Name is not null)
        {
            writer.WriteTextString("name");
            writer.WriteTextString(Name);
        }

        if (DisplayName is not null)
        {
            writer.WriteTextString("displayName");
            writer.WriteTextString(DisplayName);
        }

        writer.WriteEndMap();
    }
}