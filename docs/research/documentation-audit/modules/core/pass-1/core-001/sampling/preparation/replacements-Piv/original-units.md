# PIV replacement whole units

**No content verdicts.** Pinned source `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; read complete units with enclosing context as needed.

## `core-001:unit:be11505c581308a5`
`src/Piv/examples/PivTool/Cli/Commands/CliRunner.cs:534–587`; implementation-state-and-internal-contracts; original rank 9.

````csharp


    private static async Task<int> HandleCertCsrAsync(
        Dictionary<string, string?> flags, int? serial, bool useJson, CancellationToken ct)
    {
        var slot = DeviceHelper.ParseSlot(DeviceHelper.GetFlag(flags, "slot"));
        if (slot is null) return 1;

        var subject = DeviceHelper.GetFlag(flags, "subject") ?? "CN=Test User";
        var outputPath = DeviceHelper.GetFlag(flags, "output");

        // PIN is needed to sign the CSR on the YubiKey
        var pin = DeviceHelper.ParseCredentialBytes(DeviceHelper.GetFlag(flags, "pin"), "pin");
        if (pin is null) return 1;

        var device = await DeviceHelper.GetDeviceAsync(serial, ct);
        if (device is null) { ZeroAndReturn(pin); return 1; }

        try
        {
            await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);
            var pinResult = await PinManagement.VerifyPinAsync(session, pin, ct);
            if (!pinResult.Success)
            {
                if (useJson) JsonOutput.Write(pinResult);
                else Console.Error.WriteLine($"Error: {pinResult.ErrorMessage}");
                return 1;
            }

            var result = await PivExamples.Certificates.GenerateCsrAsync(session, slot.Value, subject, ct);

            if (!result.Success)
            {
                if (useJson) JsonOutput.Write(result);
                else Console.Error.WriteLine($"Error: {result.ErrorMessage}");
                return 1;
            }

            if (outputPath is not null && result.CsrPem is not null)
            {
                await File.WriteAllTextAsync(outputPath, result.CsrPem, ct);
                if (!useJson) Console.WriteLine($"CSR written to: {outputPath}");
            }

            if (useJson) JsonOutput.Write(result);
            else if (outputPath is null) Console.WriteLine(result.CsrPem ?? string.Empty);

            return 0;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pin);
        }
    }
````

## `core-001:unit:a4315aa9922ecd83`
`src/Piv/src/Cryptography/PrivateKeyOperations.cs:72–159`; implementation-state-and-internal-contracts; original rank 10.

````csharp


    private static async Task<byte[]> SignOrDecryptCoreAsync(
        IBackend backend,
        Slot slot,
        KeyType keyType,
        ReadOnlyMemory<byte> data,
        UserPresenceNotification userPresenceNotification,
        CancellationToken cancellationToken)
    {
        Logger.LogDebug("PIV: Signing/decrypting with slot 0x{Slot:X2}, key type {KeyType}", (byte)slot, keyType);

        // Size the TAG 81 payload for the algorithm; the bytes are written straight into the APDU buffer below.
        int preparedLength = PreparedDataLength(keyType, data.Length);

        // Build command data: TAG 0x7C [ TAG 0x82 (response) + TAG 0x81 (challenge) ]
        // Pre-compute lengths to avoid List<byte> resizing (which can leave unzeroed copies)

        // Inner content: TAG 0x82 (2 bytes) + TAG 0x81 + length encoding + data
        int dataLenEncodingSize = BerLength.EncodingSize(preparedLength);
        int innerLength = 2 + 1 + dataLenEncodingSize + preparedLength; // 0x82,0x00 + 0x81 + len + data

        // Template: TAG 0x7C + length encoding + inner content
        int templateLenEncodingSize = BerLength.EncodingSize(innerLength);
        int totalLength = 1 + templateLenEncodingSize + innerLength; // 0x7C + template len + inner

        var commandData = new byte[totalLength];
        try
        {
            int offset = 0;

            commandData[offset++] = TagDynamicAuth;

            // Template length encoding
            offset += BerLength.Write(commandData.AsSpan(offset), innerLength);

            commandData[offset++] = TagResponse;
            commandData[offset++] = 0;

            commandData[offset++] = TagChallenge;
            offset += BerLength.Write(commandData.AsSpan(offset), preparedLength);

            WritePreparedData(keyType, data.Span, commandData.AsSpan(offset, preparedLength));

            var command = new ApduCommand(Cla, InsAuthenticate, KeyTypeEncoding.EncodeAsymmetric(keyType), (byte)slot, commandData);
            await userPresenceNotification.RequestAsync(cancellationToken).ConfigureAwait(false);

            var response = await backend.SendAsync(command, throwOnError: false, cancellationToken).ConfigureAwait(false);
            byte[]? result = null;
            try
            {
                if (!response.IsOK())
                {
                    // Check if PIN verification is required
                    if (response.SW == SWConstants.SecurityStatusNotSatisfied)
                    {
                        throw new InvalidOperationException(
                            "Security status not satisfied. PIN verification may be required before this operation.");
                    }

                    throw ApduException.FromStatusWord(response.SW,
                        $"Sign/decrypt operation failed for slot 0x{(byte)slot:X2}");
                }

                // Parse response: TAG 0x7C [ TAG 0x82 (response data) ]
                result = ParseCryptoResponse(response.Data);
                byte[] transferredResult = await ResolveAndTransferCryptoResultAsync(
                        result,
                        userPresenceNotification)
                    .ConfigureAwait(false);
                result = null;
                return transferredResult;
            }
            finally
            {
                if (result is not null)
                {
                    CryptographicOperations.ZeroMemory(result);
                }

                response.Clear();
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(commandData);
        }
    }
````

## `core-001:unit:eb2ea938be1dc7f7`
`src/Piv/examples/PivTool/README.md:8–24`; narrative-examples-and-maps; original rank 4.

````markdown
## Run and select a device

Install the .NET 10 SDK and enable PIV SmartCard access. Linux needs
[PC/SC setup](../../../../docs/linux-setup.md). From the repository root:

```bash
dotnet run --project src/Piv/examples/PivTool/PivTool.csproj
dotnet run --project src/Piv/examples/PivTool/PivTool.csproj -- device-info --json
dotnet run --project src/Piv/examples/PivTool/PivTool.csproj -- slot-info --serial 12345678 --json
```

No arguments opens the interactive menus. Supplying arguments selects CLI mode; `help` lists options.
CLI mode requires `--serial <number>` when multiple keys are attached. Device discovery uses
`YubiKeyManager`; the PIV session opens SmartCard transport. Firmware requirements follow each SDK
operation: metadata and high-level decryption need 5.3+, while larger RSA/25519 generation needs 5.7+.
Device/profile restrictions still apply.


````

