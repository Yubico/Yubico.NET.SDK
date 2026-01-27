# JIRA Request: Large Blob Without Extension at MakeCredential

## ✅ VERIFIED: SDK Already Supports This Workflow

**Test Date:** 2026-01-27

**Result:** The SDK already supports retrieving `largeBlobKey` via `GetAssertion` for credentials created WITHOUT the extension at `MakeCredential` time.

**No SDK code changes required** - only documentation updates needed.

---

## Summary

A JIRA request asks to "relax the strict requirements for large blob creation" in the .NET SDK. The reporter believes the SDK requires `largeBlobKey` extension at credential creation time, but OpenSSH/libfido2 examples show blobs being added to existing credentials without this.

## Background

### The PowerShell Script
- Source: https://gist.github.com/JMarkstrom/8ee848840116bedefcb9c5a941d8842a
- Uses `SetSerializedLargeBlobArray` to store blobs
- Takes a `CredentialId` parameter but comment says "conceptual association only for now"
- Uses custom `LargeBlobHelper.FromRawBytes()` not in SDK

### The SSH Certificate Workflow (Yubico Documentation)
- https://developers.yubico.com/SSH/Storing_SSH_Certificates.html
- Creates SSH key with `ssh-keygen -t ecdsa-sk -O resident`
- Later stores certificate with `fido2-token -S -b -n ssh:demo id_ecdsa-cert.pub`
- No explicit largeBlobKey extension mentioned in the workflow

## Key Findings

### CTAP 2.1 Specification (per Dain Nilsson)
> "Authenticators **MAY optionally generate a largeBlobKey** for a credential if the Large Blob Key (largeBlobKey) extension is **absent**, but MUST NOT return an unsolicited largeBlobKey extension response"

**Translation:**
- YubiKeys CAN generate largeBlobKeys for all resident credentials automatically
- But they WON'T return it in MakeCredential unless the extension was requested
- You CAN retrieve it later via GetAssertion (with extension) or Credential Management

### Mario Bodemann's Test (Yubico Engineer)
- Created credential via Chrome **WITHOUT** largeBlobKey extension
- **Successfully** stored and retrieved largeBlob data via assertion/get flow
- Tested on YubiKey 5.7.1

### Current SDK Behavior
1. **Documentation says** extension is required at MakeCredential (`large-blobs.md` line 117-131)
2. **SDK supports** retrieving largeBlobKey via:
   - `GetAssertionData.LargeBlobKey` (line 103 in GetAssertionData.cs)
   - `CredentialUserInfo.LargeBlobKey` (line 53 in CredentialUserInfo.cs)
   - Credential Management API (`EnumerateCredentialsForRelyingParty`)

## Unresolved Question

**What exactly is being requested?**

| Option | Description | CTAP Compliant? | SDK Support |
|--------|-------------|-----------------|-------------|
| **A** | Retrieve auto-generated largeBlobKey via GetAssertion for credentials created without extension | ✅ Yes | Likely already works, needs testing |
| **B** | Store blob without largeBlobKey, reference by credential ID only | ❌ No | Should NOT implement |

### How `fido2-token -S -b -n ssh:demo` Actually Works
1. Takes the RP ID (`ssh:demo`)
2. Looks up credential for that RP via credential management
3. **Retrieves the largeBlobKey** (that YubiKey auto-generated)
4. Encrypts blob with that key
5. Stores encrypted blob

The `-n` flag doesn't bypass encryption - it identifies which credential's largeBlobKey to use.

## Test Plan

