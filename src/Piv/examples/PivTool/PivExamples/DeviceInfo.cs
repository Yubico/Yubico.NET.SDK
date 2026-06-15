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
/// Demonstrates retrieving PIV-specific information from a YubiKey.
/// </summary>
/// <remarks>
/// <para>
/// This class provides examples for querying PIV retry counters and metadata.
/// For full device information (firmware, serial, etc.), use the Management module.
/// </para>
/// </remarks>
public static class DeviceInfoQuery
{
    /// <summary>
    /// Gets PIV retry information from a session.
    /// </summary>
    /// <param name="session">A PIV session.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing retry information or error.</returns>
    /// <example>
    /// <code>
    /// await using var session = await device.CreatePivSessionAsync(ct);
    /// 
    /// var result = await DeviceInfoQuery.GetPivRetryInfoAsync(session, ct);
    /// if (result.Success)
    /// {
    ///     Console.WriteLine($"PIN retries: {result.PinRetriesRemaining}");
    ///     Console.WriteLine($"PUK retries: {result.PukRetriesRemaining}");
    /// }
    /// </code>
    /// </example>
    public static async Task<DeviceInfoResult> GetPivRetryInfoAsync(
        IPivSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            int? pinRetries = null;
            int? pukRetries = null;

            try
            {
                pinRetries = await session.GetPinAttemptsAsync(cancellationToken);
            }
            catch
            {
                // Ignore errors getting PIN retries
            }

            try
            {
                var pukMetadata = await session.GetPukMetadataAsync(cancellationToken);
                pukRetries = pukMetadata.RetriesRemaining;
            }
            catch (NotSupportedException)
            {
                // Metadata not supported on older firmware
            }
            catch
            {
                // Ignore other errors
            }

            // We don't have direct access to DeviceInfo through IPivSession,
            // so we return a partial result with just retry info
            return DeviceInfoResult.RetryInfoOnly(pinRetries, pukRetries);
        }
        catch (Exception ex)
        {
            return DeviceInfoResult.Failed($"Failed to get PIV info: {ex.Message}");
        }
    }
}
