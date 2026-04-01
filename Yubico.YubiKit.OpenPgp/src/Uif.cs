// Copyright 2025 Yubico AB
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

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     User Interaction Flag (UIF) values controlling touch policy for OpenPGP key operations.
/// </summary>
public enum Uif : byte
{
    /// <summary>
    ///     Touch is not required.
    /// </summary>
    Off = 0x00,

    /// <summary>
    ///     Touch is required for each operation.
    /// </summary>
    On = 0x01,

    /// <summary>
    ///     Touch is required and cannot be changed.
    /// </summary>
    Fixed = 0x02,

    /// <summary>
    ///     Touch is required but cached for a short period.
    /// </summary>
    Cached = 0x03,

    /// <summary>
    ///     Touch is required with caching and cannot be changed.
    /// </summary>
    CachedFixed = 0x04,
}

/// <summary>
///     Extension methods for <see cref="Uif" />.
/// </summary>
public static class UifExtensions
{
    /// <summary>
    ///     Gets whether this UIF value is fixed (cannot be changed without a factory reset).
    /// </summary>
    public static bool IsFixed(this Uif uif) =>
        uif is Uif.Fixed or Uif.CachedFixed;

    /// <summary>
    ///     Gets whether this UIF value uses caching (touch result is cached briefly).
    /// </summary>
    public static bool IsCached(this Uif uif) =>
        uif is Uif.Cached or Uif.CachedFixed;

    /// <summary>
    ///     Encodes the UIF value as a two-byte array: the UIF value followed by the BUTTON feature flag.
    /// </summary>
    public static byte[] ToBytes(this Uif uif) =>
        [(byte)uif, (byte)GeneralFeatureManagement.Button];

    /// <summary>
    ///     Parses a UIF value from the first byte of the encoded data.
    /// </summary>
    public static Uif ParseUif(ReadOnlySpan<byte> encoded) =>
        (Uif)encoded[0];
}
