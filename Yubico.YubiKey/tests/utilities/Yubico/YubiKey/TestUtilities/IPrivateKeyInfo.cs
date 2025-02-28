// Copyright 2024 Yubico AB
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

namespace Yubico.YubiKey.TestUtilities;

public interface IPrivateKeyInfo
{
    public string AlgorithmOid { get; }
    public string? CurveOid { get; }
    public byte[] PrivateKey { get; }
}

public class PrivateKeyInfo : IPrivateKeyInfo
{
    public required string AlgorithmOid { get; init; }
    public string? CurveOid { get; init; }
    public required byte[] PrivateKey { get; init; }
}

public class EdPrivateKeyInfo : PrivateKeyInfo
{
}

public class EcPrivateKeyInfo : PrivateKeyInfo
{
}

public class RsaPrivateKeyInfo : PrivateKeyInfo
{
    public required byte[] Modulus { get; set; }
    public required byte[] PublicExponent { get; init; }
    public required byte[] PrivateExponent { get; set; }
    public required byte[] Prime1 { get; set; }
    public required byte[] Prime2 { get; set; }
    public required byte[] Exponent1 { get; set; }
    public required byte[] Exponent2 { get; set; }
    public required byte[] Coefficient { get; set; }
}
