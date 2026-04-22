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

using System.Runtime.InteropServices;

namespace Yubico.YubiKit.WebAuthn.Cose;

/// <summary>
/// Authenticator Attestation Global Unique ID (128 bits).
/// </summary>
/// <remarks>
/// WebAuthn AAGUID is a 16-byte big-endian UUID. See
/// <see href="https://www.w3.org/TR/webauthn-3/#aaguid">WebAuthn AAGUID</see>.
/// </remarks>
public readonly struct Aaguid : IEquatable<Aaguid>
{
    // NOTE: AAGUID is a public identifier per WebAuthn spec, not sensitive —
    // byte[] storage in struct is intentional and safe per CLAUDE.md exception.
    private readonly byte[] _bytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="Aaguid"/> struct from raw bytes.
    /// </summary>
    /// <param name="bytes">The 16-byte AAGUID value (big-endian).</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="bytes"/> is not exactly 16 bytes.</exception>
    public Aaguid(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 16)
        {
            throw new ArgumentException("AAGUID must be exactly 16 bytes.", nameof(bytes));
        }
        _bytes = bytes.ToArray();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Aaguid"/> struct from a <see cref="Guid"/>.
    /// </summary>
    /// <param name="guid">The GUID value.</param>
    /// <remarks>
    /// Converts from .NET's mixed-endian GUID representation to WebAuthn's big-endian AAGUID.
    /// </remarks>
    public Aaguid(Guid guid)
    {
        // .NET Guid.ToByteArray() produces mixed endianness:
        // - First 4 bytes (time_low): little-endian
        // - Next 2 bytes (time_mid): little-endian
        // - Next 2 bytes (time_hi_and_version): little-endian
        // - Last 8 bytes (clock_seq_and_node): big-endian
        // WebAuthn AAGUID is fully big-endian, so we need to reverse the first three components.
        Span<byte> bytes = stackalloc byte[16];
        guid.TryWriteBytes(bytes);

        // Reverse first 3 components to convert to big-endian
        bytes[0..4].Reverse();  // time_low
        bytes[4..6].Reverse();  // time_mid
        bytes[6..8].Reverse();  // time_hi_and_version
        // bytes[8..16] already big-endian

        _bytes = bytes.ToArray();
    }

    /// <summary>
    /// Gets the AAGUID as a span of bytes (big-endian, 16 bytes).
    /// </summary>
    /// <returns>A read-only span containing the 16-byte AAGUID.</returns>
    public ReadOnlySpan<byte> AsSpan() => _bytes ?? [];

    /// <summary>
    /// Gets the AAGUID as a <see cref="Guid"/>.
    /// </summary>
    /// <remarks>
    /// Converts from WebAuthn's big-endian AAGUID to .NET's mixed-endian GUID representation.
    /// </remarks>
    public Guid Value
    {
        get
        {
            if (_bytes is null or { Length: 0 })
            {
                return Guid.Empty;
            }

            // Convert from big-endian AAGUID to mixed-endian Guid
            Span<byte> temp = stackalloc byte[16];
            _bytes.AsSpan().CopyTo(temp);

            // Reverse first 3 components to convert from big-endian to .NET's mixed-endian
            temp[0..4].Reverse();  // time_low
            temp[4..6].Reverse();  // time_mid
            temp[6..8].Reverse();  // time_hi_and_version
            // temp[8..16] stays big-endian

            return new Guid(temp);
        }
    }

    /// <summary>
    /// Determines whether two <see cref="Aaguid"/> instances are equal.
    /// </summary>
    public bool Equals(Aaguid other) =>
        _bytes is not null && other._bytes is not null && _bytes.AsSpan().SequenceEqual(other._bytes);

    /// <summary>
    /// Determines whether this instance and a specified object are equal.
    /// </summary>
    public override bool Equals(object? obj) => obj is Aaguid other && Equals(other);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    public override int GetHashCode()
    {
        if (_bytes is null or { Length: 0 })
        {
            return 0;
        }
        // Use first 4 bytes as hash seed
        return MemoryMarshal.Read<int>(_bytes);
    }

    /// <summary>
    /// Returns the AAGUID as a hyphenated hex string (8-4-4-4-12 format).
    /// </summary>
    public override string ToString() => Value.ToString();

    public static bool operator ==(Aaguid left, Aaguid right) => left.Equals(right);
    public static bool operator !=(Aaguid left, Aaguid right) => !left.Equals(right);
}
