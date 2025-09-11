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

using System;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Diagnostics;
using Yubico.YubiKey.Fido2.Cbor;
using Yubico.YubiKey.Fido2.PinProtocols;
using Yubico.YubiKey.Utilities;

namespace Yubico.YubiKey.Fido2
{
    internal static class HmacSecretExtension
    {
        public static Memory<byte> Encode(
            PinUvAuthProtocolBase authProtocol,
            ReadOnlyMemory<byte> salt1,
             ReadOnlyMemory<byte>? salt2 = null)
        {
            ValidateParameters(authProtocol, salt1, salt2);

            var dataToEncrypt = salt2.HasValue
                ? salt1.Concat(salt2.Value)
                : salt1;

            byte[] encryptedSalt = authProtocol.Encrypt(dataToEncrypt);
            byte[] authenticatedSalt = authProtocol.Authenticate(encryptedSalt);

            return new CborMapWriter<int>()
                .Entry(HmacSecretConstants.TagKeyAgreeKey, authProtocol.PlatformPublicKey!)
                .Entry(HmacSecretConstants.TagEncryptedSalt, encryptedSalt.AsMemory())
                .Entry(HmacSecretConstants.TagAuthenticatedSalt, authenticatedSalt.AsMemory())
                .Entry(HmacSecretConstants.TagPinProtocol, (int)authProtocol.Protocol)
                .Encode();
        }


        private static void ValidateParameters(PinUvAuthProtocolBase authProtocol, ReadOnlyMemory<byte> salt1, ReadOnlyMemory<byte>? salt2)
        {
            Guard.IsNotNull(authProtocol, nameof(authProtocol));

            if (authProtocol.EncryptionKey is null || authProtocol.PlatformPublicKey is null)
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionMessages.Fido2NotEncapsulated);
            }

            if (salt1.Length != HmacSecretConstants.HmacSecretSaltLength)
            {
                ThrowHelper.ThrowArgumentException(ExceptionMessages.InvalidSaltLength, nameof(salt1));
            }

            if (salt2.HasValue && salt2.Value.Length != HmacSecretConstants.HmacSecretSaltLength)
            {
                ThrowHelper.ThrowArgumentException(ExceptionMessages.InvalidSaltLength, nameof(salt2));
            }
        }
    }
}
