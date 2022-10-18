// Copyright 2022 Yubico AB
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
using System.Security;
using System.Threading.Tasks;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Fido2.ParameterBuilder;

namespace Yubico.YubiKey.Fido2
{
    // This portion of the Fido2Session class deals with creating (making) a
    // credential on the YubiKey.
    public sealed partial class Fido2Session
    {
        /// <summary>
        /// A delegate facilitating the fluent builder pattern used by the
        /// <see cref="Fido2Session.MakeCredential(Yubico.YubiKey.Fido2.Fido2Session.MakeCredentialParamBuilderFn)"/>
        /// function.
        /// </summary>
        public delegate MakeCredentialParameters MakeCredentialParamBuilderFn(IMcClientHashBuilder b);

        /// <summary>
        /// Creates a FIDO2 credential on the YubiKey using a fluent-builder for
        /// gathering the parameters.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this version of MakeCredential to take advantage of the fluent builder for
        /// the <see cref="MakeCredentialParameters"/> object. The builder will guide you
        /// through the mandatory parameters, as well as allowing you to specify all of the
        /// optional parameters in a clear and concise way.
        /// </para>
        /// <para>
        /// The builder is accessed through the only parameter passed along into the builder
        /// delegate. Call the methods on this parameter to begin constructing the parameters
        /// object. For example:
        /// <code language="csharp">
        /// using var fido2 = new Fido2Session(yubiKeyDevice);
        ///
        /// // Collect the PIN or UV prior to calling MakeCredential
        ///
        /// fido2.MakeCredential(b => b
        ///     .SetClientHash(clientHashData)
        ///     .SetRelyingParty("my-rp-id")
        ///     .SetUser(userId, "username", "Display Name")
        ///     .Build());
        /// </code>
        /// </para>
        /// <para>
        /// See the documentation for <see cref="MakeCredentialParameters"/> for more information
        /// on the various parameters and input to MakeCredential.
        /// </para>
        /// </remarks>
        /// <param name="paramBuilder">
        /// A delegate that receives the start of a fluent builder object's workflow and
        /// returns the constructed <see cref="MakeCredentialParameters"/> object. See
        /// the remarks section for more details.
        /// </param>
        /// <returns>
        /// <inheritdoc cref="MakeCredential(MakeCredentialParameters)"/>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="paramBuilder"/> argument is null.
        /// </exception>
        public MakeCredentialData MakeCredential(MakeCredentialParamBuilderFn paramBuilder)
        {
            if (paramBuilder is null)
            {
                throw new ArgumentNullException(nameof(paramBuilder));
            }

            return MakeCredential(paramBuilder(new MakeCredentialParameterBuilder()));
        }

        /// <summary>
        /// Creates a FIDO2 credential on the YubiKey given a parameters object.
        /// </summary>
        /// <remarks>
        /// Detailed information about the parameters structure and its expected values can be found on
        /// the <see cref="MakeCredentialParameters"/> page.
        /// </remarks>
        /// <param name="parameters">
        /// A fully populated <see cref="MakeCredentialParameters"/> structure that
        /// follows all of the rules set forth by that object.
        /// </param>
        /// <returns>
        /// An object containing all of the relevant information returned by the YubiKey
        /// after calling make credential. This includes the public key for the credential
        /// itself, along with supporting information like the attestation statement, and
        /// other authenticator data.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="parameters"/> argument was null.
        /// </exception>
        /// <exception cref="SecurityException">
        /// The YubiKey has not been authenticated. Call <see cref="VerifyPin"/> or <see cref="VerifyUv"/> before
        /// calling this method.
        /// </exception>
        /// <exception cref="TimeoutException">
        /// The YubiKey either required touch for a user presence check or a biometric touch for user verification.
        /// The YubiKey timed out waiting for this action to be performed.
        /// </exception>
        public MakeCredentialData MakeCredential(MakeCredentialParameters parameters)
        {
            if (parameters is null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (AuthToken.HasValue == false)
            {
                throw new SecurityException("The YubiKey has not been authenticated.");
            }

            parameters.Protocol = AuthProtocol.Protocol;
            parameters.PinUvAuthParam = AuthProtocol.AuthenticateUsingPinToken(
                AuthToken.Value.ToArray(),
                parameters.ClientDataHash.ToArray());

            Func<KeyEntryData, bool> keyCollector = EnsureKeyCollector();

            var keyEntryData = new KeyEntryData()
            {
                Request = KeyEntryRequest.TouchRequest
            };

            try
            {
                var touchNotifyTask = Task.Run(() => keyCollector(keyEntryData));
                var makeCredentialTask = Task.Run(() => Connection.SendCommand(new MakeCredentialCommand(parameters)));

                // Ideally, we'd like the UI to be able to cancel this operation, but there's no good way
                // of doing this. The YubiKey blocks until UP has been satisfied or it times out. As far
                // as I can see, there is no way to tell the YubiKey "stop". While we could let the thread
                // run out in the background, the YubiKey would also not be able to process other operations.
                // So doing so might cause weird failures and timeouts in other areas of the code. Better
                // to just wait until timeout no matter what.
                Task.WaitAll(touchNotifyTask, makeCredentialTask);

                MakeCredentialResponse response = makeCredentialTask.Result;

                if (GetCtapError(response) == CtapStatus.OperationDenied
                    || GetCtapError(response) == CtapStatus.ActionTimeout)
                {
                    throw new TimeoutException("The YubiKey was not touched in the allotted time.");
                }

                return makeCredentialTask.Result.GetData();
            }
            finally
            {
                keyEntryData.Request = KeyEntryRequest.Release;
                _ = keyCollector(keyEntryData);
            }
        }
    }
}
