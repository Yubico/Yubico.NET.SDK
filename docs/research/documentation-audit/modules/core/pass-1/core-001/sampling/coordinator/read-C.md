PROPOSED SAMPLE — no verdict: core-001:unit:5a12583307568a0a / C / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Credentials/ICredentialPrompt.cs:99–116
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    /// <summary>
    /// Requests a secret from the user or another source.
    /// </summary>
    /// <param name="context">Describes what is being requested and why.</param>
    /// <param name="cancellationToken">Token that the implementation must monitor for cancellation requests.</param>
    /// <returns>
    /// The secret bytes in a buffer whose <see cref="IMemoryOwner{T}.Memory"/> is
    /// sized exactly to the secret, or <c>null</c> if the user declined to supply one. Ownership
    /// transfers to the caller only when the returned task completes successfully with a non-null
    /// owner.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when cancellation is requested through <paramref name="cancellationToken"/>.
    /// </exception>
    ValueTask<IMemoryOwner<byte>?> RequestSecretAsync(
        CredentialPromptContext context,
        CancellationToken cancellationToken);


PROPOSED SAMPLE — no verdict: core-001:unit:8136013eac673552 / C / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Credentials/IUserPresencePrompt.cs:32–48
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    /// <summary>Notifies the application that an operation requires or may require user presence.</summary>
    /// <param name="context">
    ///     Describes the application, operation, scope, and basis for the notification, plus any application intent
    ///     supplied through <see cref="UserPresenceIntent.BeginScope" />.
    /// </param>
    /// <param name="cancellationToken">Token that the implementation must monitor for cancellation requests.</param>
    /// <remarks>
    ///     Implementations should present or update their user-presence indication and return without waiting for
    ///     the physical interaction itself. The device operation determines whether presence was supplied. The SDK
    ///     pairs this request with a resolution only after this callback completes successfully. If it throws, the
    ///     SDK performs any protocol-safe cancellation, drain, or reset that is required and propagates the exception
    ///     without calling <see cref="OnUserPresenceResolvedAsync" />.
    /// </remarks>
    ValueTask OnUserPresenceRequestedAsync(
        UserPresenceContext context,
        CancellationToken cancellationToken);


PROPOSED SAMPLE — no verdict: core-001:unit:a4915672d200fafe / C / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Credentials/UserPresenceApplications.cs:25–27
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    /// <summary>OpenPGP application.</summary>
    public const string OpenPgp = "OpenPGP";


PROPOSED SAMPLE — no verdict: core-001:unit:719384e829908b14 / C / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Credentials/UserPresenceOperations.cs:1–24
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
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

namespace Yubico.YubiKit.Core.Credentials;

/// <summary>Identifiers for the SDK operations that report <see cref="UserPresenceContext.Operation" />.</summary>
/// <remarks>
///     Operations are grouped by SDK operation surface. Each value is unique across all groups and has the form
///     <c>Group.Method</c>, where <c>Method</c> is the public SDK method name without <c>Async</c>, so a
///     prompt can switch on <see cref="UserPresenceContext.Operation" /> alone. This is an open set: future
///     releases may add values, so consumers should include a default branch.
///     The group can differ from the application identifier; CTAP2 operations report the FIDO2 application.
/// </remarks>



PROPOSED SAMPLE — no verdict: core-001:unit:2f3a453051016bf1 / C / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Credentials/UserPresenceOperations.cs:45–47
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        /// <summary>Private-key signing or decryption; the SDK method does not distinguish the two.</summary>
        public const string SignOrDecrypt = "Piv.SignOrDecrypt";


PROPOSED SAMPLE — no verdict: core-001:unit:720f2bf625934af1 / C / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Credentials/UserPresenceOperations.cs:76–79
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        /// <summary>OATH code calculation.</summary>
        public const string CalculateCode = "Oath.CalculateCode";


PROPOSED SAMPLE — no verdict: core-001:unit:76337fd3c2c48418 / C / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Cryptography/AlgorithmInfo.cs:100–102
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    /// <summary>OR alternatives: a key matches if any constraint matches all of its specified fields.</summary>
    public ImmutableList<KeyApplicability> Applicability { get; }


PROPOSED SAMPLE — no verdict: core-001:unit:c07b65f7049672fd / C / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Cryptography/AlgorithmInfo.cs:102–104
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    /// <summary>Operation OID, or null when none is modeled; may identify multiple parameterized definitions.</summary>
    public string? AlgorithmOid { get; }


