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
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey
{
    /// <summary>
    /// Base class for all YubiKey responses.
    /// </summary>
    /// <seealso cref="IYubiKeyResponse" />
    /// <remarks>
    /// <para>This base class is primarily responsible for mapping YubiKey specific status words to more
    /// generic constructs like ResponseStatus and exceptions.</para>
    /// <para>This class can also be overridden to customize error handling if a certain application
    /// or command requires special casing.</para>
    ///
    /// <para>
    /// If the subtype needs to change the mappings associated with an
    /// existing status code, it should override <see cref="StatusCodeMap"/>.
    /// For example:
    /// </para>
    /// <para>
    /// <code language="csharp">
    /// public class MyResponse : YubiKeyResponse
    /// {
    ///     // MyResponse has custom definitions for what certain StatusWord
    ///     // values mean. It only has to override this map, providing
    ///     // the new values. It calls the base method so that any unknown
    ///     // values are passed through, potentially being "understood"
    ///     // by types MyResponse inherited from.
    ///     protected override ResponseStatusPair StatusCodeMap =>
    ///         StatusWord switch
    ///         {
    ///             // Add new maps or override existing ones here
    ///             _ => base.StatusCodeMap,
    ///         };
    ///
    ///     public MyResponse(ResponseApdu responseApdu) : base(responseApdu)
    ///     {
    ///     }
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <see cref="StatusCodeMap"/> can also be overridden if the subtype introduces
    /// a new status code. This typically happens when the
    /// <c>ResponseApdu.Data</c> is actually an encoded message which
    /// contains its own status code.
    /// </para>
    /// </remarks>
    public class YubiKeyResponse : IYubiKeyResponse
    {
        /// <summary>
        /// The APDU returned by the YubiKey.
        /// </summary>
        protected ResponseApdu ResponseApdu { get; set; }

        /// <summary>
        /// Retrieves the details describing the processing state.
        /// </summary>
        /// <remarks>
        /// Implementers of subtypes can override this member to change or add mappings.
        /// </remarks>
        /// <returns>
        /// The ResponseStatus and a descriptive message, as a <see cref="ResponseStatusPair"/>.
        /// </returns>
        protected virtual ResponseStatusPair StatusCodeMap =>
            StatusWord switch
            {
                //
                // These mappings are based off of ISO7816-4
                //
                SWConstants.Success => new ResponseStatusPair(ResponseStatus.Success, ResponseStatusMessages.BaseSuccess),

                // Warnings
                SWConstants.WarningNvmUnchanged => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseWarningNvmUnchanged),
                SWConstants.PartialCorruption => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BasePartialCorruption),
                SWConstants.EOFReached => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseEOFReached),
                SWConstants.FileDeactivated => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseFileDeactivated),
                SWConstants.InvalidFileFormat => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseInvalidFileFormat),
                SWConstants.FileTerminated => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseFileTerminated),
                SWConstants.NoSensorData => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseNoSensorData),

                SWConstants.WarningNvmChanged => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseWarningNvmChanged),
                SWConstants.NoMoreSpaceInFile => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseNoMoreSpaceInFile),

                // Errors
                SWConstants.ExecutionError => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseExecutionError),
                SWConstants.ResponseRequired => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseResponseRequired),

                SWConstants.ErrorNvmChanged => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseErrorNvmChanged),
                SWConstants.MemoryFailure => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseMemoryFailure),

                SWConstants.WrongLength => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseWrongLength),

                SWConstants.FunctionError => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseFunctionError),
                SWConstants.LogicalChannelNotSupported => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseLogicalChannelNotSupported),
                SWConstants.SecureMessagingNotSupported => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseSecureMessagingNotSupported),
                SWConstants.LastCommandOfChainExpected => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseLastCommandOfChainExpected),
                SWConstants.CommandChainingNotSupported => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseCommandChainingNotSupported),

                SWConstants.CommandNotAllowed => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseCommandNotAllowed),
                SWConstants.IncompatibleCommand => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseIncompatibleCommand),
                SWConstants.SecurityStatusNotSatisfied => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseSecurityStatusNotSatisfied),
                SWConstants.AuthenticationMethodBlocked => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseAuthenticationMethodBlocked),
                SWConstants.ReferenceDataUnusable => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseReferenceDataUnusable),
                SWConstants.ConditionsNotSatisfied => new ResponseStatusPair(ResponseStatus.ConditionsNotSatisfied, ResponseStatusMessages.BaseConditionsNotSatisfied),
                SWConstants.CommandNotAllowedNoEF => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseCommandNotAllowedNoEF),
                SWConstants.SecureMessageDataMissing => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseSecureMessageDataMissing),
                SWConstants.SecureMessageMalformed => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseSecureMessageMalformed),

                SWConstants.InvalidParameter => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseInvalidParameter),
                SWConstants.InvalidCommandDataParameter => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseInvalidCommandDataParameter),
                SWConstants.FunctionNotSupported => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseFunctionNotSupported),
                SWConstants.FileOrApplicationNotFound => new ResponseStatusPair(ResponseStatus.NoData, ResponseStatusMessages.BaseFileOrApplicationNotFound),
                SWConstants.RecordNotFound => new ResponseStatusPair(ResponseStatus.NoData, ResponseStatusMessages.BaseRecordNotFound),
                SWConstants.NotEnoughSpace => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseNotEnoughSpace),
                SWConstants.InconsistentLengthWithTlv => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseInconsistentLengthWithTlv),
                SWConstants.IncorrectP1orP2 => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseIncorrectP1orP2),
                SWConstants.InconsistentLengthWithP1P2 => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseInconsistentLengthWithP1P2),
                SWConstants.DataNotFound => new ResponseStatusPair(ResponseStatus.NoData, ResponseStatusMessages.BaseDataNotFound),
                SWConstants.FileAlreadyExists => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseFileAlreadyExists),
                SWConstants.DFNameAlreadyExists => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseDFNameAlreadyExists),

                SWConstants.InsNotSupported => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseInsNotSupported),

                SWConstants.ClaNotSupported => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseClaNotSupported),

                // Default
                _ => new ResponseStatusPair(ResponseStatus.Failed, ResponseStatusMessages.BaseFailed),
            };

        /// <summary>
        /// Initializes a new instance of the <see cref="YubiKeyResponse"/> class.
        /// </summary>
        /// <param name="responseApdu">The ResponseApdu from the YubiKey.</param>
        /// <exception cref="ArgumentNullException">responseApdu</exception>
        public YubiKeyResponse(ResponseApdu responseApdu)
        {
            ResponseApdu = responseApdu ?? throw new ArgumentNullException(nameof(responseApdu));
        }

        /// <inheritdoc />
        public ResponseStatus Status => StatusCodeMap.Status;

        /// <inheritdoc />
        public short StatusWord => ResponseApdu.SW;

        /// <inheritdoc />
        public string StatusMessage => StatusCodeMap.StatusMessage;

        public override string ToString() => string.Join(
            ", ",
            new[]
            {
                $"Status: [{ StatusMessage }]",
                $"Code[Status.{ Status }]",
                $"APDU SW[0x{ ResponseApdu.SW.ToString("x4", CultureInfo.InvariantCulture) }]"
            });

        /// <summary>
        /// Represents a ResponseStatus and StatusMessage pair returned by <see cref="StatusCodeMap"/>.
        /// </summary>
        protected sealed class ResponseStatusPair
        {
            public ResponseStatus Status { get; }
            public string StatusMessage { get; }

            public ResponseStatusPair(ResponseStatus status, string statusMessage)
            {
                Status = status;
                StatusMessage = statusMessage;
            }
        }
    }
}
