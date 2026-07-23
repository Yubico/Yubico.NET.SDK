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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using Moq;
using Xunit;
using Xunit.Abstractions;
using Yubico.YubiKey.Fido2.Commands;

namespace Yubico.YubiKey.Fido2.PinRetry;

/// <summary>
/// Destructive, manually opted-in hardware validation of the FIDO2 PIN_AUTH_BLOCKED
/// (<see cref="CtapStatus.PowerCycleRequired"/>, 0x34) fix. It drives one confirmed serial
/// through a bounded wrong-PIN cascade and asserts the fixed behavior: the resulting
/// <see cref="Fido2Exception"/> carries the real CTAP status, and a correct PIN submitted
/// while blocked is refused (0x34) without decrementing the counter.
/// </summary>
/// <remarks>
/// Safety: both tests select exactly one device by serial (never the first match), transmit
/// at most three wrong-PIN commands (enforced independently by the collector and the
/// <see cref="RecordingConnection"/> budget), and never reset FIDO2. Recovering the device
/// from the transient blocked state needs a physical unplug/reinsert, so the cascade and the
/// recovery assertion are split across two tests with that human step in between.
///
/// Enable by setting, exactly:
///   YUBIKEY_FIDO2_PIN_RETRY_REPRO_SERIAL=&lt;serial&gt;
///   YUBIKEY_FIDO2_PIN_RETRY_REPRO_CONFIRM=CONSUME_3_PIN_RETRIES_ON_&lt;serial&gt;
/// </remarks>
public sealed class Fido2PinRetryHardwareTests
{
    private const string SerialEnvironmentVariable = "YUBIKEY_FIDO2_PIN_RETRY_REPRO_SERIAL";
    private const string ConfirmationEnvironmentVariable = "YUBIKEY_FIDO2_PIN_RETRY_REPRO_CONFIRM";
    private const string RelyingPartyId = "sdk-repro.example";
    private const int MaximumWrongPinCommands = 3;
    private const int MinimumRetriesToStart = 7;

    // The standard SDK integration-test PIN; the deliberately wrong PIN reuses the
    // established invalid value from PinCollectionTests.
    private static readonly byte[] CorrectPin = "11234567"u8.ToArray();
    private static readonly byte[] WrongPin = "000000"u8.ToArray();

    private readonly ITestOutputHelper _output;

