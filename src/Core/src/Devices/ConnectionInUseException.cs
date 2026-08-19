// Copyright 2026 Yubico AB
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

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     Thrown when this process already holds the physical YubiKey or connection being requested.
/// </summary>
/// <remarks>
///     <para>
///         Connection and session acquisition raise this at two scopes:
///     </para>
///     <list type="bullet">
///         <item>
///             Opening a second connection to any known interface of a grouped physical YubiKey. The first
///             connection claims every stable member interface ID, so the refusal occurs before native open.
///         </item>
///         <item>
///             Creating a second session on a connection that already hosts a live one. Same mechanism,
///             one level down: the new session's SELECT runs on the same card channel.
///         </item>
///     </list>
///     <para>
///         Both are refused at acquisition, before any command reaches the device, so the exception lands on
///         the call that would have caused the damage rather than on the victim's next operation. Dispose the
///         current holder first; a connection may host any number of sessions in sequence, and an exclusive
///         physical key any number of connections in sequence.
///     </para>
///     <para>
///         In-process only. A different process holding the card surfaces as a PC/SC sharing violation
///         instead.
///     </para>
/// </remarks>
public sealed class ConnectionInUseException : InvalidOperationException
{
    /// <inheritdoc cref="ConnectionInUseException" />
    public ConnectionInUseException()
        : base("The connection is already in use by another holder in this process.")
    {
    }

    /// <inheritdoc cref="ConnectionInUseException" />
    public ConnectionInUseException(string message)
        : base(message)
    {
    }

    /// <inheritdoc cref="ConnectionInUseException" />
    public ConnectionInUseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}