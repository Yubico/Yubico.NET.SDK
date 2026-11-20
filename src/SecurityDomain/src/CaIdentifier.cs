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

/// <summary>A CA identifier and its associated Security Domain key reference.</summary>
/// <param name="KeyReference">The key reference decoded from the response's reference value.</param>
/// <param name="Identifier">CA identifier value bytes, without their TLV tag or length; not a certificate or trust proof.</param>
public sealed record CaIdentifier(KeyReference KeyReference, ReadOnlyMemory<byte> Identifier);