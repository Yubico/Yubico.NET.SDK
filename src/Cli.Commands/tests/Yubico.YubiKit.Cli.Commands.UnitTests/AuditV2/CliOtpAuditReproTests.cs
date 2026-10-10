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

using Yubico.YubiKit.Cli.Commands.Otp;

namespace Yubico.YubiKit.Cli.Commands.UnitTests.AuditV2;

/// <summary>
///     Audit #25 a) (YESDK-1633): <c>--length</c> is documented as 1-38 but is passed unchecked to
///     <see cref="OtpHelpers.GenerateStaticPassword" />, which uses it as a <c>stackalloc</c> size.
/// </summary>
public class CliOtpAuditReproTests
{
    [Theory]
    [Trait("Audit", "YESDK-1633")]
    [InlineData(0)]
    [InlineData(39)]
    public void YESDK1633_GenerateStaticPasswordOutOfDocumentedRange_Throws(int length) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => OtpHelpers.GenerateStaticPassword(length));

    /// <summary>
    ///     A negative length reaches <c>stackalloc</c> and terminates the process; runs in a child process.
    /// </summary>
    [Fact]
    [Trait("Audit", "YESDK-1633")]
    public async Task YESDK1633_GenerateStaticPasswordNegativeLength_DoesNotTerminateProcess()
    {
        var (exitCode, output) = await ChildProcessTestRunner.RunAsync(
            typeof(CliOtpAuditReproTests),
            nameof(YESDK1633_ChildBody_NegativeLength_ThrowsArgumentOutOfRange),
            TestContext.Current.CancellationToken);

        Assert.True(
            exitCode == 0,
            $"Child test process exited with {exitCode} (134 = SIGABRT). Output head:\n" +
            (output.Length <= 300 ? output : output[..300]));
    }

    [Fact]
    [Trait("Audit", "YESDK-1633")]
    public void YESDK1633_ChildBody_NegativeLength_ThrowsArgumentOutOfRange()
    {
        ChildProcessTestRunner.SkipUnlessChild(nameof(YESDK1633_ChildBody_NegativeLength_ThrowsArgumentOutOfRange));

        Assert.Throws<ArgumentOutOfRangeException>(() => OtpHelpers.GenerateStaticPassword(-1));
    }
}
