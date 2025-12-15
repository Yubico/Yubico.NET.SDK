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

/// <summary>
///     ISO 7816-4 status word constants and utilities.
/// </summary>
public static class SWConstants
{
    #region Success (0x9000)

    /// <summary>Normal processing - command completed successfully.</summary>
    public const short Success = unchecked((short)0x9000);

    #endregion

    #region Warnings - NVM Unchanged (0x62XX)

    /// <summary>Warning: No information given, NVM unchanged.</summary>
    public const short WarningNvmUnchanged = 0x6200;

    /// <summary>Warning: Part of returned data may be corrupted.</summary>
    public const short PartialCorruption = 0x6281;

    /// <summary>Warning: End of file reached before reading expected length.</summary>
    public const short EOFReached = 0x6282;

    /// <summary>Warning: Selected file is deactivated.</summary>
    public const short FileDeactivated = 0x6283;

    /// <summary>Warning: File control information not formatted according to ISO 7816-4.</summary>
    public const short InvalidFileFormat = 0x6284;

    /// <summary>Warning: Selected file is in termination state.</summary>
    public const short FileTerminated = 0x6285;

    /// <summary>Warning: No input data available from sensor.</summary>
    public const short NoSensorData = 0x6286;

    #endregion

    #region Warnings - NVM Changed (0x63XX)

    /// <summary>Warning: No information given, NVM changed.</summary>
    public const short WarningNvmChanged = 0x6300;

    /// <summary>Warning: File filled up by last write operation.</summary>
    public const short NoMoreSpaceInFile = 0x6381;

    /// <summary>Verification failed (counter bits indicate remaining attempts).</summary>
    public const short VerifyFail = 0x63C0;

    #endregion

    #region Execution Errors - NVM Unchanged (0x64XX)

    /// <summary>Execution error: No information given, NVM unchanged.</summary>
    public const short ExecutionError = 0x6400;

    /// <summary>Execution error: Immediate response required by the card.</summary>
    public const short ResponseRequired = 0x6401;

    #endregion

    #region Execution Errors - NVM Changed (0x65XX)

    /// <summary>Execution error: No information given, NVM changed.</summary>
    public const short ErrorNvmChanged = 0x6500;

    /// <summary>Execution error: Memory failure.</summary>
    public const short MemoryFailure = 0x6581;

    #endregion

    #region Checking Errors - Wrong Length (0x67XX)

    /// <summary>Wrong length field (Lc or Le).</summary>
    public const short WrongLength = 0x6700;

    #endregion

    #region Checking Errors - Functions Not Supported (0x68XX)

    /// <summary>Functions in CLA not supported: No information given.</summary>
    public const short FunctionError = 0x6800;

    /// <summary>Functions in CLA not supported: Logical channel not supported.</summary>
    public const short LogicalChannelNotSupported = 0x6881;

    /// <summary>Functions in CLA not supported: Secure messaging not supported.</summary>
    public const short SecureMessagingNotSupported = 0x6882;

    /// <summary>Functions in CLA not supported: Last command of chain expected.</summary>
    public const short LastCommandOfChainExpected = 0x6883;

    /// <summary>Functions in CLA not supported: Command chaining not supported.</summary>
    public const short CommandChainingNotSupported = 0x6884;

    #endregion

    #region Checking Errors - Command Not Allowed (0x69XX)

    /// <summary>Command not allowed: No information given.</summary>
    public const short CommandNotAllowed = 0x6900;

    /// <summary>Command not allowed: Command incompatible with file structure.</summary>
    public const short IncompatibleCommand = 0x6981;

    /// <summary>Command not allowed: Security status not satisfied (authentication required).</summary>
    public const short SecurityStatusNotSatisfied = 0x6982;

    /// <summary>Command not allowed: Authentication method blocked (too many failed attempts).</summary>
    public const short AuthenticationMethodBlocked = 0x6983;

    /// <summary>Command not allowed: Reference data not usable.</summary>
    public const short ReferenceDataUnusable = 0x6984;

    /// <summary>Command not allowed: Conditions of use not satisfied.</summary>
    public const short ConditionsNotSatisfied = 0x6985;

    /// <summary>Command not allowed: Command not allowed (no current EF).</summary>
    public const short CommandNotAllowedNoEF = 0x6986;

    /// <summary>Command not allowed: Expected secure messaging data objects missing.</summary>
    public const short SecureMessageDataMissing = 0x6987;

    /// <summary>Command not allowed: Incorrect secure messaging data objects.</summary>
    public const short SecureMessageMalformed = 0x6988;

    #endregion

    #region Checking Errors - Wrong Parameters (0x6AXX)

    /// <summary>Wrong parameters: No information given.</summary>
    public const short InvalidParameter = 0x6A00;

    /// <summary>Wrong parameters: Incorrect data in command data field.</summary>
    public const short InvalidCommandDataParameter = 0x6A80;

    /// <summary>Wrong parameters: Function not supported.</summary>
    public const short FunctionNotSupported = 0x6A81;

    /// <summary>Wrong parameters: File or application not found.</summary>
    public const short FileOrApplicationNotFound = 0x6A82;

    /// <summary>Wrong parameters: Record not found.</summary>
    public const short RecordNotFound = 0x6A83;

    /// <summary>Wrong parameters: Not enough memory space in the file.</summary>
    public const short NotEnoughSpace = 0x6A84;

    /// <summary>Wrong parameters: Lc inconsistent with TLV structure.</summary>
    public const short InconsistentLengthWithTlv = 0x6A85;

    /// <summary>Wrong parameters: Incorrect P1 or P2 parameter.</summary>
    public const short IncorrectP1orP2 = 0x6A86;

    /// <summary>Wrong parameters: Lc inconsistent with P1-P2.</summary>
    public const short InconsistentLengthWithP1P2 = 0x6A87;