PROPOSED SAMPLE — no verdict: core-001:unit:05d91bd48cc4ba9f / C / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Cryptography/AlgorithmInfo.cs:137–156
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Id);
        hash.Add(DisplayName);
        hash.Add(Operation);
        foreach (var constraint in Applicability) hash.Add(constraint);
        hash.Add(AlgorithmOid);
        hash.Add(CoseAlgorithmId);
        hash.Add(HashAlgorithm);
        hash.Add(SignaturePadding);
        hash.Add(MaskGenerationHashAlgorithm);
        hash.Add(SaltLengthBytes);
        hash.Add(CipherMode);
        hash.Add(Derivation);
        return hash.ToHashCode();
    }


PROPOSED SAMPLE — no verdict: core-001:unit:abc4e9861f35daf8 / C / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Cryptography/CmacPrimitives.cs:1–16
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0

using System.Security.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;

namespace Yubico.YubiKit.Core.Cryptography;

/// <summary>
/// Creates the default .NET CMAC primitive implementation.
/// </summary>



PROPOSED SAMPLE — no verdict: core-001:unit:f5ca60315dba1804 / C / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Cryptography/CryptographicDefinitions.Curves.cs:43–47
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        /// <summary>All modeled curves in declaration order.</summary>
        public static ImmutableList<CurveInfo> All { get; } =
            [P256, P384, P521, Curve25519, Edwards25519, Secp256k1, BrainpoolP256r1, BrainpoolP384r1, BrainpoolP512r1];


PROPOSED SAMPLE — no verdict: core-001:unit:cb619071acc9bba8 / C / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Cryptography/CryptographicDefinitions.Curves.cs:63–70
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        /// <summary>Looks up only a standard named-curve OID, never a key algorithm or operation OID.</summary>
        /// <param name="curveOid">The dotted-decimal named-curve OID.</param>
        /// <returns>The catalog's immutable curve instance.</returns>
        /// <exception cref="NotSupportedException">No modeled curve has that named-curve OID.</exception>
        public static CurveInfo GetByOid(string curveOid) => TryGetByOid(curveOid, out var curveInfo) ? curveInfo
            : throw new NotSupportedException($"Unsupported named-curve OID '{curveOid}'.");


PROPOSED SAMPLE — no verdict: core-001:unit:64d441b7b1bb1484 / C / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Cryptography/CryptographicDefinitions.Curves.cs:70–79
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        /// <summary>Tries to resolve a named-curve OID, never a key algorithm or operation OID.</summary>
        /// <param name="curveOid">A dotted-decimal curve OID, or null when no OID is available.</param>
        /// <param name="curveInfo">The immutable curve instance on success; null otherwise.</param>
        /// <returns>False for null or unmodeled OIDs.</returns>
        public static bool TryGetByOid(string? curveOid, [NotNullWhen(true)] out CurveInfo? curveInfo)
        {
            curveInfo = null;
            return curveOid is not null && ByOid.TryGetValue(curveOid, out curveInfo);
        }


PROPOSED SAMPLE — no verdict: core-001:unit:dceaa831d1742f95 / C / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Cryptography/CryptographicDefinitions.Keys.cs:44–46
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        /// <summary>RSA with a 4096-bit modulus.</summary>
        public static KeyInfo Rsa4096 { get; } = Rsa(KeyType.Rsa4096, 4096);


PROPOSED SAMPLE — no verdict: core-001:unit:0b78e34a16aba2d2 / C / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Cryptography/CryptographicDefinitions.Keys.cs:77–82
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        /// <summary>Tries to find immutable metadata for a concrete key identity.</summary>
        /// <param name="keyType">The concrete key identity.</param>
        /// <param name="keyInfo">The shared metadata instance on success; null on a miss.</param>
        /// <returns>False for None or an unknown identity, without throwing.</returns>
        public static bool TryGet(KeyType keyType, [NotNullWhen(true)] out KeyInfo? keyInfo) => ByKeyType.TryGetValue(keyType, out keyInfo);


PROPOSED SAMPLE — no verdict: core-001:unit:60b8bbc5afbaceb8 / C / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Cryptography/CurveInfo.cs:1–17
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
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

namespace Yubico.YubiKit.Core.Cryptography;

/// <summary>The mathematical form of an elliptic curve, not a signing algorithm.</summary>



