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

namespace Yubico.YubiKey.U2f
{
    internal enum U2fHidStatus 
    {
        Success = 0x00,
        Ctap1ErrInvalidCommand = 0x01,
        Ctap1ErrInvalidParameter = 0x02,
        Ctap1ErrInvalidLength = 0x03,
        Ctap1ErrInvalidSequencing = 0x04,
        Ctap1ErrTimeout = 0x05,
        Ctap1ErrChannelBusy = 0x06,
        Ctap1ErrLockRequired = 0x0a,
        Ctap1ErrInvalidChannel = 0x0b,
        Ctap1ErrOther = 0x7f,
    }
}
