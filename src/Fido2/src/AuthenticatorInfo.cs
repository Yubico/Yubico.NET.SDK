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

using System.Collections.ObjectModel;
using System.Formats.Cbor;
using Yubico.YubiKit.Core.Cryptography.Cose;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Fido2;

/// <summary>
/// Represents authenticator information returned by the authenticatorGetInfo command.
/// </summary>
/// <remarks>
/// <para>
/// See: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#authenticatorGetInfo
/// </para>
/// </remarks>
public sealed class AuthenticatorInfo
{
    // CBOR map keys for GetInfo response
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
    private const int KeyEncCredStoreState = 0x1E;
    private const int KeyAuthenticatorConfigCommands = 0x1F;
    
    /// <summary>
    /// Gets the CTAP versions supported by the authenticator.
    /// </summary>
    /// <remarks>
    /// Common values: "FIDO_2_0", "FIDO_2_1", "FIDO_2_1_PRE", "U2F_V2"
    /// </remarks>
    public IReadOnlyList<string> Versions { get; init; } = [];
    
    /// <summary>
    /// Gets the extensions supported by the authenticator.
    /// </summary>
    public IReadOnlyList<string> Extensions { get; init; } = [];
    
    /// <summary>
    /// Gets the AAGUID (Authenticator Attestation GUID).
    /// </summary>
    /// <remarks>
    /// This is a 128-bit identifier that identifies the type of authenticator.
    /// </remarks>
    public ReadOnlyMemory<byte> Aaguid { get; init; }
    
    /// <summary>
    /// Gets the options supported by the authenticator.
    /// </summary>
    /// <remarks>
    /// Common options: "rk" (resident key), "up" (user presence), "uv" (user verification),
    /// "plat" (platform device), "clientPin", "credentialMgmt", "bioEnroll", etc.
    /// </remarks>
    public IReadOnlyDictionary<string, bool> Options { get; init; } = ReadOnlyDictionary<string, bool>.Empty;
    
    /// <summary>
    /// Gets the maximum message size in bytes.
    /// </summary>
    public int? MaxMsgSize { get; init; }
    
    /// <summary>
    /// Gets the PIN/UV auth protocol versions supported.
    /// </summary>
    public IReadOnlyList<int> PinUvAuthProtocols { get; init; } = [];
    
    /// <summary>
    /// Gets the maximum number of credentials in the allow/exclude list.
    /// </summary>
    public int? MaxCredentialCountInList { get; init; }
    
    /// <summary>
    /// Gets the maximum credential ID length in bytes.
    /// </summary>
    public int? MaxCredentialIdLength { get; init; }
    
    /// <summary>
    /// Gets the transports supported by the authenticator.
    /// </summary>
    public IReadOnlyList<string> Transports { get; init; } = [];
    
    /// <summary>
    /// Gets the algorithms supported for credential creation.
    /// </summary>
    public IReadOnlyList<PublicKeyCredentialParameters> Algorithms { get; init; } = [];
    
    /// <summary>
    /// Gets the maximum size of the serialized large blob array.
    /// </summary>
    public int? MaxSerializedLargeBlobArray { get; init; }
    
    /// <summary>
    /// Gets whether the authenticator requires PIN change.
    /// </summary>
    public bool? ForcePinChange { get; init; }
    
    /// <summary>
    /// Gets the minimum PIN length required.
    /// </summary>
    public int? MinPinLength { get; init; }
    
    /// <summary>
    /// Gets the firmware version as reported by the authenticator.
    /// </summary>
    public FirmwareVersion? FirmwareVersion { get; init; }
    
    /// <summary>
    /// Gets the maximum credBlob length in bytes.
    /// </summary>
    public int? MaxCredBlobLength { get; init; }
    
    /// <summary>
    /// Gets the maximum number of RP IDs for setMinPINLength.
    /// </summary>
    public int? MaxRpidsForSetMinPinLength { get; init; }
    
    /// <summary>
    /// Gets the preferred number of platform UV attempts.
    /// </summary>
    public int? PreferredPlatformUvAttempts { get; init; }
    
    /// <summary>
    /// Gets the UV modality flags.
    /// </summary>
    public int? UvModality { get; init; }
    
