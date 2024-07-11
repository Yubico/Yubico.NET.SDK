// Copyright 2021 Yubico AB
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

// Portions of this file have been adopted from the .NET runtime under the following license:
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Original source: https://github.com/dotnet/runtime/blob/main/src/libraries/System.Security.Cryptography/src/System/Security/Cryptography/RandomNumberGenerator.cs

using System;
using System.Globalization;
using System.Security.Cryptography;

namespace Yubico.YubiKey.Cryptography
{
    /// <summary>
    /// Extension class to extend random number functionality.
    /// </summary>
    public static class RandomNumberGeneratorExt
    {
        /// <summary>
        /// Gets a random 32-bit signed int.
        /// </summary>
        /// <param name="rng">The <see cref="RandomNumberGenerator"/> instance being extended.</param>
        /// <param name="fromInclusive">The lowest value of the range.</param>
        /// <param name="toExclusive">One above the highest value of the range.</param>
        /// <returns>Random <see langword="Int32"/>.</returns>
        public static int GetInt32(
            this RandomNumberGenerator rng,
            int fromInclusive,
            int toExclusive)
        {
            if (rng is null)
            {
                throw new ArgumentNullException(nameof(rng));
            }
            if (fromInclusive >= toExclusive)
            {
                throw new ArithmeticException(string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.ValueMustBeBetweenXandY,
                    int.MinValue,
                    (long)int.MaxValue + 1));
            }

            uint range = (uint)toExclusive - (uint)fromInclusive - 1;
            // Mask away bits beyond our range.
            uint mask = range;
            mask |= mask >> 1;
            mask |= mask >> 2;
            mask |= mask >> 4;
            mask |= mask >> 8;
            mask |= mask >> 16;
            uint result = int.MaxValue;
            while (result > range)
            {
                byte[] data = new byte[sizeof(int)];
                rng.GetBytes(data);
                result = mask & BitConverter.ToUInt32(data, 0);
            }
            return (int)result + fromInclusive;
        }

        /// <summary>
        /// Fill a range with random bytes.
        /// </summary>
        /// <param name="rng">The <see cref="RandomNumberGenerator"/> instance being extended.</param>
        /// <param name="data">A <see cref="Span{T}"/> to fill with random bytes.</param>
        public static void Fill(
            this RandomNumberGenerator rng,
            Span<byte> data)
        {
            for (int i = 0; i < data.Length; ++i)
            {
                data[i] = rng.GetByte(0x00, 0x100);
            }
        }

        /// <summary>
        /// Get a <see langword="byte"/> with a random value.
        /// </summary>
        /// <param name="rng">The <see cref="RandomNumberGenerator"/> instance being extended.</param>
        /// <param name="fromInclusive">The lowest value of the range.</param>
        /// <param name="toExclusive">One above the highest value of the range.</param>
        /// <returns></returns>
        public static byte GetByte(
            this RandomNumberGenerator rng,
            int fromInclusive,
            int toExclusive)
        {
            if (fromInclusive < 0
                || toExclusive > 0x100
                || fromInclusive >= toExclusive)
            {
                throw new ArithmeticException(string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.ValueMustBeBetweenXandY,
                    byte.MinValue,
                    byte.MaxValue + 1));
            }
            return (byte)rng.GetInt32(fromInclusive, toExclusive);
        }
    }
}
