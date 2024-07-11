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

namespace Yubico.YubiKey.Otp
{
    public class TicketFlagsTests
    {
        [Fact]
        public void ValidateFlagsForUpdate_NoneSpecified_NoException()
        {
            TicketFlags flag = TicketFlags.None;

            flag.ValidateFlagsForUpdate();
        }

        [Theory]
        [InlineData(TicketFlags.AppendCarriageReturn)]
        [InlineData(TicketFlags.AppendDelayToFixed)]
        [InlineData(TicketFlags.AppendDelayToOtp)]
        [InlineData(TicketFlags.AppendTabToFixed)]
        [InlineData(TicketFlags.AppendTabToOtp)]
        [InlineData(TicketFlags.TabFirst)]
        public void ValidateFlagsForUpdate_FlagInRange_NoException(byte flagByte)
        {
            TicketFlags flag = flagByte;
            flag.ValidateFlagsForUpdate();
        }
    }
}
