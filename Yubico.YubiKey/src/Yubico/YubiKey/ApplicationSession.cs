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

using System;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Yubico.YubiKey.InterIndustry.Commands;
using Yubico.YubiKey.Scp;

namespace Yubico.YubiKey;

/// <summary>
///     Abstract base class for sessions with a YubiKey. This class is used
///     to wrap the <c>IYubiKeyConnection</c> and provide a way of
///     interacting with the connection that is more convenient for most
///     users.
/// </summary>
public abstract class ApplicationSession : IDisposable
{
    private bool _disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ApplicationSession" /> class with logging, YubiKey device,
    ///     application, and optional SCP key parameters.
    /// </summary>
    /// <param name="logger">The logger instance used for logging information.</param>
    /// <param name="device">The YubiKey device to establish a session with.</param>
    /// <param name="application">The specific YubiKey application to connect to.</param>
    /// <param name="keyParameters">The optional parameters for an SCP connection.</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="device" /> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the Yubikey does not support the requested SCP connection.</exception>
    protected ApplicationSession(
        ILogger logger,
        IYubiKeyDevice device,
        YubiKeyApplication application,
        ScpKeyParameters? keyParameters)
    {
        Logger = logger;
        YubiKey = device ?? throw new ArgumentNullException(nameof(device));
        Application = application;

        KeyParameters = keyParameters;
        Connection = GetConnection(YubiKey, Application, KeyParameters);
    }

    /// <summary>
    ///     The object that represents the connection to the YubiKey. Most
    ///     applications will ignore this, but it can be used to issue commands
    ///     directly.
    /// </summary>
    /// <remarks>
    ///     This property gives you direct access to the existing connection to the YubiKey using the
    ///     <see cref="IYubiKeyConnection" /> interface. To send your own commands, call the
    ///     <see cref="IYubiKeyConnection.SendCommand{TResponse}" />
    /// </remarks>
    public IYubiKeyConnection Connection { get; protected set; }

    /// <summary>
    ///     Gets the parameters used for establishing a Secure Channel Protocol (SCP) connection.
    /// </summary>
    public ScpKeyParameters? KeyParameters { get; }

    /// <summary>
    ///     The specific YubiKey application to connect to.
    /// </summary>
    public YubiKeyApplication Application { get; }

    /// <summary>
    ///     The logger instance used for logging information.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    ///     The YubiKey device to establish a session with.
    /// </summary>
    protected IYubiKeyDevice YubiKey { get; }

    #region IDisposable Members

    /// <summary>
    ///     When the ApplicationSession object goes out of scope, this method is called.
    ///     It will close the session. The most important function of closing a
    ///     session is to "un-authenticate" the management key and "un-verify"
    ///     the PIN.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion

    protected IYubiKeyConnection GetConnection(
        IYubiKeyDevice yubiKey,
        YubiKeyApplication application,
        ScpKeyParameters? keyParameters)
    {
        string scpType = keyParameters switch
        {
            Scp03KeyParameters when application == YubiKeyApplication.Oath &&
                yubiKey.HasFeature(YubiKeyFeature.Scp03Oath)
                => "SCP03",
            Scp03KeyParameters when yubiKey.HasFeature(YubiKeyFeature.Scp03)
                => "SCP03",
            Scp11KeyParameters when yubiKey.HasFeature(YubiKeyFeature.Scp11)
                => "SCP11",
            null => string.Empty,
            _ => throw new InvalidOperationException("The YubiKey does not support the requested SCP connection.")
        };

        string possibleScpDescription = string.IsNullOrEmpty(scpType)
            ? string.Empty
            : $" over {scpType}";

        Logger.LogInformation($"Connecting to {GetApplicationFriendlyName(application)}{possibleScpDescription}");

        var connection = keyParameters != null
            ? yubiKey.Connect(application, keyParameters)
            : yubiKey.Connect(application);

        Logger.LogInformation($"Connected to {GetApplicationFriendlyName(application)}");
        return connection;
    }

    private static string GetApplicationFriendlyName(YubiKeyApplication application) =>
        Enum.GetName(typeof(YubiKeyApplication), application) ?? "Unknown";

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // At the moment, there is no "close session" method. So for now,
            // just connect to the management application.
            // This can fail, possibly resulting in a SCardException (or other), so we wrap it in a try catch-block to complete the disposal of the PivSession
            try
            {
                _ = Connection.SendCommand(new SelectApplicationCommand(YubiKeyApplication.Management));
                Connection.Dispose();
            }
            catch (Exception e)
            {
                string message = string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.SessionDisposeUnknownError, e.GetType(), e.Message);

                Logger.LogWarning(message);
            }

            _disposed = true;
        }
    }
}
