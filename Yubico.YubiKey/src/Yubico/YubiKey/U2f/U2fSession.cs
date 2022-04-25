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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security;
using Yubico.YubiKey.U2f.Commands;

namespace Yubico.YubiKey.U2f
{
    public sealed class U2fSession : IDisposable
    {
        private readonly IYubiKeyDevice _yubiKeyDevice;
        private bool _disposed;

        public IYubiKeyConnection Connection { get; private set; }

        public Func<KeyEntryData, bool>? KeyCollector { get; set; }

        private U2fSession()
        {
            throw new NotImplementedException();
        }

        public U2fSession(IYubiKeyDevice yubiKey)
        {
            if (yubiKey is null)
            {
                throw new ArgumentNullException(nameof(yubiKey));
            }

            _yubiKeyDevice = yubiKey;

            Connection = yubiKey.Connect(YubiKeyApplication.FidoU2f);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="clientDataHash"></param>
        /// <returns></returns>
        /// <exception cref="SecurityException"></exception>
        public RegistrationData Register(ReadOnlyMemory<byte> applicationId, ReadOnlyMemory<byte> clientDataHash)
        {
            if (!TryRegister(applicationId, clientDataHash, out RegistrationData? registrationData))
            {
                throw new SecurityException("User presence or authentication failed.");
            }

            return registrationData;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="clientDataHash"></param>
        /// <param name="registrationData"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public bool TryRegister(
            ReadOnlyMemory<byte> applicationId,
            ReadOnlyMemory<byte> clientDataHash,
            [MaybeNullWhen(returnValue: false)] out RegistrationData registrationData)
        {
            var command = new RegisterCommand(clientDataHash, applicationId);

            if (_yubiKeyDevice.IsFipsSeries)
            {
                // TODO: Extra FIPS handling?
            }

            RegisterResponse response = Connection.SendCommand(command);

            if (response.Status == ResponseStatus.AuthenticationRequired)
            {
                registrationData = null;
                return false;
            }
            else if (response.Status != ResponseStatus.Success)
            {
                // TODO: throw
                throw new InvalidOperationException();
            }

            registrationData = response.GetData();

            return true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            KeyCollector = null;
            Connection.Dispose();
            _disposed = true;
        }

        private void EnsureKeyCollector()
        {
            if (KeyCollector is null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.MissingKeyCollector));
            }
        }
    }
}
