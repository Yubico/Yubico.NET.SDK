// Copyright 2023 Yubico AB
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
using System.Collections.Generic;
using System.Formats.Cbor;
using System.Globalization;
using Yubico.YubiKey.Fido2.Cbor;
using Yubico.YubiKey.Fido2.Cose;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// These are the possible values of an element that has requirement
    /// conditions.
    /// </summary>
    /// <remarks>
    /// Some elements might be required in some situations, others are optional,
    /// and others are ignored. This enum is used to report what the requirement
    /// level is.
    /// </remarks>
    public enum RequirementValue
    {
        /// <summary>
        /// The element is ignored. It is not required, but it is not optional
        /// either. But if the element is provided, it is ignored, as opposed to
        /// an error.
        /// </summary>
        Ignored = 0,

        /// <summary>
        /// The element is required.
        /// </summary>
        Required = 1,

        /// <summary>
        /// The element is optional.
        /// </summary>
        Optional = 2,
    }
}

