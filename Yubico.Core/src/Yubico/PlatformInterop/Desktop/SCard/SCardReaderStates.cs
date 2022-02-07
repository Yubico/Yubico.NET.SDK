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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Yubico.Core.Iso7816;

namespace Yubico.PlatformInterop
{
    internal class SCardReaderStates : IDisposable, IEnumerable<SCardReaderStates.Entry>, ICloneable
    {
        public byte[] Buffer { get; private set; }

        public int Count { get; }

        private SCardReaderStates(byte[] buffer)
        {
            Count = buffer.Length / Entry.Size;
            Buffer = buffer;
        }

        public SCardReaderStates(int numberOfStates)
        {
            Count = numberOfStates;
            Buffer = new byte[Entry.Size * numberOfStates];
        }

        public SCardReaderStates(string[] readerNames)
        {
            Count = readerNames.Length;
            Buffer = new byte[Entry.Size * readerNames.Length];

            for (int i = 0; i < readerNames.Length; i++)
            {
                this[i].ReaderName = readerNames[i];
            }
        }

        public Entry this[int index]
        {
            get
            {
                if (index > Count)
                {
                    throw new IndexOutOfRangeException();
                }

                return new Entry(Buffer.AsMemory(index * Entry.Size, Entry.Size));
            }
        }

        public class Entry
        {
            private const uint SequenceMask = 0xFFFF_0000;
            private const uint StateMask = 0x0000_FFFF;

            private readonly Memory<byte> _bufferSlice;

            private bool _disposed;

            private static readonly int _readerNameOffset;
            private static readonly int _currentStateOffset;
            private static readonly int _eventStateOffset;
            private static readonly int _atrSizeOffset;
            private static readonly int _atrOffset;
            public static readonly int Size;

#pragma warning disable CA1810 // Justification: Allow explicit static constructor here
            static Entry()
#pragma warning restore CA1810
            {
                _readerNameOffset = 0;
                _currentStateOffset = _readerNameOffset + (IntPtr.Size * 2);
                _eventStateOffset = _currentStateOffset + SdkPlatformInfo.DwordSize;
                _atrSizeOffset = _eventStateOffset + SdkPlatformInfo.DwordSize;
                _atrOffset = _atrSizeOffset + SdkPlatformInfo.DwordSize;

                // Note: Windows defines the maximum ATR size to be 36 bytes.
                // PCSC defines it as 33. For Linux, however, because of
                // alignment, set the size to 40.
                // Since this byte array is embedded in the structure, and the
                // SCardGetStatusChange function takes an array of this
                // structure, we need to adjust the size depending on the
                // operating system / implementation. Failure to do this results
                // in alignment issues past the first smart card reader entry and
                // will cause an access violation in the best of cases.
                Size = SdkPlatformInfo.OperatingSystem switch
                {
                    SdkPlatform.Windows => _atrOffset + 36,
                    SdkPlatform.Linux => _atrOffset + 40,
                    _ => _atrOffset + 33,
                };
            }

            public Entry(Memory<byte> bufferSlice)
            {
                _bufferSlice = bufferSlice;
            }

            public string ReaderName
            {
                get
                {
                    IntPtr value = ReadIntPtr(_readerNameOffset);
                    if (value == IntPtr.Zero)
                    {
                        return string.Empty;
                    }

                    return MarshalToString(value);
                }
                set
                {
                    IntPtr existingValue = ReadIntPtr(_readerNameOffset);
                    IntPtr newValue = IntPtr.Zero;

                    try
                    {
                        if (existingValue != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(existingValue);
                        }

                        newValue = MarshalFromString(value);
                        WriteIntPtr(_readerNameOffset, newValue);
                        newValue = IntPtr.Zero;
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(newValue);
                    }
                }
            }

            public SCARD_STATE CurrentState
            {
                get => (SCARD_STATE)(ReadInt32(_currentStateOffset) & StateMask);
                set => WriteUInt32(_currentStateOffset, ((uint)CurrentSequence << 16) | ((uint)value & StateMask));
            }

            public SCARD_STATE EventState
            {
                get => (SCARD_STATE)(ReadInt32(_eventStateOffset) & StateMask);
                set => WriteUInt32(_eventStateOffset, ((uint)EventSequence << 16) | ((uint)value & StateMask));
            }