    public Fido2PinRetryHardwareTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [SkippableFact]
    public void Cascade_AfterFix_WrongPinCascadeSurfacesPowerCycleRequiredStatus()
    {
        int requestedSerial = RequireExactOptIn();
        IYubiKeyDevice realDevice = SelectExactHidFidoDevice(requestedSerial);

        byte[] correctPin = CorrectPin.ToArray();
        byte[] wrongPin = WrongPin.ToArray();
        var evidence = new PinRetryEvidence(requestedSerial, "cascade", MaximumWrongPinCommands);
        RecordingConnection? recordingConnection = null;
        Fido2Session? session = null;

        try
        {
            evidence.RecordDevice(realDevice);
            recordingConnection = new RecordingConnection(realDevice.Connect(YubiKeyApplication.Fido2));
            session = new Fido2Session(CreateSessionDeviceProxy(realDevice, recordingConnection));
            evidence.MinimumPinLength = session.AuthenticatorInfo.MinimumPinLength;

            // Establish a trustworthy, unblocked baseline before consuming any retry.
            recordingConnection.BeginPhase("initial-retry-query-1");
            RetryObservation initialFirst = QueryRetries(session, evidence, "initial-1");
            recordingConnection.BeginPhase("initial-retry-query-2");
            RetryObservation initialSecond = QueryRetries(session, evidence, "initial-2");

            RequireSafety(
                initialFirst.RetriesRemaining == initialSecond.RetriesRemaining,
                $"Initial retry queries differed: {initialFirst.RetriesRemaining} and {initialSecond.RetriesRemaining}.");
            RequireSafety(initialFirst.RetriesRemaining >= MinimumRetriesToStart, $"Initial retry count must be at least {MinimumRetriesToStart}.");
            RequireSafety(initialFirst.PowerCycleRequired != true, "Device already requires a power cycle.");
            RequireSafety(
                evidence.MinimumPinLength is null || wrongPin.Length >= evidence.MinimumPinLength.Value,
                "The established wrong PIN is shorter than this authenticator's minimum PIN length.");

            // Validate the known-correct PIN once. This proves the PIN and resets both the
            // retry counter and the per-boot mismatch count, giving the cascade a clean start.
            recordingConnection.BeginPhase("validated-correct-pin");
            evidence.ValidatedCorrectPinResult = TryVerifyLowLevel(session, correctPin, evidence, isPreCascadeGuard: true);

            RequireSafety(evidence.ValidatedCorrectPinResult == true, "Known PIN validation did not succeed.");
            RequireSafety(
                recordingConnection.CountTransmittedPinTokenCommands("validated-correct-pin") == 1,
                "Known PIN validation did not transmit exactly one PIN-token command.");

            recordingConnection.BeginPhase("normalized-maximum-query");
            RetryObservation afterCorrectPin = QueryRetries(session, evidence, "after-correct-pin");
            evidence.NormalizedMaximumRetries = Math.Max(initialFirst.RetriesRemaining, afterCorrectPin.RetriesRemaining);

            RequireSafety(afterCorrectPin.PowerCycleRequired != true, "Correct PIN validation left the device blocked.");
            RequireSafety(evidence.NormalizedMaximumRetries >= MinimumRetriesToStart, $"Normalized maximum retry count must be at least {MinimumRetriesToStart}.");

            int? knownMaximum = GetKnownMaximumRetries(requestedSerial);
            RequireSafety(
                knownMaximum is null || evidence.NormalizedMaximumRetries == knownMaximum.Value,
                $"Serial {requestedSerial} reported maximum {evidence.NormalizedMaximumRetries}; expected {knownMaximum}.");

            // The one destructive step: submit the wrong PIN through the high-level path,
            // capped at three transmitted commands.
            session.KeyCollector = new BoundedWrongPinCollector(wrongPin, MaximumWrongPinCommands, evidence).Collect;
            recordingConnection.BeginPhase("cascade", MaximumWrongPinCommands);
            try
            {
                evidence.CascadeReturnValue = session.TryVerifyPin(PinUvAuthTokenPermissions.GetAssertion, RelyingPartyId);
            }
            catch (Exception exception)
            {
                evidence.CascadeException = ExceptionObservation.From(exception);
            }

            recordingConnection.BeginPhase("after-cascade-query");
            evidence.AfterCascadeRetries = Observe(QueryRetries(session, evidence, "after-cascade"), out bool? afterCascadePcr);
            evidence.AfterCascadePowerCycleRequired = afterCascadePcr;

            // Confirm the block: the correct PIN is now refused without being evaluated.
            recordingConnection.BeginPhase("blocked-correct-pin");
            evidence.BlockedCorrectPinResult = TryVerifyLowLevel(session, correctPin, evidence, isPreCascadeGuard: false);

            recordingConnection.BeginPhase("after-blocked-correct-query");
            evidence.AfterBlockedCorrectRetries = Observe(QueryRetries(session, evidence, "after-blocked-correct"), out bool? afterBlockedPcr);
            evidence.AfterBlockedCorrectPowerCycleRequired = afterBlockedPcr;
        }
        finally
        {
            session?.Dispose();
            recordingConnection?.Dispose();
            CryptographicOperations.ZeroMemory(correctPin);
            CryptographicOperations.ZeroMemory(wrongPin);
            evidence.WriteTo(_output, recordingConnection);
        }

        Assert.NotNull(recordingConnection);

        List<CommandObservation> cascadePinCommands = TransmittedPinCommands(recordingConnection, "cascade");
        List<CommandObservation> blockedCorrectPinCommands = TransmittedPinCommands(recordingConnection, "blocked-correct-pin");

        // Firmware-robust submission bounds. On firmware that omits powerCycleState
        // (report: 5.8.0) the cascade runs the full three attempts and the third returns
        // 0x34; on firmware that reports powerCycleState=true the fixed SDK stops after the
        // first attempt. Either way the count is bounded by the cap.
        Assert.InRange(evidence.CollectorSubmissionCount, 1, MaximumWrongPinCommands);
        Assert.Equal(evidence.CollectorSubmissionCount, evidence.VerifyPinCallbackCount);
        Assert.Equal(0, evidence.CollectorFourthAttemptRefusalCount);
        Assert.Equal(1, evidence.ReleaseCallbackCount);
        Assert.Empty(evidence.UnexpectedCollectorRequests);

        // Every collected submission transmitted exactly one PIN-token command; the
        // connection budget was never exceeded.
        Assert.Equal(evidence.CollectorSubmissionCount, cascadePinCommands.Count);

        // The fix: the high-level path now surfaces the real CTAP status instead of a
        // status-less generic exception.
        Assert.NotNull(evidence.CascadeException);
        Assert.Equal(typeof(Fido2Exception).FullName, evidence.CascadeException.ExceptionType);
        Assert.Equal(CtapStatus.PowerCycleRequired, evidence.CascadeException.Fido2Status);
        Assert.Null(evidence.CascadeReturnValue);

        // Each transmitted wrong-PIN command decremented the persistent counter exactly
        // once, and we never consumed more retries than we transmitted.
        int cascadeCounterDelta = evidence.NormalizedMaximumRetries - evidence.AfterCascadeRetries;
        Assert.Equal(cascadePinCommands.Count, cascadeCounterDelta);
        Assert.InRange(cascadeCounterDelta, 1, MaximumWrongPinCommands);
        Assert.True(evidence.AfterCascadeRetries > 0);

        // A correct PIN submitted while blocked is refused without being evaluated: the
        // token command returns 0x34, the counter is unchanged, and the exception now
        // carries the status.
        Assert.Single(blockedCorrectPinCommands);
        Assert.Equal(CtapStatus.PowerCycleRequired, blockedCorrectPinCommands[0].CtapStatus);
        Assert.NotNull(evidence.BlockedCorrectPinException);
        Assert.Equal(typeof(Fido2Exception).FullName, evidence.BlockedCorrectPinException.ExceptionType);
        Assert.Equal(CtapStatus.PowerCycleRequired, evidence.BlockedCorrectPinException.Fido2Status);
        Assert.Null(evidence.BlockedCorrectPinResult);
        // powerCycleRequired is firmware-variable (report: null on 5.8.0), so it is only logged.
        Assert.Equal(evidence.AfterCascadeRetries, evidence.AfterBlockedCorrectRetries);
        Assert.True(evidence.AfterBlockedCorrectRetries > 0);

        AssertNoResetWasSent(recordingConnection);
    }

