// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Microsoft.Extensions.Logging;
using Spectre.Console;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.Cli.Shared.Device;

/// <summary>
/// Discovers complete device views, filters eligibility, and selects or prompts according to the caller's policy.
/// Metadata enrichment is best-effort and local to each selection invocation, not authoritative live configuration.
/// </summary>
public sealed class DeviceSelector
{
    private static readonly ILogger Logger = YubiKitLogging.LoggerFactory.CreateLogger<DeviceSelector>();
    private readonly ConnectionType[] _transports;
    private readonly NonInteractiveSelection _nonInteractiveSelection;
    private readonly int? _serialNumber;
    private readonly IAnsiConsole _console;
    private readonly Func<CancellationToken, Task<IReadOnlyList<IYubiKey>>> _findAll;
    private readonly Func<IYubiKey, CancellationToken, Task<DeviceInfo?>> _readInfo;

    /// <param name="supportedTransports">Ordered concrete transports this endpoint supports.</param>
    /// <param name="nonInteractiveSelection">Explicit policy for multiple devices without interaction.</param>
    /// <param name="serialNumber">Optional target serial number.</param>
    /// <param name="requestedTransport">Optional restriction, not a discovery scope or a trimmed device view.</param>
    /// <param name="console">Console for prompts and diagnostics; defaults to the current terminal.</param>
    public DeviceSelector(
        ConnectionType[] supportedTransports,
        NonInteractiveSelection nonInteractiveSelection,
        int? serialNumber = null,
        ConnectionType? requestedTransport = null,
        IAnsiConsole? console = null)
        : this(supportedTransports, nonInteractiveSelection, console ?? AnsiConsole.Console,
            YubiKeyManager.FindAllAsync,
            async (device, token) => await device.GetDeviceInfoAsync(
                new SessionCreationOptions { PreferredConnectionType = requestedTransport }, token),
            serialNumber, requestedTransport)
    {
    }

    // Only discovery and the live read are substituted in tests; filtering, retry, metadata reuse,
    // selection, and the real Spectre prompt all remain on the production path.
    internal DeviceSelector(
        ConnectionType[] supportedTransports,
        NonInteractiveSelection nonInteractiveSelection,
        IAnsiConsole console,
        Func<CancellationToken, Task<IReadOnlyList<IYubiKey>>> findAll,
        Func<IYubiKey, CancellationToken, Task<DeviceInfo?>> readInfo,
        int? serialNumber = null,
        ConnectionType? requestedTransport = null)
    {
        ArgumentNullException.ThrowIfNull(supportedTransports);
        if (supportedTransports.Length == 0 || supportedTransports.Any(type => type is not
                (ConnectionType.SmartCard or ConnectionType.HidFido or ConnectionType.HidOtp)))
        {
            throw new ArgumentException("Supply at least one concrete supported transport.", nameof(supportedTransports));
        }

        if (requestedTransport is { } transport && !supportedTransports.Contains(transport))
        {
            throw new ArgumentException(
                $"Transport {ConnectionTypeFormatter.Format(transport)} is not supported by this command.",
                nameof(requestedTransport));
        }

        _transports = requestedTransport is { } requested ? [requested] : [.. supportedTransports];
        _nonInteractiveSelection = nonInteractiveSelection;
        _serialNumber = serialNumber;
        _console = console;
        _findAll = findAll;
        _readInfo = readInfo;
    }

    /// <summary>Selects a device, or returns null when none match or an explicit choice is declined/unavailable.</summary>
    /// <param name="cancellationToken">Cancellation propagates, including from best-effort metadata reads.</param>
    /// <param name="allowInteraction">False for command paths that must never prompt, even on a terminal.</param>
    public async Task<DeviceSelection?> SelectDeviceAsync(
        CancellationToken cancellationToken = default,
        bool allowInteraction = true)
    {
        bool interactive = allowInteraction && _console.Profile.Capabilities.Interactive;
        var candidates = await FindCandidatesWithRetryAsync(interactive, cancellationToken);
        if (candidates.Count == 0)
        {
            return null;
        }

        if (candidates.Count == 1)
        {
            return await GetSelectionAsync(candidates[0], cancellationToken);
        }

        if (interactive)
        {
            return await PromptAsync(candidates, cancellationToken);
        }

        var selected = _nonInteractiveSelection switch
        {
            NonInteractiveSelection.FirstEligible => candidates[0],
            NonInteractiveSelection.PreferFirstTransport =>
                candidates.FirstOrDefault(candidate => candidate.Device.SupportsConnection(_transports[0])) ?? candidates[0],
            _ => null
        };

        if (selected is not null)
        {
            return await GetSelectionAsync(selected, cancellationToken);
        }

        _console.MarkupLine("[red]Multiple YubiKeys detected. An explicit selection is required; use --serial where available.[/]");
        return null;
    }

