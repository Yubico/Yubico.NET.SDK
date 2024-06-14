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
using System.Globalization;

namespace Yubico.YubiKey.Piv
{
    /// <summary>
    /// This class provides extension methods for the <see cref="PivSession"/> class.
    /// </summary>
    public static class PivSessionExtensions
    {
        /// <summary>
        /// Throws a <see cref="NotSupportedException"/> if the specified <see cref="PivAlgorithm"/>
        /// is not supported by the provided <see cref="IYubiKeyDevice"/>.
        /// </summary>
        /// <param name="device">The YubiKey device to check for algorithm support.</param>
        /// <param name="algorithm">The PIV algorithm to check.</param>
        /// <exception cref="NotSupportedException">
        /// Thrown if the specified <paramref name="algorithm"/> is not supported by the
        /// <paramref name="device"/>.
        /// </exception>
        public static void ThrowIfUnsupportedAlgorithm(
            this IYubiKeyDevice device,
            PivAlgorithm algorithm)
        {
            bool isSupported = algorithm switch
            {
                PivAlgorithm.Rsa3072 => device.HasFeature(YubiKeyFeature.PivRsa3072),
                PivAlgorithm.Rsa4096 => device.HasFeature(YubiKeyFeature.PivRsa4096),
                _ => true
            };

            if (!isSupported)
            {
                throw new NotSupportedException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.NotSupportedByYubiKeyVersion));
            }
        }
    }
}
