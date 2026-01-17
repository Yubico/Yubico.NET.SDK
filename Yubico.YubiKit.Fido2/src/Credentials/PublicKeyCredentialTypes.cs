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
using Yubico.YubiKit.Fido2.Cbor;

namespace Yubico.YubiKit.Fido2.Credentials;

/// <summary>
/// Represents a public key credential descriptor used in exclude/allow lists.
/// </summary>
/// <remarks>
/// <para>
/// Per WebAuthn/CTAP specification, a credential descriptor identifies a credential
/// by its type and ID, with optional transport hints.
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
    /// <param name="id">The credential ID.</param>
    /// <param name="type">The credential type (default: "public-key").</param>
    /// <param name="transports">Optional transport hints.</param>
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
/// Represents a user entity for credential creation.
/// </summary>
/// <remarks>
/// <para>
/// Per WebAuthn/CTAP specification, the user entity describes the user
/// account for which the credential is being created.
/// </para>
/// <para>
/// See: https://www.w3.org/TR/webauthn-2/#dictdef-publickeycredentialuserentity
/// </para>
/// </remarks>
public sealed class PublicKeyCredentialUserEntity
{
    /// <summary>
    /// Gets the user handle (unique identifier for the user).
    /// </summary>
    public ReadOnlyMemory<byte> Id { get; }
    
    /// <summary>
    /// Gets the human-readable user name.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Gets the human-readable display name.
    /// </summary>
    public string DisplayName { get; }
    
    /// <summary>
    /// Creates a new user entity.
    /// </summary>
    /// <param name="id">The user handle (up to 64 bytes).</param>
    /// <param name="name">The user name.</param>
    /// <param name="displayName">The display name.</param>
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
    /// Parses a user entity from CBOR.
    /// </summary>
    /// <param name="reader">The CBOR reader.</param>
    /// <returns>The parsed user entity.</returns>
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
        
        if (id is null || name is null || displayName is null)
        {
            throw new InvalidOperationException("User entity missing required fields.");
        }
        
        return new PublicKeyCredentialUserEntity(id, name, displayName);
    }
    
    /// <summary>
    /// Encodes this user entity as CBOR.
    /// </summary>
    /// <param name="writer">The CBOR writer.</param>
    public void Encode(CborWriter writer)
    {
        writer.WriteStartMap(3);
        
        writer.WriteTextString("id");
        writer.WriteByteString(Id.Span);
        
        writer.WriteTextString("name");
        writer.WriteTextString(Name);
        
        writer.WriteTextString("displayName");
        writer.WriteTextString(DisplayName);
        
        writer.WriteEndMap();
    }
}
