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
using System.Buffers.Binary;
using System.Globalization;
using System.IO;

namespace Yubico.Core.Iso7816
{
    /// <summary>
    /// Represents an ISO 7816 application command
    /// </summary>
    public class CommandApdu
    {
        private const int maximumSizeShortEncoding = 256;
        private const int maximumSizeExtendedEncoding = 65536;

        // Backing store for `public int Ne`
        private int _ne;

        /// <summary>
        /// Indicates the class of the instruction.
        /// </summary>
        public byte Cla { get; set; }

        /// <summary>
        /// Indicates the command or instruction to process.
        /// </summary>
        public byte Ins { get; set; }

        /// <summary>
        /// First parameter byte.
        /// </summary>
        public byte P1 { get; set; }

        /// <summary>
        /// Second parameter byte.
        /// </summary>
        public byte P2 { get; set; }

        /// <summary>
        /// Gets or sets the optional command data payload.
        /// </summary>
        public ReadOnlyMemory<byte> Data { get; set; } = ReadOnlyMemory<byte>.Empty;

        /// <summary>
        /// The number of bytes in <see cref="Data"/>.
        /// </summary>
        /// <remarks>
        /// If <see cref="Data"/> is <c>null</c>, returns 0.
        /// </remarks>
        public int Nc => Data.Length;

