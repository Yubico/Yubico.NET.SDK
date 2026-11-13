// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid.Windows;

public class WindowsOtpDirectFacadeTests : WindowsRoutingDirectContract
{
    protected override bool Otp => true;
}