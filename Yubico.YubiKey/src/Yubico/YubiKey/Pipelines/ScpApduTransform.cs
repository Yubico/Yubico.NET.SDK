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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.InterIndustry.Commands;
using Yubico.YubiKey.Oath.Commands;
using Yubico.YubiKey.Scp;
using ResetCommand = Yubico.YubiKey.Scp.Commands.ResetCommand;

namespace Yubico.YubiKey.Pipelines;

/// <summary>
///     Constructs the shared state for SCP communication over SCP03, SCP11a/b/c.
///     Performs SCP encrypt-then-MAC on commands and verify-then-decrypt on responses.
/// </summary>
/// <remarks>
///     Does an SCP Initialize Update / External Authenticate handshake at setup.
///     Commands and responses sent through this pipeline are confidential and authenticated.
/// </remarks>
internal class ScpApduTransform : IApduTransform, IDisposable
{
    private readonly IApduTransform _pipeline;
    private EncryptDataFunc? _dataEncryptor;
    private bool _disposed;
    private ScpState? _scpState;

    /// <summary>
    ///     Constructs a new pipeline from the given one.
    /// </summary>
    /// <param name="pipeline">Underlying pipeline to send and receive encoded APDUs with</param>
    /// <param name="keyParameters">The <see cref="ScpKeyParameters" /> for the SCP connection</param>
    public ScpApduTransform(IApduTransform pipeline, ScpKeyParameters keyParameters)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        KeyParameters = keyParameters ?? throw new ArgumentNullException(nameof(keyParameters));
    }

    /// <summary>
    ///     The <see cref="ScpKeyParameters" /> for the SCP connection
    /// </summary>
    public ScpKeyParameters KeyParameters { get; }

    /// <summary>
    ///     The <see cref="EncryptDataFunc" /> which encrypts any data using the session key
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the <see cref="ScpState" /> has not been initialized.</exception>
    public EncryptDataFunc EncryptDataFunc => _dataEncryptor ?? ThrowIfUninitialized<EncryptDataFunc>();

    private ScpState ScpState => _scpState ?? ThrowIfUninitialized<ScpState>();

    #region IApduTransform Members

    /// <summary>
    ///     Performs SCP handshake. Must be called after SELECT.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if the instance <see cref="KeyParameters" /> is invalid.</exception>
    public void Setup()
    {
        _pipeline.Setup();

        switch (KeyParameters)
        {
            case Scp03KeyParameters scp03KeyParameters:
                InitializeScp03(scp03KeyParameters);
                break;
            case Scp11KeyParameters scp11KeyParameters:
                InitializeScp11(scp11KeyParameters);
                break;
            default:
                throw new ArgumentException(
                    $"Type of {nameof(KeyParameters)} is not recognized", nameof(KeyParameters));
        }
    }

    /// <summary>
    ///     Passes the supplied command into the pipeline, and returns the final response.
    /// </summary>
    /// <remarks>
    ///     Encodes the command using the SCP state, sends it to the underlying pipeline, and then decodes the response.
    ///     <para>
    ///         Note: Some commands should not be encoded. For those, the pipeline is invoked directly without encoding.
    ///     </para>
    /// </remarks>
    public ResponseApdu Invoke(CommandApdu command, Type commandType, Type responseType)
    {
        if (ShouldNotEncode(commandType))
        {
            // Invoke the pipeline without encoding the command
            return _pipeline.Invoke(command, commandType, responseType);
        }

        // Encode command
        var encodedCommand = ScpState.EncodeCommand(command);
        var response = _pipeline.Invoke(encodedCommand, commandType, responseType);

        // Decode response
        return ScpState.DecodeResponse(response);
    }

    // There is a call to cleanup and a call to Dispose. The cleanup only
    // needs to call the cleanup on the local APDU Pipeline object.
    public void Cleanup() => _pipeline.Cleanup();

    #endregion

    #region IDisposable Members

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion

    private void InitializeScp03(Scp03KeyParameters keyParams)
    {
        // Generate host challenge
        using var rng = CryptographyProviders.RngCreator();

        byte[] hostChallenge = new byte[8];
        rng.GetBytes(hostChallenge);

        // Create the state object that manages keys, mac chaining, etc.
        _scpState = Scp03State.CreateScpState(_pipeline, keyParams, hostChallenge);

        // Set the data encryptor for later use
        _dataEncryptor = _scpState.GetDataEncryptor();
    }

    private void InitializeScp11(Scp11KeyParameters keyParameters)
    {
        // Create the state object that manages keys, mac chaining, etc.
        _scpState = Scp11State.CreateScpState(_pipeline, keyParameters);

        // Set the data encryptor for later use
        _dataEncryptor = _scpState.GetDataEncryptor();
    }

    [DoesNotReturn]
    private static T ThrowIfUninitialized<T>() =>
        throw new InvalidOperationException(
            $"{nameof(Scp.ScpState)} has not been initialized. The Setup method must be called.");

    private static bool ShouldNotEncode(Type commandType)
    {
        // This method introduces some coupling between the SCP pipeline and the applications.
        // The applications should not have to know about the SCP pipeline, or they should be able to
        // send the commands without the pipeline.
        var exemptionList = new[]
        {
            typeof(SelectApplicationCommand),
            typeof(SelectOathCommand),
            typeof(ResetCommand)
        };

        return exemptionList.Contains(commandType);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (!disposing)
        {
            return;
        }

        _scpState?.Dispose();
        _disposed = true;
    }
}
