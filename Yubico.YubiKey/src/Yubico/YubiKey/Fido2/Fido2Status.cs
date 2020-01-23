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

namespace Yubico.YubiKey.Fido2
{
    internal enum Fido2Status 
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
        Ctap2ErrCborUnexpectedType = 0x11,
        Ctap2ErrCbor = 0x12,
        Ctap2ErrMissingParameter = 0x14,
        Ctap2ErrLimitExceeded = 0x15,
        Ctap2ErrUnsupportedExtension = 0x16,
        Ctap2ErrCredentialExcluded = 0x19,
        Ctap2ErrProcessing = 0x21,
        Ctap2ErrInvalidCredential = 0x22,
        Ctap2ErrUserActionPending = 0x23,
        Ctap2ErrOperationPending = 0x24, 
        Ctap2ErrNoOperationsPending = 0x25,
        Ctap2ErrUnsupportedAlgorithm = 0x26,
        Ctap2ErrOperationDenied = 0x27,
        Ctap2ErrKeyStoreFull = 0x28,
        Ctap2ErrInvalidOption = 0x2c,
        Ctap2ErrKeepAliveCancel = 0x2d,
        Ctap2ErrNoCredentialsProvided = 0x2e,
        Ctap2ErrUserActionTimeout = 0x2f,
        Ctap2ErrNotAllowed = 0x30,
        Ctap2ErrPinInvalid = 0x31,
        Ctap2ErrPinBlocked = 0x32,
        Ctap2ErrPinAuthInvalid = 0x33,
        Ctap2ErrPinAthBlocked = 0x34,
        Ctap2ErrPinNotSet = 0x35,
        Ctap2ErrPinRequired = 0x36,
        Ctap2ErrPinPolicyViolation = 0x37,
        Ctap2ErrPinTokenExpired = 0x38,
        Ctap2ErrRequestTooLarge = 0x39,
        Ctap2ErrActionTimeout = 0x3a,
        Ctap2ErrUserPresenceRequired = 0x3b,
        Ctap2ErrOther = 0x7f,
        Ctap2ErrSpecLast = 0xdf
    }
}
