// Copyright 2025 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using System.Formats.Cbor;
using CommunityToolkit.Diagnostics;
using Yubico.YubiKey.Fido2;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.TestUtilities.Fido2
{
    public static class PreviewSignParametersExtensions
    {
        public const CoseAlgorithmIdentifier ArkgP256Esp256 =
            (CoseAlgorithmIdentifier)(-65539);

        public const string ExtensionName = Extensions.PreviewSign;

        public static void AddPreviewSignExtension(
            this GetAssertionParameters parameters,
            ReadOnlyMemory<byte> deviceKeyHandle,
            ReadOnlyMemory<byte> arkgKeyHandle,
            ReadOnlyMemory<byte> context,
            byte[] messageDigest)
        {
            Guard.IsNotNull(parameters, nameof(parameters));
            Guard.IsNotNull(messageDigest, nameof(messageDigest));

            if (messageDigest.Length != 32)
            {
                throw new ArgumentException("previewSign requires a 32-byte SHA-256 digest.", nameof(messageDigest));
            }

            byte[] additionalArgs = EncodeArkgSignArgs(arkgKeyHandle, context);
            parameters.AddPreviewSignExtension(deviceKeyHandle, messageDigest, additionalArgs);
        }

        private static byte[] EncodeArkgSignArgs(
            ReadOnlyMemory<byte> arkgKeyHandle,
            ReadOnlyMemory<byte> context)
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(3);

            cbor.WriteInt32(3);
            cbor.WriteInt32((int)ArkgP256Esp256);

            cbor.WriteInt32(-1);
            cbor.WriteByteString(arkgKeyHandle.Span);

            cbor.WriteInt32(-2);
            cbor.WriteByteString(context.Span);

            cbor.WriteEndMap();
            return cbor.Encode();
        }
    }
}
