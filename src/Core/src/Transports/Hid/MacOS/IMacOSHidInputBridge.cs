// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

namespace Yubico.YubiKit.Core.Transports.Hid.MacOS;

internal interface IMacOSHidInputBridge
{
    nint CreateDevice(long entryId);
    void OpenDevice(nint device);
    int OpenDeviceResult(nint device)
    {
        OpenDevice(device);
        return 0;
    }
    int InputSize(nint device);
    int OutputSize(nint device) => InputSize(device);
    bool CloseUnstarted(nint device);
    void ReleaseDevice(nint device);
    nint CreateInput(nint device, int size, Action<ReadOnlyMemory<byte>> report, Action<int> terminal);
    int Start(nint owner);
    void Cancel(nint owner);
    int WaitShutdown(nint owner);
    int Destroy(nint owner);
    void SetReport(nint device, byte[] report);
}