// Copyright 2025 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.Protocols.Fido.Hid;

/// <summary>
/// Protocol interface for FIDO HID communication using CTAP HID framing.
/// Supports CTAP HID channel initialization and YubiKey Management vendor commands.
/// </summary>
/// <remarks>
///     Implementations admit one full logical exchange at a time. An overlapping operation throws
///     <see cref="InvalidOperationException" /> immediately. A token is checked before entry; an exchange already
///     in flight runs to completion to avoid stranding the device mid-transaction.
/// </remarks>
internal interface IFidoHidProtocol : IProtocol
{
    /// <summary>
    /// Initializes the CTAP HID channel if it has not already been initialized.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>Sends a raw CTAP HID command without an SDK user-presence notification.</summary>
    /// <param name="command">The CTAPHID command byte, not a CTAP2 operation code.</param>
    /// <param name="data">The command payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response data from the YubiKey.</returns>
    Task<ReadOnlyMemory<byte>> SendCommandAsync(
        byte command,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default) =>
        SendCommandAsync(command, data, UserPresenceNotification.None, cancellationToken);

    /// <summary>
    /// Sends a CTAPHID command (including standard and vendor commands) and receives the response.
    /// Used for Management application over HID.
    /// </summary>
    /// <param name="command">The CTAPHID command byte (e.g., 0xC2 for READ_CONFIG), not a CTAP2 operation code.</param>
    /// <param name="data">The command payload.</param>
    /// <param name="userPresenceNotification">
    ///     The non-null operation handle requested only for authoritative user-presence keep-alive status.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response data from the YubiKey.</returns>
    Task<ReadOnlyMemory<byte>> SendCommandAsync(
        byte command,
        ReadOnlyMemory<byte> data,
        UserPresenceNotification userPresenceNotification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether the HID channel has been initialized.
    /// </summary>
    bool IsChannelInitialized { get; }

    /// <summary>
    /// Gets the firmware version reported during channel initialization.
    /// </summary>
    FirmwareVersion? FirmwareVersion { get; }
}