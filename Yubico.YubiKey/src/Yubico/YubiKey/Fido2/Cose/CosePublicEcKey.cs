// Copyright 2022 Yubico AB
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
using System.Security.Cryptography;
using Yubico.YubiKey.Fido2.Commands;

namespace Yubico.YubiKey.Fido2.Cose
{
    public class CosePublicEcKey : CoseKey
    {
        private const long TagCurve = -1;
        private const long TagX = -2;
        private const long TagY = -3;

        private ECParameters _ecParameters;

        public CoseEcCurve Curve
        {
            get => NamedCurveToCoseCurve(_ecParameters.Curve);
            set => CoseCurveToNamedCurve(value);
        }

        public ReadOnlyMemory<byte> X
        {
            get => _ecParameters.Q.X;
            set => _ecParameters.Q.X = value.ToArray();
        }

        public ReadOnlyMemory<byte> Y
        {
            get => _ecParameters.Q.Y;
            set => _ecParameters.Q.Y = value.ToArray();
        }

        /// <inheritdoc />
        public CosePublicEcKey(CborMap map) : base(map)
        {
            if (map is null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            Curve = (CoseEcCurve)map.ReadUInt64(TagCurve);
            X = map.ReadByteString(TagX);
            Y = map.ReadByteString(TagY);
        }

        public CosePublicEcKey(ECParameters ecParameters)
        {
            _ecParameters = ecParameters;
        }

        public CosePublicEcKey()
        {

        }

        public ECParameters AsEcParameters() => _ecParameters;

        private static ECCurve CoseCurveToNamedCurve(CoseEcCurve curveId) =>
            curveId switch
            {
                CoseEcCurve.P256 => ECCurve.NamedCurves.nistP256,
                CoseEcCurve.P384 => ECCurve.NamedCurves.nistP384,
                CoseEcCurve.P521 => ECCurve.NamedCurves.nistP521,
                _ => throw new NotSupportedException("Elliptic curve not supported.")
            };

        private static CoseEcCurve NamedCurveToCoseCurve(ECCurve namedCurve) =>
            namedCurve switch
            {
                _ when namedCurve.Oid == ECCurve.NamedCurves.nistP256.Oid => CoseEcCurve.P256,
                _ when namedCurve.Oid == ECCurve.NamedCurves.nistP384.Oid => CoseEcCurve.P384,
                _ when namedCurve.Oid == ECCurve.NamedCurves.nistP521.Oid => CoseEcCurve.P521,
                _ => throw new NotSupportedException("Elliptic curve not supported.")
            };
    }
}
