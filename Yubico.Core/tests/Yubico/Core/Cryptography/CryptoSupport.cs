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

using System.Security.Cryptography;

namespace Yubico.Core.Cryptography
{
    public static class CryptoSupport
    {
        // This method will get one of three curves: P-256, P384, or P-512.
        // If the curveNum is
        //    0, return P-256
        //    1, return P-384
        //    2, return P-512
        // If the curveNum is any other value (other than 0, 1, or 2), return
        // P-256.
        public static ECCurve GetNamedCurve(int curveNum) => curveNum switch
        {
            1 => ECCurve.NamedCurves.nistP384,
            2 => ECCurve.NamedCurves.nistP521,
            _ => ECCurve.NamedCurves.nistP256,
        };
    }
}
