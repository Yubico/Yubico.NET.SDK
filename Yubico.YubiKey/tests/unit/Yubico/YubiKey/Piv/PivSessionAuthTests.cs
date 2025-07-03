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
using Xunit;

namespace Yubico.YubiKey.Piv
{
    public class PivSessionAuthTests : PivSessionUnitTestBase
    {
        [Fact]
        public void PivSession_NullYubiKey_ThrowsArgumentNullException()
        {
            _ = Assert.Throws<ArgumentNullException>(() => new PivSession(null!));
        }

        [Fact]
        public void TryAuthenticateManagementKey_NullKeyCollector_ThrowsInvalidOperationException()
        {
            PivSessionMock.KeyCollector = null;
            _ = Assert.Throws<InvalidOperationException>(() => PivSessionMock.TryAuthenticateManagementKey());
        }

        [Fact]
        public void AuthenticateManagementKey_NullKeyCollector_ThrowsInvalidOperationException()
        {
            PivSessionMock.KeyCollector = null;
            _ = Assert.Throws<InvalidOperationException>(() => PivSessionMock.AuthenticateManagementKey());
        }

        [Fact]
        public void TryAuthenticateManagementKey_KeyCollectorFalse_ReturnsFalse()
        {
            PivSessionMock.KeyCollector = ReturnFalseKeyCollectorDelegate;
            var isValid = PivSessionMock.TryAuthenticateManagementKey();
            Assert.False(isValid);
        }

        [Fact]
        public void AuthenticateManagementKey_KeyCollectorFalse_ThrowsOperationCanceledException()
        {
            PivSessionMock.KeyCollector = ReturnFalseKeyCollectorDelegate;
            _ = Assert.Throws<OperationCanceledException>(() => PivSessionMock.AuthenticateManagementKey());
        }

        [Fact]
        public void TryChangeManagementKey_NullKeyCollector_ThrowsInvalidOperationException()
        {
            PivSessionMock.KeyCollector = null;
            _ = Assert.Throws<InvalidOperationException>(() => PivSessionMock.TryChangeManagementKey());
        }

        [Fact]
        public void ChangeManagementKey_NullKeyCollector_ThrowsInvalidOperationException()
        {
            PivSessionMock.KeyCollector = null;
            _ = Assert.Throws<InvalidOperationException>(() => PivSessionMock.ChangeManagementKey());
        }

        [Fact]
        public void TryChangeManagementKey_KeyCollectorFalse_ReturnsFalse()
        {
            PivSessionMock.KeyCollector = ReturnFalseKeyCollectorDelegate;
            var isValid = PivSessionMock.TryChangeManagementKey();
            Assert.False(isValid);
        }

        [Fact]
        public void ChangeManagementKey_KeyCollectorFalse_ThrowsOperationCanceledException()
        {
            PivSessionMock.KeyCollector = ReturnFalseKeyCollectorDelegate;
            _ = Assert.Throws<OperationCanceledException>(() => PivSessionMock.ChangeManagementKey());
        }

        [Fact]
        public void TryVerifyPin_NullKeyCollector_ThrowsInvalidOperationException()
        {
            PivSessionMock.KeyCollector = null;
            _ = Assert.Throws<InvalidOperationException>(() => PivSessionMock.TryVerifyPin());
        }

        [Fact]
        public void VerifyPin_NullKeyCollector_ThrowsInvalidOperationException()
        {
            PivSessionMock.KeyCollector = null;
            _ = Assert.Throws<InvalidOperationException>(() => PivSessionMock.VerifyPin());
        }

        [Fact]
        public void TryVerifyPin_KeyCollectorFalse_ReturnsFalse()
        {
            PivSessionMock.KeyCollector = ReturnFalseKeyCollectorDelegate;
            var isValid = PivSessionMock.TryVerifyPin();
            Assert.False(isValid);
        }

        [Fact]
        public void VerifyPin_KeyCollectorFalse_ThrowsOperationCanceledException()
        {
            PivSessionMock.KeyCollector = ReturnFalseKeyCollectorDelegate;
            _ = Assert.Throws<OperationCanceledException>(() => PivSessionMock.VerifyPin());
        }

