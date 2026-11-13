// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Runtime.InteropServices;
using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Native.Windows.Kernel32;
using HidNative = Yubico.YubiKit.Core.Native.Windows.HidD.NativeMethods;
using Kernel = Yubico.YubiKit.Core.Native.Windows.Kernel32.NativeMethods;

namespace Yubico.YubiKit.Core.Transports.Hid.Windows;

/// <summary>Worker-directed synchronous feature access with checked native resource release.</summary>
/// <remarks>
/// The owner serializes Open, GET/SET and CloseOnce. No native call retains the borrowed report
/// after return. Controlled tests cover borrowing and release classification; Windows native and
/// hardware validation must exercise the actual imports and descriptor acquisition separately.
/// </remarks>
internal sealed class WindowsHidFeatureReportApi : IWindowsHidFeatureReportApi
{
    private const string AccessDeniedGuidance =
        "Windows denied access to the HID interface. The interface may be held exclusively by another process, " +
        "or this environment may require running the process elevated as Administrator to open YubiKey HID reports.";
    private readonly WindowsHidSafeHandle _handle;
    private nint _preparsedData;
    private bool _handleReference;
    private bool _openStarted;
    private bool _ready;
    private bool _closeStarted;
    private ushort _featureLength;
    private WindowsHidRelease _release;

    internal WindowsHidFeatureReportApi() : this(new WindowsHidSafeHandle()) { }

    internal WindowsHidFeatureReportApi(WindowsHidSafeHandle handle) => _handle = handle;

    public WindowsHidFeatureReportMetadata Open(string devicePath)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException();
        }

        if (_openStarted || _closeStarted)
        {
            throw new InvalidOperationException("The HID native resources cannot be reopened.");
        }

        _openStarted = true;
        nint value = Kernel.CreateHidFile(devicePath, Kernel.DESIRED_ACCESS.NONE,
            Kernel.FILE_SHARE.ALL, 0, Kernel.CREATION_DISPOSITION.OPEN_EXISTING,
            Kernel.FILE_FLAG.NORMAL, 0);
        int error = Marshal.GetLastPInvokeError();
        _handle.Initialize(value);
        if (_handle.IsInvalid)
        {
            ThrowWin32Failure("CreateFile", error, $"Failed to open HID device '{devicePath}'.");
        }

        _handle.DangerousAddRef(ref _handleReference);
        // Store even a partially returned resource before inspecting success. Failed Open is
        // cleaned up by the same owner's CloseOnce, with no hidden retry or replacement handle.
        byte result = HidNative.GetHidPreparsedData(value, out _preparsedData);
        error = Marshal.GetLastPInvokeError();
        if (result == 0)
        {
            ThrowWin32Failure("HidD_GetPreparsedData", error, "Failed to get HID preparsed data.");
        }

        HidNative.HIDP_CAPS capabilities = new();
        int status = HidNative.HidP_GetCaps(_preparsedData, ref capabilities);
        if (status != HidNative.HidpStatusSuccess)
        {
            throw new PlatformApiException("HidP_GetCaps", status, "Failed to get HID capabilities.");
        }

        _featureLength = unchecked((ushort)capabilities.FeatureReportByteLength);
        _ready = true;
        return new WindowsHidFeatureReportMetadata(unchecked((ushort)capabilities.UsagePage),
            unchecked((ushort)capabilities.Usage), _featureLength);
    }

    public WindowsHidFeatureResult GetFeature(byte[] nativeReport)
    {
        EnsureReport(nativeReport);
        return Transfer(_handle, nativeReport, HidNative.GetHidFeature);
    }

    public WindowsHidFeatureResult SetFeature(byte[] nativeReport)
    {
        EnsureReport(nativeReport);
        return Transfer(_handle, nativeReport, HidNative.SetHidFeature);
    }

    private void EnsureReport(byte[] nativeReport)
    {
        if (!_ready || _closeStarted)
        {
            throw new InvalidOperationException("The HID feature handle is not open.");
        }

        if (nativeReport.Length != _featureLength || nativeReport.Length < 2 || nativeReport[0] != 0)
        {
            throw new ArgumentException("Expected a descriptor-sized feature report with a zero report-ID prefix.", nameof(nativeReport));
        }
    }

    internal static WindowsHidFeatureResult Transfer(WindowsHidSafeHandle handle, byte[] report,
        Func<nint, byte[], uint, byte> call)
    {
        bool reference = false;
        try
        {
            handle.DangerousAddRef(ref reference);
            byte result = call(handle.DangerousGetHandle(), report, (uint)report.Length);
            uint error = result == 0 ? unchecked((uint)Marshal.GetLastPInvokeError()) : 0;
            // Unlike overlapped IO, ERROR_IO_PENDING is a failure here. FALSE/error zero must
            // also remain failure in the frozen error-only result (ERROR_GEN_FAILURE = 31).
            return new WindowsHidFeatureResult(result != 0 ? 0 : error == 0 ? 31u : error);
        }
        finally
        {
            if (reference)
            {
                handle.DangerousRelease();
            }
        }
    }

    public WindowsHidRelease CloseOnce()
    {
        if (_closeStarted)
        {
            return _release;
        }

        // Publish before any fallible release. An exception leaves cached lack of proof, and
        // pending SafeHandle references never become permission to retry or revise this outcome.
        _closeStarted = true;
        _release = ReleaseResources(_handle, ref _handleReference, ref _preparsedData, HidNative.FreeHidPreparsedData);
        return _release;
    }

    internal static WindowsHidRelease ReleaseResources(WindowsHidSafeHandle handle, ref bool reference,
        ref nint preparsedData, Func<nint, byte> free)
    {
        bool metadataReleased = preparsedData == 0;
        uint error = 0;
        bool handleAcquired = !handle.IsInvalid;
        WindowsHidRelease handleRelease;
        try
        {
            if (!metadataReleased)
            {
                byte result = free(preparsedData);
                error = result == 0 ? unchecked((uint)Marshal.GetLastPInvokeError()) : 0;
                metadataReleased = result != 0;
                if (metadataReleased)
                {
                    preparsedData = 0;
                }
            }
        }
        finally
        {
            // Still attempt handle release if metadata release fails or throws. No metadata
            // failure can prove full release, even when the handle itself closes successfully.
            try
            {
                if (reference)
                {
                    reference = false;
                    handle.DangerousRelease();
                }
            }
            finally
            {
                handleRelease = handle.CloseOnce();
            }
        }

        return new WindowsHidRelease(metadataReleased && (!handleAcquired || handleRelease.NativeReleaseProven),
            error != 0 ? error : handleRelease.ErrorCode);
    }

    private static void ThrowWin32Failure(string source, int error, string message)
    {
        if (error == 5)
        {
            throw new UnauthorizedAccessException($"{message} {AccessDeniedGuidance}");
        }

        throw new PlatformApiException(source, error, message);
    }
}