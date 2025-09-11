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

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Contains constant values for the hmac-secret extension.
    /// </summary>
    internal static class HmacSecretConstants
    {
        internal const int TagKeyAgreeKey = 1;
        internal const int TagEncryptedSalt = 2;
        internal const int TagAuthenticatedSalt = 3;
        internal const int TagPinProtocol = 4;
        internal const int HmacSecretSaltLength = 32;
    }
}
