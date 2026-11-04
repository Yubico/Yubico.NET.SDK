// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Yubico.YubiKit.Core.Cryptography.Cose;

/// <summary>Immutable COSE representation metadata, independent of operation algorithms.</summary>
/// <remarks>A mapping does not imply codec, operation, profile, or hardware support.</remarks>
public sealed record CoseKeyDefinition
{
    /// <summary>Gets the registered COSE key family (kty).</summary>
    public required CoseKeyType Type { get; init; }

    /// <summary>Gets the curve (crv) for EC2/OKP keys, or null for RSA/symmetric keys.</summary>
    public CoseEcCurve? CurveIdentifier { get; init; }
}