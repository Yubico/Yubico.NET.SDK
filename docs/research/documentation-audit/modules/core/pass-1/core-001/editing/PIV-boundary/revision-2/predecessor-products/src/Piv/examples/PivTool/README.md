# PivTool: explore PIV workflows

PivTool is an evaluation application for device discovery, credentials, key generation, certificates,
signing/decryption, attestation retrieval and reset. Start with the [PIV task guide](../../README.md)
for SDK contracts and complete host-verification/plaintext-cleanup examples. The tool demonstrates a
subset of the SDK and has the limitations below; use a device you are authorized to provision.

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

## Inspect and provision

Use `device-info` and `slot-info` first. The latter reports separate `HasKey` and `HasCertificate` fields;
on older firmware missing metadata can produce `HasKey=false` even when a key exists. Certificate
presence is independent of private-key occupancy and binding.

For a dedicated evaluation key, generate a P-256 key, create a matching self-signed certificate and sign
a file. Each command opens its own session, so each protected task supplies its own credentials.
The public factory credentials below are suitable only for a key actually configured with those values.
These commands change persistent state and can replace the selected key/certificate.

```bash
dotnet run --project src/Piv/examples/PivTool/PivTool.csproj -- key-generate --slot 9a --key-type eccp256 --pin-policy once --touch-policy never --management-key 010203040506070801020304050607080102030405060708 --json
dotnet run --project src/Piv/examples/PivTool/PivTool.csproj -- cert-self-sign --slot 9a --pin 123456 --management-key 010203040506070801020304050607080102030405060708 --subject "CN=PIV evaluation" --validity-days 30 --json
dotnet run --project src/Piv/examples/PivTool/PivTool.csproj -- sign --slot 9a --pin 123456 --input message.bin --output signature.bin --hash sha256 --json
dotnet run --project src/Piv/examples/PivTool/PivTool.csproj -- verify-signature --slot 9a --data message.bin --signature signature.bin --hash sha256 --json
```

Check both `Success` and `IsValid` for verification. A successful verification operation can report an
invalid signature. The helper verifies using the stored certificate; enrollment must establish that it
belongs to the intended slot key. Self-signed certificates demonstrate device signing through an external
`X509SignatureGenerator`, rather than a production identity or certificate trust policy.

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

## Choose the supported crypto path

Use the generic `sign`/`Signing.SignDataAsync` helper for ECDSA evaluation: it hashes every input before
calling the raw SDK primitive. For RSA it omits EMSA encoding, and for Ed25519 it signs the hash rather
than the original message. X25519 is an agreement algorithm. For those algorithms call the SDK directly:
prepare a complete RSA signature representative, pass the original Ed25519 message, or use
`CalculateSecretAsync` with an application-selected KDF. See the [crypto tasks](../../README.md#verify-the-pin-sign-and-verify-on-the-host).

The CLI decrypt handler selects PKCS#1 v1.5 and exposes no padding flag. Use direct `DecryptAsync` for
OAEP and mutable plaintext cleanup. `DecryptionResult.DecryptedData` currently exposes read-only memory
without an ordinary clearing API; preserve ownership boundaries rather than casting it writable. Treat
the tool's plaintext output/files as sensitive application data.

## Credentials, recovery and reset

Interactive prompts filter decimal digits; CLI credential strings are UTF-8 encoded. Shell arguments can
appear in process listings, shell history and automation logs. Use the SDK with application-owned mutable
credential buffers in production. The sample clears many working buffers, but generated/KDF source arrays
and some exceptional paths still have cleanup gaps. It provides no secure key escrow: back up a replacement
management key through your own secure store before installing it.

Reset text mode asks for `RESET` unless `--force` is present; JSON mode requires `--force`. Normal PIV
reset removes ordinary keys/certificates/objects and restores credentials, while retaining F9 attestation
material. Blocking PIN/PUK and refreshing post-reset metadata can fail on either side of the physical reset;
inspect state before retrying. Bio MPE requires its product device-reset route. `cert-delete` executes
without confirmation: the `--force` shown in runtime help has no effect in that handler.

The interactive CSR menu currently omits PIN verification, and its certificate export path reads the wrong
result field. Use the CLI CSR/export tasks or direct SDK calls for these tasks. Callback signature generators
block while calling the async SDK and have limited cancellation propagation; a displayed timeout can also
represent cancellation caught by the helper. Await terminal work and inspect device state before repeating
mutations. These limitations remain engineering follow-ups rather than guarantees of the sample.

Keep the original SDK task and its inputs/session/prompt alive until actual completion, even if an outer wait ends.
Core guards one logical exchange, not the tool's complete credential, certificate or PIN-only workflow. PIV presence
callbacks and local RSA unpadding sit outside that exchange guard. A cancellation token does not interrupt admitted
SmartCard traffic or supply a native deadline; the sample's broad error/timeout mapping does not establish one.

## Find the SDK examples

- `PivExamples/`: task helpers using real SDK types, including device-backed certificate signature generators.
- `PivExamples/Results/`: result records. Callers dispose returned `Certificate`, `AttestationCertificate` and
  `IntermediateCertificate` instances after use; result records themselves provide no disposal mechanism.
- `Cli/Commands/`: argument handling and JSON output.
- `Cli/Menus/`, `Cli/Prompts/`, `Cli/Output/`: interactive presentation.
- `Program.cs`: selects interactive or argument-driven mode.

XML examples are composable fragments: supply an `IYubiKey device`, `CancellationToken ct`, required
credential/data buffers and, where used, an application-owned `IUserPresencePrompt`. Keep borrowed inputs
alive through awaited calls and clear secrets in the caller's finally scope. Returned host crypto objects,
certificates and imported `IPrivateKey` objects need their own disposal scopes. See the module task guide
for complete signature verification, known-plaintext decryption and protected-mode reopen/recovery scopes.

## License

Apache License 2.0.
