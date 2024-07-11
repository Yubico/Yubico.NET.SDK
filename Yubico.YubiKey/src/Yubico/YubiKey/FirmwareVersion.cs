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
using System.Diagnostics.CodeAnalysis;

namespace Yubico.YubiKey
{
    public class FirmwareVersion : IComparable<FirmwareVersion>, IComparable, IEquatable<FirmwareVersion>
    {
        public FirmwareVersion() { }

        public FirmwareVersion(byte major, byte minor = 0, byte patch = 0)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public byte Major { get; set; }
        public byte Minor { get; set; }
        public byte Patch { get; set; }

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
            if (obj is null)
            {
                return 1;
            }

            if (!(obj is FirmwareVersion))
            {
                throw new ArgumentException(ExceptionMessages.ArgumentMustBeFirmwareVersion, nameof(obj));
            }

            return CompareTo((FirmwareVersion)obj);
        }

        /// <summary>
        ///     Compares the relative sort order of the current instance with another object of
        ///     the same type.
        /// </summary>
        /// <returns>
        ///     An integer that indicates whether the current instance precedes (negative value),
        ///     follows (positive value), or occurs in the same position (0) in the sort order
        ///     as the other object.
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

        public bool Equals(FirmwareVersion other) => CompareTo(other) == 0;

        public static bool operator >(FirmwareVersion left, FirmwareVersion right)
        {
            // CA1065, these operators shouldn't throw exceptions.
            if (left is null)
            {
                return false;
            }

            return left.CompareTo(right) > 0;
        }

        public static bool operator <(FirmwareVersion left, FirmwareVersion right)
        {
            // CA1065, these operators shouldn't throw exceptions.
            if (right is null)
            {
                return !(left is null);
            }

            return left is null
                ? false
                : left.CompareTo(right) < 0;
        }

        public static bool operator >=(FirmwareVersion left, FirmwareVersion right)
        {
            // CA1065, these operators shouldn't throw exceptions.
            if (left is null)
            {
                return right is null;
            }

            return left.CompareTo(right) >= 0;
        }

        public static bool operator <=(FirmwareVersion left, FirmwareVersion right)
        {
            // CA1065, these operators shouldn't throw exceptions.
            if (right is null)
            {
                return !(left is null);
            }

            return left is null
                ? false
                : left.CompareTo(right) <= 0;
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

        public static bool operator !=(FirmwareVersion left, FirmwareVersion right) => !(left == right);

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (!(obj is FirmwareVersion))
            {
                return false;
            }

            return Equals((FirmwareVersion)obj);
        }

        public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch);

        public override string ToString() => $"{Major}.{Minor}.{Patch}";

        #region Frequently Used Versions

        // Note that these are for internal use. It's expected that SDK users should call `.HasFeature` on the
        // YubiKey device to check for features instead of FW versions.
        internal static readonly FirmwareVersion All = new FirmwareVersion(major: 1, minor: 0, patch: 0);
        internal static readonly FirmwareVersion V2_0_0 = new FirmwareVersion(major: 2, minor: 0, patch: 0);
        internal static readonly FirmwareVersion V2_1_0 = new FirmwareVersion(major: 2, minor: 1, patch: 0);
        internal static readonly FirmwareVersion V2_2_0 = new FirmwareVersion(major: 2, minor: 2, patch: 0);
        internal static readonly FirmwareVersion V2_3_0 = new FirmwareVersion(major: 2, minor: 3, patch: 0);
        internal static readonly FirmwareVersion V2_3_2 = new FirmwareVersion(major: 2, minor: 3, patch: 2);
        internal static readonly FirmwareVersion V2_4_0 = new FirmwareVersion(major: 2, minor: 4, patch: 0);
        internal static readonly FirmwareVersion V3_1_0 = new FirmwareVersion(major: 3, minor: 1, patch: 0);
        internal static readonly FirmwareVersion V4_0_0 = new FirmwareVersion(major: 4, minor: 0, patch: 0);
        internal static readonly FirmwareVersion V4_2_4 = new FirmwareVersion(major: 4, minor: 2, patch: 4);
        internal static readonly FirmwareVersion V4_3_0 = new FirmwareVersion(major: 4, minor: 3, patch: 0);
        internal static readonly FirmwareVersion V4_3_1 = new FirmwareVersion(major: 4, minor: 3, patch: 1);
        internal static readonly FirmwareVersion V4_3_4 = new FirmwareVersion(major: 4, minor: 3, patch: 4);
        internal static readonly FirmwareVersion V4_4_0 = new FirmwareVersion(major: 4, minor: 4, patch: 0);
        internal static readonly FirmwareVersion V4_5_0 = new FirmwareVersion(major: 4, minor: 5, patch: 0);
        internal static readonly FirmwareVersion V5_0_0 = new FirmwareVersion(major: 5, minor: 0, patch: 0);
        internal static readonly FirmwareVersion V5_3_0 = new FirmwareVersion(major: 5, minor: 3, patch: 0);
        internal static readonly FirmwareVersion V5_3_1 = new FirmwareVersion(major: 5, minor: 3, patch: 1);
        internal static readonly FirmwareVersion V5_4_2 = new FirmwareVersion(major: 5, minor: 4, patch: 2);
        internal static readonly FirmwareVersion V5_4_3 = new FirmwareVersion(major: 5, minor: 4, patch: 3);
        internal static readonly FirmwareVersion V5_6_0 = new FirmwareVersion(major: 5, minor: 6, patch: 0);
        internal static readonly FirmwareVersion V5_7_0 = new FirmwareVersion(major: 5, minor: 7, patch: 0);

        #endregion
    }
}