    [SkippableFact]
    public void Recovery_AfterPhysicalPowerCycle_CorrectPinRestoresRetryMaximum()
    {
        int requestedSerial = RequireExactOptIn();
        IYubiKeyDevice realDevice = SelectExactHidFidoDevice(requestedSerial);

        byte[] correctPin = CorrectPin.ToArray();
        var evidence = new PinRetryEvidence(requestedSerial, "recovery", MaximumWrongPinCommands);
        RecordingConnection? recordingConnection = null;
        Fido2Session? session = null;

        try
        {
            evidence.RecordDevice(realDevice);
            recordingConnection = new RecordingConnection(realDevice.Connect(YubiKeyApplication.Fido2));
            session = new Fido2Session(CreateSessionDeviceProxy(realDevice, recordingConnection));
            evidence.MinimumPinLength = session.AuthenticatorInfo.MinimumPinLength;

            recordingConnection.BeginPhase("recovery-before-query");
            evidence.RecoveryBeforeRetries = Observe(QueryRetries(session, evidence, "recovery-before-correct"), out bool? beforePcr);
            evidence.RecoveryBeforePowerCycleRequired = beforePcr;

            recordingConnection.BeginPhase("recovery-correct-pin");
            evidence.ValidatedCorrectPinResult = TryVerifyLowLevel(session, correctPin, evidence, isPreCascadeGuard: false);

            recordingConnection.BeginPhase("recovery-after-query");
            evidence.RecoveryAfterRetries = Observe(QueryRetries(session, evidence, "recovery-after-correct"), out bool? afterPcr);
            evidence.RecoveryAfterPowerCycleRequired = afterPcr;
        }
        finally
        {
            session?.Dispose();
            recordingConnection?.Dispose();
            CryptographicOperations.ZeroMemory(correctPin);
            evidence.WriteTo(_output, recordingConnection);
        }

        Assert.NotNull(recordingConnection);
        Assert.True(evidence.RecoveryBeforePowerCycleRequired != true);
        Assert.True(evidence.ValidatedCorrectPinResult == true);
        Assert.Null(evidence.ValidatedCorrectPinException);
        Assert.Null(evidence.ValidatedCorrectPinRetries);
        Assert.Null(evidence.ValidatedCorrectPinPowerCycleRequired);
        Assert.Equal(1, recordingConnection.CountTransmittedPinTokenCommands("recovery-correct-pin"));
        Assert.True(evidence.RecoveryAfterPowerCycleRequired != true);
        Assert.True(evidence.RecoveryAfterRetries >= evidence.RecoveryBeforeRetries);

        int? knownMaximum = GetKnownMaximumRetries(requestedSerial);
        if (knownMaximum is not null)
        {
            Assert.Equal(knownMaximum.Value, evidence.RecoveryAfterRetries);
        }

        AssertNoResetWasSent(recordingConnection);
    }

