using System;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace AuditCt001.Prepared.guide_memory;

public static class Example
{
public static void Run(byte[] pinBytes)
{
// With byte[] pinBytes supplied by the caller, in a synchronous method.
Span<byte> pinHash = stackalloc byte[32];
try
{
    SHA256.HashData(pinBytes, pinHash);
}
finally
{
    CryptographicOperations.ZeroMemory(pinHash);
    CryptographicOperations.ZeroMemory(pinBytes);
}
}
}
