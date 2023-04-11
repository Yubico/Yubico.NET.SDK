// Copyright 2022 Yubico AB
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
using System.Linq;
using System.Formats.Cbor;
using System.Globalization;
using System.Collections.Generic;
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
        private const int KeyVersions = 1;
        private const int KeyExtensions = 2;
        private const int KeyAaguid = 3;
        private const int KeyOptions = 4;
        private const int KeyMaxMsgSize = 5;
        private const int KeyPinUvAuthProtocols = 6;
        private const int KeyMaxCredentialCountInList = 7;
        private const int KeyMaxCredentialIdLength = 8;
        private const int KeyTransports = 9;
        private const int KeyAlgorithms = 10;
        private const int KeyMaxSerializedLargeBlobArray = 11;
        private const int KeyForcePinChange = 12;
        private const int KeyMinPinLength = 13;
        private const int KeyFirmwareVersion = 14;
        private const int KeyMaxCredBlobLength = 15;
        private const int KeyMaxRpidsForSetMinPinLength = 16;
        private const int KeyPreferredPlatformUvAttempts = 17;
        private const int KeyUvModality = 18;
        private const int KeyCertifications = 19;
        private const int KeyRemainingDiscoverableCredentials = 20;

        // There is one more Key
        //   private const int KeyVendorPrototypeConfigCommands = 21;
        // however, currently this is not supported by the YubiKey.

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

        private readonly byte[] _aaguid;

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
        public IReadOnlyList<string> Versions { get; private set; }

        /// <summary>
        /// List of extension strings of CTAP supported by the authenticator.
        /// This propery is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null.
        /// </summary>
        public IReadOnlyList<string>? Extensions { get; private set; }

        /// <summary>
        /// The AAGUID, unique to the authenticator and model.
        /// This is a REQUIRED value.
        /// </summary>
        public ReadOnlyMemory<byte> Aaguid { get; private set; }

        /// <summary>
        /// The list of supported options. Each entry in the list is a string
        /// describing the option and a boolean, indicating whether it is
        /// supported (<c>true</c>) or not (<c>false</c>).
        /// This propery is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null.
        /// </summary>
        public IReadOnlyDictionary<string, bool>? Options {get; private set; }

        /// <summary>
        /// The maximum size, in bytes, of a message sent to the YubiKey.
        /// This propery is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null. The standard specifies a default of 1024
        /// (see the field <see cref="DefaultMaximumMessageSize"/>).
        /// </summary>
        public int? MaximumMessageSize { get; private set; }

        /// <summary>
        /// List of PIN/UV Auth Protocols the YubiKey supports. They are given in
        /// the order from most to least preferred.
        /// This propery is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null.
        /// </summary>
        public IReadOnlyList<PinUvAuthProtocol>? PinUvAuthProtocols { get; private set; }

        /// <summary>
        /// The maximum number of credentials in the CredentialID list. Note that
        /// this is not the maximum number of credentials on a YubiKey, but the
        /// maximum number of credentials represented in a CredentialID list.
        /// This propery is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null.
        /// </summary>
        public int? MaximumCredentialCountInList { get; private set; }

        /// <summary>
        /// The maximum length of a CredentialID.
        /// This propery is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null.
        /// </summary>
        public int? MaximumCredentialIdLength { get; private set; }

        /// <summary>
        /// List of transport strings of CTAP supported by the authenticator.
        /// This propery is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null.
        /// </summary>
        public IReadOnlyList<string>? Transports { get; private set; }

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
        public IReadOnlyList<Tuple<string, CoseAlgorithmIdentifier>>? Algorithms { get; private set; }

        /// <summary>
        /// The maximum size, in bytes, of the serialized large-blob array that
        /// this YubiKey can store. If the authenticatorLargeBlobs command is not
        /// supported, this will be null. If it is supported, it will be a value
        /// greater than 1024.
        /// This propery is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null (the authenticatorLargeBlobs command is not supported).
        /// </summary>
        public int? MaximumSerializedLargeBlobArray { get; }

        /// <summary>
        /// If <c>true</c>, certain PIN commands will return errors until the PIN
        /// has been changed. If <c>false</c>, a PIN change is not necessary.
        /// This propery is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null.
        /// </summary>
        public bool? ForcePinChange { get; }

        /// <summary>
        /// The current minimum PIN length, in Unicode code points.
        /// This propery is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null. The standard specifies a default of 4
        /// (see the field <see cref="DefaultMinimumPinLength"/>).
        /// </summary>
        public int? MinimumPinLength { get; }

        /// <summary>
        /// The version of the firmware on the YubiKey. Note that this is an
        /// <c>int</c>, not an instance of the <c>FirmwareVersion</c> class. The
        /// standard specifies returning an int.
        /// This propery is OPTIONAL, and if the YubiKey provides no value, this
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
        /// This propery is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null.
        /// </summary>
        public int? MaximumCredentialBlobLength { get; }

        /// <summary>
        /// The maximum number of Relying Party IDs that the YubiKey can set via
        /// the setMinPINLength subcommand.
        /// This propery is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null.
        /// </summary>
        public int? MaximumRpidsForSetMinPinLength { get; }

        /// <summary>
        /// The number of attempts to authenticate the UV (e.g. fingerprint) that
        /// fail before using the PIN.
        /// This propery is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null.
        /// </summary>
        public int? PreferredPlatformUvAttempts { get; }

        /// <summary>
        /// A bit field indicating the user verification methods supported by the
        /// YubiKey. The meanings of the bits are specified in the FIDO standard,
        /// namely the Registry of Predefined Values, section 3.1.
        /// This propery is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null.
        /// </summary>
        public int? UvModality { get; }

        /// <summary>
        /// The list of certifications the YubiKey has obtained. Each
        /// certification is a string and number. The string is the name of the
        /// certification, and the number describes the level. See The FIDO
        /// standard for more information, specifically section 7.3 of CTAP.
        /// This propery is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null.
        /// </summary>
        public IReadOnlyDictionary<string, int>? Certifications { get; private set; }

        /// <summary>
        /// The estimated number of additional discoverable credentials that can
        /// be stored.
        /// This propery is OPTIONAL, and if the YubiKey provides no value, this
        /// will be null.
        /// </summary>
        public int? RemainingDiscoverableCredentials { get; }

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
                _aaguid = cborMap.ReadByteString(KeyAaguid).ToArray();
                Aaguid = new ReadOnlyMemory<byte>(_aaguid);
                if (cborMap.Contains(KeyExtensions))
                {
                    Extensions = (IReadOnlyList<string>)cborMap.ReadArray<string>(KeyExtensions);
                }
                if (cborMap.Contains(KeyOptions))
                {
                    CborMap<string> optionsMap = cborMap.ReadMap<string>(KeyOptions);
                    Options = optionsMap.AsDictionary<bool>();
                }
                MaximumMessageSize = (int?)cborMap.ReadOptional<int>(KeyMaxMsgSize);
                if (cborMap.Contains(KeyPinUvAuthProtocols))
                {
                    IReadOnlyList<int> temp = cborMap.ReadArray<int>(KeyPinUvAuthProtocols);
                    var translator = new List<PinUvAuthProtocol>(temp.Count);
                    for (int index = 0; index < temp.Count; index++)
                    {
                        translator.Add((PinUvAuthProtocol)temp[index]);
                    }
                    PinUvAuthProtocols = translator;
                }
                MaximumCredentialCountInList = (int?)cborMap.ReadOptional<int>(KeyMaxCredentialCountInList);
                MaximumCredentialIdLength = (int?)cborMap.ReadOptional<int>(KeyMaxCredentialIdLength);
                if (cborMap.Contains(KeyTransports))
                {
                    Transports = (IReadOnlyList<string>)cborMap.ReadArray<string>(KeyTransports);
                }
                if (cborMap.Contains(KeyAlgorithms))
                {
                    ReadAlgorithms(cborMap);
                }
                MaximumSerializedLargeBlobArray = (int?)cborMap.ReadOptional<int>(KeyMaxSerializedLargeBlobArray);
                ForcePinChange = (bool?)cborMap.ReadOptional<bool>(KeyForcePinChange);
                MinimumPinLength = (int?)cborMap.ReadOptional<int>(KeyMinPinLength);
                FirmwareVersion = (int?)cborMap.ReadOptional<int>(KeyFirmwareVersion);
                MaximumCredentialBlobLength = (int?)cborMap.ReadOptional<int>(KeyMaxCredBlobLength);
                MaximumRpidsForSetMinPinLength = (int?)cborMap.ReadOptional<int>(KeyMaxRpidsForSetMinPinLength);
                PreferredPlatformUvAttempts = (int?)cborMap.ReadOptional<int>(KeyPreferredPlatformUvAttempts);
                UvModality = (int?)cborMap.ReadOptional<int>(KeyUvModality);
                if (cborMap.Contains(KeyCertifications))
                {
                    CborMap<string> certMap = cborMap.ReadMap<string>(KeyCertifications);
                    Certifications = certMap.AsDictionary<int>();
                }
                RemainingDiscoverableCredentials = (int?)cborMap.ReadOptional<int>(KeyRemainingDiscoverableCredentials);
            }
            catch (CborContentException cborException)
            {
                throw new Ctap2DataException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidFido2Info),
                    cborException);
            }
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
        /// If the option is unknown (not one of the standard-definde options),
        /// and it is not listed, this method will return "unknown".
        /// </para>
        /// </remarks>
        /// <returns>
        /// An <c>OptionValue</c> enum that specifies the option as either
        /// <c>True</c>, <c>False</c>, <c>NotSupported</c>, or <c>Unknown</c>.
        /// </returns>
        public OptionValue GetOptionValue(string option)
        {
            if (!(Options is null))
            {
                if (Options.ContainsKey(option))
                {
                    return Options[option] ? OptionValue.True : OptionValue.False;
                }
            }

            return AuthenticatorOptions.GetDefaultOptionValue(option);
        }

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
        public bool IsExtensionSupported(string extension)
        {
            if (!(Extensions is null))
            {
                if (Extensions.Contains(extension))
                {
                    return true;
                }
            }

            return false;
        }

        // We've checked, the KeyAlgorithms is in cborMap.
        // The value is an array.
        // Each entry in the array is a map.
        // Each map should be the same, two elements
        //   key/value     "alg"/<int>
        //   key/value     "type"/<string>
        // The return from ReadArray will be a List, the list of entries.
        // Because each entry is a map, the return from ReadArray will be the
        // type returned by the CborMap, which is the type returned by processing
        // a map, namely an IDictionary.
        // Look in this map to find the entry for "alg" and collect the value.
        // Look in this map to find the entry for "type" and collect the value.
        // Now build a Tuple out of the two values and add it to the Algorithms
        // list.
        private void ReadAlgorithms(CborMap<int> cborMap)
        {
            var algorithms = new List<Tuple<string, CoseAlgorithmIdentifier>>();

            IReadOnlyList<CborMap<string>> entries = cborMap.ReadArray<CborMap<string>>(KeyAlgorithms);
            for (int index = 0; index < entries.Count; index++)
            {
                string currentType = entries[index].ReadTextString(ParameterHelpers.TagType);
                var currentAlg = (CoseAlgorithmIdentifier)entries[index].ReadInt32(ParameterHelpers.TagAlg);

                algorithms.Add(new Tuple<string, CoseAlgorithmIdentifier>(currentType, currentAlg));
            }

            Algorithms = algorithms;
        }
    }
}
