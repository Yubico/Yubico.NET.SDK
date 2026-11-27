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
/// Represents the authenticator's current user-verification retry status.
/// </summary>
/// <remarks>For exhausted built-in UV retries, CTAP 2.1 permits recovery by reset or a successful configured ClientPIN token request. This snapshot does not perform recovery.</remarks>
public sealed class UserVerificationRetryStatus
{
    /// <summary>
    /// Gets the built-in UV retry count reported by the authenticator.
    /// </summary>
    public int RetriesRemaining { get; }

    /// <summary>
    /// Gets <c>false</c>; UV retry responses do not report a power-cycle indication.
    /// </summary>
    /// <remarks>Use <see cref="RetriesRemaining"/> for the UV retry count; this property is not evidence about recovery.</remarks>
    public bool PowerCycleRequired => false;

    internal UserVerificationRetryStatus(int retriesRemaining)
    {
        RetriesRemaining = retriesRemaining;
    }
}