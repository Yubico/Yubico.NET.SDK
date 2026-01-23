# PIV Tool - YubiKey PIV Management Example

A command-line tool demonstrating the Yubico.NET.SDK PIV functionality. This example application showcases all major PIV operations including device discovery, PIN management, key generation, certificate operations, cryptographic operations, and more.

## Features

- **Device Discovery**: List connected YubiKeys with PIV capabilities
- **PIN Management**: Manage PIN, PUK, and management key
- **Key Generation**: Generate keys in PIV slots with configurable policies
- **Certificate Operations**: View, import, export, and generate certificates
- **Cryptographic Operations**: Sign and decrypt data, verify signatures
- **Key Attestation**: Verify keys were generated on-device
- **Slot Overview**: View summary of all PIV slots
- **PIV Reset**: Reset PIV application to factory defaults

## Requirements

- .NET 10.0 SDK
- YubiKey with PIV support (firmware 4.0+)

## Building

```bash
# From the repository root
dotnet build.cs build
```

## Running

```bash
# From the repository root
dotnet run --project Yubico.YubiKit.Piv/examples/PivTool/PivTool.csproj
```

## Usage

The application presents an interactive menu:

```
📋 Device Info         - Show connected YubiKey information
🔐 PIN Management      - Manage PIN, PUK, and management key
🔑 Key Generation      - Generate keys in PIV slots
📜 Certificate Ops     - Manage certificates in slots
✍️  Crypto Operations   - Sign, decrypt, verify operations
🛡️  Key Attestation     - Verify on-device key generation
📊 Slot Overview       - View all PIV slots at a glance
⚠️  Reset PIV           - Reset to factory defaults
```

## Security Notes

- All sensitive data (PINs, PUKs, management keys) is zeroed from memory after use
- Default credentials are detected and warnings are displayed
- Reset operations require multiple confirmations

## Cross-Platform Support

This tool runs on:
- Windows (with WinSCard)
- macOS (with CryptoTokenKit or PC/SC)
- Linux (with pcscd)

## SDK Methods Demonstrated

This example uses the following SDK methods:

1. `YubiKey.FindAllAsync()` - Device discovery
2. `device.ConnectAsync<ISmartCardConnection>()` - Connection
3. `PivSession.CreateAsync()` - Session creation
4. `ManagementSession.CreateAsync()` - Device info
5. `VerifyPinAsync()` - PIN verification
6. `ChangePinAsync()` - PIN change
7. `ChangePukAsync()` - PUK change
8. `UnblockPinAsync()` - PIN unblock
9. `SetRetryLimitsAsync()` - Retry configuration
10. `GetPinMetadataAsync()` - PIN metadata
11. `GetPukMetadataAsync()` - PUK metadata
12. `GetManagementKeyMetadataAsync()` - Management key info
13. `AuthenticateAsync()` - Management key auth
14. `SetManagementKeyAsync()` - Change management key
15. `GenerateKeyAsync()` - Key generation
16. `GetSlotMetadataAsync()` - Slot info
17. `GetCertificateAsync()` - Read certificate
18. `StoreCertificateAsync()` - Store certificate
19. `DeleteCertificateAsync()` - Delete certificate
20. `SignOrDecryptAsync()` - Cryptographic operations
21. `AttestKeyAsync()` - Key attestation
22. `ResetAsync()` - PIV reset
23. `DeviceInfo.SerialNumber` - Device identification
24. `DeviceInfo.FirmwareVersion` - Firmware version

## License

Apache License 2.0
