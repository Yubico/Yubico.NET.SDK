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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;

namespace Yubico.YubiKit.Piv.UnitTests;

public class PivToolTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SlotOverview_DisposesPartialResultsButTransfersSuccessfulCertificates(bool failLater)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var certificate = new CertificateRequest("CN=PIV unit test", key, HashAlgorithmName.SHA256)
            .CreateSelfSigned(DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1));
        var session = Substitute.For<IPivSession>();
        var token = TestContext.Current.CancellationToken;
        session.GetCertificateAsync(Slot.Authentication, token).Returns(certificate);
        if (failLater)
            session.GetCertificateAsync(Slot.Signature, token)
                .Returns(Task.FromException<X509Certificate2?>(new InvalidOperationException("later slot failed")));

        var result = await SlotInfoQuery.GetAllSlotsInfoAsync(session, token);

        Assert.Equal(!failLater, result.Success);
        if (failLater)
        {
            Assert.Empty(result.Slots);
            Assert.ThrowsAny<CryptographicException>(() => _ = certificate.RawData);
        }
        else
        {
            Assert.Same(certificate, result.Slots[0].Certificate);
            Assert.NotEmpty(certificate.RawData);
        }
    }
}