    private async Task<List<Candidate>> FindCandidatesWithRetryAsync(bool interactive, CancellationToken cancellationToken)
    {
        // A retry can return the same reference. Even failed enrichment is attempted at most once per invocation.
        var observed = new Dictionary<IYubiKey, Candidate>(ReferenceEqualityComparer.Instance);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var devices = await _findAll(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var candidates = new List<Candidate>();
            foreach (var device in devices.Where(device => _transports.Any(device.SupportsConnection)))
            {
                if (!observed.TryGetValue(device, out var candidate))
                {
                    candidate = new Candidate(device);
                    observed.Add(device, candidate);
                }

                if (await MatchesSerialAsync(candidate, cancellationToken))
                {
                    candidates.Add(candidate);
                }
            }

            if (candidates.Count > 0)
            {
                return candidates;
            }

            _console.MarkupLine(_serialNumber is { } serial
                ? $"[red]No YubiKey found with serial number {serial}.[/]"
                : "[red]No YubiKey detected. Please insert a YubiKey and try again.[/]");
            _console.MarkupLine($"[grey]Supported transports: {string.Join(", ", _transports.Select(ConnectionTypeFormatter.Format))}[/]");
            if (!interactive || !await new ConfirmationPrompt("Retry?") { DefaultValue = true }
                    .ShowAsync(_console, cancellationToken))
            {
                return [];
            }
        }
    }

    private async Task<bool> MatchesSerialAsync(Candidate candidate, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_serialNumber is not { } target)
        {
            return true;
        }

        // Latched identity takes precedence over metadata, including a newer serial-less snapshot.
        if (candidate.SerialNumber is { } known)
        {
            return known == target;
        }

        await EnsureInfoAsync(candidate, cancellationToken);
        return candidate.SerialNumber == target;
    }

    private async Task EnsureInfoAsync(Candidate candidate, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (candidate.Info is not null || candidate.LiveReadAttempted)
        {
            return;
        }

        candidate.LiveReadAttempted = true;
        try
        {
            candidate.Info = await _readInfo(candidate.Device, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Logger.LogDebug(ex, "{ConnectionType} device info query failed", candidate.Device.AvailableConnections);
        }
    }

    private async Task<DeviceSelection> GetSelectionAsync(Candidate candidate, CancellationToken cancellationToken)
    {
        await EnsureInfoAsync(candidate, cancellationToken);
        return new DeviceSelection(candidate.Device, candidate.SerialNumber,
            candidate.Info?.FormFactor ?? FormFactor.Unknown,
            candidate.Info?.FirmwareVersion.ToString() ?? "Unknown", candidate.Device.AvailableConnections)
        {
            Info = candidate.Info
        };
    }

    private async Task<DeviceSelection?> PromptAsync(List<Candidate> candidates, CancellationToken cancellationToken)
    {
        var selections = new List<DeviceSelection>();
        // Ambiguous views can be siblings of the same key: never overlap live fallback reads.
        foreach (var candidate in candidates)
        {
            selections.Add(await GetSelectionAsync(candidate, cancellationToken));
        }

        var labels = selections.Select(FormatChoice).ToList();
        var indices = Enumerable.Range(0, labels.Count).OrderBy(index => labels[index]).ToList();
        indices.Add(-1);
        // Select by index, not label equality: identical labels must still select distinct devices.
        int selected = await new SelectionPrompt<int>()
            .Title("Multiple YubiKeys detected. Select one:")
            .PageSize(10)
            .UseConverter(index => index < 0 ? "Cancel" : Markup.Escape(labels[index]))
            .AddChoices(indices)
            .ShowAsync(_console, cancellationToken);
        return selected < 0 ? null : selections[selected];
    }

    private static string FormatChoice(DeviceSelection selection) =>
        $"YubiKey {FormFactorFormatter.Format(selection.FormFactor)} - Serial: {selection.SerialNumber?.ToString() ?? "N/A"}, " +
        $"Firmware: {selection.FirmwareVersion} ({ConnectionTypeFormatter.Format(selection.ConnectionType)})";

    private sealed class Candidate(IYubiKey device)
    {
        public IYubiKey Device { get; } = device;
        public DeviceInfo? Info { get; set; } = device.CachedDeviceInfo;
        public bool LiveReadAttempted { get; set; }
        public int? SerialNumber => Device.SerialNumber ?? Info?.SerialNumber;
    }
}