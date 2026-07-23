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
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Fido2.Commands;

namespace Yubico.YubiKey.Fido2;

/// <summary>
/// Destructive, manually opted-in HARDWARE validation of the FIDO2 PIN_AUTH_BLOCKED
/// (CtapStatus.PowerCycleRequired, 0x34) fix. It drives one physical, confirmed serial
/// through a bounded wrong-PIN cascade and asserts the FIXED behavior: the resulting
/// <see cref="Fido2Exception"/> carries the real CTAP status, and a correct PIN submitted
/// while blocked is refused (0x34) without decrementing the counter. These tests never
/// reset FIDO2 and cap transmission at three wrong-PIN commands. Recovery from the blocked
/// state requires a physical unplug/reinsert (a human gate), performed by the separate
/// recovery test.
/// </summary>
public sealed class Fido2PinRetryHardwareCharacterizationTests
{
    private const string SerialEnvironmentVariable = "YUBIKEY_FIDO2_PIN_RETRY_REPRO_SERIAL";
    private const string ConfirmationEnvironmentVariable = "YUBIKEY_FIDO2_PIN_RETRY_REPRO_CONFIRM";
    private const string RelyingPartyId = "sdk-repro.example";
    private const int MaximumWrongPinCommands = 3;

    // The bug report's live transcript shows the retry counter dropping by 3
    // across the cascade (8 -> 5): each of the three transmitted wrong-PIN
    // commands decrements once, including the third one that also returns
    // PIN_AUTH_BLOCKED (0x34). The exact decrement count is firmware-variable,
    // so it is RECORDED and bounded (never more than we transmitted) rather than
    // hard-asserted to an exact value; the load-bearing proof is the raw 0x34
    // status and the lost public exception status, not the decrement count.
    private const int ReportedCounterDelta = 3;

    private readonly ITestOutputHelper _output;

    public Fido2PinRetryHardwareCharacterizationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [SkippableFact]
    public void Cascade_AfterFix_WrongPinCascadeSurfacesPowerCycleRequiredStatus()
    {
        int requestedSerial = RequireExactOptIn();
        IYubiKeyDevice realDevice = SelectExactHidFidoDevice(requestedSerial);

        byte[] correctPin = "11234567"u8.ToArray();
        byte[] wrongPin = "000000"u8.ToArray();
        var evidence = new CharacterizationEvidence(requestedSerial, "cascade");
        RecordingConnection? recordingConnection = null;
        Fido2Session? session = null;

        try
        {
            evidence.RecordDevice(realDevice);
            IYubiKeyConnection realConnection = realDevice.Connect(YubiKeyApplication.Fido2);
            recordingConnection = new RecordingConnection(realConnection);
            IYubiKeyDevice proxyDevice = CreateSessionDeviceProxy(realDevice, recordingConnection);
            session = new Fido2Session(proxyDevice);

            evidence.MinimumPinLength = session.AuthenticatorInfo.MinimumPinLength;

            recordingConnection.BeginPhase("initial-retry-query-1");
            RetryObservation initialFirst = QueryRetries(session, evidence, "initial-1");
            recordingConnection.BeginPhase("initial-retry-query-2");
            RetryObservation initialSecond = QueryRetries(session, evidence, "initial-2");

            RequireSafety(
                initialFirst.RetriesRemaining == initialSecond.RetriesRemaining,
                $"Initial retry queries differed: {initialFirst.RetriesRemaining} and {initialSecond.RetriesRemaining}.");
            RequireSafety(initialFirst.RetriesRemaining >= 7, "Initial retry count must be at least 7.");
            RequireSafety(initialFirst.PowerCycleRequired != true, "Device already requires a power cycle.");
            RequireSafety(initialSecond.PowerCycleRequired != true, "Device already requires a power cycle.");
            RequireSafety(
                evidence.MinimumPinLength is null || wrongPin.Length >= evidence.MinimumPinLength.Value,
                "The established wrong PIN is shorter than this authenticator's minimum PIN length.");

            recordingConnection.BeginPhase("validated-correct-pin");
            try
            {
                evidence.ValidatedCorrectPinResult = session.TryVerifyPin(
                    correctPin,
                    PinUvAuthTokenPermissions.GetAssertion,
                    RelyingPartyId,
                    out int? validatedCorrectPinRetries,
                    out bool? validatedCorrectPinPowerCycleRequired);
                evidence.ValidatedCorrectPinRetries = validatedCorrectPinRetries;
                evidence.ValidatedCorrectPinPowerCycleRequired = validatedCorrectPinPowerCycleRequired;
            }
            catch (Exception exception)
            {
                evidence.ValidatedCorrectPinException = ExceptionObservation.From(exception);
                throw new InvalidOperationException(
                    "Safety precondition failed; known PIN validation threw before the cascade started.",
                    exception);
            }

            RequireSafety(evidence.ValidatedCorrectPinResult == true, "Known PIN validation did not succeed.");
            RequireSafety(
                recordingConnection.CountTransmittedPinTokenCommands("validated-correct-pin") == 1,
                "Known PIN validation did not transmit exactly one PIN-token command.");

            recordingConnection.BeginPhase("normalized-maximum-query");
            RetryObservation afterCorrectPin = QueryRetries(session, evidence, "after-correct-pin");
            evidence.NormalizedMaximumRetries = Math.Max(initialFirst.RetriesRemaining, afterCorrectPin.RetriesRemaining);

            RequireSafety(afterCorrectPin.PowerCycleRequired != true, "Correct PIN validation left the device blocked.");
            RequireSafety(evidence.NormalizedMaximumRetries >= 7, "Normalized maximum retry count must be at least 7.");

            int? knownMaximum = GetKnownMaximumRetries(requestedSerial);
            RequireSafety(
                knownMaximum is null || evidence.NormalizedMaximumRetries == knownMaximum.Value,
                $"Serial {requestedSerial} reported maximum {evidence.NormalizedMaximumRetries}; expected {knownMaximum}.");

            var collector = new ThreeAttemptWrongPinCollector(wrongPin, evidence);
            session.KeyCollector = collector.Collect;

            recordingConnection.BeginPhase("cascade", MaximumWrongPinCommands);
            try
            {
                evidence.CascadeReturnValue = session.TryVerifyPin(
                    PinUvAuthTokenPermissions.GetAssertion,
                    RelyingPartyId);
            }
            catch (Exception exception)
            {
                evidence.CascadeException = ExceptionObservation.From(exception);
            }

            recordingConnection.BeginPhase("after-cascade-query");
            RetryObservation afterCascade = QueryRetries(session, evidence, "after-cascade");

            recordingConnection.BeginPhase("blocked-correct-pin");
            try
            {
                evidence.BlockedCorrectPinResult = session.TryVerifyPin(
                    correctPin,
                    PinUvAuthTokenPermissions.GetAssertion,
                    RelyingPartyId,
                    out int? blockedCorrectPinRetries,
                    out bool? blockedCorrectPinPowerCycleRequired);
                evidence.BlockedCorrectPinRetries = blockedCorrectPinRetries;
                evidence.BlockedCorrectPinPowerCycleRequired = blockedCorrectPinPowerCycleRequired;
            }
            catch (Exception exception)
            {
                evidence.BlockedCorrectPinException = ExceptionObservation.From(exception);
            }

            recordingConnection.BeginPhase("after-blocked-correct-query");
            RetryObservation afterBlockedCorrect = QueryRetries(session, evidence, "after-blocked-correct");

            evidence.AfterCascadeRetries = afterCascade.RetriesRemaining;
            evidence.AfterBlockedCorrectRetries = afterBlockedCorrect.RetriesRemaining;
            evidence.AfterCascadePowerCycleRequired = afterCascade.PowerCycleRequired;
            evidence.AfterBlockedCorrectPowerCycleRequired = afterBlockedCorrect.PowerCycleRequired;
        }
        finally
        {
            session?.Dispose();
            recordingConnection?.Dispose();
            CryptographicOperations.ZeroMemory(correctPin);
            CryptographicOperations.ZeroMemory(wrongPin);
            WriteDiagnostics(evidence, recordingConnection);
        }

        Assert.NotNull(recordingConnection);

        List<CommandObservation> cascadePinCommands = recordingConnection.Transcript
            .Where(entry => entry.Phase == "cascade" && entry.IsPinTokenCommand && entry.Transmitted)
            .ToList();
        List<CommandObservation> blockedCorrectPinCommands = recordingConnection.Transcript
            .Where(entry => entry.Phase == "blocked-correct-pin" && entry.IsPinTokenCommand && entry.Transmitted)
            .ToList();

        // Firmware-robust submission bounds. On firmware that omits powerCycleState
        // (report: 5.8.0) the cascade runs the full 3 attempts and the third returns
        // 0x34; on firmware that reports powerCycleState=true the fixed SDK stops
        // after the first attempt. Either way the count is bounded by the cap.
        Assert.InRange(evidence.CollectorSubmissionCount, 1, MaximumWrongPinCommands);
        Assert.Equal(evidence.CollectorSubmissionCount, evidence.VerifyPinCallbackCount);
        Assert.Equal(0, evidence.CollectorFourthAttemptRefusalCount);
        Assert.Equal(1, evidence.ReleaseCallbackCount);
        Assert.Empty(evidence.UnexpectedCollectorRequests);

        // Every collected submission transmitted exactly one PIN-token command; the
        // 4th-command connection budget was never exceeded.
        Assert.Equal(evidence.CollectorSubmissionCount, cascadePinCommands.Count);

        // THE FIX (defect 1): the high-level path now surfaces the real CTAP status
        // instead of a status-less generic exception.
        Assert.NotNull(evidence.CascadeException);
        Assert.Equal(typeof(Fido2Exception).FullName, evidence.CascadeException.ExceptionType);
        Assert.Equal(CtapStatus.PowerCycleRequired, evidence.CascadeException.Fido2Status);
        Assert.Null(evidence.CascadeReturnValue);

        // Each transmitted wrong-PIN command decremented the persistent counter
        // exactly once, and we never consumed more retries than we transmitted.
        int cascadeCounterDelta = evidence.NormalizedMaximumRetries - evidence.AfterCascadeRetries;
        Assert.Equal(cascadePinCommands.Count, cascadeCounterDelta);
        Assert.InRange(cascadeCounterDelta, 1, MaximumWrongPinCommands);
        Assert.True(evidence.AfterCascadeRetries > 0);

        // A correct PIN submitted while the device is blocked is refused without
        // being evaluated: the token command returns 0x34, the counter is unchanged,
        // and (THE FIX) the exception now carries the status.
        Assert.Single(blockedCorrectPinCommands);
        Assert.Equal(CtapStatus.PowerCycleRequired, blockedCorrectPinCommands[0].CtapStatus);
        Assert.NotNull(evidence.BlockedCorrectPinException);
        Assert.Equal(typeof(Fido2Exception).FullName, evidence.BlockedCorrectPinException.ExceptionType);
        Assert.Equal(CtapStatus.PowerCycleRequired, evidence.BlockedCorrectPinException.Fido2Status);
        Assert.Null(evidence.BlockedCorrectPinResult);
        // powerCycleRequired is firmware-variable (report: null on 5.8.0), so it is
        // only logged, not asserted.
        Assert.Equal(evidence.AfterCascadeRetries, evidence.AfterBlockedCorrectRetries);
        Assert.True(evidence.AfterBlockedCorrectRetries > 0);

        Assert.Equal(1, recordingConnection.InnerDisposeCount);
        Assert.DoesNotContain(
            recordingConnection.Transcript,
            entry => entry.CommandType.EndsWith(".ResetCommand", StringComparison.Ordinal));
    }

