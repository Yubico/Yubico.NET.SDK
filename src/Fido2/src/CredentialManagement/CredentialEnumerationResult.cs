// Copyright 2026 Yubico AB
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

namespace Yubico.YubiKit.Fido2.CredentialManagement;

/// <summary>
/// Represents the result of enumerating discoverable credentials for a relying party and owns the
/// returned credential entries.
/// </summary>
public sealed class CredentialEnumerationResult : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Gets the credentials in authenticator enumeration order.
    /// </summary>
    public IReadOnlyList<StoredCredentialInfo> Credentials { get; }

    /// <summary>
    /// Gets the total reported by the first authenticator response, if present.
    /// </summary>
    public int? ReportedTotal { get; }

    internal CredentialEnumerationResult(List<StoredCredentialInfo> credentials, int? reportedTotal)
    {
        Credentials = credentials.AsReadOnly();
        ReportedTotal = reportedTotal;
    }

    /// <summary>
    /// Disposes every credential owned by this result.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (StoredCredentialInfo credential in Credentials)
        {
            credential.Dispose();
        }

        _disposed = true;
    }
}