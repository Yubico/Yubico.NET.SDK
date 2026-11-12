# Successor non-SW family replacement whole units

**No content verdicts.** Pinned source `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; read complete units and relevant enclosing context.

## `core-001:unit:0052beaaca7e3aab`
`src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/RawOtpHidSessionTests.cs:124–126`; B5 tests-and-helpers; **UNREAD / zero credit**.

````csharp


        public List<byte[]> SentReports { get; } = [];
````

## `core-001:unit:891bea911fc43528`
`src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/Otp/Hid/OtpHidProtocolTests.cs:69–78`; B5 tests-and-helpers; **UNREAD / zero credit**.

````csharp


    private static OtpHidProtocol CreateProtocolWithMock(
        MockHidConnection mock,
        TimeSpan? touchTimeout = null)
    {
        // Queue initial status report for initialization (firmware 5.4.3)
        mock.QueueReport([0x00, 0x05, 0x04, 0x03, 0x00, 0x00, 0x00, 0x00]);
        return new OtpHidProtocol(new OtpHidConnection(mock), touchTimeout: touchTimeout);
    }
````

## `core-001:unit:f96355d3db0dfcd7`
`src/Core/src/Protocols/SmartCard/Scp/ScpProcessor.cs:34–39`; B6 XML-contracts; **UNREAD / zero credit**.

````csharp


    /// <summary>
    ///     Gets the SCP state for this processor.
    /// </summary>
    private ScpState State { get; } = state;
````

## `core-001:unit:d41ecdd3d18d0e91`
`src/Core/src/Protocols/SmartCard/Apdu/ApduCommand.cs:75–79`; B6 XML-contracts; **UNREAD / zero credit**.

````csharp


    /// <inheritdoc />
    public override string ToString() =>
        $"CLA: 0x{Cla:X2} INS: 0x{Ins:X2} P1: 0x{P1:X2} P2: 0x{P2:X2} Le: {Le} Data: {Data.Length} bytes";
````

## `core-001:unit:035ae50978ce88d7`
`src/Core/src/Protocols/SmartCard/Scp/SessionKeys.cs:51–56`; B6 XML-contracts; **UNREAD / zero credit**.

````csharp


    /// <summary>
    ///     Gets the session encryption key (S-ENC).
    /// </summary>
    public ReadOnlySpan<byte> Senc => _senc;
````

## `core-001:unit:9cd17279b86d5878`
`src/Core/src/Protocols/SmartCard/Apdu/PcscProtocol.cs:79–86`; B6 XML-contracts; **UNREAD / zero credit**.

````csharp


    /// <summary>
    ///     The guard refusing overlapping logical exchanges on this protocol's connection. Shared with
    ///     decorators that drive the same connection through their own processors (e.g. the SCP wrapper), so
    ///     plain and wrapped traffic can never interleave.
    /// </summary>
    internal ExchangeGuard ExchangeGuard => _exchangeGuard;
````

## `core-001:unit:62ede839d51aa516`
`src/Core/src/Protocols/SmartCard/Scp/ScpProcessor.cs:183–189`; B6 XML-contracts; **UNREAD / zero credit**.

````csharp



    /// <summary>
    ///     Gets the APDU formatter.
    /// </summary>
    public IApduFormatter Formatter { get; } = @delegate.Formatter;
````

## `core-001:unit:edaef2dcf282acc6`
`src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreScpAuditReproTests.cs:171–186`; B6 tests-and-helpers; **UNREAD / zero credit**.

````csharp


    /// <summary>
    ///     ISO/IEC 9797-1 Padding Method 2 covers the whole block: 80 00..00 is an empty message,
    ///     while all zeros has no 0x80 and is bad padding.
    /// </summary>
    [Fact]
    [Trait("Audit", "N2")]
    public void N2_DecryptPaddingOnlyBlock_IsEmptyMessage_AndAllZerosIsBadPadding()
    {
        using var state = new ScpState(new SessionKeys(new byte[16], new byte[16], new byte[16]), new byte[16]);
        _ = state.Encrypt([]); // advance the counter so Decrypt has a command to answer

        Assert.Empty(state.Decrypt(EncryptBlockForResponse(state, [0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0])));
        Assert.Throws<BadResponseException>(() => state.Decrypt(EncryptBlockForResponse(state, new byte[16])));
    }
````