    [SkippableFact]
    public void Recovery_AfterPhysicalPowerCycle_CorrectPinRestoresRetryMaximum()
    {
        int requestedSerial = RequireExactOptIn();
        IYubiKeyDevice realDevice = SelectExactHidFidoDevice(requestedSerial);

        byte[] correctPin = "11234567"u8.ToArray();
        var evidence = new CharacterizationEvidence(requestedSerial, "recovery");
        RecordingConnection? recordingConnection = null;
        Fido2Session? session = null;

        try
        {
            evidence.RecordDevice(realDevice);
            IYubiKeyConnection realConnection = realDevice.Connect(YubiKeyApplication.Fido2);
            recordingConnection = new RecordingConnection(realConnection);
            IYubiKeyDevice proxyDevice = CreateSessionDeviceProxy(realDevice, recordingConnection);
            session = new Fido2Session(proxyDevice);

            evidence.MinimumPinLength = session.AuthenticatorInfo.MinimumPinLength;

            recordingConnection.BeginPhase("recovery-before-query");
            RetryObservation beforeCorrectPin = QueryRetries(session, evidence, "recovery-before-correct");

            recordingConnection.BeginPhase("recovery-correct-pin");
            try
            {
                evidence.ValidatedCorrectPinResult = session.TryVerifyPin(
                    correctPin,
                    PinUvAuthTokenPermissions.GetAssertion,
                    RelyingPartyId,
                    out int? validatedCorrectPinRetries,
                    out bool? validatedCorrectPinPowerCycleRequired);
                evidence.ValidatedCorrectPinRetries = validatedCorrectPinRetries;
                evidence.ValidatedCorrectPinPowerCycleRequired = validatedCorrectPinPowerCycleRequired;
            }
            catch (Exception exception)
            {
                evidence.ValidatedCorrectPinException = ExceptionObservation.From(exception);
            }

            recordingConnection.BeginPhase("recovery-after-query");
            RetryObservation afterCorrectPin = QueryRetries(session, evidence, "recovery-after-correct");

            evidence.RecoveryBeforeRetries = beforeCorrectPin.RetriesRemaining;
            evidence.RecoveryBeforePowerCycleRequired = beforeCorrectPin.PowerCycleRequired;
            evidence.RecoveryAfterRetries = afterCorrectPin.RetriesRemaining;
            evidence.RecoveryAfterPowerCycleRequired = afterCorrectPin.PowerCycleRequired;
        }
        finally
        {
            session?.Dispose();
            recordingConnection?.Dispose();
            CryptographicOperations.ZeroMemory(correctPin);
            WriteDiagnostics(evidence, recordingConnection);
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

        Assert.Equal(1, recordingConnection.InnerDisposeCount);
        Assert.DoesNotContain(
            recordingConnection.Transcript,
            entry => entry.CommandType.EndsWith(".ResetCommand", StringComparison.Ordinal));
    }

    private static int RequireExactOptIn()
    {
        string? serialText = Environment.GetEnvironmentVariable(SerialEnvironmentVariable);
        string? confirmation = Environment.GetEnvironmentVariable(ConfirmationEnvironmentVariable);
        bool hasSerial = int.TryParse(serialText, NumberStyles.None, CultureInfo.InvariantCulture, out int serial) && serial > 0;
        string expectedConfirmation = hasSerial ? $"CONSUME_3_PIN_RETRIES_ON_{serial}" : string.Empty;

        Skip.IfNot(
            hasSerial && string.Equals(confirmation, expectedConfirmation, StringComparison.Ordinal),
            $"Manual hardware characterization disabled. Set {SerialEnvironmentVariable}=<serial> and " +
            $"{ConfirmationEnvironmentVariable}=CONSUME_3_PIN_RETRIES_ON_<serial> exactly.");

        return serial;
    }

    private static IYubiKeyDevice SelectExactHidFidoDevice(int requestedSerial)
    {
        var serialAllowList = new HashSet<int> { requestedSerial };
        List<IYubiKeyDevice> allowListedMatches = YubiKeyDevice
            .FindByTransport(Transport.HidFido)
            .Where(device =>
                device.AvailableTransports.HasFlag(Transport.HidFido) &&
                device.SerialNumber is int serial &&
                serialAllowList.Contains(serial))
            .ToList();

        IYubiKeyDevice selected = Assert.Single(allowListedMatches);
        Assert.True(selected.SerialNumber.HasValue);
        Assert.Equal(requestedSerial, selected.SerialNumber.Value);
        Assert.True(selected.AvailableTransports.HasFlag(Transport.HidFido));
        return selected;
    }

    private static IYubiKeyDevice CreateSessionDeviceProxy(
        IYubiKeyDevice realDevice,
        RecordingConnection recordingConnection)
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

    private static RetryObservation QueryRetries(
        Fido2Session session,
        CharacterizationEvidence evidence,
        string label)
    {
        GetPinRetriesResponse response = session.Connection.SendCommand(new GetPinRetriesCommand());
        (int retriesRemaining, bool? powerCycleRequired) = response.GetData();
        var observation = new RetryObservation(label, retriesRemaining, powerCycleRequired);
        evidence.RetryObservations.Add(observation);
        return observation;
    }

    private static int? GetKnownMaximumRetries(int serial) => serial == 103 ? 8 : null;

    private static void RequireSafety(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Safety precondition failed; no cascade started. {message}");
        }
    }

