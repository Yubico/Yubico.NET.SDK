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

using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Unicode;
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Core.Devices;

public class FirmwareVersion : IComparable<FirmwareVersion>, IComparable, IEquatable<FirmwareVersion>
{
    public FirmwareVersion() { }

    // Existing alpha convenience constructors intentionally support both byte and validated int inputs.
#pragma warning disable RS0026
    public FirmwareVersion(byte major, byte minor = 0, byte patch = 0)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
    }

    public FirmwareVersion(int major, int minor = 0, int patch = 0)
        : this(ByteUtils.ValidateByte(major, nameof(major)), ByteUtils.ValidateByte(minor, nameof(minor)),
            ByteUtils.ValidateByte(patch, nameof(patch)))
    {
    }
#pragma warning restore RS0026

    public byte Major { get; }
    public byte Minor { get; }
    public byte Patch { get; }

    /// <summary>
    ///     Gets a value indicating whether this firmware version represents an alpha or beta YubiKey.
    ///     Alpha/beta keys report firmware versions with major version 0 but should be treated as the latest version
    ///     for feature compatibility checks.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Treat an alpha or beta version as newer than any released firmware, and as being at least 5.8.0.
    ///         Every alpha and beta key is built from a development branch at or ahead of that release, so a
    ///         feature gate must not refuse one. <see cref="IsAtLeast(int,int,int)" /> already returns
    ///         <see langword="true" /> for these versions, and
    ///         <c>Feature.IsSupportedByFirmware</c> short-circuits on this property, so the usual gates need no
    ///         special handling. Do not write <c>!IsAlphaOrBeta &amp;&amp; IsAtLeast(...)</c>: that cancels the
    ///         built-in allowance and downgrades a development key to legacy behavior.
    ///     </para>
    ///     <para>
    ///         The version carries no finer detail. Different development builds report the same placeholder
    ///         (typically 0.0.1), so it cannot distinguish one build from another. An individual applet build can
    ///         therefore lack a feature this property implies, usually because the applet was cut before the
    ///         instruction landed. The device reports that itself, as <c>SW=0x6D00</c> (instruction not supported)
    ///         or a comparable rejection.
    ///     </para>
    ///     <para>
    ///         That case is deliberately not handled in code. Detecting it would mean either abandoning the
    ///         optimistic assumption, which breaks every correctly built development key, or probing the device
    ///         before each gated call, which costs a round trip to accommodate a defective build. A test failing
    ///         against a development key with <c>SW=0x6D00</c> is the expected signal: the applet on that specific
    ///         key is missing the instruction. Check the key before suspecting the gate or the command encoding.
    ///     </para>
    /// </remarks>
    public bool IsAlphaOrBeta => Major == 0;

    /// <summary>
    ///     A default firmware version (0.0.0), representing unknown or uninitialized version.
    /// </summary>
    public static readonly FirmwareVersion Default = new(0);


    /// <summary>
    ///     Compares the relative sort order of the specified object to the current object.
    /// </summary>
    /// <remarks>
    ///     By definition any object compares greater than <see langword="null" />.
    /// </remarks>
    /// <returns>
    ///     An integer that indicates whether the current instance precedes (negative value),
    ///     follows (positive value), or occurs in the same position (0) in the sort order
    ///     as the other object.
    /// </returns>
    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;

        return obj is not FirmwareVersion version
            ? throw new ArgumentException("Argument must be a FirmwareVersion", nameof(obj))
            : CompareTo(version);
    }



    /// <summary>
    ///     Compares the relative sort order of the current instance with another object of
    ///     the same type.
    /// </summary>
    /// <remarks>
    ///     Alpha/beta keys (version 0.0.0) are treated as the latest version and will compare
    ///     greater than any other version.
    /// </remarks>
    /// <returns>
    ///     An integer that indicates whether the current instance precedes (negative value),
    ///     follows (positive value), or occurs in the same position (0) in the sort order
    ///     as the other object.
    /// </returns>
    public int CompareTo(FirmwareVersion? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;

        // Alpha/beta keys (0.x.x) are treated as the latest version
        bool thisIsAlphaOrBeta = IsAlphaOrBeta;
        bool otherIsAlphaOrBeta = other.IsAlphaOrBeta;

        if (thisIsAlphaOrBeta && otherIsAlphaOrBeta)
            return CompareVersion(other.Major, other.Minor, other.Patch);
        if (thisIsAlphaOrBeta) return 1;  // This is alpha/beta, so it's "greater"
        if (otherIsAlphaOrBeta) return -1; // Other is alpha/beta, so this is "less"

        return CompareVersion(other.Major, other.Minor, other.Patch);
    }



    public bool Equals(FirmwareVersion? other) =>
        other is not null && Major == other.Major && Minor == other.Minor && Patch == other.Patch;


    /// <summary>
    ///     Parses a firmware version string in format "major.minor.patch".
    /// </summary>
    public static FirmwareVersion? FromString(string version)
    {
        var parts = version.Split('.');
        if (parts.Length != 3)
            return null;

        if (!int.TryParse(parts[0], out var major) || major < 0 || major > 255)
            return null;
        if (!int.TryParse(parts[1], out var minor) || minor < 0 || minor > 255)
            return null;
        if (!int.TryParse(parts[2], out var patch) || patch < 0 || patch > 255)
            return null;

        return new FirmwareVersion(major, minor, patch);
    }

    /// <summary>
    ///     Extracts a firmware version from applet SELECT response text.
    /// </summary>
    /// <remarks>
    ///     SELECT response text is untrusted. Malformed or unexpected content returns <c>null</c> so callers
    ///     can fall back to another version source.
    /// </remarks>
    internal static FirmwareVersion? FromSelectResponse(ReadOnlySpan<byte> selectResponse)
    {
        if (!Utf8.IsValid(selectResponse))
            return null;

        string[] tokens = Encoding.UTF8.GetString(selectResponse)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        for (var i = tokens.Length - 1; i >= 0; i--)
        {
            FirmwareVersion? version = FromString(tokens[i]);
            if (version is not null)
                return version;
        }

        return null;
    }

    public bool IsAtLeast(FirmwareVersion firmwareVersion)
    {
        return CompareTo(firmwareVersion) >= 0;
    }

    public bool IsLessThan(FirmwareVersion firmwareVersion) => CompareTo(firmwareVersion) < 0;

    public bool IsAtLeast(int major, int minor, int patch) =>
        IsAlphaOrBeta || CompareVersion(major, minor, patch) >= 0;

    public bool IsLessThan(int major, int minor, int patch) =>
        !IsAlphaOrBeta && CompareVersion(major, minor, patch) < 0;

    private int CompareVersion(int major, int minor, int patch) =>
        ((Major << 16) | (Minor << 8) | Patch).CompareTo((major << 16) | (minor << 8) | patch);

    /// <summary>
    ///     Creates a <see cref="FirmwareVersion" /> from a byte array.
    ///     The byte array must contain exactly three bytes, representing the major, minor, and patch versions.
    /// </summary>
    /// <param name="bytes">A byte array containing the version information.</param>
    /// <returns>A <see cref="FirmwareVersion" /> instance.</returns>
    /// <exception cref="ArgumentException">Thrown if the byte array does not contain exactly three bytes.</exception>
    /// <remarks>
    ///     The first byte represents the major version, the second byte represents the minor version,
    ///     and the third byte represents the patch version.
    /// </remarks>
    public static FirmwareVersion FromBytes(ReadOnlySpan<byte> bytes) => bytes.Length != 3
        ? throw new ArgumentException("Invalid length of data")
        : new FirmwareVersion(bytes[0], bytes[1], bytes[2]);

    public static bool operator >(FirmwareVersion? left, FirmwareVersion? right)
    {
        if (left is null) return false; // null > anything is false
        if (right is null) return true; // non-null > null is true

        return left.CompareTo(right) > 0;
    }

    public static bool operator <(FirmwareVersion? left, FirmwareVersion? right)
    {
        if (left is null) return right is not null; // null < non-null is true, null < null is false
        if (right is null) return false; // non-null < null is false

        return left.CompareTo(right) < 0;
    }

    public static bool operator >=(FirmwareVersion? left, FirmwareVersion? right)
    {
        if (left is null) return right is null; // null >= null is true, null >= non-null is false
        if (right is null) return true; // non-null >= null is true

        return left.CompareTo(right) >= 0;
    }

    public static bool operator <=(FirmwareVersion? left, FirmwareVersion? right)
    {
        if (left is null) return true; // null <= anything is true
        if (right is null) return false; // non-null <= null is false

        return left.CompareTo(right) <= 0;
    }

    public static bool operator ==(FirmwareVersion? left, FirmwareVersion? right)
    {
        if (left is null && right is null) return true;

        if (left is null || right is null) return false;

        return left.Equals(right);
    }

    public static bool operator !=(FirmwareVersion? left, FirmwareVersion? right) =>
        !(left == right);

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is FirmwareVersion version && Equals(version);

    public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch);

    public override string ToString() => $"{Major}.{Minor}.{Patch}";


    internal static readonly FirmwareVersion V4_0_0 = new(4);
    internal static readonly FirmwareVersion V4_3_0 = new(4, 3);
    internal static readonly FirmwareVersion V5_0_0 = new(5);
    public static readonly FirmwareVersion V5_3_0 = new(5, 3);
    public static readonly FirmwareVersion V5_4_3 = new(5, 4, 3);
    internal static readonly FirmwareVersion V5_7_0 = new(5, 7);
    public static readonly FirmwareVersion V5_7_2 = new(5, 7, 2);
    public static readonly FirmwareVersion V5_8_0 = new(5, 8);

}