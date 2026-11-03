// Isolate the array-to-nullable-memory conversion used by LargeBlobProcessor.
// This performs no SDK/device operation and does not test blob-storage behavior.
using System;

byte[]? missing = null;
var result = new Output(Blob: missing);
Console.WriteLine($"Null byte[] argument: HasValue={result.Blob.HasValue}; Length={result.Blob?.Length}");
if (result.Blob is not { Length: 0 })
{
    throw new InvalidOperationException("Expected present empty memory from the implicit array conversion.");
}
Console.WriteLine("PASS: this constructor shape converts a null byte[] into a present empty ReadOnlyMemory<byte>.");

sealed record Output(ReadOnlyMemory<byte>? Blob = null);