    private void WriteDiagnostics(
        CharacterizationEvidence evidence,
        RecordingConnection? recordingConnection)
    {
        _output.WriteLine("=== FIDO2 PIN retry hardware characterization ===");
        _output.WriteLine($"Mode: {evidence.Mode}");
        _output.WriteLine($"Requested/selected serial: {evidence.RequestedSerial}/{evidence.SelectedSerial?.ToString(CultureInfo.InvariantCulture) ?? "<not-selected>"}");
        _output.WriteLine($"Firmware: {evidence.FirmwareVersion ?? "<unknown>"}");
        _output.WriteLine($"Minimum PIN length: {FormatNullable(evidence.MinimumPinLength)}");
        _output.WriteLine(
            $"Normalized maximum retries: {(evidence.NormalizedMaximumRetries == 0 ? "<not-recorded>" : evidence.NormalizedMaximumRetries.ToString(CultureInfo.InvariantCulture))}");

        if (evidence.NormalizedMaximumRetries != 0 && evidence.AfterCascadeRetries != 0)
        {
            int observedDelta = evidence.NormalizedMaximumRetries - evidence.AfterCascadeRetries;
            _output.WriteLine(
                $"Cascade retry-counter delta: observed={observedDelta}, reportClaims={ReportedCounterDelta}, " +
                $"transmittedWrongPinCap={MaximumWrongPinCommands} " +
                $"({(observedDelta == ReportedCounterDelta ? "matches report" : "DIFFERS from report")})");
        }

        foreach (RetryObservation observation in evidence.RetryObservations)
        {
            _output.WriteLine(
                $"Retries[{observation.Label}]: remaining={observation.RetriesRemaining}, " +
                $"powerCycleRequired={FormatNullable(observation.PowerCycleRequired)}");
        }

        _output.WriteLine(
            $"Validated correct PIN: result={FormatNullable(evidence.ValidatedCorrectPinResult)}, " +
            $"outRetries={FormatNullable(evidence.ValidatedCorrectPinRetries)}, " +
            $"outPowerCycleRequired={FormatNullable(evidence.ValidatedCorrectPinPowerCycleRequired)}, " +
            $"exception={FormatException(evidence.ValidatedCorrectPinException)}");
        _output.WriteLine(
            $"Cascade: return={FormatNullable(evidence.CascadeReturnValue)}, " +
            $"exception={FormatException(evidence.CascadeException)}");
        _output.WriteLine(
            $"Blocked correct PIN: result={FormatNullable(evidence.BlockedCorrectPinResult)}, " +
            $"outRetries={FormatNullable(evidence.BlockedCorrectPinRetries)}, " +
            $"outPowerCycleRequired={FormatNullable(evidence.BlockedCorrectPinPowerCycleRequired)}, " +
            $"exception={FormatException(evidence.BlockedCorrectPinException)}");
        _output.WriteLine(
            $"Collector: verifyCallbacks={evidence.VerifyPinCallbackCount}, submissions={evidence.CollectorSubmissionCount}, " +
            $"fourthRefusals={evidence.CollectorFourthAttemptRefusalCount}, releases={evidence.ReleaseCallbackCount}");

        foreach (CollectorObservation callback in evidence.CollectorObservations)
        {
            _output.WriteLine(
                $"Collector[{callback.Sequence}]: request={callback.Request}, isRetry={callback.IsRetry}, " +
                $"retriesRemaining={FormatNullable(callback.RetriesRemaining)}, submitted={callback.Submitted}");
        }

        if (evidence.UnexpectedCollectorRequests.Count > 0)
        {
            _output.WriteLine($"Unexpected collector requests: {string.Join(", ", evidence.UnexpectedCollectorRequests)}");
        }

        if (recordingConnection is null)
        {
            _output.WriteLine("Connection transcript: <connection not created>");
            return;
        }

        _output.WriteLine("Connection transcript (response data only; sensitive success payloads redacted):");
        foreach (CommandObservation command in recordingConnection.Transcript)
        {
            _output.WriteLine(
                $"Command[{command.Sequence}]: phase={command.Phase}, type={command.CommandType}, " +
                $"transmitted={command.Transmitted}, statusWord={FormatStatusWord(command.StatusWord)}, " +
                $"ctapStatus={FormatCtapStatus(command.CtapStatus)}, responseData={command.ResponseData}, note={command.Note}");
        }

        _output.WriteLine($"Real connection dispose count: {recordingConnection.InnerDisposeCount}");
    }

