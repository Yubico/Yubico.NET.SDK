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

using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.Core.YubiKey;

/// <summary>
///     Connection-selection helpers for <see cref="IYubiKey" />.
/// </summary>
public static class YubiKeyConnectionExtensions
{
    /// <summary>
    ///     Returns the first connection in <paramref name="preferenceOrder" /> that this device supports, or
    ///     <see cref="ConnectionType.Unknown" /> when it supports none of them.
    /// </summary>
    /// <remarks>
    ///     This is a policy-free mechanism: the caller supplies the preference order. Application/module
    ///     extension methods use it to pick a concrete transport on a physical (possibly multi-connection)
    ///     device instead of the ambiguity-throwing parameterless <see cref="IYubiKey.ConnectAsync(System.Threading.CancellationToken)" />.
    /// </remarks>
    public static ConnectionType ResolvePreferredConnection(
        this IYubiKey yubiKey,
        params ConnectionType[] preferenceOrder)
    {
        ArgumentNullException.ThrowIfNull(yubiKey);
        ArgumentNullException.ThrowIfNull(preferenceOrder);

        foreach (var candidate in preferenceOrder)
        {
            // Only concrete, openable connections are valid results; ignore Hid/All/Unknown candidates so
            // the resolver never returns a non-openable group flag.
            if (candidate is ConnectionType.SmartCard or ConnectionType.HidFido or ConnectionType.HidOtp
                && yubiKey.SupportsConnection(candidate))
                return candidate;
        }

        return ConnectionType.Unknown;
    }
}