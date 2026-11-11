// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Yubico.YubiKit.Management;

/// <summary>Supplies a reboot request and configuration lock codes when applying a device configuration.</summary>
/// <remarks>
///     Lock-code memory is borrowed, not copied by this options object. Keep the buffers valid and unchanged
///     until the configuration operation completes, including any asynchronous session creation by a one-shot
///     extension. The caller retains ownership and must clear sensitive input with
///     <see cref="System.Security.Cryptography.CryptographicOperations.ZeroMemory" /> in a
///     <see langword="finally" /> block after completion, including failure or cancellation. The session does
///     not retain the options object or clear the source buffers; an options object you keep still references them.
/// </remarks>
public sealed class SetDeviceConfigOptions
{
    /// <summary>Gets whether to request a YubiKey reboot after the configuration is accepted.</summary>
    /// <remarks>
    ///     The default is <see langword="false" />, which omits the reboot request. A
    ///     <see langword="true" /> value requests a reboot only if the device accepts the configuration,
    ///     including any required lock-code authorization. This is not a factory reset. A reboot disconnects
    ///     the device; dispose existing sessions and rediscover the device before opening new ones.
    /// </remarks>
    public bool Reboot { get; init; }

    /// <summary>Gets the borrowed current configuration lock code used to authorize this command.</summary>
    /// <remarks>
    ///     Supply the correct 16-byte code for each protected configuration command, including a command
    ///     that replaces or removes the code. This does not unlock the session or remove the persistent lock.
    ///     A <see langword="null" /> value omits authorization; the device checks whether a code is required
    ///     and correct. The session rejects a present value whose length is not 16 bytes when applying the
    ///     configuration; an empty buffer is not equivalent to <see langword="null" />.
    /// </remarks>
    public ReadOnlyMemory<byte>? CurrentLockCode { get; init; }

    /// <summary>Gets the borrowed replacement configuration lock code.</summary>
    /// <remarks>
    ///     A <see langword="null" /> value leaves the lock unchanged. Supply 16 bytes that are not all zero
    ///     to set or replace the lock code, or sixteen zero bytes to remove the lock. If the configuration is
    ///     already locked, supply <see cref="CurrentLockCode" /> in the same operation. The session rejects
    ///     a present value whose length is not 16 bytes when applying the configuration; an empty buffer
    ///     does not remove the lock.
    /// </remarks>
    public ReadOnlyMemory<byte>? NewLockCode { get; init; }
}