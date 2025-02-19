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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Yubico.Core.Iso7816;

namespace Yubico.PlatformInterop
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    // Justification: Fields are read/write via interop. Readonly might not have any effect there, but it may give
    // maintainers a falls impression about the true nature of these fields.
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
    [SuppressMessage("Style", "IDE0044:Add readonly modifier")]
    internal struct SCARD_READER_STATE
    {
#pragma warning disable IDE0032 // Use auto property
        [MarshalAs(UnmanagedType.LPStr)]
        private string _readerName;
#pragma warning restore IDE0032 // Use auto property
        private IntPtr _userData;
        private uint _currentState;
        private uint _eventState;
        private uint _atrLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]
        private byte[] _answerToReset;

        private const uint SequenceMask = 0xFFFF_0000;
        private const uint StateMask = 0x0000_FFFF;

        public string ReaderName
        {
            get => _readerName;
            set => _readerName = value;
        }

        public SCARD_STATE CurrentState => (SCARD_STATE)(_currentState & StateMask);
        public SCARD_STATE EventState => (SCARD_STATE)(_eventState & StateMask);
        public int CurrentSequence => (int)(_currentState & SequenceMask) >> 16;
        public int EventSequence => (int)(_eventState & SequenceMask) >> 16;
        public AnswerToReset Atr => new AnswerToReset(_answerToReset.AsSpan(0, (int)_atrLength));

        public static SCARD_READER_STATE[] CreateFromReaderNames(IEnumerable<string> readerNames) =>
            readerNames.Select(r => new SCARD_READER_STATE { ReaderName = r }).ToArray();

        public void AcknowledgeChanges()
        {
            _currentState = _eventState & ~(uint)SCARD_STATE.CHANGED;
            _eventState = 0;
        }

        public override string ToString() =>
            $"{ReaderName}: [{CurrentSequence} : {CurrentState}] => [{EventSequence} : {EventState}]";
    }
}