    /// <summary>
    /// Gets the certifications held by the authenticator.
    /// </summary>
    public IReadOnlyDictionary<string, int> Certifications { get; init; } = ReadOnlyDictionary<string, int>.Empty;
    
    /// <summary>
    /// Gets the remaining discoverable credential slots.
    /// </summary>
    public int? RemainingDiscoverableCredentials { get; init; }
    
    /// <summary>
    /// Gets the vendor prototype config commands supported.
    /// </summary>
    public IReadOnlyList<int> VendorPrototypeConfigCommands { get; init; } = [];
    
    /// <summary>
    /// Gets the attestation formats supported.
    /// </summary>
    public IReadOnlyList<string> AttestationFormats { get; init; } = [];
    
    /// <summary>
    /// Gets the UV count since last PIN entry.
    /// </summary>
    public int? UvCountSinceLastPinEntry { get; init; }
    
    /// <summary>
    /// Gets whether long touch is required for reset.
    /// </summary>
    public bool? LongTouchForReset { get; init; }
    
    /// <summary>
    /// Gets the encrypted credential identifier (YubiKey 5.7+).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This field contains an encrypted unique identifier for discoverable credentials.
    /// It can be decrypted using the PPUAT (Persistent PIN/UV Auth Token) with the
    /// <see cref="Crypto.EncryptedMetadataDecryptor.DecryptIdentifier"/> method.
    /// </para>
    /// <para>
    /// Available on YubiKey firmware 5.7+ when a PPUAT is active.
    /// </para>
    /// </remarks>
    public ReadOnlyMemory<byte>? EncIdentifier { get; init; }
    
    /// <summary>
    /// Gets the transports available for authenticator reset (CTAP 2.3).
    /// </summary>
    /// <remarks>
    /// This field indicates which transport methods can be used to reset the authenticator.
    /// Common values include "usb", "nfc", and "ble".
    /// </remarks>
    public IReadOnlyList<string> TransportsForReset { get; init; } = [];
    
    /// <summary>
    /// Gets whether PIN complexity policy is enforced (CTAP 2.3).
    /// </summary>
    /// <remarks>
    /// When true, the authenticator enforces a PIN complexity policy as defined
    /// by <see cref="PinComplexityPolicyUrl"/>.
    /// </remarks>
    public bool? PinComplexityPolicy { get; init; }
    
    /// <summary>
    /// Gets the URL describing the PIN complexity policy (CTAP 2.3).
    /// </summary>
    /// <remarks>
    /// This URL points to a human-readable description of the PIN complexity requirements
    /// enforced by the authenticator.
    /// </remarks>
    public string? PinComplexityPolicyUrl { get; init; }
    
    /// <summary>
    /// Gets the maximum PIN length supported by the authenticator (CTAP 2.3).
    /// </summary>
    /// <remarks>
    /// This value indicates the maximum number of Unicode code points allowed in a PIN.
    /// Complements <see cref="MinPinLength"/>.
    /// </remarks>
    public int? MaxPinLength { get; init; }
    
    /// <summary>
    /// Gets the encrypted credential store state (YubiKey 5.8+).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This field contains encrypted metadata about the credential storage state.
    /// It can be decrypted using the PPUAT (Persistent PIN/UV Auth Token) with the
    /// <see cref="Crypto.EncryptedMetadataDecryptor.DecryptCredStoreState"/> method.
    /// </para>
    /// <para>
    /// Available on YubiKey firmware 5.8+ when a PPUAT is active.
    /// </para>
    /// </remarks>
    public ReadOnlyMemory<byte>? EncCredStoreState { get; init; }
    
    /// <summary>
    /// Gets the authenticator config commands supported (CTAP 2.3).
    /// </summary>
    /// <remarks>
    /// This list contains the subcommand values supported by the
    /// authenticatorConfig command. Common values include:
    /// <list type="bullet">
    /// <item><description>0x01 - enableEnterpriseAttestation</description></item>
    /// <item><description>0x02 - toggleAlwaysUv</description></item>
    /// <item><description>0x03 - setMinPINLength</description></item>
    /// </list>
    /// </remarks>
    public IReadOnlyList<int> AuthenticatorConfigCommands { get; init; } = [];
    
