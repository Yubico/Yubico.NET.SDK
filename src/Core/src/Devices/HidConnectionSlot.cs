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

using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.Hid.Linux;
using Yubico.YubiKit.Core.Transports.Hid.MacOS;
using Yubico.YubiKit.Core.Transports.Hid.Windows;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>One live enumerated HID interface candidate that can open its raw FIDO or OTP connection.</summary>
internal sealed class HidConnectionSlot : IYubiKeyConnectionSlot, IDiscoveryConnectionProvider
{
    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<HidConnectionSlot>();

    private readonly IHidInterface _hidInterface;
    private readonly IMacOSHidInputBridge? _inputBridge;
    private readonly IMacOSHidReportApi? _macOSReportApi;
    private readonly ILinuxHidReportApi? _linuxReportApi;
    private readonly ILinuxHidReadReactor? _linuxReadReactor;
    private readonly Func<IWindowsHidReportApi> _windowsReportApiFactory;
    private readonly Func<IWindowsHidFeatureReportApi> _windowsFeatureReportApiFactory;

    // Windows factories create one fresh, unopened API per acquisition. API instances are stateful and
    // belong exclusively to the receiving owner; neither a second slot nor a reopen may reuse them.
    internal HidConnectionSlot(IHidInterface hidInterface, IMacOSHidInputBridge? inputBridge = null,
        IMacOSHidReportApi? macOSReportApi = null, ILinuxHidReportApi? linuxReportApi = null,
        ILinuxHidReadReactor? linuxReadReactor = null,
        Func<IWindowsHidReportApi>? windowsReportApiFactory = null,
        Func<IWindowsHidFeatureReportApi>? windowsFeatureReportApiFactory = null)
    {
        _hidInterface = hidInterface;
        _inputBridge = inputBridge;
        _macOSReportApi = macOSReportApi;
        _linuxReportApi = linuxReportApi;
        _linuxReadReactor = linuxReadReactor;
        _windowsReportApiFactory = windowsReportApiFactory ?? (static () => new WindowsHidReportApi());
        _windowsFeatureReportApiFactory = windowsFeatureReportApiFactory ?? (static () => new WindowsHidFeatureReportApi());
        InterfaceId = $"hid:{hidInterface.ReaderName}:{hidInterface.DescriptorInfo.Usage:X4}";
        ConnectionType = ConnectionTypeMapper.ToConnectionType(hidInterface.InterfaceType)
            .SingleConcreteConnectionOrUnknown();
        if (ConnectionType == ConnectionType.Unknown)
        {
            throw new NotSupportedException(
                $"HID interface type {hidInterface.InterfaceType} is not supported as a connection slot.");
        }
    }

    public string InterfaceId { get; }

    /// <summary>Either <see cref="ConnectionType.HidFido" /> or <see cref="ConnectionType.HidOtp" /> (ctor-enforced).</summary>
    public ConnectionType ConnectionType { get; }

    public Task<IConnection> OpenRawConnectionAsync(
        ConnectionType connection,
        CancellationToken cancellationToken = default)
    {
        if (connection != ConnectionType)
        {
            return Task.FromException<IConnection>(new NotSupportedException(
                $"Connection type {connection} is not supported by this device connection slot."));
        }

        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IConnection>(cancellationToken);

        Logger.LogInformation(
            "Connecting to {ConnectionType} HID interface VID={VendorId:X4} PID={ProductId:X4}",
            ConnectionType,
            _hidInterface.DescriptorInfo.VendorId,
            _hidInterface.DescriptorInfo.ProductId);

        if (OwnsNativeLifetime)
            return OpenBuiltInConnectionAsync(physicalClaim: null, cancellationToken);

        // Custom interfaces retain the synchronous compatibility adapters.
        return Task.FromResult<IConnection>(ConnectionType == ConnectionType.HidFido
            ? new FidoHidConnection(_hidInterface.ConnectToIOReports())
            : new OtpHidConnection(_hidInterface.ConnectToFeatureReports()));
    }

    /// <summary>The built-in connection accepts its claim before acquiring native resources.</summary>
    internal bool OwnsNativeLifetime => _hidInterface is MacOSHidInterface or LinuxHidInterface or WindowsHidInterface;

