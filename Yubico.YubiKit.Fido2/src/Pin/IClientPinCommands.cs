// Copyright 2025 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Yubico.YubiKit.Fido2.Pin;

/// <summary>
/// Interface for sending CTAP ClientPin commands.
/// </summary>
/// <remarks>
/// This interface allows for dependency injection and unit testing of ClientPin operations.
/// </remarks>
internal interface IClientPinCommands
{
    /// <summary>
    /// Sends a CTAP request and returns the response.
    /// </summary>
    /// <param name="request">The serialized CTAP request (command byte + CBOR).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The CBOR-encoded response data.</returns>
    Task<ReadOnlyMemory<byte>> SendRequestAsync(
        ReadOnlyMemory<byte> request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of <see cref="IClientPinCommands"/> that uses a <see cref="FidoSession"/>.
/// </summary>
internal sealed class FidoSessionClientPinCommands : IClientPinCommands
{
    private readonly FidoSession _session;
    
    public FidoSessionClientPinCommands(FidoSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }
    
    public Task<ReadOnlyMemory<byte>> SendRequestAsync(
        ReadOnlyMemory<byte> request,
        CancellationToken cancellationToken = default)
    {
        return _session.SendCborRequestAsync(request, cancellationToken);
    }
}
