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

using Xunit;
using Yubico.YubiKey.Fido2.PinProtocols;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2.Commands;

[Trait(TraitTypes.Category, TestCategories.RequiresBio)]
public class BioEnrollmentCommandTests : NeedPinToken
{
    public BioEnrollmentCommandTests()
        : base(YubiKeyApplication.Fido2, StandardTestDevice.Fw5Bio)
    {
    }

    [SkippableFact(typeof(DeviceNotFoundException))]
    public void GetModalityCommand_Succeeds()
    {
        var cmd = new GetBioModalityCommand();
        var rsp = Connection.SendCommand(cmd);
        var modality = rsp.GetData();

        Assert.Equal(1, modality);
    }

    [SkippableFact(typeof(DeviceNotFoundException))]
    public void GetSensorInfoCommand_Succeeds()
    {
        var cmd = new GetFingerprintSensorInfoCommand();
        var rsp = Connection.SendCommand(cmd);
        var sensorInfo = rsp.GetData();

        Assert.Equal(1, sensorInfo.FingerprintKind);
        Assert.Equal(16, sensorInfo.MaxCaptureCount);
        Assert.Equal(15, sensorInfo.MaxFriendlyNameBytes);
    }

    [SkippableFact(typeof(DeviceNotFoundException))]
    public void BioEnrollBegin_Succeeds()
    {
        var protocol = new PinUvAuthProtocolTwo();
        var isValid = GetPinToken(
            protocol, PinUvAuthTokenPermissions.BioEnrollment, out var pinToken);
        Assert.True(isValid);

        var cmd = new BioEnrollBeginCommand(5000, pinToken, protocol);
        var rsp = Connection.SendCommand(cmd);
        var enrollResult = rsp.GetData();

        Assert.Equal(BioEnrollSampleStatus.FpGood, enrollResult.LastEnrollSampleStatus);
    }

    [SkippableFact(typeof(DeviceNotFoundException))]
    public void BioEnrollNextSample_Succeeds()
    {
        var protocol = new PinUvAuthProtocolTwo();
        var isValid = GetPinToken(
            protocol, PinUvAuthTokenPermissions.BioEnrollment, out var pinToken);
        Assert.True(isValid);

        var cmd = new BioEnrollBeginCommand(null, pinToken, protocol);
        var rsp = Connection.SendCommand(cmd);
        var enrollResult = rsp.GetData();
        Assert.Equal(BioEnrollSampleStatus.FpGood, enrollResult.LastEnrollSampleStatus);

        var nextCmd = new BioEnrollNextSampleCommand(enrollResult.TemplateId, null, pinToken, protocol);
        var totalCount = 1;
        do
        {
            var nextRsp = Connection.SendCommand(nextCmd);
            enrollResult = nextRsp.GetData();
            totalCount++;
        } while (enrollResult.RemainingSampleCount > 0);

        Assert.Equal(0, enrollResult.RemainingSampleCount);
    }

    [SkippableFact(typeof(DeviceNotFoundException))]
    public void BioEnrollCancel_Succeeds()
    {
        var protocol = new PinUvAuthProtocolTwo();
        var isValid = GetPinToken(
            protocol, PinUvAuthTokenPermissions.BioEnrollment, out var pinToken);
        Assert.True(isValid);

        var cmd = new BioEnrollBeginCommand(null, pinToken, protocol);
        var rsp = Connection.SendCommand(cmd);
        var enrollResult = rsp.GetData();
        Assert.Equal(BioEnrollSampleStatus.FpGood, enrollResult.LastEnrollSampleStatus);

        var nextCmd = new BioEnrollNextSampleCommand(enrollResult.TemplateId, null, pinToken, protocol);

        var nextRsp = Connection.SendCommand(nextCmd);
        enrollResult = nextRsp.GetData();

        Assert.True(enrollResult.RemainingSampleCount != 0);

        var cancelCmd = new BioEnrollCancelCommand();
        var cancelRsp = Connection.SendCommand(cancelCmd);

        Assert.Equal(ResponseStatus.Success, cancelRsp.Status);
    }

    [SkippableFact(typeof(DeviceNotFoundException))]
    public void EnumerateEnrollmentsCommand_Succeeds()
    {
        var protocol = new PinUvAuthProtocolTwo();
        var isValid = GetPinToken(
            protocol, PinUvAuthTokenPermissions.BioEnrollment, out var pinToken);
        Assert.True(isValid);

        var cmd = new BioEnrollEnumerateCommand(pinToken, protocol);
        var rsp = Connection.SendCommand(cmd);

        Assert.Equal(ResponseStatus.Success, rsp.Status);

        var templateInfos = rsp.GetData();
        _ = Assert.Single(templateInfos);
    }

    [SkippableFact(typeof(DeviceNotFoundException))]
    public void FriendlyNameCommand_Succeeds()
    {
        var protocol = new PinUvAuthProtocolTwo();
        var isValid = GetPinToken(
            protocol, PinUvAuthTokenPermissions.BioEnrollment, out var pinToken);
        Assert.True(isValid);

        var cmd = new BioEnrollEnumerateCommand(pinToken, protocol);
        var rsp = Connection.SendCommand(cmd);

        Assert.Equal(ResponseStatus.Success, rsp.Status);
        var templateInfos = rsp.GetData();

        var nameCmd = new BioEnrollSetFriendlyNameCommand(
            templateInfos[0].TemplateId, "NameTwo", pinToken, protocol);
        var nameRsp = Connection.SendCommand(nameCmd);

        Assert.Equal(ResponseStatus.Success, nameRsp.Status);
    }

    [SkippableFact(typeof(DeviceNotFoundException))]
    public void DeleteCommand_Succeeds()
    {
        var protocol = new PinUvAuthProtocolTwo();
        var isValid = GetPinToken(
            protocol, PinUvAuthTokenPermissions.BioEnrollment, out var pinToken);
        Assert.True(isValid);

        var cmd = new BioEnrollEnumerateCommand(pinToken, protocol);
        var rsp = Connection.SendCommand(cmd);

        Assert.Equal(ResponseStatus.Success, rsp.Status);
        var templateInfos = rsp.GetData();

        var removeCmd = new BioEnrollRemoveCommand(
            templateInfos[0].TemplateId, pinToken, protocol);
        var removeRsp = Connection.SendCommand(removeCmd);

        Assert.Equal(ResponseStatus.Success, removeRsp.Status);
    }
}
