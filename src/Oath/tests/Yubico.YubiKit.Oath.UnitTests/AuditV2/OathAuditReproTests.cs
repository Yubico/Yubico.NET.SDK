using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.Oath.UnitTests.AuditV2;

public sealed class OathAuditReproTests
{
    private static byte[] SelectResponse() =>
    [
        0x79, 0x03, 0x05, 0x07, 0x00,
        0x71, 0x08, 1, 2, 3, 4, 5, 6, 7, 8,
        0x90, 0x00
    ];

    [Fact]
    [Trait("Audit", "YESDK-1636")]
    public void YESDK1636_ParseUriRejectsZeroPeriod()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            using var parsed = CredentialData.ParseUri("otpauth://totp/demo?secret=JBSWY3DPEHPK3PXP&period=0");
        });
    }

    [Fact]
    [Trait("Audit", "YESDK-1636")]
    public async Task YESDK1636_MalformedPeriodDoesNotAbortNormalBatchEntry()
    {
        byte[] malformed = "0/demo"u8.ToArray();
        byte[] normal = "normal"u8.ToArray();
        byte[] response =
        [
            0x71, (byte)malformed.Length, .. malformed,
            0x76, 0x05, 0x06, 0, 0, 0, 1,
            0x71, (byte)normal.Length, .. normal,
            0x76, 0x05, 0x06, 0, 0, 0, 2,
            0x90, 0x00
        ];
        var connection = new RecordingSmartCardConnection(SelectResponse(), response);
        await using var session = await OathSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);

        var result = await session.CalculateAllAsync(1_704_067_200, TestContext.Current.CancellationToken);

        Assert.Contains(result, entry => entry.Key.Name == "normal" && entry.Value is not null);
        // SELECT shares INS A4 with CALCULATE ALL; P1 tells them apart (SELECT 04, CALCULATE ALL 00).
        Assert.Equal(1, connection.TransmittedCommands.Count(command =>
            command[1] == OathConstants.InsCalculateAll && command[2] == 0x00));
    }

    /// <summary>HOTP has no time step; the Key URI "period" is ignored for it (RFC 4226 counter-based).</summary>
    [Fact]
    [Trait("Audit", "YESDK-1636")]
    public void YESDK1636_ParseUriHotpWithZeroPeriod_StillParses()
    {
        using var parsed = CredentialData.ParseUri("otpauth://hotp/demo?secret=JBSWY3DPEHPK3PXP&period=0&counter=0");

        Assert.Equal(OathType.Hotp, parsed.OathType);
    }

    [Theory]
    [Trait("Audit", "YESDK-1636")]
    [InlineData("99999999999/demo")] // overflows int
    [InlineData("-30/demo")] // not a period prefix at all: parses as a 30 s credential named "-30/demo"
    [InlineData("\u0663\u0660/demo")] // Arabic-Indic digits: \d would match, int.Parse would not
    public async Task YESDK1636_OtherBadPeriodPrefixes_DoNotAbortBatch(string malformedName)
    {
        byte[] malformed = Encoding.UTF8.GetBytes(malformedName);
        byte[] normal = "normal"u8.ToArray();
        byte[] response =
        [
            0x71, (byte)malformed.Length, .. malformed,
            0x76, 0x05, 0x06, 0, 0, 0, 1,
            0x71, (byte)normal.Length, .. normal,
            0x76, 0x05, 0x06, 0, 0, 0, 2,
            0x90, 0x00
        ];
        var connection = new RecordingSmartCardConnection(SelectResponse(), response);
        await using var session = await OathSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);

        var result = await session.CalculateAllAsync(1_704_067_200, TestContext.Current.CancellationToken);

        Assert.Contains(result, entry => entry.Key.Name == "normal" && entry.Value is not null);
    }

    /// <summary>LIST parses the same IDs; one malformed name must not hide the others.</summary>
    [Fact]
    [Trait("Audit", "YESDK-1636")]
    public async Task YESDK1636_MalformedPeriodDoesNotAbortList()
    {
        byte[] malformed = "0/demo"u8.ToArray();
        byte[] normal = "normal"u8.ToArray();
        byte[] response =
        [
            0x72, (byte)(malformed.Length + 1), 0x21, .. malformed, // TOTP | SHA1
            0x72, (byte)(normal.Length + 1), 0x21, .. normal,
            0x90, 0x00
        ];
        var connection = new RecordingSmartCardConnection(SelectResponse(), response);
        await using var session = await OathSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);

        var credentials = await session.ListCredentialsAsync(TestContext.Current.CancellationToken);

        var only = Assert.Single(credentials);
        Assert.Equal("normal", only.Name);
    }

    [Theory(Skip = "Parked: YESDK-1634 (26k) — reproduces an open finding; unskip when fixed")]
    [InlineData("delete")]
    [InlineData("rename")]
    [InlineData("calculate")]
    [InlineData("code")]
    [Trait("Audit", "YESDK-1634")]
    public async Task YESDK1634_ForeignCredentialCannotTargetLocalDevice(string operation)
    {
        byte[] operationResponse = operation switch
        {
            "calculate" => [0x75, 0x01, 0x01, 0x90, 0x00],
            "code" => [0x76, 0x05, 0x06, 0, 0, 0, 1, 0x90, 0x00],
            _ => [0x90, 0x00]
        };
        var connection = new RecordingSmartCardConnection(SelectResponse(), operationResponse);
        await using var session = await OathSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);
        var foreign = new Credential("another-device", "shared"u8.ToArray(), null, "shared",
            OathType.Totp, 30, false);
        int before = connection.TransmittedCommands.Count;

        await Assert.ThrowsAsync<ArgumentException>(() => operation switch
        {
            "delete" => session.DeleteCredentialAsync(foreign, TestContext.Current.CancellationToken),
            "rename" => session.RenameCredentialAsync(foreign, null, "renamed", TestContext.Current.CancellationToken),
            "calculate" => session.CalculateAsync(foreign, new byte[8], TestContext.Current.CancellationToken),
            _ => session.CalculateCodeAsync(foreign, 1_704_067_200, TestContext.Current.CancellationToken)
        });
        Assert.Equal(before, connection.TransmittedCommands.Count);
    }

    [Fact]
    [Trait("Audit", "YESDK-1634")]
    public async Task YESDK1634_RetryDoesNotZeroCallerPassword()
    {
        byte[] lockedSelect = [.. SelectResponse()[..^2], 0x74, 0x08, 1, 2, 3, 4, 5, 6, 7, 8, 0x90, 0x00];
        var connection = new RecordingSmartCardConnection(lockedSelect, [0x69, 0x82], [0x6A, 0x80]);
        await using var session = await OathSession.CreateAsync(connection,
            cancellationToken: TestContext.Current.CancellationToken);
        byte[] password = Encoding.UTF8.GetBytes("test-password");
        try
        {
            await Assert.ThrowsAnyAsync<Exception>(() => session.AuthenticateAndRetryAsync(
                ct => session.ListCredentialsAsync(ct),
                _ => Task.FromResult((ReadOnlyMemory<byte>)password), TestContext.Current.CancellationToken));

            Assert.Equal("test-password"u8.ToArray(), password);
            Assert.Equal(3, connection.TransmittedCommands.Count); // SELECT, LIST, VALIDATE
        }
        finally
        {
            CryptographicOperations.ZeroMemory(password);
        }
    }
}