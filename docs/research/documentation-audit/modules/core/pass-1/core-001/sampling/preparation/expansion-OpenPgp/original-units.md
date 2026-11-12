# OpenPGP additional whole units

**No content verdicts.** Pinned source `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; read complete units with enclosing context as needed.

## `core-001:unit:f266412cbf666ee5`
`src/OpenPgp/src/PrivateKeyImport.cs:82–110`; implementation-state-and-internal-contracts; original rank 5.

````csharp


    private static AlgorithmAttributes GetAttributes(IPrivateKey privateKey, KeyRef keyRef, FirmwareVersion firmwareVersion)
    {
        switch (privateKey)
        {
            case RSAPrivateKey rsa:
                if (!PublicExponent65537.AsSpan().SequenceEqual(TrimLeadingZeros(rsa.Parameters.Exponent)))
                {
                    throw new NotSupportedException("RSA keys with a public exponent other than 65537 are not supported.");
                }

                var rsaSize = rsa.KeyInfo.GetOpenPgpInfo()?.RsaSize
                    ?? throw new NotSupportedException($"RSA {rsa.KeyInfo.KeySizeBits} is not a size the OpenPGP application imports.");

                return RsaAttributes.Create(
                    rsaSize,
                    firmwareVersion.Major is > 0 and < 4 ? RsaImportFormat.Crt : RsaImportFormat.Standard);

            case ECPrivateKey ec:
                return EcAttributes.Create(keyRef, ec.KeyType);

            case Curve25519PrivateKey curve:
                return EcAttributes.Create(keyRef, curve.KeyType);

            default:
                throw new NotSupportedException($"Private key type {privateKey.GetType().Name} is not supported for OpenPGP import.");
        }
    }
````

## `core-001:unit:8490dabf2e2bd6ea`
`src/OpenPgp/src/OpenPgpSession.Pin.cs:142–193`; implementation-state-and-internal-contracts; original rank 6.

````csharp


    private async Task ResetPinCoreAsync(
        ReadOnlyMemory<byte> resetCode,
        ReadOnlyMemory<byte> newPin,
        bool useAdmin,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        _logger.LogDebug("Resetting User PIN (useAdmin={UseAdmin})", useAdmin);

        byte[]? resetBytes = null;
        byte[]? newPinBytes = null;
        byte[]? data = null;
        try
        {
            var kdf = await GetOrLoadKdfAsync(cancellationToken).ConfigureAwait(false);
            newPinBytes = kdf.Process(Pw.User, newPin.Span);

            // P1=0x02 (admin): Admin PIN (PW3) must have been verified beforehand
            //   by the caller via VerifyAdminAsync. Data = new PIN only.
            // P1=0x00 (reset code): data = resetCode + newPin concatenated.
            var p1 = useAdmin ? (byte)0x02 : (byte)0x00;

            if (useAdmin)
            {
                data = newPinBytes;
            }
            else
            {
                resetBytes = kdf.Process(Pw.Reset, resetCode.Span);
                data = new byte[resetBytes.Length + newPinBytes.Length];
                resetBytes.CopyTo(data.AsSpan());
                newPinBytes.CopyTo(data.AsSpan(resetBytes.Length));
            }

            var command = new ApduCommand(0x00, (int)Ins.ResetRetryCounter, p1, (int)Pw.User, data);

            await TransmitAsync(command, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (resetBytes is not null)
                CryptographicOperations.ZeroMemory(resetBytes);
            if (newPinBytes is not null)
                CryptographicOperations.ZeroMemory(newPinBytes);
            // Only zero 'data' separately if it's the combined buffer (not same ref as newPinBytes)
            if (data is not null && !ReferenceEquals(data, newPinBytes))
                CryptographicOperations.ZeroMemory(data);
        }
    }
````

## `core-001:unit:c695a3c8180b434c`
`src/OpenPgp/src/PrivateKeyImport.cs:110–139`; implementation-state-and-internal-contracts; original rank 7.

````csharp


    private PrivateKeyTemplate RsaTemplate(RSAPrivateKey rsa, bool useCrt)
    {
        var parameters = rsa.Parameters;
        if (parameters.P is null || parameters.Q is null)
        {
            throw new ArgumentException("The RSA private key has no prime factors.", nameof(rsa));
        }

        var componentLength = rsa.KeyInfo.KeySizeBytes / 2;
        var p = FixedLength(parameters.P, componentLength);
        var q = FixedLength(parameters.Q, componentLength);
        if (!useCrt)
        {
            return new RsaKeyTemplate(PublicExponent65537, p, q);
        }

        if (parameters.InverseQ is null || parameters.DP is null || parameters.DQ is null || parameters.Modulus is null)
        {
            throw new ArgumentException("The RSA private key has no CRT parameters.", nameof(rsa));
        }

        return new RsaCrtKeyTemplate(
            PublicExponent65537, p, q,
            FixedLength(parameters.InverseQ, componentLength),
            FixedLength(parameters.DP, componentLength),
            FixedLength(parameters.DQ, componentLength),
            FixedLength(parameters.Modulus, componentLength * 2));
    }
````

## `core-001:unit:606c7462df485c41`
`src/OpenPgp/src/PrivateKeyImport.cs:145–158`; implementation-state-and-internal-contracts; original rank 8.

````csharp


    private PrivateKeyTemplate Curve25519Template(Curve25519PrivateKey curve)
    {
        if (curve.KeyType == KeyType.Ed25519)
        {
            return new EcKeyTemplate(curve.PrivateKey);
        }

        var reversed = curve.PrivateKey.ToArray();
        Array.Reverse(reversed);
        _ownedBuffers.Add(reversed);
        return new EcKeyTemplate(reversed);
    }
````

## `core-001:unit:3f79f975e26f3c6c`
`src/OpenPgp/PACKAGE_README.md:12–20`; narrative-examples-and-maps; original rank 3.

````markdown
## Installation

```bash
dotnet nuget add source https://yubico.github.io/Yubico.NET.SDK/alpha/index.json -n yubikit-alpha
dotnet add package Yubico.YubiKit.OpenPgp --prerelease
```

`Yubico.YubiKit.Core` is installed transitively.


````

## `core-001:unit:15611fb8dbaa0ab5`
`src/OpenPgp/README.md:500–527`; narrative-examples-and-maps; original rank 4.

````markdown
## Example tool and further reading

Run the interactive sample from the repository root:

```bash
dotnet run --project src/OpenPgp/examples/OpenPgpTool/OpenPgpTool.csproj
```

Treat OpenPgpTool as a sample with current limitations. Its attestation `--pin` option is declared but unused;
use the direct SDK attestation method above to verify the User PIN explicitly. `info` infers key presence from
fingerprint length, so use `ListKeyInformationAsync` for state inspection. Command-line PINs and PEM text live in
immutable strings, even when temporary byte buffers are cleared. Noninteractive device selection takes the first
SmartCard device, and the tool installs no touch-notification prompt. Certificate command results also need improved
disposal. Use application-owned input buffers, explicit device selection, complete resource scopes and your UI with
the direct SDK for those responsibilities. The sample's reset/fixed-policy help strings overstate reset scope;
apply the ATT exception described above.

- [Core guide](../Core/README.md): discovery, connections, APDU and TLV.
- [PIV guide](../Piv/README.md) and [Management guide](../Management/README.md).
- [OpenPGP Card specification](https://gnupg.org/ftp/specs/OpenPGP-smart-card-application-3.4.pdf):
  *Functional Specification of the OpenPGP application on ISO Smart Card Operating Systems*, Version **3.4.1,
  March 18, 2020** (the URL retains “3.4”). Relevant clauses: §4.3.2 KDF, §4.4.3.9 attributes, §§7.2.2–4 PINs,
  §§7.2.10–11 crypto, §7.2.14 key generation and §§7.2.16–17 lifecycle.
- [RFC 9580 §3.7.1.3](https://www.rfc-editor.org/rfc/rfc9580.html#section-3.7.1.3): S2K primitive;
  [RFC 8017](https://www.rfc-editor.org/rfc/rfc8017.html): RSA encodings;
  [RFC 8032](https://www.rfc-editor.org/rfc/rfc8032.html): Ed25519.
- [Vendor touch/reset guidance](https://docs.yubico.com/software/yubikey/tools/ykman/OpenPGP_Commands.html).
- [Developer guide](../../docs/DEV-GUIDE.md): build, test and contribution workflow.

````

