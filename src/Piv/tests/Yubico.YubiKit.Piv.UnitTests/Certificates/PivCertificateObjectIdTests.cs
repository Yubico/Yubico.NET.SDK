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
/// Exact-value coverage for the <c>Slot</c> to PIV certificate data-object-id mapping in
/// <see cref="PivCertificateProtocol.GetCertificateObjectId(Slot)"/>. Each mapped slot arm
/// asserts the specific <see cref="PivDataObject"/> constant it must resolve to, so that changing
/// any single arm's mapping fails a test.
/// </summary>
public class PivCertificateObjectIdTests
{
    [Theory]
    [InlineData(Slot.Authentication, PivDataObject.Authentication)]
    [InlineData(Slot.Signature, PivDataObject.Signature)]
    [InlineData(Slot.KeyManagement, PivDataObject.KeyManagement)]
    [InlineData(Slot.CardAuthentication, PivDataObject.CardAuthentication)]
    [InlineData(Slot.Attestation, PivDataObject.Attestation)]
    [InlineData(Slot.Retired1, PivDataObject.Retired1)]
    [InlineData(Slot.Retired2, PivDataObject.Retired2)]
    [InlineData(Slot.Retired3, PivDataObject.Retired3)]
    [InlineData(Slot.Retired4, PivDataObject.Retired4)]
    [InlineData(Slot.Retired5, PivDataObject.Retired5)]
    [InlineData(Slot.Retired6, PivDataObject.Retired6)]
    [InlineData(Slot.Retired7, PivDataObject.Retired7)]
    [InlineData(Slot.Retired8, PivDataObject.Retired8)]
    [InlineData(Slot.Retired9, PivDataObject.Retired9)]
    [InlineData(Slot.Retired10, PivDataObject.Retired10)]
    [InlineData(Slot.Retired11, PivDataObject.Retired11)]
    [InlineData(Slot.Retired12, PivDataObject.Retired12)]
    [InlineData(Slot.Retired13, PivDataObject.Retired13)]
    [InlineData(Slot.Retired14, PivDataObject.Retired14)]
    [InlineData(Slot.Retired15, PivDataObject.Retired15)]
    [InlineData(Slot.Retired16, PivDataObject.Retired16)]
    [InlineData(Slot.Retired17, PivDataObject.Retired17)]
    [InlineData(Slot.Retired18, PivDataObject.Retired18)]
    [InlineData(Slot.Retired19, PivDataObject.Retired19)]
    [InlineData(Slot.Retired20, PivDataObject.Retired20)]
    public void GetCertificateObjectId_MappedSlot_ReturnsExpectedObjectId(Slot slot, int expectedObjectId)
    {
        int actual = PivCertificateProtocol.GetCertificateObjectId(slot);

        Assert.Equal(expectedObjectId, actual);
    }

    [Fact]
    public void GetCertificateObjectId_UnmappedSlot_ThrowsArgumentExceptionWithSlotSpecificMessage()
    {
        var slot = (Slot)0x00;

        var exception = Assert.Throws<ArgumentException>(() => PivCertificateProtocol.GetCertificateObjectId(slot));

        Assert.Equal("slot", exception.ParamName);
        Assert.StartsWith("Slot 0x00 does not support certificates", exception.Message);
    }
}