        [Fact]
        public void TryChangePin_NullKeyCollector_ThrowsInvalidOperationException()
        {
            PivSessionMock.KeyCollector = null;
            _ = Assert.Throws<InvalidOperationException>(() => PivSessionMock.TryChangePin());
        }

        [Fact]
        public void ChangePin_NullKeyCollector_ThrowsInvalidOperationException()
        {
            PivSessionMock.KeyCollector = null;
            _ = Assert.Throws<InvalidOperationException>(() => PivSessionMock.ChangePin());
        }

        [Fact]
        public void TryChangePin_KeyCollectorFalse_ReturnsFalse()
        {
            PivSessionMock.KeyCollector = ReturnFalseKeyCollectorDelegate;
            var isValid = PivSessionMock.TryChangePin();
            Assert.False(isValid);
        }

        [Fact]
        public void ChangePin_KeyCollectorFalse_ThrowsOperationCanceledException()
        {
            PivSessionMock.KeyCollector = ReturnFalseKeyCollectorDelegate;
            _ = Assert.Throws<OperationCanceledException>(() => PivSessionMock.ChangePin());
        }

        [Fact]
        public void TryResetPin_NullKeyCollector_ThrowsInvalidOperationException()
        {
            PivSessionMock.KeyCollector = null;
            _ = Assert.Throws<InvalidOperationException>(() => PivSessionMock.TryResetPin());
        }

        [Fact]
        public void ResetPin_NullKeyCollector_ThrowsInvalidOperationException()
        {
            PivSessionMock.KeyCollector = null;
            _ = Assert.Throws<InvalidOperationException>(() => PivSessionMock.ResetPin());
        }

        [Fact]
        public void TryResetPin_KeyCollectorFalse_ReturnsFalse()
        {
            PivSessionMock.KeyCollector = ReturnFalseKeyCollectorDelegate;
            var isValid = PivSessionMock.TryResetPin();
            Assert.False(isValid);
        }

        [Fact]
        public void ResetPin_KeyCollectorFalse_ThrowsOperationCanceledException()
        {
            PivSessionMock.KeyCollector = ReturnFalseKeyCollectorDelegate;
            _ = Assert.Throws<OperationCanceledException>(() => PivSessionMock.ResetPin());
        }

        [Fact]
        public void TryChangePuk_NullKeyCollector_Throws()
        {
            PivSessionMock.KeyCollector = null;
            _ = Assert.Throws<InvalidOperationException>(() => PivSessionMock.TryChangePuk());
        }

        [Fact]
        public void ChangePuk_NullKeyCollector_ThrowsInvalidOperationException()
        {
            PivSessionMock.KeyCollector = null;
            _ = Assert.Throws<InvalidOperationException>(() => PivSessionMock.ChangePuk());
        }

        [Fact]
        public void TryChangePuk_KeyCollectorFalse_ReturnsFalse()
        {
            PivSessionMock.KeyCollector = ReturnFalseKeyCollectorDelegate;
            var isValid = PivSessionMock.TryChangePuk();
            Assert.False(isValid);
        }

        [Fact]
        public void ChangePuk_KeyCollectorFalse_ThrowsOperationCanceledException()
        {
            PivSessionMock.KeyCollector = ReturnFalseKeyCollectorDelegate;
            _ = Assert.Throws<OperationCanceledException>(() => PivSessionMock.ChangePuk());
        }

        [Theory]
        [InlineData(0, 5)]
        [InlineData(6, 0)]
        [InlineData(0, 0)]
        public void RetryCount_Zero_ThrowsArgumentException(byte pinRetry, byte pukRetry)
        {
            _ = Assert.Throws<ArgumentException>(() => PivSessionMock.ChangePinAndPukRetryCounts(pinRetry, pukRetry));
        }

        private static bool ReturnFalseKeyCollectorDelegate(KeyEntryData _) => false;
    }
}
