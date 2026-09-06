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

using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;

namespace Yubico.YubiKit.Core.UnitTests.Cryptography;

/// <summary>
/// The EC facts the ASN.1 encoder and decoder tests restate independently of the code under test.
/// </summary>
/// <remarks>
/// <see cref="UncompressedPoint"/> deliberately reimplements the point layout instead of calling
/// <c>AsnUtilities.BuildUncompressedEcPoint</c>: a test that built its input with the production
/// helper would only be checking that helper against itself.
/// </remarks>
internal static class EcTestSupport
{
    public static ECCurve NamedCurve(string curveOid) => curveOid switch
    {
        Oids.ECP256 => ECCurve.NamedCurves.nistP256,
        Oids.ECP384 => ECCurve.NamedCurves.nistP384,
        Oids.ECP521 => ECCurve.NamedCurves.nistP521,
        _ => throw new ArgumentOutOfRangeException(nameof(curveOid))
    };

    /// <summary>
    /// The coordinate size of each supported prime curve, restated from the curve definitions rather
    /// than looked up through <c>KeyDefinitions</c>, for the same reason as
    /// <see cref="UncompressedPoint"/>. The private scalar is the same width.
    /// </summary>
    public static int CoordinateSize(string curveOid) => curveOid switch
    {
        Oids.ECP256 => 32,
        Oids.ECP384 => 48,
        Oids.ECP521 => 66,
        _ => throw new ArgumentOutOfRangeException(nameof(curveOid))
    };

    public static byte[] UncompressedPoint(byte[] x, byte[] y)
    {
        var point = new byte[1 + x.Length + y.Length];
        point[0] = 0x04;
        x.CopyTo(point, 1);
        y.CopyTo(point, 1 + x.Length);
        return point;
    }
}