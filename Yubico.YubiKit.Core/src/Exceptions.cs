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

public class CtapException : Exception
{
    //An error response from a YubiKey
}

public class BadResponseException(string message) : Exception(message)
{
    //The data contained in a YubiKey response was invalid
}

/// <summary>
/// Exception thrown when a platform interop (P/Invoke) operation fails.
/// </summary>
/// <remarks>
/// This exception wraps platform-specific errors from native API calls (PC/SC, HID, etc.)
/// and provides context about what operation failed.
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