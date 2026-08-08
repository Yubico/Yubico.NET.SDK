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
/// Provides replaceable elliptic-curve Diffie-Hellman primitive operations.
/// </summary>
internal interface IEcdhPrimitives
{
    /// <summary>
    /// Generates an ECDH key pair on <paramref name="curve"/>.
    /// </summary>
    /// <remarks>The returned private value in <see cref="ECParameters.D"/> is caller-owned and must be zeroed.</remarks>
    ECParameters GenerateKeyPair(ECCurve curve);

    /// <summary>
    /// Computes the raw ECDH shared secret between <paramref name="localPrivateKey"/> and
    /// <paramref name="remotePublicKey"/>.
    /// </summary>
    /// <remarks>
    /// <paramref name="localPrivateKey"/> must contain a private value (<see cref="ECParameters.D"/>) and a
    /// public point (<see cref="ECParameters.Q"/>) that correspond to the same key pair, such as the value
    /// returned by exporting an existing key's parameters with its private component included. Passing a
    /// public point that does not correspond to the private value produces an invalid shared secret and may
    /// be rejected by some platform crypto backends.
    /// </remarks>
    byte[] ComputeSharedSecret(ECParameters localPrivateKey, ECParameters remotePublicKey);
}