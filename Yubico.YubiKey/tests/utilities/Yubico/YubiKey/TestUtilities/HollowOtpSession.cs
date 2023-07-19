// Copyright 2021 Yubico AB
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

namespace Yubico.YubiKey.TestUtilities
{
    public sealed class HollowOtpSession : IOtpSession
    {
        public HollowOtpSession() : this(FirmwareVersion.V5_4_3) { }

        public HollowOtpSession(FirmwareVersion version)
            : this(new HollowYubiKeyDevice { FirmwareVersion = version }) { }

        public HollowOtpSession(IYubiKeyDevice yubiKey)
        {
            YubiKey = yubiKey;
            Connection = yubiKey.Connect(YubiKeyApplication.Otp);
            ReadStatusResponse response = Connection.SendCommand(new ReadStatusCommand());
            Status = response.GetData();
        }

       public OtpStatus Status { get; set; }

        public bool IsShortPressConfigured => Status.ShortPressConfigured;

        public bool ShortPressRequiresTouch => Status.ShortPressRequiresTouch;

        public bool IsLongPressConfigured => Status.LongPressConfigured;

        public bool LongPressRequiresTouch => Status.LongPressRequiresTouch;

        FirmwareVersion IOtpSession.FirmwareVersion => FirmwareVersion;
        FirmwareVersion FirmwareVersion => Status.FirmwareVersion;

        IYubiKeyDevice IOtpSession.YubiKey => YubiKey;
        IYubiKeyDevice YubiKey { get; set; }

        IYubiKeyConnection Connection { get; set; }

        Slot Slot { get; set; }

        public CalculateChallengeResponse CalculateChallengeResponse(Slot slot) =>
            new CalculateChallengeResponse(Connection, this, Slot);

        public ConfigureChallengeResponse ConfigureChallengeResponse(Slot slot) =>
            new ConfigureChallengeResponse(Connection, this, Slot);

        public ConfigureHotp ConfigureHotp(Slot slot) =>
            new ConfigureHotp(Connection, this, Slot);

        public ConfigureNdef ConfigureNdef(Slot slot) =>
            new ConfigureNdef(Connection, this, Slot);

        public ConfigureStaticPassword ConfigureStaticPassword(Slot slot) =>
            new ConfigureStaticPassword(Connection, this, Slot);

        public ConfigureYubicoOtp ConfigureYubicoOtp(Slot slot) =>
            new ConfigureYubicoOtp(Connection, this, Slot);

        public DeleteSlotConfiguration DeleteSlotConfiguration(Slot slot) =>
            new DeleteSlotConfiguration(Connection, this, Slot);

        public UpdateSlot UpdateSlot(Slot slot) =>
            new UpdateSlot(Connection, this, Slot);

        public NdefDataReader ReadNdefTag()
        {
            throw new NotImplementedException();
        }

        public void SwapSlots()
        {
            throw new NotImplementedException();
        }

        public void DeleteSlot(Slot slot)
        {
            throw new NotImplementedException();
        }

        public void Dispose() => Connection.Dispose();
    }
}
