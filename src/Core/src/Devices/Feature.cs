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

namespace Yubico.YubiKit.Core.Devices;

/// <summary>A named feature and its minimum release-firmware version for SDK compatibility checks.</summary>
public readonly record struct Feature
{
    /// <summary>Defines a feature's name and minimum major.minor.revision version.</summary>
    /// <param name="name">The feature's display name.</param>
    /// <param name="versionMajor">The nonnegative major component.</param>
    /// <param name="versionMinor">The minor component.</param>
    /// <param name="versionRevision">The patch component.</param>
    public Feature(string name, int versionMajor, int versionMinor, int versionRevision)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentOutOfRangeException.ThrowIfNegative(versionMajor);

        Name = name;
        VersionMajor = versionMajor;
        VersionMinor = versionMinor;
        VersionRevision = versionRevision;
    }

    /// <summary>The feature's name.</summary>
    public string Name { get; init; }
    /// <summary>The minimum major component.</summary>
    public int VersionMajor { get; init; }
    /// <summary>The minimum minor component.</summary>
    public int VersionMinor { get; init; }
    /// <summary>The minimum patch component.</summary>
    public int VersionRevision { get; init; }
    /// <summary>The minimum version, with each component validated to fit one byte when accessed.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A component is outside 0..255.</exception>
    public FirmwareVersion Version => new(VersionMajor, VersionMinor, VersionRevision);

    /// <summary>Checks the minimum firmware version, accepting major-zero firmware optimistically.</summary>
    /// <param name="firmwareVersion">The firmware version to check.</param>
    /// <returns>True for major-zero firmware or firmware at least as new as <see cref="Version"/>.</returns>
    /// <remarks>Application configuration and actual device support remain prerequisites of the operation.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="firmwareVersion"/> is null.</exception>
    public bool IsSupportedByFirmware(FirmwareVersion firmwareVersion)
    {
        ArgumentNullException.ThrowIfNull(firmwareVersion);

        return firmwareVersion.IsAlphaOrBeta || firmwareVersion >= Version;
    }
}