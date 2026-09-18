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

using System;

namespace Yubico.YubiKey.Fido2.Cose
{
    /// <summary>
    /// A COSE public key whose algorithm this SDK cannot decode into a strongly
    /// typed key representation, preserved in its original encoded form.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The SDK returns an instance of this class when a YubiKey reports a public
    /// key using an algorithm that its strongly typed COSE key decoder does not
    /// implement. Rather than failing, the SDK preserves the original encoding
    /// in <see cref="EncodedKey"/> along with the key type and algorithm the
    /// YubiKey reported, so callers that understand the representation can
    /// decode it themselves.
    /// </para>
    /// <para>
    /// This typically arises with a credential created by an extension that
    /// introduces its own key type, or with an algorithm registered after this
    /// version of the SDK was released.
    /// </para>
    /// <para>
    /// <see cref="CoseKey.Type"/> and <see cref="CoseKey.Algorithm"/> preserve
    /// the values from the encoding. Either value may still be a named member
    /// of <see cref="CoseKeyType"/> or <see cref="CoseAlgorithmIdentifier"/>;
    /// do not assume that both values are unrecognized merely because the full
    /// algorithm is not implemented by the typed decoder.
    /// Check whether a decoded key is an instance of this class rather than inferring support from
    /// whether the reported enum values are named.
    /// </para>
    /// <para>
    /// This class has no public constructor. Instances are produced by the SDK
    /// while decoding a response from a YubiKey, and by
    /// <see cref="CoseKey.CreateOrUnsupported(System.ReadOnlyMemory{byte})"/>,
    /// which callers can use to decode an encoding they hold themselves — for
    /// example one their application persisted after an earlier registration.
    /// </para>
    /// <para>
    /// <see cref="EncodedKey"/> is fixed at construction. The inherited
    /// <see cref="CoseKey.Type"/> and <see cref="CoseKey.Algorithm"/>
    /// properties are settable, but changing them does not alter
    /// <see cref="EncodedKey"/> or the result of <see cref="Encode"/>, and
    /// will make the reported metadata disagree with the encoded key. Treat
    /// them as read-only.
    /// </para>
    /// </remarks>
    public sealed class CoseUnsupportedPublicKey : CoseKey
    {
        /// <summary>
        /// The original COSE encoding this key was decoded from, byte for byte.
        /// </summary>
        /// <remarks>
        /// When the key came from a YubiKey response this is exactly what the
        /// YubiKey returned. When it came from
        /// <see cref="CoseKey.CreateOrUnsupported(System.ReadOnlyMemory{byte})"/>
        /// it is exactly the encoding the caller supplied, excluding any trailing
        /// data that followed the key in the caller's buffer.
        /// </remarks>
        public ReadOnlyMemory<byte> EncodedKey { get; }

        /// <summary>
        /// Build a new instance from the given encoded key and the key type and
        /// algorithm reported within it.
        /// </summary>
        /// <param name="encodedKey">
        /// The COSE encoding of the key. The data is copied.
        /// </param>
        /// <param name="type">
        /// The key type (COSE label 1) reported by the encoding. COSE requires
        /// this label, so callers reject an encoding that omits it rather than
        /// passing a placeholder.
        /// </param>
        /// <param name="algorithm">
        /// The algorithm (COSE label 3) reported by the encoding.
        /// </param>
        internal CoseUnsupportedPublicKey(
            ReadOnlyMemory<byte> encodedKey,
            CoseKeyType type,
            CoseAlgorithmIdentifier algorithm)
        {
            EncodedKey = encodedKey.ToArray();
            Type = type;
            Algorithm = algorithm;
        }

        /// <summary>
        /// Return a new byte array containing the original COSE encoding of the
        /// key.
        /// </summary>
        /// <remarks>
        /// Because the SDK does not implement this key's algorithm, it cannot
        /// re-encode the key from decoded components. This method returns a
        /// copy of <see cref="EncodedKey"/>, so the result is byte-for-byte
        /// identical to the encoding this key was decoded from.
        /// </remarks>
        /// <returns>
        /// The encoded key.
        /// </returns>
        public override byte[] Encode() => EncodedKey.ToArray();
    }
}
