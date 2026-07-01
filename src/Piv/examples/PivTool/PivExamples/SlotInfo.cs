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

using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

namespace Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;

/// <summary>
/// Demonstrates querying PIV slot information from a YubiKey.
/// </summary>
/// <remarks>
/// <para>
/// This class provides examples for querying the status of PIV slots,
/// including whether keys and certificates are present, and their metadata.
/// </para>
/// </remarks>
public static class SlotInfoQuery
{
    private static readonly (PivSlot Slot, string Name)[] StandardSlots =
    [
        (PivSlot.Authentication, "Authentication (9A)"),
        (PivSlot.Signature, "Digital Signature (9C)"),
        (PivSlot.KeyManagement, "Key Management (9D)"),
        (PivSlot.CardAuthentication, "Card Authentication (9E)")
    ];

    /// <summary>
    /// Gets information about all standard PIV slots.
    /// </summary>
    /// <param name="session">A PIV session.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing slot information or error.</returns>
    /// <example>
    /// <code>
    /// await using var session = await device.CreatePivSessionAsync(ct);
    /// 
    /// var result = await SlotInfoQuery.GetAllSlotsInfoAsync(session, ct);
    /// if (result.Success)
    /// {
    ///     foreach (var slot in result.Slots)
    ///     {
    ///         Console.WriteLine($"{slot.Name}: Key={slot.HasKey}, Cert={slot.HasCertificate}");
    ///     }
    /// }
    /// </code>
    /// </example>
    public static async Task<SlotInfoResult> GetAllSlotsInfoAsync(
        IPivSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            var slots = new List<SlotInfo>();

            foreach (var (slot, name) in StandardSlots)
            {
                var slotInfo = await GetSlotInfoAsync(session, slot, name, cancellationToken);
                slots.Add(slotInfo);
            }

            return SlotInfoResult.Succeeded(slots);
        }
        catch (Exception ex)
        {
            return SlotInfoResult.Failed($"Failed to get slot info: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets information about a specific PIV slot.
    /// </summary>
    /// <param name="session">A PIV session.</param>
    /// <param name="slot">The slot to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing slot information or error.</returns>
    public static async Task<SlotInfoResult> GetSlotInfoAsync(
        IPivSession session,
        PivSlot slot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            var name = GetSlotName(slot);
            var slotInfo = await GetSlotInfoAsync(session, slot, name, cancellationToken);
            return SlotInfoResult.Succeeded([slotInfo]);
        }
        catch (Exception ex)
        {
            return SlotInfoResult.Failed($"Failed to get slot info: {ex.Message}");
        }
    }

    private static async Task<SlotInfo> GetSlotInfoAsync(
        IPivSession session,
        PivSlot slot,
        string name,
        CancellationToken cancellationToken)
    {
        PivSlotMetadata? metadata = null;
        try
        {
            metadata = await session.GetSlotMetadataAsync(slot, cancellationToken);
        }
        catch (NotSupportedException)
        {
            // Metadata not supported on older firmware
        }

        var certificate = await session.GetCertificateAsync(slot, cancellationToken);

        return new SlotInfo
        {
            Slot = slot,
            Name = name,
            Metadata = metadata,
            Certificate = certificate
        };
    }

    private static string GetSlotName(PivSlot slot)
    {
        return slot switch
        {
            PivSlot.Authentication => "Authentication (9A)",
            PivSlot.Signature => "Digital Signature (9C)",
            PivSlot.KeyManagement => "Key Management (9D)",
            PivSlot.CardAuthentication => "Card Authentication (9E)",
            PivSlot.Attestation => "Attestation (F9)",
            _ => $"Slot {slot}"
        };
    }
}