    private static string FormatException(ExceptionObservation? observation) => observation is null
        ? "<none>"
        : $"type={observation.ExceptionType}; message={observation.Message}; status={FormatCtapStatus(observation.Fido2Status)}";

    private static string FormatStatusWord(short? statusWord) => statusWord is null
        ? "<none>"
        : $"0x{unchecked((ushort)statusWord.Value):X4}";

    private static string FormatCtapStatus(CtapStatus? status) => status is null
        ? "<none>"
        : $"0x{(byte)status.Value:X2} ({status.Value})";

    private static string FormatNullable<T>(T? value) where T : struct => value?.ToString() ?? "<null>";

    private sealed class ThreeAttemptWrongPinCollector
    {
        private readonly byte[] _wrongPin;
        private readonly CharacterizationEvidence _evidence;

        public ThreeAttemptWrongPinCollector(byte[] wrongPin, CharacterizationEvidence evidence)
        {
            _wrongPin = wrongPin;
            _evidence = evidence;
        }

        public bool Collect(KeyEntryData keyEntryData)
        {
            if (keyEntryData.Request == KeyEntryRequest.Release)
            {
                _evidence.ReleaseCallbackCount++;
                _evidence.CollectorObservations.Add(new CollectorObservation(
                    _evidence.CollectorObservations.Count + 1,
                    keyEntryData.Request,
                    keyEntryData.IsRetry,
                    keyEntryData.RetriesRemaining,
                    false));
                return true;
            }

            if (keyEntryData.Request != KeyEntryRequest.VerifyFido2Pin)
            {
                _evidence.UnexpectedCollectorRequests.Add(keyEntryData.Request);
                _evidence.CollectorObservations.Add(new CollectorObservation(
                    _evidence.CollectorObservations.Count + 1,
                    keyEntryData.Request,
                    keyEntryData.IsRetry,
                    keyEntryData.RetriesRemaining,
                    false));
                return false;
            }

            _evidence.VerifyPinCallbackCount++;
            if (_evidence.CollectorSubmissionCount >= MaximumWrongPinCommands)
            {
                _evidence.CollectorFourthAttemptRefusalCount++;
                _evidence.CollectorObservations.Add(new CollectorObservation(
                    _evidence.CollectorObservations.Count + 1,
                    keyEntryData.Request,
                    keyEntryData.IsRetry,
                    keyEntryData.RetriesRemaining,
                    false));
                return false;
            }

            keyEntryData.SubmitValue(_wrongPin);
            _evidence.CollectorSubmissionCount++;
            _evidence.CollectorObservations.Add(new CollectorObservation(
                _evidence.CollectorObservations.Count + 1,
                keyEntryData.Request,
                keyEntryData.IsRetry,
                keyEntryData.RetriesRemaining,
                true));
            return true;
        }
    }

