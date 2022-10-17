// Copyright 2022 Yubico AB
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
    public enum CtapStatus
    {
        // CTAP1 / Shared codes
        Ok = 0x00,
        InvalidCommand = 0x01,
        InvalidParameter = 0x02,
        InvalidLength = 0x03,
        InvalidSeq = 0x04,
        MessageTimeout = 0x05,
        ChannelBusy = 0x06,
        LockRequired = 0x0A,
        InvalidChannel = 0x0B,
        ErrOther = 0x7f,

        // CTAP2 specific codes
        UnexpectedType = 0x11,
        InvalidCbor = 0x12,
        MissingParameter = 0x14,
        LimitExceeded = 0x15,
        UnsupportedExtension = 0x16,
        CredentialExcluded = 0x19,
        Processing = 0x21,
        UserActionPending = 0x23,
        OperationPending = 0x24,
        NoOperations = 0x25,
        UnsupportedAlgorithm = 0x26,
        OperationDenied = 0x27,
        KeyStoreFull = 0x28,
        NotBusy = 0x29,
        NoOperationPending = 0x2A,
        UnsupportedOption = 0x2B,
        InvalidOption = 0x2C,
        KeepAliveCancel = 0x2D,
        NoCredentials = 0x2E,
        UserActionTimeout = 0x2F,
        NotAllowed = 0x30,
        PinInvalid = 0x31,
        PinBlocked = 0x32,
        PinAuthInvalid = 0x33,
        PowerCycleRequired = 0x34,
        PinNotSet = 0x35,
        PinRequired = 0x36,
        PinPolicyViolation = 0x37,
        TokenExpired = 0x38,
        RequestTooLarge = 0x39,
        ActionTimeout = 0x3A,
        TupRequired = 0x3B,
        UvBlocked = 0x3C,
        IntegrityFailure = 0x3D,
        InvalidSubcommand = 0x3E,
        UvInvalid = 0x3F,
        UnauthorizedPermission = 0x40,
        SpecLast = 0xDF, // Not a real error - Last in error range defined by the CTAP2 spec
        ExtensionFirst = 0xE0, // Extension specific error range begin
        ExtensionLast = 0xEF, // Extension specific error range end
        VendorFirst = 0xF0, // Vendor specific error range begin
        VendorLast = 0xFF, // Vendor specific error range last
    }
}
