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

using System;
using System.Diagnostics.CodeAnalysis;

namespace Yubico.YubiKey
{
    public class FirmwareVersion : IComparable<FirmwareVersion>, IComparable, IEquatable<FirmwareVersion>
    {
        #region Frequently Used Versions
        // Note that these are for internal use. It's expected that SDK users should call `.HasFeature` on the
        // YubiKey device to check for features instead of FW versions.
        internal static readonly FirmwareVersion All = new FirmwareVersion(1, 0, 0);
        internal static readonly FirmwareVersion V2_0_0 = new FirmwareVersion(2, 0, 0);
        internal static readonly FirmwareVersion V2_1_0 = new FirmwareVersion(2, 1, 0);
        internal static readonly FirmwareVersion V2_2_0 = new FirmwareVersion(2, 2, 0);
        internal static readonly FirmwareVersion V2_3_0 = new FirmwareVersion(2, 3, 0);
        internal static readonly FirmwareVersion V2_3_2 = new FirmwareVersion(2, 3, 2);
        internal static readonly FirmwareVersion V2_4_0 = new FirmwareVersion(2, 4, 0);
        internal static readonly FirmwareVersion V3_1_0 = new FirmwareVersion(3, 1, 0);
        internal static readonly FirmwareVersion V4_0_0 = new FirmwareVersion(4, 0, 0);
        internal static readonly FirmwareVersion V4_2_4 = new FirmwareVersion(4, 2, 4);
        internal static readonly FirmwareVersion V4_3_0 = new FirmwareVersion(4, 3, 0);
        internal static readonly FirmwareVersion V4_3_1 = new FirmwareVersion(4, 3, 1);
        internal static readonly FirmwareVersion V4_3_4 = new FirmwareVersion(4, 3, 4);
        internal static readonly FirmwareVersion V4_4_0 = new FirmwareVersion(4, 4, 0);
        internal static readonly FirmwareVersion V4_5_0 = new FirmwareVersion(4, 5, 0);
        internal static readonly FirmwareVersion V5_0_0 = new FirmwareVersion(5, 0, 0);
        internal static readonly FirmwareVersion V5_3_0 = new FirmwareVersion(5, 3, 0);
        internal static readonly FirmwareVersion V5_3_1 = new FirmwareVersion(5, 3, 1);
        internal static readonly FirmwareVersion V5_4_2 = new FirmwareVersion(5, 4, 2);
        internal static readonly FirmwareVersion V5_4_3 = new FirmwareVersion(5, 4, 3);
        internal static readonly FirmwareVersion V5_6_0 = new FirmwareVersion(5, 6, 0);
        internal static readonly FirmwareVersion V5_6_3 = new FirmwareVersion(5, 6, 3);
        internal static readonly FirmwareVersion V5_7_0 = new FirmwareVersion(5, 7, 0);
        internal static readonly FirmwareVersion V5_7_2 = new FirmwareVersion(5, 7, 2);
        internal static readonly FirmwareVersion V5_8_0 = new FirmwareVersion(5, 8, 0);

        #endregion

        public byte Major { get; set; }
        public byte Minor { get; set; }
        public byte Patch { get; set; }

        public FirmwareVersion() { }

        public FirmwareVersion(byte major, byte minor = 0, byte patch = 0)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        /// <summary>
        /// Parse a string of the form "major.minor.patch"
        /// </summary>
        /// <param name="versionString"></param>
        /// <returns>Returns a FirmwareVersion instance</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static FirmwareVersion Parse(string versionString)
        {
            if (versionString is null)
            {
                throw new ArgumentNullException(nameof(versionString));
            }

            string[] parts = versionString.Split('.');
            if (parts.Length != 3)
            {
                throw new ArgumentException("Must include major.minor.patch", nameof(versionString));
            }

            if (!byte.TryParse(parts[0], out byte major) ||
                !byte.TryParse(parts[1], out byte minor) ||
                !byte.TryParse(parts[2], out byte patch))
            {
                throw new ArgumentException("Major, minor and patch must be valid numbers", nameof(versionString));
            }

            return new FirmwareVersion(major, minor, patch);
        }