    /// <summary>
    /// Decodes an AuthenticatorInfo from CBOR-encoded data.
    /// </summary>
    /// <param name="data">The CBOR-encoded response data.</param>
    /// <returns>The decoded AuthenticatorInfo.</returns>
    public static AuthenticatorInfo Decode(ReadOnlyMemory<byte> data)
    {
        var reader = new CborReader(data, CborConformanceMode.Ctap2Canonical);
        return Parse(reader);
    }
    
    /// <summary>
    /// Parses an AuthenticatorInfo from a CBOR reader.
    /// </summary>
    internal static AuthenticatorInfo Parse(CborReader reader)
    {
        var mapLength = reader.ReadStartMap();
        
        List<string>? versions = null;
        List<string>? extensions = null;
        byte[]? aaguid = null;
        Dictionary<string, bool>? options = null;
        int? maxMsgSize = null;
        List<int>? pinUvAuthProtocols = null;
        int? maxCredentialCountInList = null;
        int? maxCredentialIdLength = null;
        List<string>? transports = null;
        List<PublicKeyCredentialParameters>? algorithms = null;
        int? maxSerializedLargeBlobArray = null;
        bool? forcePinChange = null;
        int? minPinLength = null;
        FirmwareVersion? firmwareVersion = null;
        int? maxCredBlobLength = null;
        int? maxRpidsForSetMinPinLength = null;
        int? preferredPlatformUvAttempts = null;
        int? uvModality = null;
        Dictionary<string, int>? certifications = null;
        int? remainingDiscoverableCredentials = null;
        List<int>? vendorPrototypeConfigCommands = null;
        List<string>? attestationFormats = null;
        int? uvCountSinceLastPinEntry = null;
        bool? longTouchForReset = null;
        byte[]? encIdentifier = null;
        List<string>? transportsForReset = null;
        bool? pinComplexityPolicy = null;
        string? pinComplexityPolicyUrl = null;
        int? maxPinLength = null;
        byte[]? encCredStoreState = null;
        List<int>? authenticatorConfigCommands = null;
        
        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadInt32();
            
            switch (key)
            {
                case KeyVersions:
                    versions = ReadStringArray(reader);
                    break;
                    
                case KeyExtensions:
                    extensions = ReadStringArray(reader);
                    break;
                    
                case KeyAaguid:
                    aaguid = reader.ReadByteString();
                    break;
                    
                case KeyOptions:
                    options = ReadBoolMap(reader);
                    break;
                    
                case KeyMaxMsgSize:
                    maxMsgSize = reader.ReadInt32();
                    break;
                    
                case KeyPinUvAuthProtocols:
                    pinUvAuthProtocols = ReadIntArray(reader);
                    break;
                    
                case KeyMaxCredentialCountInList:
                    maxCredentialCountInList = reader.ReadInt32();
                    break;
                    
                case KeyMaxCredentialIdLength:
                    maxCredentialIdLength = reader.ReadInt32();
                    break;
                    
                case KeyTransports:
                    transports = ReadStringArray(reader);
                    break;
                    
                case KeyAlgorithms:
                    algorithms = ReadAlgorithms(reader);
                    break;
                    
                case KeyMaxSerializedLargeBlobArray:
                    maxSerializedLargeBlobArray = reader.ReadInt32();
                    break;
                    
                case KeyForcePinChange:
                    forcePinChange = reader.ReadBoolean();
                    break;
                    
                case KeyMinPinLength:
                    minPinLength = reader.ReadInt32();
                    break;
                    
                case KeyFirmwareVersion:
                    var fwValue = reader.ReadUInt64();
                    firmwareVersion = DecodeFirmwareVersion(fwValue);
                    break;
                    
                case KeyMaxCredBlobLength:
                    maxCredBlobLength = reader.ReadInt32();
                    break;
                    
                case KeyMaxRpidsForSetMinPinLength:
                    maxRpidsForSetMinPinLength = reader.ReadInt32();
                    break;
                    
                case KeyPreferredPlatformUvAttempts:
                    preferredPlatformUvAttempts = reader.ReadInt32();
                    break;
                    
                case KeyUvModality:
                    uvModality = reader.ReadInt32();
                    break;
                    
                case KeyCertifications:
                    certifications = ReadCertifications(reader);
                    break;
                    
                case KeyRemainingDiscoverableCredentials:
                    remainingDiscoverableCredentials = reader.ReadInt32();
                    break;
                    
                case KeyVendorPrototypeConfigCommands:
                    vendorPrototypeConfigCommands = ReadIntArray(reader);
                    break;
                    
                case KeyAttestationFormats:
                    attestationFormats = ReadStringArray(reader);
                    break;
                    
                case KeyUvCountSinceLastPinEntry:
                    uvCountSinceLastPinEntry = reader.ReadInt32();
                    break;
                    
                case KeyLongTouchForReset:
                    longTouchForReset = reader.ReadBoolean();
                    break;
                    
                case KeyEncIdentifier:
                    encIdentifier = reader.ReadByteString();
                    break;
                    
                case KeyTransportsForReset:
                    transportsForReset = ReadStringArray(reader);
                    break;
                    
                case KeyPinComplexityPolicy:
                    pinComplexityPolicy = reader.ReadBoolean();
                    break;
                    
                case KeyPinComplexityPolicyUrl:
                    // CBOR encodes as byte string (UTF-8), decode to string
                    var policyUrlBytes = reader.ReadByteString();
                    pinComplexityPolicyUrl = System.Text.Encoding.UTF8.GetString(policyUrlBytes);
                    break;
                    
                case KeyMaxPinLength:
                    maxPinLength = reader.ReadInt32();
                    break;
                    
                case KeyEncCredStoreState:
                    encCredStoreState = reader.ReadByteString();
                    break;
                    
                case KeyAuthenticatorConfigCommands:
                    authenticatorConfigCommands = ReadIntArray(reader);
                    break;
                    
                default:
                    reader.SkipValue();
                    break;
            }
        }
        