    private sealed class RecordingConnection : IYubiKeyConnection
    {
        private readonly IYubiKeyConnection _inner;
        private int _sequence;
        private int? _phasePinTokenBudget;
        private int _phasePinTokenCommands;
        private bool _disposed;

        public RecordingConnection(IYubiKeyConnection inner)
        {
            _inner = inner;
        }

        public List<CommandObservation> Transcript { get; } = new();

        public int InnerDisposeCount { get; private set; }

        public string Phase { get; private set; } = "connection-created";

        public Yubico.YubiKey.InterIndustry.Commands.ISelectApplicationData? SelectApplicationData
        {
            get => _inner.SelectApplicationData;
            set => _inner.SelectApplicationData = value;
        }

        public void BeginPhase(string phase, int? pinTokenBudget = null)
        {
            Phase = phase;
            _phasePinTokenBudget = pinTokenBudget;
            _phasePinTokenCommands = 0;
        }

        public int CountTransmittedPinTokenCommands(string phase) => Transcript.Count(
            entry => entry.Phase == phase && entry.IsPinTokenCommand && entry.Transmitted);

        public TResponse SendCommand<TResponse>(IYubiKeyCommand<TResponse> yubiKeyCommand)
            where TResponse : IYubiKeyResponse
        {
            bool isPinTokenCommand = IsPinTokenCommand(yubiKeyCommand.GetType());
            if (isPinTokenCommand &&
                _phasePinTokenBudget is int budget &&
                _phasePinTokenCommands >= budget)
            {
                Transcript.Add(new CommandObservation(
                    ++_sequence,
                    Phase,
                    yubiKeyCommand.GetType().FullName ?? yubiKeyCommand.GetType().Name,
                    true,
                    false,
                    null,
                    null,
                    "<not-received>",
                    $"Rejected locally before transmission: phase PIN-token budget {budget} exhausted."));
                throw new InvalidOperationException(
                    $"Refusing to transmit PIN-token command {budget + 1}; phase budget is {budget}.");
            }

            if (isPinTokenCommand)
            {
                _phasePinTokenCommands++;
            }

            var observation = new CommandObservation(
                ++_sequence,
                Phase,
                yubiKeyCommand.GetType().FullName ?? yubiKeyCommand.GetType().Name,
                isPinTokenCommand,
                true,
                null,
                null,
                "<awaiting-response>",
                "Forwarded to the inner connection.");
            Transcript.Add(observation);

            try
            {
                return _inner.SendCommand(new RecordingCommand<TResponse>(yubiKeyCommand, this, observation));
            }
            catch (Exception exception)
            {
                if (observation.StatusWord is null)
                {
                    observation.Note = $"Inner send failed with {exception.GetType().FullName}.";
                }

                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _inner.Dispose();
            InnerDisposeCount++;
            _disposed = true;
        }

        private static bool IsPinTokenCommand(Type commandType) =>
            commandType == typeof(GetPinUvAuthTokenUsingPinCommand) ||
            commandType == typeof(GetPinTokenCommand);

        private void Complete(
            CommandObservation observation,
            ResponseApdu responseApdu,
            IYubiKeyResponse? response,
            Exception? parserException = null)
        {
            CtapStatus? ctapStatus = (response as Fido2Response)?.CtapStatus;
            if (ctapStatus is null && observation.IsPinTokenCommand)
            {
                ctapStatus = (CtapStatus)(responseApdu.SW & 0xFF);
            }

            observation.StatusWord = responseApdu.SW;
            observation.CtapStatus = ctapStatus;
            observation.ResponseData = ShouldRedactResponse(observation.IsPinTokenCommand, ctapStatus)
                ? $"<redacted-token-bearing-success-response:{responseApdu.Data.Length}-bytes>"
                : responseApdu.Data.IsEmpty
                    ? "<empty>"
                    : Convert.ToHexString(responseApdu.Data.Span);
            observation.Note = parserException is null
                ? "Response parsed and recorded."
                : $"Response parser failed with {parserException.GetType().FullName}.";
        }

        private static bool ShouldRedactResponse(bool isPinTokenCommand, CtapStatus? ctapStatus) =>
            isPinTokenCommand && ctapStatus == Yubico.YubiKey.Fido2.CtapStatus.Ok;

        private sealed class RecordingCommand<TResponse> : IYubiKeyCommand<TResponse>
            where TResponse : IYubiKeyResponse
        {
            private readonly IYubiKeyCommand<TResponse> _innerCommand;
            private readonly RecordingConnection _owner;
            private readonly CommandObservation _observation;

            public RecordingCommand(
                IYubiKeyCommand<TResponse> innerCommand,
                RecordingConnection owner,
                CommandObservation observation)
            {
                _innerCommand = innerCommand;
                _owner = owner;
                _observation = observation;
            }

            public YubiKeyApplication Application => _innerCommand.Application;

            public CommandApdu CreateCommandApdu() => _innerCommand.CreateCommandApdu();

            public TResponse CreateResponseForApdu(ResponseApdu responseApdu)
            {
                TResponse response;
                try
                {
                    response = _innerCommand.CreateResponseForApdu(responseApdu);
                }
                catch (Exception exception)
                {
                    _owner.Complete(_observation, responseApdu, null, exception);
                    throw;
                }

                _owner.Complete(_observation, responseApdu, response);
                return response;
            }
        }
    }

