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

using System;
using System.Collections.Generic;
using Yubico.YubiKey.Fido2.Serialization;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Device information returned by the GetDeviceInfo FIDO2 command.
    /// </summary>
    [CborSerializable]
    internal class DeviceInfo
    {
        /// <summary>
        /// List of version strings of CTAP supported by the authenticator.
        /// </summary>
        [CborLabelId(0x01)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Arrays for CTAP properties")]
        public string[] Versions { get; set; } = Array.Empty<string>();

        /// <summary>
        /// An optional dictionary of extensions.
        /// </summary>
        [CborLabelId(0x02)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Arrays for CTAP properties")]
        public string[]? Extensions { get; set; }

        /// <summary>
        /// The claimed 'Authenticator Attestation Globally Unique Identifier' - a 128-bit identifier indicating the type of authenticator.
        /// </summary>
        [CborLabelId(0x03)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Arrays for CTAP properties")]
        public byte[] AAGuid { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// An optional dictionary of options.
        /// </summary>
        [CborLabelId(0x04)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Dictionaries for CTAP properties")]
        public Dictionary<string, bool>? Options { get; set; }

        /// <summary>
        /// Maximum message size supported by the authenticator.
        /// </summary>
        [CborLabelId(0x05)]
        [CborSerializeAsUnsigned]
        public int? MaxMessageSize { get; set; }

        /// <summary>
        /// List of supported PIN Protocol versions; 'pinAuthProtocols' in CTAP2.
        /// </summary>
        [CborLabelId(0x06)]
        [CborSerializeAsUnsigned]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Arrays for CTAP properties")]
        public int[]? PinUserVerificationAuthenticatorProtocols { get; set; }
    }
}
