PROPOSED SAMPLE — no verdict: core-001:unit:7d73ad99b0542ab5 / YubiHsm / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiHsm/src/HsmAuthSession.cs:950–972
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     Checks an APDU response for the 0x63Cx retry failure pattern and throws
    ///     <see cref="HsmAuthRetryException" /> with retry information if detected.
    /// </summary>
    private static void ThrowOnRetryFailure(ApduResponse response, ApduCommand command, string errorContext)
    {
        var retries = ExtractRetries(response.SW);
        if (retries is null)
            return;

        throw new HsmAuthRetryException(
            retries.Value,
            $"{errorContext}, {retries} attempt(s) remaining (SW=0x{response.SW:X4})")
        {
            SW = response.SW,
            Cla = command.Cla,
            Ins = command.Ins,
            P1 = command.P1,
            P2 = command.P2
        };
    }


PROPOSED SAMPLE — no verdict: core-001:unit:be6deb0bde5274d7 / YubiHsm / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiHsm/examples/HsmAuthTool/Cli/Commands/CredentialsCommand.cs:310–375
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    // ── credentials generate LABEL ───────────────────────────────────────────

    private static async Task<int> GenerateAsync(string[] args, CancellationToken cancellationToken)
    {
        var label = CommandArgs.GetPositional(args, subcommandCount: 2);
        if (label is null)
        {
            OutputHelpers.WriteError("Missing required argument: LABEL");
            AnsiConsole.MarkupLine(
                "[bold]Usage:[/] HsmAuthTool credentials generate LABEL [[options]]");
            return 1;
        }

        var touchRequired = CommandArgs.HasFlag(args, "--touch");

        var mgmtKey = ResolveManagementKey(args);
        if (mgmtKey is null)
        {
            return 1;
        }

        try
        {
            // Resolved inside the try so an aborted or rejected prompt still zeros mgmtKey.
            using var credentialPassword = PinPrompt.Resolve(
                CommandArgs.GetOption(args, "--credential-password"), "Credential password");
            if (credentialPassword is null)
            {
                OutputHelpers.WriteError("Credential password is required.");
                return 1;
            }

            var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
            if (selection is null)
            {
                return 1;
            }

            await using var session = await selection.Device.CreateHsmAuthSessionAsync(
                cancellationToken: cancellationToken);

            await session.GenerateCredentialAsymmetricAsync(
                mgmtKey,
                label,
                credentialPassword.Memory,
                touchRequired,
                cancellationToken);

            OutputHelpers.WriteSuccess(
                $"Asymmetric credential '{label}' generated on device.");
            OutputHelpers.WriteInfo("Private key was generated on-device and never leaves the YubiKey.");

            // Export credential PK-OCE for enrollment in the matching HSM authentication object.
            // The HSM's own trusted static public key is obtained separately through HSM provisioning.
            var publicKey = await session.GetPublicKeyAsync(label, cancellationToken);
            OutputHelpers.WriteHex("Public key", publicKey.Span);

            return 0;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(mgmtKey);
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:857ca96c5023da44 / YubiHsm / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiHsm/examples/HsmAuthTool/Cli/Menus/ManagementKeyMenu.cs:11–24
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Change Management Key");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null) return;

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateHsmAuthSessionAsync(
            cancellationToken: cancellationToken);
        await ChangeManagementKey.RunAsync(session, cancellationToken);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:969ca35d72262c60 / YubiHsm / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiHsm/CLAUDE.md:159–173
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
### Unit Tests
Located in `tests/Yubico.YubiKit.YubiHsm.UnitTests/`. Pure helpers and recording-connection tests run without hardware:

- `SessionKeysTests` — Parse/dispose/zeroing behavior
- `CredentialPasswordTests` — UTF-8 encoding, padding, validation
- `LabelValidationTests` — Label length and encoding constraints
- `RetryExtractionTests` — Status word 0x63Cx parsing
- `HsmAuthCredentialTests` — Label ordering and all-property record equality
- `Pbkdf2DerivationTests` — Derivation parameters, split shape and repeatability
- `HsmAuthSessionByteLevelTests` — Headers, selected TLV bytes/order, context guards and reset override policy
- `HsmAuthRetryExceptionTests` — Typed retry translation for nine operation paths, excluding GET_CHALLENGE
- `TouchNotificationTests` — Recording-prompt policy mapping, request/resolution lifecycle, and no-hardware CALCULATE ordering

**Run tests:** `dotnet toolchain.cs test` (never `dotnet test` directly)




PROPOSED SAMPLE — no verdict: core-001:unit:e2c611eac31ccf1c / YubiHsm / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiHsm/README.md:1–10
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
# Yubico.YubiKit.YubiHsm

The YubiHSM Auth applet stores the credentials used to authenticate to a YubiHSM 2 hardware security module, so
the long-lived HSM authentication keys live on a YubiKey instead of on the host. This package manages those
credentials and derives the S-ENC, S-MAC, and S-RMAC keys for an HSM session. Your application uses an HSM
library to provision the HSM, complete authentication, and exchange protected commands through its connector.

> The v2 SDK is a pre-release alpha; see the [repository README](../../README.md) for the current status and
> constraints.




PROPOSED SAMPLE — no verdict: core-001:unit:673587f212268a6c / YubiHsm / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiHsm/tests/Yubico.YubiKit.YubiHsm.UnitTests/HsmAuthRetryExceptionTests.cs:160–181
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task ChangeCredentialPasswordAdminAsync_WhenManagementKeyRetryFailure_ExposesRetriesRemaining()
    {
        // SW 63C2: management-key verification failed for admin password change, 2 attempts remaining.
        var connection = CreateInitializedConnection([0x63, 0xC2]);
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            options: new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 8, 0) },
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<HsmAuthRetryException>(() => session.ChangeCredentialPasswordAdminAsync(
            Sequence(0x10, 16),
            "cred",
            "newpass"u8.ToArray(),
            TestContext.Current.CancellationToken));

        Assert.Equal(2, exception.RetriesRemaining);
        Assert.Equal(unchecked((short)0x63C2), exception.SW);
        Assert.Contains("Management key verification failed", exception.Message);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:d5c991345bf8977b / YubiHsm / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiHsm/tests/Yubico.YubiKit.YubiHsm.UnitTests/HsmAuthSessionByteLevelTests.cs:32–67
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task PutCredentialSymmetricAsync_TransmitsOrderedCredentialTlvs()
    {
        var connection = CreateInitializedConnection(OkResponse());
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            options: new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 4, 3) },
            cancellationToken: TestContext.Current.CancellationToken);

        await session.PutCredentialSymmetricAsync(
            Sequence(0x10, 16),
            "cred",
            Sequence(0x20, 16),
            Sequence(0x30, 16),
            "pass"u8.ToArray(),
            touchRequired: true,
            TestContext.Current.CancellationToken);

        var command = LastCommand(connection);
        // APDU header: INS=PUT, P1/P2 default to 0 for YubiHSM Auth credential storage.
        Assert.Equal(0x01, command[1]);
        Assert.Equal(0x00, command[2]);
        Assert.Equal(0x00, command[3]);
        // Data: management key(7B), label(71), algorithm(74), K-ENC(75), K-MAC(76), password(73), touch(7A).
        Assert.Equal([
            0x7B, 0x10, .. Sequence(0x10, 16),
            0x71, 0x04, (byte)'c', (byte)'r', (byte)'e', (byte)'d',
            0x74, 0x01, (byte)HsmAuthAlgorithm.Aes128YubicoAuthentication,
            0x75, 0x10, .. Sequence(0x20, 16),
            0x76, 0x10, .. Sequence(0x30, 16),
            0x73, 0x10, (byte)'p', (byte)'a', (byte)'s', (byte)'s', .. new byte[12],
            0x7A, 0x01, 0x01
        ], CommandData(command).ToArray());
    }