```csharp
/// <summary>
/// Tests: Can we retrieve largeBlobKey via GetAssertion for a credential
/// created WITHOUT the extension at MakeCredential time?
/// </summary>
[Fact]
public void LargeBlobKey_RetrievedViaGetAssertion_ForCredentialWithoutExtension()
{
    using var fido2Session = new Fido2Session(_testDevice);
    fido2Session.KeyCollector = keyCollector;
    
    // STEP 1: Create credential WITHOUT largeBlobKey extension
    var mcParams = new MakeCredentialParameters(_rp, _user)
    {
        ClientDataHash = _clientDataHash
    };
    mcParams.AddOption(AuthenticatorOptions.rk, true);
    // NOTE: NOT adding largeBlobKey extension
    
    var mcData = fido2Session.MakeCredential(mcParams);
    Assert.Null(mcData.LargeBlobKey); // Per CTAP spec, should be null
    
    // STEP 2: Try to retrieve largeBlobKey via GetAssertion
    var gaParams = new GetAssertionParameters(_rp, _clientDataHash);
    gaParams.AddExtension(Extensions.LargeBlobKey, new byte[] { 0xF5 });
    
    var assertions = fido2Session.GetAssertions(gaParams);
    
    // KEY TEST: On YubiKeys, this SHOULD return a key (per Mario's test)
    ReadOnlyMemory<byte>? retrievedKey = assertions[0].LargeBlobKey;
    
    if (retrievedKey is null)
    {
        Assert.Fail("YubiKey did not return largeBlobKey for credential created without extension");
    }
    
    // STEP 3: Verify blob storage works with retrieved key
    var blobArray = fido2Session.GetSerializedLargeBlobArray();
    byte[] testData = { 0x01, 0x02, 0x03, 0x04 };
    blobArray.AddEntry(testData, retrievedKey.Value);
    fido2Session.SetSerializedLargeBlobArray(blobArray);
    
    // Verify round-trip
    blobArray = fido2Session.GetSerializedLargeBlobArray();
    Assert.Single(blobArray.Entries);
    Assert.True(blobArray.Entries[0].TryDecrypt(retrievedKey.Value, out var plaintext));
    Assert.True(plaintext.Span.SequenceEqual(testData));
}
```

## Suggested Clarification Question for Reporter

> "To clarify: Are you expecting to retrieve a largeBlobKey via GetAssertion (with the extension) for a credential that was created WITHOUT the extension at MakeCredential time?
>
> Or are you expecting to store blob data without any largeBlobKey at all, using only the credential ID as an association?"

## Recommendations

### ~~If Answer is Option A (retrieve auto-generated key):~~
### ✅ CONFIRMED: Option A Works

1. ~~Run the test above to confirm YubiKey behavior~~ **DONE - Test passed**
2. Update SDK documentation to clarify:
   - YubiKeys auto-generate largeBlobKey for all resident credentials
   - Can retrieve via GetAssertion with extension, even if not requested at creation
   - This is YubiKey-specific behavior, not guaranteed on other authenticators
3. **No SDK code changes needed**

### If Answer is Option B (bypass encryption):
- Reject the request - violates CTAP encryption requirement
- Explain that `fido2-token` still uses largeBlobKey, it just retrieves it automatically

---

## Documentation Update Needed

The current documentation in `large-blobs.md` states:

> "If a YubiKey supports the large blob option, **you must make a credential with the large blob extension set to true.**"

This should be updated to:

> "To use large blobs, you need a `largeBlobKey`. You can either:
> 1. Request it at credential creation by adding the `largeBlobKey` extension to `MakeCredential`
> 2. Retrieve it later via `GetAssertion` with the `largeBlobKey` extension (YubiKeys auto-generate this key for all resident credentials)
>
> **Note:** Option 2 relies on YubiKey-specific behavior. Other FIDO2 authenticators may not auto-generate the key."

## References

- CTAP 2.1 Spec: largeBlobKey extension
- SDK Documentation: `/docs/users-manual/application-fido2/large-blobs.md`
- SDK Code:
  - `Yubico.YubiKey/src/Yubico/YubiKey/Fido2/GetAssertionData.cs`
  - `Yubico.YubiKey/src/Yubico/YubiKey/Fido2/CredentialUserInfo.cs`
  - `Yubico.YubiKey/src/Yubico/YubiKey/Fido2/Fido2Session.LargeBlobs.cs`
  - `Yubico.YubiKey/tests/integration/Yubico/YubiKey/Fido2/LargeBlobTests.cs`
- Yubico SSH Certificates: https://developers.yubico.com/SSH/Storing_SSH_Certificates.html
- PowerShell Script: https://gist.github.com/JMarkstrom/8ee848840116bedefcb9c5a941d8842a

## Conversation Date

2026-01-23 / 2026-01-27
