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

namespace Yubico.YubiKit.Core.Core.PlatformInterop.Desktop.SCard;

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

    public static SCARD_READER_STATE[] CreateMany(IEnumerable<string> readerNames) =>
    [
        .. readerNames.Select(name => new SCARD_READER_STATE(Marshal.StringToHGlobalAnsi(name)))
    ];
}