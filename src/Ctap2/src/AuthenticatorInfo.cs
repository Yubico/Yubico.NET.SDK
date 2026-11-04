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
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Ctap2;

/// <summary>
/// Represents authenticator information returned by the authenticatorGetInfo command.
/// </summary>
/// <remarks>
/// <para>
/// A GetInfo response is a snapshot, not SDK enforcement or permission to use a feature. Some option
/// values describe configured state. Query again after state changes, outside any continuation sequence.
/// Absent lists and maps become empty; optional numeric, Boolean and text fields remain null.
/// A missing required AAGUID also becomes empty. The encrypted metadata properties expose absent
/// fields as present empty memory. Use RawData to inspect encoded presence rather than these fallbacks.
/// </para>
/// <para>
/// See: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#authenticatorGetInfo
/// CTAP2.3 fields: https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html#authenticatorGetInfo
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
    /// Gets the complete original CBOR-encoded authenticatorGetInfo response.
    /// </summary>
    /// <remarks>
    /// This is populated by <see cref="Decode(ReadOnlyMemory{byte})"/> and preserves fields that
    /// are valid but not modeled by this SDK version. It is empty on caller-constructed instances.
    /// </remarks>
    public ReadOnlyMemory<byte> RawData { get; private init; }

    /// <summary>
    /// Gets the CTAP versions supported by the authenticator.
    /// </summary>
    /// <remarks>
    /// Common values: "FIDO_2_0", "FIDO_2_1", "FIDO_2_1_PRE", "FIDO_2_3", "U2F_V2".
    /// Advertisement does not establish complete SDK implementation of that revision.
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
    /// The protocol requires a 16-byte identifier for the authenticator type, not an individual
    /// credential. This decoder does not enforce field presence or length; a missing field becomes empty.
    /// </remarks>
    public ReadOnlyMemory<byte> Aaguid { get; init; }

    /// <summary>
    /// Gets reported capability and configured-state option values.
    /// </summary>
    /// <remarks>
    /// Common keys include "rk", "up", "plat", "clientPin", "uv", "credMgmt", "authnrCfg" and
    /// "bioEnroll". For clientPin, true means capable with a PIN set, false means capable without
    /// a PIN, and absence means not capable. For uv, true means built-in verification is configured,
    /// false means capable but unconfigured, and absence means unsupported. A false bioEnroll value
    /// still indicates enrollment capability, with no enrollment provisioned. Interpret each key's
    /// protocol definition rather than treating every false or absent value as the same condition.
    /// </remarks>
    public IReadOnlyDictionary<string, bool> Options { get; init; } = ReadOnlyDictionary<string, bool>.Empty;

    /// <summary>
    /// Gets the maximum message size in bytes.
    /// </summary>
    /// <remarks>Null preserves absence; the CTAP2.1 protocol default is 1024 bytes. This model
    /// does not enforce the limit on requests, and the HID framing limit is a separate constraint.</remarks>
    public int? MaxMsgSize { get; init; }

    /// <summary>
    /// Gets advertised PIN/UV auth protocol versions in decreasing authenticator preference.
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
    /// <remarks>Advertised names are not proof of an exposed or reachable host connection.
    /// Unknown names are preserved; callers must tolerate them.</remarks>
    public IReadOnlyList<string> Transports { get; init; } = [];

    /// <summary>
    /// Gets advertised credential-creation algorithms in authenticator preference order.
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
    /// Gets the current minimum PIN length in Unicode code points, when reported.
    /// </summary>
    public int? MinPinLength { get; init; }

    /// <summary>
    /// Gets the reported firmware value interpreted using YubiKey's packed major/minor/patch layout.
    /// </summary>
    /// <remarks>CTAP's unsigned firmwareVersion field does not standardize this layout. This
    /// property is not a Management firmware lookup or an update to the session firmware snapshot.</remarks>
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
    /// Gets the estimated number of additional discoverable credentials, when reported.
    /// </summary>
    /// <remarks>The estimate uses credential-size assumptions; it is not an exact free-slot count.</remarks>
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
    /// <remarks>In CTAP2.3, true means supported and enabled (touch for at least five seconds),
    /// false means supported but disabled, and absence means unsupported (represented here by null).
    /// This is separate from reset's
    /// power-up-to-command-receipt window; see <see cref="ICtap2Session.ResetAsync"/>.</remarks>
    public bool? LongTouchForReset { get; init; }

    /// <summary>
    /// Gets the optional encrypted device identifier defined by CTAP2.3.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The representation is IV followed by ciphertext for a 128-bit device identifier, not a
    /// credential ID or a means to select a credential. It can be decrypted using the corresponding
    /// persistent PIN/UV auth token with the
    /// <see cref="Crypto.EncryptedMetadataDecryptor.DecryptIdentifier"/> method.
    /// </para>
    /// <para>
    /// Field availability depends on product, firmware and persistent-token state; an ordinary
    /// permission-scoped token is not interchangeable. Decode reads the bytes without validating
    /// encrypted length or decryptability.
    /// When this field is absent, Decode produces a present zero-length memory value, not null.
    /// HasValue therefore does not establish that the authenticator returned the field or supports it;
    /// inspect RawData for encoded presence.
    /// </para>
    /// </remarks>
    public ReadOnlyMemory<byte>? EncIdentifier { get; init; }

    /// <summary>
    /// Gets the transports available for authenticator reset (CTAP 2.3).
    /// </summary>
    /// <remarks>
    /// This field indicates which transport methods can be used to reset the authenticator.
    /// Common values include "usb", "nfc", and "ble".
    /// Absence becomes an empty list in this model, not a conclusion that reset is unsupported.
    /// Unknown transport names are retained. This list does not arrange the reset timing or presence
    /// ceremony; see <see cref="ICtap2Session.ResetAsync"/>.
    /// </remarks>
    public IReadOnlyList<string> TransportsForReset { get; init; } = [];

    /// <summary>
    /// Gets whether PIN complexity policy is enforced (CTAP 2.3).
    /// </summary>
    /// <remarks>
    /// In CTAP2.3, true means supported and enabled; false means supported but disabled; absence
    /// means unsupported (represented here by null). <see cref="PinComplexityPolicyUrl"/> can
    /// explain the policy to the user but is optional.
    /// </remarks>
    public bool? PinComplexityPolicy { get; init; }

    /// <summary>
    /// Gets the URL describing the PIN complexity policy (CTAP 2.3).
    /// </summary>
    /// <remarks>
    /// The protocol field is an optional UTF-8 byte string, decoded here as text. It can inform the
    /// user about the policy; the SDK does not fetch the URL or validate the described requirements.
    /// </remarks>
    public string? PinComplexityPolicyUrl { get; init; }

    /// <summary>
    /// Gets the maximum PIN length supported by the authenticator (CTAP 2.3).
    /// </summary>
    /// <remarks>
    /// This value indicates the maximum number of Unicode code points allowed in a PIN.
    /// Complements <see cref="MinPinLength"/>.
    /// Null preserves absence; with ClientPIN support the CTAP2.3 effective default is 63 code points.
    /// The UTF-8 byte-length restriction remains separate; this value is not complete PIN validation.
    /// </remarks>
    public int? MaxPinLength { get; init; }

    /// <summary>
    /// Gets optional encrypted credential-store state defined by CTAP2.3.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is IV followed by ciphertext for a 128-bit credential-store state. It can be decrypted
    /// using the corresponding persistent PIN/UV auth token with the
    /// <see cref="Crypto.EncryptedMetadataDecryptor.DecryptCredStoreState"/> method.
    /// </para>
    /// <para>
    /// Availability depends on product, firmware and persistent-token state. Decode retains the
    /// byte string without validating encrypted length or proving decryptability.
    /// When this field is absent, Decode produces a present zero-length memory value, not null.
    /// HasValue therefore does not establish that the authenticator returned the field or supports it;
    /// inspect RawData for encoded presence.
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
    /// <remarks>The input is borrowed and left unchanged; the result owns an independent complete
    /// RawData copy and separately materialized fields. Unknown fields are skipped at the typed
    /// surface and preserved in RawData. Absent EncIdentifier and EncCredStoreState fields become
    /// present empty memory values; use RawData to check encoded presence, not HasValue alone.
    /// This parser does not enforce required Versions/AAGUID
    /// presence, AAGUID length or all protocol integer constraints. It does not check for unread
    /// bytes after the map: ReadEndMap and the default single-root reader setting do not establish
    /// complete input consumption. A decoded result is not a conformance verdict.</remarks>
    /// <param name="data">The CBOR-encoded response data.</param>
    /// <returns>The decoded AuthenticatorInfo.</returns>
    public static AuthenticatorInfo Decode(ReadOnlyMemory<byte> data)
    {
        byte[] rawCbor = data.ToArray();
        var reader = new CborReader(rawCbor, CborConformanceMode.Ctap2Canonical);
        return Parse(reader, rawCbor);
    }

    /// <summary>
    /// Parses an AuthenticatorInfo from a CBOR reader.
    /// </summary>
    private static AuthenticatorInfo Parse(CborReader reader, ReadOnlyMemory<byte> rawCbor)
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
            RawData = rawCbor,
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
            result[key] = reader.ReadBoolean();
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
        // Interpret the YubiKey packed layout: major << 16 | minor << 8 | patch.
        // CTAP itself specifies an unsigned model version, not these component bytes.
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
    /// Gets the credential type; defaults to "public-key" but can retain other supplied values.
    /// </summary>
    public string Type { get; init; } = TypePublicKey;

    /// <summary>
    /// Gets the COSE algorithm identifier.
    /// </summary>
    /// <remarks>An identifier is not device support, mathematical key validation or an RP's
    /// approved-algorithm policy. The GetInfo parser defaults a missing algorithm to None.</remarks>
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
    public static PublicKeyCredentialParameters CreateES256() => new(CoseAlgorithmIdentifier.Es256);

    /// <summary>
    /// Creates RS256 credential parameters.
    /// </summary>
    public static PublicKeyCredentialParameters CreateRS256() => new(CoseAlgorithmIdentifier.Rs256);

    /// <summary>
    /// Creates EdDSA credential parameters.
    /// </summary>
    public static PublicKeyCredentialParameters CreateEdDSA() => new(CoseAlgorithmIdentifier.EdDsa);

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