PROPOSED SAMPLE — no verdict: core-001:unit:4848f6c1538b430a / C / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Cryptography/CurveInfo.cs:41–44
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>Stable, case-sensitive catalog identity, independent of display names and wire identifiers.</summary>
    public string Id { get; }


PROPOSED SAMPLE — no verdict: core-001:unit:361e6cdfab43c71d / C / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Cryptography/CurveInfo.cs:50–52
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    /// <summary>Bit length of the prime subgroup order, not the full curve group order.</summary>
    public int OrderSizeBits { get; }


PROPOSED SAMPLE — no verdict: core-001:unit:f160af6cad1f3e94 / C / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Cryptography/KeyTypeExtensions.cs:1–24
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
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

using Yubico.YubiKit.Core.Cryptography.Cose;

namespace Yubico.YubiKit.Core.Cryptography;

/// <summary>Queries shared metadata and structural classifications for concrete key identities.</summary>
/// <remarks>
/// These queries do not establish operation, codec, provider, hardware, slot or protocol-profile support.
/// Metadata/size/mapping lookups reject <see cref="KeyType.None"/> and unknown values; classification
/// predicates return false for them instead.
/// </remarks>



PROPOSED SAMPLE — no verdict: core-001:unit:d9b9e31e257e7796 / C / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Cryptography/Oids.cs:39–44
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    /// The RFC 8410 Ed25519 key/signature algorithm identifier, with absent ASN.1 parameters.
    /// </summary>
    public const string Ed25519 = "1.3.101.112";


PROPOSED SAMPLE — no verdict: core-001:unit:c8eec8d48a4c1a53 / C / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Cryptography/Oids.cs:86–88
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    /// <summary>The brainpoolP512r1 named-curve OID (RFC 5639).</summary>
    public const string BrainpoolP512r1 = "1.3.36.3.3.2.8.1.1.13";


PROPOSED SAMPLE — no verdict: core-001:unit:2a1d93e88d87f1d5 / C / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Cryptography/PublicKey.cs:1–27
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
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

namespace Yubico.YubiKit.Core.Cryptography;

/// <summary>
/// Abstract base class for public key implementations.
/// </summary>
/// <remarks>
/// This class provides common structure for public key types, requiring derived classes
/// to implement key type identification and X.509 export operations.
/// <para>
/// Concrete implementations include <see cref="ECPublicKey"/>, <see cref="RSAPublicKey"/> and <see cref="Curve25519PublicKey"/>, each providing
/// algorithm-specific public key handling and export mechanisms.
/// </para>
/// </remarks>



