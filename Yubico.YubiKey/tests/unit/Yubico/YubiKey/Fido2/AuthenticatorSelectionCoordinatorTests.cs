// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using NSubstitute;
using Xunit;

namespace Yubico.YubiKey.Sample.Fido2SampleCode
{
    public class AuthenticatorSelectionCoordinatorTests
    {
        [Fact]
        public void TrySelectWinner_FirstSuccessWinsOnce()
        {
            var coordinator = new AuthenticatorSelectionCoordinator();
            IYubiKeyDevice first = Device();
            IYubiKeyDevice second = Device();

            Assert.True(coordinator.TrySelectWinner(first));
            Assert.False(coordinator.TrySelectWinner(second));

            Assert.True(coordinator.HasWinner);
            Assert.Same(first, coordinator.SelectedDevice);
        }

        [Fact]
        public void TrySelectWinner_LoserDelegatesInvokedOnce()
        {
            var coordinator = new AuthenticatorSelectionCoordinator();
            IYubiKeyDevice winner = Device();
            IYubiKeyDevice loser = Device();
            int winnerCancelCount = 0;
            int loserCancelCount = 0;

            coordinator.CaptureCancel(winner, () => winnerCancelCount++);
            coordinator.CaptureCancel(loser, () => loserCancelCount++);

            _ = coordinator.TrySelectWinner(winner);
            coordinator.CancelLosers();

            Assert.Equal(0, winnerCancelCount);
            Assert.Equal(1, loserCancelCount);
        }

        [Fact]
        public void CaptureCancel_LateLoserDelegateImmediatelyCanceled()
        {
            var coordinator = new AuthenticatorSelectionCoordinator();
            IYubiKeyDevice winner = Device();
            IYubiKeyDevice lateLoser = Device();
            int lateCancelCount = 0;

            _ = coordinator.TrySelectWinner(winner);
            coordinator.CaptureCancel(lateLoser, () => lateCancelCount++);

            Assert.Equal(1, lateCancelCount);
        }

        [Fact]
        public void CancelLosers_BeforeWinnerDoesNotCancel()
        {
            var coordinator = new AuthenticatorSelectionCoordinator();
            IYubiKeyDevice device = Device();
            int cancelCount = 0;

            coordinator.CaptureCancel(device, () => cancelCount++);
            coordinator.CancelLosers();

            Assert.False(coordinator.HasWinner);
            Assert.Null(coordinator.SelectedDevice);
            Assert.Equal(0, cancelCount);
        }

        [Fact]
        public void IsExpectedLoserCancellation_LoserCancellationExpectedAfterWinner()
        {
            var coordinator = new AuthenticatorSelectionCoordinator();
            IYubiKeyDevice winner = Device();
            IYubiKeyDevice loser = Device();

            _ = coordinator.TrySelectWinner(winner);

            Assert.True(coordinator.IsExpectedLoserCancellation(loser));
            Assert.False(coordinator.IsExpectedLoserCancellation(winner));
        }

        private static IYubiKeyDevice Device() => Substitute.For<IYubiKeyDevice>();
    }
}
