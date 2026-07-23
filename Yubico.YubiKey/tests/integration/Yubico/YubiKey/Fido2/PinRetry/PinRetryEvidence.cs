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
using Xunit.Abstractions;

namespace Yubico.YubiKey.Fido2.PinRetry;

/// <summary>One <c>getPINRetries</c> reading.</summary>
internal sealed record RetryObservation(string Label, int RetriesRemaining, bool? PowerCycleRequired);

/// <summary>One invocation of the test <c>KeyCollector</c>.</summary>
internal sealed record CollectorObservation(int Sequence, KeyEntryRequest Request, bool IsRetry, int? RetriesRemaining, bool Submitted);

/// <summary>The type, message, and (if any) CTAP status of an exception the SDK threw.</summary>
internal sealed record ExceptionObservation(string ExceptionType, string Message, CtapStatus? Fido2Status)
{
    public static ExceptionObservation From(Exception exception) => new(
        exception.GetType().FullName ?? exception.GetType().Name,
        exception.Message,
        (exception as Fido2Exception)?.Status);
}

/// <summary>
/// Collects everything observed during a PIN retry hardware run — retry readings, collector
/// callbacks, SDK results/exceptions, and derived counts — and renders it as a diagnostic
/// transcript. The test asserts against these properties after the run completes.
/// </summary>
internal sealed class PinRetryEvidence
{
    /// <summary>The retry-counter decrement the original bug report observed across the cascade (8 -> 5).</summary>
    public const int ReportedCounterDelta = 3;

    public PinRetryEvidence(int requestedSerial, string mode, int wrongPinCap)
    {
        RequestedSerial = requestedSerial;
        Mode = mode;
        WrongPinCap = wrongPinCap;
    }

    public int RequestedSerial { get; }
    public string Mode { get; }
    public int WrongPinCap { get; }
    public int? SelectedSerial { get; private set; }
    public string? FirmwareVersion { get; private set; }
    public int? MinimumPinLength { get; set; }
    public int NormalizedMaximumRetries { get; set; }

    public IReadOnlyList<RetryObservation> RetryObservations => _retryObservations;
    private readonly List<RetryObservation> _retryObservations = new();

    public IReadOnlyList<CollectorObservation> CollectorObservations => _collectorObservations;
    private readonly List<CollectorObservation> _collectorObservations = new();

    public IReadOnlyList<KeyEntryRequest> UnexpectedCollectorRequests => _unexpectedCollectorRequests;
    private readonly List<KeyEntryRequest> _unexpectedCollectorRequests = new();

    public int VerifyPinCallbackCount { get; private set; }
    public int CollectorSubmissionCount { get; private set; }
    public int CollectorFourthAttemptRefusalCount { get; private set; }
    public int ReleaseCallbackCount { get; private set; }

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

    public RetryObservation RecordRetries(string label, int retriesRemaining, bool? powerCycleRequired)
    {
        var observation = new RetryObservation(label, retriesRemaining, powerCycleRequired);
        _retryObservations.Add(observation);
        return observation;
    }

    // ---- KeyCollector bookkeeping -------------------------------------------

    public void RecordSubmission(KeyEntryData data)
    {
        VerifyPinCallbackCount++;
        CollectorSubmissionCount++;
        AddCollectorObservation(data, submitted: true);
    }

    public void RecordRefusedFourthAttempt(KeyEntryData data)
    {
        VerifyPinCallbackCount++;
        CollectorFourthAttemptRefusalCount++;
        AddCollectorObservation(data, submitted: false);
    }

    public void RecordRelease(KeyEntryData data)
    {
        ReleaseCallbackCount++;
        AddCollectorObservation(data, submitted: false);
    }

    public void RecordUnexpectedRequest(KeyEntryData data)
    {
        _unexpectedCollectorRequests.Add(data.Request);
        AddCollectorObservation(data, submitted: false);
    }

    private void AddCollectorObservation(KeyEntryData data, bool submitted) =>
        _collectorObservations.Add(new CollectorObservation(
            _collectorObservations.Count + 1, data.Request, data.IsRetry, data.RetriesRemaining, submitted));

    // ---- Diagnostics --------------------------------------------------------

