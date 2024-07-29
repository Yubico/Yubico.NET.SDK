---
uid: UsersManualSensitive
---

<!-- Copyright 2021 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

# Handling Sensitive Data (PINs, Passwords, and Keys)

## Introduction

This guide addresses the challenges of handling sensitive data like PINs, passwords, and keys in C# applications, particularly when using the SDK. While no method provides absolute security, these practices can significantly reduce the risk of data leakage.

## Potential Risks

- Clear-text sensitive data in memory: If an attacker gains access to the program's memory, they could potentially read sensitive information.
- Data persistence after memory release: Sensitive data may remain in memory even after it's no longer needed, increasing the risk of exposure.
- Unintended copies due to memory management: C#'s memory management might create copies of data in memory, spreading sensitive information to multiple locations.
- Exposure through memory swapping: When the operating system swaps memory to disk, sensitive data could be written to non-volatile storage.

**Note:** If an attacker has access to running program memory, the system is already compromised. These measures mitigate risks but don't provide absolute security.

## Best Practices

1. **Use Byte Arrays over Strings**: 
   Prefer `byte[]` over strings for sensitive data. Byte arrays allow direct memory manipulation and can be securely overwritten.

   Strings in C# are immutable, meaning any operation on a string creates a new string object, potentially leaving copies of sensitive data in memory.

2. **Overwrite Buffers**: 
   Use `CryptographicOperations.ZeroMemory()` to clear data after use.

   Simply setting a reference to null doesn't clear the actual data from memory. Overwriting ensures the sensitive data is actually removed.

3. **Minimize Data Lifespan**: 
   Collect sensitive data just before use and clear immediately after.

   The longer sensitive data remains in memory, the higher the risk of exposure. Minimizing its lifespan reduces this risk.

4. **Control Buffer Sizes**: 
   Pre-allocate maximum-sized buffers to avoid resizing risks.

   Resizing operations can create copies of data in memory. By pre-allocating the maximum size, you avoid these copies and maintain better control over where your sensitive data resides.

5. **Pin Memory**: 
   Use techniques like [`stackalloc`](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/stackalloc), [`GC.AllocateArray()`](https://learn.microsoft.com/en-us/dotnet/api/system.gc.allocatearray), [`fixed` statement](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/fixed), or [`GCHandle.Alloc()`](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.gchandle.alloc) to prevent unintended copies.

   These techniques prevent the garbage collector from moving your sensitive data around in memory, reducing the risk of leaving copies in various memory locations.

### Avoid Using:

- **Strings**: 
  Cannot be securely wiped without risking runtime crashes.
  Strings are immutable and internally optimized by the runtime, making it impossible to guarantee that all copies of the string have been securely erased.

- **SecureString**: 
  No longer recommended by Microsoft for new development.
  SecureString was designed to mitigate some risks, but its implementation is platform-specific and doesn't provide significant advantages over properly managed byte arrays.

## Implementation Examples

### Buffer Management

```csharp
private const int MaxPinLength = 8;

var pin = new byte[MaxPinLength];

public void CollectPin(byte[] pin)
{
    // Implementation to safely collect PIN
    // This method should handle the PIN input and store it directly in the provided byte array
    // It should also ensure that no more than MaxPinLength bytes are written
}
```

### Secure Usage Pattern
```csharp
var managementKey = new byte[ManagementKeySize];

try
{
    CollectManagementKey(managementKey);
    AuthenticateManagementKey(managementKey);
    CryptographicOperations.ZeroMemory(managementKey);

    // Continue with operation
}
finally
{
    CryptographicOperations.ZeroMemory(managementKey);
}
```

This pattern ensures that the management key is cleared from memory even if an exception occurs during the authentication process.

## Conclusion
While these practices significantly reduce risks, they don't guarantee complete security. Always consider the specific security requirements of your application and the potential threats in your environment. Regular security audits and staying updated with the latest security best practices are crucial in maintaining the security of sensitive data handling in your applications.