    /// <summary>
    /// Verifies a PIN via the low-level (single-attempt) overload, recording the result,
    /// out-parameters, and any exception. When <paramref name="isPreCascadeGuard"/> is true a
    /// throw is fatal (the safety baseline is invalid), so it is rethrown; otherwise it is
    /// recorded (the blocked-state attempt is expected to throw with a status).
    /// </summary>
    private static bool? TryVerifyLowLevel(Fido2Session session, byte[] pin, PinRetryEvidence evidence, bool isPreCascadeGuard)
    {
        try
        {
            bool result = session.TryVerifyPin(
                pin, PinUvAuthTokenPermissions.GetAssertion, RelyingPartyId, out int? retries, out bool? powerCycleRequired);

            if (isPreCascadeGuard)
            {
                evidence.ValidatedCorrectPinRetries = retries;
                evidence.ValidatedCorrectPinPowerCycleRequired = powerCycleRequired;
            }
            else
            {
                evidence.BlockedCorrectPinRetries = retries;
                evidence.BlockedCorrectPinPowerCycleRequired = powerCycleRequired;
            }

            return result;
        }
        catch (Exception exception) when (!isPreCascadeGuard)
        {
            evidence.BlockedCorrectPinException = ExceptionObservation.From(exception);
            return null;
        }
        catch (Exception exception)
        {
            evidence.ValidatedCorrectPinException = ExceptionObservation.From(exception);
            throw new InvalidOperationException(
                "Safety precondition failed; known PIN validation threw before the cascade started.", exception);
        }
    }

    private static int Observe(RetryObservation observation, out bool? powerCycleRequired)
    {
        powerCycleRequired = observation.PowerCycleRequired;
        return observation.RetriesRemaining;
    }

    private static List<CommandObservation> TransmittedPinCommands(RecordingConnection connection, string phase) =>
        connection.Transcript.Where(entry => entry.Phase == phase && entry.IsPinTokenCommand && entry.Transmitted).ToList();

    private static void AssertNoResetWasSent(RecordingConnection connection)
    {
        Assert.Equal(1, connection.InnerDisposeCount);
        Assert.DoesNotContain(connection.Transcript, entry => entry.CommandType.EndsWith(".ResetCommand", StringComparison.Ordinal));
    }

