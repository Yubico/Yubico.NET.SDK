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
using System.Diagnostics;
using System.Runtime.Serialization;
using Yubico.Core;

namespace Yubico.PlatformInterop;

[Serializable]
public class PlatformApiException : Exception
{
    public PlatformApiException() :
        this(ExceptionMessages.UnknownPlatformApiError)
    {
        Debug.Assert(false, "You should always call a more specific constructor for this exception type.");
    }

    public PlatformApiException(string message) : base(message)
    {
    }

    public PlatformApiException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected PlatformApiException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public PlatformApiException(string source, int errorCode, string message) :
        base($"Encountered a platform API exception. {source} = 0x{errorCode:X8}.\n\n{message}")
    {
    }
}
