PROPOSED SAMPLE — no verdict: core-001:unit:1bf9d5c2d8ded722 / Oath / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Oath/examples/OathTool/Commands/AccountsCommand.cs:166–217
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    /// Deletes a credential by name.
    /// </summary>
    public static async Task<int> DeleteAsync(
        string name,
        bool force = false,
        IMemoryOwner<byte>? passwordBytes = null,
        CancellationToken cancellationToken = default,
        OathToolServices? services = null)
    {
        services ??= OathToolServices.Default;
        var result = await OathSessionHelper.CreateUnlockedSessionAsync(passwordBytes, cancellationToken, services);
        if (result is null)
        {
            return 1;
        }

        var session = result;
        await using (session)
        {
            var credential = await FindCredentialAsync(session, name, cancellationToken);
            if (credential is null)
            {
                return 1;
            }

            var displayName = OutputHelpers.FormatCredentialName(credential.Issuer, credential.Name);

            if (!force)
            {
                if (!await services.Confirm($"Delete credential {displayName}?", cancellationToken))
                {
                    OutputHelpers.WriteInfo("Delete cancelled.");
                    return ExitCode.UserCancelled;
                }
            }

            try
            {
                await session.DeleteCredentialAsync(credential, cancellationToken);
                OutputHelpers.WriteSuccess($"Credential deleted: {displayName}");
                return 0;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                OutputHelpers.WriteError($"Failed to delete credential: {ex.Message}");
                return 1;
            }
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:ab98f777e0e5a2e5 / Oath / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Oath/src/CredentialData.cs:365–372
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     Parses a Base32-encoded key string, supporting ASCII case, literal spaces, and complete or absent padding.
    /// </summary>
    /// <param name="key">The Base32-encoded string.</param>
    /// <returns>Independently owned decoded bytes, which the caller must clear.</returns>
    internal static byte[] ParseBase32Key(string key) => Base32.Decode(key.AsSpan());


PROPOSED SAMPLE — no verdict: core-001:unit:1da1115a6a2d8624 / Oath / XML-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Oath/src/OathSession.cs:827–839
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <inheritdoc />
    public async Task UnsetKeyAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        // Send SET CODE with empty key TLV to clear the access key
        using var keyTlv = new Tlv(OathConstants.TagKey, ReadOnlySpan<byte>.Empty);
        var command = new ApduCommand(0x00, OathConstants.InsSetCode, 0x00, 0x00, keyTlv.AsMemory());
        await SendAsync(command, cancellationToken).ConfigureAwait(false);
        IsPasswordProtected = false;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:054ed96fe3237098 / Oath / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Oath/examples/OathTool/Cli/OathToolServices.cs:17–33
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    public async Task<IOathSession?> OpenSession(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = await openSessionAsync(cancellationToken);
        if (cancellationToken.IsCancellationRequested)
        {
            if (session is not null)
            {
                await session.DisposeAsync();
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        return session;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:d8dd51ea527f6add / Oath / implementation-state-and-internal-contracts
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Oath/src/Backend/OathBackend.cs:72–78
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public Task<ApduResponse> SendAsync(
        ApduCommand command,
        bool throwOnError = true,
        CancellationToken cancellationToken = default) =>
        _protocol.TransmitAndReceiveAsync(command, throwOnError, cancellationToken);


PROPOSED SAMPLE — no verdict: core-001:unit:1b7bc942bb2b181f / Oath / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Oath/CLAUDE.md:5–18
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
## Module Context

The OATH module implements TOTP (RFC 6238) and HOTP (RFC 4226) one-time password management for YubiKey devices. It communicates via ISO 7816-4 APDUs through the SmartCard transport only.

**Key characteristics:**
- **SmartCard-only** — no FIDO/OTP backend abstraction needed (like SecurityDomain)
- **No general session-open firmware floor** — SDK gates use the effective version for rename and secure-channel creation; touch and SHA-512 device requirements are not preflight-checked. See the table below.
- **Sealed session** — `OathSession` is sealed, extended via `IOathSession` interface
- **Two-phase init** — private constructor + static `CreateAsync` (matches Management/SecurityDomain)
- **Credential policy ownership** — `CredentialData.RequireTouch` travels with creation policy
- **Read-only aggregate results** — `CalculateAllAsync` returns `IReadOnlyDictionary<Credential, Code?>`
- **User-presence notifications** — individual calculations notify only when `Credential.TouchRequired` is `true`;
  `CalculateAllAsync` remains silent because it reports touch-required credentials without waiting on them




PROPOSED SAMPLE — no verdict: core-001:unit:bd9fbdc6508adac7 / Oath / narrative-examples-and-maps
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Oath/README.md:64–88
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
### Add a TOTP credential from an otpauth URI

`ParseUri` returns a `CredentialData` that owns its decoded secret bytes. Dispose it after use.
`PutCredentialAsync` adds the credential or replaces one with the same raw credential ID, produced by `GetId()`.
Set `requireTouch: true` to require touch for individual calculations (firmware 4.2.4+). Bulk calculations return
`null` for these entries, as described below.

`ParseUri` and PUT require digits from 6 through 8, a positive TOTP period, and a HOTP counter from 0 through
`Int32.MaxValue`. A missing HOTP counter defaults to zero. HOTP ignores the period; TOTP ignores the counter.
In a URI, a query-string issuer takes precedence over the label issuer. Numeric query values must still parse
as `Int32`, including fields the selected OATH type ignores.

Base32 secrets accept ASCII upper/lower case and literal spaces, with either no padding or complete trailing
padding. Invalid characters, impossible terminal lengths, and incomplete or embedded padding are rejected;
unused bits in the last symbol remain permissive. OATH requires nonempty decoded material, even though the
shared Core decoder accepts empty input. Short nonempty seeds retain the SDK's 14-byte storage padding;
that padding is not an entropy requirement.

```csharp
using var credentialData = CredentialData.ParseUri(
    "otpauth://totp/GitHub:user@example.com?secret=JBSWY3DPEHPK3PXP&issuer=GitHub", requireTouch: false);
await using var session = await device.CreateOathSessionAsync();
await session.PutCredentialAsync(credentialData);
```




PROPOSED SAMPLE — no verdict: core-001:unit:55b07354d429a6be / Oath / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Oath/tests/Yubico.YubiKit.Oath.IntegrationTests/OathHashAlgorithmTests.cs:71–90
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
    public async Task TotpCredential_Sha512_CalculatesCode(YubiKeyTestState state) =>
        await state.WithOathSessionAsync(async session =>
        {
            using var credData = CreateCredential("sha512@example.com", OathHashAlgorithm.Sha512, "Sha512Test");

            await session.PutCredentialAsync(credData, cancellationToken: NewToken());

            var credentials = await session.ListCredentialsAsync(NewToken());
            Assert.Single(credentials);

            var credential = credentials[0];
            long timestamp = 1704067200;
            var code = await session.CalculateCodeAsync(credential, timestamp, NewToken());
            Assert.NotNull(code);
            Assert.Equal(6, code.Value.Length);
        }, cancellationToken: NewToken());


PROPOSED SAMPLE — no verdict: core-001:unit:61c7599de7ec9de1 / Oath / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Oath/tests/Yubico.YubiKit.Oath.IntegrationTests/OathLifecycleTimeTests.cs:279–290
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
        {
            ConnectCount++;
            if (ConnectCount > 1)
            {
                Assert.Equal(1, connections[ConnectCount - 2].Inner.DisposeCount);
                CleanupToken = cancellationToken;
            }
            return Task.FromResult(Assert.IsAssignableFrom<TConnection>(connections[ConnectCount - 1]));
        }


PROPOSED SAMPLE — no verdict: core-001:unit:d4ac23565d565892 / Oath / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Oath/tests/Yubico.YubiKit.Oath.IntegrationTests/OathProtocolResponseTests.cs:227–250
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
        {
            ConnectCount++;
            if (cancellationToken.IsCancellationRequested)
            {
                OperationFailure = new OperationCanceledException("Operation token expired after SET CODE.", cancellationToken);
                throw OperationFailure;
            }

            IConnection connection;
            if (ConnectCount == 1)
                connection = new RecordingSmartCardConnection(SelectResponse(), [0x90, 0]);
            else if (ConnectCount == 2)
                connection = new CancelAfterSetCodeConnection(operation);
            else
            {
                CleanupToken = cancellationToken;
                connection = ConnectCount == 4 ? CleanupConnection : new RecordingSmartCardConnection(SelectResponse());
            }
            return Task.FromResult(Assert.IsAssignableFrom<TConnection>(connection));
        }


PROPOSED SAMPLE — no verdict: core-001:unit:74a5f93a6223fb56 / Oath / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Oath/tests/Yubico.YubiKit.Oath.IntegrationTests/OathProtocolResponseTests.cs:258–265
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(ReadOnlyMemory<byte> command, CancellationToken cancellationToken = default)
        {
            var response = await _inner.TransmitAndReceiveAsync(command, cancellationToken);
            if (command.Span[1] == 0x03)
                operation.Cancel();
            return response;
        }


PROPOSED SAMPLE — no verdict: core-001:unit:adb4dcb90b9853b3 / Oath / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Oath/tests/Yubico.YubiKit.Oath.IntegrationTests/OathSessionTests.cs:64–77
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0", CustomFilter = typeof(BetaSerial103Filter))]
    public async Task ManagementPreflight_Serial103_ReportsFirmware(YubiKeyTestState state) =>
        await state.WithManagementAsync(async (mgmt, _) =>
        {
            var deviceInfo = await mgmt.GetDeviceInfoAsync();

            Assert.Equal(103, deviceInfo.SerialNumber);
            Assert.True(
                deviceInfo.FirmwareVersion.IsAtLeast(5, 8, 0),
                $"Expected beta firmware 5.8.x or later, got {deviceInfo.FirmwareVersion}.");
        }, cancellationToken: NewToken());


PROPOSED SAMPLE — no verdict: core-001:unit:a9579efc62584f84 / Oath / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Oath/tests/Yubico.YubiKit.Oath.IntegrationTests/OathSessionTests.cs:89–104
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.6.3", CustomFilter = typeof(BetaSerial103Filter))]
    public async Task OathSession_CreateWithScp03_ReadsSelectMetadataWithoutReset(YubiKeyTestState state)
    {
        using var scpParams = Scp03KeyParameters.Default;
        await using var session = await state.Device.CreateOathSessionAsync(
            new SessionCreationOptions { ScpKeyParameters = scpParams },
            cancellationToken: NewToken());

        Assert.Equal(103, state.SerialNumber);
        Assert.NotEmpty(session.DeviceId);
        Assert.True(session.Salt.Length > 0);
        Assert.NotNull(session.FirmwareVersion);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:039b97880290cbc2 / Oath / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Oath/tests/Yubico.YubiKit.Oath.IntegrationTests/OathSessionTests.cs:222–249
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
    public async Task CalculateAll_WithMultipleCredentials_ReturnsAllCodes(YubiKeyTestState state) =>
        await state.WithOathSessionAsync(async session =>
        {
            // Put multiple TOTP credentials
            using var cred1 = CreateTotpCredential("alice@example.com", "ServiceA");
            using var cred2 = CreateTotpCredential("bob@example.com", "ServiceB");

            await session.PutCredentialAsync(cred1, cancellationToken: NewToken());
            await session.PutCredentialAsync(cred2, cancellationToken: NewToken());

            // Calculate all
            long timestamp = 1704067200;
            var results = await session.CalculateAllAsync(timestamp, NewToken());

            Assert.Equal(2, results.Count);

            // All TOTP credentials should have non-null codes
            foreach (var (credential, code) in results)
            {
                Assert.NotNull(code);
                Assert.Equal(6, code.Value.Length);
                Assert.Equal(OathType.Totp, credential.OathType);
            }
        }, cancellationToken: NewToken());


PROPOSED SAMPLE — no verdict: core-001:unit:d7db7354164023a4 / Oath / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/OathScpResetTests.cs:110–122
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public void Dispose()
        {
            DisposeCount++;
            foreach (byte[] command in Commands.Concat(_handshake.TransmittedCommands))
                CryptographicOperations.ZeroMemory(command);
            foreach (string field in new[] { "_senc", "_smac", "_srmac", "_macChain" })
            {
                if (_handshake.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_handshake) is byte[] bytes)
                    CryptographicOperations.ZeroMemory(bytes);
            }
            _handshake.Dispose();
        }


PROPOSED SAMPLE — no verdict: core-001:unit:9ab2dca35fa7c063 / Oath / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/OathSessionTests.cs:680–687
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public List<(
            UserPresenceContext Context,
            UserPresenceOutcome Outcome,
            CancellationToken CancellationToken,
            int CommandCount)> Resolved
        { get; } = [];


PROPOSED SAMPLE — no verdict: core-001:unit:b4961ad59e422291 / Oath / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/OathToolDeviceSelectionTests.cs:42–56
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task MetadataCancellation_IsNotConvertedToMissingInfoOrFollowedByOathOpen()
    {
        using var console = new WaitingConsole();
        var device = new FakeDevice("cancelled") { CancelMetadata = true };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => OathToolServices.OpenSelectedSessionAsync(
            TestContext.Current.CancellationToken, console,
            _ => Task.FromResult<IReadOnlyList<IYubiKey>>([device])));

        Assert.Equal(1, device.ConnectionAttempts);
        Assert.False(console.Waiting.Started.Task.IsCompleted);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:868041ae1b1ae700 / Oath / type-declarations
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Oath/src/Backend/OathBackend.cs:23–26
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal readonly record struct OathInitialization(
    FirmwareVersion FirmwareVersion,
    byte[] Salt,
    byte[] Challenge);
