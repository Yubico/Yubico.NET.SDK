﻿// Copyright 2024 Yubico AB
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

namespace Yubico.YubiKey.Scp
{
    /// <summary>
    /// Abstract base class for parameters for a Secure Channel Protocol (SCP) key.
    /// </summary>
    public abstract class ScpKeyParameters
    {
        /// <summary>
        /// The <see cref="KeyReference"/> associated with the key parameters, used in all SCP variations.
        /// </summary>
        public KeyReference KeyReference { get; }

        /// <summary>
        /// Creates a new instance of <see cref="ScpKeyParameters"/>.
        /// </summary>
        /// <param name="keyReference"></param>
        protected ScpKeyParameters(KeyReference keyReference)
        {
            KeyReference = keyReference;
        }
    }
}
