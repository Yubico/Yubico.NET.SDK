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

        private readonly List<string> _versions = new List<string>();
        private readonly byte[] _aaguid = Array.Empty<byte>();

        /// <summary>
        /// List of version strings of CTAP supported by the authenticator.
        /// This is a REQUIRED value.
        /// </summary>
        public IReadOnlyList<string> Versions => _versions;

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
        /// The list of supported algorithms for credential generation. Each
        /// entry in the list is an algorithm specified in the COSE algorithm
        /// identifier list and a string specifying the type of credential.
        /// Currently the standard has only one type defined, "public-key".
        /// This is an OPTIONAL value and the <c>Count</c> can be zero.
        /// </summary>
        public IReadOnlyDictionary<CoseAlgorithmIdentifier, string>? Algorithms { get; private set; }

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
        /// given Cbor encoding.
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
            Aaguid = new ReadOnlyMemory<byte>(_aaguid);

            try
            {
                var cbor = new CborReader(cborEncoding, CborConformanceMode.Ctap2Canonical);

                int? entries = cbor.ReadStartMap();
                int count = entries ?? 0;

                while (count > 0)
                {
                    int mapKey = (int)cbor.ReadUInt32();

                    switch (mapKey)
                    {
                        case KeyVersions:
                            _ = CborHelpers.ReadStringArray(cbor, _versions);
                            break;

                        case KeyExtensions:
                            Extensions = CborHelpers.ReadStringArray(cbor, null);
                            break;

                        case KeyAaguid:
                            _aaguid = cbor.ReadByteString();
                            Aaguid = new ReadOnlyMemory<byte>(_aaguid);
                            break;

                        case KeyOptions:
                            Options = ReadOptionsMap(cbor);
                            break;

                        case KeyMaxMsgSize:
                            MaximumMessageSize = (int)cbor.ReadUInt32();
                            break;

                        case KeyPinUvAuthProtocols:
                            PinUvAuthProtocols = ReadProtocolsArray(cbor);
                            break;

                        case KeyMaxCredentialCountInList:
                            MaximumCredentialCountInList = (int)cbor.ReadUInt32();
                            break;

                        case KeyMaxCredentialIdLength:
                            MaximumCredentialIdLength = (int)cbor.ReadUInt32();
                            break;

                        case KeyTransports:
                            Transports = CborHelpers.ReadStringArray(cbor, null);
                            break;

                        case KeyAlgorithms:
                            Algorithms = ReadAlgorithmsMapArray(cbor);
                            break;

                        case KeyMaxSerializedLargeBlobArray:
                            MaximumSerializedLargeBlobArray = (int)cbor.ReadUInt32();
                            break;

                        case KeyForcePinChange:
                            ForcePinChange = cbor.ReadBoolean();
                            break;

                        case KeyMinPinLength:
                            MinimumPinLength = (int)cbor.ReadUInt32();
                            break;

                        case KeyFirmwareVersion:
                            FirmwareVersion = (int)cbor.ReadUInt32();
                            break;

                        case KeyMaxCredBlobLength:
                            MaximumCredentialBlobLength = (int)cbor.ReadUInt32();
                            break;

                        case KeyMaxRpidsForSetMinPinLength:
                            MaximumRpidsForSetMinPinLength = (int)cbor.ReadUInt32();
                            break;

                        case KeyPreferredPlatformUvAttempts:
                            PreferredPlatformUvAttempts = (int)cbor.ReadUInt32();
                            break;

                        case KeyUvModality:
                            UvModality = (int)cbor.ReadUInt32();
                            break;

                        case KeyCertifications:
                            Certifications = ReadCertificationsMap(cbor);
                            break;

                        case KeyRemainingDiscoverableCredentials:
                            RemainingDiscoverableCredentials = (int)cbor.ReadUInt32();
                            break;

                        default:
                            throw new ArgumentException(
                                string.Format(
                                    CultureInfo.CurrentCulture,
                                    ExceptionMessages.InvalidFido2Info));
                    }

                    count--;
                }

                cbor.ReadEndMap();
            }
            catch (CborContentException cborException)
            {
                throw new Ctap2DataException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidFido2Info),
                    cborException);
            }

            // There are two elements that must be present. If not, this is a
            // Malformed response.
            if ((Versions.Count == 0) || (Aaguid.Length != AuthenticatorInfo.AaguidLength))
            {
                throw new MalformedYubiKeyResponseException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.Ctap2MissingRequiredField));
            }
        }

        // Read a map of (string, bool) key/value pairs, placing them into a new
        // Dictionary. Return the new Dictionary.
        // We're expecting to find a map(string, bool).
        private static Dictionary<string, bool>? ReadOptionsMap(CborReader cbor)
        {
            int? entries = cbor.ReadStartMap();
            int count = entries ?? 0;

            var destination = new Dictionary<string, bool>(count);

            for (int index = 0; index < count; index++)
            {
                string mapKey = cbor.ReadTextString();
                bool isSupported = cbor.ReadBoolean();

                destination.Add(mapKey, isSupported);
            }

            cbor.ReadEndMap();

            return destination;
        }

        // Read an array of UInts, returning them (converted to the enum) in a
        // new List.
        private static List<PinUvAuthProtocol> ReadProtocolsArray(CborReader cbor)
        {
            int? entries = cbor.ReadStartArray();
            int count = entries ?? 0;

            var destination = new List<PinUvAuthProtocol>(count);

            for (int index = 0; index < count; index++)
            {
                destination.Add((PinUvAuthProtocol)cbor.ReadUInt32());
            }

            cbor.ReadEndArray();

            return destination;
        }

        // We're expecting to find an
        //   array of
        //     [map(negative int, string), map(string, string)]
        // The first map is a key/value pair, where we only want the value. Call
        // this value-1. The second map is a key/value pair, where we only want
        // the value. Call this value-2.
        // We're going to create a dictionary of key/value pairs, where value-1
        // is the key and value-2 is the value of this new key/value pair.
        // If the destination is not null, set it with the key/value pairs and
        // return it. If it is null, create a new Dictionary, set it, and return
        // it.
        private static Dictionary<CoseAlgorithmIdentifier, string>? ReadAlgorithmsMapArray(CborReader cbor)
        {
            int? entries = cbor.ReadStartArray();
            int count = entries ?? 0;

            var destination = new Dictionary<CoseAlgorithmIdentifier, string>(count);

            for (int index = 0; index < count; index++)
            {
                entries = cbor.ReadStartMap();
                int mapCount = entries ?? 0;

                // Init algorithm to 0. That is not a valid number for an
                // algorithm, so if, after reading, this is not 0, we know there
                // was an error.
                int algorithm = 0;
                string credentialType = "";

                // At this point there should be two maps:
                //   string, negative integer,
                //   string, string
                // in either order. If not, error.
                for (int indexM = 0; indexM < mapCount; indexM++)
                {
                    string mapKey = cbor.ReadTextString();

                    switch (mapKey)
                    {
                        default:
                            throw new Ctap2DataException(
                                string.Format(
                                    CultureInfo.CurrentCulture,
                                    ExceptionMessages.InvalidFido2Info));

                        case "alg":
                            algorithm = cbor.ReadInt32();
                            break;

                        case "type":
                           credentialType = cbor.ReadTextString();
                           break;
                    }
                }

                if ((mapCount != 2) || (algorithm == 0) || (!credentialType.Equals("public-key", StringComparison.Ordinal)))
                {
                    throw new Ctap2DataException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.InvalidFido2Info));
                }

                destination.Add((CoseAlgorithmIdentifier)algorithm, credentialType);

                cbor.ReadEndMap();
            }

            cbor.ReadEndArray();

            return destination;
        }

        // There are an indefinite number of certifications. Each one is a
        // key/value pair of string/int.
        // Read them and build the Dictionary. If destination is not null, set it
        // and return it. If it is null, create a new Dictionary, then set it and
        // return it.
        private static Dictionary<string, int>? ReadCertificationsMap(CborReader cbor)
        {
            int? entries = cbor.ReadStartMap();
            int count = entries ?? 0;

            var destination = new Dictionary<string, int>(count);

            for (int index = 0; index < count; index++)
            {
                string certifier = cbor.ReadTextString();
                int certLevel = (int)cbor.ReadUInt32();

                destination.Add(certifier, certLevel);
            }

            cbor.ReadEndMap();

            return destination;
        }
    }
}
