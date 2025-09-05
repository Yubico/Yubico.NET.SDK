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
using System.Collections.Generic;
using System.Formats.Cbor;
using System.Globalization;
using System.Linq;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Fido2.Cbor;
using Yubico.YubiKey.Fido2.Cose;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Device information returned by the FIDO2 GetDeviceInfo command.
    /// </summary>
    public class AuthenticatorInfo
    {
        private const int KeyVersions = 0x01;
        private const int KeyExtensions = 0x02;
        private const int KeyAaguid = 0x03;
        private const int KeyOptions = 0x04;
        private const int KeyMaxMsgSize = 0x05;
        private const int KeyPinUvAuthProtocols = 0x06;
        private const int KeyMaxCredentialCountInList = 0x07;
        private const int KeyMaxCredentialIdLength = 0x08;
        private const int KeyTransports = 0x09;
        private const int KeyAlgorithms = 0x0A;
        private const int KeyMaxSerializedLargeBlobArray = 0x0B;
        private const int KeyForcePinChange = 0x0C;
        private const int KeyMinPinLength = 0x0D;
        private const int KeyFirmwareVersion = 0x0E;
        private const int KeyMaxCredBlobLength = 0x0F;
        private const int KeyMaxRpidsForSetMinPinLength = 0x10;
        private const int KeyPreferredPlatformUvAttempts = 0x11;
        private const int KeyUvModality = 0x12;
        private const int KeyCertifications = 0x13;
        private const int KeyRemainingDiscoverableCredentials = 0x14;
        private const int KeyVendorPrototypeConfigCommands = 0x15;
        private const int KeyAttestationFormats = 0x16;
        private const int KeyUvCountSinceLastPinEntry = 0x17;
        private const int KeyLongTouchForReset = 0x18;
        private const int KeyEncIdentifier = 0x19;
        private const int KeyTransportsForReset = 0x1A;
        private const int KeyPinComplexityPolicy = 0x1B;
        private const int KeyPinComplexityPolicyUrl = 0x1C;
        private const int KeyMaxPinLength = 0x1D;

        /// <summary>
        /// An <see cref="Aaguid"/> is defined in the standard as 16 bytes, no
        /// more, no less.
        /// </summary>
        public const int AaguidLength = 16;

        /// <summary>
        /// If no <see cref="MaximumMessageSize"/> is given, the standard
        /// specifies a default size of 1024.
        /// </summary>
        public const int DefaultMaximumMessageSize = 1024;

        /// <summary>
        /// If no <see cref="MinimumPinLength"/> is given, the standard
        /// specifies a default length of 4.
        /// </summary>
        public const int DefaultMinimumPinLength = 4;

        /// <summary>
        /// The string in the <see cref="Versions"/> property that indicates
        /// FIDO U2F.
        /// </summary>
        public const string VersionU2f = "U2F_V2";

        /// <summary>
        /// The string in the <see cref="Versions"/> property that indicates
        /// FIDO2 version 2.0.
        /// </summary>
        public const string Version20 = "FIDO_2_0";

        /// <summary>
        /// The string in the <see cref="Versions"/> property that indicates
        /// FIDO2 version 2.1 preview.
        /// </summary>
        public const string Version21Pre = "FIDO_2_1_PRE";

        /// <summary>
        /// The string in the <see cref="Versions"/> property that indicates
        /// FIDO2 version 2.1.
        /// </summary>
        public const string Version21 = "FIDO_2_1";

        /// <summary>
        /// List of version strings of CTAP supported by the authenticator.
        /// This is a REQUIRED value.
        /// </summary>
        /// <remarks>
        /// A list of strings is not the easiest to parse, but that is the way
        /// the standard specifies reporting the supported versions. If you want
        /// to know if a particular version is supported, call the
        /// <c>Contains</c> method of the <c>IReadOnlyList</c> interface, using
        /// the strings defined in this class. For example, suppose you build a
        /// <c>Fido2Session</c> object and you want to know whether the connected
        /// YubiKey supports version 2.1, your code would look something like
        /// this.
        /// <code language="csharp">
        ///    if (fido2Session.AuthenticatorInfo.Versions.Contains(AuthenticatorInfo.Version21))
        ///    {
        ///        . . .
        ///    }
        /// </code>
        /// </remarks>
        public IReadOnlyList<string> Versions { get; }

        /// <summary>
        /// List of extension strings of CTAP supported by the authenticator.
        /// This property is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null.
        /// </summary>
        public IReadOnlyList<string>? Extensions { get; }

        /// <summary>
        /// The AAGUID, unique to the authenticator and model.
        /// This is a REQUIRED value.
        /// </summary>
        public ReadOnlyMemory<byte> Aaguid { get; private set; }

        /// <summary>
        /// The list of supported options. Each entry in the list is a string
        /// describing the option and a boolean, indicating whether it is
        /// supported (<c>true</c>) or not (<c>false</c>).
        /// This property is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null.
        /// </summary>
        public IReadOnlyDictionary<string, bool>? Options { get; }

        /// <summary>
        /// The maximum size, in bytes, of a message sent to the YubiKey.
        /// This property is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null. The standard specifies a default of 1024
        /// (see the field <see cref="DefaultMaximumMessageSize"/>).
        /// </summary>
        public int? MaximumMessageSize { get; }

        /// <summary>
        /// List of PIN/UV Auth Protocols the YubiKey supports. They are given in
        /// the order from most to least preferred.
        /// This property is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null.
        /// </summary>
        public IReadOnlyList<PinUvAuthProtocol>? PinUvAuthProtocols { get; }

        /// <summary>
        /// The maximum number of credentials in the CredentialID list. Note that
        /// this is not the maximum number of credentials on a YubiKey, but the
        /// maximum number of credentials represented in a CredentialID list.
        /// This property is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null.
        /// </summary>
        public int? MaximumCredentialCountInList { get; }

        /// <summary>
        /// The maximum length of a CredentialID.
        /// This property is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null.
        /// </summary>
        public int? MaximumCredentialIdLength { get; }

        /// <summary>
        /// List of transport strings of CTAP supported by the authenticator.
        /// This property is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null.
        /// Valid values are defined in the
        /// <see cref="AuthenticatorTransports"/> class, which contains the
        /// standard-defined transport strings.
        /// </summary>
        public IReadOnlyList<string>? Transports { get; }

        /// <summary>
        /// The list of supported algorithms for credential generation. This is
        /// an optional value and can be null.
        /// </summary>
        /// <remarks>
        /// Each entry in the list is a type and algorithm. Neither the type nor
        /// algorithm are guaranteed to be unique, although each combination is.
        /// Currently, the only type defined is "public-key". The only algorithm
        /// the YubiKey supports is ECDSA with SHA-256 using the NIST P-256
        /// curve. This is the pair
        /// "public-key"/<c>CoseAlgorithmIdentifier.ES256</c>.
        /// </remarks>
        public IReadOnlyList<Tuple<string, CoseAlgorithmIdentifier>>? Algorithms { get; }

        /// <summary>
        /// The maximum size, in bytes, of the serialized large-blob array that
        /// this YubiKey can store. If the authenticatorLargeBlobs command is not
        /// supported, this will be null. If it is supported, it will be a value
        /// greater than 1024.
        /// This property is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null (the authenticatorLargeBlobs command is not supported).
        /// </summary>
        public int? MaximumSerializedLargeBlobArray { get; }

        /// <summary>
        /// If <c>true</c>, certain PIN commands will return errors until the PIN
        /// has been changed. If <c>false</c>, a PIN change is not necessary.
        /// This property is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null.
        /// </summary>
        public bool? ForcePinChange { get; }

        /// <summary>
        /// The current minimum PIN length, in Unicode code points.
        /// This property is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null. The standard specifies a default of 4
        /// (see the field <see cref="DefaultMinimumPinLength"/>).
        /// </summary>
        public int? MinimumPinLength { get; }

        /// <summary>
        /// The version of the firmware on the YubiKey. Note that this is an
        /// <c>int</c>, not an instance of the <c>FirmwareVersion</c> class. The
        /// standard specifies returning an int.
        /// This property is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null.
        /// </summary>
        /// <remarks>
        /// If you examine the result as a hexadecimal 32-bit value, the major,
        /// minor, and Patch numbers will be bytes 2, 1, and 0. For example, if
        /// the YubiKey's firmware is vers 5.4.2, then the result will be decimal
        /// 328,706, which in hex is 0x00050402.
        /// </remarks>
        public int? FirmwareVersion { get; }

        /// <summary>
        /// The maximum length, in bytes, of the "credBlob" if supported.
        /// This property is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null.
        /// </summary>
        public int? MaximumCredentialBlobLength { get; }

        /// <summary>
        /// The maximum number of Relying Party IDs that the YubiKey can set via
        /// the setMinPINLength subcommand.
        /// This property is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null.
        /// </summary>
        public int? MaximumRpidsForSetMinPinLength { get; }

        /// <summary>
        /// The number of attempts to authenticate the UV (e.g. fingerprint) that
        /// fail before using the PIN.
        /// This property is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null.
        /// </summary>
        public int? PreferredPlatformUvAttempts { get; }

        /// <summary>
        /// A bit field indicating the user verification methods supported by the
        /// YubiKey. The meanings of the bits are specified in the FIDO standard,
        /// namely the Registry of Predefined Values, section 3.1.
        /// This property is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null.
        /// </summary>
        public int? UvModality { get; }

        /// <summary>
        /// The list of certifications the YubiKey has obtained. Each
        /// certification is a string and number. The string is the name of the
        /// certification, and the number describes the level. See The FIDO
        /// standard for more information, specifically section 7.3 of CTAP.
        /// This property is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null.
        /// </summary>
        public IReadOnlyDictionary<string, int>? Certifications { get; }

        /// <summary>
        /// The estimated number of additional discoverable credentials that can
        /// be stored.
        /// This property is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null.
        /// </summary>
        public int? RemainingDiscoverableCredentials { get; }

        /// <summary>
        /// A list of vendor command IDs. If this is not null, then the YubiKey
        /// chosen supports the vendor prototype subcommand of Authenticator
        /// Config. If so, the list, which can be empty, will contain the valid
        /// vendor IDs that can be used in that subcommand. If this is null, the
        /// YubiKey chosen does not support the feature.
        /// </summary>
        /// <remarks>
        /// Note that the standard defines a vendor ID as a 64-bit unsigned
        /// integer. These numbers are to be random values.
        /// </remarks>
        public IReadOnlyList<long>? VendorPrototypeConfigCommands { get; }

        /// <summary>
        /// The maximum length of a PIN (in bytes) that the authenticator supports.
        /// Default value is 63, which is the maximum length of a PIN in Unicode code points.
        /// </summary>
        public int MaximumPinLength { get; }

        /// <summary>
        /// If present, a URL that the platform can use to provide the user more information about the enforced PIN policy.
        /// If <c>true</c>, the authenticator is enforcing a PIN complexity policy.
        /// If <c>false</c>, the authenticator is not enforcing a PIN complexity policy
        /// If <c>null</c>, the authenticator does not support this feature.
        /// </summary>
        public ReadOnlyMemory<byte> PinComplexityPolicyUrl { get; }

        /// <summary>
        /// If present, indicates whether the authenticator is enforcing an additional current PIN complexity policy beyond minPINLength.
        /// If <c>true</c>, the authenticator is enforcing a PIN complexity policy.
        /// If <c>false</c>, the authenticator is not enforcing a PIN complexity policy.
        /// If <c>null</c>, the authenticator does not support this feature.
        /// </summary>
        public bool? PinComplexityPolicy { get; }

        /// <summary>
        /// List of transports that support the reset command.
        /// For example, an authenticator may choose not to support this command over NFC
        /// Valid values are defined in the
        /// <see cref="AuthenticatorTransports"/> class, which contains the
        /// standard-defined transport strings.
        /// </summary>
        public IReadOnlyList<string> TransportsForReset { get; } = new List<string>();

        /// <summary>
        /// If present, an encrypted identifier that the platform can use to identify the authenticator across resets.
        /// The platform must use the persistent UV auth token as input to decrypt the identifier.
        /// If <c>null</c>, the authenticator does not support this feature.
        /// The encrypted identifier is 32 bytes: the first 16 bytes are the IV,
        /// and the second 16 bytes are the ciphertext.
        /// The encryption algorithm is AES-128-CBC.
        /// The key is derived from the persistent UV auth token using HKDF-SHA-256
        /// with the info string "encIdentifier" and a salt of 32 bytes of 0x00.
        /// The plaintext is 16 bytes.
        /// </summary>
        public ReadOnlyMemory<byte>? EncIdentifier { get; }

        /// <summary>
        /// If <c>true</c>, the authenticator requires a 10-second touch for reset.
        /// If <c>false</c>, the authenticator does not require a 10-second touch for reset.
        /// </summary>
        public bool LongTouchForReset { get; }

        /// <summary>
        /// If present, the number of internal User Verification operations since the last pin entry including all failed attempts. 
        /// </summary>
        public int? UvCountSinceLastPinEntry { get; }

        /// <summary>
        /// A list of <see cref="Yubico.YubiKey.Fido2.AttestationFormats"/>  supported by the authenticator.
        /// </summary>
        public IReadOnlyList<string> AttestationFormats { get; } = new List<string>();

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private AuthenticatorInfo()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Build a new instance of <see cref="AuthenticatorInfo"/> based on the
        /// given CBOR encoding.
        /// </summary>
        /// <remarks>
        /// The encoding must follow the definition of
        /// <c>authenticatorGetInfo</c> in section 6.4 of the CTAP 2.1 standard.
        /// </remarks>
        /// <param name="cborEncoding">
        /// The device info, encoded following the CTAP 2.1 and CBOR (RFC 8949)
        /// standards.
        /// </param>
        /// <exception cref="Ctap2DataException">
        /// The <c>cborEncoding</c> is not a valid CBOR encoding, or it is not a
        /// correct encoding for FIDO2 device info.
        /// </exception>
        public AuthenticatorInfo(ReadOnlyMemory<byte> cborEncoding)
        {
            try
            {
                var cborMap = new CborMap<int>(cborEncoding);

                Versions = cborMap.ReadArray<string>(KeyVersions);
                Aaguid = cborMap.ReadByteString(KeyAaguid);

                Extensions = cborMap.Contains(KeyExtensions)
                    ? cborMap.ReadArray<string>(KeyExtensions)
                    : null;

                Options = cborMap.Contains(KeyOptions)
                    ? cborMap.ReadMap<string>(KeyOptions).AsDictionary<bool>()
                    : null;

                MaximumMessageSize = cborMap.Contains(KeyMaxMsgSize)
                    ? cborMap.ReadInt32(KeyMaxMsgSize)
                    : null;

                PinUvAuthProtocols = cborMap.Contains(KeyPinUvAuthProtocols)
                    ? ParsePinUvAuthProtocols(cborMap)
                    : null;

                MaximumCredentialCountInList = cborMap.Contains(KeyMaxCredentialCountInList)
                    ? cborMap.ReadInt32(KeyMaxCredentialCountInList)
                    : null;

                MaximumCredentialIdLength = cborMap.Contains(KeyMaxCredentialIdLength)
                    ? cborMap.ReadInt32(KeyMaxCredentialIdLength)
                    : null;

                Transports = cborMap.Contains(KeyTransports)
                    ? cborMap.ReadArray<string>(KeyTransports)
                    : null;

                Algorithms = cborMap.Contains(KeyAlgorithms)
                    ? ParseAlgorithms(cborMap)
                    : null;

                MaximumSerializedLargeBlobArray = cborMap.Contains(KeyMaxSerializedLargeBlobArray)
                    ? cborMap.ReadInt32(KeyMaxSerializedLargeBlobArray)
                    : null;

                ForcePinChange = cborMap.Contains(KeyForcePinChange)
                    ? cborMap.ReadBoolean(KeyForcePinChange)
                    : null;

                MinimumPinLength = cborMap.Contains(KeyMinPinLength)
                    ? cborMap.ReadInt32(KeyMinPinLength)
                    : null;

                FirmwareVersion = cborMap.Contains(KeyFirmwareVersion)
                    ? cborMap.ReadInt32(KeyFirmwareVersion)
                    : null;

                MaximumCredentialBlobLength = cborMap.Contains(KeyMaxCredBlobLength)
                    ? cborMap.ReadInt32(KeyMaxCredBlobLength)
                    : null;

                MaximumRpidsForSetMinPinLength = cborMap.Contains(KeyMaxRpidsForSetMinPinLength)
                    ? cborMap.ReadInt32(KeyMaxRpidsForSetMinPinLength)
                    : null;

                PreferredPlatformUvAttempts = cborMap.Contains(KeyPreferredPlatformUvAttempts)
                    ? cborMap.ReadInt32(KeyPreferredPlatformUvAttempts)
                    : null;

                UvModality = cborMap.Contains(KeyUvModality)
                    ? cborMap.ReadInt32(KeyUvModality)
                    : null;

                Certifications = cborMap.Contains(KeyCertifications)
                    ? cborMap.ReadMap<string>(KeyCertifications).AsDictionary<int>()
                    : null;

                RemainingDiscoverableCredentials = cborMap.Contains(KeyRemainingDiscoverableCredentials)
                    ? cborMap.ReadInt32(KeyRemainingDiscoverableCredentials)
                    : null;

                VendorPrototypeConfigCommands = cborMap.Contains(KeyVendorPrototypeConfigCommands)
                    ? ParseVendorPrototypeConfigCommands(cborMap)
                    : null;

                AttestationFormats = cborMap.Contains(KeyAttestationFormats)
                    ? cborMap.ReadArray<string>(KeyAttestationFormats)
                    : [];

                UvCountSinceLastPinEntry = cborMap.Contains(KeyUvCountSinceLastPinEntry)
                    ? cborMap.ReadInt32(KeyUvCountSinceLastPinEntry)
                    : null;

                LongTouchForReset = cborMap.Contains(KeyLongTouchForReset) && cborMap.ReadBoolean(KeyLongTouchForReset);

                EncIdentifier = cborMap.Contains(KeyEncIdentifier)
                    ? cborMap.ReadByteString(KeyEncIdentifier)
                    : null;

                TransportsForReset = cborMap.Contains(KeyTransportsForReset)
                    ? cborMap.ReadArray<string>(KeyTransportsForReset)
                    : [];

                PinComplexityPolicy = cborMap.Contains(KeyPinComplexityPolicy)
                    ? cborMap.ReadBoolean(KeyPinComplexityPolicy)
                    : null;

                PinComplexityPolicyUrl = cborMap.Contains(KeyPinComplexityPolicyUrl)
                    ? cborMap.ReadByteString(KeyPinComplexityPolicyUrl)
                    : null;

                MaximumPinLength = cborMap.Contains(KeyMaxPinLength)
                    ? cborMap.ReadInt32(KeyMaxPinLength)
                    : 63;
            }
            catch (CborContentException cborException)
            {
                throw new Ctap2DataException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidFido2Info),
                    cborException);
            }

            return;

            List<long> ParseVendorPrototypeConfigCommands(CborMap<int> cborMap)
            {
                var intList = cborMap.ReadArray<object>(KeyVendorPrototypeConfigCommands);
                var int64List = new List<long>(intList.Count);
                for (int index = 0; index < intList.Count; index++)
                {
                    object? currentValue = CborMap<int>.ConvertValue<long>(intList[index]);
                    if (currentValue is long currentValue64)
                    {
                        int64List.Add(currentValue64);
                    }
                    else
                    {
                        int64List.Add(0);
                    }
                }

                return int64List;
            }

            List<Tuple<string, CoseAlgorithmIdentifier>> ParseAlgorithms(CborMap<int> cborMap)
            {
                var entries = cborMap.ReadArray<CborMap<string>>(KeyAlgorithms);
                var algorithms = (
                    from entry in entries
                    let currentType = entry.ReadTextString(ParameterHelpers.TagType)
                    let currentAlg = (CoseAlgorithmIdentifier)entry.ReadInt32(ParameterHelpers.TagAlg)
                    select new Tuple<string, CoseAlgorithmIdentifier>(currentType, currentAlg)
                    ).ToList();

                return algorithms;
            }

            List<PinUvAuthProtocol> ParsePinUvAuthProtocols(CborMap<int> cborMap)
            {
                var temp = cborMap.ReadArray<int>(KeyPinUvAuthProtocols);
                var uvAuthProtocols = new List<PinUvAuthProtocol>(temp.Count);
                uvAuthProtocols.AddRange(temp.Select(t => (PinUvAuthProtocol)t));

                return uvAuthProtocols;
            }
        }

        /// <summary>
        /// Retrieves the identifier derived from the encrypted identifier, using the provided persistent UV authentication token.
        /// </summary>
        /// <param name="persistentUvAuthToken">
        /// The persistent UV authentication token used to derive the key for decryption.
        /// </param>
        /// <returns>
        /// The decrypted identifier as a read-only memory block of bytes, or null if the encrypted identifier is not set.
        /// </returns>
        public ReadOnlyMemory<byte>? GetIdentifier(ReadOnlyMemory<byte> persistentUvAuthToken)
        {
            if (EncIdentifier is null)
            {
                return null;
            }

            if (persistentUvAuthToken.Length == 0)
            {
                return null;
            }

            Span<byte> iv = stackalloc byte[16];
            Span<byte> ct = stackalloc byte[16];
            Span<byte> salt = stackalloc byte[32];
            EncIdentifier.Value.Span[..16].CopyTo(iv);
            EncIdentifier.Value.Span[16..].CopyTo(ct);

            var key = HkdfUtilities.DeriveKey(persistentUvAuthToken.Span, salt, "encIdentifier"u8, 16);
            var decryptedIdentifier = AesUtilities.AesCbcDecrypt(key.Span, iv, ct);
            return decryptedIdentifier;
        }

        /// <summary>
        /// Get the value of the given <c>option</c> in this
        /// <c>AuthenticatorInfo</c>.
        /// </summary>
        /// <remarks>
        /// An option can be "true", "false", or "not supported". This method
        /// will determine which value is appropriate for the given option.
        /// <para>
        /// The FIDO2 standard specifies that each option has a value, even if
        /// an authenticator does not list it. If an option is not listed, its
        /// value is a default, and the standard specifies default values for
        /// each option. This method will determine if an option is listed, and
        /// if so, return the listed value. If not, it will return the default
        /// value. A default value can be "true", "false", or "not supported".
        /// </para>
        /// <para>
        /// If the option is unknown (not one of the standard-defined options),
        /// and it is not listed, this method will return "unknown".
        /// </para>
        /// </remarks>
        /// <returns>
        /// An <c>OptionValue</c> enum that specifies the option as either
        /// <c>True</c>, <c>False</c>, <c>NotSupported</c>, or <c>Unknown</c>.
        /// </returns>
        public OptionValue GetOptionValue(string option) =>
            Options?.TryGetValue(option, out bool value) == true
                ? value
                    ? OptionValue.True
                    : OptionValue.False
                : AuthenticatorOptions.GetDefaultOptionValue(option);

        /// <summary>
        /// Determine if the given <c>extension</c> is listed in this
        /// <c>AuthenticatorInfo</c>.
        /// </summary>
        /// <remarks>
        /// Because the <see cref="Extensions"/> property can be null (this
        /// happens if a YubiKey does not specify any extensions), to check for
        /// any particular extension requires first checking for null. If it is
        /// not null, then it is necessary to check to see if that extension is
        /// listed.
        /// <para>
        /// This method offers a convenient way to determine if an extension is
        /// listed. This method will determine if <c>Extensions</c> is null. If
        /// it is null, it will return <c>false</c>. If not, it will check to see
        /// if the given value is listed. If so, return <c>true</c>, otherwise
        /// return <c>false</c>.
        /// </para>
        /// </remarks>
        public bool IsExtensionSupported(string extension) => Extensions?.Contains(extension) == true;
    }
}
