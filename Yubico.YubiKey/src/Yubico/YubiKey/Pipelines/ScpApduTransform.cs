// Copyright 2021 Yubico AB
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
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Scp;

namespace Yubico.YubiKey.Pipelines
{
    /// <summary>
    /// Performs SCP encrypt-then-MAC on commands and verify-then-decrypt on responses.
    /// </summary>
    /// <remarks>
    /// Does an SCP Initialize Update / External Authenticate handshake at setup.
    ///
    /// Commands and responses sent through this pipeline are confidential and authenticated.
    ///
    /// Requires pre-shared <see cref="Scp03.StaticKeys"/>. TODO
    /// </remarks>
    // broken into two transforms
    internal class ScpApduTransform : IApduTransform, IDisposable
    {
        public ScpKeyParameters KeyParameters { get; }
        public DataEncryptor? DataEncryptor;

        private ScpState ScpState =>
            _scpState ?? throw new InvalidOperationException($"{nameof(Scp.ScpState)} has not been initialized. The Setup method must be called.");
        
        private readonly IApduTransform _pipeline;
        private ScpState? _scpState;
        private bool _disposed;

        /// <summary>
        /// Constructs a new pipeline from the given one.
        /// </summary>
        /// <param name="pipeline">Underlying pipeline to send and receive encoded APDUs with</param>
        /// <param name="keyParameters"></param>//todo
        public ScpApduTransform(IApduTransform pipeline, ScpKeyParameters keyParameters)
        {
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            KeyParameters = keyParameters ?? throw new ArgumentNullException(nameof(keyParameters));
        }

        /// <summary>
        /// Performs SCP handshake. Must be called after SELECT.
        /// </summary>
        public void Setup()
        {
            _pipeline.Setup();

            if (KeyParameters.GetType() == typeof(Scp03KeyParameters))
            {
                DataEncryptor = InitializeScp03((Scp03KeyParameters)KeyParameters);
            }
            else if (KeyParameters.GetType() == typeof(Scp11KeyParameters))
            {
                DataEncryptor = InitializeScp11((Scp11KeyParameters)KeyParameters);
            }
        }

        private DataEncryptor InitializeScp11(Scp11KeyParameters keyParameters)
        {
            _scpState = Scp11State.CreateScpState(_pipeline, keyParameters);
            return _scpState.GetDataEncryptor();
        }

        private DataEncryptor InitializeScp03(Scp03KeyParameters keyParams)
        {
            // Generate host challenge
            using var rng = CryptographyProviders.RngCreator();
            byte[] hostChallenge = new byte[8];
            rng.GetBytes(hostChallenge);

            _scpState = Scp03State.CreateScpState(_pipeline, keyParams, hostChallenge);

            return _scpState.GetDataEncryptor();
        }
        

        public ResponseApdu Invoke(CommandApdu command, Type commandType, Type responseType)
        {
            // Encode command
            var encodedCommand = ScpState.EncodeCommand(command);

            // Pass along the encoded command
            var response = _pipeline.Invoke(encodedCommand, commandType, responseType);

            // Special carve out for SelectApplication here, since there will be nothing to decode
            if (commandType == typeof(InterIndustry.Commands.SelectApplicationCommand))
            {
                return response;
            }

            // Decode response and return it
            return ScpState.DecodeResponse(response);
        }

        // There is a call to cleanup and a call to Dispose. The cleanup only
        // needs to call the cleanup on the local APDU Pipeline object.
        public void Cleanup() => _pipeline.Cleanup();

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        // The Dispose needs to make sure the local disposable fields are
        // disposed.
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _scpState?.Dispose();

                    _disposed = true;
                }
            }
        }
    }
}
