# SCP03 Test Application

A simple console application to test and debug SCP03 secure channel initialization with YubiKey.

## Purpose

This test app helps diagnose SCP03 connection issues by:
1. Testing a normal connection WITHOUT SCP
2. Testing a connection WITH SCP03 using default keys
3. Providing clear output showing where failures occur

## Running the Test

```bash
cd /home/dyallo/Code/y/Yubico.NET.SDK
dotnet run --project Scp03TestApp.csproj
```

## Prerequisites

- **YubiKey** with default SCP03 keys configured
  - Default keys: `404142434445464748494A4B4C4D4E4F`
  - Key Version Number (KVN): `0xFF`
  - Key ID (KID): `0x01`

## Expected Output

### Success Case
```
=== SCP03 Test Application ===

Looking for YubiKey devices...
Found device

=== Test 1: Connection WITHOUT SCP ===
✓ SUCCESS: Got device info (Serial: 12345678)

=== Test 2: Connection WITH SCP03 (Default Keys) ===

Static Keys (all same): 404142434445464748494A4B4C4D4E4F

Key Reference: KID=0x01, KVN=0xFF

Connecting to YubiKey with SCP03...
✓ SCP03 session established!

✓ Got device info over SCP: Serial 12345678
```

### Failure Case (Wrong Keys)
```
=== Test 2: Connection WITH SCP03 (Default Keys) ===

...

Connecting to YubiKey with SCP03...

✗ FAILED: Wrong SCP03 key set - Expected: 1234567890ABCDEF, Got: FEDCBA0987654321

This means the card cryptogram didn't match.
The error message shows: Expected (received from card) vs Got (calculated)
```

## What the Error Message Means

**`Wrong SCP03 key set - Expected: X, Got: Y`**

- **Expected**: The card cryptogram received from the YubiKey
- **Got**: The card cryptogram we calculated

If these don't match, it means:
1. The keys are wrong (most common)
2. The key derivation algorithm has a bug
3. The context (host + card challenge) is incorrect

## Troubleshooting

### "No YubiKey devices found"
- Ensure YubiKey is plugged in
- Wait a few seconds and try again
- Check device permissions (Linux)

### "Wrong SCP03 key set"
- Verify the YubiKey has SCP03 keys configured
- Check if default keys are being used (KVN 0xFF)
- If using custom keys, modify the test app to use your keys

### "SCP is not supported by this YubiKey"
- The YubiKey firmware doesn't support SCP03
- Or SCP03 keys are not configured

## Modifying for Custom Keys

To test with custom SCP03 keys, replace this section in Program.cs:

```csharp
// Instead of:
using var staticKeys = StaticKeys.GetDefaultKeys();

// Use:
var encKey = new byte[] { /* your 16-byte ENC key */ };
var macKey = new byte[] { /* your 16-byte MAC key */ };
var dekKey = new byte[] { /* your 16-byte DEK key */ };
using var staticKeys = new StaticKeys(encKey, macKey, dekKey);

// And set the correct KVN:
var keyRef = new KeyRef(Kid: 0x01, Kvn: 0x01); // Your actual KVN
```

## Exit Codes

- `0`: Success - SCP03 connection established and working
- `1`: Failure - See output for details
