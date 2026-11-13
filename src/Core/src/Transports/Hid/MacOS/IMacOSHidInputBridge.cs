// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

namespace Yubico.YubiKit.Core.Transports.Hid.MacOS;

/// <summary>
///     Provides device operations and persistent native input delivery for the macOS FIDO HID owner.
/// </summary>
/// <remarks>
///     <para>
///         This internal seam supplies native mechanisms, not connection admission or physical-claim
///         ownership. <see cref="MacOSFidoHidNativeOwner" /> controls operation and shutdown ordering.
///         Device references and input-owner handles have separate lifetimes: destroy the input owner
///         before releasing the device reference.
///     </para>
///     <para>
///         Canceling input requests shutdown; it does not acknowledge shutdown or release resources.
///         The caller checks <see cref="WaitShutdown" /> and <see cref="Destroy" /> before treating
///         native input as released. Report callback memory is borrowed only until the callback returns.
///     </para>
/// </remarks>
internal interface IMacOSHidInputBridge
{
    /// <summary>
    ///     Resolves an IO registry entry ID to a retained, unopened <c>IOHIDDeviceRef</c>.
    /// </summary>
    /// <param name="entryId">The IO registry entry ID of the HID interface.</param>
    /// <returns>A device reference owned by the caller, to be released with <see cref="ReleaseDevice" />.</returns>
    /// <exception cref="Yubico.YubiKit.Core.Native.PlatformApiException">
    ///     The production bridge could not find the registry entry or create the device reference.
    /// </exception>
    nint CreateDevice(long entryId);

    /// <summary>
    ///     Opens a device without seizing it, throwing if the native open fails.
    /// </summary>
    /// <param name="device">The device reference returned by <see cref="CreateDevice" />.</param>
    /// <exception cref="Yubico.YubiKit.Core.Native.PlatformApiException">
    ///     The production bridge received a nonzero IOKit open status.
    /// </exception>
    void OpenDevice(nint device);

    /// <summary>
    ///     Opens a device without seizing it and returns the native open status.
    /// </summary>
    /// <param name="device">The device reference returned by <see cref="CreateDevice" />.</param>
    /// <returns>Zero on success; otherwise, the IOKit open error status.</returns>
    /// <remarks>
    ///     The default implementation calls <see cref="OpenDevice" /> and returns zero if it completes;
    ///     exceptions are not converted to status codes. The production override preserves the native
    ///     status, including <c>kIOReturnExclusiveAccess</c> (<c>0xE00002C5</c>), which the owner treats
    ///     as an attempted open requiring close even though the open failed.
    /// </remarks>
    int OpenDeviceResult(nint device)
    {
        OpenDevice(device);
        return 0;
    }
    /// <summary>
    ///     Reads the device's maximum input report size.
    /// </summary>
    /// <param name="device">The device reference whose input metadata is read.</param>
    /// <returns>The <c>MaxInputReportSize</c> property value, in bytes.</returns>
    int InputSize(nint device);

    /// <summary>
    ///     Reads the device's maximum output report size.
    /// </summary>
    /// <param name="device">The device reference whose output metadata is read.</param>
    /// <returns>The maximum output report size, in bytes.</returns>
    /// <remarks>
    ///     The default implementation returns <see cref="InputSize" />. The production bridge reads
    ///     the separate <c>MaxOutputReportSize</c> property.
    /// </remarks>
    int OutputSize(nint device) => InputSize(device);

    /// <summary>
    ///     Performs a checked device close when native input has not successfully started.
    /// </summary>
    /// <param name="device">The device reference for an open attempt requiring cleanup.</param>
    /// <returns><see langword="true" /> if native close returns zero; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    ///     Closing does not release the device reference. A failed close is not release proof; the
    ///     caller must not proceed as though cleanup succeeded.
    /// </remarks>
    bool CloseUnstarted(nint device);

    /// <summary>
    ///     Releases the caller-owned CoreFoundation device reference.
    /// </summary>
    /// <param name="device">The retained device reference returned by <see cref="CreateDevice" />.</param>
    /// <remarks>
    ///     This is not a device close or an input shutdown request. The caller must first complete
    ///     the required input-owner destruction and device-close cleanup.
    /// </remarks>
    void ReleaseDevice(nint device);

