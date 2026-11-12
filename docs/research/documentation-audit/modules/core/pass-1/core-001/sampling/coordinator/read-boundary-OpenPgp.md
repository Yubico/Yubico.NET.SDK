PROPOSED SAMPLE — no verdict: core-001:unit:869f7f727a2bacd9 / OpenPgp / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/OpenPgp/src/AlgorithmAttributes.cs:145–179
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     Creates EC attributes for the given key slot and concrete key representation.
    ///     The algorithm ID is automatically determined:
    ///     Ed25519 → EdDSA (0x16), X25519 → ECDH (0x12), DEC slot → ECDH (0x12), others → ECDSA (0x13).
    /// </summary>
    /// <remarks>
    ///     This factory chooses an identifier; the caller checks that the key/slot combination is supported
    ///     by the card. In particular, Ed25519 is a signing/authentication algorithm and X25519 is for agreement.
    /// </remarks>
    /// <param name="keyRef">The target slot, used to select ECDH for ordinary EC decryption keys.</param>
    /// <param name="keyType">A concrete EC key with modeled OpenPGP representation metadata.</param>
    /// <returns>Algorithm attributes using the standard EC import format.</returns>
    /// <exception cref="ArgumentException">The key has no modeled OpenPGP EC representation.</exception>
    public static EcAttributes Create(KeyRef keyRef, KeyType keyType)
    {
        var keyInfo = CryptographicDefinitions.Keys.Get(keyType);
        if (keyInfo.GetOpenPgpInfo()?.CurveOid is null)
        {
            throw new ArgumentException($"Key '{keyType}' has no modeled OpenPGP EC representation.", nameof(keyType));
        }
        return new()
        {
            AlgorithmId = keyInfo.Curve?.Form switch
            {
                CurveForm.Edwards => EddsaAlgorithmId,
                CurveForm.Montgomery => EcdhAlgorithmId,
                CurveForm.ShortWeierstrass => keyRef == KeyRef.Dec ? EcdhAlgorithmId : EcdsaAlgorithmId,
                _ => throw new ArgumentException($"Key '{keyType}' is not an OpenPGP EC construction.", nameof(keyType)),
            },
            KeyType = keyType,
            ImportFormat = EcImportFormat.Standard,
        };
    }


PROPOSED SAMPLE — no verdict: core-001:unit:d5e9bd9e80ac58c8 / OpenPgp / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/OpenPgp/src/OpenPgpSession.Keys.cs:101–128
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <inheritdoc />
    public async Task DeleteKeyAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _logger.LogInformation("Deleting key from {Slot}", keyRef);

        // Attribute switching is destructive and can leave partial state between writes.
        // NEO's vendor deletion path instead overwrites a key; this sequence needs separate
        // old-family qualification. Certificate and host metadata management remain separate.
        var rsa4096 = RsaAttributes.Create(RsaSize.Rsa4096);
        var rsa2048 = RsaAttributes.Create(RsaSize.Rsa2048);

        await PutDataAsync(keyRef.AlgorithmAttributesDo(), rsa4096.ToBytes(), cancellationToken)
            .ConfigureAwait(false);

        await SetAlgorithmAttributesAsync(keyRef, rsa2048, cancellationToken)
            .ConfigureAwait(false);

        // Refresh cached app data
        _appData = await GetApplicationRelatedDataCoreAsync(cancellationToken)
            .ConfigureAwait(false);
        _updatedUifs.Clear();
    }


