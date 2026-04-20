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

namespace Yubico.YubiKit.Piv;

/// <summary>
/// PIV PIN metadata information.
/// </summary>
public readonly record struct PivPinMetadata(
    bool IsDefault,
    int TotalRetries,
    int RetriesRemaining);

/// <summary>
/// PIV PUK metadata information.
/// </summary>
public readonly record struct PivPukMetadata(
    bool IsDefault,
    int TotalRetries,
    int RetriesRemaining);

/// <summary>
/// PIV management key metadata information.
/// </summary>
public readonly record struct PivManagementKeyMetadata(
    PivManagementKeyType KeyType,
    bool IsDefault,
    PivTouchPolicy TouchPolicy);

/// <summary>
/// PIV slot metadata information.
/// </summary>
public readonly record struct PivSlotMetadata(
    PivAlgorithm Algorithm,
    PivPinPolicy PinPolicy,
    PivTouchPolicy TouchPolicy,
    bool IsGenerated,
    ReadOnlyMemory<byte> PublicKey);

/// <summary>
/// PIV biometric metadata information.
/// </summary>
public readonly record struct PivBioMetadata(
    bool IsConfigured,
    int RetriesRemaining,
    bool HasTemporaryPin);