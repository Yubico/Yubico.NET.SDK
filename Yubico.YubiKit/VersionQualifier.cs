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

using Version = Yubico.YubiKit.Core.Version;

namespace Yubico.YubiKit;

/// <summary>
///     Represents the type of version qualifier for a firmware version.
///     The version qualifier type indicates whether the version is an Alpha, Beta, or Final release.
/// </summary>
public enum VersionQualifierType : byte
{
    Alpha = 0x00,
    Beta = 0x01,
    Final = 0x02
}

/// <summary>
///     Represents a version qualifier for a firmware version.
///     A version qualifier typically includes the firmware version, a type (such as Alpha, Beta, or Final),
///     and an iteration number.
/// </summary>
public class VersionQualifier
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="VersionQualifier" /> class.
    ///     This constructor allows you to specify the firmware version, type, and iteration.
    ///     The iteration must be a non-negative value and less than or equal to int.MaxValue.
    ///     If the firmware version is null, an <see cref="ArgumentNullException" /> will be thrown.
    ///     If the iteration is negative or greater than int.MaxValue, an <see cref="ArgumentOutOfRangeException" /> will be
    ///     thrown.
    /// </summary>
    /// <param name="version">The firmware version associated with this qualifier.</param>
    /// <param name="type">The type of version qualifier (Alpha, Beta, Final).</param>
    /// <param name="iteration">
    ///     The iteration number of the version qualifier, must be a non-negative value and less than or
    ///     equal to int.MaxValue.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public VersionQualifier(Version version, VersionQualifierType type, long iteration)
    {
        if (iteration is < 0 or > uint.MaxValue)
            throw new ArgumentOutOfRangeException(
                nameof(iteration),
                $"Iteration must be between 0 and {uint.MaxValue}.");

        Version = version ?? throw new ArgumentNullException(nameof(version));
        Type = type;
        Iteration = iteration;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="VersionQualifier" /> class with default values.
    ///     The default firmware version is set to a new instance of <see cref="Version" />,
    ///     the type is set to <see cref="VersionQualifierType.Final" />, and the iteration is set to 0.
    /// </summary>
    public VersionQualifier()
    {
        Version = new Version();
        Type = VersionQualifierType.Final;
    }

    /// <summary>
    ///     Represents the firmware version associated with this qualifier.
    /// </summary>
    public Version Version { get; }

    /// <summary>
    ///     Represents the type of version qualifier, such as Alpha, Beta, or Final.
    /// </summary>
    public VersionQualifierType Type { get; }

    /// <summary>
    ///     Represents the iteration number of the version qualifier.
    /// </summary>
    public long Iteration { get; }

    /// <summary>
    ///     Returns a string that represents the current <see cref="VersionQualifier" />.
    /// </summary>
    public override string ToString() => $"{Version}.{Type.ToString().ToLowerInvariant()}.{Iteration}";

    public override bool Equals(object? obj) =>
        obj is VersionQualifier other &&
        Version.Equals(other.Version) &&
        Type == other.Type &&
        Iteration == other.Iteration;

    public override int GetHashCode() => HashCode.Combine(Version, Type, Iteration);
}