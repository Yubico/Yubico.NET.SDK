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
/// Demonstrates PIV application reset operations.
/// </summary>
/// <remarks>
/// <para>
/// WARNING: Resetting the PIV application permanently destroys all PIV data,
/// keys, and certificates. This operation cannot be undone.
/// </para>
/// <para>
/// A reset can only be performed when both PIN and PUK are blocked
/// (zero retries remaining), or when biometrics are not configured.
/// </para>
/// </remarks>
public static class Reset
{
    /// <summary>
    /// Resets the PIV application to factory defaults.
    /// </summary>
    /// <param name="session">A PIV session.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    /// <example>
    /// <code>
    /// // WARNING: This destroys all PIV data!
    /// await using var session = await device.CreatePivSessionAsync(ct);
    /// 
    /// // Ensure user confirms this destructive action
    /// if (userConfirmedReset)
    /// {
    ///     var result = await Reset.ResetPivApplicationAsync(session, ct);
    ///     if (result.Success)
    ///     {
    ///         Console.WriteLine("PIV application reset to factory defaults.");
    ///     }
    /// }
    /// </code>
    /// </example>
    public static async Task<ResetResult> ResetPivApplicationAsync(
        IPivSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            await session.ResetAsync(cancellationToken);
            return ResetResult.Succeeded();
        }
        catch (InvalidOperationException ex)
        {
            return ResetResult.Failed($"Cannot reset: {ex.Message}. Biometrics may be configured.");
        }
        catch (Exception ex)
        {
            return ResetResult.Failed($"Reset failed: {ex.Message}");
        }
    }
}
