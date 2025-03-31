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

using System;
using System.Security.Cryptography;

namespace Yubico.YubiKey.Cryptography
{
    /// <summary>
    /// Base class for EC key parameters
    /// </summary>
    public abstract class ECKeyParameters : IKeyParameters // TODO Should be removed if possible without breaking changes 
    {
        private KeyDefinition _keyDefinition { get; }

        /// <summary>
        /// Gets the EC parameters associated with this key.
        /// </summary>
        /// <remarks>
        /// These parameters include the curve information and key data.
        /// </remarks>
        public ECParameters Parameters { get; }

        /// <summary>
        /// Creates an <see cref="ECKeyParameters"/> from an <see cref="ECParameters"/> object
        /// </summary>
        /// <param name="parameters"></param>
        /// <exception cref="NotSupportedException"></exception>
        protected ECKeyParameters(ECParameters parameters)
        {
            Parameters = parameters.DeepCopy();
            _keyDefinition = KeyDefinitions.GetByOid(parameters.Curve.Oid);
        }

        public KeyDefinition KeyDefinition => _keyDefinition;
        public KeyType KeyType => _keyDefinition.KeyType;
    }
}
