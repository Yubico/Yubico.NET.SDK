// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Source: https://github.com/dotnet/runtime/tree/v5.0.0-preview.7.20364.11/src/libraries/System.Formats.Cbor/src/System/Formats/Cbor

using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace System.Formats.Cbor
{
    [Serializable]
    public class CborContentException : Exception
    {
        public CborContentException()
        {

        }

        public CborContentException(string? message)
            : base(message ?? CborExceptionMessages.CborContentException_DefaultMessage)
        {

        }

        public CborContentException(string? message, Exception? inner)
            : base(message ?? CborExceptionMessages.CborContentException_DefaultMessage, inner)
        {

        }

        protected CborContentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }
}
