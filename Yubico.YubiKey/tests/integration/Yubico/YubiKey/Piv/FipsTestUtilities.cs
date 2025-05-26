// Copyright 2024 Yubico AB
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

using Xunit;
using Yubico.YubiKey.Scp;

namespace Yubico.YubiKey.Piv
{
    public static class FipsTestUtilities
    {
        public static readonly byte[] FipsPin = {
            0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88
        };

        public static readonly byte[] FipsPuk = {
            0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88
        };

        public static readonly byte[] FipsManagementKey = {
            0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
            0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x12,
            0x23, 0x34, 0x45, 0x56, 0x67, 0x78, 0x89, 0x9A
        };

        public static void SetFipsApprovedCredentials(PivSession session)
        {
            session.ResetApplication();
            session.KeyCollector = new Simple39KeyCollector().Simple39KeyCollectorDelegate;

            session.TryChangePin(PivSessionIntegrationTestBase.DefaultPin, FipsPin, out _);
            session.TryChangePuk(PivSessionIntegrationTestBase.DefaultPuk, FipsPuk, out _);
            session.TryChangeManagementKey(PivSessionIntegrationTestBase.DefaultManagementKey, FipsManagementKey, PivTouchPolicy.Always);
            Assert.True(session.TryVerifyPin(FipsPin, out _));
        }

        public static void SetFipsApprovedCredentials(
            IYubiKeyDevice device,
            ScpKeyParameters parameters)
        {
            using var session = new PivSession(device, parameters);
            SetFipsApprovedCredentials(session);
        }
    }
}
