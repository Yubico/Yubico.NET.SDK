// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

namespace Yubico.YubiKit.Core.UnitTests.Devices;

[Collection(DiscoveryWorkerAdmissionCollection.Name)]
public class WindowsFidoHidRoutingTests : WindowsRoutingContract
{
    protected override bool Otp => false;
}