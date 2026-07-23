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
using System.Linq;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Fido2.Commands;

namespace Yubico.YubiKey.Fido2.PinRetry;

/// <summary>
/// One command/response pair observed by <see cref="RecordingConnection"/>.
/// </summary>
/// <remarks>
/// The status word and CTAP status are filled in once the response is parsed, so the
/// mutable properties start as placeholders and are completed by the connection.
/// </remarks>
internal sealed class CommandObservation
{
    public CommandObservation(int sequence, string phase, string commandType, bool isPinTokenCommand, bool transmitted)
    {
        Sequence = sequence;
        Phase = phase;
        CommandType = commandType;
        IsPinTokenCommand = isPinTokenCommand;
        Transmitted = transmitted;
        ResponseData = transmitted ? "<awaiting-response>" : "<not-received>";
        Note = transmitted ? "Forwarded to the inner connection." : string.Empty;
    }

    public int Sequence { get; }
    public string Phase { get; }
    public string CommandType { get; }

    /// <summary>True for the PIN-verifying token commands whose transmission is safety-budgeted.</summary>
    public bool IsPinTokenCommand { get; }

    /// <summary>False only when the command was rejected locally before it reached the device.</summary>
    public bool Transmitted { get; }

    public short? StatusWord { get; set; }
    public CtapStatus? CtapStatus { get; set; }
    public string ResponseData { get; set; }
    public string Note { get; set; }
}

/// <summary>
/// An <see cref="IYubiKeyConnection"/> decorator that records the raw status word, CTAP
/// status, and (non-sensitive) response bytes of every command a <see cref="Fido2Session"/>
/// issues, without changing what the session itself sees. It also enforces a per-phase
/// budget on PIN-token commands so a hardware test can never transmit more wrong-PIN
/// attempts than intended, even if the SDK or firmware behaves unexpectedly.
/// </summary>
internal sealed class RecordingConnection : IYubiKeyConnection
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

    /// <summary>Every command observed, in the order it was issued.</summary>
    public IReadOnlyList<CommandObservation> Transcript => _transcript;
    private readonly List<CommandObservation> _transcript = new();

    /// <summary>Number of times the wrapped connection was disposed (must end at exactly 1).</summary>
    public int InnerDisposeCount { get; private set; }

    /// <summary>The label applied to commands recorded until the next <see cref="BeginPhase"/>.</summary>
    public string Phase { get; private set; } = "connection-created";

    public InterIndustry.Commands.ISelectApplicationData? SelectApplicationData
    {
        get => _inner.SelectApplicationData;
        set => _inner.SelectApplicationData = value;
    }

    /// <summary>
    /// Start a new named phase. When <paramref name="pinTokenBudget"/> is set, at most that
    /// many PIN-token commands may be transmitted during the phase; the next one is rejected
    /// locally (recorded, then an exception is thrown) before it reaches the device.
    /// </summary>
    public void BeginPhase(string phase, int? pinTokenBudget = null)
    {
        Phase = phase;
        _phasePinTokenBudget = pinTokenBudget;
        _phasePinTokenCommands = 0;
    }

    public int CountTransmittedPinTokenCommands(string phase) =>
        _transcript.Count(entry => entry.Phase == phase && entry.IsPinTokenCommand && entry.Transmitted);

    public TResponse SendCommand<TResponse>(IYubiKeyCommand<TResponse> yubiKeyCommand)
        where TResponse : IYubiKeyResponse
    {
        Type commandType = yubiKeyCommand.GetType();
        bool isPinTokenCommand = IsPinTokenCommand(commandType);
        string commandTypeName = commandType.FullName ?? commandType.Name;

        // Safety interlock: never transmit more PIN-token commands than the phase budget allows.
        if (isPinTokenCommand && _phasePinTokenBudget is int budget && _phasePinTokenCommands >= budget)
        {
            var rejected = new CommandObservation(++_sequence, Phase, commandTypeName, isPinTokenCommand: true, transmitted: false)
            {
                Note = $"Rejected locally before transmission: phase PIN-token budget {budget} exhausted."
            };
            _transcript.Add(rejected);
            throw new InvalidOperationException(
                $"Refusing to transmit PIN-token command {budget + 1}; phase budget is {budget}.");
        }

        if (isPinTokenCommand)
        {
            _phasePinTokenCommands++;
        }

        var observation = new CommandObservation(++_sequence, Phase, commandTypeName, isPinTokenCommand, transmitted: true);
        _transcript.Add(observation);

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

    private void Complete(CommandObservation observation, ResponseApdu responseApdu, IYubiKeyResponse? response, Exception? parserException)
    {
        // Prefer the parsed Fido2Response status; fall back to the low byte of the status
        // word for PIN-token commands, whose error responses have no parseable body.
        CtapStatus? ctapStatus = (response as Fido2Response)?.CtapStatus;
        if (ctapStatus is null && observation.IsPinTokenCommand)
        {
            ctapStatus = (CtapStatus)(responseApdu.SW & 0xFF);
        }

        observation.StatusWord = responseApdu.SW;
        observation.CtapStatus = ctapStatus;
        observation.ResponseData = DescribeResponseData(observation.IsPinTokenCommand, ctapStatus, responseApdu);
        observation.Note = parserException is null
            ? "Response parsed and recorded."
            : $"Response parser failed with {parserException.GetType().FullName}.";
    }

    // A successful PIN-token response carries the encrypted auth token; never log it.
    private static string DescribeResponseData(bool isPinTokenCommand, CtapStatus? ctapStatus, ResponseApdu responseApdu)
    {
        if (isPinTokenCommand && ctapStatus == CtapStatus.Ok)
        {
            return $"<redacted-token-bearing-success-response:{responseApdu.Data.Length}-bytes>";
        }

        return responseApdu.Data.IsEmpty ? "<empty>" : Convert.ToHexString(responseApdu.Data.Span);
    }

    /// <summary>
    /// Wraps the real command so the response APDU can be inspected as it is parsed. The
    /// command APDU and the typed response handed back to the session are unchanged; this
    /// decorator is purely observational.
    /// </summary>
    private sealed class RecordingCommand<TResponse> : IYubiKeyCommand<TResponse>
        where TResponse : IYubiKeyResponse
    {
        private readonly IYubiKeyCommand<TResponse> _innerCommand;
        private readonly RecordingConnection _owner;
        private readonly CommandObservation _observation;

        public RecordingCommand(IYubiKeyCommand<TResponse> innerCommand, RecordingConnection owner, CommandObservation observation)
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
                _owner.Complete(_observation, responseApdu, response: null, parserException: exception);
                throw;
            }

            _owner.Complete(_observation, responseApdu, response, parserException: null);
            return response;
        }
    }
}
