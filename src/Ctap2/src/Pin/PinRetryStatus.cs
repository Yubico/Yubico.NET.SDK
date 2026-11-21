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

namespace Yubico.YubiKit.Ctap2.Pin;

/// <summary>
/// Represents the authenticator's current PIN retry status.
/// </summary>
/// <remarks>A snapshot, not permission to retry. PIN_AUTH_BLOCKED requires a power cycle; PIN_BLOCKED means retries are exhausted and reset is required.</remarks>
public sealed class PinRetryStatus
{
    /// <summary>
    /// Gets the PIN retry count reported by the authenticator.
    /// </summary>
    public int RetriesRemaining { get; }

    /// <summary>
    /// Gets the power-cycle indication, or <c>null</c> if the authenticator did not report it.
    /// </summary>
    /// <remarks>Use <c>is true</c> to act on an affirmative indication. A snapshot is not permission to retry.</remarks>
    public bool? PowerCycleRequired { get; }

    internal PinRetryStatus(int retriesRemaining, bool? powerCycleRequired)
    {
        RetriesRemaining = retriesRemaining;
        PowerCycleRequired = powerCycleRequired;
    }
}