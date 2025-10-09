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

namespace Yubico.YubiKit.Core.SmartCard;

public static class SWConstants
{
    // Success
    public const short Success = unchecked((short)0x9000);

    // Warning
    public const short WarningNvmUnchanged = 0x6200;
    public const short PartialCorruption = 0x6281;
    public const short EOFReached = 0x6282;
    public const short FileDeactivated = 0x6283;
    public const short InvalidFileFormat = 0x6284;
    public const short FileTerminated = 0x6285;
    public const short NoSensorData = 0x6286;

    public const short WarningNvmChanged = 0x6300;
    public const short NoMoreSpaceInFile = 0x6381;
    public const short VerifyFail = 0x63C0;

    // Error
    public const short ExecutionError = 0x6400;
    public const short ResponseRequired = 0x6401;

    public const short ErrorNvmChanged = 0x6500;
    public const short MemoryFailure = 0x6581;

    public const short WrongLength = 0x6700;

    public const short FunctionError = 0x6800;
    public const short LogicalChannelNotSupported = 0x6881;
    public const short SecureMessagingNotSupported = 0x6882;
    public const short LastCommandOfChainExpected = 0x6883;
    public const short CommandChainingNotSupported = 0x6884;

    public const short CommandNotAllowed = 0x6900;
    public const short IncompatibleCommand = 0x6981;
    public const short SecurityStatusNotSatisfied = 0x6982;
    public const short AuthenticationMethodBlocked = 0x6983;
    public const short ReferenceDataUnusable = 0x6984;
    public const short ConditionsNotSatisfied = 0x6985;
    public const short CommandNotAllowedNoEF = 0x6986;
    public const short SecureMessageDataMissing = 0x6987;
    public const short SecureMessageMalformed = 0x6988;

    public const short InvalidParameter = 0x6A00;
    public const short InvalidCommandDataParameter = 0x6A80;
    public const short FunctionNotSupported = 0x6A81;
    public const short FileOrApplicationNotFound = 0x6A82;
    public const short RecordNotFound = 0x6A83;
    public const short NotEnoughSpace = 0x6A84;
    public const short InconsistentLengthWithTlv = 0x6A85;
    public const short IncorrectP1orP2 = 0x6A86;
    public const short InconsistentLengthWithP1P2 = 0x6A87;
    public const short DataNotFound = 0x6A88;
    public const short FileAlreadyExists = 0x6A89;
    public const short DFNameAlreadyExists = 0x6A8A;

    public const short InsNotSupported = 0x6D00;

    public const short ClaNotSupported = 0x6E00;

    public const short NoPreciseDiagnosis = 0x6F00;
}