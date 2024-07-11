// Copyright 2021 Yubico AB
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

using System;

namespace Yubico.YubiKey.Piv
{
    /// <summary>
    /// This enum lists the possible PIN-only modes for the PIV application.
    /// </summary>
    /// <remarks>
    /// PIN-only mode means that the application does not need to enter the
    /// management key in order to perform PIV operations that normally
    /// require it, only the PIN is needed.
    /// <para>
    /// See the User's Manual entry on
    /// <xref href="UsersManualPinOnlyMode"> PIN-only mode</xref> for a
    /// deeper discussion of this feature.
    /// </para>
    /// <para>
    /// This enum is a bit field. If it is 0 (None, no bits set), that means
    /// there is no PIN-only mode set on the YubiKey, but it is available to be
    /// set.
    /// </para>
    /// <para>
    /// If the 1 bit is set (PinProtectedUnavailable), then the YubiKey cannot be
    /// set to PIN-Protected.
    /// </para>
    /// <para>
    /// If the 2 bit is set (PinDerivedUnavailable), then the YubiKey cannot be
    /// set to PIN-derived.
    /// </para>
    /// <para>
    /// If the 4 bit is set (PinProtected), then the YubiKey is currently set to
    /// PIN-protected.
    /// </para>
    /// <para>
    /// If the 8 bit is set (PinDerived), then the YubiKey is currently set to
    /// PIN-derived.
    /// </para>
    /// <para>
    /// Certain combinations of bits are possible:
    /// <list type="bullet">
    /// <item><description>PinProtectedUnavailable | PinProtectedUnavailable
    /// </description></item>
    /// <item><description>PinDerivedUnavailable | PinProtected
    /// </description></item>
    /// <item><description>PinProtectedUnavailable | PinDerived
    /// </description></item>
    /// <item><description>PinProtected | PinDerived
    /// </description></item>
    /// </list>
    /// </para>
    /// <para>
    /// To determine if a field is set, you can use the <c>Enum.HasFlag</c>
    /// method.
    /// </para>
    /// </remarks>
    [Flags]
    public enum PivPinOnlyMode
    {
        /// <summary>
        /// The YubiKey is not set for PIN-only, but is available for this
        /// feature.
        /// </summary>
        None = 0,

        /// <summary>
        /// The YubiKey is not available for PIN-protected. This happens if some
        /// other application is using the ADMIN DATA and/or the PRINTED storage
        /// area.
        /// </summary>
        PinProtectedUnavailable = 1,

        /// <summary>
        /// The YubiKey is not available for PIN-derived. This happens if some
        /// other application is using the ADMIN DATA.
        /// </summary>
        PinDerivedUnavailable = 2,

        /// <summary>
        /// The YubiKey is set for PIN-protected.
        /// </summary>
        PinProtected = 4,

        /// <summary>
        /// The YubiKey is set for PIN-derived.
        /// &gt; [!WARNING]
        /// &gt; You should not set a YubiKey for PIN-derived, this feature is
        /// &gt; provided only for backwards compatibility.
        /// </summary>
        PinDerived = 8,
    }
}