            public int CurrentSequence
            {
                get => (int)(ReadInt32(_currentStateOffset) & SequenceMask) >> 16;
                set => WriteUInt32(_currentStateOffset, (((uint)value << 16) & SequenceMask) | (uint)CurrentState);
            }

            public int EventSequence
            {
                get => (int)(ReadInt32(_eventStateOffset) & SequenceMask) >> 16;
                set => WriteUInt32(_eventStateOffset, (((uint)value << 16) & SequenceMask) | (uint)EventState);
            }

            private int AtrDynamicSize => ReadInt32(_atrSizeOffset);

            public AnswerToReset Atr => new AnswerToReset(_bufferSlice.Slice(_atrOffset, AtrDynamicSize).Span);

            public override string ToString() => $"{ReaderName}: [{CurrentSequence} : {CurrentState}] => [{EventSequence} : {EventState}]";

            private static string MarshalToString(IntPtr readerNamePtr)
            {
                string? value = SdkPlatformInfo.OperatingSystem switch
                {
                    SdkPlatform.Windows => Marshal.PtrToStringUni(readerNamePtr),
                    _ => Marshal.PtrToStringAnsi(readerNamePtr)
                };

                if (value is null)
                {
                    throw new ArgumentNullException(nameof(readerNamePtr));
                }

                return value;
            }

            private static IntPtr MarshalFromString(string readerName) => SdkPlatformInfo.OperatingSystem switch
            {
                SdkPlatform.Windows => Marshal.StringToHGlobalUni(readerName),
                _ => Marshal.StringToHGlobalAnsi(readerName)
            };

            private int ReadInt32(int offset)
            {
                Span<byte> slice = _bufferSlice.Span.Slice(offset, 4);
                return BitConverter.ToInt32(slice.ToArray(), 0);
            }

            private IntPtr ReadIntPtr(int offset)
            {
                long ptr = IntPtr.Size == sizeof(int)
                    ? BitConverter.ToInt32(_bufferSlice.Span.Slice(offset, 4).ToArray(), 0)
                    : BitConverter.ToInt64(_bufferSlice.Span.Slice(offset, 8).ToArray(), 0);

                return new IntPtr(ptr);
            }

            private void WriteInt32(int offset, int value)
            {
                byte[] raw = BitConverter.GetBytes(value);
                raw.AsMemory().CopyTo(_bufferSlice.Slice(offset, raw.Length));
            }

            private void WriteUInt32(int offset, uint value)
            {
                byte[] raw = BitConverter.GetBytes(value);
                raw.AsMemory().CopyTo(_bufferSlice.Slice(offset, raw.Length));
            }

            private void WriteIntPtr(int offset, IntPtr ptr)
            {
                byte[] raw;

                if (IntPtr.Size == sizeof(int))
                {
                    raw = BitConverter.GetBytes(ptr.ToInt32());
                }
                else
                {
                    raw = BitConverter.GetBytes(ptr.ToInt64());
                }

                raw.AsMemory().CopyTo(_bufferSlice.Slice(offset, raw.Length));
            }

            internal void ReleaseReaderName()
            {
                if (!_disposed)
                {
                    IntPtr readerName = ReadIntPtr(_readerNameOffset);
                    if (readerName != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(readerName);
                    }

                    _disposed = true;
                }
            }
        }

        private void ReleaseUnmanagedResources()
        {
            foreach (Entry item in this)
            {
                item.ReleaseReaderName();
            }
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~SCardReaderStates()
        {
            ReleaseUnmanagedResources();
        }

        public object Clone()
        {
            byte[] buffer = (byte[])Buffer.Clone();

            // Writing zeros so that reader names in the original states
            // won't be deallocated when we copy them to the new list.
            for (int i = 0; i < Count; i++)
            {
                byte[] raw = BitConverter.GetBytes(
                    IntPtr.Size == sizeof(int)
                        ? IntPtr.Zero.ToInt32()
                        : IntPtr.Zero.ToInt64());
                System.Buffer.BlockCopy(raw, 0, buffer, i * Entry.Size, raw.Length);
            }

            var newStates = new SCardReaderStates(buffer);

            for (int i = 0; i < Count; i++)
            {
                SCardReaderStates.Entry state = this[i];
                newStates[i].ReaderName = state.ReaderName;
            }

            return newStates;
        }

        public IEnumerator<Entry> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (Entry? e in this.AsEnumerable())
            {
                _ = sb.AppendLine(e.ToString());
            }
            return sb.ToString();
        }
    }
}
