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
/// Represents the response from authenticatorGetAssertion.
/// </summary>
/// <remarks>
/// <para>
/// CTAP2 getAssertion response structure:
/// - 0x01: credential (map) - selected credential; see <see cref="Credential"/> for version conditions
/// - 0x02: authData (byte string) - authenticator data
/// - 0x03: signature (byte string) - assertion signature
/// - 0x04: user (map, optional) - authenticator-supplied account information
/// - 0x05: numberOfCredentials (uint, optional) - total credentials for first response
/// - 0x06: userSelected (bool, optional) - user selected this credential
/// - 0x07: largeBlobKey (byte string, optional) - large blob key
/// </para>
/// <para>
/// CTAP 2.3 §6.2.2 also defines unsigned extension outputs at key 0x08. This decoder skips that
/// field and other unknown outer fields; it does not retain a complete response envelope.
/// </para>
/// <para>
/// Decoding copies byte strings, including the signature, IDs, large-blob key and authenticator data.
/// Authenticator-data parsing creates further independent copies; <see cref="ExtensionOutputs"/>
/// refers to its extension copy. Neither this result nor session disposal clears these buffers.
/// Large-blob keys and secret-derived extension data are sensitive: do not log them or assume that
/// clearing the original input clears the decoded copies. This type provides no disposal/zeroing API.
/// </para>
/// <para>
/// Parsing is not authentication. The RP must verify the signature and client-data challenge/origin,
/// credential/account binding, RP ID, required UP/UV and counter policy before accepting the assertion.
/// </para>
/// </remarks>
public sealed class GetAssertionResponse
{
    /// <summary>
    /// Gets the credential descriptor for the selected credential.
    /// </summary>
    /// <remarks>
    /// CTAP 2.0 §5.2 permits omission only when the request allowList contains exactly one descriptor,
    /// not merely when one credential matches. CTAP 2.1/2.3 §6.2.2 require this field. This decoder has
    /// no request/version context and exposes absence as null; it does not recover the requested ID.
    /// A caller handling the 2.0 exception must retain the known singleton descriptor for association.
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
    /// Gets the received assertion signature bytes; their encoding depends on the credential algorithm.
    /// </summary>
    public ReadOnlyMemory<byte> Signature { get; }

    /// <summary>
    /// Gets the authenticator-supplied user entity, if present.
    /// </summary>
    /// <remarks>
    /// CTAP 2.1/2.3 §6.2.2 require at least the opaque user ID for discoverable credentials and permit
    /// a user entity for server-side credentials. The ID is not conditional on UV. Authenticators MUST
    /// NOT return identifying name/displayName/icon fields without UV; this decoder passes modeled
    /// name/displayName through rather than enforcing that privacy rule, and skips icon. Use the ID,
    /// not display fields, for RP account binding and authorization. Names can be absent even with UV.
    /// </remarks>
    public PublicKeyCredentialUserEntity? User { get; }

    /// <summary>
    /// Gets the total number of credentials matching the request.
    /// </summary>
    /// <remarks>
    /// CTAP defaults an absent count to one; authenticator account selection can omit it even when
    /// multiple accounts exist. When a valid first-response count N exceeds one, collect N−1 next
    /// assertions promptly, without unrelated authenticator operations between calls. The SDK does
    /// not reserve the sequence or validate this uint32-to-int conversion against Int32.MaxValue.
    /// Next-assertion responses omit the count.
    /// </remarks>
    public int? NumberOfCredentials { get; }

    /// <summary>
    /// Gets whether the user explicitly selected this credential.
    /// </summary>
    /// <remarks>Reports the received field, not an SDK account-picker result. CTAP defaults absence to false.</remarks>
    public bool? UserSelected { get; }

    /// <summary>
    /// Gets the secret symmetric large-blob storage key from response key 0x07, if supplied.
    /// </summary>
    /// <remarks>Presence does not establish the required key length; the decoder does not check it.</remarks>
    public ReadOnlyMemory<byte>? LargeBlobKey { get; }

    /// <summary>
    /// Gets the signed authenticator-data extension bytes, if the ED bit is set.
    /// </summary>
    /// <remarks>These bytes are not the unsigned output map at response key 0x08, and are not verified here.</remarks>
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
    /// <remarks>
    /// Uses a lax CBOR reader and checks authData/signature presence, but not signature nonemptiness,
    /// version-dependent fields, all conditional rules or complete input consumption. CBOR errors can
    /// raise <see cref="CborContentException"/>; missing required modeled fields raise
    /// <see cref="InvalidOperationException"/>. Authenticator-data and entity checks can also fail.
    /// </remarks>
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
    /// <remarks>Consumes one response map using the supplied reader; the same model/verification limits apply.</remarks>
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
            // byte[]? → ReadOnlyMemory<byte>? converts a null array to an EMPTY memory; keep absent as null.
            largeBlobKey is null ? null : (ReadOnlyMemory<byte>?)largeBlobKey,
            extensionOutputs);
    }

    /// <summary>
    /// Gets the credential ID from the credential descriptor or returns empty.
    /// </summary>
    /// <remarks>Empty reports a missing descriptor, not a recovered request ID or a failed signature verification.</remarks>
    public ReadOnlyMemory<byte> GetCredentialId() => Credential?.Id ?? ReadOnlyMemory<byte>.Empty;

    /// <summary>
    /// Gets the user handle from the user entity or returns empty.
    /// </summary>
    /// <remarks>Empty reports absence of the entity, not proof that no account exists or authentication failed.</remarks>
    public ReadOnlyMemory<byte> GetUserHandle() => User?.Id ?? ReadOnlyMemory<byte>.Empty;
}