    public void WriteTo(ITestOutputHelper output, RecordingConnection? connection)
    {
        output.WriteLine("=== FIDO2 PIN retry hardware run ===");
        output.WriteLine($"Mode: {Mode}");
        output.WriteLine($"Requested/selected serial: {RequestedSerial}/{SelectedSerial?.ToString(CultureInfo.InvariantCulture) ?? "<not-selected>"}");
        output.WriteLine($"Firmware: {FirmwareVersion ?? "<unknown>"}");
        output.WriteLine($"Minimum PIN length: {Nullable(MinimumPinLength)}");
        output.WriteLine($"Normalized maximum retries: {(NormalizedMaximumRetries == 0 ? "<not-recorded>" : NormalizedMaximumRetries.ToString(CultureInfo.InvariantCulture))}");

        if (NormalizedMaximumRetries != 0 && AfterCascadeRetries != 0)
        {
            int observedDelta = NormalizedMaximumRetries - AfterCascadeRetries;
            string match = observedDelta == ReportedCounterDelta ? "matches report" : "DIFFERS from report";
            output.WriteLine($"Cascade retry-counter delta: observed={observedDelta}, reportClaims={ReportedCounterDelta}, wrongPinCap={WrongPinCap} ({match})");
        }

        foreach (RetryObservation retry in _retryObservations)
        {
            output.WriteLine($"Retries[{retry.Label}]: remaining={retry.RetriesRemaining}, powerCycleRequired={Nullable(retry.PowerCycleRequired)}");
        }

        output.WriteLine($"Validated correct PIN: {DescribeAttempt(ValidatedCorrectPinResult, ValidatedCorrectPinRetries, ValidatedCorrectPinPowerCycleRequired, ValidatedCorrectPinException)}");
        output.WriteLine($"Cascade: return={Nullable(CascadeReturnValue)}, exception={Exception(CascadeException)}");
        output.WriteLine($"Blocked correct PIN: {DescribeAttempt(BlockedCorrectPinResult, BlockedCorrectPinRetries, BlockedCorrectPinPowerCycleRequired, BlockedCorrectPinException)}");
        output.WriteLine($"Collector: verifyCallbacks={VerifyPinCallbackCount}, submissions={CollectorSubmissionCount}, fourthRefusals={CollectorFourthAttemptRefusalCount}, releases={ReleaseCallbackCount}");

        foreach (CollectorObservation callback in _collectorObservations)
        {
            output.WriteLine($"Collector[{callback.Sequence}]: request={callback.Request}, isRetry={callback.IsRetry}, retriesRemaining={Nullable(callback.RetriesRemaining)}, submitted={callback.Submitted}");
        }

        if (_unexpectedCollectorRequests.Count > 0)
        {
            output.WriteLine($"Unexpected collector requests: {string.Join(", ", _unexpectedCollectorRequests)}");
        }

        if (connection is null)
        {
            output.WriteLine("Connection transcript: <connection not created>");
            return;
        }

        output.WriteLine("Connection transcript (response data only; sensitive success payloads redacted):");
        foreach (CommandObservation command in connection.Transcript)
        {
            output.WriteLine(
                $"Command[{command.Sequence}]: phase={command.Phase}, type={command.CommandType}, transmitted={command.Transmitted}, " +
                $"statusWord={StatusWord(command.StatusWord)}, ctapStatus={CtapStatusText(command.CtapStatus)}, responseData={command.ResponseData}, note={command.Note}");
        }

        output.WriteLine($"Real connection dispose count: {connection.InnerDisposeCount}");
    }

    private static string DescribeAttempt(bool? result, int? retries, bool? powerCycleRequired, ExceptionObservation? exception) =>
        $"result={Nullable(result)}, outRetries={Nullable(retries)}, outPowerCycleRequired={Nullable(powerCycleRequired)}, exception={Exception(exception)}";

    private static string Exception(ExceptionObservation? observation) => observation is null
        ? "<none>"
        : $"type={observation.ExceptionType}; message={observation.Message}; status={CtapStatusText(observation.Fido2Status)}";

    private static string StatusWord(short? statusWord) => statusWord is null
        ? "<none>"
        : $"0x{unchecked((ushort)statusWord.Value):X4}";

    private static string CtapStatusText(CtapStatus? status) => status is null
        ? "<none>"
        : $"0x{(byte)status.Value:X2} ({status.Value})";

    private static string Nullable<T>(T? value) where T : struct => value?.ToString() ?? "<null>";
}
