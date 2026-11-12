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

namespace Yubico.YubiKit.Core;

/// <summary>Reports invalid data in a YubiKey response.</summary>
public class BadResponseException(string message) : Exception(message)
{
}

/// <summary>
/// Exception thrown when a platform interop (P/Invoke) operation fails.
/// </summary>
/// <remarks>
/// Call sites use this type to add operation context. Native failures are not universally wrapped in it:
/// PC/SC, Configuration Manager, and Windows report calls can expose other exception types.
/// </remarks>
public class PlatformInteropException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlatformInteropException"/> class.
    /// </summary>
    public PlatformInteropException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlatformInteropException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public PlatformInteropException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlatformInteropException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public PlatformInteropException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/* We also use:
InvalidOperationException (IllegalStateException)
TimeoutException The operation timed out waiting for something
*/