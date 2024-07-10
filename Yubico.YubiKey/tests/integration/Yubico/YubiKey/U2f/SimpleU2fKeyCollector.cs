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

namespace Yubico.YubiKey.U2f
{
    // This KeyCollector class can be used to provide the KeyCollector delegate
    // to the PivSession class (and others in the future).
    // It is called Simple because it returns fixed, default values. The "39" is
    // there because it is possible to return the fixed values with one byte
    // changed to 0x39.
    public class SimpleU2fKeyCollector
    {
        private readonly ReadOnlyMemory<byte> _firstPin = new ReadOnlyMemory<byte>(new byte[]
        {
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36
        });

        private readonly ReadOnlyMemory<byte> _secondPin = new ReadOnlyMemory<byte>(new byte[]
        {
            0x41, 0x42, 0x43, 0x44, 0x45, 0x46
        });

        // If the caller sets the input arg to true, then the YubiKey's U2F PIN
        // is alreadys set. So the current PIN is "123456" and the new PIN is
        // "ABCDEF"
        // Change, return the old and new, then set KeyFlag to the opposite of
        // what it currently is.
        // For false, then this returns old and new, but does nothing to KeyFlag.
        // If there is no arg, that's false.
        public SimpleU2fKeyCollector(bool isU2fPinSet)
        {
            if (isU2fPinSet)
            {
                CurrentPin = _firstPin;
                NewPin = _secondPin;
            }
            else
            {
                CurrentPin = ReadOnlyMemory<byte>.Empty;
                NewPin = _firstPin;
            }
        }

        public ReadOnlyMemory<byte> CurrentPin { get; private set; }

        public ReadOnlyMemory<byte> NewPin { get; private set; }

        public bool SimpleU2fKeyCollectorDelegate(KeyEntryData keyEntryData)
        {
            if (keyEntryData is null)
            {
                return false;
            }

            if (keyEntryData.IsRetry)
            {
                return false;
            }

            switch (keyEntryData.Request)
            {
                default:
                    return false;

                case KeyEntryRequest.Release:
                    return true;

                case KeyEntryRequest.TouchRequest:
                    return true;

                case KeyEntryRequest.SetU2fPin:
                    keyEntryData.SubmitValue(NewPin.Span);
                    CurrentPin = _firstPin;
                    NewPin = _secondPin;
                    break;

                case KeyEntryRequest.VerifyU2fPin:
                    keyEntryData.SubmitValue(CurrentPin.Span);
                    break;

                case KeyEntryRequest.ChangeU2fPin:
                    keyEntryData.SubmitValues(CurrentPin.Span, NewPin.Span);
                    var temp = CurrentPin;
                    CurrentPin = NewPin;
                    NewPin = temp;
                    break;
            }

            return true;
        }
    }
}
