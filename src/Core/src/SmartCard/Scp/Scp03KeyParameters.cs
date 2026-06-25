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

namespace Yubico.YubiKit.Core.SmartCard.Scp;

/// <summary>
///     SCP key parameters for SCP03 authentication.
///     SCP03 uses three keys (enc, mac, dek) with shared KVN but different KIDs.
/// </summary>
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