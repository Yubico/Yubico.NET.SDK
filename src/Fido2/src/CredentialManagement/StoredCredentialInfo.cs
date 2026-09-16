// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Formats.Cbor;
using System.Security.Cryptography;
using Yubico.YubiKit.Fido2.Credentials;

namespace Yubico.YubiKit.Fido2.CredentialManagement;

/// <summary>
/// Represents a stored discoverable credential.
/// </summary>
public sealed class StoredCredentialInfo : IDisposable
{
    private readonly byte[] _ownedResponse;
    private readonly ReadOnlyMemory<byte>? _largeBlobKey;
    private bool _disposed;

    /// <summary>
    /// Gets the complete original CBOR-encoded authenticator response.
    /// </summary>
    /// <remarks>
    /// This view is valid until this instance is disposed. Copy it before disposal if it must be
    /// retained. Disposal clears only the private response copy owned by this instance; it does not
    /// clear the buffer passed to <see cref="Decode(ReadOnlyMemory{byte})"/> or transport buffers.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public ReadOnlyMemory<byte> RawData
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _ownedResponse;
        }
    }

    /// <summary>
    /// Gets the user entity associated with the credential.
    /// </summary>
    public PublicKeyCredentialUserEntity User { get; }

    /// <summary>
    /// Gets the credential ID (public key credential descriptor).
    /// </summary>
    public PublicKeyCredentialDescriptor CredentialId { get; }

    /// <summary>
    /// Gets the COSE public key.
    /// </summary>
    public ReadOnlyMemory<byte> PublicKey { get; }

    /// <summary>
    /// Gets the total number of credentials for this RP (only present in first response).
    /// </summary>
    public int? TotalCredentials { get; }

    /// <summary>
    /// Gets the credential protection policy.
    /// </summary>
    public int? CredProtectPolicy { get; }

    /// <summary>
    /// Gets the large blob key associated with this credential.
    /// </summary>
    /// <remarks>
    /// This is a view into the owned raw response and is valid until this instance is disposed.
    /// Copy it before disposal if it must be retained.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public ReadOnlyMemory<byte>? LargeBlobKey
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _largeBlobKey;
        }
    }

    /// <summary>
    /// Gets the third party payment flag.
    /// </summary>
    public bool? ThirdPartyPayment { get; }

    private StoredCredentialInfo(
        byte[] ownedResponse,
        PublicKeyCredentialUserEntity user,
        PublicKeyCredentialDescriptor credentialId,
        ReadOnlyMemory<byte> publicKey,
        int? totalCredentials,
        int? credProtectPolicy,
        ReadOnlyMemory<byte>? largeBlobKey,
        bool? thirdPartyPayment)
    {
        _ownedResponse = ownedResponse;
        _largeBlobKey = largeBlobKey;
        User = user;
        CredentialId = credentialId;
        PublicKey = publicKey;
        TotalCredentials = totalCredentials;
        CredProtectPolicy = credProtectPolicy;
        ThirdPartyPayment = thirdPartyPayment;
    }

    /// <summary>
    /// Decodes stored credential info from a CBOR response.
    /// </summary>
    /// <param name="data">The borrowed CBOR-encoded response data. This method does not modify it.</param>
    /// <returns>The decoded credential info, which owns a private response copy and must be disposed.</returns>
    public static StoredCredentialInfo Decode(ReadOnlyMemory<byte> data)
    {
        byte[] ownedResponse = data.ToArray();

        try
        {
            var reader = new CborReader(ownedResponse, CborConformanceMode.Ctap2Canonical);

            PublicKeyCredentialUserEntity? user = null;
            PublicKeyCredentialDescriptor? credentialId = null;
            byte[]? publicKey = null;
            int? totalCredentials = null;
            int? credProtectPolicy = null;
            ReadOnlyMemory<byte>? largeBlobKey = null;
            bool? thirdPartyPayment = null;

            var mapLength = reader.ReadStartMap();
            for (var i = 0; i < mapLength; i++)
            {
                var key = reader.ReadInt32();
                switch (key)
                {
                    case 6: // user
                        user = PublicKeyCredentialUserEntity.Parse(reader);
                        break;
                    case 7: // credentialID (PublicKeyCredentialDescriptor)
                        credentialId = PublicKeyCredentialDescriptor.Parse(reader);
                        break;
                    case 8: // publicKey (COSE_Key)
                        publicKey = reader.ReadEncodedValue().ToArray();
                        break;
                    case 9: // totalCredentials
                        totalCredentials = reader.ReadInt32();
                        break;
                    case 10: // credProtect
                        credProtectPolicy = reader.ReadInt32();
                        break;
                    case 11: // largeBlobKey
                        largeBlobKey = reader.ReadDefiniteLengthByteString();
                        break;
                    case 12: // thirdPartyPayment (CTAP 2.2)
                        thirdPartyPayment = reader.ReadBoolean();
                        break;
                    default:
                        reader.SkipValue();
                        break;
                }
            }
            reader.ReadEndMap();

            if (user is null || credentialId is null || publicKey is null)
            {
                throw new InvalidOperationException("Invalid credential info response: missing required fields.");
            }

            return new StoredCredentialInfo(
                ownedResponse,
                user,
                credentialId,
                publicKey,
                totalCredentials,
                credProtectPolicy,
                largeBlobKey,
                thirdPartyPayment);
        }
        catch
        {
            CryptographicOperations.ZeroMemory(ownedResponse);
            throw;
        }
    }

    /// <summary>
    /// Clears the complete private response copy, including unrecognized fields.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CryptographicOperations.ZeroMemory(_ownedResponse);
        _disposed = true;
    }
}