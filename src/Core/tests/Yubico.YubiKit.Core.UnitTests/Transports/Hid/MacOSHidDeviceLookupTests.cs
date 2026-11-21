// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Transports.Hid.MacOS;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid;

public sealed class MacOSHidDeviceLookupTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(22)]
    public void SuccessfulEmptyQuery_IsEligibleForRediscovery(int iterator)
    {
        var native = new Native { Iterator = iterator, Service = 0 };

        Assert.Throws<MacOSHidEntryUnavailableException>(() => MacOSHidDeviceLookup.CreateDevice(native, 42));

        Assert.Equal(iterator == 0 ? ["dictionary", "query"] : ["dictionary", "query", "next", "valid", "release 22"], native.Calls);
    }

    [Fact]
    public void QueryErrorPreservesStatusWithoutAttemptingIterationOrReleasingConsumedDictionary()
    {
        var native = new Native { Status = unchecked((int)0xE00002C1), Iterator = 0 };

        var error = Assert.Throws<PlatformApiException>(() => MacOSHidDeviceLookup.CreateDevice(native, 42));

        Assert.Contains("IOServiceGetMatchingServices = 0xE00002C1", error.Message);
        Assert.Equal(["dictionary", "query"], native.Calls);
    }

    [Fact]
    public void DictionaryCreationFailureIsNotAnEmptyLookup()
    {
        var native = new Native { Dictionary = 0 };

        var error = Assert.Throws<PlatformApiException>(() => MacOSHidDeviceLookup.CreateDevice(native, 42));

        Assert.Contains("IORegistryEntryIDMatching", error.Message);
        Assert.Equal(["dictionary"], native.Calls);
    }

    [Fact]
    public void InvalidIteratorIsNotAnEmptyLookupAndIsReleased()
    {
        var native = new Native { Service = 0, IteratorValid = false };

        var error = Assert.Throws<PlatformApiException>(() => MacOSHidDeviceLookup.CreateDevice(native, 42));

        Assert.Contains("IOIteratorIsValid", error.Message);
        Assert.Equal(["dictionary", "query", "next", "valid", "release 22"], native.Calls);
    }

    [Fact]
    public void QueryBindingFailurePreservesCauseAndReleasesUnconsumedDictionary()
    {
        var failure = new EntryPointNotFoundException();
        var native = new Native { BindingFailure = failure };

        Assert.Same(failure, Assert.Throws<EntryPointNotFoundException>(() => MacOSHidDeviceLookup.CreateDevice(native, 42)));
        Assert.Equal(["dictionary", "query", "cf release 11"], native.Calls);
    }

    [Theory]
    [InlineData(44)]
    [InlineData(0)]
    public void FoundServiceAndIteratorAreReleasedEvenIfHidDeviceCreationFails(int device)
    {
        var native = new Native { Device = device };

        if (device == 0)
            Assert.Throws<PlatformApiException>(() => MacOSHidDeviceLookup.CreateDevice(native, 42));
        else
            Assert.Equal((nint)device, MacOSHidDeviceLookup.CreateDevice(native, 42));

        Assert.Equal(["dictionary", "query", "next", "create", "release 33", "release 22"], native.Calls);
    }

    private sealed class Native : MacOSHidDeviceLookup.INative
    {
        public List<string> Calls { get; } = [];
        public nint Dictionary { get; init; } = 11;
        public int Status { get; init; }
        public int Iterator { get; init; } = 22;
        public int Service { get; init; } = 33;
        public bool IteratorValid { get; init; } = true;
        public nint Device { get; init; } = 44;
        public EntryPointNotFoundException? BindingFailure { get; init; }

        public nint CreateMatchingDictionary(ulong entryId)
        {
            Calls.Add("dictionary");
            Assert.Equal(42UL, entryId);
            return Dictionary;
        }

        public int GetMatchingServices(nint dictionary, out int iterator)
        {
            Calls.Add("query");
            Assert.Equal((nint)11, dictionary);
            if (BindingFailure is not null)
                throw BindingFailure;
            iterator = Iterator;
            return Status;
        }

        public int NextService(int iterator)
        {
            Calls.Add("next");
            Assert.Equal(22, iterator);
            return Service;
        }

        public bool IsIteratorValid(int iterator)
        {
            Calls.Add("valid");
            Assert.Equal(22, iterator);
            return IteratorValid;
        }

        public nint CreateHidDevice(int service)
        {
            Calls.Add("create");
            Assert.Equal(33, service);
            return Device;
        }

        public void ReleaseIoObject(int handle) => Calls.Add($"release {handle}");
        public void ReleaseCFObject(nint handle) => Calls.Add($"cf release {handle}");
    }
}
