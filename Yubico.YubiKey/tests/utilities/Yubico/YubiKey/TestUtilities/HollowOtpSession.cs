// Copyright 2025 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Yubico.YubiKey.Otp;
using Yubico.YubiKey.Otp.Commands;
using Yubico.YubiKey.Otp.Operations;

namespace Yubico.YubiKey.TestUtilities;

public sealed class HollowOtpSession : IOtpSession
{
    public HollowOtpSession() : this(FirmwareVersion.V5_4_3) { }

    public HollowOtpSession(
        FirmwareVersion version)
        : this(new HollowYubiKeyDevice { FirmwareVersion = version })
    {
    }

    public HollowOtpSession(
        IYubiKeyDevice yubiKey)
    {
        YubiKey = yubiKey;
        Connection = yubiKey.Connect(YubiKeyApplication.Otp);
        var response = Connection.SendCommand(new ReadStatusCommand());
        Status = response.GetData();
    }

    public OtpStatus Status { get; set; }
    private FirmwareVersion FirmwareVersion => Status.FirmwareVersion;
    private IYubiKeyDevice YubiKey { get; }

    private IYubiKeyConnection Connection { get; }

    private Slot Slot { get; set; }

    #region IOtpSession Members

    public bool IsShortPressConfigured => Status.ShortPressConfigured;

    public bool ShortPressRequiresTouch => Status.ShortPressRequiresTouch;

    public bool IsLongPressConfigured => Status.LongPressConfigured;

    public bool LongPressRequiresTouch => Status.LongPressRequiresTouch;

    FirmwareVersion IOtpSession.FirmwareVersion => FirmwareVersion;

    IYubiKeyDevice IOtpSession.YubiKey => YubiKey;

    public CalculateChallengeResponse CalculateChallengeResponse(
        Slot slot)
    {
        return new CalculateChallengeResponse(Connection, this, Slot);
    }

    public ConfigureChallengeResponse ConfigureChallengeResponse(
        Slot slot)
    {
        return new ConfigureChallengeResponse(Connection, this, Slot);
    }

    public ConfigureHotp ConfigureHotp(
        Slot slot)
    {
        return new ConfigureHotp(Connection, this, Slot);
    }

    public ConfigureNdef ConfigureNdef(
        Slot slot)
    {
        return new ConfigureNdef(Connection, this, Slot);
    }

    public ConfigureStaticPassword ConfigureStaticPassword(
        Slot slot)
    {
        return new ConfigureStaticPassword(Connection, this, Slot);
    }

    public ConfigureYubicoOtp ConfigureYubicoOtp(
        Slot slot)
    {
        return new ConfigureYubicoOtp(Connection, this, Slot);
    }

    public DeleteSlotConfiguration DeleteSlotConfiguration(
        Slot slot)
    {
        return new DeleteSlotConfiguration(Connection, this, Slot);
    }

    public UpdateSlot UpdateSlot(
        Slot slot)
    {
        return new UpdateSlot(Connection, this, Slot);
    }

    public NdefDataReader ReadNdefTag()
    {
        throw new NotImplementedException();
    }

    public void SwapSlots()
    {
        throw new NotImplementedException();
    }

    public void DeleteSlot(
        Slot slot)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        Connection.Dispose();
    }

    #endregion
}