        /// <summary>
        /// Creates a <see cref="FirmwareVersion"/> from a byte array.
        /// The byte array must contain exactly three bytes, representing the major, minor, and patch versions.
        /// </summary>
        /// <param name="bytes">A byte array containing the version information.</param>
        /// <returns>A <see cref="FirmwareVersion"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown if the byte array does not contain exactly three bytes.</exception>
        /// <remarks>
        /// The first byte represents the major version, the second byte represents the minor version,
        /// and the third byte represents the patch version.
        /// </remarks>
        public static FirmwareVersion FromBytes(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length != 3)
            {
                throw new ArgumentException("Invalid length of data");
            }

            return new FirmwareVersion(bytes[0], bytes[1], bytes[2]);
        }

        public static bool operator >(FirmwareVersion left, FirmwareVersion right)
        {
            if (left is null)
            {
                return false;
            }

            return left.CompareTo(right) > 0;
        }

        public static bool operator <(FirmwareVersion left, FirmwareVersion right)
        {
            if (right is null)
            {
                return left is not null;
            }

            return left is not null && left.CompareTo(right) < 0;
        }

        public static bool operator >=(FirmwareVersion left, FirmwareVersion right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.CompareTo(right) >= 0;
        }

        public static bool operator <=(FirmwareVersion left, FirmwareVersion right)
        {
            if (right is null)
            {
                return left is not null;
            }

            return left is not null && left.CompareTo(right) <= 0;
        }

        public static bool operator ==(FirmwareVersion left, FirmwareVersion right)
        {
            if (left is null && right is null)
            {
                return true;
            }

            if (left is null || right is null)
            {
                return false;
            }

            return left.CompareTo(right) == 0;
        }

        public static bool operator !=(FirmwareVersion left, FirmwareVersion right) =>
            !(left == right);

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is not FirmwareVersion)
            {
                return false;
            }

            return Equals((FirmwareVersion)obj);
        }

        public bool Equals(FirmwareVersion other) => CompareTo(other) == 0;

        public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch);

        public override string ToString() => $"{Major}.{Minor}.{Patch}";

        /// <summary>
        /// Compares the relative sort order of the specified object to the current object.
        /// </summary>
        /// <remarks>
        /// By definition any object compares greater than <see langword="null"/>.
        /// </remarks>
        /// <returns>
        /// An integer that indicates whether the current instance precedes (negative value),
        /// follows (positive value), or occurs in the same position (0) in the sort order
        /// as the other object.
        /// </returns>
        public int CompareTo(object? obj)
        {
            if (obj is null)
            {
                return 1;
            }

            if (obj is not FirmwareVersion)
            {
                throw new ArgumentException(ExceptionMessages.ArgumentMustBeFirmwareVersion, nameof(obj));
            }

            return CompareTo((FirmwareVersion)obj);
        }

        /// <summary>
        /// Compares the relative sort order of the current instance with another object of
        /// the same type.
        /// </summary>
        /// <returns>
        /// An integer that indicates whether the current instance precedes (negative value),
        /// follows (positive value), or occurs in the same position (0) in the sort order
        /// as the other object.
        /// </returns>
        public int CompareTo(FirmwareVersion other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }
            if (other is null)
            {
                return 1;
            }

            // The version comparison depends on the comparison of
            // the underlying values.
            int majorComparison = Major.CompareTo(other.Major);
            if (majorComparison == 0)
            {
                int minorComparison = Minor.CompareTo(other.Minor);
                if (minorComparison == 0)
                {
                    int patchComparison = Patch.CompareTo(other.Patch);
                    return patchComparison;
                }

                return minorComparison;
            }

            return majorComparison;
        }
    }
}