    /// <summary>Transfers a registry connection or discovery claim to the built-in native owner before open.</summary>
    /// <param name="physicalClaim">
    /// The opaque release token acquired by <see cref="YubiKeyDevice" /> or discovery. The native owner,
    /// not the caller, releases it after proven native cleanup, including failed or canceled opens;
    /// uncertain release retains the claim. It is not the connection being opened.
    /// </param>
    /// <param name="cancellationToken">Requests cancellation without detaching native acquisition or cleanup.</param>
    /// <returns>The opened connection after the owner has accepted responsibility for the claim.</returns>
    internal Task<IConnection> OpenRegisteredConnectionAsync(IDisposable physicalClaim, CancellationToken cancellationToken) =>
        OpenBuiltInConnectionAsync(physicalClaim, cancellationToken);

    /// <summary>Opens a built-in connection with an optional transferred physical claim.</summary>
    /// <param name="physicalClaim">
    /// Registry/discovery release token, or <see langword="null" /> for an unregistered raw open.
    /// Forwarded unchanged: the registry encapsulates concrete token types and quarantine handling.
    /// </param>
    /// <param name="cancellationToken">Passed to the native owner even when already canceled.</param>
    /// <remarks>
    /// Do not short-circuit cancellation here: after transfer the native owner alone releases the claim,
    /// including a pre-canceled open. Raw callers checked cancellation before reaching this route.
    /// </remarks>
    private async Task<IConnection> OpenBuiltInConnectionAsync(IDisposable? physicalClaim, CancellationToken cancellationToken) =>
        (_hidInterface, ConnectionType) switch
        {
            (MacOSHidInterface device, ConnectionType.HidFido) =>
                await MacOSFidoHidConnection.OpenAsync(device.EntryId, _inputBridge ?? new MacOSHidInputBridge(), cancellationToken, physicalClaim).ConfigureAwait(false),
            (MacOSHidInterface device, ConnectionType.HidOtp) =>
                await MacOSOtpHidConnection.OpenAsync(device.EntryId, _macOSReportApi ?? MacOSHidReportApi.Instance, cancellationToken, physicalClaim).ConfigureAwait(false),
            (LinuxHidInterface device, ConnectionType.HidFido) =>
                await LinuxFidoHidConnection.OpenAsync(device.ReaderName, _linuxReportApi ?? LinuxHidReportApi.Instance,
                    _linuxReadReactor ?? LinuxHidReadReactor.Shared, cancellationToken, physicalClaim).ConfigureAwait(false),
            (LinuxHidInterface device, ConnectionType.HidOtp) =>
                await LinuxOtpHidConnection.OpenAsync(device.ReaderName, _linuxReportApi ?? LinuxHidReportApi.Instance,
                    cancellationToken, physicalClaim).ConfigureAwait(false),
            (WindowsHidInterface device, ConnectionType.HidFido) =>
                await WindowsFidoHidConnection.OpenAsync(device.ReaderName,
                    CreateWindowsApi(_windowsReportApiFactory, physicalClaim),
                    cancellationToken, physicalClaim).ConfigureAwait(false),
            (WindowsHidInterface device, ConnectionType.HidOtp) =>
                await WindowsOtpHidConnection.OpenAsync(device.ReaderName,
                    CreateWindowsApi(_windowsFeatureReportApiFactory, physicalClaim),
                    cancellationToken, physicalClaim).ConfigureAwait(false),
            _ => throw new InvalidOperationException("Only built-in HID connections own their claims before native open.")
        };

    private static T CreateWindowsApi<T>(Func<T> factory, IDisposable? physicalClaim) where T : class
    {
        // Factories construct per-owner state only. No native acquisition is allowed before owner transfer.
        // If construction fails, no owner exists; this is the explicitly no-native release path.
        try { return factory() ?? throw new InvalidOperationException("The Windows report API factory returned null."); }
        catch (Exception constructionError)
        {
            try { physicalClaim?.Dispose(); }
            catch (Exception releaseError) { throw new AggregateException(constructionError, releaseError); }
            throw;
        }
    }

    Task<IConnection> IDiscoveryConnectionProvider.ConnectForDiscoveryAsync(
        ConnectionType connection,
        CancellationToken cancellationToken) =>
        OpenRawConnectionAsync(connection, cancellationToken);
}