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
///     Describes a credential at the time of a YubiHSM Auth LIST response.
/// </summary>
/// <remarks>
///     LIST preserves unknown algorithm identifiers and represents unknown touch values as <c>null</c>.
///     The constructor and init properties accept supplied metadata without enrollment validation.
///     Record equality compares all four properties, including the case-sensitive label.
///     Sorting compares labels using ordinal, case-insensitive ordering, so two unequal records can
///     have a comparison result of zero.
/// </remarks>
/// <param name="Label">The credential label (1-64 UTF-8 bytes).</param>
/// <param name="Algorithm">The algorithm used by this credential.</param>
/// <param name="RetriesRemaining">
///     A snapshot of the attempts remaining before the applet permanently deletes this credential.
///     A fresh credential starts at eight. Yubico's YubiHSM Auth overview describes failed password
///     authentication as decrementing the counter and successful authentication before deletion as
///     restoring it to eight. Refresh the list to observe later changes.
/// </param>
/// <param name="TouchRequired">Whether touch is required to use this credential, or <c>null</c> if unknown.</param>
public sealed record HsmAuthCredential(
    string Label,
    HsmAuthAlgorithm Algorithm,
    int RetriesRemaining,
    bool? TouchRequired) : IComparable<HsmAuthCredential>
{
    /// <summary>
    ///     Compares labels using ordinal, case-insensitive ordering. A non-null credential sorts after null.
    /// </summary>
    public int CompareTo(HsmAuthCredential? other)
    {
        if (other is null)
            return 1;

        return string.Compare(Label, other.Label, StringComparison.OrdinalIgnoreCase);
    }
}