    private static int RequireExactOptIn()
    {
        string? serialText = Environment.GetEnvironmentVariable(SerialEnvironmentVariable);
        string? confirmation = Environment.GetEnvironmentVariable(ConfirmationEnvironmentVariable);
        bool hasSerial = int.TryParse(serialText, NumberStyles.None, CultureInfo.InvariantCulture, out int serial) && serial > 0;
        string expectedConfirmation = hasSerial ? $"CONSUME_3_PIN_RETRIES_ON_{serial}" : string.Empty;

        Skip.IfNot(
            hasSerial && string.Equals(confirmation, expectedConfirmation, StringComparison.Ordinal),
            $"Manual hardware run disabled. Set {SerialEnvironmentVariable}=<serial> and " +
            $"{ConfirmationEnvironmentVariable}=CONSUME_3_PIN_RETRIES_ON_<serial> exactly.");

        return serial;
    }

    private static IYubiKeyDevice SelectExactHidFidoDevice(int requestedSerial)
    {
        List<IYubiKeyDevice> matches = YubiKeyDevice
            .FindByTransport(Transport.HidFido)
            .Where(device => device.AvailableTransports.HasFlag(Transport.HidFido) && device.SerialNumber == requestedSerial)
            .ToList();

        IYubiKeyDevice selected = Assert.Single(matches);
        Assert.Equal(requestedSerial, selected.SerialNumber);
        return selected;
    }

    /// <summary>
    /// Wraps the real device so the session connects through the <see cref="RecordingConnection"/>,
    /// while every other property the session reads reflects the real hardware.
    /// </summary>
    private static IYubiKeyDevice CreateSessionDeviceProxy(IYubiKeyDevice realDevice, RecordingConnection recordingConnection)
    {
        var proxy = new Mock<IYubiKeyDevice>();
        proxy.SetupGet(device => device.SerialNumber).Returns(realDevice.SerialNumber);
        proxy.SetupGet(device => device.FirmwareVersion).Returns(realDevice.FirmwareVersion);
        proxy.SetupGet(device => device.AvailableUsbCapabilities).Returns(realDevice.AvailableUsbCapabilities);
        proxy.SetupGet(device => device.EnabledUsbCapabilities).Returns(realDevice.EnabledUsbCapabilities);
        proxy.SetupGet(device => device.AvailableTransports).Returns(realDevice.AvailableTransports);
        proxy.Setup(device => device.Connect(YubiKeyApplication.Fido2)).Returns(recordingConnection);
        return proxy.Object;
    }

    private static RetryObservation QueryRetries(Fido2Session session, PinRetryEvidence evidence, string label)
    {
        (int retriesRemaining, bool? powerCycleRequired) = session.Connection.SendCommand(new GetPinRetriesCommand()).GetData();
        return evidence.RecordRetries(label, retriesRemaining, powerCycleRequired);
    }

    // Known configured maximum retry counts for the lab devices, used as an extra guard.
    private static int? GetKnownMaximumRetries(int serial) => serial == 103 ? 8 : null;

    private static void RequireSafety(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Safety precondition failed; no cascade started. {message}");
        }
    }

    /// <summary>
    /// A <c>KeyCollector</c> that submits a fixed wrong PIN up to <c>maxSubmissions</c> times,
    /// then refuses. The refusal is a safety net only: the fixed SDK stops on its own once the
    /// authenticator returns PIN_AUTH_BLOCKED.
    /// </summary>
    private sealed class BoundedWrongPinCollector
    {
        private readonly byte[] _wrongPin;
        private readonly int _maxSubmissions;
        private readonly PinRetryEvidence _evidence;

        public BoundedWrongPinCollector(byte[] wrongPin, int maxSubmissions, PinRetryEvidence evidence)
        {
            _wrongPin = wrongPin;
            _maxSubmissions = maxSubmissions;
            _evidence = evidence;
        }

        public bool Collect(KeyEntryData data)
        {
            switch (data.Request)
            {
                case KeyEntryRequest.Release:
                    _evidence.RecordRelease(data);
                    return true;

                case KeyEntryRequest.VerifyFido2Pin when _evidence.CollectorSubmissionCount >= _maxSubmissions:
                    _evidence.RecordRefusedFourthAttempt(data);
                    return false;

                case KeyEntryRequest.VerifyFido2Pin:
                    data.SubmitValue(_wrongPin);
                    _evidence.RecordSubmission(data);
                    return true;

                default:
                    _evidence.RecordUnexpectedRequest(data);
                    return false;
            }
        }
    }
}
