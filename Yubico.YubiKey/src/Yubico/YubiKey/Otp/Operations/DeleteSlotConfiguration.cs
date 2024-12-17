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
using System.Globalization;
using Yubico.YubiKey.Otp.Commands;

namespace Yubico.YubiKey.Otp.Operations
{
    public class DeleteSlotConfiguration : OperationBase<DeleteSlotConfiguration>
    {
        internal DeleteSlotConfiguration(IYubiKeyConnection connection, IOtpSession session, Slot slot)
            : base(connection, session, slot) { }

        protected override void ExecuteOperation()
        {
            var cmd = new DeleteSlotCommand
            {
                OtpSlot = OtpSlot!.Value
            };
            cmd.ApplyCurrentAccessCode(CurrentAccessCode);

            var response = Connection.SendCommand(cmd);
            if (response.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.YubiKeyOperationFailed,
                    response.StatusMessage));
            }
        }

        protected override void PreLaunchOperation()
        {
            if (!(OtpSlot ==
                Slot.ShortPress
                ? Session.IsShortPressConfigured
                : Session.IsLongPressConfigured))
            {
                throw new InvalidOperationException(ExceptionMessages.CantDeleteEmptySlot);
            }
        }
    }
}
