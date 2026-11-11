// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using System.Reflection;
using System.Runtime.InteropServices;
using Yubico.YubiKit.Core.Native.Windows.Kernel32;
using Yubico.YubiKit.Core.Transports.Hid.Windows;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid.Windows;

public class WindowsHidFeatureReportApiTests
{
    [Theory]
    [InlineData("GetHidFeature", "HidD_GetFeature")]
    [InlineData("SetHidFeature", "HidD_SetFeature")]
    public void FeatureImports_PreserveBooleanAndUnsignedLengthAbi(string name, string entryPoint)
    {
        MethodInfo method = typeof(Yubico.YubiKit.Core.Native.Windows.HidD.NativeMethods)
            .GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic) ?? throw new InvalidOperationException(name);
        var import = Assert.IsType<LibraryImportAttribute>(method.GetCustomAttribute<LibraryImportAttribute>());
        Assert.Equal(entryPoint, import.EntryPoint);
        Assert.True(import.SetLastError);
        Assert.Equal(typeof(byte), method.ReturnType);
        Assert.Equal(new[] { typeof(nint), typeof(byte[]), typeof(uint) }, method.GetParameters().Select(p => p.ParameterType));
        Assert.Equal(DllImportSearchPath.SafeDirectories,
            method.GetCustomAttribute<DefaultDllImportSearchPathsAttribute>()?.Paths);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CloseOnce_NoAcquiredHandleProvesRelease(int value)
    {
        using var handle = new WindowsHidSafeHandle(_ => throw new InvalidOperationException("No native handle acquired"));
        handle.Initialize(value);
        var api = new WindowsHidFeatureReportApi(handle);
        Assert.True(api.CloseOnce().NativeReleaseProven);
        Assert.True(api.CloseOnce().NativeReleaseProven);
    }

    [Fact]
    public void ReleaseResources_MetadataFailureWithZeroErrorRemainsUnproven()
    {
        using var handle = new WindowsHidSafeHandle(_ => new(true, 0));
        handle.Initialize(123);
        bool reference = false;
        nint preparsed = 456;
        WindowsHidRelease result = WindowsHidFeatureReportApi.ReleaseResources(handle, ref reference, ref preparsed, _ =>
        {
            Marshal.SetLastPInvokeError(0);
            return 0;
        });
        Assert.False(result.NativeReleaseProven);
        Assert.Equal(0u, result.ErrorCode);
        Assert.Equal((nint)456, preparsed);
        Assert.True(handle.CloseOnce().NativeReleaseProven);
    }

    [Fact]
    public void ReleaseResources_MetadataExceptionStillClosesAcquiredHandle()
    {
        int closes = 0;
        using var handle = new WindowsHidSafeHandle(_ => { closes++; return new(true, 0); });
        handle.Initialize(123);
        bool reference = false;
        handle.DangerousAddRef(ref reference);
        nint preparsed = 456;
        Assert.Throws<InvalidOperationException>(() => WindowsHidFeatureReportApi.ReleaseResources(handle,
            ref reference, ref preparsed, _ => throw new InvalidOperationException("free failed")));
        Assert.Equal(1, closes);
        Assert.False(reference);
        Assert.Equal((nint)456, preparsed);
    }

    [Theory]
    [InlineData(1, 5, 0u)]
    [InlineData(255, 995, 0u)]
    [InlineData(0, 5, 5u)]
    [InlineData(0, 995, 995u)]
    [InlineData(0, 997, 997u)]
    [InlineData(0, 0, 31u)]
    public void Transfer_BorrowsExactBufferAndClassifiesSynchronousResult(byte nativeResult, int error, uint expected)
    {
        int closes = 0;
        using var handle = new WindowsHidSafeHandle(_ => { closes++; return new(true, 0); });
        handle.Initialize(123);
        byte[] report = [0, 1, 2, 3, 4, 5, 6, 7, 8];
        WindowsHidFeatureResult result = WindowsHidFeatureReportApi.Transfer(handle, report, (value, buffer, length) =>
        {
            Assert.Equal((nint)123, value);
            Assert.Same(report, buffer);
            Assert.Equal(9u, length);
            Assert.False(handle.CloseOnce().NativeReleaseProven);
            Assert.Equal(0, closes);
            buffer[8] = 42;
            Marshal.SetLastPInvokeError(error);
            return nativeResult;
        });
        Assert.Equal(expected, result.ErrorCode);
        Assert.Equal(42, report[8]);
        Assert.Equal(1, closes);
    }

    [Fact]
    public void Transfer_ExceptionReleasesBorrowedReference()
    {
        int closes = 0;
        using var handle = new WindowsHidSafeHandle(_ => { closes++; return new(true, 0); });
        handle.Initialize(123);
        Assert.Throws<InvalidOperationException>(() => WindowsHidFeatureReportApi.Transfer(handle, [0, 1], (_, _, _) =>
        {
            Assert.False(handle.CloseOnce().NativeReleaseProven);
            throw new InvalidOperationException("native boundary failure");
        }));
        Assert.Equal(1, closes);
    }

    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public void ReleaseResources_RequiresBothMetadataAndHandleProof(bool freeSuccess, bool closeSuccess, bool expected)
    {
        int closes = 0;
        using var handle = new WindowsHidSafeHandle(_ => { closes++; return new(closeSuccess, closeSuccess ? 0u : 6u); });
        handle.Initialize(123);
        bool reference = false;
        handle.DangerousAddRef(ref reference);
        nint preparsed = 456;
        WindowsHidRelease result = WindowsHidFeatureReportApi.ReleaseResources(handle, ref reference, ref preparsed, value =>
        {
            Assert.Equal((nint)456, value);
            Assert.Equal(0, closes);
            Marshal.SetLastPInvokeError(5);
            return freeSuccess ? (byte)1 : (byte)0;
        });
        Assert.Equal(expected, result.NativeReleaseProven);
        Assert.Equal(freeSuccess ? closeSuccess ? 0u : 6u : 5u, result.ErrorCode);
        Assert.Equal(freeSuccess ? 0 : (nint)456, preparsed);
        Assert.False(reference);
        Assert.Equal(1, closes);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CloseOnce_CachesEvidenceWithoutRetry(bool success)
    {
        int closes = 0;
        using var handle = new WindowsHidSafeHandle(_ => { closes++; return new(success, success ? 0u : 6u); });
        handle.Initialize(123);
        var api = new WindowsHidFeatureReportApi(handle);
        WindowsHidRelease first = api.CloseOnce();
        Assert.Equal(success, first.NativeReleaseProven);
        Assert.Equal(first, api.CloseOnce());
        Assert.Equal(1, closes);
    }

    [Fact]
    public void CloseOnce_PendingReferenceNeverBecomesCachedReleaseProof()
    {
        using var handle = new WindowsHidSafeHandle(_ => new(true, 0));
        handle.Initialize(123);
        bool reference = false;
        handle.DangerousAddRef(ref reference);
        var api = new WindowsHidFeatureReportApi(handle);
        try
        {
            Assert.False(api.CloseOnce().NativeReleaseProven);
        }
        finally
        {
            handle.DangerousRelease();
        }
        Assert.True(handle.CloseOnce().NativeReleaseProven);
        Assert.False(api.CloseOnce().NativeReleaseProven);
    }
}