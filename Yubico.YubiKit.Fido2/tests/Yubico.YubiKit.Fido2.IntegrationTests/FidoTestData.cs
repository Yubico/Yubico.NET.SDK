// Copyright 2025 Yubico AB
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
using Yubico.YubiKit.Core.Cryptography.Cose;
using Yubico.YubiKit.Fido2.Credentials;

namespace Yubico.YubiKit.Fido2.IntegrationTests;

/// <summary>
/// Shared test constants and generators for FIDO2 integration tests.
/// </summary>
public static class FidoTestData
{
    /// <summary>
    /// Standard test relying party ID.
    /// </summary>
    public const string RpId = "localhost";
    
    /// <summary>
    /// Standard test relying party name.
    /// </summary>
    public const string RpName = "Test RP";
    
    /// <summary>
    /// Standard test user name.
    /// </summary>
    public const string UserName = "testuser@example.com";
    
    /// <summary>
    /// Standard test user display name.
    /// </summary>
    public const string UserDisplayName = "Test User";
    
    /// <summary>
    /// Test PIN that meets enhanced complexity requirements (8+ chars, mixed case + numbers).
    /// </summary>
    public const string Pin = "Abc12345";
    
    /// <summary>
    /// Simple PIN for devices without complexity enforcement.
    /// </summary>
    public const string SimplePinFallback = "123456";
    
    /// <summary>
    /// Generates a random 16-byte user ID.
    /// </summary>
    public static byte[] GenerateUserId() => RandomNumberGenerator.GetBytes(16);
    
    /// <summary>
    /// Generates a random 32-byte challenge (client data hash).
    /// </summary>
    public static byte[] GenerateChallenge() => RandomNumberGenerator.GetBytes(32);
    
    /// <summary>
    /// Creates a standard relying party entity for tests.
    /// </summary>
    public static PublicKeyCredentialRpEntity CreateRelyingParty() => 
        new(RpId, RpName);
    
    /// <summary>
    /// Creates a standard user entity for tests with a random user ID.
    /// </summary>
    public static PublicKeyCredentialUserEntity CreateUser() => 
        new(GenerateUserId(), UserName, UserDisplayName);
    
    /// <summary>
    /// Creates a user entity with a specific user ID.
    /// </summary>
    public static PublicKeyCredentialUserEntity CreateUser(byte[] userId) => 
        new(userId, UserName, UserDisplayName);
    
    /// <summary>
    /// Gets the standard ES256 credential parameters.
    /// </summary>
    public static IReadOnlyList<PublicKeyCredentialParameters> ES256Params =>
        [new PublicKeyCredentialParameters(CoseAlgorithmIdentifier.ES256)];
    
    /// <summary>
    /// Gets EdDSA credential parameters.
    /// </summary>
    public static IReadOnlyList<PublicKeyCredentialParameters> EdDSAParams =>
        [new PublicKeyCredentialParameters(CoseAlgorithmIdentifier.EdDSA)];
}
