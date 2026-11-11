// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0

using System.Security.Cryptography;

namespace Yubico.YubiKit.Core.Cryptography;

/// <summary>
/// Creates the default .NET ECDH primitive implementation.
/// </summary>
internal static class EcdhPrimitives
{
    /// <summary>
    /// Creates a new ECDH primitive implementation.
    /// </summary>
    internal static IEcdhPrimitives Create() => new DotNetEcdhPrimitives();

    private sealed class DotNetEcdhPrimitives : IEcdhPrimitives
    {
        public ECParameters GenerateKeyPair(ECCurve curve)
        {
            using var ecdh = ECDiffieHellman.Create(curve);
            return ecdh.ExportParameters(true);
        }

        public byte[] ComputeSharedSecret(ECParameters localPrivateKey, ECParameters remotePublicKey)
        {
            using var localKey = ECDiffieHellman.Create(localPrivateKey);
            using var peerKey = ECDiffieHellman.Create(remotePublicKey);
            using ECDiffieHellmanPublicKey peerPublicKey = peerKey.PublicKey;
            return localKey.DeriveRawSecretAgreement(peerPublicKey);
        }
    }
}