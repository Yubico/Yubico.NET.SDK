# Core family eligibility/qualification replacement whole units

**No content verdicts.** Pinned source `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; read exact whole units and enclosing context.

## `core-001:unit:0052beaaca7e3aab`
B5 tests-and-helpers; `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/RawOtpHidSessionTests.cs:124–126`; original rank 52.

````csharp


        public List<byte[]> SentReports { get; } = [];
````

## `core-001:unit:891bea911fc43528`
B5 tests-and-helpers; `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/Otp/Hid/OtpHidProtocolTests.cs:69–78`; original rank 53.

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
B6 XML-contracts; `src/Core/src/Protocols/SmartCard/Scp/ScpProcessor.cs:34–39`; original rank 12.

````csharp


    /// <summary>
    ///     Gets the SCP state for this processor.
    /// </summary>
    private ScpState State { get; } = state;
````

## `core-001:unit:3ad8b9f956a4e672`
B6 XML-contracts; `src/Core/src/Protocols/SmartCard/Apdu/SWConstants.cs:52–55`; original rank 13.

````csharp


    /// <summary>Warning: File filled up by last write operation.</summary>
    public const short NoMoreSpaceInFile = 0x6381;
````

## `core-001:unit:aa9d6d49701b178d`
B6 XML-contracts; `src/Core/src/Protocols/SmartCard/Apdu/SWConstants.cs:142–145`; original rank 14.

````csharp


    /// <summary>Wrong parameters: Not enough memory space in the file.</summary>
    public const short NotEnoughSpace = 0x6A84;
````

## `core-001:unit:a3d37dc1aed22b2f`
B6 XML-contracts; `src/Core/src/Protocols/SmartCard/Apdu/SWConstants.cs:151–154`; original rank 15.

````csharp


    /// <summary>Wrong parameters: Lc inconsistent with P1-P2.</summary>
    public const short InconsistentLengthWithP1P2 = 0x6A87;
````

## `core-001:unit:f39183b2d438d21d`
B6 XML-contracts; `src/Core/src/Protocols/SmartCard/Apdu/SWConstants.cs:71–74`; original rank 16.

````csharp


    /// <summary>Execution error: Memory failure.</summary>
    public const short MemoryFailure = 0x6581;
````

## `core-001:unit:edaef2dcf282acc6`
B6 tests-and-helpers; `src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreScpAuditReproTests.cs:171–186`; original rank 14.

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

