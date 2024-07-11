// Copyright 2021 Yubico AB
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

namespace Yubico.YubiKey.Otp
{
    /// <summary>
    /// The NFC Forum well known types that are supported by the YubiKey's NDEF payload.
    /// </summary>
    public enum NdefDataType
    {
        /// <summary>
        /// An NFC text record. Type "T" / "urn:nfc:wkt:T".
        /// </summary>
        Text,

        /// <summary>
        /// An NFC Uniform Resource Identifier (URI) record. Type "U" / "urn:nfc:wkt:U".
        /// </summary>
        Uri,
    }
}
