// Copyright 2026 Yubico AB
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

using Yubico.YubiKit.Piv.Certificates;

namespace Yubico.YubiKit.Piv.UnitTests.Certificates;

/// <summary>
/// Exact-value coverage for the <c>PivSlot</c> to PIV certificate data-object-id mapping in
/// <see cref="PivCertificateProtocol.GetCertificateObjectId(PivSlot)"/>. Each mapped slot arm
/// asserts the specific <see cref="PivDataObject"/> constant it must resolve to, so that changing
/// any single arm's mapping fails a test.
/// </summary>
public class PivCertificateObjectIdTests
{
    [Theory]
    [InlineData(PivSlot.Authentication, PivDataObject.Authentication)]
    [InlineData(PivSlot.Signature, PivDataObject.Signature)]
    [InlineData(PivSlot.KeyManagement, PivDataObject.KeyManagement)]
    [InlineData(PivSlot.CardAuthentication, PivDataObject.CardAuthentication)]
    [InlineData(PivSlot.Attestation, PivDataObject.Attestation)]
    [InlineData(PivSlot.Retired1, PivDataObject.Retired1)]
    [InlineData(PivSlot.Retired2, PivDataObject.Retired2)]
    [InlineData(PivSlot.Retired3, PivDataObject.Retired3)]
    [InlineData(PivSlot.Retired4, PivDataObject.Retired4)]
    [InlineData(PivSlot.Retired5, PivDataObject.Retired5)]
    [InlineData(PivSlot.Retired6, PivDataObject.Retired6)]
    [InlineData(PivSlot.Retired7, PivDataObject.Retired7)]
    [InlineData(PivSlot.Retired8, PivDataObject.Retired8)]
    [InlineData(PivSlot.Retired9, PivDataObject.Retired9)]
    [InlineData(PivSlot.Retired10, PivDataObject.Retired10)]
    [InlineData(PivSlot.Retired11, PivDataObject.Retired11)]
    [InlineData(PivSlot.Retired12, PivDataObject.Retired12)]
    [InlineData(PivSlot.Retired13, PivDataObject.Retired13)]
    [InlineData(PivSlot.Retired14, PivDataObject.Retired14)]
    [InlineData(PivSlot.Retired15, PivDataObject.Retired15)]
    [InlineData(PivSlot.Retired16, PivDataObject.Retired16)]
    [InlineData(PivSlot.Retired17, PivDataObject.Retired17)]
    [InlineData(PivSlot.Retired18, PivDataObject.Retired18)]
    [InlineData(PivSlot.Retired19, PivDataObject.Retired19)]
    [InlineData(PivSlot.Retired20, PivDataObject.Retired20)]
    public void GetCertificateObjectId_MappedSlot_ReturnsExpectedObjectId(PivSlot slot, int expectedObjectId)
    {
        int actual = PivCertificateProtocol.GetCertificateObjectId(slot);

        Assert.Equal(expectedObjectId, actual);
    }

    [Fact]
    public void GetCertificateObjectId_UnmappedSlot_ThrowsArgumentExceptionWithSlotSpecificMessage()
    {
        var slot = (PivSlot)0x00;

        var exception = Assert.Throws<ArgumentException>(() => PivCertificateProtocol.GetCertificateObjectId(slot));

        Assert.Equal("slot", exception.ParamName);
        Assert.StartsWith("Slot 0x00 does not support certificates", exception.Message);
    }
}