PROPOSED SAMPLE — no verdict: core-001:unit:873ce41195e42542 / OpenPgp / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/OpenPgp/src/OpenPgpSession.Keys.cs:128–147
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <inheritdoc />
    public async Task<ReadOnlyMemory<byte>> GetPublicKeyAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _logger.LogDebug("Reading public key from {Slot}", keyRef);

        var crtBytes = keyRef.GetCrt();

        // GENERATE ASYMMETRIC KEY PAIR with P1=0x81 (read existing)
        var command = new ApduCommand(0x00, (int)Ins.GenerateAsym, 0x81, 0x00, crtBytes);
        var response = await TransmitWithResponseAsync(command, cancellationToken)
            .ConfigureAwait(false);

        return response.Data;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:758748a063d82837 / OpenPgp / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/OpenPgp/src/OpenPgpSession.cs:208–232
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <inheritdoc />
    public async Task<ReadOnlyMemory<byte>> GetChallengeAsync(
        int length,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);

        var command = new ApduCommand
        {
            Cla = 0x00,
            Ins = (byte)Ins.GetChallenge,
            P1 = 0x00,
            P2 = 0x00,
            Le = length,
        };

        _logger.LogDebug("GET CHALLENGE ({Length} bytes)", length);
        var response = await TransmitWithResponseAsync(command, cancellationToken)
            .ConfigureAwait(false);
        return response.Data;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:c7086050193e168d / OpenPgp / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/OpenPgp/examples/OpenPgpTool/Cli/Commands/OpenPgpCommand.cs:26–48
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    public sealed override async Task<int> ExecuteAsync(CommandContext context, TSettings settings)
    {
        try
        {
            var selection = await DeviceSelector.SelectDeviceAsync();
            if (selection is null)
            {
                OutputHelpers.WriteError("No YubiKey detected.");
                return 1;
            }

            OutputHelpers.WriteActiveDevice(selection.DisplayName);

            await using var session = await selection.Device.CreateOpenPgpSessionAsync();
            return await ExecuteCommandAsync(context, settings, session);
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError(ex.Message);
            return 1;
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:bcb85f9e4e5ea743 / OpenPgp / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/OpenPgp/src/OpenPgpKeyInfo.cs:74–77
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    internal static KeyType FromOidValueBytes(ReadOnlySpan<byte> oidValueBytes) => TryFromOidValueBytes(oidValueBytes, out var keyType)
        ? keyType : throw new ArgumentException("Unknown OpenPGP curve OID value octets.", nameof(oidValueBytes));


PROPOSED SAMPLE — no verdict: core-001:unit:4dbf70904bbff16f / OpenPgp / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/OpenPgp/src/OpenPgpSession.cs:74–84
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    // ── Constructor (private — use CreateAsync) ───────────────────────

    private OpenPgpSession(
        ISmartCardConnection connection,
        IUserPresencePrompt? userPresencePrompt = null)
        : base(connection, userPresencePrompt)
    {
        _logger = Logger;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:8b2a95f01db9bf2c / OpenPgp / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/OpenPgp/src/OpenPgpSession.cs:320–372
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private async Task<UserPresenceContext?> GetUserPresenceContextAsync(
        KeyRef keyRef,
        string operation,
        CancellationToken cancellationToken)
    {
        if (!IsUserPresenceNotificationEnabled || !IsSupported(FeatureUif))
            return null;

        UserPresenceBasis basis;
        try
        {
            Uif? cachedUif = _updatedUifs.TryGetValue(keyRef, out Uif updatedUif)
                ? updatedUif
                : keyRef switch
                {
                    KeyRef.Sig => _appData.Discretionary.UifSig,
                    KeyRef.Dec => _appData.Discretionary.UifDec,
                    KeyRef.Aut => _appData.Discretionary.UifAut,
                    KeyRef.Att => _appData.Discretionary.UifAtt,
                    _ => null
                };
            Uif uif = cachedUif ?? await GetUifAsync(keyRef, cancellationToken).ConfigureAwait(false);
            if (uif == Uif.Off)
                return null;

            basis = uif switch
            {
                Uif.On or Uif.Fixed => UserPresenceBasis.PolicyRequires,
                Uif.Cached or Uif.CachedFixed => UserPresenceBasis.PolicyMayRequire,
                _ => UserPresenceBasis.PolicyMayRequire
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // UIF is advisory metadata. A failed read must not replace the cryptographic operation.
            _logger.LogDebug(ex, "Could not read UIF for {Slot}; user presence may be required", keyRef);
            basis = UserPresenceBasis.PolicyMayRequire;
        }

        return new UserPresenceContext
        {
            Application = UserPresenceApplications.OpenPgp,
            Operation = operation,
            Scope = keyRef.ToString(),
            Basis = basis
        };
    }


PROPOSED SAMPLE — no verdict: core-001:unit:710814862611f595 / OpenPgp / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/OpenPgp/CLAUDE.md:192–202
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
## User-presence notifications

When `SessionCreationOptions.UserPresencePrompt` is supplied, signing, decryption, authentication, and
attestation use the relevant cached UIF before the cryptographic APDU. `On`/`Fixed` map to `PolicyRequires`,
`Cached`/`CachedFixed` map to `PolicyMayRequire`, and `Off` is silent. Attestation uses the attestation-key
UIF (`KeyRef.Att`). A failed UIF read falls back to `PolicyMayRequire` without replacing the cryptographic
operation; cancellation still propagates. Firmware without UIF support stays silent because it cannot configure
that policy. A direct GET DATA is issued only when the cached application-related data omitted that key's UIF.
Device APDU errors resolve as `Failed`; OpenPGP statuses including 6600 currently receive no `TimedOut`
classification. Terminal outcomes are `Completed`, `Cancelled`, and `Failed`. Decryption resolves successfully before materializing the caller's
plaintext copy; the raw response is cleared whether resolution succeeds or throws.



PROPOSED SAMPLE — no verdict: core-001:unit:33e76ca38aabdeb3 / OpenPgp / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/OpenPgp/README.md:297–358
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
## Sign and verify on the host

`SignAsync` and `AuthenticateAsync` accept messages/challenges with algorithm-specific handling:

| Slot algorithm | SDK input processing | Returned signature |
|---|---|---|
| RSA | Hash with the named algorithm; supply DigestInfo; card applies PKCS#1 v1.5 padding | Raw modulus-sized bytes |
| ECDSA | Hash with the named algorithm | DER sequence of integers `(r, s)` |
| Ed25519/EdDSA | Send the raw message; `hashAlgorithm` is unused | Raw 64 bytes |

RSA/ECDSA accept SHA-1, SHA-256, SHA-384 and SHA-512; choose a current algorithm suitable for your protocol.
Passing SHA-256 with Ed25519 still performs ordinary Ed25519, rather than selecting a prehashed variant.

This P-256 method reads the card's 7F49 public-key template, decodes field 86, and verifies the returned DER signature
on the host. The helper is included; the input message and PIN remain borrowed. Use a separately trusted public key
when verifying identity—the card read establishes which key verifies the signature, not who owns the device.

```csharp
static ECParameters DecodeP256PublicKey(ReadOnlyMemory<byte> template)
{
    using var outer = Tlv.Create(template.Span);
    if (outer.Tag != 0x7F49)
        throw new CryptographicException("Expected a card public-key template.");
    var point = TlvHelper.DecodeDictionary(outer.Value.Span)[0x86];
    if (point.Length != 65 || point.Span[0] != 0x04)
        throw new CryptographicException("Expected an uncompressed P-256 point.");
    return new ECParameters
    {
        Curve = ECCurve.NamedCurves.nistP256,
        Q = new ECPoint { X = point.Slice(1, 32).ToArray(), Y = point.Slice(33, 32).ToArray() }
    };
}

static async Task<bool> SignAndVerifyP256Async(
    IYubiKey device, ReadOnlyMemory<byte> userPin, ReadOnlyMemory<byte> message,
    CancellationToken cancellationToken)
{
    await using var session = await device.CreateOpenPgpSessionAsync(cancellationToken: cancellationToken);
    var attributes = await session.GetAlgorithmAttributesAsync(KeyRef.Sig, cancellationToken);
    if (attributes is not EcAttributes { KeyType: KeyType.P256 })
        throw new InvalidOperationException("Provision a P-256 signing key first.");
    using var verifier = ECDsa.Create(DecodeP256PublicKey(
        await session.GetPublicKeyAsync(KeyRef.Sig, cancellationToken)));
    await session.VerifyPinAsync(userPin, cancellationToken: cancellationToken);
    ReadOnlyMemory<byte> signature = await session.SignAsync(message, HashAlgorithmName.SHA256, cancellationToken);
    try
    {
        return verifier.VerifyData(message.Span, signature.Span, HashAlgorithmName.SHA256,
            DSASignatureFormat.Rfc3279DerSequence);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(MemoryMarshal.AsMemory(signature).Span);
    }
}
```

`PinPolicy.Always` requires User PIN verification for each signature; `Once` retains authorization until the card
access state ends. `UnverifyPinAsync` clears both User modes using the SDK's 5.6.0 check. For `AuthenticateAsync`,
verify with `extended: true`, then pass your challenge and verify the result with the Authentication public key.
Apply the current distinct-salt KDF limitation described above to that route.




PROPOSED SAMPLE — no verdict: core-001:unit:14baa3192f491ff3 / OpenPgp / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.IntegrationTests/OpenPgpAdvancedTests.cs:129–150
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    // ── RSA 4096 Key Generation ─────────────────────────────────────

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task GenerateRsaKey_4096_Succeeds(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                await session.GenerateKeyAsync(KeyRef.Sig, RsaAttributes.Create(RsaSize.Rsa4096));

                var attrs = await session.GetAlgorithmAttributesAsync(KeyRef.Sig);
                Assert.IsType<RsaAttributes>(attrs);
                Assert.Equal(4096, ((RsaAttributes)attrs).NLen);

                var pubKey = await session.GetPublicKeyAsync(KeyRef.Sig);
                Assert.True(pubKey.Length > 0);
            });


PROPOSED SAMPLE — no verdict: core-001:unit:62fb7fa48b9550d8 / OpenPgp / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.IntegrationTests/OpenPgpSessionTests.cs:102–124
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task ChangePin_AndRestore_Succeeds(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                var newPin = Encoding.UTF8.GetBytes("654321");

                // Change PIN
                await session.ChangePinAsync(DefaultUserPin, newPin);

                // Verify with new PIN
                await session.VerifyPinAsync(newPin);

                // Restore original PIN
                await session.ChangePinAsync(newPin, DefaultUserPin);

                // Verify restored PIN
                await session.VerifyPinAsync(DefaultUserPin);
            });


PROPOSED SAMPLE — no verdict: core-001:unit:6295ba49ce88cf2b / OpenPgp / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/OpenPgpPutKeyWireTests.cs:222–231
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static void RequireHostCurve(KeyType keyType)
    {
        var curveOid = keyType.GetKeyInfo().Curve?.NamedCurveOid
            ?? throw new InvalidOperationException("The vector requires a named ordinary curve.");
        try { using var provider = ECDsa.Create(ECCurve.CreateFromValue(curveOid)); }
        catch (PlatformNotSupportedException) { Assert.Skip($"Host provider does not implement curve {curveOid}."); }
        catch (CryptographicException) { Assert.Skip($"Host provider cannot generate curve {curveOid}."); }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:c8d2b18f767cdae2 / OpenPgp / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/OpenPgpPutKeyWireTests.cs:231–249
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     Returns (CRT bytes, 7F48 header bytes, 5F48 concatenated values) of the PUT DATA ODD, reassembled
    ///     from its command-chained APDUs.
    /// </summary>
    private static (byte[] Crt, byte[] Headers, byte[] Values) ParseImport(RecordingSmartCardConnection connection)
    {
        var imports = connection.TransmittedCommands.Where(c => c[1] == InsPutDataOdd).ToList();
        Assert.NotEmpty(imports);
        Assert.All(imports, import => Assert.Equal([0x3F, 0xFF], import[2..4]));
        byte[] data = [.. imports.SelectMany(import => ApduData(import).ToArray())];
        using var outer = Tlv.Create(data);
        Assert.Equal(0x4D, outer.Tag);
        var inner = outer.Value.Span;
        byte[] crt = inner[..2].ToArray();
        var dict = TlvHelper.DecodeDictionary(inner[2..]);
        return (crt, dict[0x7F48].ToArray(), dict[0x5F48].ToArray());
    }


PROPOSED SAMPLE — no verdict: core-001:unit:9a2795debe7eb7e0 / OpenPgp / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/OpenPgpSessionWireTests.cs:40–66
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task CreateAsync_TransmitsSelectVersionAndApplicationRelatedData()
    {
        var connection = CreateInitializedConnection();

        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(connection.TransmittedCommands.Count >= 3);
        Assert.Contains(connection.TransmittedCommands, command =>
            command.Length >= 5 &&
            command[1] == 0xA4 && // SELECT
            command[2] == 0x04 &&
            command[3] == 0x00 &&
            CommandData(command).SequenceEqual(ApplicationIds.OpenPgp.Span));
        Assert.Contains(connection.TransmittedCommands, command =>
            command.Length >= 4 &&
            command[1] == 0xF1); // GET VERSION
        Assert.Contains(connection.TransmittedCommands, command =>
            command.Length >= 4 &&
            command[1] == 0xCA && // GET DATA
            command[2] == 0x00 &&
            command[3] == 0x6E); // Application Related Data DO
    }


PROPOSED SAMPLE — no verdict: core-001:unit:8a438c950ba16617 / OpenPgp / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/OpenPgpSessionWireTests.cs:275–327
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Theory]
    [InlineData(Uif.On, UserPresenceBasis.PolicyRequires)]
    [InlineData(Uif.Fixed, UserPresenceBasis.PolicyRequires)]
    [InlineData(Uif.Cached, UserPresenceBasis.PolicyMayRequire)]
    [InlineData(Uif.CachedFixed, UserPresenceBasis.PolicyMayRequire)]
    [InlineData(Uif.Off, null)]
    public async Task SignAsync_MapsSignatureUifToNotification(
        Uif uif,
        UserPresenceBasis? expectedBasis)
    {
        var connection = CreateInitializedConnectionWithUifs(
            sig: uif,
            dec: null,
            aut: null,
            att: null,
            CryptoResponse());
        var prompt = new RecordingUserPresencePrompt(connection);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);
        int commandsBeforeOperation = connection.TransmittedCommands.Count;

        _ = await session.SignAsync(
            "message"u8.ToArray(),
            HashAlgorithmName.SHA256,
            TestContext.Current.CancellationToken);

        Assert.Equal(0, connection.TransmittedCommands.Count(command =>
            command[1] == 0xCA && command[2] == 0x00 && command[3] == (byte)DataObject.UifSig));

        if (expectedBasis is null)
        {
            Assert.Empty(prompt.Requested);
            Assert.Empty(prompt.Resolved);
            return;
        }

        var requested = Assert.Single(prompt.Requested);
        Assert.Equal("OpenPGP", requested.Context.Application);
        Assert.Equal(UserPresenceOperations.OpenPgp.Sign, requested.Context.Operation);
        Assert.Equal("Sig", requested.Context.Scope);
        Assert.Equal(expectedBasis, requested.Context.Basis);
        Assert.Equal(commandsBeforeOperation, requested.CommandCount);

        var resolved = Assert.Single(prompt.Resolved);
        Assert.Same(requested.Context, resolved.Context);
        Assert.Equal(UserPresenceOutcome.Completed, resolved.Outcome);
        Assert.Equal(CancellationToken.None, resolved.CancellationToken);
        Assert.Equal(commandsBeforeOperation + 1, resolved.CommandCount);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:15b19a2931c6434a / OpenPgp / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/OpenPgpSessionWireTests.cs:448–476
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task AttestKeyAsync_UsesAttestationUifAndResolvesFailure()
    {
        var connection = CreateInitializedConnectionWithUifs(
            sig: null,
            dec: null,
            aut: null,
            att: Uif.On,
            [0x6A, 0x80]);
        var prompt = new RecordingUserPresencePrompt(connection);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        _ = await Assert.ThrowsAsync<ApduException>(() =>
            session.AttestKeyAsync(KeyRef.Sig, TestContext.Current.CancellationToken));

        var requested = Assert.Single(prompt.Requested);
        Assert.Equal("Att", requested.Context.Scope);
        Assert.Equal(UserPresenceOperations.OpenPgp.AttestKey, requested.Context.Operation);
        Assert.Equal(UserPresenceBasis.PolicyRequires, requested.Context.Basis);
        Assert.Equal(UserPresenceOutcome.Failed, Assert.Single(prompt.Resolved).Outcome);
        Assert.DoesNotContain(connection.TransmittedCommands, command =>
            command[1] == 0xCA && command[3] == (byte)DataObject.UifAtt);
        Assert.Contains(connection.TransmittedCommands, command => command[1] == (byte)Ins.GetAttestation);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:24e90797dbd9ae65 / OpenPgp / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/OpenPgpSessionWireTests.cs:503–575
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Theory]
    [InlineData(KeyMutation.Generate)]
    [InlineData(KeyMutation.Import)]
    [InlineData(KeyMutation.Delete)]
    public async Task KeyMutationRefresh_ClearsSetUifPolicyOverride(KeyMutation mutation)
    {
        byte[][] trailingResponses = mutation switch
        {
            KeyMutation.Generate =>
            [
                OkResponse(),
                OkResponse(),
                OkResponse(),
                ApplicationRelatedDataResponse(Uif.Off),
                CryptoResponse()
            ],
            KeyMutation.Import =>
            [
                OkResponse(), // algorithm attributes
                OkResponse(), // import, chained in two short APDUs
                OkResponse(),
                OkResponse(),
                ApplicationRelatedDataResponse(Uif.Off),
                CryptoResponse()
            ],
            KeyMutation.Delete =>
            [
                OkResponse(),
                OkResponse(),
                OkResponse(),
                ApplicationRelatedDataResponse(Uif.Off),
                CryptoResponse()
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(mutation))
        };
        var connection = CreateInitializedConnectionWithUifs(
            Uif.Off,
            dec: null,
            aut: null,
            att: null,
            trailingResponses);
        var prompt = new RecordingUserPresencePrompt(connection);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        await session.SetUifAsync(KeyRef.Sig, Uif.On, TestContext.Current.CancellationToken);
        using var rsa = RSA.Create(2048);
        using var importKey = RSAPrivateKey.CreateFromParameters(rsa.ExportParameters(includePrivateParameters: true));
        await (mutation switch
        {
            KeyMutation.Generate => session.GenerateKeyAsync(
                KeyRef.Sig,
                RsaAttributes.Create(RsaSize.Rsa2048),
                TestContext.Current.CancellationToken),
            KeyMutation.Import => session.PutKeyAsync(
                KeyRef.Sig,
                importKey,
                TestContext.Current.CancellationToken),
            KeyMutation.Delete => session.DeleteKeyAsync(KeyRef.Sig, TestContext.Current.CancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(mutation))
        });
        _ = await session.SignAsync(
            "message"u8.ToArray(),
            HashAlgorithmName.SHA256,
            TestContext.Current.CancellationToken);

        Assert.Empty(prompt.Requested);
        Assert.Empty(prompt.Resolved);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:683eb679b38bf0b7 / OpenPgp / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/OpenPgpSessionWireTests.cs:674–697
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task DecryptAsync_WhenResolutionFailsAfterSuccess_PropagatesResolutionException()
    {
        var expected = new InvalidOperationException("resolution failed");
        var connection = CreateInitializedConnectionWithUifs(
            sig: null,
            dec: Uif.On,
            aut: null,
            att: null,
            CryptoResponse());
        var prompt = new RecordingUserPresencePrompt(connection, resolutionException: expected);
        await using var session = await OpenPgpSession.CreateAsync(
            connection,
            new SessionCreationOptions { UserPresencePrompt = prompt },
            TestContext.Current.CancellationToken);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            session.DecryptAsync(new byte[] { 0x01, 0x02 }, TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(UserPresenceOutcome.Completed, Assert.Single(prompt.Resolved).Outcome);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:f654c14528cd489f / OpenPgp / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/OpenPgpSessionWireTests.cs:986–1051
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static byte[] BuildApplicationRelatedData(Uif? sig, Uif? dec, Uif? aut, Uif? att)
    {
        // AID: D276000124010304000612345678 (OpenPGP v3.4, Yubico, serial 12345678).
        byte[] aid = [0xD2, 0x76, 0x00, 0x01, 0x24, 0x01, 0x03, 0x04, 0x00, 0x06, 0x12, 0x34, 0x56, 0x78];
        byte[] historicalBytes = [0x00, 0x73, 0x00, 0x01, 0x80, 0x05, 0x90, 0x00];
        byte[] rsa2048Attributes = [0x01, 0x08, 0x00, 0x00, 0x11, 0x00];
        byte[] extendedCapabilities = [0x75, 0x00, 0x00, 0xFF, 0x04, 0x80, 0x00, 0xFF, 0x00, 0x00];
        byte[] pwStatus = [0x00, 0x7F, 0x7F, 0x7F, 0x03, 0x00, 0x03];
        var fingerprints = new byte[60];
        var caFingerprints = new byte[60];
        var generationTimes = new byte[12];

        var discretionaryTlvs = new List<Tlv>
        {
            new(0xC0, extendedCapabilities),
            new(0xC1, rsa2048Attributes),
            new(0xC2, rsa2048Attributes),
            new(0xC3, rsa2048Attributes),
            new(0xC4, pwStatus),
            new(0xC5, fingerprints),
            new(0xC6, caFingerprints),
            new(0xCD, generationTimes),
        };
        AddUif(discretionaryTlvs, DataObject.UifSig, sig);
        AddUif(discretionaryTlvs, DataObject.UifDec, dec);
        AddUif(discretionaryTlvs, DataObject.UifAut, aut);
        AddUif(discretionaryTlvs, DataObject.UifAtt, att);

        byte[] discretionaryContent;
        try
        {
            discretionaryContent = TlvHelper.EncodeList(discretionaryTlvs.ToArray()).ToArray();
        }
        finally
        {
            foreach (var tlv in discretionaryTlvs)
            {
                tlv.Dispose();
            }
        }

        var outerTlvs = new Tlv[]
        {
            new(0x4F, aid),
            new(0x5F52, historicalBytes),
            new(0x73, discretionaryContent),
        };

        byte[] outerContent;
        try
        {
            outerContent = TlvHelper.EncodeList(outerTlvs).ToArray();
        }
        finally
        {
            foreach (var tlv in outerTlvs)
            {
                tlv.Dispose();
            }
        }

        using var result = new Tlv(0x6E, outerContent);
        return result.AsMemory().ToArray();
    }


PROPOSED SAMPLE — no verdict: core-001:unit:aceb51379b5ee76b / OpenPgp / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/PrivateKeyTemplateTests.cs:90–121
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void RsaCrtKeyTemplate_ToBytes_ContainsAllComponents()
    {
        byte[] e = [0x01, 0x00, 0x01];
        byte[] p = [0xAA];
        byte[] q = [0xBB];
        byte[] iqmp = [0xCC];
        byte[] dmp1 = [0xDD];
        byte[] dmq1 = [0xEE];
        byte[] n = [0xFF];

        var template = new RsaCrtKeyTemplate(e, p, q, iqmp, dmp1, dmq1, n);
        var result = template.ToBytes(KeyRef.Sig);

        using var outer = Tlv.Create(result);
        Assert.Equal(0x4D, outer.Tag);

        var dict = TlvHelper.DecodeDictionary(outer.Value.Span);

        // Values = e + p + q + iqmp + dmp1 + dmq1 + n
        var values = dict[0x5F48].ToArray();
        var expectedLen = e.Length + p.Length + q.Length + iqmp.Length + dmp1.Length + dmq1.Length + n.Length;
        Assert.Equal(expectedLen, values.Length);

        // Headers should contain tags 0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97
        var headers = dict[0x7F48].Span;
        var headerDescriptors = ParseHeaderDescriptors(headers);
        var tags = headerDescriptors.Select(h => h.Tag).ToArray();
        Assert.Equal([0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97], tags);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:7ad9dd6d6d1845d6 / OpenPgp / type-declarations
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/OpenPgp/src/IOpenPgpSession.cs:34–35
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
public interface IOpenPgpSession : IApplicationSession
{
