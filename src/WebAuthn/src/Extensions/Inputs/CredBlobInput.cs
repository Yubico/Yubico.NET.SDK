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

namespace Yubico.YubiKit.WebAuthn.Extensions.Inputs;

/// <summary>
/// Input for the credBlob extension during registration.
/// </summary>
/// <remarks>
/// Allows storing a small blob of data (typically 1-32 bytes) with the credential.
/// The blob is retrieved during assertions.
/// </remarks>
/// <param name="Blob">The blob data to store (must be 1-32 bytes per CTAP2.1).</param>
public sealed record class CredBlobInput(ReadOnlyMemory<byte> Blob)
{
    /// <summary>
    /// Validates that the blob size is within CTAP2 limits.
    /// </summary>
    public void Validate()
    {
        if (Blob.Length is < 1 or > 32)
        {
            throw new ArgumentException(
                "CredBlob must be between 1 and 32 bytes",
                nameof(Blob));
        }
    }
}
