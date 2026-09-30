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

namespace Yubico.YubiKit.Core.Credentials;

/// <summary>SDK method identifiers for user-presence notifications.</summary>
/// <remarks>This is an open set: future releases may add values. Consumers should include a default branch.</remarks>
public static class UserPresenceOperations
{
    /// <summary>Emitted by FIDO2.</summary>
    public const string Selection = "Selection";
    /// <summary>Emitted by FIDO2.</summary>
    public const string Reset = "Reset";
    /// <summary>Emitted by FIDO2.</summary>
    public const string MakeCredential = "MakeCredential";
    /// <summary>Emitted by FIDO2.</summary>
    public const string GetAssertion = "GetAssertion";
    /// <summary>Emitted by PIV.</summary>
    public const string SignOrDecrypt = "SignOrDecrypt";
    /// <summary>Emitted by PIV and OpenPGP.</summary>
    public const string Decrypt = "Decrypt";
    /// <summary>Emitted by PIV.</summary>
    public const string CalculateSecret = "CalculateSecret";
    /// <summary>Emitted by OpenPGP.</summary>
    public const string Sign = "Sign";
    /// <summary>Emitted by OpenPGP.</summary>
    public const string Authenticate = "Authenticate";
    /// <summary>Emitted by OpenPGP.</summary>
    public const string AttestKey = "AttestKey";
    /// <summary>Emitted by OATH.</summary>
    public const string Calculate = "Calculate";
    /// <summary>Emitted by OATH.</summary>
    public const string CalculateCode = "CalculateCode";
    /// <summary>Emitted by YubiOTP.</summary>
    public const string CalculateHmacSha1 = "CalculateHmacSha1";
    /// <summary>Emitted by YubiOTP.</summary>
    public const string CalculateYubicoOtp = "CalculateYubicoOtp";
    /// <summary>Emitted by YubiHSM Auth.</summary>
    public const string CalculateSessionKeysSymmetric = "CalculateSessionKeysSymmetric";
    /// <summary>Emitted by YubiHSM Auth.</summary>
    public const string CalculateSessionKeysAsymmetric = "CalculateSessionKeysAsymmetric";
}