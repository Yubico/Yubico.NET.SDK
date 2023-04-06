// Copyright 2023 Yubico AB
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
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Fido2.PinProtocols;
using Yubico.YubiKey.TestUtilities;
using Xunit;

namespace Yubico.YubiKey.Fido2
{
    public class BioEnrollmentCommandTests : NeedPinToken
    {
        public BioEnrollmentCommandTests()
            : base(YubiKeyApplication.Fido2, StandardTestDevice.Bio, null)
        {
        }

        [Fact]
        public void GetModalityCommand_Succeeds()
        {
            var cmd = new GetBioModalityCommand();
            GetBioModalityResponse rsp = Connection.SendCommand(cmd);
            int modality = rsp.GetData();

            Assert.Equal(1, modality);
        }

        [Fact]
        public void GetSensorInfoCommand_Succeeds()
        {
            var cmd = new GetFingerprintSensorInfoCommand();
            GetFingerprintSensorInfoResponse rsp = Connection.SendCommand(cmd);
            FingerprintSensorInfo sensorInfo = rsp.GetData();

            Assert.Equal(1, sensorInfo.FingerprintKind);
            Assert.Equal(16, sensorInfo.MaxCaptureCount);
            Assert.Equal(15, sensorInfo.MaxFriendlyNameBytes);
        }

        [Fact]
        public void BioEnrollBegin_Succeeds()
        {
            var protocol = new PinUvAuthProtocolTwo();
            bool isValid = GetPinToken(
                protocol, PinUvAuthTokenPermissions.BioEnrollment, out byte[] pinToken);
            Assert.True(isValid);

            var cmd = new BioEnrollBeginCommand(5000, pinToken, protocol);
            BioEnrollBeginResponse rsp = Connection.SendCommand(cmd);
            BioEnrollUpdateStatus enrollStatus = rsp.GetData();

            Assert.Equal(BioEnrollSampleStatus.FpGood, enrollStatus.LastEnrollSampleStatus);
        }

        [Fact]
        public void BioEnrollNextSample_Succeeds()
        {
            var protocol = new PinUvAuthProtocolTwo();
            bool isValid = GetPinToken(
                protocol, PinUvAuthTokenPermissions.BioEnrollment, out byte[] pinToken);
            Assert.True(isValid);

            var cmd = new BioEnrollBeginCommand(null, pinToken, protocol);
            BioEnrollBeginResponse rsp = Connection.SendCommand(cmd);
            BioEnrollUpdateStatus enrollStatus = rsp.GetData();
            Assert.Equal(BioEnrollSampleStatus.FpGood, enrollStatus.LastEnrollSampleStatus);

            var nextCmd = new BioEnrollNextSampleCommand(enrollStatus.TemplateId, null, pinToken, protocol);
            do
            {
                BioEnrollNextSampleResponse nextRsp = Connection.SendCommand(nextCmd);
                enrollStatus = nextRsp.GetData();
            } while (enrollStatus.RemainingSampleCount > 0);

            Assert.Equal(0, enrollStatus.RemainingSampleCount);
        }

        [Fact]
        public void BioEnrollCancel_Succeeds()
        {
            var protocol = new PinUvAuthProtocolTwo();
            bool isValid = GetPinToken(
                protocol, PinUvAuthTokenPermissions.BioEnrollment, out byte[] pinToken);
            Assert.True(isValid);

            var cmd = new BioEnrollBeginCommand(null, pinToken, protocol);
            BioEnrollBeginResponse rsp = Connection.SendCommand(cmd);
            BioEnrollUpdateStatus enrollStatus = rsp.GetData();
            Assert.Equal(BioEnrollSampleStatus.FpGood, enrollStatus.LastEnrollSampleStatus);

            var nextCmd = new BioEnrollNextSampleCommand(enrollStatus.TemplateId, null, pinToken, protocol);
            
            BioEnrollNextSampleResponse nextRsp = Connection.SendCommand(nextCmd);
            enrollStatus = nextRsp.GetData();
 
            Assert.True(enrollStatus.RemainingSampleCount != 0);
 
            var cancelCmd = new BioEnrollCancelCommand();
            Fido2Response cancelRsp = Connection.SendCommand(cancelCmd);

            Assert.Equal(ResponseStatus.Success, cancelRsp.Status);
        }
    }
}
