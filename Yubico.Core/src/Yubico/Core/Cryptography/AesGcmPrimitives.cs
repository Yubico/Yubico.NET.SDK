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

namespace Yubico.Core.Cryptography
{
    /// <summary>
    /// Factory class that will return the <c>Yubico.Core</c> implementation of
    /// the <see cref="IAesGcmPrimitives"/> interface.
    /// </summary>
    public static class AesGcmPrimitives
    {
        /// <summary>
        /// Creates a new instance of an implementation of the low level AES-GCM
        /// functions.
        /// </summary>
        /// <returns>
        /// A new instance of the default implementation of this interface.
        /// </returns>
        public static IAesGcmPrimitives Create() => new AesGcmPrimitivesOpenSsl();
    }
}
