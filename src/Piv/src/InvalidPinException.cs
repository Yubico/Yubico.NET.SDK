// Copyright 2024 Yubico AB
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

namespace Yubico.YubiKit.Piv;

/// <summary>
/// Exception thrown when PIN or PUK verification fails.
/// </summary>
public class InvalidPinException : Exception
{
    /// <summary>
    /// Gets the number of retries remaining before the PIN/PUK is locked.
    /// </summary>
    public int RetriesRemaining { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPinException"/> class.
    /// </summary>
    /// <param name="retriesRemaining">Number of retries remaining.</param>
    public InvalidPinException(int retriesRemaining)
        : base(GetMessage(retriesRemaining))
    {
        RetriesRemaining = retriesRemaining;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPinException"/> class.
    /// </summary>
    /// <param name="retriesRemaining">Number of retries remaining.</param>
    /// <param name="message">Custom error message.</param>
    public InvalidPinException(int retriesRemaining, string message)
        : base(message)
    {
        RetriesRemaining = retriesRemaining;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPinException"/> class.
    /// </summary>
    /// <param name="retriesRemaining">Number of retries remaining.</param>
    /// <param name="message">Custom error message.</param>
    /// <param name="innerException">Inner exception.</param>
    public InvalidPinException(int retriesRemaining, string message, Exception innerException)
        : base(message, innerException)
    {
        RetriesRemaining = retriesRemaining;
    }

    private static string GetMessage(int retriesRemaining)
    {
        return retriesRemaining switch
        {
            0 => "PIN/PUK verification failed. No retries remaining - PIN/PUK is now locked.",
            1 => "PIN/PUK verification failed. 1 retry remaining before lockout.",
            _ => $"PIN/PUK verification failed. {retriesRemaining} retries remaining before lockout."
        };
    }
}