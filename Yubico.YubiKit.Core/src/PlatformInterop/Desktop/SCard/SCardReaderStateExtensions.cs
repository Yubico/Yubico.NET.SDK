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
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Core.PlatformInterop.Desktop.SCard;

internal static unsafe class SCardReaderStateExtensions
{
    private const uint SequenceMask = 0xFFFF_0000;
    private const uint StateMask = 0x0000_FFFF;

    public static SCARD_STATE GetCurrentState(this in SCARD_READER_STATE state) =>
        (SCARD_STATE)(state.CurrentState & StateMask);

    public static SCARD_STATE GetEventState(this in SCARD_READER_STATE state) =>
        (SCARD_STATE)(state.EventState & StateMask);

    public static string GetReaderName(this in SCARD_READER_STATE state)
    {
        if (state.ReaderName == IntPtr.Zero) return string.Empty;

        return Marshal.PtrToStringUTF8(state.ReaderName) ?? string.Empty;
    }

    public static AnswerToReset GetAtr(this in SCARD_READER_STATE state)
    {
        var atrBytes = new byte[state.AtrLength];
        fixed (byte* src = state.AnswerToReset)
        {
            Marshal.Copy((IntPtr)src, atrBytes, 0, (int)state.AtrLength);
        }

        return new AnswerToReset(atrBytes);
    }

    public static bool IsCardPresent(this in SCARD_READER_STATE state) =>
        (state.GetEventState() & SCARD_STATE.PRESENT) != 0;

    public static void AcknowledgeChanges(this ref SCARD_READER_STATE state)
    {
        state.CurrentState = state.EventState & ~(uint)SCARD_STATE.CHANGED;
        state.EventState = 0;
    }

    public static int GetCurrentSequence(this in SCARD_READER_STATE state) =>
        (int)((state.CurrentState & SequenceMask) >> 16);

    public static int GetEventSequence(this in SCARD_READER_STATE state) =>
        (int)((state.EventState & SequenceMask) >> 16);
}