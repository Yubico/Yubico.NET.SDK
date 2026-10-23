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

namespace Yubico.YubiKit.Fido2.LargeBlobs;

/// <summary>
/// Per-credential large-blob storage over authenticatorLargeBlobs (CTAP 2.1-2.3 §6.10). Implemented by
/// <see cref="LargeBlobStorage"/>; an interface so clients built on it can be tested without a device.
/// </summary>
public interface ILargeBlobStorage
{
    /// <summary>Reads the whole array; a short or corrupt array is the initial (empty) array (§6.10.2).</summary>
    Task<LargeBlobArray> ReadLargeBlobArrayAsync(CancellationToken cancellationToken = default);

    /// <summary>Writes the whole array; requires a token with LargeBlobWrite (§6.10.2).</summary>
    Task WriteLargeBlobArrayAsync(LargeBlobArray array, CancellationToken cancellationToken = default);

    /// <summary>Returns the blob stored for the credential with this largeBlobKey, or null (§6.10.4).</summary>
    Task<byte[]?> GetBlobAsync(ReadOnlyMemory<byte> largeBlobKey, CancellationToken cancellationToken = default);

    /// <summary>Stores the blob for the credential with this largeBlobKey, replacing any existing one (§6.10.5).</summary>
    Task SetBlobAsync(ReadOnlyMemory<byte> largeBlobKey, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);

    /// <summary>Removes the blob for the credential with this largeBlobKey; false when there was none.</summary>
    Task<bool> DeleteBlobAsync(ReadOnlyMemory<byte> largeBlobKey, CancellationToken cancellationToken = default);
}