    private sealed class CharacterizationEvidence
    {
        public CharacterizationEvidence(int requestedSerial, string mode)
        {
            RequestedSerial = requestedSerial;
            Mode = mode;
        }

        public int RequestedSerial { get; }
        public string Mode { get; }
        public int? SelectedSerial { get; private set; }
        public string? FirmwareVersion { get; private set; }
        public int? MinimumPinLength { get; set; }
        public int NormalizedMaximumRetries { get; set; }
        public List<RetryObservation> RetryObservations { get; } = new();
        public List<CollectorObservation> CollectorObservations { get; } = new();
        public List<KeyEntryRequest> UnexpectedCollectorRequests { get; } = new();
        public int VerifyPinCallbackCount { get; set; }
        public int CollectorSubmissionCount { get; set; }
        public int CollectorFourthAttemptRefusalCount { get; set; }
        public int ReleaseCallbackCount { get; set; }
        public bool? ValidatedCorrectPinResult { get; set; }
        public int? ValidatedCorrectPinRetries { get; set; }
        public bool? ValidatedCorrectPinPowerCycleRequired { get; set; }
        public ExceptionObservation? ValidatedCorrectPinException { get; set; }
        public bool? CascadeReturnValue { get; set; }
        public ExceptionObservation? CascadeException { get; set; }
        public int AfterCascadeRetries { get; set; }
        public bool? AfterCascadePowerCycleRequired { get; set; }
        public bool? BlockedCorrectPinResult { get; set; }
        public int? BlockedCorrectPinRetries { get; set; }
        public bool? BlockedCorrectPinPowerCycleRequired { get; set; }
        public ExceptionObservation? BlockedCorrectPinException { get; set; }
        public int AfterBlockedCorrectRetries { get; set; }
        public bool? AfterBlockedCorrectPowerCycleRequired { get; set; }
        public int RecoveryBeforeRetries { get; set; }
        public bool? RecoveryBeforePowerCycleRequired { get; set; }
        public int RecoveryAfterRetries { get; set; }
        public bool? RecoveryAfterPowerCycleRequired { get; set; }

