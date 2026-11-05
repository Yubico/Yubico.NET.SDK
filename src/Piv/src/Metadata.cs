// Copyright 2024 Yubico AB
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

using Yubico.YubiKit.Core.Cryptography;

namespace Yubico.YubiKit.Piv;

/// <summary>
/// PIV PIN metadata information.
/// </summary>
/// <param name="IsDefault">Whether the device reports the factory PIN is installed.</param>
/// <param name="TotalRetries">Configured retry limit, measured in attempts.</param>
/// <param name="RetriesRemaining">Attempts remaining before PIN blocking.</param>
public readonly record struct PinMetadata(
    bool IsDefault,
    int TotalRetries,
    int RetriesRemaining);

/// <summary>
/// PIV PUK metadata information.
/// </summary>
/// <param name="IsDefault">Whether the device reports the factory PUK is installed.</param>
/// <param name="TotalRetries">Configured retry limit, measured in attempts.</param>
/// <param name="RetriesRemaining">Attempts remaining before PUK blocking.</param>
public readonly record struct PukMetadata(
    bool IsDefault,
    int TotalRetries,
    int RetriesRemaining);

/// <summary>
/// PIV management key metadata information.
/// </summary>
/// <param name="KeyType">Recognized symmetric type, or null for an unmodeled algorithm; inspect RawAlgorithmId.</param>
/// <param name="IsDefault">Whether the device reports the public factory key is installed.</param>
/// <param name="TouchPolicy">Device-reported management-key touch policy.</param>
public readonly record struct ManagementKeyMetadata(
    KeyType? KeyType,
    bool IsDefault,
    TouchPolicy TouchPolicy)
{
    /// <summary>Gets the raw management-key algorithm identifier, including identifiers not yet modeled.</summary>
    public byte RawAlgorithmId { get; init; } = KeyType is { } known ? KeyTypeEncoding.Encode(known) : (byte)0;
    /// <summary>Gets the complete metadata response TLVs.</summary>
    public ReadOnlyMemory<byte> RawData { get; init; }
}

/// <summary>
/// PIV slot metadata information.
/// </summary>
/// <param name="KeyType">Recognized key type, or null for an unmodeled algorithm. Null here does not mean an empty slot.</param>
/// <param name="PinPolicy">Device-reported private-key verification policy.</param>
/// <param name="TouchPolicy">Device-reported private-key touch policy.</param>
/// <param name="IsGenerated">Whether the device reports on-device generation rather than import.</param>
/// <param name="PublicKey">Raw PIV public-key TLVs: RSA modulus/exponent tags 81/82 or EC point tag 86,
/// rather than SubjectPublicKeyInfo. Use SlotMetadataExtensions for disposable RSA/ECDsa host objects.</param>
public readonly record struct SlotMetadata(
    KeyType? KeyType,
    PinPolicy PinPolicy,
    TouchPolicy TouchPolicy,
    bool IsGenerated,
    ReadOnlyMemory<byte> PublicKey)
{
    /// <summary>Gets the PIV algorithm identifier, including identifiers not yet modeled by the SDK.</summary>
    public byte RawAlgorithmId { get; init; } = KeyType is { } value ? KeyTypeEncoding.Encode(value) : (byte)0;

    /// <summary>Gets the complete metadata response TLV sequence, including unknown fields.</summary>
    public ReadOnlyMemory<byte> RawData { get; init; }

    /// <summary>Requires a recognized mapped PIV type; operations separately enforce their asymmetric/symmetric subset.</summary>
    internal KeyType RequireKeyType()
    {
        var known = KeyType ?? throw new NotSupportedException($"PIV algorithm 0x{RawAlgorithmId:X2} is not supported by this SDK.");
        _ = KeyTypeEncoding.Encode(known);
        return known;
    }
}

/// <summary>
/// PIV biometric metadata information.
/// </summary>
/// <param name="IsConfigured">Whether fingerprint verification is configured; capability and enrollment are separate.</param>
/// <param name="RetriesRemaining">Device-reported biometric verification attempts remaining.</param>
/// <param name="HasTemporaryPin">Whether the device reports a temporary PIN is available.</param>
public readonly record struct BioMetadata(
    bool IsConfigured,
    int RetriesRemaining,
    bool HasTemporaryPin);