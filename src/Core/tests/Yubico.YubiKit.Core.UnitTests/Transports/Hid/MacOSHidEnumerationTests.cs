// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.Hid.MacOS;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid;

public sealed class MacOSHidEnumerationTests
{
    [Fact]
    public void NullManagerFailsWithoutUsingOrReleasingIt()
    {
        var native = new Native { Manager = 0 };

        var error = Assert.Throws<PlatformApiException>(() => Scan(native));

        Assert.Contains("IOHIDManagerCreate", error.Message);
        Assert.Equal(["create"], native.Calls);
    }

    [Fact]
    public void NullDeviceSetReturnsEmptyAndReleasesOnlyTheManager()
    {
        var native = new Native { DeviceSet = 0 };

        Assert.Empty(Scan(native));

        Assert.Equal(["create", "match 11", "copy 11", "release 11"], native.Calls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ValidSetReturnsTheReadResultAndReleasesBothHandles(bool empty)
    {
        var native = new Native { Interfaces = empty ? [] : [new HidInterface()] };

        Assert.Same(native.Interfaces, Scan(native));
        Assert.Equal(["create", "match 11", "copy 11", "read 22", "release 11", "release 22"], native.Calls);
    }

    [Fact]
    public void ReadFailurePreservesExceptionAndReleasesBothHandles()
    {
        var failure = new InvalidOperationException("Injected descriptor read failure.");
        var native = new Native { ReadFailure = failure };

        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => Scan(native)));
        Assert.Equal(["create", "match 11", "copy 11", "read 22", "release 11", "release 22"], native.Calls);
    }

    [Fact]
    public void CopyFailurePreservesExceptionAndReleasesOnlyTheManager()
    {
        var failure = new InvalidOperationException("Injected device set copy failure.");
        var native = new Native { CopyFailure = failure };

        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => Scan(native)));
        Assert.Equal(["create", "match 11", "copy 11", "release 11"], native.Calls);
    }

    private static IReadOnlyList<IHidInterface> Scan(Native native) => MacOSHidEnumeration.GetList(
        native.Create, native.Match, native.Copy, native.Release, native.Read);

    private sealed class Native
    {
        public nint Manager { get; init; } = 11;
        public nint DeviceSet { get; init; } = 22;
        public IReadOnlyList<IHidInterface> Interfaces { get; init; } = [];
        public Exception? ReadFailure { get; init; }
        public Exception? CopyFailure { get; init; }
        public List<string> Calls { get; } = [];

        public nint Create()
        {
            Calls.Add("create");
            return Manager;
        }

        public void Match(nint manager)
        {
            Calls.Add($"match {manager}");
            if (manager == 0)
                throw new InvalidOperationException("The fake must not receive a null manager.");
        }

        public nint Copy(nint manager)
        {
            Calls.Add($"copy {manager}");
            return CopyFailure is null ? DeviceSet : throw CopyFailure;
        }

        public void Release(nint handle) => Calls.Add($"release {handle}");

        public IReadOnlyList<IHidInterface> Read(nint deviceSet)
        {
            Calls.Add($"read {deviceSet}");
            if (deviceSet == 0)
                throw new InvalidOperationException("The fake reader must not receive a null device set.");
            return ReadFailure is null ? Interfaces : throw ReadFailure;
        }
    }

    private sealed class HidInterface : IHidInterface
    {
        public string ReaderName => "fake";
        public HidDescriptorInfo DescriptorInfo => new();
        public HidInterfaceType InterfaceType => HidInterfaceType.Fido;
        public IHidConnection ConnectToFeatureReports() => throw new NotSupportedException();
        public IHidConnection ConnectToIOReports() => throw new NotSupportedException();
    }
}