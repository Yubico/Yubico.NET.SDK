// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Yubico.YubiKit.Core.Native;

namespace Yubico.YubiKit.Core.Transports.Hid.MacOS;

/// <summary>Owns the temporary handles used by a single macOS HID enumeration.</summary>
internal static class MacOSHidEnumeration
{
    internal static IReadOnlyList<IHidInterface> GetList(
        Func<nint> createManager,
        Action<nint> setMatching,
        Func<nint, nint> copyDevices,
        Action<nint> release,
        Func<nint, IReadOnlyList<IHidInterface>> readDeviceSet)
    {
        nint manager = 0;
        nint deviceSet = 0;
        try
        {
            manager = createManager();
            if (manager == 0)
                throw new PlatformApiException("IOHIDManagerCreate returned no manager.");

            setMatching(manager);
            deviceSet = copyDevices(manager);
            if (deviceSet == 0)
                return [];

            return readDeviceSet(deviceSet);
        }
        finally
        {
            if (manager != 0)
                release(manager);
            if (deviceSet != 0)
                release(deviceSet);
        }
    }
}