PROPOSED SAMPLE — no verdict: core-001:unit:75649a8bd0ae3bb6 / YubiHsm / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiHsm/tests/Yubico.YubiKit.YubiHsm.UnitTests/HsmAuthSessionByteLevelTests.cs:98–121
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Theory]
    [InlineData(15)]
    [InlineData(17)]
    public async Task CalculateSessionKeysSymmetricAsync_InvalidContextLength_ThrowsBeforeDeviceIo(int length)
    {
        var connection = CreateInitializedConnection(SessionKeyResponse());
        await using var session = await HsmAuthSession.CreateAsync(
            connection,
            options: new SessionCreationOptions { FirmwareVersionOverride = new FirmwareVersion(5, 4, 3) },
            cancellationToken: TestContext.Current.CancellationToken);
        var commandCount = connection.TransmittedCommands.Count;

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            session.CalculateSessionKeysSymmetricAsync(
                "cred",
                new byte[length],
                "pass"u8.ToArray(),
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal("context", exception.ParamName);
        Assert.Equal(commandCount, connection.TransmittedCommands.Count);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:56ac675d5c5a5b82 / YubiHsm / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiHsm/tests/Yubico.YubiKit.YubiHsm.UnitTests/SessionKeysTests.cs:59–91
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void Dispose_ZerosAllKeyMaterial()
    {
        // Arrange
        var response = new byte[48];
        for (var i = 0; i < 48; i++)
            response[i] = 0xFF;

        var keys = SessionKeys.Parse(response);

        // Capture the arrays owned by the parsed result so disposal zeroing is directly observable.
        byte[] sEnc = GetOwnedKeyBuffer(keys, "_sEnc");
        byte[] sMac = GetOwnedKeyBuffer(keys, "_sMac");
        byte[] sRmac = GetOwnedKeyBuffer(keys, "_sRmac");

        // Verify keys are non-zero before disposal
        Assert.Contains(sEnc, b => b != 0);
        Assert.Contains(sMac, b => b != 0);
        Assert.Contains(sRmac, b => b != 0);

        // Act
        keys.Dispose();

        // Assert - accessing after disposal should throw
        Assert.Throws<ObjectDisposedException>(() => _ = keys.SEnc);
        Assert.Throws<ObjectDisposedException>(() => _ = keys.SMac);
        Assert.Throws<ObjectDisposedException>(() => _ = keys.SRmac);
        Assert.All(sEnc, b => Assert.Equal(0, b));
        Assert.All(sMac, b => Assert.Equal(0, b));
        Assert.All(sRmac, b => Assert.Equal(0, b));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:56b56f5f5ddac7fd / YubiHsm / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/YubiHsm/tests/Yubico.YubiKit.YubiHsm.UnitTests/TouchNotificationTests.cs:43–65
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task CalculateSessionKeysAsymmetricAsync_WhenCredentialRequiresTouch_RequestsAndResolvesCompleted()
    {
        var connection = CreateInitializedConnection(
            ListResponse("cred", HsmAuthAlgorithm.EcP256YubicoAuthentication, touchByte: 0x01, counter: 3),
            SessionKeyResponse());
        var prompt = new RecordingUserPresencePrompt();
        await using var session = await CreateSessionAsync(connection, prompt, new FirmwareVersion(5, 6, 0));

        using var keys = await session.CalculateSessionKeysAsymmetricAsync(
            "cred",
            Sequence(0x40, 130),
            Sequence(0x60, 65),
            "pass"u8.ToArray(),
            Sequence(0x70, 8),
            TestContext.Current.CancellationToken);

        Assert.Equal(Sequence(0xA0, 16), keys.SEnc.ToArray());
        AssertNotification(prompt, UserPresenceBasis.PolicyRequires, UserPresenceOutcome.Completed,
            UserPresenceOperations.YubiHsmAuth.CalculateSessionKeysAsymmetric);
    }
