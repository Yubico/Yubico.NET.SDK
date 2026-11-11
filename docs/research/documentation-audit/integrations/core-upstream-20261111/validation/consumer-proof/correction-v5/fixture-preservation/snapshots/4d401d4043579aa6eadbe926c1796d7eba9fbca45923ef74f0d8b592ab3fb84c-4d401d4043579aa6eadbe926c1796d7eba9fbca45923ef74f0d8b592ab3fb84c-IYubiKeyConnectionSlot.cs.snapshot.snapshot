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

using Yubico.YubiKit.Core.Abstractions;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     One concrete transport slot that can open its raw, unregistered connection.
/// </summary>
internal interface IYubiKeyConnectionSlot
{
    /// <summary>The stable identifier for this enumerated interface.</summary>
    string InterfaceId { get; }

    /// <summary>The single concrete connection exposed by this slot.</summary>
    ConnectionType ConnectionType { get; }

    /// <summary>Opens this slot's raw, unregistered connection.</summary>
    /// <remarks>
    ///     The default exists for non-openable or fake slot implementations. It throws the narrow
    ///     <see cref="NonOpenableConnectionSlotException" /> so published-device discovery can translate
    ///     that case to a typed skip without misclassifying failures from real transport implementations.
    /// </remarks>
    Task<IConnection> OpenRawConnectionAsync(
        ConnectionType connection,
        CancellationToken cancellationToken = default) =>
        Task.FromException<IConnection>(
            new NonOpenableConnectionSlotException(
                $"Connection type {connection} is not supported by the {GetType().Name} slot."));
}

/// <summary>Identifies a slot that relies on the non-openable default raw-connection implementation.</summary>
internal sealed class NonOpenableConnectionSlotException(string message) : NotSupportedException(message);