PROPOSED SAMPLE — no verdict: core-001:unit:aa2f54c40c8e100c / C / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Utilities/Base32.cs:33–61
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>Decodes Base32 text into caller-owned storage and returns the number of bytes written.</summary>
    /// <param name="encoded">The borrowed encoded text.</param>
    /// <param name="destination">The output buffer, which must not overlap the encoded text's bytes; unused bytes are unchanged on success.</param>
    /// <returns>The number of decoded bytes written.</returns>
    /// <remarks>
    ///     Admission rejects overlapping storage or an encoded byte length exceeding <see cref="int.MaxValue" />
    ///     before decoding, leaving both spans unchanged. Once admitted, any decoding failure clears the entire
    ///     destination, including any partially decoded prefix and unused tail. The borrowed text is never modified.
    ///     On success, the caller owns the output and must clear secret material after use.
    /// </remarks>
    /// <exception cref="ArgumentException">The spans overlap, the text is malformed, or the destination is too small.</exception>
    /// <exception cref="OverflowException">The encoded text's byte length exceeds <see cref="int.MaxValue" />.</exception>
    public static int Decode(ReadOnlySpan<char> encoded, Span<byte> destination)
    {
        // Keep admission outside cleanup: clearing an overlapping destination would modify borrowed text.
        if (MemoryMarshal.AsBytes(encoded).Overlaps(destination))
        {
            throw new ArgumentException("Base32 destination must not overlap encoded text.", nameof(destination));
        }

        try { return DecodeCore(encoded, destination); }
        catch
        {
            CryptographicOperations.ZeroMemory(destination);
            throw;
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:7deaa6b3f76664e4 / C / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Utilities/MultiString.cs:1–21
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
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

using System.Text;

namespace Yubico.YubiKit.Core.Utilities;

/// <summary>
///     Utilities for working with multi null-terminated strings.
/// </summary>



PROPOSED SAMPLE — no verdict: core-001:unit:d8cc0aa0dc9300b5 / C / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Credentials/CredentialKind.cs:28–31
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>A PUK (PIN unblocking key).</summary>
    Puk,


PROPOSED SAMPLE — no verdict: core-001:unit:7f76d577f673f224 / C / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Cryptography/AlgorithmInfo.cs:26–28
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    /// <summary>Key derivation, possibly including a key-agreement step.</summary>
    KeyDerivation,


PROPOSED SAMPLE — no verdict: core-001:unit:5d739810055c9f09 / C / enum-values
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Cryptography/Asn1KeyInfo.cs:21–23
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    /// <summary>A single ASN.1 NULL is encoded. RSA decoders also tolerate an absent field on input.</summary>
    Null,


PROPOSED SAMPLE — no verdict: core-001:unit:30292c6fb683371c / C / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Cli.Shared/src/Output/ConsoleUserPresencePrompt.cs:27–29
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private readonly Lock _gate = new();


PROPOSED SAMPLE — no verdict: core-001:unit:3361e11010d6e697 / C / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Credentials/IConsoleInputSource.cs:89–90
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private readonly Queue<string?> _lines = new();


PROPOSED SAMPLE — no verdict: core-001:unit:79dffe8684b8c5b7 / C / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Cryptography/Asn1KeyInfo.cs:30–36
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    internal Asn1KeyInfo(string keyAlgorithmOid, Asn1ParameterKind parameterKind, string? namedCurveOid = null)
    {
        AlgorithmOid = keyAlgorithmOid;
        ParameterKind = parameterKind;
        NamedCurveOid = namedCurveOid;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:b8a65ee27fc78501 / C / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Cryptography/AsnPublicKeyEncoder.cs:161–186
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static byte[] EncodeEcPublicKey(ReadOnlyMemory<byte> publicPoint, string curveOid)
    {
        AsnUtilities.ValidateEcPointArgument(publicPoint.Span, curveOid, nameof(publicPoint));

        // Write ASN.1 structure for SubjectPublicKeyInfo
        var writer = new AsnWriter(AsnEncodingRules.DER);

        // Start SubjectPublicKeyInfo SEQUENCE
        _ = writer.PushSequence();

        // Algorithm Identifier SEQUENCE
        _ = writer.PushSequence();
        writer.WriteObjectIdentifier(Oids.EcPublicKey);
        writer.WriteObjectIdentifier(curveOid);
        writer.PopSequence();

        // Write subject public key as BIT STRING
        writer.WriteBitString(publicPoint.Span);

        // End SubjectPublicKeyInfo SEQUENCE
        writer.PopSequence();

        return writer.Encode();
    }


PROPOSED SAMPLE — no verdict: core-001:unit:5439bcf06648bd8c / C / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Cryptography/CmacPrimitives.cs:30–42
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public void CmacInit(ReadOnlySpan<byte> keyData)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (keyData.Length != 16)
            {
                throw new ArgumentException("Key must be 16 bytes.", nameof(keyData));
            }

            _cmac?.Dispose();
            _cmac = new AesCmac(keyData);
        }


PROPOSED SAMPLE — no verdict: core-001:unit:56d58ec2173bbf1a / C / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Cryptography/CmacPrimitives.cs:42–48
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public void CmacUpdate(ReadOnlySpan<byte> dataToMac)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            (_cmac ?? throw new InvalidOperationException("CMAC has not been initialized.")).AppendData(dataToMac);
        }


PROPOSED SAMPLE — no verdict: core-001:unit:9bb620ed76fdd0f3 / C / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Cryptography/CryptographicDefinitions.Keys.cs:135–137
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private static KeyInfo Aes(KeyType keyType, int keySizeBits) => new(keyType, $"AES-{keySizeBits}", KeyFamily.Aes,
            keySizeBits, keySizeBits / 8, cose: new(CoseKeyType.Symmetric), privateValueLengthBytes: keySizeBits / 8);


PROPOSED SAMPLE — no verdict: core-001:unit:ff3f23e7387cb746 / C / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Utilities/DisposableArrayPoolBuffer.cs:25–26
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private byte[]? _rentedBuffer;


PROPOSED SAMPLE — no verdict: core-001:unit:34cd91fd41729511 / C / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Cli.Shared/tests/Yubico.YubiKit.Cli.Shared.UnitTests/Output/ConsoleUserPresencePromptTests.cs:12–26
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Theory]
    [InlineData(UserPresenceBasis.DeviceWaiting)]
    [InlineData(UserPresenceBasis.PolicyRequires)]
    public void Requested_WithCertainBasis_WritesOnceImmediately(UserPresenceBasis basis)
    {
        var output = new ConcurrentQueue<string>();
        var prompt = new ConsoleUserPresencePrompt(output.Enqueue, Task.Delay, Debounce);

        ValueTask request = prompt.OnUserPresenceRequestedAsync(CreateContext(basis), CancellationToken.None);

        Assert.True(request.IsCompletedSuccessfully);
        Assert.Equal(["Touch your YubiKey."], output);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:b334fdf119dbbfd3 / C / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Cli.Shared/tests/Yubico.YubiKit.Cli.Shared.UnitTests/Output/ConsoleUserPresencePromptTests.cs:41–67
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task Requested_WithIntentAndPolicyMayRequireAfterDelay_IncludesIntent()
    {
        var output = new ConcurrentQueue<string>();
        var written = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var delay = new ControlledDelay();
        var prompt = new ConsoleUserPresencePrompt(
            message =>
            {
                output.Enqueue(message);
                written.TrySetResult();
            },
            delay.WaitAsync,
            Debounce);
        UserPresenceContext context = CreateContext(UserPresenceBasis.PolicyMayRequire) with
        {
            Intent = "sign the release"
        };

        _ = prompt.OnUserPresenceRequestedAsync(context, CancellationToken.None);
        delay.Complete(0);

        await written.Task.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        Assert.Equal(["Touch your YubiKey to sign the release."], output);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:460986605acc85a7 / C / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Cli.Shared/tests/Yubico.YubiKit.Cli.Shared.UnitTests/Output/ConsoleUserPresencePromptTests.cs:134–165
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task Resolved_CorrelatesEqualContextsByInstance()
    {
        var output = new ConcurrentQueue<string>();
        var written = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var delay = new ControlledDelay();
        var prompt = new ConsoleUserPresencePrompt(
            message =>
            {
                output.Enqueue(message);
                written.TrySetResult();
            },
            delay.WaitAsync,
            Debounce);
        UserPresenceContext first = CreateContext(UserPresenceBasis.PolicyMayRequire);
        UserPresenceContext second = CreateContext(UserPresenceBasis.PolicyMayRequire);

        await prompt.OnUserPresenceRequestedAsync(first, CancellationToken.None);
        await prompt.OnUserPresenceRequestedAsync(second, CancellationToken.None);
        ValueTask resolved = prompt.OnUserPresenceResolvedAsync(
            first,
            UserPresenceOutcome.Completed,
            CancellationToken.None);
        delay.Complete(0);
        delay.Complete(1);

        await resolved;
        await written.Task.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        Assert.Equal(["Touch your YubiKey."], output);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:9f62ea84d8758ba3 / C / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Cli.Shared/tests/Yubico.YubiKit.Cli.Shared.UnitTests/Output/ConsoleUserPresencePromptTests.cs:185–187
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public void Complete(int index) => _delays[index].SetResult();


PROPOSED SAMPLE — no verdict: core-001:unit:032798b4fc91ebdd / C / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Cli.Shared/tests/Yubico.YubiKit.Cli.Shared.UnitTests/Output/PinPromptTests.cs:19–29
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void ConfirmMatches_ReturnsTrueForIdenticalEntry()
    {
        using var first = SecureCredential.FromUtf8String("correct horse");

        Assert.True(PinPrompt.ConfirmMatches(
            first,
            () => SecureCredential.FromUtf8String("correct horse")));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:921ec54140918360 / C / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Credentials/HexParsingTests.cs:19–20
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
public class HexParsingTests
{


PROPOSED SAMPLE — no verdict: core-001:unit:d2454b088aa8beac / C / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Credentials/UserPresenceIntentTests.cs:121–137
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task RepeatedDisposal_DoesNotAffectLaterScopes()
    {
        IDisposable first = UserPresenceIntent.BeginScope("first");
        first.Dispose();

        using (UserPresenceIntent.BeginScope("second"))
        {
            first.Dispose();
            Assert.Equal("second", UserPresenceIntent.Current);
        }

        Assert.Null(UserPresenceIntent.Current);
        await Task.CompletedTask;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:f1e20cac55e48d42 / C / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Credentials/UserPresenceIntentTests.cs:360–369
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static UserPresenceContext CreateContext(
        UserPresenceBasis basis = UserPresenceBasis.PolicyRequires) => new()
        {
            Basis = basis,
            Application = UserPresenceApplications.Fido2,
            Operation = UserPresenceOperations.Ctap2.GetAssertion,
            Scope = "example.com"
        };


PROPOSED SAMPLE — no verdict: core-001:unit:2c9ec2cab002ab5d / C / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Cryptography/AlgorithmIdentifierParameterTests.cs:153–173
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static byte[] ReplaceParameters(ReadOnlyMemory<byte> encodedKey, string parametersHex, bool privateKey)
    {
        var sequence = new AsnReader(encodedKey, AsnEncodingRules.DER).ReadSequence();
        if (privateKey) _ = sequence.ReadInteger();
        var algorithmOid = sequence.ReadSequence().ReadObjectIdentifier();
        var encodedValue = sequence.ReadEncodedValue();
        var writer = new AsnWriter(AsnEncodingRules.DER);
        try
        {
            using (writer.PushSequence())
            {
                if (privateKey) writer.WriteInteger(0);
                WriteAlgorithmIdentifier(writer, algorithmOid, parametersHex);
                writer.WriteEncodedValue(encodedValue.Span);
            }
            return writer.Encode();
        }
        finally { writer.Reset(); }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:7b9d951edf0e42b3 / C / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Cryptography/AsnPublicKeyDecoderTests.cs:45–55
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    // RFC 8410 section 10.2, "Example X25519 Certificate". SubjectPublicKeyInfo reconstructed from
    // the certificate's ASN.1 dump at offset 115.
    private static readonly byte[] Rfc8410X25519PublicKeySpki =
    [
        0x30, 0x2A, 0x30, 0x05, 0x06, 0x03, 0x2B, 0x65, 0x6E, 0x03, 0x21, 0x00,
        0x85, 0x20, 0xF0, 0x09, 0x89, 0x30, 0xA7, 0x54, 0x74, 0x8B, 0x7D, 0xDC,
        0xB4, 0x3E, 0xF7, 0x5A, 0x0D, 0xBF, 0x3A, 0x0D, 0x26, 0x38, 0x1A, 0xF4,
        0xEB, 0xA4, 0xA9, 0x8E, 0xAA, 0x9B, 0x4E, 0x6A
    ];


PROPOSED SAMPLE — no verdict: core-001:unit:fefb527db102790a / C / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Cryptography/AsnPublicKeyDecoderTests.cs:57–75
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Theory]
    [InlineData("1.2.840.10045.3.1.7")] // P-256
    [InlineData("1.3.132.0.34")] // P-384
    [InlineData("1.3.132.0.35")] // P-521
    public void CreatePublicKey_EcSpkiFromBcl_DecodesToMatchingParameters(string curveOid)
    {
        var curve = ECCurve.CreateFromValue(curveOid);
        using var ecdsa = ECDsa.Create(curve);
        var spki = ecdsa.ExportSubjectPublicKeyInfo();
        var expected = ecdsa.ExportParameters(includePrivateParameters: false);

        var decoded = AsnPublicKeyDecoder.CreatePublicKey(spki).Cast<ECPublicKey>();

        Assert.Equal(expected.Curve.Oid.Value, decoded.Parameters.Curve.Oid.Value);
        Assert.Equal(expected.Q.X, decoded.Parameters.Q.X);
        Assert.Equal(expected.Q.Y, decoded.Parameters.Q.Y);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:df139e68d3c46022 / C / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Cryptography/AsnPublicKeyDecoderTests.cs:99–108
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void CreatePublicKey_Rfc8410X25519Vector_DecodesToPublishedRawKey()
    {
        var decoded = AsnPublicKeyDecoder.CreatePublicKey(Rfc8410X25519PublicKeySpki).Cast<Curve25519PublicKey>();

        Assert.Equal(KeyType.X25519, decoded.KeyType);
        Assert.Equal(Rfc8410X25519RawPublicKey, decoded.PublicPoint.ToArray());
    }


PROPOSED SAMPLE — no verdict: core-001:unit:deb12c6dc5e5dad5 / C / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Cryptography/CryptographicDefinitionsTests.cs:117–135
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void Misses_HaveNullableFlowAndContextualErrors()
    {
        Assert.False(Keys.TryGet(KeyType.None, out var keyInfo));
        Assert.Null(keyInfo);
        Assert.False(Curves.TryGet("future-curve", out var curveInfo));
        Assert.Null(curveInfo);
        Assert.False(Algorithms.TryGet("future-algorithm", out var algorithmInfo));
        Assert.Null(algorithmInfo);
        Assert.Empty(Algorithms.FindByOid("1.2.3.4.5"));
        Assert.Contains("None", Assert.Throws<ArgumentException>(() => Keys.Get(KeyType.None)).Message, StringComparison.Ordinal);
        Assert.Contains("future-curve", Assert.Throws<NotSupportedException>(() => Curves.Get("future-curve")).Message, StringComparison.Ordinal);
        Assert.Contains("1.2.3.4.5", Assert.Throws<NotSupportedException>(() => Curves.GetByOid("1.2.3.4.5")).Message, StringComparison.Ordinal);
        Assert.Contains("future-algorithm", Assert.Throws<NotSupportedException>(() => Algorithms.Get("future-algorithm")).Message, StringComparison.Ordinal);
        Assert.False(KeyType.None.IsAsymmetric());
        Assert.False(((KeyType)999).IsAsymmetric());
    }


PROPOSED SAMPLE — no verdict: core-001:unit:7cc410f5a51db597 / C / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Cryptography/Curve25519PrivateKeyTests.cs:82–94
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void CreateFromValue_CopiesCallerOwnedBytes()
    {
        var rawPrivateKey = Convert.FromHexString(Rfc7748AlicePrivateKeyHex);
        var expected = rawPrivateKey.ToArray();
        using var key = Curve25519PrivateKey.CreateFromValue(rawPrivateKey, KeyType.X25519);

        CryptographicOperations.ZeroMemory(rawPrivateKey);

        Assert.Equal(expected, key.PrivateKey.ToArray());
    }


PROPOSED SAMPLE — no verdict: core-001:unit:bc0fa8e079704b98 / C / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Cryptography/KnownPrivateKeyVectors.cs:33–41
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public const string Rfc7517Rsa2048D =
        "5F8713B5E258FE09F81583EC5C1F2B7578B1E6FC2C83514B37913711A1BA449A151FE1CB2CA0FD33B771E68A3B194464" +
        "9DC867AD1C1E5240BB853E5F24B33459B14028D2D6636BEFEC1E8DA974B352FC53D3F6127EA8A3C29DD14F3941682C56" +
        "A78768164E4DDA8F06CBF9C734AAE8003224278EA9454A21B17CB06D178075868CC05B3DB6FF1DFDC3D56378B4EDADED" +
        "F0C37A4CDC26D1D49AC26F6FE3B5220A5DD29396621BBC688CF2EEE2C6E0D54DA3C782014CD0739DB252CC51CAEBA8D3" +
        "F1B824BAAB24D068EC903264D7D678AB08F06EC9E7E23D960628B744BF94B3694656463C7E417399ED73D076C891FCF4" +
        "63A9AA9CE62DA9CD17E237DC2A8002F1";


PROPOSED SAMPLE — no verdict: core-001:unit:8196d33310623fdc / C / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Cryptography/PrivateKeyConsistencyTests.cs:140–151
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    [Trait("Audit", "YESDK-1635")]
    public void Rsa_ModulusFromAnotherKey_IsRejectedViaCreateFromParameters()
    {
        using var a = RSA.Create(2048);
        using var b = RSA.Create(2048);
        var parameters = a.ExportParameters(true) with { Modulus = b.ExportParameters(false).Modulus };

        Assert.Throws<CryptographicException>(() => RSAPrivateKey.CreateFromParameters(parameters));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:62c982eb667d5210 / C / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Cryptography/PrivateKeyConsistencyTests.cs:165–176
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    [Trait("Audit", "YESDK-1635")]
    public void Ec_PointFromAnotherScalar_IsRejectedViaCreateFromParameters()
    {
        using var a = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        using var b = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        var parameters = a.ExportParameters(true) with { Q = b.ExportParameters(false).Q };

        Assert.Throws<CryptographicException>(() => ECPrivateKey.CreateFromParameters(parameters));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:c96b32548ca7a275 / C / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Cryptography/PrivateKeyConsistencyTests.cs:189–196
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static ECParameters EcParameters(string curve, string d, string x, string y) => new()
    {
        Curve = ECCurve.CreateFromFriendlyName(curve),
        D = Convert.FromHexString(d),
        Q = new ECPoint { X = Convert.FromHexString(x), Y = Convert.FromHexString(y) },
    };


PROPOSED SAMPLE — no verdict: core-001:unit:3752027db9a798b9 / C / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Utilities/Base32Tests.cs:12–40
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    [Theory]
    [InlineData("", "")]
    [InlineData("MY======", "f")]
    [InlineData("MZXQ====", "fo")]
    [InlineData("MZXW6===", "foo")]
    [InlineData("MZXW6YQ=", "foob")]
    [InlineData("MZXW6YTB", "fooba")]
    [InlineData("MZXW6YTBOI======", "foobar")]
    [InlineData("MZ", "f")]
    [InlineData("m z x w 6 y t b o i", "foobar")]
    public void Decode_RfcVectors_WriteExactBytesAndLeaveUnusedDestinationAlone(string encoded, string expected)
    {
        foreach (string input in new[] { encoded, encoded.TrimEnd('=') })
        {
            byte[] destination = new byte[32];
            Array.Fill(destination, (byte)0xA5);
            byte[] decoded = Base32.Decode(input.AsSpan());
            try
            {
                int written = Base32.Decode(input.AsSpan(), destination);
                Assert.Equal(expected.Length, written);
                Assert.True(CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), destination.AsSpan(0, written)));
                Assert.True(CryptographicOperations.FixedTimeEquals(decoded, destination.AsSpan(0, written)));
                Assert.True(destination.AsSpan(written).IndexOfAnyExcept((byte)0xA5) < 0);
            }
            finally { CryptographicOperations.ZeroMemory(destination); CryptographicOperations.ZeroMemory(decoded); }
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:04dffa6613a85a0c / C / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Utilities/DisposableTlvListTests.cs:72–87
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void AsSpan_ReturnsSpanWithCorrectLength()
    {
        // Arrange
        var tlv1 = new Tlv(0x01, [0x11]);
        var tlv2 = new Tlv(0x02, [0x22]);

        // Act
        using var collection = new DisposableTlvList(tlv1, tlv2);
        var span = collection.AsSpan();

        // Assert
        Assert.Equal(2, span.Length);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:f147b8be51f14f4c / C / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Utilities/DisposableTlvListTests.cs:87–103
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void AsSpan_ContainsCorrectTlvs()
    {
        // Arrange
        var tlv1 = new Tlv(0x01, [0x11]);
        var tlv2 = new Tlv(0x02, [0x22]);

        // Act
        using var collection = new DisposableTlvList(tlv1, tlv2);
        var span = collection.AsSpan();

        // Assert
        Assert.Equal(0x01, span[0].Tag);
        Assert.Equal(0x02, span[1].Tag);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:34fe797e985d886a / C / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Utilities/TlvTests.cs:154–159
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void Create_IndefiniteLength0x80_ThrowsArgumentException() =>
        // Tag 0x5A with length byte 0x80 (BER-TLV indefinite length — not supported)
        Assert.Throws<ArgumentException>(() => Tlv.Create((ReadOnlySpan<byte>)[0x5A, 0x80, 0x01, 0x02]));


PROPOSED SAMPLE — no verdict: core-001:unit:5632a5298da7f3dd / C / type-declarations
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Credentials/IConsoleInputSource.cs:87–88
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal sealed class MockConsoleInput : IConsoleInputSource
{


PROPOSED SAMPLE — no verdict: core-001:unit:ccd14dceb9360243 / C / type-declarations
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Credentials/UserPresenceOperations.cs:93–94
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    public static class YubiHsmAuth
    {


PROPOSED SAMPLE — no verdict: core-001:unit:459ee132eb757e46 / C / type-declarations
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Cryptography/AlgorithmInfo.cs:73–74
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
public sealed record AlgorithmInfo
{


PROPOSED SAMPLE — no verdict: core-001:unit:2af033748d9a7e55 / C / type-declarations
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Cryptography/KeyTypeExtensions.cs:25–26
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
public static class KeyTypeExtensions
{


PROPOSED SAMPLE — no verdict: core-001:unit:b01defda889ad3bd / C / type-declarations
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/src/Utilities/Crc13239.cs:20–21
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
public static class Crc13239
{
