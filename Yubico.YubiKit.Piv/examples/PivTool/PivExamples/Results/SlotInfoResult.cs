// Copyright 2026 Yubico AB
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

using System.Security.Cryptography.X509Certificates;

namespace Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

/// <summary>
/// Information about a single PIV slot.
/// </summary>
public sealed record SlotInfo
{
    /// <summary>
    /// Gets the PIV slot identifier.
    /// </summary>
    public required PivSlot Slot { get; init; }

    /// <summary>
    /// Gets the human-readable name of the slot.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the slot metadata if a key is present.
    /// </summary>
    public PivSlotMetadata? Metadata { get; init; }

    /// <summary>
    /// Gets the certificate stored in the slot.
    /// </summary>
    public X509Certificate2? Certificate { get; init; }

    /// <summary>
    /// Gets whether the slot has a key.
    /// </summary>
    public bool HasKey => Metadata is not null;

    /// <summary>
    /// Gets whether the slot has a certificate.
    /// </summary>
    public bool HasCertificate => Certificate is not null;
}

/// <summary>
/// Result of a slot info query.
/// </summary>
public sealed record SlotInfoResult
{
    /// <summary>
    /// Gets whether the slot info query succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the list of slot information.
    /// </summary>
    public IReadOnlyList<SlotInfo> Slots { get; init; } = [];

    /// <summary>
    /// Gets the error message when the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful slot info result.
    /// </summary>
    public static SlotInfoResult Succeeded(IReadOnlyList<SlotInfo> slots) =>
        new() { Success = true, Slots = slots };

    /// <summary>
    /// Creates a failed slot info result.
    /// </summary>
    public static SlotInfoResult Failed(string error) =>
        new() { Success = false, ErrorMessage = error };
}
