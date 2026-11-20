// Copyright (C) 2024 Yubico.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Yubico.YubiKit.Core.Protocols.SmartCard.Scp;

/// <summary>
///     SCP key parameters for SCP03 authentication.
///     SCP03 uses three keys (enc, mac, dek) with shared KVN but different KIDs.
/// </summary>
/// <remarks>
///     This owner retains and disposes the exact StaticKeys instance supplied by the caller. A record with-clone
///     shares that owner rather than cloning its key arrays. Keep parameters alive until all using sessions have
///     been disposed; Security Domain also borrows them for reset. Supply per-card keys: initialization does not
///     diversify a base master key from INITIALIZE UPDATE's diversification bytes.
/// </remarks>
public sealed record Scp03KeyParameters : ScpKeyParameters
{
    private bool _disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Scp03KeyParameters" /> record.
    /// </summary>
    /// <param name="keyReference">The key reference (KID must be 0-3 for SCP03).</param>
    /// <param name="keys">The static key material.</param>
    /// <exception cref="ArgumentException">Thrown if KID is not in the range 0-3.</exception>
    /// <exception cref="ArgumentNullException">Thrown if keys is null.</exception>
    public Scp03KeyParameters(KeyReference keyReference, StaticKeys keys)
    {
        if (keyReference.Kid > 3) throw new ArgumentException($"SCP03 KID must be 0-3, got {keyReference.Kid}", nameof(keyReference));

        KeyReference = keyReference;
        Keys = keys ?? throw new ArgumentNullException(nameof(keys));
    }

    /// <summary>
    ///     Gets the static keys used for derivation.
    /// </summary>
    public StaticKeys Keys { get; }

    /// <summary>Creates a new owner containing the public factory-default SCP03 keys.</summary>
    /// <remarks>Use only with an authorized key still configured with those defaults; they provide no secret provisioning.</remarks>
    public static Scp03KeyParameters Default => new(KeyReference.Default, StaticKeys.GetDefaultKeys());
    public override void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            Keys.Dispose();
        }
        finally
        {
            _disposed = true;
        }
    }
}