    /// <summary>
    ///     Creates a persistent input owner and retains its managed callback state until successful destruction.
    /// </summary>
    /// <param name="device">The open device reference, retained by the caller through input-owner cleanup.</param>
    /// <param name="size">The maximum input report size, in bytes, supplied to the native input owner.</param>
    /// <param name="report">
    ///     Receives report memory borrowed only for the callback invocation. Copy any bytes that must
    ///     survive callback return; the production bridge clears its temporary copy after delivery.
    /// </param>
    /// <param name="terminal">
    ///     Receives an input terminal reason: removed (1), overflow (2), or fault (3). This notification
    ///     is not shutdown acknowledgment or permission to release callback state.
    /// </param>
    /// <returns>An input-owner handle to pass to <see cref="Start" /> and later <see cref="Destroy" />.</returns>
    /// <remarks>
    ///     The production bridge permits one input owner at a time. Callback exceptions are contained;
    ///     report delivery failures are reported as terminal faults rather than escaping into native code.
    /// </remarks>
    /// <exception cref="PlatformNotSupportedException">The production bridge is not running on macOS.</exception>
    /// <exception cref="InvalidOperationException">
    ///     The production bridge already retains an owner or callback state, or native creation failed.
    /// </exception>
    nint CreateInput(nint device, int size, Action<ReadOnlyMemory<byte>> report, Action<int> terminal);

    /// <summary>
    ///     Starts persistent input delivery for a created input owner.
    /// </summary>
    /// <param name="owner">The input-owner handle returned by <see cref="CreateInput" />.</param>
    /// <returns>A native <c>HidInputResult</c> code: zero (<c>Ok</c>) on success, or a nonzero failure code.</returns>
    /// <remarks>A failed start does not destroy the input owner.</remarks>
    int Start(nint owner);

    /// <summary>
    ///     Requests input shutdown without waiting for acknowledgment.
    /// </summary>
    /// <param name="owner">The started input-owner handle.</param>
    /// <remarks>
    ///     The caller must still check <see cref="WaitShutdown" /> and <see cref="Destroy" /> before
    ///     releasing resources. This request does not cancel an individual managed receive operation.
    /// </remarks>
    void Cancel(nint owner);

    /// <summary>
    ///     Waits for native input shutdown acknowledgment without a finite timeout in the production bridge.
    /// </summary>
    /// <param name="owner">The started input-owner handle for which shutdown was requested.</param>
    /// <returns>
    ///     A native <c>HidInputResult</c> code. <c>Ok</c> (0) and <c>Fault</c> (4) are accepted by the
    ///     connection owner as native quiescence; <c>Fault</c> also reports a terminal input error.
    ///     Other results do not establish the acknowledgment required for cleanup.
    /// </returns>
    /// <remarks>This does not destroy the owner or release its managed callback state.</remarks>
    int WaitShutdown(nint owner);

    /// <summary>
    ///     Destroys the input owner, releasing managed callback roots only when native destruction succeeds.
    /// </summary>
    /// <param name="owner">The input-owner handle to destroy.</param>
    /// <returns>A native <c>HidInputResult</c> code: zero (<c>Ok</c>) on success, or a nonzero failure code.</returns>
    /// <remarks>
    ///     After a successful start, the caller first requests and acknowledges shutdown. An owner
    ///     whose start failed is also destroyed during cleanup. A nonzero result retains the production
    ///     bridge's owner handle and callback roots; it is not permission to release the device reference.
    /// </remarks>
    int Destroy(nint owner);

    /// <summary>
    ///     Synchronously sends an output report with report ID zero to the open device.
    /// </summary>
    /// <param name="device">The open device reference.</param>
    /// <param name="report">
    ///     The complete output payload, borrowed until this call returns. The production bridge sends
    ///     exactly this array's length without adding a report-ID byte or clearing the caller's buffer.
    /// </param>
    /// <exception cref="Yubico.YubiKit.Core.Native.PlatformApiException">
    ///     The production bridge received a nonzero <c>IOHIDDeviceSetReport</c> status.
    /// </exception>
    void SetReport(nint device, byte[] report);
}