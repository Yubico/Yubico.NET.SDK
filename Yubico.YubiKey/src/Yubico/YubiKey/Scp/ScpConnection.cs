// Copyright 2023 Yubico AB
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
using Yubico.Core.Devices.SmartCard;
using Yubico.YubiKey.Pipelines;

namespace Yubico.YubiKey.Scp
{
    internal class ScpConnection : SmartCardConnection, IScpYubiKeyConnection
    {
        public ScpKeyParameters KeyParameters => _scpApduTransform.KeyParameters;

        EncryptDataFunc IScpYubiKeyConnection.EncryptDataFunc => _scpApduTransform.EncryptDataFunc;

        private bool _disposed;
        private readonly ScpApduTransform _scpApduTransform;

        public ScpConnection(
            ISmartCardDevice smartCardDevice,
            YubiKeyApplication application,
            ScpKeyParameters keyParameters)
            : base(smartCardDevice, application, null)
        {
            var scpPipeline = CreateScpPipeline(keyParameters);
            var withErrorHandling = CreateParentPipeline(scpPipeline, application);

            // Have the base class use the new error augmented pipeline
            SetPipeline(withErrorHandling);

            // Setup the full pipeline
            withErrorHandling.Setup();
            _scpApduTransform = scpPipeline;
        }

        private static IApduTransform CreateParentPipeline(IApduTransform pipeline, YubiKeyApplication application)
        {
            // Wrap the pipeline with error handling if needed
            if (application == YubiKeyApplication.Fido2)
            {
                return new FidoErrorTransform(pipeline);
            }

            if (application == YubiKeyApplication.Otp)
            {
                return new OtpErrorTransform(pipeline);
            }

            return pipeline;
        }

        private ScpApduTransform CreateScpPipeline(ScpKeyParameters keyParameters)
        {
            // Get the current pipeline
            var previousPipeline = GetPipeline();

            // Wrap the pipeline in ScpApduTransform
            var scpApduTransform = new ScpApduTransform(previousPipeline, keyParameters);

            // Return both pipeline
            return scpApduTransform;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _scpApduTransform.Dispose();
                    _disposed = true;
                }
            }

            base.Dispose(disposing);
        }
    }
}