        public void RecordDevice(IYubiKeyDevice device)
        {
            SelectedSerial = device.SerialNumber;
            FirmwareVersion = device.VersionName;
        }
    }

    private sealed class CommandObservation
    {
        public CommandObservation(
            int sequence,
            string phase,
            string commandType,
            bool isPinTokenCommand,
            bool transmitted,
            short? statusWord,
            CtapStatus? ctapStatus,
            string responseData,
            string note)
        {
            Sequence = sequence;
            Phase = phase;
            CommandType = commandType;
            IsPinTokenCommand = isPinTokenCommand;
            Transmitted = transmitted;
            StatusWord = statusWord;
            CtapStatus = ctapStatus;
            ResponseData = responseData;
            Note = note;
        }

        public int Sequence { get; }
        public string Phase { get; }
        public string CommandType { get; }
        public bool IsPinTokenCommand { get; }
        public bool Transmitted { get; }
        public short? StatusWord { get; set; }
        public CtapStatus? CtapStatus { get; set; }
        public string ResponseData { get; set; }
        public string Note { get; set; }
    }

    private sealed record RetryObservation(string Label, int RetriesRemaining, bool? PowerCycleRequired);

    private sealed record CollectorObservation(
        int Sequence,
        KeyEntryRequest Request,
        bool IsRetry,
        int? RetriesRemaining,
        bool Submitted);

    private sealed record ExceptionObservation(
        string ExceptionType,
        string Message,
        CtapStatus? Fido2Status)
    {
        public static ExceptionObservation From(Exception exception) => new(
            exception.GetType().FullName ?? exception.GetType().Name,
            exception.Message,
            (exception as Fido2Exception)?.Status);
    }
}
