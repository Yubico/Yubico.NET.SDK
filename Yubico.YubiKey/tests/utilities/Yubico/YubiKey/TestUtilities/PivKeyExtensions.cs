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

using System;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Piv;
using Yubico.YubiKey.Piv.Converters;

namespace Yubico.YubiKey.TestUtilities;

[Obsolete("Usage of PivEccPublic/PivEccPrivateKey PivRsaPublic/PivRsaPrivateKey is deprecated. Use implementations of ECPublicKey, ECPrivateKey and RSAPublicKey, RSAPrivateKey instead", false)]
public static class PivKeyExtensions
{
    public static RSAPrivateKey ConvertToGeneric(
        this PivRsaPrivateKey other) =>
        PivKeyDecoder.CreateRSAPrivateKey(other.EncodedPrivateKey);
    
    public static PivRsaPrivateKey ConvertToPiv(
        this RSAPrivateKey other) =>
        PivRsaPrivateKey.CreateRsaPrivateKey(PivKeyEncoder.EncodeRSAPrivateKey(other));
    
    public static PrivateKey ConvertToGeneric(
        this PivPrivateKey other) =>
        PivKeyDecoder.CreateRSAPrivateKey(other.EncodedPrivateKey);
    
    public static PivPrivateKey ConvertToPiv(
        this IPrivateKey other) =>
        PivPrivateKey.Create(PivKeyEncoder.EncodePrivateKey(other));
    
    public static PivPublicKey ConvertToPiv(
        this IPublicKey other) =>
        PivPublicKey.Create(PivKeyEncoder.EncodePublicKey(other));
}
