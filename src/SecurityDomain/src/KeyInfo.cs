// Copyright 2026 Yubico AB
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

using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;

namespace Yubico.YubiKit.SecurityDomain;

/// <summary>A two-byte component descriptor returned in a Security Domain key-information entry.</summary>
/// <param name="Tag">Device-reported key type, for example 0x88 for AES.</param>
/// <param name="Value">Associated length descriptor, for example 0x10 for a 16-byte AES key.</param>
public readonly record struct SecurityDomainKeyInfoComponent(byte Tag, byte Value);

/// <summary>Parsed metadata for one Security Domain key reference; contains no secret key bytes.</summary>
/// <param name="KeyReference">The device-reported key identifier and version.</param>
/// <param name="Components">Component descriptors in response order, not a complete raw entry.</param>
public sealed record KeyInfo(
    KeyReference KeyReference,
    IReadOnlyList<SecurityDomainKeyInfoComponent> Components);