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
public readonly record struct PinMetadata(
    bool IsDefault,
    int TotalRetries,
    int RetriesRemaining);

/// <summary>
/// PIV PUK metadata information.
/// </summary>
public readonly record struct PukMetadata(
    bool IsDefault,
    int TotalRetries,
    int RetriesRemaining);

/// <summary>
/// PIV management key metadata information.
/// </summary>
public readonly record struct ManagementKeyMetadata(
    PivManagementKeyType KeyType,
    bool IsDefault,
    TouchPolicy TouchPolicy);

/// <summary>
/// PIV slot metadata information.
/// </summary>
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

    internal KeyType RequireKeyType() => KeyType ?? throw new NotSupportedException(
        $"PIV algorithm 0x{RawAlgorithmId:X2} is not supported by this SDK.");
}

/// <summary>
/// PIV biometric metadata information.
/// </summary>
public readonly record struct BioMetadata(
    bool IsConfigured,
    int RetriesRemaining,
    bool HasTemporaryPin);