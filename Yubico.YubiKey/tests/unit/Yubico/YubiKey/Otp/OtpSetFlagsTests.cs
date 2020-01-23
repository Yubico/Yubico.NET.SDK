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
using Xunit;
using Yubico.YubiKey.Otp.Operations;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Otp
{
    public class OtpSetFlagsTests
    {
        [Fact]
        public void TestSetSerialNumberButtonVisible()
        {
            using TestOp op = new TestOp()
                .Settings.SetSerialNumberButtonVisible();

            byte expectedExtended = 0x01;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x00;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x00;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetSerialNumberUsbVisible()
        {
            using TestOp op = new TestOp()
                .Settings.SetSerialNumberUsbVisible();

            byte expectedExtended = 0x02;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x00;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x00;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetSerialNumberApiVisible()
        {
            using TestOp op = new TestOp()
                .Settings.SetSerialNumberApiVisible();

            byte expectedExtended = 0x04;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x00;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x00;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetUseNumericKeypad()
        {
            using TestOp op = new TestOp()
                .Settings.UseNumericKeypad();

            byte expectedExtended = 0x08;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x00;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x00;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetFastTrigger()
        {
            using TestOp op = new TestOp()
                .Settings.UseFastTrigger();

            byte expectedExtended = 0x10;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x00;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x00;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetAllowUpdate()
        {
            using TestOp op = new TestOp()
                .Settings.AllowUpdate();

            byte expectedExtended = 0x20;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x00;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x00;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetDormant()
        {
            using TestOp op = new TestOp()
                .Settings.SetDormant();

            byte expectedExtended = 0x40;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x00;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x00;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetInvertLed()
        {
            using TestOp op = new TestOp()
                .Settings.SetInvertLed();

            byte expectedExtended = 0x80;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x00;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x00;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetOathHotP()
        {
            using TestOp op = new TestOp()
                .Settings.SetOathHotp();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x40;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x00;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetSendTabFirst()
        {
            using TestOp op = new TestOp()
                .Settings.SendTabFirst();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x01;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x00;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetAppendTabToFixed()
        {
            using TestOp op = new TestOp()
                .Settings.AppendTabToFixed();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x02;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x00;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetAppendTabToOtp()
        {
            using TestOp op = new TestOp()
                .Settings.SetAppendTabToOtp();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x04;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x00;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetAppendDelayToFixed()
        {
            using TestOp op = new TestOp()
                .Settings.AppendDelayToFixed();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x08;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x00;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetAppendDelayToOtp()
        {
            using TestOp op = new TestOp()
                .Settings.AppendDelayToOtp();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x10;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x00;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetAppendCarriageReturn()
        {
            using TestOp op = new TestOp()
                .Settings.AppendCarriageReturn();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x20;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x00;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetProtectLongPressSlot()
        {
            using TestOp op = new TestOp()
                .Settings.ProtectLongPressSlot();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x80;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x00;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetYubicoOtpChallengeResponse()
        {
            using TestOp op = new TestOp()
                .Settings.UseYubicoOtpChallengeResponseMode();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x40;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x20;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetHmacSha1ChallengeResponse()
        {
            using TestOp op = new TestOp()
                .Settings.UseHmacSha1ChallengeResponseMode();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x40;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x22;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetSendReferenceString()
        {
            using TestOp op = new TestOp()
                .Settings.SendReferenceString();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x00;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x01;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        [Obsolete("Reserved for YubiKey 1")]
        public void TestSetTicketFirst()
        {
            using TestOp op = new TestOp()
                .Settings.TicketFirst();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x00;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x02;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetShortTicketWithStaticTicket()
        {
            using TestOp op = new TestOp()
                .Settings.ShortTicket()
                .Settings.UseStaticTicketMode();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x00;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x22;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetShortTicketWithoutStaticTicket()
        {
            using TestOp op = new TestOp()
                .Settings.ShortTicket();

            Exception ex = Assert.Throws<InvalidOperationException>(
                () => op.Settings.YubiKeyFlags);
        }

        [Fact]
        public void TestSetStaticPasswordMode()
        {
            using TestOp op = new TestOp()
                .Settings.UseStaticPasswordMode();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x00;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x02;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetUse8DigitHotpWithOathHotP()
        {
            using TestOp op = new TestOp()
                .Settings.Use8DigitHotp()
                .Settings.SetOathHotp();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x40;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x02;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetUse8DigitHotpWithoutOathHotP()
        {
            using TestOp op = new TestOp()
                .Settings.Use8DigitHotp();

            Exception ex = Assert.Throws<InvalidOperationException>(
                () => op.Settings.YubiKeyFlags);
        }

        [Fact]
        public void TestSetUse10msPacing()
        {
            using TestOp op = new TestOp()
                .Settings.Use10msPacing();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x00;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x04;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetHmacLessThan64BytesWithHmacSha1ChallengeResponse()
        {
            using TestOp op = new TestOp()
                .Settings.HmacLessThan64Bytes()
                .Settings.UseHmacSha1ChallengeResponseMode();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x40;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x26;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetHmacLessThan64BytesWithoutYubicoOtpChallengeResponseOrHmacSha1ChallengeResponse()
        {
            using TestOp op = new TestOp()
                .Settings.HmacLessThan64Bytes();

            Exception ex = Assert.Throws<InvalidOperationException>(
                () => op.Settings.YubiKeyFlags);
        }

        [Fact]
        public void TestSetUse20msPacing()
        {
            using TestOp op = new TestOp()
                .Settings.Use20msPacing();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x00;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x08;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetUseButtonTriggerWithYubicoOtpChallengeResponse()
        {
            using TestOp op = new TestOp()
                .Settings.UseButtonTrigger()
                .Settings.UseYubicoOtpChallengeResponseMode();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x40;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x28;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetUseButtonTriggerWithHmacSha1ChallengeResponse()
        {
            using TestOp op = new TestOp()
                .Settings.UseButtonTrigger()
                .Settings.UseHmacSha1ChallengeResponseMode();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x40;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x2a;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetUseButtonTriggerWithoutYubicoOtpChallengeResponseOrHmacSha1ChallengeResponse()
        {
            using TestOp op = new TestOp()
                .Settings.UseButtonTrigger();

            Exception ex = Assert.Throws<InvalidOperationException>(
                () => op.Settings.YubiKeyFlags);
        }

        [Fact]
        [Obsolete("Reserved for YubiKey 1")]
        public void TestSetAllowHidTrigger()
        {
            using TestOp op = new TestOp()
                .Settings.AllowHidTrigger();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x00;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x10;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetUseMixedCasePassword()
        {
            using TestOp op = new TestOp()
                .Settings.UseStaticTicketMode()
                .Settings.UseMixedCasePassword();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x00;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x30;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetOathFixedModhex1WithOathHotP()
        {
            using TestOp op = new TestOp()
                .Settings.OathFixedModhex1()
                .Settings.SetOathHotp();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x40;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x10;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetOathFixedModhex1WithoutOathHotP()
        {
            using TestOp op = new TestOp()
                .Settings.OathFixedModhex1();

            Exception ex = Assert.Throws<InvalidOperationException>(
                () => op.Settings.YubiKeyFlags);
        }

        [Fact]
        public void TestSetStaticTicket()
        {
            using TestOp op = new TestOp()
                .Settings.UseStaticTicketMode();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x00;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x20;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetUseAlphaNumericPassword()
        {
            using TestOp op = new TestOp()
                .Settings.UseStaticTicketMode()
                .Settings.UseAlphaNumericPassword();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x00;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x60;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetOathFixedModhex2WithOathHotP()
        {
            using TestOp op = new TestOp()
                .Settings.UseOathFixedModhex2()
                .Settings.SetOathHotp();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x40;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x40;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetOathFixedModhex2WithoutOathHotP()
        {
            using TestOp op = new TestOp()
                .Settings.UseOathFixedModhex2();

            Exception ex = Assert.Throws<InvalidOperationException>(
                () => op.Settings.YubiKeyFlags);
        }

        [Fact]
        public void TestSetOathFixedModhexWithOathHotP()
        {
            using TestOp op = new TestOp()
                .Settings.UseOathFixedModhex()
                .Settings.SetOathHotp();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x40;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0x50;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }

        [Fact]
        public void TestSetOathFixedModhexWithoutOathHotP()
        {
            using TestOp op = new TestOp()
                .Settings.UseOathFixedModhex();

            Exception ex = Assert.Throws<InvalidOperationException>(
                () => op.Settings.YubiKeyFlags);
        }

        [Fact]
        public void TestSetAllowManualUpdate()
        {
            using TestOp op = new TestOp()
                .Settings.UseStaticTicketMode()
                .Settings.AllowManualUpdate();

            byte expectedExtended = 0x00;
            Assert.Equal(expectedExtended, op.Settings.YubiKeyFlags.Extended);

            byte expectedTicket = 0x00;
            Assert.Equal(expectedTicket, op.Settings.YubiKeyFlags.Ticket);

            byte expectedConfig = 0xA0;
            Assert.Equal(expectedConfig, op.Settings.YubiKeyFlags.Configuration);
        }
    }

    internal class TestOp : OperationBase<TestOp>, IDisposable
    {
        public TestOp() : base(_yubiKey.Connect(YubiKeyApplication.Otp), new OtpSession(_yubiKey), Slot.ShortPress)
        {
            // I'm making the serial number API visible flag default, but I'll
            // unset it here so that tests don't need to care.
            _ = Settings.SetSerialNumberApiVisible(false);
            // Ditto for allow updates.
            _ = Settings.AllowUpdate(false);
        }

        protected override void ExecuteOperation()
        {
            throw new NotImplementedException();
        }

        protected override void PreLaunchOperation()
        {
            throw new NotImplementedException();
        }

        public void Dispose() => Session.Dispose();

        private static readonly IYubiKeyDevice _yubiKey = new HollowYubiKeyDevice
        {
            FirmwareVersion = FirmwareVersion.V5_4_2
        };
    }
}
