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
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2.Cbor
{
    /// <summary>
    /// An interface for representing a class that can generate a CBOR encoding.
    /// </summary>
    /// <remarks>
    /// This is generally used to write out an encoded value for the
    /// <c>CborWriter.WriteEncodedValue</c> method. For example, suppose we need
    /// to write out a CBOR-encoded key as part of a larger CBOR encoding. We
    /// already have the <see cref="CoseKey"/> class which can encode the key. So
    /// we don't need to write the encode code again, we just need to get the
    /// encoding from the object. The resulting encoding can then be copied into
    /// the CBOR structure. An alternative is to get the encoding from the key
    /// object and pass it into the CborHelper, but then we need to know if this
    /// is encoded or a byte string, and there's the Nullable to deal with. So
    /// just implement this interface to indicate that we have an object that can
    /// encode itself and to take the output of that encoding and simply copy it
    /// into the full structure.
    /// </remarks>
    public interface ICborEncode
    {
        /// <summary>
        /// Return a new byte array that is the object encoded following the
        /// FIDO2/CBOR standard.
        /// </summary>
        /// <returns>
        /// The encoded construct.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The object contains no data.
        /// </exception>
        byte[] CborEncode();
    }
}
