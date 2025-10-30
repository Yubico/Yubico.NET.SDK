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

using System;

namespace Yubico.YubiKit.Core.SmartCard.Scp;

/// <summary>
/// SCP key parameters for SCP03 authentication.
/// SCP03 uses three keys (enc, mac, dek) with shared KVN but different KIDs.
/// </summary>
internal sealed record Scp03KeyParams : ScpKeyParams
{
    /// <summary>
    /// Gets the key reference for this SCP03 key set.
    /// </summary>
    public KeyRef KeyRef { get; init; }

    /// <summary>
    /// Gets the static keys used for derivation.
    /// </summary>
    internal StaticKeys Keys { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Scp03KeyParams"/> record.
    /// </summary>
    /// <param name="keyRef">The key reference (KID must be 0-3 for SCP03).</param>
    /// <param name="keys">The static key material.</param>
    /// <exception cref="ArgumentException">Thrown if KID is not in the range 0-3.</exception>
    /// <exception cref="ArgumentNullException">Thrown if keys is null.</exception>
    public Scp03KeyParams(KeyRef keyRef, StaticKeys keys)
    {
        if (keyRef.Kid > 3)
        {
            throw new ArgumentException($"SCP03 KID must be 0-3, got {keyRef.Kid}", nameof(keyRef));
        }

        KeyRef = keyRef;
        Keys = keys ?? throw new ArgumentNullException(nameof(keys));
    }
}
