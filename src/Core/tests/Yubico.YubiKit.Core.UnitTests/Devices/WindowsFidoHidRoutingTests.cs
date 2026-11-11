// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.Hid.Windows;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

[Collection(DiscoveryWorkerAdmissionCollection.Name)]
public class WindowsFidoHidRoutingTests : WindowsRoutingContract
{
    protected override bool Otp => false;
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WindowsInterface_TransfersClaimBeforeNativeOpen(bool otp)
    {
        var descriptor = new HidDescriptorInfo
        {
            DevicePath = $"windows-routing-{Guid.NewGuid():N}",
            VendorId = 0x1050,
            ProductId = 0x407,
            UsagePage = otp ? (ushort)1 : (ushort)0xf1d0,
            Usage = otp ? (ushort)6 : (ushort)1
        };
        var hid = new WindowsHidInterface(descriptor);
        Assert.True(new HidConnectionSlot(hid).OwnsNativeLifetime);
    }
}