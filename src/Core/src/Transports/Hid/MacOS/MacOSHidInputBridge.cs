// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Native.MacOS.IOKitFramework;
using HidInputInterop = Yubico.YubiKit.Core.Native.MacOS.HidInput.NativeMethods;
using HidInputResult = Yubico.YubiKit.Core.Native.MacOS.HidInput.HidInputResult;
using HidInputTerminalReason = Yubico.YubiKit.Core.Native.MacOS.HidInput.HidInputTerminalReason;

namespace Yubico.YubiKit.Core.Transports.Hid.MacOS;

internal sealed unsafe class MacOSHidInputBridge : IMacOSHidInputBridge
{
    private GCHandle _context;
    private nint _owner;

    // Native callbacks borrow the context until destroy succeeds.
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnNativeReport(nint ctx, nint data, nuint length)
    {
        try
        {
            ((CallbackState)GCHandle.FromIntPtr(ctx).Target!).Report(data, length);
        }
        catch { /* Never throw through the C callback ABI. */ }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnNativeTerminal(nint ctx, int reason)
    {
        try
        {
            ((CallbackState)GCHandle.FromIntPtr(ctx).Target!).Terminal(reason);
        }
        catch { /* Never throw through the C callback ABI. */ }
    }

    internal sealed class CallbackState(Action<ReadOnlyMemory<byte>> report, Action<int> terminal)
    {
        internal void Report(nint data, nuint length)
        {
            try
            {
                if (length is 0 or > MacOSFidoHidConnection.MaxInputReportLength)
                {
                    Terminal(HidInputTerminalReason.Fault);
                    return;
                }
                var copy = new byte[(int)length];
                try
                {
                    Marshal.Copy(data, copy, 0, copy.Length);
                    Deliver(copy);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(copy);
                }
            }
            catch { Terminal(HidInputTerminalReason.Fault); }
        }

        internal void Deliver(ReadOnlyMemory<byte> packet)
        {
            try { report(packet); }
            catch { Terminal(HidInputTerminalReason.Fault); }
        }

        internal void Terminal(int reason)
        {
            try { terminal(reason); }
            catch { /* Callbacks cannot escape into C, even if terminal delivery faults. */ }
        }
    }

    public nint CreateDevice(long entryId) => MacOSHidReportApi.Instance.CreateDevice(entryId);
    public void OpenDevice(nint device) => MacOSHidReportApi.Instance.OpenDevice(device);
    public int OpenDeviceResult(nint device) => MacOSHidReportApi.Instance.OpenDeviceResult(device);
    public int InputSize(nint device) => MacOSHidReportApi.Instance.GetIntProperty(device, IOKitHidConstants.MaxInputReportSize);
    public int OutputSize(nint device) => MacOSHidReportApi.Instance.GetIntProperty(device, IOKitHidConstants.MaxOutputReportSize);
    public bool CloseUnstarted(nint device) => NativeMethods.IOHIDDeviceClose(device, 0) == 0;
    public void ReleaseDevice(nint device) => MacOSHidReportApi.Instance.ReleaseCFObject(device);
    public void SetReport(nint device, byte[] report)
    {
        var status = NativeMethods.IOHIDDeviceSetReport(device, IOKitHidConstants.kIOHidReportTypeOutput, 0, report, report.Length);
        if (status != 0)
        {
            throw new Yubico.YubiKit.Core.Native.PlatformApiException(nameof(NativeMethods.IOHIDDeviceSetReport), status,
                "Failed to set HID report.");
        }
    }

    public nint CreateInput(nint device, int size, Action<ReadOnlyMemory<byte>> report, Action<int> terminal)
    {
        // Guard each shim call so Native AOT does not require macOS-only exports on Linux or Windows.
        if (!OperatingSystem.IsMacOS()) throw new PlatformNotSupportedException();
        if (_owner != 0 || _context.IsAllocated)
        {
            throw new InvalidOperationException("This native input bridge already owns a device.");
        }
        _context = GCHandle.Alloc(new CallbackState(report, terminal));
        try
        {
            // Native and managed delivery each have a separately bounded queue.
            _owner = HidInputInterop.HidInputCreate(device, (nuint)size, MacOSFidoHidConnection.ReportCapacity,
                &OnNativeReport, &OnNativeTerminal,
                GCHandle.ToIntPtr(_context));
        }
        catch
        {
            _context.Free();
            throw;
        }
        if (_owner != 0)
        {
            return _owner;
        }
        _context.Free();
        throw new InvalidOperationException("Native_HidInputCreate failed");
    }
    public int Start(nint owner) => OperatingSystem.IsMacOS()
        ? HidInputInterop.HidInputStart(owner)
        : throw new PlatformNotSupportedException();
    public void Cancel(nint owner)
    {
        if (!OperatingSystem.IsMacOS()) throw new PlatformNotSupportedException();
        HidInputInterop.HidInputCancel(owner);
    }
    public int WaitShutdown(nint owner) => OperatingSystem.IsMacOS()
        ? HidInputInterop.HidInputWaitShutdown(owner, uint.MaxValue)
        : throw new PlatformNotSupportedException();
    public int Destroy(nint owner)
    {
        if (!OperatingSystem.IsMacOS()) throw new PlatformNotSupportedException();
        var result = HidInputInterop.HidInputDestroy(owner);
        if (result == HidInputResult.Ok)
        {
            _context.Free();
            _owner = 0;
        }
        return result;
    }
}