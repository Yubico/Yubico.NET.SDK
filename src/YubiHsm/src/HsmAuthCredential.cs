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

namespace Yubico.YubiKit.YubiHsm;

/// <summary>
///     Represents a credential stored in the YubiHSM Auth applet.
///     Parsed from the LIST command response (TAG_LABEL_LIST).
/// </summary>
/// <param name="Label">The credential label (1-64 UTF-8 bytes).</param>
/// <param name="Algorithm">The algorithm used by this credential.</param>
/// <param name="RetriesRemaining">
///     The number of authentication attempts remaining before this credential is permanently deleted by
///     the device. Hardware-verified: decrements on each failed credential-password authentication
///     attempt (observed 8 to 7 after one failed <see cref="IHsmAuthSession.CalculateSessionKeysSymmetricAsync"/>
///     attempt). Behavior on successful authentication was not separately hardware-verified; per the
///     YubiHSM Auth applet's design it is not expected to change on success. A fresh credential starts at 8.
/// </param>
/// <param name="TouchRequired">Whether touch is required to use this credential, or <c>null</c> if unknown.</param>
public sealed record HsmAuthCredential(
    string Label,
    HsmAuthAlgorithm Algorithm,
    int RetriesRemaining,
    bool? TouchRequired) : IComparable<HsmAuthCredential>
{
    /// <inheritdoc />
    public int CompareTo(HsmAuthCredential? other)
    {
        if (other is null)
            return 1;

        return string.Compare(Label, other.Label, StringComparison.OrdinalIgnoreCase);
    }
}