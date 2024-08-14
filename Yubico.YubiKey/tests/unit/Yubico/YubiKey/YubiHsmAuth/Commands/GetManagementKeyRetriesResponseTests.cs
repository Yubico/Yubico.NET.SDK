// Copyright 2022 Yubico AB
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
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.YubiHsmAuth.Commands
{
    public class GetManagementKeyRetriesResponseTests
    {
        [Fact]
        public void GetData_ResponseStatusFailed_ThrowsInvalidOperationException()
        {
            var apdu = new ResponseApdu(new byte[0], SWConstants.AuthenticationMethodBlocked);

            var response =
                new GetManagementKeyRetriesResponse(apdu);

            Action action = () => response.GetData();

            _ = Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void GetData_ResponseStatusFailed_ExceptionMessageMatchesStatusMessage()
        {
            var apdu = new ResponseApdu(new byte[0], SWConstants.AuthenticationMethodBlocked);

            var response =
                new GetManagementKeyRetriesResponse(apdu);

            try
            {
                _ = response.GetData();
            }
            catch (InvalidOperationException ex)
            {
                Assert.Equal(response.StatusMessage, ex.Message);
            }
        }

        [Fact]
        public void GetData_SuccessApdu6Retries_Returns6Retries()
        {
            byte[] dataWithoutSw = new byte[] { 6 };
            var apdu = new ResponseApdu(dataWithoutSw, SWConstants.Success);

            var response =
                new GetManagementKeyRetriesResponse(apdu);

            int retries = response.GetData();

            Assert.Equal(6, retries);
        }
    }
}
