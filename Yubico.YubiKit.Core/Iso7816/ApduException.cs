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

using System;
using System.Runtime.Serialization;

namespace Yubico.YubiKit.Core.Iso7816;

/// <summary>
/// The exception that is thrown when an ISO 7816 application has encountered an error.
/// </summary>
[Serializable]
public class ApduException : Exception
{
    /// <summary>
    /// Gets or sets the status word (SW), the ISO 7816 numerical value which represents
    /// the specific error or warning encountered.
    /// </summary>
    /// <value>
    /// The status word value. This can either be an industry defined error, or vendor defined.
    /// </value>
    public short? SW { get; set; }

    /// <summary>
    /// Gets or sets the APDU class associated with this exception.
    /// </summary>
    /// <value>
    /// The class value of the command APDU when the exception occurred.
    /// </value>
    public byte? Cla { get; set; }

    /// <summary>
    /// Gets or sets the APDU instruction associated with this exception.
    /// </summary>
    /// <value>
    /// The instruction of the command APDU when the exception occurred.
    /// </value>
    public byte? Ins { get; set; }

    /// <summary>
    /// Gets or sets the P1 parameter associated with this exception.
    /// </summary>
    /// <value>
    /// The first parameter of the command APDU when the exception occurred.
    /// </value>
    public byte? P1 { get; set; }

    /// <summary>
    /// Gets or sets the P2 parameter associated with this exception.
    /// </summary>
    /// <value>
    /// The second parameter of the command APDU when the exception occurred.
    /// </value>
    public byte? P2 { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApduException"/> class with a default message.
    /// </summary>
    public ApduException() :
        base("ExceptionMessages.UnknownApduError")
    {

    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApduException"/> class with a specified error
    /// message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ApduException(string message) :
        base(message)
    {

    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApduException"/> class with a specified error
    /// message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception,
    /// or a null reference (<see langword="Nothing" /> in Visual Basic) if no inner exception
    /// is specified.</param>
    public ApduException(string message, Exception innerException) :
        base(message, innerException)
    {

    }
}
