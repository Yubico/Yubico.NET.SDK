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

using System.Runtime.InteropServices;

namespace Yubico.YubiKit.Core.Native.Desktop.SCard;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct SCARD_READER_STATE
{
    public IntPtr ReaderName;
    public IntPtr UserData;
    public uint CurrentState;
    public uint EventState;
    public uint AtrLength;
    public fixed byte AnswerToReset[36];

    private SCARD_READER_STATE(IntPtr readerName)
    {
        ReaderName = readerName;
        UserData = IntPtr.Zero;
        CurrentState = 0;
        EventState = 0;
        AtrLength = 0;
    }

    public static SCARD_READER_STATE Create(string readerName) => new(Marshal.StringToHGlobalAnsi(readerName));

    /// <summary>
    ///     Allocates reader names, releasing earlier allocations if construction fails.
    ///     On success, the caller owns the names and must call <see cref="FreeReaderNames" /> in finally.
    /// </summary>
    /// <remarks>
    ///     Struct copies borrow the same pointers; release only through one owning array.
    ///     Optional allocation/release delegates must be paired, and release must not throw.
    /// </remarks>
    public static SCARD_READER_STATE[] CreateMany(
        IEnumerable<string> readerNames,
        Func<string, IntPtr>? allocateReaderName = null,
        Action<IntPtr>? freeReaderName = null)
    {
        // Complete managed enumeration and storage allocation before owning any native names.
        string[] names = readerNames.ToArray();
        var states = new SCARD_READER_STATE[names.Length];
        allocateReaderName ??= Marshal.StringToHGlobalAnsi;
        try
        {
            for (int i = 0; i < names.Length; i++)
                states[i] = new SCARD_READER_STATE(allocateReaderName(names[i]));
            return states;
        }
        catch
        {
            FreeReaderNames(states, freeReaderName);
            throw;
        }
    }

    /// <summary>
    ///     Releases owned reader names and clears their pointers in the supplied storage.
    ///     Clearing permits repeated cleanup of that storage, not cleanup of borrowed struct copies.
    /// </summary>
    public static void FreeReaderNames(Span<SCARD_READER_STATE> states, Action<IntPtr>? freeReaderName = null)
    {
        freeReaderName ??= Marshal.FreeHGlobal;
        foreach (ref SCARD_READER_STATE state in states)
        {
            if (state.ReaderName == IntPtr.Zero)
                continue;
            freeReaderName(state.ReaderName);
            state.ReaderName = IntPtr.Zero;
        }
    }
}