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

public class Feature(string name, int versionMajor, int versionMinor, int versionRevision)
{
    public string Name { get; } = name;
    public int VersionMajor { get; init; } = versionMajor;
    public int VersionMinor { get; init; } = versionMinor;
    public int VersionRevision { get; init; } = versionRevision;
    public FirmwareVersion Version { get; } = new(versionMajor, versionMinor, versionRevision);
}