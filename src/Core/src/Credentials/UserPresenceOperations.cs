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

/// <summary>Identifiers for the SDK operations that report <see cref="UserPresenceContext.Operation" />.</summary>
/// <remarks>
///     Operations are grouped by application. Each value is unique across all applications and has the form
///     <c>Application.Method</c>, where <c>Method</c> is the public SDK method name without <c>Async</c>, so a
///     prompt can switch on <see cref="UserPresenceContext.Operation" /> alone. This is an open set: future
///     releases may add values, so consumers should include a default branch.
/// </remarks>
public static class UserPresenceOperations
{
    /// <summary>Operations reported by FIDO2 sessions.</summary>
    public static class Fido2
    {
        /// <summary>Authenticator selection.</summary>
        public const string Selection = "Fido2.Selection";

        /// <summary>FIDO application reset.</summary>
        public const string Reset = "Fido2.Reset";

        /// <summary>Credential registration.</summary>
        public const string MakeCredential = "Fido2.MakeCredential";

        /// <summary>Credential assertion (sign-in).</summary>
        public const string GetAssertion = "Fido2.GetAssertion";
    }

    /// <summary>Operations reported by PIV sessions.</summary>
    public static class Piv
    {
        /// <summary>Private-key signing or decryption; the SDK method does not distinguish the two.</summary>
        public const string SignOrDecrypt = "Piv.SignOrDecrypt";

        /// <summary>RSA decryption.</summary>
        public const string Decrypt = "Piv.Decrypt";

        /// <summary>Key agreement.</summary>
        public const string CalculateSecret = "Piv.CalculateSecret";
    }

    /// <summary>Operations reported by OpenPGP sessions.</summary>
    public static class OpenPgp
    {
        /// <summary>Signing with the signature key.</summary>
        public const string Sign = "OpenPgp.Sign";

        /// <summary>Decryption with the decryption key.</summary>
        public const string Decrypt = "OpenPgp.Decrypt";

        /// <summary>Authentication with the authentication key.</summary>
        public const string Authenticate = "OpenPgp.Authenticate";

        /// <summary>Key attestation.</summary>
        public const string AttestKey = "OpenPgp.AttestKey";
    }

    /// <summary>Operations reported by OATH sessions.</summary>
    public static class Oath
    {
        /// <summary>Raw OATH calculation.</summary>
        public const string Calculate = "Oath.Calculate";

        /// <summary>OATH code calculation.</summary>
        public const string CalculateCode = "Oath.CalculateCode";
    }

    /// <summary>Operations reported by YubiOTP sessions.</summary>
    public static class YubiOtp
    {
        /// <summary>HMAC-SHA1 challenge-response.</summary>
        public const string CalculateHmacSha1 = "YubiOtp.CalculateHmacSha1";

        /// <summary>Yubico OTP challenge-response.</summary>
        public const string CalculateYubicoOtp = "YubiOtp.CalculateYubicoOtp";
    }

    /// <summary>Operations reported by YubiHSM Auth sessions.</summary>
    public static class YubiHsmAuth
    {
        /// <summary>Session-key calculation with a symmetric credential.</summary>
        public const string CalculateSessionKeysSymmetric = "YubiHsmAuth.CalculateSessionKeysSymmetric";

        /// <summary>Session-key calculation with an asymmetric credential.</summary>
        public const string CalculateSessionKeysAsymmetric = "YubiHsmAuth.CalculateSessionKeysAsymmetric";
    }
}