        /// <summary>
        /// The maximum number of bytes expected in the response data.
        /// Must be a non-negative number.
        /// </summary>
        /// <remarks>
        /// Values of note:
        /// <list type="bullet">
        /// <item>
        /// <term>0</term>
        /// <description>No data is expected to be returned.</description>
        /// </item>
        /// <item>
        /// <term><see cref="int.MaxValue"/></term>
        /// <description>
        /// Maximum value according to the encoding used. See <see cref="AsByteArray()"/>
        /// and <see cref="AsByteArray(ApduEncoding)"/>.
        /// </description>
        /// </item>
        /// </list>
        /// </remarks>
        public int Ne
        {
            get => _ne;

            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(Ne), ExceptionMessages.CommandApduNeRangeError);
                }
                else
                {
                    _ne = value;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandApdu"/> class.
        /// </summary>
        public CommandApdu()
        {

        }

        /// <summary>
        /// Transforms the CommandApdu into an array of bytes.
        /// </summary>
        /// <remarks>
        /// Automatically determines the appropriate encoding to use.
        /// See also <seealso cref="AsByteArray(ApduEncoding)"/>.
        /// </remarks>
        /// <returns>An array of bytes representing an ISO 7816 CommandApdu.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no valid <see cref="ApduEncoding"/> scheme is found for the
        /// current state of <see cref="CommandApdu"/>.
        /// </exception>
        public byte[] AsByteArray() => AsByteArray(ApduEncoding.Automatic);

        /// <summary>
        /// Transforms the CommandApdu into an array of bytes.
        /// </summary>
        /// <remarks>
        /// All <see cref="CommandApdu"/> fields must be valid for the given
        /// <paramref name="apduEncoding"/>.
        /// </remarks>
        /// <param name="apduEncoding">
        /// The <see cref="ApduEncoding"/> in which the output is written.
        /// </param>
        /// <returns>An array of bytes representing an ISO 7816 CommandApdu.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no valid <see cref="ApduEncoding"/> scheme is found for the
        /// current state of <see cref="CommandApdu"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="apduEncoding"/> is invalid.
        /// </exception>
        public byte[] AsByteArray(ApduEncoding apduEncoding)
        {
            if (apduEncoding == ApduEncoding.Automatic)
            {
                apduEncoding = GetApduEncoding();
            }

            using var apduBuffer = new MemoryStream();
            using var apduWriter = new BinaryWriter(apduBuffer);

            // Write command header
            apduWriter.Write(Cla);
            apduWriter.Write(Ins);
            apduWriter.Write(P1);
            apduWriter.Write(P2);

            // Write Lc
            apduWriter.Write(GetLcField(apduEncoding));

            // Write Data
            apduWriter.Write(Data.ToArray());

            // Write Le
            apduWriter.Write(GetLeField(apduEncoding));

            return apduBuffer.ToArray();
        }

        // Uses the current values of Nc and Ne to determine the appropriate
        // ApduEncoding to use. If there is no valid encoding, it throws an exception.
        private ApduEncoding GetApduEncoding()
        {
            if (ValidNc(ApduEncoding.ShortLength) && ValidNe(ApduEncoding.ShortLength))
            {
                return ApduEncoding.ShortLength;
            }
            else if (ValidNc(ApduEncoding.ExtendedLength) && ValidNe(ApduEncoding.ExtendedLength))
            {
                return ApduEncoding.ExtendedLength;
            }
            else
            {
                throw new InvalidOperationException(ExceptionMessages.CommandApduNoValidEncoding);
            }
        }

        // Returns the inclusive upper bound for a data length value.
        // <exception cref="ArgumentOutOfRangeException">
        // <paramref name="apduEncoding"/> is not supported.
        // </exception>
        private static int GetInclusiveUpperBound(ApduEncoding apduEncoding) =>
            apduEncoding switch
            {
                ApduEncoding.Automatic => maximumSizeExtendedEncoding,
                ApduEncoding.ShortLength => maximumSizeShortEncoding,
                ApduEncoding.ExtendedLength => maximumSizeExtendedEncoding,
                _ => throw new ArgumentOutOfRangeException(nameof(apduEncoding)),
            };

        // Checks that Nc is valid, given the encoding.
        private bool ValidNc(ApduEncoding apduEncoding)
        {
            int inclusiveUpperBound = GetInclusiveUpperBound(apduEncoding);

            return Nc >= 0 && Nc <= inclusiveUpperBound;
        }

        // Checks that Ne is valid, given the encoding.
        private bool ValidNe(ApduEncoding apduEncoding)
        {
            int inclusiveUpperBound = GetInclusiveUpperBound(apduEncoding);

            return Ne == int.MaxValue || (Ne >= 0 && Ne <= inclusiveUpperBound);
        }

        // Validates Nc, then returns the Lc field as a byte array in the given encoding.
        // Does not accept ApduEncoding.Automatic; see CommandApdu.GetApduEncoding().
        private byte[] GetLcField(ApduEncoding apduEncoding)
        {
            if (apduEncoding == ApduEncoding.Automatic)
            {
                throw new ArgumentOutOfRangeException(nameof(apduEncoding));
            }
            else
            {
                if (!ValidNc(apduEncoding))
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.CommandApduFieldOutOfRangeEncoding,
                            nameof(Nc),
                            Enum.GetName(typeof(ApduEncoding), apduEncoding)));
                }
            }

            byte[] lcField = Array.Empty<byte>();

            if (Nc > 0)
            {
                int lcValue = Nc;   // The encoded value, derived from Nc

                if (apduEncoding == ApduEncoding.ExtendedLength)
                {
                    if (Nc == maximumSizeExtendedEncoding)
                    {
                        lcValue = 0;
                    }

                    lcField = new byte[3];
                    lcField[0] = 0;
                    BinaryPrimitives.WriteInt16BigEndian(lcField.AsSpan(1), (short)lcValue);
                }
                else
                {
                    if (Nc == maximumSizeShortEncoding)
                    {
                        lcValue = 0;
                    }

                    lcField = new byte[] { (byte)lcValue };
                }
            }

            return lcField;
        }

        // Validates Ne, then writes the Le field as a byte array in the given encoding.
        // Does not accept ApduEncoding.Automatic; see CommandApdu.GetApduEncoding().
        private byte[] GetLeField(ApduEncoding apduEncoding)
        {
            if (apduEncoding == ApduEncoding.Automatic)
            {
                throw new ArgumentOutOfRangeException(nameof(apduEncoding));
            }
            else
            {
                if (!ValidNe(apduEncoding))
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.CommandApduFieldOutOfRangeEncoding,
                            nameof(Ne),
                            Enum.GetName(typeof(ApduEncoding), apduEncoding)));
                }
            }

            byte[] leField = Array.Empty<byte>();

            if (Ne > 0)
            {
                int leValue = Ne == int.MaxValue ? 0 : Ne;   // The encoded value, derived from Ne

                if (apduEncoding == ApduEncoding.ExtendedLength)
                {
                    if (Ne == maximumSizeExtendedEncoding)
                    {
                        leValue = 0;
                    }

                    if (Nc == 0)
                    {
                        leField = new byte[3];
                        leField[0] = 0;
                        BinaryPrimitives.WriteInt16BigEndian(leField.AsSpan(1), (short)leValue);
                    }
                    else
                    {
                        leField = new byte[2];
                        BinaryPrimitives.WriteInt16BigEndian(leField, (short)leValue);
                    }
                }
                else
                {

                    if (Ne == maximumSizeShortEncoding)
                    {
                        leValue = 0;
                    }

                    leField = new byte[] { (byte)leValue };
                }
            }

            return leField;
        }

        public override string ToString() => $"CLA: 0x{Cla:X2} INS: 0x{Ins:X2} P1: 0x{P1:X2} P2: 0x{P2:X2} Lc: {Nc} Le: {Ne} Data: {Data.Length} bytes";
    }
}
