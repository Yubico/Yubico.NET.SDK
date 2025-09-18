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
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands;

public class PutDataCommandTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ClassType_DerivedFromPivCommand_IsTrue(
        int constructorToUse)
    {
        var data = PivCommandResponseTestData.PutDataEncoding(PivDataTag.IrisImages, true);
        var command = GetPutDataCommandObject(constructorToUse, PivDataTag.IrisImages, 0, data);

        Assert.True(command is IYubiKeyCommand<PutDataResponse>);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void Constructor_IntTag_ValidObject(
        int constructorToUse)
    {
        var data = new byte[] { 0x53, 0x03, 0x31, 0x32, 0x33 };
        var command = GetPutDataCommandObject(constructorToUse, PivDataTag.Unknown, 0x005fff0A, data);

        Assert.True(command is IYubiKeyCommand<PutDataResponse>);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Constructor_Application_Piv(
        int constructorToUse)
    {
        var data = PivCommandResponseTestData.PutDataEncoding(PivDataTag.Chuid, true);
        var command = GetPutDataCommandObject(constructorToUse, PivDataTag.Chuid, 0, data);

        var application = command.Application;

        Assert.Equal(YubiKeyApplication.Piv, application);
    }

    [Fact]
    public void SetProperties_Old_MatchesNew()
    {
        var command = new PutDataCommand
        {
            DataTag = (int)PivDataTag.Capability
        };

#pragma warning disable CS0618 // Testing an obsolete feature
        Assert.Equal(PivDataTag.Capability, command.Tag);
#pragma warning restore CS0618
    }

    [Fact]
    public void SetProperties_New_MatchesOld()
    {
        var command = new PutDataCommand
        {
#pragma warning disable CS0618 // Testing an obsolete feature
            Tag = PivDataTag.SecurityObject
        };
#pragma warning restore CS0618

        Assert.Equal((int)PivDataTag.SecurityObject, command.DataTag);
    }

    [Theory]
    [InlineData(1, PivDataTag.Discovery)]
    [InlineData(2, PivDataTag.Discovery)]
    [InlineData(3, PivDataTag.Discovery)]
    public void DisallowedTag_ThrowsException(
        int constructorToUse,
        PivDataTag tag)
    {
        var encoding = new byte[] { 0x53, 0x03, 0x39, 0x38, 0x37 };

        _ = Assert.Throws<ArgumentException>(() => GetPutDataCommandObject(constructorToUse, tag, 0, encoding));
    }

    [Fact]
    public void NoArgConstructer_NoSet_CorrectException()
    {
        var command = new PutDataCommand();

        _ = Assert.Throws<InvalidOperationException>(() => command.CreateCommandApdu());
    }

    [Fact]
    public void NoArgConstructer_NoData_CorrectException()
    {
        var command = new PutDataCommand
        {
            DataTag = (int)PivDataTag.Retired12
        };

        _ = Assert.Throws<InvalidOperationException>(() => command.CreateCommandApdu());
    }

    [Fact]
    public void CreateResponseForApdu_ReturnsCorrectType()
    {
        var encoding = new byte[] { 0x53, 0x03, 0x39, 0x38, 0x37 };
        var command = new PutDataCommand(0x5F0000, encoding);

        var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });
        var response = command.CreateResponseForApdu(responseApdu);

        Assert.True(response is PutDataResponse);
    }

    [Theory]
    [InlineData(PivDataTag.Chuid)]
    [InlineData(PivDataTag.Capability)]
    [InlineData(PivDataTag.IrisImages)]
    [InlineData(PivDataTag.Fingerprints)]
    public void Constructor_Property_Tag(
        PivDataTag tag)
    {
        var encoding = PivCommandResponseTestData.PutDataEncoding(tag, true);
        var command = new PutDataCommand
        {
#pragma warning disable CS0618 // testing an obsolete feature
            Tag = tag,
            EncodedData = encoding,
#pragma warning restore CS0618
        };

        var getTag = command.DataTag;

        Assert.Equal((int)tag, getTag);
    }

    [Theory]
    [InlineData(1, PivDataTag.Signature)]
    [InlineData(2, PivDataTag.Signature)]
    [InlineData(3, PivDataTag.Signature)]
    [InlineData(1, PivDataTag.Retired1)]
    [InlineData(2, PivDataTag.Retired1)]
    [InlineData(3, PivDataTag.Retired1)]
    [InlineData(1, PivDataTag.KeyHistory)]
    [InlineData(2, PivDataTag.KeyHistory)]
    [InlineData(3, PivDataTag.KeyHistory)]
    [InlineData(1, PivDataTag.PairingCodeReferenceData)]
    [InlineData(2, PivDataTag.PairingCodeReferenceData)]
    [InlineData(3, PivDataTag.PairingCodeReferenceData)]
    public void Constructor_Property_EncodedData(
        int constructorToUse,
        PivDataTag tag)
    {
        var encoding = PivCommandResponseTestData.PutDataEncoding(tag, true);
        var expectedResult = new Span<byte>(encoding);
        var command = GetPutDataCommandObject(constructorToUse, tag, 0, encoding);

        var encodedData = command.Data;

        var compareResult = expectedResult.SequenceEqual(encodedData.Span);

        Assert.True(compareResult);
    }

    [Theory]
    [InlineData(PivDataTag.Chuid)]
    [InlineData(PivDataTag.Capability)]
    [InlineData(PivDataTag.Authentication)]
    [InlineData(PivDataTag.Signature)]
    [InlineData(PivDataTag.KeyManagement)]
    [InlineData(PivDataTag.CardAuthentication)]
    [InlineData(PivDataTag.Retired1)]
    [InlineData(PivDataTag.Retired2)]
    [InlineData(PivDataTag.Retired3)]
    [InlineData(PivDataTag.Retired4)]
    [InlineData(PivDataTag.Retired5)]
    [InlineData(PivDataTag.Retired6)]
    [InlineData(PivDataTag.Retired7)]
    [InlineData(PivDataTag.Retired8)]
    [InlineData(PivDataTag.Retired9)]
    [InlineData(PivDataTag.Retired10)]
    [InlineData(PivDataTag.Retired11)]
    [InlineData(PivDataTag.Retired12)]
    [InlineData(PivDataTag.Retired13)]
    [InlineData(PivDataTag.Retired14)]
    [InlineData(PivDataTag.Retired15)]
    [InlineData(PivDataTag.Retired16)]
    [InlineData(PivDataTag.Retired17)]
    [InlineData(PivDataTag.Retired18)]
    [InlineData(PivDataTag.Retired19)]
    [InlineData(PivDataTag.Retired20)]
    [InlineData(PivDataTag.SecurityObject)]
    [InlineData(PivDataTag.IrisImages)]
    [InlineData(PivDataTag.FacialImage)]
    [InlineData(PivDataTag.Fingerprints)]
    [InlineData(PivDataTag.SecureMessageSigner)]
    [InlineData(PivDataTag.PairingCodeReferenceData)]
    public void DataTagConstructor_CmdApdu_Correct(
        PivDataTag tag)
    {
        var expected = new List<byte>();
        if (tag != PivDataTag.BiometricGroupTemplate)
        {
            expected = PivCommandResponseTestData.GetDataCommandExpectedApduData(tag);
        }

        var encoding = PivCommandResponseTestData.PutDataEncoding(tag, true);

        expected.AddRange(encoding);

        for (var index = 1; index <= 3; index++)
        {
            var command = GetPutDataCommandObject(index, tag, 0, encoding);
            var cmdApdu = command.CreateCommandApdu();
            var data = cmdApdu.Data;
            var compareResult = expected.SequenceEqual(data.ToArray());

            Assert.True(compareResult);
        }
    }

    [Theory]
    [InlineData(0x005FFF00)]
    [InlineData(0x005FFF01)]
    [InlineData(0x005FC109)]
    public void ProtectedConstructor_CmdApdu_Correct(
        int tag)
    {
        var encoding = new byte[] { 0x53, 0x03, 0x71, 0x72, 0x73 };

        var expected = PivCommandResponseTestData.GetDataCommandExpectedApduDataInt(tag);
        expected.AddRange(encoding);

        var command = new PutDataCommand(tag, encoding);
        var cmdApdu = command.CreateCommandApdu();

        var data = cmdApdu.Data;

        var compareResult = expected.SequenceEqual(data.ToArray());

        Assert.True(compareResult);
    }

    // The constructorToUse is either 1, 2, or 3
    // 1: use obsolete constructor
    // 2: use constructor with int and data
    // 3: use no-arg constructor and set properties
    private static PutDataCommand GetPutDataCommandObject(
        int constructorToUse,
        PivDataTag pivDataTag,
        int dataTag,
        byte[] data)
    {
        var dataTagInt = dataTag;
        if (pivDataTag != PivDataTag.Unknown)
        {
            dataTagInt = (int)pivDataTag;
        }

        return constructorToUse switch
        {
#pragma warning disable CS0618 // Testing an obsolete feature
            1 => new PutDataCommand(pivDataTag, data),
#pragma warning restore CS0618
            2 => new PutDataCommand(dataTagInt, data),
            _ => new PutDataCommand
            {
                DataTag = dataTagInt,
                Data = data
            }
        };
    }
}
