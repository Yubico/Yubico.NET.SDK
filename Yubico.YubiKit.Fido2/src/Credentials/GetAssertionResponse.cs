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

namespace Yubico.YubiKit.Fido2.Credentials;

/// <summary>
/// Represents the response from authenticatorGetAssertion.
/// </summary>
/// <remarks>
/// <para>
/// CTAP2 getAssertion response structure:
/// - 0x01: credential (map, optional) - selected credential
/// - 0x02: authData (byte string) - authenticator data
/// - 0x03: signature (byte string) - assertion signature
/// - 0x04: user (map, optional) - user entity (discoverable credentials only)
/// - 0x05: numberOfCredentials (uint, optional) - total credentials for first response
/// - 0x06: userSelected (bool, optional) - user selected this credential
/// - 0x07: largeBlobKey (byte string, optional) - large blob key
/// </para>
/// </remarks>
public sealed class GetAssertionResponse
{
    /// <summary>
    /// Gets the credential descriptor for the selected credential.
    /// </summary>
    /// <remarks>
    /// May be absent if only one credential matches (pre-CTAP2.1) or
    /// if using non-discoverable credentials.
    /// </remarks>
    public PublicKeyCredentialDescriptor? Credential { get; }
    
    /// <summary>
    /// Gets the authenticator data.
    /// </summary>
    public AuthenticatorData AuthenticatorData { get; }
    
    /// <summary>
    /// Gets the raw authenticator data bytes.
    /// </summary>
    public ReadOnlyMemory<byte> AuthenticatorDataRaw { get; }
    
    /// <summary>
    /// Gets the assertion signature.
    /// </summary>
    public ReadOnlyMemory<byte> Signature { get; }
    
    /// <summary>
    /// Gets the user entity for discoverable credentials.
    /// </summary>
    /// <remarks>
    /// Only present for discoverable credentials when user verification is performed.
    /// </remarks>
    public PublicKeyCredentialUserEntity? User { get; }
    
    /// <summary>
    /// Gets the total number of credentials matching the request.
    /// </summary>
    /// <remarks>
    /// Only present in the first response when multiple credentials match.
    /// Use GetNextAssertionAsync to retrieve additional assertions.
    /// </remarks>
    public int? NumberOfCredentials { get; }
    
    /// <summary>
    /// Gets whether the user explicitly selected this credential.
    /// </summary>
    public bool? UserSelected { get; }
    
    /// <summary>
    /// Gets the large blob key for this credential.
    /// </summary>
    public ReadOnlyMemory<byte>? LargeBlobKey { get; }
    
    /// <summary>
    /// Gets the CBOR-encoded extension outputs, if any.
    /// </summary>
    public ReadOnlyMemory<byte>? ExtensionOutputs { get; }
    
    private GetAssertionResponse(
        PublicKeyCredentialDescriptor? credential,
        AuthenticatorData authenticatorData,
        ReadOnlyMemory<byte> authenticatorDataRaw,
        ReadOnlyMemory<byte> signature,
        PublicKeyCredentialUserEntity? user,
        int? numberOfCredentials,
        bool? userSelected,
        ReadOnlyMemory<byte>? largeBlobKey,
        ReadOnlyMemory<byte>? extensionOutputs)
    {
        Credential = credential;
        AuthenticatorData = authenticatorData;
        AuthenticatorDataRaw = authenticatorDataRaw;
        Signature = signature;
        User = user;
        NumberOfCredentials = numberOfCredentials;
        UserSelected = userSelected;
        LargeBlobKey = largeBlobKey;
        ExtensionOutputs = extensionOutputs;
    }
    
    /// <summary>
    /// Parses a getAssertion response from CBOR-encoded data.
    /// </summary>
    /// <param name="data">The CBOR-encoded response (excluding status byte).</param>
    /// <returns>The parsed response.</returns>
    public static GetAssertionResponse Decode(ReadOnlyMemory<byte> data)
    {
        var reader = new CborReader(data, CborConformanceMode.Lax);
        return Decode(reader);
    }
    
    /// <summary>
    /// Parses a getAssertion response from a CBOR reader.
    /// </summary>
    /// <param name="reader">The CBOR reader.</param>
    /// <returns>The parsed response.</returns>
    public static GetAssertionResponse Decode(CborReader reader)
    {
        var mapLength = reader.ReadStartMap();
        
        PublicKeyCredentialDescriptor? credential = null;
        byte[]? authDataRaw = null;
        AuthenticatorData? authData = null;
        byte[]? signature = null;
        PublicKeyCredentialUserEntity? user = null;
        int? numberOfCredentials = null;
        bool? userSelected = null;
        byte[]? largeBlobKey = null;
        ReadOnlyMemory<byte>? extensionOutputs = null;
        
        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadInt32();
            switch (key)
            {
                case 1: // credential
                    credential = PublicKeyCredentialDescriptor.Parse(reader);
                    break;
                case 2: // authData
                    authDataRaw = reader.ReadByteString();
                    authData = AuthenticatorData.Parse(authDataRaw);
                    if (authData.HasExtensions && authData.Extensions.HasValue)
                    {
                        extensionOutputs = authData.Extensions;
                    }
                    break;
                case 3: // signature
                    signature = reader.ReadByteString();
                    break;
                case 4: // user
                    user = PublicKeyCredentialUserEntity.Parse(reader);
                    break;
                case 5: // numberOfCredentials
                    numberOfCredentials = (int)reader.ReadUInt32();
                    break;
                case 6: // userSelected
                    userSelected = reader.ReadBoolean();
                    break;
                case 7: // largeBlobKey
                    largeBlobKey = reader.ReadByteString();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }
        
        reader.ReadEndMap();
        
        if (authData is null || authDataRaw is null || signature is null)
        {
            throw new InvalidOperationException("GetAssertion response missing required fields.");
        }
        
        return new GetAssertionResponse(
            credential,
            authData,
            authDataRaw,
            signature,
            user,
            numberOfCredentials,
            userSelected,
            largeBlobKey,
            extensionOutputs);
    }
    
    /// <summary>
    /// Gets the credential ID from the credential descriptor or returns empty.
    /// </summary>
    public ReadOnlyMemory<byte> GetCredentialId() => Credential?.Id ?? ReadOnlyMemory<byte>.Empty;
    
    /// <summary>
    /// Gets the user handle from the user entity or returns empty.
    /// </summary>
    public ReadOnlyMemory<byte> GetUserHandle() => User?.Id ?? ReadOnlyMemory<byte>.Empty;
}
