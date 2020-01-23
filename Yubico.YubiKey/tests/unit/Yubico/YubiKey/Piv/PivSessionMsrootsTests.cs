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
using System.IO;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.TestUtilities;
using Xunit;

namespace Yubico.YubiKey.Piv
{
    public class PivSessionMsrootsTests
    {
        [Fact]
        public void Write_TooMuchData_ThrowsOutOfRangeException()
        {
            var yubiKey = new HollowYubiKeyDevice();
            byte[] inputData = new byte[16000];

            using (var pivSession = new PivSession(yubiKey))
            {
                _ = Assert.Throws<ArgumentOutOfRangeException>(() => pivSession.WriteMsroots(inputData));
            }
        }

        [Fact]
        public void Write_NoKeyCollector_ThrowsInvalidOpException()
        {
            var yubiKey = new HollowYubiKeyDevice();
            byte[] inputData = new byte[100];

            using (var pivSession = new PivSession(yubiKey))
            {
                _ = Assert.Throws<InvalidOperationException>(() => pivSession.WriteMsroots(inputData));
            }
        }

        [Fact]
        public void Write_KeyCollectorFalse_ThrowsCanceledException()
        {
            var yubiKey = new HollowYubiKeyDevice();
            byte[] inputData = new byte[100];

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = ReturnFalseKeyCollectorDelegate;
                _ = Assert.Throws<OperationCanceledException>(() => pivSession.WriteMsroots(inputData));
            }
        }

        [Fact]
        public void WriteStream_TooMuchData_ThrowsOutOfRangeException()
        {
            var yubiKey = new HollowYubiKeyDevice();
            byte[] inputData = new byte[16000];
            var memStream = new MemoryStream(inputData);

            using (var pivSession = new PivSession(yubiKey))
            {
                _ = Assert.Throws<ArgumentOutOfRangeException>(() => pivSession.WriteMsrootsStream(memStream));
            }
        }

        [Fact]
        public void WriteStream_NoKeyCollector_ThrowsInvalidOpException()
        {
            var yubiKey = new HollowYubiKeyDevice();
            byte[] inputData = new byte[100];
            var memStream = new MemoryStream(inputData);

            using (var pivSession = new PivSession(yubiKey))
            {
                _ = Assert.Throws<InvalidOperationException>(() => pivSession.WriteMsrootsStream(memStream));
            }
        }

        [Fact]
        public void WriteStream_KeyCollectorFalse_ThrowsCanceledException()
        {
            var yubiKey = new HollowYubiKeyDevice();
            byte[] inputData = new byte[100];
            var memStream = new MemoryStream(inputData);

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = ReturnFalseKeyCollectorDelegate;
                _ = Assert.Throws<OperationCanceledException>(() => pivSession.WriteMsrootsStream(memStream));
            }
        }

        [Fact]
        public void Delete_NoKeyCollector_ThrowsInvalidOpException()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                _ = Assert.Throws<InvalidOperationException>(() => pivSession.DeleteMsroots());
            }
        }

        [Fact]
        public void Delete_KeyCollectorFalse_ThrowsCanceledException()
        {
            var yubiKey = new HollowYubiKeyDevice();

            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.KeyCollector = ReturnFalseKeyCollectorDelegate;
                _ = Assert.Throws<OperationCanceledException>(() => pivSession.DeleteMsroots());
            }
        }

        public static bool ReturnFalseKeyCollectorDelegate(KeyEntryData keyEntryData)
        {
            return false;
        }
    }
}
