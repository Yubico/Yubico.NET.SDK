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

namespace Yubico.YubiKit.Core.YubiKey;

public readonly record struct Feature
{
    public Feature(string name, int versionMajor, int versionMinor, int versionRevision)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentOutOfRangeException.ThrowIfNegative(versionMajor);

        Name = name;
        VersionMajor = versionMajor;
        VersionMinor = versionMinor;
        VersionRevision = versionRevision;
    }
    
    public string Name { get; init; }
    public int VersionMajor { get; init; }
    public int VersionMinor { get; init; }
    public int VersionRevision { get; init; }
    public FirmwareVersion Version => new(VersionMajor, VersionMinor, VersionRevision);
}