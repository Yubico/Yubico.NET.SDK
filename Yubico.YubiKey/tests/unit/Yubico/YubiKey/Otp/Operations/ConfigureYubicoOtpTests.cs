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
using Xunit;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Otp.Operations
{
    public class ConfigureYubicoOtpTests : IDisposable
    {
        private readonly HollowOtpSession _session;
        private readonly ConfigureYubicoOtp _op;
        private readonly static byte[] _validKey = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
        private bool _disposedValue;

        public ConfigureYubicoOtpTests()
        {
            _session = new HollowOtpSession(FirmwareVersion.V5_4_3);
            _op = _session.ConfigureYubicoOtp(Slot.ShortPress);
        }

        [Fact]
        public void TestNoSlot()
        {
            ConfigureYubicoOtp op = _session.ConfigureYubicoOtp(Slot.None);
            InvalidOperationException ex =
                Assert.Throws<InvalidOperationException>(() => op.Execute());
            Assert.Equal(ExceptionMessages.SlotNotSet, ex.Message);
        }

        [Fact]
        public void TestGeneratedAndSpecifiedKey()
        {
          _ = _op.GenerateKey(new byte[ConfigureYubicoOtp.KeySize]);
            InvalidOperationException ex =
                Assert.Throws<InvalidOperationException>(() => _op.UseKey(_validKey));
            Assert.Equal(ExceptionMessages.CantSpecifyKeyAndGenerate, ex.Message);
        }

        [Fact]
        public void TestSpecifiedAndGeneratedKey()
        {
            _ = _op.UseKey(_validKey);
            InvalidOperationException ex =
                Assert.Throws<InvalidOperationException>(
                    () => _op.GenerateKey(new byte[ConfigureYubicoOtp.KeySize]));
            Assert.Equal(ExceptionMessages.CantSpecifyKeyAndGenerate, ex.Message);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _session.Dispose();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
