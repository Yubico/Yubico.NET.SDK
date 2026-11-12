# Last bounded role-quota replacement whole units

**No content verdicts.** Pinned source `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; read complete units and relevant enclosing context.

## `core-001:unit:e8471885414d88c9`
`scripts/toolchain/Discovery.cs:27–32`; A implementation-state-and-internal-contracts; original rank 28.

````csharp


    public static TestProject[] ToTestProjects(string repoRoot, IEnumerable<string> projectPaths) =>
        projectPaths
            .Select(p => new TestProject(p, UsesMicrosoftTestingPlatformRunner(repoRoot, p)))
            .ToArray();
````

## `core-001:unit:a92c6aa039f27e15`
`docs/migration/v1-to-v2-map.yml:546–557`; A narrative-examples-and-maps; original rank 22.

````markdown
  - id: core-device-serial-and-correlation
    status: assisted
    confidence: high
    migration_kind: member
    v1: No direct equivalent; correlating a physical key across discovery events required an explicit Management session read of DeviceInfo.SerialNumber or ad hoc per-transport device-path comparison
    v2: IYubiKey.SerialNumber (int?, populated by discovery without opening a session) and IYubiKey.SameDeviceAs(IYubiKey) -> DeviceCorrelation (Unknown/Same/Different)
    evidence:
      - yubikit:src/Core/src/Abstractions/IYubiKey.cs
      - yubikit:src/Core/src/Abstractions/DeviceCorrelation.cs
      - yubikit:docs/architecture/device-identity.md
    notes: Additive default-interface members on the public IYubiKey; existing implementers still compile unchanged. SerialNumber is null until a discovery metadata read succeeds (and possibly forever - reads can fail persistently, and whole device classes such as the Security Key series report no serial at all); once non-null it never reverts to null. SameDeviceAs evaluates reference identity first, then compares known serials, returning Unknown (not a guess) when either side's serial is unknown - this is a three-valued correlation, not a total equality predicate, and must not be coerced to true/false for deduplication. This complements, not replaces, recipe-management-device-info. Core CachedDeviceInfo now exposes optional last-known metadata; Management remains the live source for firmware/configuration. The serial latch remains independent of metadata replacement.
    last_reviewed_commit: 277920d9de48d4aedad7ecc5d00201a015b5a297

````

## `core-001:unit:3a5c3c12f2af0a27`
`docs/architecture/physical-device-model.md:298–303`; A narrative-examples-and-maps; original rank 23.

````markdown
## SCP Note

Secure Channel Protocol is valid only on SmartCard. Supplying `SessionCreationOptions.ScpKeyParameters`
without an explicit override selects SmartCard automatically. Explicitly selecting a non-SmartCard transport
with SCP parameters throws `NotSupportedException` during session initialization.


````

## `core-001:unit:04aeedc7f7928b92`
`src/Core/README.md:1–11`; B4 narrative-examples-and-maps; original rank 13.

````markdown
# Yubico.YubiKit.Core

Core is the foundation every other module builds on. It discovers YubiKeys, opens and owns connections
over SmartCard (PC/SC) and HID, runs the ISO 7816-4 APDU pipeline with automatic command chaining,
implements Secure Channel Protocol (SCP03 and SCP11), and supplies the shared device metadata,
cryptography, and TLV types. It has no application of its own; install an application module and Core
comes with it.

> The v2 SDK is a pre-release alpha; see the [repository README](../../README.md) for the current status and
> constraints.


````

## `core-001:unit:8f341f2d9ed91af4`
`src/OpenPgp/src/Backend/OpenPgpBackend.cs:32–53`; OpenPgp implementation-state-and-internal-contracts; original rank 10.

````csharp


    public async Task<OpenPgpInitialization> ReadInitialStateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SelectAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ApduException ex) when (ex.SW is SWConstants.FileTerminated or SWConstants.ConditionsNotSatisfied)
        {
            Logger.LogDebug("OpenPGP applet in terminated state, sending ACTIVATE");
            await SendAsync(
                    new ApduCommand(0x00, (int)Ins.Activate, 0x00, 0x00),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            await SelectAsync(cancellationToken).ConfigureAwait(false);
        }

        var firmwareVersion = await GetVersionAsync(cancellationToken).ConfigureAwait(false);
        return new OpenPgpInitialization(firmwareVersion);
    }
````

## `core-001:unit:72ae07bf970ed5ec`
`src/Piv/src/Metadata/MetadataProtocol.cs:79–87`; Piv implementation-state-and-internal-contracts; original rank 11.

````csharp


    private static ReadOnlyMemory<byte> RequiredField(
        IDictionary<int, ReadOnlyMemory<byte>> fields, int tag, int length)
    {
        if (!fields.TryGetValue(tag, out var value) || value.Length != length)
            throw new ApduException($"Missing or invalid metadata field 0x{tag:X2}.");
        return value;
    }
````

## `core-001:unit:0cb494f7969547df`
`src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/WebAuthnClientFactoryTests.cs:33–57`; WebAuthn tests-and-helpers; original rank 26.

````csharp

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task CreateWebAuthnClientAsync_WithSmartCard_CreatesDisposableClient(YubiKeyTestState state)
    {
        var origin = ParseOrigin(TestOriginUrl);

        try
        {
            await using var client = await state.Device.CreateWebAuthnClientAsync(
                origin,
                new WebAuthnClientOptions
                {
                    RpIdValidator = (requestOrigin, rpId) => requestOrigin.StringValue == TestOriginUrl && rpId == TestRpId
                },
                sessionOptions: new SessionCreationOptions { PreferredConnectionType = ConnectionType.SmartCard });

            Assert.NotNull(client);
        }
        catch (NotSupportedException)
        {
            Skip.If(true,
                "CTAP2 SmartCard session failed because the connected authenticator did not expose the FIDO AID or does not support USB SmartCard CTAP2 on this firmware.");
        }
    }
````