    /// <summary>Wrong parameters: Referenced data not found.</summary>
    public const short DataNotFound = 0x6A88;

    /// <summary>Wrong parameters: File already exists.</summary>
    public const short FileAlreadyExists = 0x6A89;

    /// <summary>Wrong parameters: DF name already exists.</summary>
    public const short DFNameAlreadyExists = 0x6A8A;

    #endregion

    #region Checking Errors - Instruction Not Supported (0x6DXX)

    /// <summary>Instruction code not supported or invalid.</summary>
    public const short InsNotSupported = 0x6D00;

    #endregion

    #region Checking Errors - Class Not Supported (0x6EXX)

    /// <summary>Class not supported.</summary>
    public const short ClaNotSupported = 0x6E00;

    #endregion

    #region Undiagnosed Error (0x6FXX)

    /// <summary>No precise diagnosis possible.</summary>
    public const short NoPreciseDiagnosis = 0x6F00;

    #endregion

    /// <summary>
    ///     Gets a human-readable description for a status word.
    /// </summary>
    /// <param name="sw">The status word to describe.</param>
    /// <returns>A descriptive message explaining the status word meaning.</returns>
    public static string GetStatusMessage(short sw) => sw switch
    {
        Success => "Command completed successfully",

        // Warnings - NVM Unchanged
        WarningNvmUnchanged => "No information given (NVM unchanged)",
        PartialCorruption => "Part of returned data may be corrupted",
        EOFReached => "End of file reached before reading expected length",
        FileDeactivated => "Selected file is deactivated",
        InvalidFileFormat => "File control information not formatted correctly",
        FileTerminated => "Selected file is in termination state",
        NoSensorData => "No input data available from sensor",

        // Warnings - NVM Changed
        WarningNvmChanged => "No information given (NVM changed)",
        NoMoreSpaceInFile => "File filled up by last write operation",
        >= 0x63C0 and <= 0x63CF => $"Verification failed ({sw & 0x0F} attempts remaining)",

        // Execution Errors - NVM Unchanged
        ExecutionError => "Execution error (NVM unchanged)",
        ResponseRequired => "Immediate response required by the card",

        // Execution Errors - NVM Changed
        ErrorNvmChanged => "Execution error (NVM changed)",
        MemoryFailure => "Memory failure",

        // Checking Errors
        WrongLength => "Wrong length field (Lc or Le)",
        FunctionError => "Functions in CLA not supported",
        LogicalChannelNotSupported => "Logical channel not supported",
        SecureMessagingNotSupported => "Secure messaging not supported",
        LastCommandOfChainExpected => "Last command of chain expected",
        CommandChainingNotSupported => "Command chaining not supported",

        CommandNotAllowed => "Command not allowed",
        IncompatibleCommand => "Command incompatible with file structure",
        SecurityStatusNotSatisfied => "Security status not satisfied - authentication required before executing this command",
        AuthenticationMethodBlocked => "Authentication method blocked - too many failed attempts",
        ReferenceDataUnusable => "Reference data not usable",
        ConditionsNotSatisfied => "Conditions of use not satisfied",
        CommandNotAllowedNoEF => "Command not allowed (no current EF)",
        SecureMessageDataMissing => "Expected secure messaging data objects missing",
        SecureMessageMalformed => "Incorrect secure messaging data objects",

        InvalidParameter => "Invalid parameters",
        InvalidCommandDataParameter => "Incorrect data in command data field",
        FunctionNotSupported => "Function not supported",
        FileOrApplicationNotFound => "File or application not found",
        RecordNotFound => "Record not found",
        NotEnoughSpace => "Not enough memory space in the file",
        InconsistentLengthWithTlv => "Lc inconsistent with TLV structure",
        IncorrectP1orP2 => "Incorrect P1 or P2 parameter",
        InconsistentLengthWithP1P2 => "Lc inconsistent with P1-P2",
        DataNotFound => "Referenced data not found",
        FileAlreadyExists => "File already exists",
        DFNameAlreadyExists => "DF name already exists",

        InsNotSupported => "Instruction code not supported or invalid",
        ClaNotSupported => "Class not supported",
        NoPreciseDiagnosis => "No precise diagnosis possible",

        // More data available (0x61XX)
        >= 0x6100 and <= 0x61FF => $"{sw & 0xFF} bytes still available",

        // Default for unknown status words - check high byte ranges
        _ => (sw & 0xFF00) switch
        {
            0x6200 => $"Warning - NVM unchanged (SW=0x{sw:X4})",
            0x6300 => $"Warning - NVM changed (SW=0x{sw:X4})",
            0x6400 => $"Execution error - NVM unchanged (SW=0x{sw:X4})",
            0x6500 => $"Execution error - NVM changed (SW=0x{sw:X4})",
            0x6600 => $"Security error (SW=0x{sw:X4})",
            0x6700 => $"Wrong length (SW=0x{sw:X4})",
            0x6800 => $"Function not supported (SW=0x{sw:X4})",
            0x6900 => $"Command not allowed (SW=0x{sw:X4})",
            0x6A00 => $"Wrong parameters (SW=0x{sw:X4})",
            0x6B00 => $"Wrong parameters (SW=0x{sw:X4})",
            0x6C00 => $"Wrong length (SW=0x{sw:X4})",
            0x6D00 => $"Instruction not supported (SW=0x{sw:X4})",
            0x6E00 => $"Class not supported (SW=0x{sw:X4})",
            0x6F00 => $"Undiagnosed error (SW=0x{sw:X4})",
            _ => sw >= 0 ? $"Unknown status word (SW=0x{sw:X4})" : $"Unknown success code (SW=0x{sw:X4})"
        }
    };
}
