// Copyright 2025 Yubico AB
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

using Microsoft.Extensions.Logging;

namespace Yubico.YubiKit.Core.Transports.Hid.Linux;

/// <summary>
/// Linux implementation of HID device listener using udev_monitor with poll().
/// </summary>
/// <remarks>
/// <para>
/// The listener does not auto-start. Call <see cref="Start"/> after setting up
/// <see cref="HidDeviceListener.DeviceEvent"/> callback.
/// </para>
/// <para>
/// Each <see cref="Start"/> creates an independent session that owns its native resources
/// (udev context, monitor, shutdown event fd). The session's listener thread disposes those
/// resources when its loop exits. If <see cref="Stop"/> times out waiting for a wedged thread,
/// the cancelled session is abandoned rather than torn down, so a late-exiting thread can never
/// touch recycled file descriptors or state belonging to a newer session.
/// </para>
/// </remarks>
internal sealed class LinuxHidDeviceListener : HidDeviceListener
{
    private static readonly TimeSpan HidrawReadinessFallbackDelay = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan MaxDisposalWaitTime = TimeSpan.FromSeconds(8);

    private static readonly ILogger Logger = YubiKitLogging.CreateLogger<LinuxHidDeviceListener>();

    private readonly Lock _syncLock = new();
    private readonly Func<ILinuxHidEventSource> _eventSourceFactory;
    private ListenerSession? _session;
    private bool _disposed;

    /// <summary>
    /// Creates a new instance. The listener does not start automatically - call <see cref="Start"/>
    /// after setting up the <see cref="HidDeviceListener.DeviceEvent"/> callback.
    /// </summary>
    public LinuxHidDeviceListener()
        : this(null)
    {
    }

    /// <summary>
    /// Creates a new instance with a custom event source factory. Used by tests to inject a
    /// fake <see cref="ILinuxHidEventSource"/> for no-hardware fault injection.
    /// </summary>
    /// <param name="eventSourceFactory">
    /// Factory invoked once per <see cref="Start"/> to create the session's event source,
    /// or null to use the udev-backed production source.
    /// </param>
    internal LinuxHidDeviceListener(Func<ILinuxHidEventSource>? eventSourceFactory)
    {
        _eventSourceFactory = eventSourceFactory ?? (static () => new LinuxUdevHidEventSource());
    }

    /// <inheritdoc />
    public override void Start()
    {
        lock (_syncLock)
        {
            if (Status == DeviceListenerStatus.Started)
            {
                return;
            }

            // A previous session's thread may still be exiting after a Stop() join timeout.
            // It is cancelled and owns its own cleanup; start a fresh, independent session.
            if (_session?.Thread is { IsAlive: true })
            {
                Logger.LogWarning("Previous Linux HID listener thread is still exiting; starting a new session");
            }

            var source = _eventSourceFactory();
            try
            {
                if (!source.Initialize())
                {
                    Status = DeviceListenerStatus.Error;
                    source.Dispose();
                    return;
                }

                var session = new ListenerSession(source);
                var thread = new Thread(() => ListenerThreadProc(session))
                {
                    Name = "LinuxHidDeviceListener",
                    IsBackground = true
                };
                session.Thread = thread;

                _session = session;
                Status = DeviceListenerStatus.Started;
                thread.Start();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to start Linux HID listener");
                Status = DeviceListenerStatus.Error;
                _session = null;
                source.Dispose();
            }
        }
    }

    /// <inheritdoc />
    public override void Stop()
    {
        lock (_syncLock)
        {
            if (Status == DeviceListenerStatus.Stopped)
            {
                return;
            }

            var session = _session;
            if (session is not null)
            {
                session.Cancel();
                session.Source.SignalShutdown();

                if (session.Thread is { IsAlive: true } thread && !thread.Join(MaxDisposalWaitTime))
                {
                    // Abandon rather than tear down: the cancelled session owns its handles and
                    // disposes them when its thread finally unblocks. Freeing them here would let
                    // a still-running thread poll recycled file descriptors.
                    Logger.LogError(
                        "Linux HID listener thread did not exit within {Timeout}; abandoning its session resources",
                        MaxDisposalWaitTime);
                }

                _session = null;
            }

            Status = DeviceListenerStatus.Stopped;
        }
    }

    private void ListenerThreadProc(ListenerSession session)
    {
        try
        {
            RunEventLoop(session);
        }
        catch (Exception ex)
        {
            if (!session.Cancelled)
            {
                Logger.LogError(ex, "Linux HID listener thread encountered an error");
                SetErrorStatus(session);
            }
        }
        finally
        {
            session.Source.Dispose();
        }
    }

