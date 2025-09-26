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

namespace Yubico.YubiKit.Core;

public class Version : IComparable<Version>, IComparable, IEquatable<Version>
{
    public Version() { }

    public Version(byte major, byte minor = 0, byte patch = 0)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
    }

    public byte Major { get; }
    public byte Minor { get; }
    public byte Patch { get; }

    public static Version Default => new(0);

    #region IComparable Members

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

        return obj is not Version version
            ? throw new ArgumentException("ExceptionMessages.ArgumentMustBeFirmwareVersion, nameof(obj)")
            : CompareTo(version);
    }

    #endregion

    #region IComparable<Version> Members

    /// <summary>
    ///     Compares the relative sort order of the current instance with another object of
    ///     the same type.
    /// </summary>
    /// <returns>
    ///     An integer that indicates whether the current instance precedes (negative value),
    ///     follows (positive value), or occurs in the same position (0) in the sort order
    ///     as the other object.
    /// </returns>
    public int CompareTo(Version? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;

        var majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0) return majorComparison;

        var minorComparison = Minor.CompareTo(other.Minor);
        if (minorComparison != 0) return minorComparison;

        var patchComparison = Patch.CompareTo(other.Patch);
        return patchComparison;
    }

    #endregion

    #region IEquatable<Version> Members

    public bool Equals(Version? other) => CompareTo(other) == 0;

    #endregion

    /// <summary>
    ///     Parse a string of the form "major.minor.patch"
    /// </summary>
    /// <param name="versionString"></param>
    /// <returns>Returns a FirmwareVersion instance</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static Version Parse(string versionString)
    {
        ArgumentNullException.ThrowIfNull(versionString);

        var parts = versionString.Split('.');
        if (parts.Length != 3) throw new ArgumentException("Must include major.minor.patch", nameof(versionString));

        if (!byte.TryParse(parts[0], out var major) ||
            !byte.TryParse(parts[1], out var minor) ||
            !byte.TryParse(parts[2], out var patch))
            throw new ArgumentException("Major, minor and patch must be valid numbers", nameof(versionString));

        return new Version(major, minor, patch);
    }

    /// <summary>
    ///     Creates a <see cref="Version" /> from a byte array.
    ///     The byte array must contain exactly three bytes, representing the major, minor, and patch versions.
    /// </summary>
    /// <param name="bytes">A byte array containing the version information.</param>
    /// <returns>A <see cref="Version" /> instance.</returns>
    /// <exception cref="ArgumentException">Thrown if the byte array does not contain exactly three bytes.</exception>
    /// <remarks>
    ///     The first byte represents the major version, the second byte represents the minor version,
    ///     and the third byte represents the patch version.
    /// </remarks>
    public static Version FromBytes(ReadOnlySpan<byte> bytes) => bytes.Length != 3
        ? throw new ArgumentException("Invalid length of data")
        : new Version(bytes[0], bytes[1], bytes[2]);

    public static bool operator >(Version? left, Version right)
    {
        if (left is null) return false;

        return left.CompareTo(right) > 0;
    }

    public static bool operator <(Version? left, Version? right)
    {
        if (right is null) return left is not null;

        return left is not null && left.CompareTo(right) < 0;
    }

    public static bool operator >=(Version? left, Version? right)
    {
        if (left is null) return right is null;

        return left.CompareTo(right) >= 0;
    }

    public static bool operator <=(Version? left, Version? right)
    {
        if (right is null) return left is not null;

        return left is not null && left.CompareTo(right) <= 0;
    }

    public static bool operator ==(Version? left, Version? right)
    {
        if (left is null && right is null) return true;

        if (left is null || right is null) return false;

        return left.CompareTo(right) == 0;
    }

    public static bool operator !=(Version left, Version right) =>
        !(left == right);

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Version version && Equals(version);

    public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch);

    public override string ToString() => $"{Major}.{Minor}.{Patch}";

    #region Frequently Used Versions

    internal static readonly Version All = new(1);
    internal static readonly Version V5_0_0 = new(5);
    internal static readonly Version V5_3_0 = new(5, 3);
    internal static readonly Version V5_3_1 = new(5, 3, 1);
    internal static readonly Version V5_4_2 = new(5, 4, 2);
    internal static readonly Version V5_4_3 = new(5, 4, 3);
    internal static readonly Version V5_6_0 = new(5, 6);
    internal static readonly Version V5_6_3 = new(5, 6, 3);
    internal static readonly Version V5_7_0 = new(5, 7);
    internal static readonly Version V5_7_2 = new(5, 7, 2);
    internal static readonly Version V5_8_0 = new(5, 8);

    #endregion
}