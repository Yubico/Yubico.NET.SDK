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

namespace Yubico.YubiKit.Tests.Shared.Infrastructure;

/// <summary>
///     Exception thrown when allow list validation fails.
///     This exception indicates a critical safety violation - either the allow list
///     is not configured, or a device is not authorized for testing.
/// </summary>
public class AllowListException : Exception
{
    /// <summary>
    ///     Initializes a new instance of <see cref="AllowListException"/> with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the allow list violation.</param>
    public AllowListException(string message) : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of <see cref="AllowListException"/> with a specified error message
    ///     and a reference to the inner exception that caused this exception.
    /// </summary>
    /// <param name="message">The message that describes the allow list violation.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public AllowListException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
