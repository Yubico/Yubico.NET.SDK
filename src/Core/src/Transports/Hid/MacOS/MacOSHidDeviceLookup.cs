// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Yubico.YubiKit.Core.Native;

namespace Yubico.YubiKit.Core.Transports.Hid.MacOS;

/// <summary>Classifies the result of looking up an existing registry entry and owns its temporary handles.</summary>
internal static class MacOSHidDeviceLookup
{
    internal interface INative
    {
        nint CreateMatchingDictionary(ulong entryId);

        // Consumes the dictionary once native code is entered, even on a failed status.
        int GetMatchingServices(nint dictionary, out uint iterator);

        uint NextService(uint iterator);
        bool IsIteratorValid(uint iterator);
        nint CreateHidDevice(uint service);
        void ReleaseIoObject(uint handle);
        void ReleaseCFObject(nint handle);
    }

    internal static nint CreateDevice(INative native, ulong entryId)
    {
        nint dictionary = native.CreateMatchingDictionary(entryId);
        if (dictionary == 0)
            throw new PlatformApiException("IORegistryEntryIDMatching failed to create a matching dictionary.");

        int status;
        uint iterator;
        try
        {
            status = native.GetMatchingServices(dictionary, out iterator);
        }
        catch (Exception ex) when (ex is TypeLoadException or BadImageFormatException)
        {
            // Binding failed before IOKit could consume its argument.
            native.ReleaseCFObject(dictionary);
            throw;
        }

        if (status != 0)
            throw new PlatformApiException("IOServiceGetMatchingServices", status, "HID registry lookup failed.");
        // A null iterator can mean no matches, but the kernel can also return it when it cannot
        // allocate or wrap the iterator. Require a valid iterator before declaring the entry absent.
        if (iterator == 0)
            throw new PlatformApiException("IOServiceGetMatchingServices returned no iterator. Reselect the device.");

        try
        {
            return CreateDeviceFromIterator(native, iterator);
        }
        finally
        {
            native.ReleaseIoObject(iterator);
        }
    }

    private static nint CreateDeviceFromIterator(INative native, uint iterator)
    {
        uint service = native.NextService(iterator);
        if (service == 0)
        {
            // Apple requires checking validity when IOIteratorNext returns zero.
            if (!native.IsIteratorValid(iterator))
                throw new PlatformApiException("IOIteratorIsValid could not validate the HID registry lookup iterator.");
            throw new MacOSHidEntryUnavailableException();
        }

        try
        {
            nint device = native.CreateHidDevice(service);
            return device != 0 ? device : throw new PlatformApiException("Failed to create HID device handle.");
        }
        finally
        {
            native.ReleaseIoObject(service);
        }
    }
}
