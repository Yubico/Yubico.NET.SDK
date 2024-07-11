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
using System.Linq;
using Xunit;

namespace Yubico.YubiKey.Cryptography
{
    public class KeyEntryDataTests
    {
        [Fact]
        public void Constructor_RetriesNull()
        {
            var entryData = new KeyEntryData();

            Assert.Null(entryData.RetriesRemaining);
        }

        [Fact]
        public void Constructor_RequestIsRelease()
        {
            var entryData = new KeyEntryData();

            Assert.Equal(KeyEntryRequest.Release, entryData.Request);
        }

        [Fact]
        public void Constructor_IsRetryIsFalse()
        {
            var entryData = new KeyEntryData();

            Assert.False(entryData.IsRetry);
        }

        [Fact]
        public void Constructor_ClearSucceeds()
        {
            var entryData = new KeyEntryData();
            entryData.Clear();
            ReadOnlyMemory<byte> value = entryData.GetCurrentValue();

            Assert.Equal(0, value.Length);
        }

        [Fact]
        public void SubmitValue_Succeeds()
        {
            var entryData = new KeyEntryData();

            byte[] dataToSubmit = new byte[]
            {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36
            };
            entryData.SubmitValue(dataToSubmit);

            ReadOnlyMemory<byte> value = entryData.GetCurrentValue();
            byte[] getValue = value.ToArray();

            bool compareResult = getValue.SequenceEqual(dataToSubmit);

            Assert.True(compareResult);
        }

        [Fact]
        public void Submit_Clear_Succeeds()
        {
            var entryData = new KeyEntryData();

            byte[] dataToSubmit = new byte[]
            {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47
            };
            entryData.SubmitValue(dataToSubmit);
            entryData.Clear();

            ReadOnlyMemory<byte> value = entryData.GetCurrentValue();
            byte[] getValue = value.ToArray();

            byte[] expected = new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };
            bool compareResult = getValue.SequenceEqual(expected);

            Assert.True(compareResult);
        }

        [Fact]
        public void SubmitValues_Succeeds()
        {
            var entryData = new KeyEntryData();

            byte[] currentValue = new byte[]
            {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36
            };
            byte[] newValue = new byte[]
            {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
            };
            entryData.SubmitValues(currentValue, newValue);

            ReadOnlyMemory<byte> value = entryData.GetCurrentValue();
            byte[] getValue = value.ToArray();
            bool compareResult = getValue.SequenceEqual(currentValue);

            Assert.True(compareResult);

            value = entryData.GetNewValue();
            getValue = value.ToArray();
            compareResult = getValue.SequenceEqual(newValue);

            Assert.True(compareResult);
        }

        [Fact]
        public void SetRetries_Succeeds()
        {
            var entryData = new KeyEntryData
            {
                RetriesRemaining = 5
            };

            _ = Assert.NotNull(entryData.RetriesRemaining);
        }
    }
}