        reader.ReadEndMap();
        
        return new AuthenticatorInfo
        {
            Versions = versions ?? [],
            Extensions = extensions ?? [],
            Aaguid = aaguid ?? [],
            Options = options is not null
                ? new ReadOnlyDictionary<string, bool>(options)
                : ReadOnlyDictionary<string, bool>.Empty,
            MaxMsgSize = maxMsgSize,
            PinUvAuthProtocols = pinUvAuthProtocols ?? [],
            MaxCredentialCountInList = maxCredentialCountInList,
            MaxCredentialIdLength = maxCredentialIdLength,
            Transports = transports ?? [],
            Algorithms = algorithms ?? [],
            MaxSerializedLargeBlobArray = maxSerializedLargeBlobArray,
            ForcePinChange = forcePinChange,
            MinPinLength = minPinLength,
            FirmwareVersion = firmwareVersion,
            MaxCredBlobLength = maxCredBlobLength,
            MaxRpidsForSetMinPinLength = maxRpidsForSetMinPinLength,
            PreferredPlatformUvAttempts = preferredPlatformUvAttempts,
            UvModality = uvModality,
            Certifications = certifications is not null
                ? new ReadOnlyDictionary<string, int>(certifications)
                : ReadOnlyDictionary<string, int>.Empty,
            RemainingDiscoverableCredentials = remainingDiscoverableCredentials,
            VendorPrototypeConfigCommands = vendorPrototypeConfigCommands ?? [],
            AttestationFormats = attestationFormats ?? [],
            UvCountSinceLastPinEntry = uvCountSinceLastPinEntry,
            LongTouchForReset = longTouchForReset,
            EncIdentifier = encIdentifier,
            TransportsForReset = transportsForReset ?? [],
            PinComplexityPolicy = pinComplexityPolicy,
            PinComplexityPolicyUrl = pinComplexityPolicyUrl,
            MaxPinLength = maxPinLength,
            EncCredStoreState = encCredStoreState,
            AuthenticatorConfigCommands = authenticatorConfigCommands ?? []
        };
    }
    
    private static List<string> ReadStringArray(CborReader reader)
    {
        var length = reader.ReadStartArray() ?? 0;
        var result = new List<string>((int)length);
        
        for (var i = 0; i < length; i++)
        {
            result.Add(reader.ReadTextString());
        }
        
        reader.ReadEndArray();
        return result;
    }
    
    private static List<int> ReadIntArray(CborReader reader)
    {
        var length = reader.ReadStartArray() ?? 0;
        var result = new List<int>((int)length);
        
        for (var i = 0; i < length; i++)
        {
            result.Add(reader.ReadInt32());
        }
        
        reader.ReadEndArray();
        return result;
    }
    
    private static Dictionary<string, bool> ReadBoolMap(CborReader reader)
    {
        var length = reader.ReadStartMap() ?? 0;
        var result = new Dictionary<string, bool>((int)length);
        
        for (var i = 0; i < length; i++)
        {
            var key = reader.ReadTextString();
            var value = reader.ReadBoolean();
            result[key] = value;
        }
        
        reader.ReadEndMap();
        return result;
    }
    
    private static Dictionary<string, int> ReadCertifications(CborReader reader)
    {
        var length = reader.ReadStartMap() ?? 0;
        var result = new Dictionary<string, int>((int)length);
        
        for (var i = 0; i < length; i++)
        {
            var key = reader.ReadTextString();
            var value = reader.ReadInt32();
            result[key] = value;
        }
        
        reader.ReadEndMap();
        return result;
    }
    
    private static List<PublicKeyCredentialParameters> ReadAlgorithms(CborReader reader)
    {
        var length = reader.ReadStartArray() ?? 0;
        var result = new List<PublicKeyCredentialParameters>((int)length);
        
        for (var i = 0; i < length; i++)
        {
            result.Add(PublicKeyCredentialParameters.Parse(reader));
        }
        
        reader.ReadEndArray();
        return result;
    }
    
    private static FirmwareVersion DecodeFirmwareVersion(ulong value)
    {
        // Firmware version is encoded as major.minor.patch in the upper bytes
        // YubiKey encodes as: major << 16 | minor << 8 | patch
        var major = (byte)((value >> 16) & 0xFF);
        var minor = (byte)((value >> 8) & 0xFF);
        var patch = (byte)(value & 0xFF);
        return new FirmwareVersion(major, minor, patch);
    }
}

