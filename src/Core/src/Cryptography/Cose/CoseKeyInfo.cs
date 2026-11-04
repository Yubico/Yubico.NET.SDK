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

namespace Yubico.YubiKit.Core.Cryptography.Cose;

/// <summary>Immutable registered COSE key representation (kty/crv), independent of alg.</summary>
/// <remarks>A mapping implies neither codec, profile, operation nor hardware capability.</remarks>
public sealed record CoseKeyInfo
{
    internal CoseKeyInfo(CoseKeyType type, CoseEcCurve? curveIdentifier = null)
    {
        Type = type;
        CurveIdentifier = curveIdentifier;
    }

    /// <summary>The registered COSE key type (kty), independent of the operation's alg number.</summary>
    public CoseKeyType Type { get; }
    /// <summary>The registered COSE curve (crv), or null for key types without a modeled curve.</summary>
    public CoseEcCurve? CurveIdentifier { get; }
}