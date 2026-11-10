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
/// <see cref="CertificateProtocol.GetCertificateObjectId(Slot)"/>. Each mapped slot arm
/// asserts the specific <see cref="DataObject"/> constant it must resolve to, so that changing
/// any single arm's mapping fails a test.
/// </summary>
public class PivCertificateObjectIdTests
{
    [Theory]
    [InlineData(Slot.Authentication, DataObject.Authentication)]
    [InlineData(Slot.Signature, DataObject.Signature)]
    [InlineData(Slot.KeyManagement, DataObject.KeyManagement)]
    [InlineData(Slot.CardAuthentication, DataObject.CardAuthentication)]
    [InlineData(Slot.Attestation, DataObject.Attestation)]
    [InlineData(Slot.Retired1, DataObject.Retired1)]
    [InlineData(Slot.Retired2, DataObject.Retired2)]
    [InlineData(Slot.Retired3, DataObject.Retired3)]
    [InlineData(Slot.Retired4, DataObject.Retired4)]
    [InlineData(Slot.Retired5, DataObject.Retired5)]
    [InlineData(Slot.Retired6, DataObject.Retired6)]
    [InlineData(Slot.Retired7, DataObject.Retired7)]
    [InlineData(Slot.Retired8, DataObject.Retired8)]
    [InlineData(Slot.Retired9, DataObject.Retired9)]
    [InlineData(Slot.Retired10, DataObject.Retired10)]
    [InlineData(Slot.Retired11, DataObject.Retired11)]
    [InlineData(Slot.Retired12, DataObject.Retired12)]
    [InlineData(Slot.Retired13, DataObject.Retired13)]
    [InlineData(Slot.Retired14, DataObject.Retired14)]
    [InlineData(Slot.Retired15, DataObject.Retired15)]
    [InlineData(Slot.Retired16, DataObject.Retired16)]
    [InlineData(Slot.Retired17, DataObject.Retired17)]
    [InlineData(Slot.Retired18, DataObject.Retired18)]
    [InlineData(Slot.Retired19, DataObject.Retired19)]
    [InlineData(Slot.Retired20, DataObject.Retired20)]
    public void GetCertificateObjectId_MappedSlot_ReturnsExpectedObjectId(Slot slot, int expectedObjectId)
    {
        int actual = CertificateProtocol.GetCertificateObjectId(slot);

        Assert.Equal(expectedObjectId, actual);
    }

    [Fact]
    public void GetCertificateObjectId_UnmappedSlot_ThrowsArgumentExceptionWithSlotSpecificMessage()
    {
        var slot = (Slot)0x00;

        var exception = Assert.Throws<ArgumentException>(() => CertificateProtocol.GetCertificateObjectId(slot));

        Assert.Equal("slot", exception.ParamName);
        Assert.StartsWith("Slot 0x00 does not support certificates", exception.Message);
    }
}