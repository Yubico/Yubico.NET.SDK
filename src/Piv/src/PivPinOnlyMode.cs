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

namespace Yubico.YubiKit.Piv;

/// <summary>
/// PIN-only management-key mode for the PIV application.
/// </summary>
/// <remarks>
/// PIN-only mode means the caller does not need to supply the management key for privileged PIV
/// operations; only the PIN is needed, because the management key is either stored PIN-protected
/// (see <see cref="PinProtected"/>) or derived from the PIN (see <see cref="PinDerived"/>).
/// This is a bit field; see <see cref="IPivSession.GetPinOnlyModeAsync"/>.
/// </remarks>
[Flags]
public enum PivPinOnlyMode
{
    /// <summary>No PIN-only mode is set, and both modes are available to be set.</summary>
    None = 0,

    /// <summary>
    /// PIN-protected mode is unavailable, generally because another application is using the
    /// PRINTED storage location for something other than a PIN-protected management key.
    /// </summary>
    PinProtectedUnavailable = 1,

    /// <summary>
    /// PIN-derived mode is unavailable, generally because another application is using the ADMIN
    /// DATA storage location for something other than PIN-only mode state.
    /// </summary>
    PinDerivedUnavailable = 2,

    /// <summary>The management key is currently PIN-protected (stored in the PRINTED object).</summary>
    PinProtected = 4,

    /// <summary>
    /// The management key is currently PIN-derived (derived from the PIN and a salt stored in
    /// ADMIN DATA).
    /// </summary>
    /// <remarks>
    /// PIN-derived management keys are a deprecated, weaker mechanism kept only for backwards
    /// compatibility; new callers should prefer <see cref="PinProtected"/>.
    /// </remarks>
    PinDerived = 8,
}