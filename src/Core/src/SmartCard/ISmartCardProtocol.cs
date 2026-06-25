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

using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.Core.SmartCard;

/// <summary>
///     Protocol interface for SmartCard communication.
/// </summary>
public interface ISmartCardProtocol : IProtocol
{
    /// <summary>
    /// Transmits an APDU command and returns the response.
    /// </summary>
    /// <param name="command">The APDU command to transmit.</param>
    /// <param name="throwOnError">
    /// When <c>true</c> (default), throws <see cref="ApduException"/> for non-success status words.
    /// When <c>false</c>, returns the response without throwing.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The APDU response containing data and status word.</returns>
    Task<ApduResponse> TransmitAndReceiveAsync(
        ApduCommand command,
        bool throwOnError = true,
        CancellationToken cancellationToken = default);

    Task<ReadOnlyMemory<byte>> SelectAsync(ReadOnlyMemory<byte> applicationId,
        CancellationToken cancellationToken = default);
}