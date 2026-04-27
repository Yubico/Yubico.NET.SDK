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
    /// Factory for the default <see cref="IArkgPrimitives"/> implementation.
    /// </summary>
    /// <remarks>
    /// This factory creates instances of the platform-specific ARKG-P256
    /// cryptographic primitives implementation. The default implementation
    /// uses OpenSSL via P/Invoke through the Yubico.NativeShims library.
    /// </remarks>
    public static class ArkgPrimitives
    {
        /// <summary>
        /// Creates the OpenSSL-backed ARKG primitives instance.
        /// </summary>
        /// <returns>
        /// An <see cref="IArkgPrimitives"/> implementation that performs
        /// ARKG-P256 operations using OpenSSL.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method returns a new instance on each call. The implementation
        /// is stateless and thread-safe.
        /// </para>
        /// <para>
        /// For testing or custom implementations, applications can replace the
        /// default factory by setting the <c>ArkgPrimitivesCreator</c> property
        /// in <c>Yubico.YubiKey.Cryptography.CryptographyProviders</c>.
        /// </para>
        /// </remarks>
        public static IArkgPrimitives Create() => new ArkgPrimitivesOpenSsl();
    }
}
