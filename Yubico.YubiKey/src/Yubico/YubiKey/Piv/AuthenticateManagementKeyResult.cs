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

namespace Yubico.YubiKey.Piv
{
    /// <summary>
    /// This enum lists the possible results of authenticating the management key.
    /// </summary>
    /// <remarks>
    /// The response to the
    /// <see cref="Commands.CompleteAuthenticateManagementKeyCommand"/> is
    /// <see cref="Commands.CompleteAuthenticateManagementKeyResponse"/>. Call the
    /// <c>GetData</c> method in the response object to get the result of the
    /// authentication process. This enum is returned.
    /// <para>
    /// There are two possible modes of authenticating: single and mutual. In
    /// single authentication, only the "Off-Card" application is authenticated
    /// to the YubiKey. In mutual authentication, the Off-Card app is
    /// authenticated, but also the YubiKey is authenticated to the Off-Card app.
    /// </para>
    /// <para>
    /// There are five possible results of a management key authentication:
    /// <code>
    ///                   OffCard                YubiKey
    ///   mutual :  Authenticated          Authenticated
    ///   mutual :  Authenticated          AuthenticationFailed
    ///   mutual :  AuthenticationFailed   Unknown
    ///   single :  Authenticated          Unknown
    ///   single :  AuthenticationFailed   Unknown
    /// </code>
    /// If the process is mutual authentication, but the YubiKey was not able to
    /// authenticate the Off-Card app, it will not provide any information that
    /// allows the YubiKey to be authenticated itself. Hence, if the Off-Card app
    /// is not authenticated, there is no way to know if the YubiKey is
    /// authenticated.
    /// </para>
    /// <para>
    /// In mutual authentication, if the Off-Card app authenticates, but the
    /// YubiKey does not authenticate, operations that require management key
    /// authentication will be able to process, but the device the app with which
    /// the app is communicating is likely not the YubiKey requested.
    /// </para>
    /// </remarks>
    public enum AuthenticateManagementKeyResult
    {
        /// <summary>
        /// Not authenticated, authentication has not been attempted or was not
        /// completed because of an error.
        /// </summary>
        Unauthenticated = 0,

        /// <summary>
        /// Single authentication, Off-Card app did not authenticate.
        /// </summary>
        SingleAuthenticationFailed = 1,

        /// <summary>
        /// Single authentication, Off-Card app authenticated.
        /// </summary>
        SingleAuthenticated = 2,

        /// <summary>
        /// Mutual authentication, Off-Card app did not authenticate,
        /// authentication status of the YubiKey is unknown.
        /// </summary>
        MutualOffCardAuthenticationFailed = 3,

        /// <summary>
        /// Mutual authentication, Off-Card app authenticated, the YubiKey did
        /// not authenticate.
        /// </summary>
        MutualYubiKeyAuthenticationFailed = 4,

        /// <summary>
        /// Mutual authentication, Off-Card app authenticated, the YubiKey
        /// authenticated.
        /// </summary>
        MutualFullyAuthenticated = 5,
    }
}