    private void RunEventLoop(ListenerSession session)
    {
        while (!session.Cancelled)
        {
            var outcome = session.Source.WaitForEvent();

            if (session.Cancelled)
            {
                return;
            }

            switch (outcome)
            {
                case LinuxHidPollOutcome.Event:
                    ProcessUdevEvent(session);
                    break;

                case LinuxHidPollOutcome.Retry:
                    break;

                case LinuxHidPollOutcome.ShutdownSignaled:
                    return;

                case LinuxHidPollOutcome.ShutdownFdError:
                case LinuxHidPollOutcome.MonitorFdError:
                case LinuxHidPollOutcome.PollFailed:
                    SetErrorStatus(session);
                    return;

                default:
                    Logger.LogWarning("Unexpected Linux HID poll outcome: {Outcome}", outcome);
                    SetErrorStatus(session);
                    return;
            }
        }
    }

    /// <summary>
    /// Transitions the listener to <see cref="DeviceListenerStatus.Error"/> unless the session
    /// was cancelled, so an abandoned (zombie) session can never stamp Error onto a newer session.
    /// </summary>
    private void SetErrorStatus(ListenerSession session)
    {
        if (!session.Cancelled)
        {
            Status = DeviceListenerStatus.Error;
        }
    }

    private void ProcessUdevEvent(ListenerSession session)
    {
        try
        {
            var udevEvent = session.Source.ReceiveEvent();
            if (udevEvent is null)
            {
                // Receive failures matter most during event storms (e.g. ENOBUFS after the kernel
                // dropped notifications) — exactly when a removal may have been lost. Never
                // suppress: hint a rescan so discovery re-syncs with reality.
                Logger.LogWarning("Failed to receive udev event; emitting unknown-change rescan hint");
                OnDeviceEvent(HidDeviceRescanHint.Unknown);
                return;
            }

            switch (udevEvent.Action)
            {
                case "add":
                    HandleDeviceAdd(session, udevEvent);
                    break;

                case "remove":
                    HandleDeviceRemove(udevEvent);
                    break;

                case null:
                    Logger.LogDebug("udev event missing action; emitting unknown-change rescan hint");
                    OnDeviceEvent(HidDeviceRescanHint.Unknown);
                    break;

                default:
                    // Recognized non-topology actions ("change", "bind", ...) do not affect discovery.
                    Logger.LogTrace(
                        "Ignoring udev action '{Action}' for {Identity}",
                        udevEvent.Action,
                        udevEvent.StableIdentity);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to process udev event; emitting unknown-change rescan hint");
            OnDeviceEvent(HidDeviceRescanHint.Unknown);
        }
    }

    private void HandleDeviceAdd(ListenerSession session, LinuxHidUdevEvent udevEvent)
    {
        var hint = new HidDeviceRescanHint(HidDeviceChangeKind.Added, udevEvent.StableIdentity, udevEvent.DevNode);
        OnDeviceEvent(hint);

        if (!session.Source.IsHidrawReady(hint.DevicePath))
        {
            QueueReadinessFallback(session, hint);
        }
    }

    private void HandleDeviceRemove(LinuxHidUdevEvent udevEvent)
    {
        var hint = new HidDeviceRescanHint(HidDeviceChangeKind.Removed, udevEvent.StableIdentity, udevEvent.DevNode);
        OnDeviceEvent(hint);
    }

    private void QueueReadinessFallback(ListenerSession session, HidDeviceRescanHint hint)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(HidrawReadinessFallbackDelay).ConfigureAwait(false);

                if (session.Cancelled || Status != DeviceListenerStatus.Started)
                {
                    return;
                }

                OnDeviceEvent(hint);
            }
            catch (Exception ex)
            {
                Logger.LogTrace(ex, "Ignored delayed hidraw readiness rescan hint");
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            base.Dispose(disposing);
            return;
        }

        _disposed = true;

        if (disposing)
        {
            Stop();
        }
        else
        {
            // Finalizer path: cancel and wake the session; its thread owns handle cleanup.
            var session = _session;
            if (session is not null)
            {
                session.Cancel();
                session.Source.SignalShutdown();
            }
        }

        base.Dispose(disposing);
    }

    ~LinuxHidDeviceListener()
    {
        Dispose(disposing: false);
    }

    /// <summary>
    /// Per-<see cref="Start"/> state: the event source, its listener thread, and the
    /// cancellation flag. Sessions are never reused; a session's thread is the sole owner of
    /// its source's disposal.
    /// </summary>
    private sealed class ListenerSession
    {
        private volatile bool _cancelled;

        public ListenerSession(ILinuxHidEventSource source)
        {
            Source = source;
        }

        public ILinuxHidEventSource Source { get; }

        public Thread? Thread { get; set; }

        public bool Cancelled => _cancelled;

        public void Cancel() => _cancelled = true;
    }
}