/// <summary>
/// Represents a public key credential algorithm parameter.
/// </summary>
public sealed class PublicKeyCredentialParameters
{
    private const string TypePublicKey = "public-key";
    
    /// <summary>
    /// Gets the credential type (always "public-key").
    /// </summary>
    public string Type { get; init; } = TypePublicKey;
    
    /// <summary>
    /// Gets the COSE algorithm identifier.
    /// </summary>
    public CoseAlgorithmIdentifier Algorithm { get; init; }
    
    /// <summary>
    /// Creates a new credential parameters instance.
    /// </summary>
    public PublicKeyCredentialParameters()
    {
    }
    
    /// <summary>
    /// Creates a new credential parameters instance with the specified algorithm.
    /// </summary>
    /// <param name="algorithm">The COSE algorithm identifier.</param>
    public PublicKeyCredentialParameters(CoseAlgorithmIdentifier algorithm)
    {
        Algorithm = algorithm;
    }
    
    /// <summary>
    /// Creates ES256 credential parameters.
    /// </summary>
    public static PublicKeyCredentialParameters CreateES256() => new(CoseAlgorithmIdentifier.ES256);
    
    /// <summary>
    /// Creates RS256 credential parameters.
    /// </summary>
    public static PublicKeyCredentialParameters CreateRS256() => new(CoseAlgorithmIdentifier.RS256);
    
    /// <summary>
    /// Creates EdDSA credential parameters.
    /// </summary>
    public static PublicKeyCredentialParameters CreateEdDSA() => new(CoseAlgorithmIdentifier.EdDSA);
    
    /// <summary>
    /// Encodes this parameters object as CBOR.
    /// </summary>
    /// <param name="writer">The CBOR writer.</param>
    public void Encode(CborWriter writer)
    {
        writer.WriteStartMap(2);
        
        writer.WriteTextString("type");
        writer.WriteTextString(Type);
        
        writer.WriteTextString("alg");
        writer.WriteInt32((int)Algorithm);
        
        writer.WriteEndMap();
    }
    
    internal static PublicKeyCredentialParameters Parse(CborReader reader)
    {
        var mapLength = reader.ReadStartMap();
        
        string type = TypePublicKey;
        var algorithm = CoseAlgorithmIdentifier.None;
        
        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadTextString();
            
            switch (key)
            {
                case "type":
                    type = reader.ReadTextString();
                    break;
                case "alg":
                    algorithm = (CoseAlgorithmIdentifier)reader.ReadInt32();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }
        
        reader.ReadEndMap();
        
        return new PublicKeyCredentialParameters
        {
            Type = type,
            Algorithm = algorithm
        };
    }
}
