// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Yubico.YubiKit.Ctap2.LargeBlobs;

/// <summary>
/// Per-credential large-blob storage over authenticatorLargeBlobs (CTAP 2.1-2.3 §6.10). Implemented by
/// <see cref="LargeBlobStorage"/>; an interface so clients built on it can be tested without a device.
/// </summary>
/// <remarks>
/// LargeBlobStorage uses the largeBlobKey response-envelope route, not direct largeBlob extension
/// outputs. It borrows its session/protocol/token and caches GetInfo limits per instance. Callers must
/// coordinate whole multi-command operations on the shared array/session; no transaction, conflict
/// detection, rollback or resume is provided. Cancellation/failure can follow device changes.
/// Actual writes require protocol/token configuration in this SDK even where CTAP permits an
/// unverified write. Keep borrowed key/data storage stable through each await; clear caller-owned
/// secrets and returned plaintext afterward. SDK internal copies are not comprehensively cleared.
/// </remarks>
public interface ILargeBlobStorage
{
    /// <summary>Reads the whole array; a short or corrupt array is the initial (empty) array (§6.10.2).</summary>
    Task<LargeBlobArray> ReadLargeBlobArrayAsync(CancellationToken cancellationToken = default);

    /// <summary>Replaces the whole shared array; this SDK requires a protocol/token with LargeBlobWrite.</summary>
    Task WriteLargeBlobArrayAsync(LargeBlobArray array, CancellationToken cancellationToken = default);

    /// <summary>Returns caller-owned plaintext to clear after use, or null when no entry decrypts within the SDK read cap.</summary>
    Task<byte[]?> GetBlobAsync(ReadOnlyMemory<byte> largeBlobKey, CancellationToken cancellationToken = default);

    /// <summary>Reads, modifies and rewrites the full array to replace matching decryptable entries (§6.10.5).</summary>
    Task SetBlobAsync(ReadOnlyMemory<byte> largeBlobKey, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);

    /// <summary>Rewrites the array without matching decryptable entries; false when none matched within the read cap.</summary>
    Task<bool> DeleteBlobAsync(ReadOnlyMemory<byte> largeBlobKey, CancellationToken cancellationToken = default);
}