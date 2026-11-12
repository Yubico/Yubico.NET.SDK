PROPOSED SAMPLE — no verdict: core-001:unit:7ef8a2e75575c5d2 / Piv / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/examples/PivTool/Cli/Commands/DeviceHelper.cs:175–187
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    /// Parses a management key algorithm type string.
    /// </summary>
    internal static KeyType ParseMgmtKeyType(string? value) =>
        value?.ToLowerInvariant() switch
        {
            "aes128" => KeyType.Aes128,
            "aes192" => KeyType.Aes192,
            "aes256" => KeyType.Aes256,
            _ => KeyType.TripleDes
        };


PROPOSED SAMPLE — no verdict: core-001:unit:3eb8053339c6e5be / Piv / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/examples/PivTool/Cli/Prompts/PinPrompt.cs:97–118
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    /// Prompts for a PUK with option to use the default. Caller must dispose the returned memory owner.
    /// </summary>
    /// <param name="prompt">The prompt text to display.</param>
    /// <returns>The PUK as UTF-8 bytes in a memory owner, or null if cancelled.</returns>
    public static IMemoryOwner<byte>? GetPukWithDefault(string prompt = "PUK")
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[blue]{prompt}:[/]")
                .AddChoices(["Use default", "Enter custom PUK"]));

        if (choice == "Use default")
        {
            return CreateFromDefault(DefaultPukValue);
        }

        var options = CredentialReaderOptions.ForPuk();
        return CredentialReader.ReadCredential(options);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:1990c8b8a253ce31 / Piv / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/examples/PivTool/PivExamples/Certificates.cs:320–414
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    /// Generates a Certificate Signing Request (CSR).
    /// </summary>
    /// <param name="session">A PIV session.</param>
    /// <param name="slot">The slot containing the key.</param>
    /// <param name="subject">Certificate subject (e.g., "CN=Test User").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the CSR in PEM format or error.</returns>
    /// <example>
    /// <code>
    /// await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);
    /// await session.VerifyPinAsync(pin, ct); // Borrowed caller-owned PIN; required by the slot policy.
    ///
    /// var result = await Certificates.GenerateCsrAsync(
    ///     session, Slot.Authentication, "CN=Test User", ct);
    /// 
    /// using var certificate = result.Certificate;
    /// if (result.Success &amp;&amp; result.CsrPem is { } csrPem)
    /// {
    ///     await File.WriteAllTextAsync("request.csr", csrPem, ct);
    /// }
    /// </code>
    /// </example>
    public static async Task<CertificateResult> GenerateCsrAsync(
        IPivSession session,
        Slot slot,
        string subject,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            var metadata = await session.GetSlotMetadataAsync(slot, cancellationToken);
            if (metadata is null)
            {
                return CertificateResult.Failed($"Slot {slot} is empty. Generate or import a key first.");
            }

            var slotMetadata = metadata.Value;
            if (slotMetadata.PublicKey.IsEmpty)
            {
                return CertificateResult.Failed($"Slot {slot} has no public key. Generate a key first.");
            }

            var keyType = slotMetadata.KeyType ?? throw new NotSupportedException($"Unknown PIV algorithm 0x{slotMetadata.RawAlgorithmId:X2}.");
            using var publicKey = keyType.IsRsa()
                ? (AsymmetricAlgorithm)slotMetadata.GetRsaPublicKey()
                : slotMetadata.GetECDsaPublicKey();

            if (publicKey is null)
            {
                return CertificateResult.Failed("Failed to extract public key from slot metadata.");
            }

            var subjectName = new X500DistinguishedName(subject);
            var hashAlgorithm = slotMetadata.KeyType == KeyType.P384
                ? HashAlgorithmName.SHA384
                : HashAlgorithmName.SHA256;

            // CSR must be signed by the private key on the YubiKey.
            // Use the custom X509SignatureGenerator that delegates signing to the device.
            CertificateRequest request;
            X509SignatureGenerator signatureGenerator;

            if (publicKey is RSA rsa)
            {
                request = new CertificateRequest(subjectName, rsa, hashAlgorithm, RSASignaturePadding.Pkcs1);
                signatureGenerator = new YubiKeyRsaSignatureGenerator(session, slot, keyType, rsa);
            }
            else if (publicKey is ECDsa ecdsa)
            {
                request = new CertificateRequest(subjectName, ecdsa, hashAlgorithm);
                signatureGenerator = new YubiKeyEcdsaSignatureGenerator(session, slot, keyType, ecdsa);
            }
            else
            {
                return CertificateResult.Failed($"Unsupported key type: {publicKey.GetType().Name}");
            }

            // CreateSigningRequest(X509SignatureGenerator) returns DER-encoded PKCS#10 bytes.
            var derBytes = request.CreateSigningRequest(signatureGenerator);
            var pem = "-----BEGIN CERTIFICATE REQUEST-----\n"
                + Convert.ToBase64String(derBytes, Base64FormattingOptions.InsertLineBreaks)
                + "\n-----END CERTIFICATE REQUEST-----";

            return CertificateResult.CsrGenerated(pem);
        }
        catch (Exception ex)
        {
            return CertificateResult.Failed($"CSR generation failed: {ex.Message}");
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:93c8f7b10e5bd567 / Piv / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/src/Authentication/AuthenticationProtocol.cs:300–331
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    /// Shared implementation for single-block ECB encrypt/decrypt with buffer management.
    /// </summary>
    private static void CryptoBlockCore(ReadOnlySpan<byte> key, ReadOnlySpan<byte> input, Span<byte> output, KeyType keyType, bool encrypt)
    {
        if (keyType == KeyType.TripleDes)
        {
            // Preserve explicit EDE for the public factory default, which .NET rejects as a
            // weak TripleDES key. Yubico uses this factory pattern; each DES component is still valid.
            if (encrypt)
                TripleDesEncryptManual(key, input, output);
            else
                TripleDesDecryptManual(key, input, output);
            return;
        }
        byte[] keyCopy = key.ToArray();
        try
        {
            using var aes = Aes.Create();
            aes.Key = keyCopy;
            if (encrypt)
                _ = aes.EncryptEcb(input, output, PaddingMode.None);
            else
                _ = aes.DecryptEcb(input, output, PaddingMode.None);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyCopy);
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:009632cb50b26e3d / Piv / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/src/Certificates/CertificateProtocol.cs:39–95
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    /// Gets the certificate stored in the specified slot.
    /// </summary>
    /// <param name="slot">The slot to read from.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The certificate, or null if the slot is empty.</returns>
    internal static async Task<X509Certificate2?> GetCertificateAsync(
        IBackend backend,
        Slot slot,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Getting certificate from slot 0x{Slot:X2}", (byte)slot);

        // Get the object ID for this slot
        int objectId = GetCertificateObjectId(slot);

        // Read the data object
        var certData = await DataObjectProtocol.GetObjectAsync(backend, objectId, cancellationToken).ConfigureAwait(false);

        if (certData.Length == 0)
        {
            return null;
        }

        // Parse TLV: TAG 0x70 (cert) + TAG 0x71 (info) + TAG 0xFE (LRC)
        // Note: GetObjectAsync already unwraps the outer 0x53 tag
        var tlvDict = TlvHelper.DecodeDictionary(certData.AsSpan());

        byte[]? certBytes = tlvDict.TryGetValue(TagCertificate, out var cert)
            ? cert.ToArray()
            : null;

        bool isCompressed = tlvDict.TryGetValue(TagCertificateInfo, out var info)
            && info.Length > 0
            && info.Span[0] == CompressedCertificate;

        if (certBytes is null || certBytes.Length == 0)
        {
            return null;
        }

        // Decompress if needed
        if (isCompressed)
        {
            using var input = new MemoryStream(certBytes);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            await gzip.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
            certBytes = output.ToArray();
        }

#pragma warning disable SYSLIB0057 // X509Certificate2(byte[]) is obsolete
        return new X509Certificate2(certBytes);
#pragma warning restore SYSLIB0057
    }


PROPOSED SAMPLE — no verdict: core-001:unit:6eec962fc55a985c / Piv / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/src/DataObjects/CardholderUniqueId.cs:95–119
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    /// Encodes this instance as the inner content of the CHUID object, matching the bytes
    /// returned by <see cref="IPivSession.GetObjectAsync"/> and expected by
    /// <see cref="IPivSession.PutObjectAsync"/> (i.e. without the outer <c>0x53</c> wrapper).
    /// </summary>
    public ReadOnlyMemory<byte> Encode()
    {
        if (IsEmpty)
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        Tlv[] tlvs =
        [
            new Tlv(FascNumberTag, FixedFascNumberBytes),
            new Tlv(GuidTag, _guidValue.Span),
            new Tlv(ExpirationDateTag, FixedExpirationDateAscii),
            new Tlv(SignatureTag, ReadOnlySpan<byte>.Empty),
            new Tlv(LrcTag, ReadOnlySpan<byte>.Empty)
        ];

        return TlvHelper.EncodeAndDisposeList(tlvs).ToArray();
    }


PROPOSED SAMPLE — no verdict: core-001:unit:81297035b8793da4 / Piv / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/src/Metadata.cs:78–86
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>Requires a recognized mapped PIV type; operations separately enforce their asymmetric/symmetric subset.</summary>
    internal KeyType RequireKeyType()
    {
        var known = KeyType ?? throw new NotSupportedException($"PIV algorithm 0x{RawAlgorithmId:X2} is not supported by this SDK.");
        _ = KeyTypeEncoding.Encode(known);
        return known;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:6576de010de5767d / Piv / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/src/PivSession.cs:387–408
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <inheritdoc />
    public async Task ImportKeyAsync(
        Slot slot,
        IPrivateKey privateKey,
        KeyCreationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        EnsureBackend(cancellationToken);
        ValidateSlot(slot);

        var pinPolicy = options?.PinPolicy ?? PinPolicy.Default;
        var touchPolicy = options?.TouchPolicy ?? TouchPolicy.Default;

        ArgumentNullException.ThrowIfNull(privateKey);
        EnsureKeyOperationSupported(privateKey.KeyType, pinPolicy, touchPolicy, generate: false);
        await EnsureBiometricPolicySupportedAsync(pinPolicy, cancellationToken).ConfigureAwait(false);
        EnsureManagementKeyAuthenticated();
        await KeyProtocol.ImportKeyAsync(_backend, slot, privateKey, pinPolicy, touchPolicy, cancellationToken)
            .ConfigureAwait(false);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:a579b6a5cf4242e4 / Piv / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/examples/PivTool/Cli/Commands/CliRunner.cs:155–176
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static async Task<int> HandlePinVerifyAsync(
        Dictionary<string, string?> flags, int? serial, bool useJson, CancellationToken ct)
    {
        var pinBytes = DeviceHelper.ParseCredentialBytes(DeviceHelper.GetFlag(flags, "pin"), "pin");
        if (pinBytes is null) return 1;

        var device = await DeviceHelper.GetDeviceAsync(serial, ct);
        if (device is null) { ZeroAndReturn(pinBytes); return 1; }

        try
        {
            await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);
            var result = await PinManagement.VerifyPinAsync(session, pinBytes, ct);
            return WriteResult(result, useJson);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pinBytes);
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:8fc815177a95b970 / Piv / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/examples/PivTool/Cli/Commands/CliRunner.cs:256–277
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static async Task<int> HandleMgmtKeyAuthAsync(
        Dictionary<string, string?> flags, int? serial, bool useJson, CancellationToken ct)
    {
        var key = DeviceHelper.ParseHex(DeviceHelper.GetFlag(flags, "management-key"), "management-key");
        if (key is null) return 1;

        var device = await DeviceHelper.GetDeviceAsync(serial, ct);
        if (device is null) { ZeroAndReturn(key); return 1; }

        try
        {
            await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);
            var result = await PinManagement.AuthenticateAsync(session, key, ct);
            return WriteResult(result, useJson);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:b3919addb32e317d / Piv / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/examples/PivTool/Cli/Commands/JsonOutput.cs:165–205
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    internal static void Write(SlotInfoResult result)
    {
        var slots = new JsonArray();
        foreach (var slot in result.Slots)
        {
            var slotNode = new JsonObject
            {
                ["slot"] = slot.Slot.ToString(),
                ["name"] = slot.Name,
                ["hasKey"] = slot.HasKey,
                ["hasCertificate"] = slot.HasCertificate,
                ["certificate"] = SerializeCert(slot.Certificate)
            };

            if (slot.Metadata is SlotMetadata m)
            {
                slotNode["metadata"] = new JsonObject
                {
                    ["keyType"] = m.KeyType.ToString(),
                    ["pinPolicy"] = m.PinPolicy.ToString(),
                    ["touchPolicy"] = m.TouchPolicy.ToString(),
                    ["isGenerated"] = m.IsGenerated,
                    ["publicKeyBase64"] = m.PublicKey.Length > 0
                        ? Convert.ToBase64String(m.PublicKey.Span)
                        : null
                };
            }

            slots.Add(slotNode);
        }

        var node = new JsonObject
        {
            ["success"] = result.Success,
            ["slots"] = slots,
            ["error"] = result.ErrorMessage
        };
        Console.WriteLine(node.ToJsonString(Options));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:8f112e59fc6debcb / Piv / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/src/Backends/SmartCardBackend.cs:50–61
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static FirmwareVersion ParseVersionResponse(ReadOnlySpan<byte> response)
    {
        if (response.Length < 3)
        {
            throw new InvalidOperationException(
                $"Invalid version response: expected at least 3 bytes, got {response.Length}");
        }

        return new FirmwareVersion(response[0], response[1], response[2]);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:df0399d60d88c3f0 / Piv / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/src/KeyTypeEncoding.cs:47–54
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    internal static byte EncodeAsymmetric(KeyType keyType)
    {
        if (!keyType.IsAsymmetric())
            throw new NotSupportedException($"Key type {keyType} is not an asymmetric PIV key type.");
        return Encode(keyType);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:0aa0e8aee63a59f5 / Piv / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/src/KeyTypeEncoding.cs:54–61
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    internal static byte EncodeManagement(KeyType keyType)
    {
        if (!keyType.IsSymmetric())
            throw new NotSupportedException($"Key type {keyType} is not a PIV management key type.");
        return Encode(keyType);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:d741c3bac5834946 / Piv / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/src/PivSession.cs:977–984
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private void EnsureFipsKeyPolicySupported(KeyType keyType, PinPolicy pinPolicy)
    {
        if (!FirmwareVersion.IsAlphaOrBeta && FirmwareVersion.IsAtLeast(4, 4, 0) && !FirmwareVersion.IsAtLeast(4, 5, 0)
            && (keyType == KeyType.Rsa1024 || pinPolicy == PinPolicy.Never))
            throw new NotSupportedException("RSA1024 and PIN policy Never are not supported on YubiKey 4 FIPS.");
    }


PROPOSED SAMPLE — no verdict: core-001:unit:e0d8e2e0ada81ba1 / Piv / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/src/Yubico.YubiKit.Piv.csproj:1–26
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- TargetFrameworks inherited from Directory.Build.props -->
    <RootNamespace>Yubico.YubiKit.Piv</RootNamespace>
    <AssemblyName>Yubico.YubiKit.Piv</AssemblyName>

    <!-- Package Metadata -->
    <Description>PIV (Personal Identity Verification) application support for YubiKey devices including certificate management, key generation, and digital signatures.</Description>
    <PackageTags>yubikey;yubico;piv;smart-card;certificates;pkcs11</PackageTags>

    <!-- Opts this published library into the shared Native AOT analyzer and metadata properties. -->
    <IsYubiKitSdkLibrary>true</IsYubiKitSdkLibrary>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\Core\src\Yubico.YubiKit.Core.csproj" />
  </ItemGroup>

  <!-- The PivSession constructor is internal so callers cannot bypass ApplicationSession.Construct;
       the unit tests still need it to build an uninitialised session and assert its defaults. -->
  <ItemGroup>
    <InternalsVisibleTo Include="Yubico.YubiKit.Piv.UnitTests" />
  </ItemGroup>

</Project>



PROPOSED SAMPLE — no verdict: core-001:unit:89c5d1ecb6fd0e8d / Piv / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/README.md:142–175
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
### Verify the PIN, sign and verify on the host

This fragment uses the P-256 key generated above and firmware 5.3+ metadata. The public-key extractor returns
a disposable host object containing public material only. The host verifies the exact digest and DER signature.

```csharp
await using var session = await device.CreatePivSessionAsync();

byte[] pin = Encoding.UTF8.GetBytes("123456");
byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes("message to sign"));
Memory<byte> signature = Memory<byte>.Empty;
try
{
    await session.VerifyPinAsync(pin);
    var metadata = await session.GetSlotMetadataAsync(Slot.Authentication)
        ?? throw new InvalidOperationException("Generate a P-256 key first.");
    using var verifier = metadata.GetECDsaPublicKey();
    signature = await session.SignOrDecryptAsync(Slot.Authentication, KeyType.P256, digest);
    if (!verifier.VerifyHash(digest, signature.Span, DSASignatureFormat.Rfc3279DerSequence))
        throw new CryptographicException("The slot signature failed host verification.");
}
finally
{
    CryptographicOperations.ZeroMemory(pin);
    CryptographicOperations.ZeroMemory(digest);
    CryptographicOperations.ZeroMemory(signature.Span);
}
```

ECDSA accepts a digest and returns ASN.1 DER `SEQUENCE(r,s)`. Ed25519 accepts the original message and returns
a raw 64-byte signature. RSA signing accepts a complete host-prepared EMSA-PKCS1-v1_5 or EMSA-PSS encoded
representative and returns a modulus-sized result. A hash alone is insufficient RSA signature encoding.
The overload without `KeyType` reads metadata (5.3+); older keys need the explicit type, matched to the slot.




PROPOSED SAMPLE — no verdict: core-001:unit:65b137a50ae4091c / Piv / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/README.md:281–301
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
### Read provisioning objects and attest keys

Use the typed CHUID, CCC, ADMIN DATA and Key History methods for the SDK's modeled provisioning shapes.
CHUID fixes FASC-N/expiration/signature fields; CCC fixes its surrounding fields. Other valid vendor/profile
encodings may need `GetObjectAsync`/`PutObjectAsync`. Raw methods exchange inner object bytes, with the
outer `53` wrapper handled by the SDK. Use defined `DataObject` identifiers; keep request IDs within the
intended PIV range because arbitrary integers currently reach the encoder without a range guard.

CHUID/CCC constructors borrow supplied identifier memory; decoding copies it and encoding returns a new
nonsecret buffer. Key History counts describe retired keys with on-/off-card certificates. Maintain it
alongside your provisioning workflow; its parser/constructor validate a narrower shape than a complete
federal profile. ADMIN flags describe stored software records. Its timestamp currently uses trimmed
little-endian Unix seconds, differing from Python YubiKey Manager's four-byte big-endian encoding;
coordinate interoperability before writing timestamps across tools.

`AttestKeyAsync` returns a certificate for an on-device-generated key. Retrieve the F9 certificate for the
signer chain, validate against your trust policy, and bind the attested public key to the intended slot.
Retrieval alone supplies neither chain trust nor that binding. Preserve factory F9 material: replacing it
is a separate, potentially irreversible provisioning decision. A host public-key object can prepare a
certificate request, while device signing needs an external `X509SignatureGenerator`, as PivTool demonstrates.




PROPOSED SAMPLE — no verdict: core-001:unit:70fb463e165b9ad9 / Piv / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/examples/PivTool/README.md:48–97
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
## CLI tasks

All 22 operational commands accept `--serial` and `--json`. Slot selectors are `9a`, `9c`, `9d`, `9e`
(Authentication, Signature, Key Management, Card Authentication). The SDK also exposes retired and
attestation slots, but these CLI selectors cover only the four standard slots.

| Task | Command and options |
| --- | --- |
| Device and PIV retry information | `device-info` |
| Verify PIN | `pin-verify --pin <current>` |
| Change PIN | `pin-change --pin <current> --new-pin <new>` |
| Change PUK | `puk-change --puk <current> --new-puk <new>` |
| Unblock PIN with usable PUK | `pin-unblock --puk <current> --new-pin <new>` |
| Read remaining PIN attempts | `pin-retries` |
| Authenticate management key in this command's session | `mgmt-key-auth --management-key <hex>` |
| Rotate management key | `mgmt-key-change --management-key <current-hex> --new-management-key <new-hex> [--key-type 3des|aes128|aes192|aes256] [--require-touch]` |
| Generate a key | `key-generate --slot <slot> --key-type <type> --management-key <hex> [--pin-policy default|never|once|always] [--touch-policy default|never|always|cached]` |
| Read certificate | `cert-view --slot <slot>` |
| Export PEM | `cert-export --slot <slot> [--output <path>]` |
| Import PEM/DER | `cert-import --slot <slot> --cert-path <path> --management-key <hex> [--compress]` |
| Generate/store self-signed certificate | `cert-self-sign --slot <slot> --management-key <hex> --pin <pin> [--subject <dn>] [--validity-days <n>]` |
| Generate CSR | `cert-csr --slot <slot> --pin <pin> [--subject <dn>] [--output <path>]` |
| Delete certificate, retaining key | `cert-delete --slot <slot> --management-key <hex>` |
| Hash and sign (use P-256/P-384) | `sign --slot <slot> --input <path> --pin <pin> [--hash sha256|sha384|sha512] [--output <path>]` |
| Decrypt RSA PKCS#1 v1.5 ciphertext | `decrypt --slot <slot> --input <path> --pin <pin> [--output <path>]` |
| Verify against stored certificate | `verify-signature --slot <slot> --data <path> --signature <path> [--hash sha256|sha384|sha512]` |
| Retrieve slot attestation | `attest --slot <slot> [--output <path>]` |
| Retrieve F9 signer certificate | `attest-intermediate [--output <path>]` |
| Inspect one/all standard slots | `slot-info [--slot <slot>]` |
| Reset normal PIV state | `reset [--force]` |

Asymmetric key selectors are `rsa2048`, `rsa3072`, `rsa4096`, `eccp256`, `eccp384`, `ed25519`
and `x25519`, subject to firmware/profile gates. Management-key type defaults to 3DES in this tool's
parser; specify it when rotating to AES. Retry-limit configuration and PIN-only workflows are SDK tasks
rather than CLI commands.

JSON records use lower-camel-case fields: `success`, optional `retriesRemaining` and `error`.
Key generation includes `keyType` and `publicKeyBase64` (base64 of DER SubjectPublicKeyInfo);
signing includes `signatureBase64`/`signatureHex`;
decryption includes `decryptedBase64`. Certificate results carry `certificate` metadata (including `pem`)
or `csrPem`; attestation results carry `attestationCertificate`/`intermediateCertificate` metadata.
`attest-intermediate` currently places F9 signer metadata in `attestationCertificate` and leaves
`intermediateCertificate` null, matching the helper's result shape.
Verification reports `isValid`; timed operations include `elapsedMs`. Text signature output is hex;
decrypt text output is base64 when no
output file is selected. With `--output`, signatures/plaintext are binary and certificates/CSRs are PEM.
Diagnostics use stderr, while JSON failure objects remain on stdout. Handled operational failure normally
returns 1; success returns 0. Inspect result fields too: `device-info` can return device data even if the
PIV retry subquery fails, and verification's exit status describes execution rather than validity.




PROPOSED SAMPLE — no verdict: core-001:unit:f066105117ef736a / Piv / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCertificateTests.cs:60–72
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task GetCertificateAsync_EmptySlot_ReturnsNull(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();

        var cert = await session.GetCertificateAsync(Slot.Authentication);

        Assert.Null(cert);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:ee5b81e0432fe552 / Piv / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCryptoTests.cs:172–191
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.0")]
    public async Task CalculateSecretAsync_X25519_ProducesSharedSecret(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        var publicKey = await session.GenerateKeyAsync(
            Slot.KeyManagement,
            KeyType.X25519);
        await session.VerifyPinAsync(DefaultPin);

        var curve25519Key = (Curve25519PublicKey)publicKey;
        Assert.Equal(CryptographicDefinitions.Keys.Ed25519.KeySizeBytes, curve25519Key.PublicPoint.Length);

        // This method checks generation and public-point width only; it performs no agreement call.
        // TODO: Add a compatible peer X25519 implementation and compare raw shared secrets.
    }


PROPOSED SAMPLE — no verdict: core-001:unit:f6b6a42a64636d03 / Piv / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivMetadataTests.cs:41–55
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.3.0")]
    public async Task GetPinMetadataAsync_ReturnsValidMetadata(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();

        var metadata = await session.GetPinMetadataAsync();

        Assert.True(metadata.IsDefault);
        Assert.Equal(3, metadata.TotalRetries);
        Assert.Equal(3, metadata.RetriesRemaining);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:d428dd99f2122ebc / Piv / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivMetadataTests.cs:67–90
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.3.0")]
    public async Task GetSlotMetadataAsync_WithKey_ReturnsMetadata(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        KeyType[] keyTypes = [KeyType.P256, KeyType.Rsa2048];
        foreach (var keyType in keyTypes)
        {
            var generated = await session.GenerateKeyAsync(Slot.Authentication, keyType);
            var metadata = await session.GetSlotMetadataAsync(Slot.Authentication);

            Assert.NotNull(metadata);
            Assert.Equal(keyType, metadata.Value.KeyType);
            Assert.True(metadata.Value.IsGenerated);
            using AsymmetricAlgorithm extracted = keyType.IsRsa()
                ? metadata.Value.GetRsaPublicKey()
                : metadata.Value.GetECDsaPublicKey();
            Assert.Equal(generated.ExportSubjectPublicKeyInfo(), extracted.ExportSubjectPublicKeyInfo());
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:fce5dda5582cebf9 / Piv / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivMultiKeyContentionTests.cs:185–240
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     A long on-card operation on one physical key must not serialize PIV work on another key.
    ///     Both directions are exercised so reader enumeration order cannot hide a cross-key gate.
    /// </summary>
    [SkippableFact]
    [Trait(TestCategories.Category, TestCategories.RequiresHardware)]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task Rsa4096Keygen_OnOneKey_DoesNotDelayPivOperationsOnAnotherKey()
    {
        var states = GetRsa4096SmartCardStatesOrSkip(2);
        var keyA = states[0];
        var keyB = states[1];

        await using var sessionA = await keyA.Device.CreatePivSessionAsync();
        await using var sessionB = await keyB.Device.CreatePivSessionAsync();

        Exception? testFailure = null;
        try
        {
            foreach (var (session, state) in new[] { (sessionA, keyA), (sessionB, keyB) })
            {
                await session.ResetAsync();
                await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
                _ = await session.GenerateKeyAsync(
                    Slot.Authentication,
                    KeyType.P256,
                    new KeyCreationOptions { PinPolicy = PinPolicy.Once, TouchPolicy = TouchPolicy.Never });
                await session.VerifyPinAsync(DefaultPin);
            }

            await AssertCrossKeyLivenessAsync(sessionA, keyA, sessionB, keyB);
            await AssertCrossKeyLivenessAsync(sessionB, keyB, sessionA, keyA);
        }
        catch (Exception exception)
        {
            testFailure = exception;
        }

        var cleanupFailure = await ResetBothPivApplicationsAsync(sessionA, keyA, sessionB, keyB);

        if (testFailure is not null)
        {
            if (cleanupFailure is not null)
            {
                testFailure.Data["PIV cleanup failure"] = cleanupFailure;
                output.WriteLine("Cleanup also failed after the primary test failure: {0}", cleanupFailure);
            }

            ExceptionDispatchInfo.Capture(testFailure).Throw();
        }

        if (cleanupFailure is not null)
            ExceptionDispatchInfo.Capture(cleanupFailure).Throw();
    }


PROPOSED SAMPLE — no verdict: core-001:unit:eb98b78558a307d8 / Piv / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivSessionContentionTests.cs:78–90
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     Provisions a PIV slot with an EccP256 key under PIN policy Once and leaves the PIN verified,
    ///     so that a subsequent sign is gated on the security state this test suite is protecting.
    /// </summary>
    private static async Task ArmPinGatedSigningAsync(PivSession session, FirmwareVersion firmware)
    {
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(firmware));
        _ = await session.GenerateKeyAsync(Slot.Authentication, KeyType.P256, new KeyCreationOptions { PinPolicy = PinPolicy.Once });
        await session.VerifyPinAsync(DefaultPin);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:13db8df479930852 / Piv / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivSigningAlgorithmTests.cs:111–146
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "4.3.5")]
    public async Task SignRsa2048_WithSha1Hash_ProducesValidSignature(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        var publicKey = await session.GenerateKeyAsync(
            Slot.Signature,
            KeyType.Rsa2048,
            new KeyCreationOptions { PinPolicy = PinPolicy.Once });
        await session.VerifyPinAsync(DefaultPin);

        var dataToSign = "SHA-1 signing test"u8.ToArray();
#pragma warning disable CA5350 // SHA1 used intentionally for algorithm coverage test
        var hash = SHA1.HashData(dataToSign);
#pragma warning restore CA5350

        var paddedData = CreatePkcs1v15SigningPadding(Sha1DigestInfo, hash, CryptographicDefinitions.Keys.Rsa2048.KeySizeBytes);

        var signature = await session.SignOrDecryptAsync(
            Slot.Signature,
            KeyType.Rsa2048,
            paddedData);

        Assert.Equal(CryptographicDefinitions.Keys.Rsa2048.KeySizeBytes, signature.Length);

        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(((RSAPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
#pragma warning disable CA5350
        Assert.True(rsa.VerifyData(dataToSign, signature.Span, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1));
#pragma warning restore CA5350
    }


PROPOSED SAMPLE — no verdict: core-001:unit:ce8af4db9c07f5ad / Piv / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivSigningAlgorithmTests.cs:146–177
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "4.3.5")]
    public async Task SignRsa2048_WithSha384Hash_ProducesValidSignature(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        var publicKey = await session.GenerateKeyAsync(
            Slot.Signature,
            KeyType.Rsa2048,
            new KeyCreationOptions { PinPolicy = PinPolicy.Once });
        await session.VerifyPinAsync(DefaultPin);

        var dataToSign = "SHA-384 signing test"u8.ToArray();
        var hash = SHA384.HashData(dataToSign);

        var paddedData = CreatePkcs1v15SigningPadding(Sha384DigestInfo, hash, CryptographicDefinitions.Keys.Rsa2048.KeySizeBytes);

        var signature = await session.SignOrDecryptAsync(
            Slot.Signature,
            KeyType.Rsa2048,
            paddedData);

        Assert.Equal(CryptographicDefinitions.Keys.Rsa2048.KeySizeBytes, signature.Length);

        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(((RSAPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
        Assert.True(rsa.VerifyData(dataToSign, signature.Span, HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:febe7cd8e74ac70c / Piv / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivSigningAlgorithmTests.cs:177–208
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "4.3.5")]
    public async Task SignRsa2048_WithSha512Hash_ProducesValidSignature(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        var publicKey = await session.GenerateKeyAsync(
            Slot.Signature,
            KeyType.Rsa2048,
            new KeyCreationOptions { PinPolicy = PinPolicy.Once });
        await session.VerifyPinAsync(DefaultPin);

        var dataToSign = "SHA-512 signing test"u8.ToArray();
        var hash = SHA512.HashData(dataToSign);

        var paddedData = CreatePkcs1v15SigningPadding(Sha512DigestInfo, hash, CryptographicDefinitions.Keys.Rsa2048.KeySizeBytes);

        var signature = await session.SignOrDecryptAsync(
            Slot.Signature,
            KeyType.Rsa2048,
            paddedData);

        Assert.Equal(CryptographicDefinitions.Keys.Rsa2048.KeySizeBytes, signature.Length);

        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(((RSAPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
        Assert.True(rsa.VerifyData(dataToSign, signature.Span, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:d36b5c72a45f3b4c / Piv / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivSlotOverwriteTests.cs:49–103
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    /// Generate RSA 2048 in a slot, then overwrite with ECC P-256. Verify via
    /// slot metadata that the algorithm changed, and that the new ECC key is
    /// functional for signing.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.3.0")]
    public async Task GenerateKey_RsaThenEcc_OverwritesWithEcc(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        // Step 1: Generate RSA 2048 key
        var rsaPublicKey = await session.GenerateKeyAsync(
            Slot.Authentication,
            KeyType.Rsa2048);

        Assert.NotNull(rsaPublicKey);
        Assert.IsType<RSAPublicKey>(rsaPublicKey);

        // Verify metadata shows RSA 2048
        var metadataRsa = await session.GetSlotMetadataAsync(Slot.Authentication);
        Assert.NotNull(metadataRsa);
        Assert.Equal(KeyType.Rsa2048, metadataRsa.Value.KeyType);

        // Step 2: Overwrite with ECC P-256
        var eccPublicKey = await session.GenerateKeyAsync(
            Slot.Authentication,
            KeyType.P256);

        Assert.NotNull(eccPublicKey);
        Assert.IsType<ECPublicKey>(eccPublicKey);

        // Verify metadata now shows ECC P-256
        var metadataEcc = await session.GetSlotMetadataAsync(Slot.Authentication);
        Assert.NotNull(metadataEcc);
        Assert.Equal(KeyType.P256, metadataEcc.Value.KeyType);

        // Verify the ECC key is functional
        await session.VerifyPinAsync(DefaultPin);
        var hash = SHA256.HashData("overwrite test"u8);
        var signature = await session.SignOrDecryptAsync(
            Slot.Authentication,
            KeyType.P256,
            hash);

        Assert.False(signature.IsEmpty);

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportSubjectPublicKeyInfo(((ECPublicKey)eccPublicKey).ExportSubjectPublicKeyInfo(), out _);
        Assert.True(ecdsa.VerifyHash(hash, signature.Span, DSASignatureFormat.Rfc3279DerSequence));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:79aade9dc99cb132 / Piv / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivSlotOverwriteTests.cs:103–181
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    /// Generate ECC P-256 in a slot, then overwrite with RSA 2048. Verify via
    /// slot metadata that the algorithm changed, and that the new RSA key is
    /// functional for signing.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.3.0")]
    public async Task GenerateKey_EccThenRsa_OverwritesWithRsa(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        // Step 1: Generate ECC P-256 key
        var eccPublicKey = await session.GenerateKeyAsync(
            Slot.Authentication,
            KeyType.P256);

        Assert.NotNull(eccPublicKey);
        Assert.IsType<ECPublicKey>(eccPublicKey);

        // Verify metadata shows ECC P-256
        var metadataEcc = await session.GetSlotMetadataAsync(Slot.Authentication);
        Assert.NotNull(metadataEcc);
        Assert.Equal(KeyType.P256, metadataEcc.Value.KeyType);

        // Step 2: Overwrite with RSA 2048
        var rsaPublicKey = await session.GenerateKeyAsync(
            Slot.Authentication,
            KeyType.Rsa2048);

        Assert.NotNull(rsaPublicKey);
        Assert.IsType<RSAPublicKey>(rsaPublicKey);

        // Verify metadata now shows RSA 2048
        var metadataRsa = await session.GetSlotMetadataAsync(Slot.Authentication);
        Assert.NotNull(metadataRsa);
        Assert.Equal(KeyType.Rsa2048, metadataRsa.Value.KeyType);

        // Verify the RSA key is functional
        await session.VerifyPinAsync(DefaultPin);

        // SHA-256 DigestInfo prefix
        byte[] sha256DigestInfo =
        [
            0x30, 0x31, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86,
            0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x05,
            0x00, 0x04, 0x20
        ];

        var dataToSign = "overwrite RSA test"u8.ToArray();
        var hash = SHA256.HashData(dataToSign);

        // Build PKCS#1 v1.5 padding
        var padded = new byte[CryptographicDefinitions.Keys.Rsa2048.KeySizeBytes];
        padded[0] = 0x00;
        padded[1] = 0x01;
        var paddingLength = padded.Length - 3 - sha256DigestInfo.Length - hash.Length;
        for (var i = 2; i < 2 + paddingLength; i++)
        {
            padded[i] = 0xFF;
        }
        padded[2 + paddingLength] = 0x00;
        Array.Copy(sha256DigestInfo, 0, padded, 3 + paddingLength, sha256DigestInfo.Length);
        Array.Copy(hash, 0, padded, 3 + paddingLength + sha256DigestInfo.Length, hash.Length);

        var signature = await session.SignOrDecryptAsync(
            Slot.Authentication,
            KeyType.Rsa2048,
            padded);

        Assert.Equal(CryptographicDefinitions.Keys.Rsa2048.KeySizeBytes, signature.Length);

        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(((RSAPublicKey)rsaPublicKey).ExportSubjectPublicKeyInfo(), out _);
        Assert.True(rsa.VerifyData(dataToSign, signature.Span, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:6d100fecb62d4abb / Piv / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/UserPresenceProbeTests.cs:27–48
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    [SkippableTheory]
    [WithYubiKey(MinFirmware = "5.7.0", ConnectionType = ConnectionType.SmartCard)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    public async Task InspectPrerequisites_ReadOnly_PrintsNonSecretState(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync(
            new SessionCreationOptions { PreferredConnectionType = ConnectionType.SmartCard });

        ManagementKeyMetadata managementKey = await session.GetManagementKeyMetadataAsync();
        PinMetadata pin = await session.GetPinMetadataAsync();
        SlotMetadata? retired20 = await session.GetSlotMetadataAsync(Slot.Retired20);

        output.WriteLine($"Firmware={state.FirmwareVersion}");
        output.WriteLine(
            $"ManagementKey: IsDefault={managementKey.IsDefault}, KeyType={managementKey.KeyType}, " +
            $"TouchPolicy={managementKey.TouchPolicy}");
        output.WriteLine(
            $"PIN: IsDefault={pin.IsDefault}, TotalRetries={pin.TotalRetries}, " +
            $"RetriesRemaining={pin.RetriesRemaining}");
        output.WriteLine($"Retired20: Occupied={retired20 is not null}");
    }


PROPOSED SAMPLE — no verdict: core-001:unit:92d416947d877101 / Piv / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/Authentication/PinOnlyWorkflowTests.cs:40–56
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Theory]
    [InlineData(0x20)]
    [InlineData(0x2C)]
    public async Task Enable_FailureStopsPersistentWorkflowAtFailedStep(int failedInstruction)
    {
        using var backend = new WorkflowBackend { FailedInstruction = (byte)failedInstruction };
        await using var session = await CreateSessionAsync(backend);

        await Assert.ThrowsAsync<ApduException>(() => session.SetPinOnlyModeAsync(PinOnlyMode.PinProtected,
            "123456"u8.ToArray(), backend.Key, TestContext.Current.CancellationToken));

        Assert.Equal(failedInstruction, backend.Commands[^1].Ins);
        Assert.DoesNotContain(backend.Commands, command => command.Ins == 0xCB);
        Assert.All(backend.SecretBuffers, buffer => Assert.All(buffer.ToArray(), b => Assert.Equal(0, b)));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:af3aaba139d19db7 / Piv / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/CatalogBehaviorTests.cs:58–75
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task UnknownManagementAlgorithm_IsInspectable_AndDoesNotBecomeAFactoryDefault()
    {
        byte[] raw = [0x01, 0x01, 0xF1, 0x02, 0x02, 0x00, 0x01, 0x05, 0x01, 0x00];
        var connection = new RecordingSmartCardConnection([0x90, 0x00], [5, 8, 0, 0x90, 0x00], [.. raw, 0x90, 0x00], [.. raw, 0x90, 0x00]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);

        var metadata = await session.GetManagementKeyMetadataAsync(TestContext.Current.CancellationToken);

        Assert.Null(session.ManagementKeyType);
        Assert.Null(metadata.KeyType);
        Assert.Equal(0xF1, metadata.RawAlgorithmId);
        Assert.Equal(raw, metadata.RawData.ToArray());
        await Assert.ThrowsAsync<NotSupportedException>(() => session.AuthenticateAsync(new byte[24], TestContext.Current.CancellationToken));
        Assert.Equal(4, connection.TransmittedCommands.Count);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:1b220d760b58a657 / Piv / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/PivSessionTests.cs:169–188
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Theory]
    [InlineData(0x6A, 0x88)]
    [InlineData(0x6D, 0x00)]
    public async Task CreateAsync_WhenReliableFirmwareManagementMetadataUnavailable_UsesAes192Fallback(
        int statusHigh,
        int statusLow)
    {
        var connection = new RecordingSmartCardConnection(
            OkResponse(),
            [0x05, 0x07, 0x00, 0x90, 0x00],
            [(byte)statusHigh, (byte)statusLow]);

        await using var session = await PivSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(KeyType.Aes192, session.ManagementKeyType);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:0cf32693862d0e51 / Piv / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/SessionConsistencyTests.cs:45–68
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Theory]
    [InlineData("generate")]
    [InlineData("move")]
    [InlineData("delete")]
    [InlineData("attest")]
    public async Task KeyOperation_UnsupportedInstruction_IsNotConfusedWithOtherDeviceErrors(string operation)
    {
        var connection = Connection([0x6D, 0x00], [0x6A, 0x80]);
        await using var session = await PivSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken);
        typeof(PivSession).GetField("_isAuthenticated", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(session, true);
        Task Invoke() => operation switch
        {
            "generate" => session.GenerateKeyAsync(Slot.Authentication, KeyType.P256, cancellationToken: TestContext.Current.CancellationToken),
            "move" => session.MoveKeyAsync(Slot.Authentication, Slot.Signature, TestContext.Current.CancellationToken),
            "delete" => session.DeleteKeyAsync(Slot.Authentication, TestContext.Current.CancellationToken),
            _ => session.AttestKeyAsync(Slot.Authentication, TestContext.Current.CancellationToken)
        };

        await Assert.ThrowsAsync<NotSupportedException>(Invoke);
        var exception = await Assert.ThrowsAsync<ApduException>(Invoke);
        Assert.True(exception.SW == 0x6A80);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:a7d45b5ed78ea182 / Piv / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/SessionConsistencyTests.cs:128–153
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Theory]
    [InlineData("pin")]
    [InlineData("puk")]
    [InlineData("management")]
    [InlineData("slot")]
    [InlineData("aes")]
    public async Task FirmwareGatedOperation_OldFirmware_RejectsBeforeTransmission(string operation)
    {
        var connection = Connection();
        await using var session = await PivSession.CreateAsync(connection,
            new SessionCreationOptions { FirmwareVersionOverride = new(4, 3, 0) }, TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<NotSupportedException>(() => operation switch
        {
            "pin" => session.GetPinMetadataAsync(TestContext.Current.CancellationToken),
            "puk" => session.GetPukMetadataAsync(TestContext.Current.CancellationToken),
            "management" => session.GetManagementKeyMetadataAsync(TestContext.Current.CancellationToken),
            "slot" => session.GetSlotMetadataAsync(Slot.Authentication, TestContext.Current.CancellationToken),
            _ => session.SetManagementKeyAsync(KeyType.Aes192, new byte[24], cancellationToken: TestContext.Current.CancellationToken)
        });

        Assert.False(session.IsSupported(Features.Metadata));
        Assert.Equal(3, connection.TransmittedCommands.Count);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:e4f10ec180cc9e83 / Piv / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/SlotMetadataExtensionsTests.cs:20–33
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    [Theory]
    [InlineData(KeyType.Rsa1024, true)]
    [InlineData(KeyType.Rsa2048, true)]
    [InlineData(KeyType.Rsa3072, true)]
    [InlineData(KeyType.Rsa4096, true)]
    [InlineData(KeyType.P256, false)]
    [InlineData(KeyType.P384, false)]
    [InlineData(KeyType.Ed25519, false)]
    [InlineData(KeyType.X25519, false)]
    public void IsRsa_ReturnsCorrectValue(KeyType keyType, bool expected)
    {
        Assert.Equal(expected, keyType.IsRsa());
    }


PROPOSED SAMPLE — no verdict: core-001:unit:e0620adee4adaf59 / Piv / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/SlotMetadataExtensionsTests.cs:84–103
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Theory]
    [InlineData(KeyType.Ed25519)]
    [InlineData(KeyType.X25519)]
    public void GetECDsaPublicKey_WithCurve25519Algorithm_ThrowsInvalidOperationException(KeyType keyType)
    {
        // Arrange: Create metadata with Curve25519 algorithm
        var metadata = new SlotMetadata(
            KeyType: keyType,
            PinPolicy: PinPolicy.Default,
            TouchPolicy: TouchPolicy.Default,
            IsGenerated: true,
            PublicKey: ReadOnlyMemory<byte>.Empty);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => metadata.GetECDsaPublicKey());
        Assert.Contains("Curve25519", exception.Message);
        Assert.Contains(keyType.ToString(), exception.Message);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:48aebb8b6acbc6ac / Piv / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/TouchNotificationTests.cs:75–100
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task SignOrDecryptAsync_AutoDetect_ReusesLoadedMetadata()
    {
        var connection = CreateInitializedConnection(
            SlotMetadataResponse(KeyType.P256, TouchPolicy.Always),
            CryptoResponse(0xAA));
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await CreateSessionAsync(connection, prompt);

        _ = await session.SignOrDecryptAsync(
            Slot.Authentication,
            new byte[32],
            TestContext.Current.CancellationToken);

        Assert.Equal(
            1,
            connection.TransmittedCommands.Count(
                command => command[1] == 0xF7 && command[3] == (byte)Slot.Authentication));
        AssertNotification(
            prompt,
            UserPresenceBasis.PolicyRequires,
            Slot.Authentication,
            UserPresenceOutcome.Completed);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:31264273bb8cc2f5 / Piv / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/Yubico.YubiKit.Piv.UnitTests.csproj:1–32
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <IsPackable>false</IsPackable>

    <!-- Enable Microsoft Testing Platform with xunit v3 -->
    <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
  </PropertyGroup>

  <ItemGroup>
    <!-- Package versions come from Directory.Packages.props -->
    <!-- When using Microsoft Testing Platform, DON'T use Microsoft.NET.Test.Sdk or xunit.runner.visualstudio -->
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="xunit.v3" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Yubico.YubiKit.Piv.csproj" />
    <ProjectReference Include="..\..\..\Tests.Shared\Yubico.YubiKit.Tests.Shared.csproj" />
  </ItemGroup>

</Project>

