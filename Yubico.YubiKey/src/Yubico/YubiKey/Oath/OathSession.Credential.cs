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
using System.Collections.Generic;
using System.Linq;
using System.Security;
using Yubico.YubiKey.Oath.Commands;

namespace Yubico.YubiKey.Oath
{
    // This portion of the OathSession class contains operations related to managing credentials.
    public sealed partial class OathSession : IDisposable
    {
        /// <summary>
        /// Gets all configured credentials on the YubiKey.
        /// </summary>
        /// <returns>
        /// The list of credentials.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded if the authentication is required or
        /// the <c>ListCommand</c> failed.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Unable to verify password either because <c>KeyCollector</c> was canceled by the user or
        /// an incorrect password was provided if authentication is required.
        /// </exception>
        public IList<Credential> GetCredentials()
        {
            var listCommand = new ListCommand();
            ListResponse listResponse = Connection.SendCommand(listCommand);

            if (listResponse.Status == ResponseStatus.AuthenticationRequired)
            {
                VerifyPassword();
                listResponse = Connection.SendCommand(listCommand);
            }

            if (listResponse.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(listResponse.StatusMessage);
            }

            IList<Credential> result = listResponse.GetData();

            return result;
        }

        /// <summary>
        /// Gets OTP (One-Time Password) values for all configured credentials on the YubiKey.
        /// </summary>
        /// <remarks>
        /// <see cref="CalculateAllCredentialsCommand"/> doesn't return a <see cref="Code"/> for
        /// HOTP credentials and credentials requiring touch. They will need to be calculated
        /// separately using <see cref="CalculateCredentialCommand"/>.
        /// </remarks>
        /// <param name="responseFormat">
        /// Full or truncated <see cref="ResponseFormat"/> to receive back. The default value is Truncated.
        /// </param>
        /// <returns>
        /// The dictionary of <see cref="Credential"/> and <see cref="Code"/> pairs.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// There is no <see cref="KeyCollector"/> loaded if the authentication is required,
        /// the <see cref="CalculateAllCredentialsCommand"/> failed, or the
        /// <see cref="CalculateCredentialCommand"/> failed.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Unable to verify password either because <see cref="KeyCollector"/> was canceled by the user or
        /// an incorrect password was provided if authentication is required.
        /// </exception>
        public IDictionary<Credential, Code> CalculateAllCredentials(
            ResponseFormat responseFormat = ResponseFormat.Truncated)
        {
            var calculateAllCommand = new CalculateAllCredentialsCommand(responseFormat);
            CalculateAllCredentialsResponse calculateAllResponse = Connection.SendCommand(calculateAllCommand);

            if (calculateAllResponse.Status == ResponseStatus.AuthenticationRequired)
            {
                VerifyPassword();
                calculateAllResponse = Connection.SendCommand(calculateAllCommand);
            }

            if (calculateAllResponse.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(calculateAllResponse.StatusMessage);
            }

            IDictionary<Credential, Code> result = calculateAllResponse.GetData();

            return result;
        }

        /// <summary>
        /// Gets an OTP (One-Time Password) value for the specific credential on the YubiKey.
        /// </summary>
        /// <param name="credential">
        /// The <see cref="Credential"/> on the YubiKey to calculate.
        /// </param>
        /// <param name="responseFormat">
        /// Full or truncated <see cref="ResponseFormat"/> to receive back. The default value is Truncated.
        /// </param>
        /// <returns>
        /// The <see cref="Code"/> for the requested credential.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Provided credential is null. 
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded if the authentication is required or
        /// the <c>CalculateCredentialCommand</c> failed.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Unable to verify password either because <c>KeyCollector</c> was canceled by the user or
        /// an incorrect password was provided if authentication is required.
        /// </exception>
        public Code CalculateCredential(
            Credential credential,
            ResponseFormat responseFormat = ResponseFormat.Truncated)
        {
            var calculateCommand = new CalculateCredentialCommand(credential, responseFormat);
            CalculateCredentialResponse calculateResponse = Connection.SendCommand(calculateCommand);

            if (calculateResponse.Status == ResponseStatus.AuthenticationRequired)
            {
                VerifyPassword();
                calculateResponse = Connection.SendCommand(calculateCommand);
            }

            if (calculateResponse.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(calculateResponse.StatusMessage);
            }

            Code otpCode = calculateResponse.GetData();

            return otpCode;
        }

        /// <summary>
        /// Gets an OTP code for the specific credential.
        /// </summary>
        /// <param name="issuer">
        /// The issuer is an optional string indicating the provider or service.
        /// </param>
        /// <param name="account">
        /// The account name that usually is the user's email address.
        /// </param>
        /// <param name="type">
        /// Indicates the <see cref="CredentialType"/> of the credential as either HOTP or TOTP.
        /// </param>
        /// <param name="period">
        /// Indicates the <see cref="CredentialPeriod"/> of the credential in seconds for TOTP code.
        /// It can only be 15, 30, or 60 seconds. For HOTP should be set to zero
        /// (<see cref="CredentialPeriod.Undefined"/>).
        /// </param>
        /// <param name="responseFormat">
        /// Full or truncated <see cref="ResponseFormat"/> to receive back. The default value is Truncated.
        /// </param>
        /// <returns>
        /// The <see cref="Code"/> for the requested credential.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded if the authentication is required,
        /// the <c>CalculateCredentialCommand</c> failed, or the provided account, type, or period is invalid.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Unable to verify password either because <c>KeyCollector</c> was canceled by the user or
        /// an incorrect password was provided if authentication is required.
        /// </exception>
        public Code CalculateCredential(
            string? issuer,
            string account,
            CredentialType type,
            CredentialPeriod period,
            ResponseFormat responseFormat = ResponseFormat.Truncated)
        {
            var credential = new Credential(issuer, account, type, period);
            Code otpCode = CalculateCredential(credential, responseFormat);

            return otpCode;
        }

        /// <summary>
        /// Adds a new credential or overwrites the existing one on the YubiKey.
        /// </summary>
        /// <param name="credential">
        /// The <see cref="Credential"/> to add to the YubiKey.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Provided credential is null. 
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded if the authentication is required,
        /// some credential properties are not supported on this YubiKey, or the <c>PutCommand</c> failed. 
        /// </exception>
        /// <exception cref="SecurityException">
        /// Unable to verify password either because <c>KeyCollector</c> was canceled by the user or
        /// an incorrect password was provided if authentication is required.
        /// </exception>
        public void AddCredential(Credential credential)
        {
            if (credential is null)
            {
                throw new ArgumentNullException(nameof(credential));
            }

            if (credential.RequiresTouch == true &&
                !_yubiKeyDevice.HasFeature(YubiKeyFeature.OathTouchCredential))
            {
                throw new InvalidOperationException(ExceptionMessages.TouchNotSupported);
            }

            if (credential.Algorithm == HashAlgorithm.Sha512 &&
                !_yubiKeyDevice.HasFeature(YubiKeyFeature.OathSha512))
            {
                throw new InvalidOperationException(ExceptionMessages.SHA512NotSupported);
            }

            var putCommand = new PutCommand(credential);
            OathResponse putResponse = Connection.SendCommand(putCommand);

            if (putResponse.Status == ResponseStatus.AuthenticationRequired)
            {
                VerifyPassword();
                putResponse = Connection.SendCommand(putCommand);
            }

            if (putResponse.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(putResponse.StatusMessage);
            }
        }

        /// <summary>
        /// Adds a new credential or overwrites the existing one on the YubiKey with default parameters.
        /// </summary>
        /// <remarks>
        /// If type and period properties are not modified, this will add a TOTP credential
        /// with default parameters like:
        /// Type - TOTP,
        /// Period - 30 seconds,
        /// Algorithm - SHA-1,
        /// Digits - 6,
        /// No secret,
        /// No touch.
        /// </remarks>
        /// <param name="issuer">
        /// The issuer is an optional string indicating the provider or service.
        /// </param>
        /// <param name="account">
        /// The account name that usually is the user's email address.
        /// </param>
        /// <param name="type">
        /// Indicates the <see cref="CredentialType"/> of the credential as either HOTP or TOTP.
        /// The default value is TOTP.
        /// </param>
        /// <param name="period">
        /// Indicates the <see cref="CredentialPeriod"/> of the credential in seconds for TOTP code.
        /// It can only be 15, 30, or 60 seconds. For HOTP should be set to zero
        /// (<see cref="CredentialPeriod.Undefined"/>). The default value is 30.
        /// </param>
        /// <returns>
        /// The <see cref="Credential"/> that was created and added to the YubiKey.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded if the authentication is required,
        /// the <c>PutCommand</c> failed, or the provided account, type, or period is invalid.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Unable to verify password either because <c>KeyCollector</c> was canceled by the user or
        /// an incorrect password was provided if authentication is required.
        /// </exception>
        public Credential AddCredential(
            string? issuer,
            string account,
            CredentialType type = CredentialType.Totp,
            CredentialPeriod period = CredentialPeriod.Period30)
        {
            var credential = new Credential(issuer, account, type, period);
            AddCredential(credential);

            return credential;
        }

        /// <summary>
        /// Adds a credential from the string that received from the QR reader or manually from the server.
        /// </summary>
        /// <remarks>
        /// This method parses an 'otpauth://' Uri string that received from the QR reader or 
        /// manually from the server, as specified by
        /// https://github.com/google/google-authenticator/wiki/Key-Uri-Format
        /// </remarks>
        /// <param name="stringFromURI">
        /// The string that received from the QR reader or manually from the server.
        /// </param>
        /// <returns>
        /// The <see cref="Credential"/> that was created and added to the YubiKey.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Provided string is null, empty, or consists only of white-space characters.  
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded if the authentication is required,
        /// some credential properties are not supported on this YubiKey, the <c>PutCommand</c> failed,
        /// or the URI string is invalid or it contains invalid elements.   
        /// </exception>
        /// <exception cref="SecurityException">
        /// Unable to verify password either because <c>KeyCollector</c> was canceled by the user or
        /// an incorrect password was provided if authentication is required.
        /// </exception>
        #pragma warning disable CA1054 // Justification: In this case, URI parameter should be a string
        public Credential AddCredential(string stringFromURI)
            #pragma warning restore CA1054
        {
            if (string.IsNullOrWhiteSpace(stringFromURI))
            {
                throw new ArgumentNullException(stringFromURI);
            }

            var credential = Credential.ParseUri(new Uri(stringFromURI));
            AddCredential(credential);

            return credential;
        }

        /// <summary>
        /// Removes an existing credential from the YubiKey.
        /// </summary>
        /// <param name="credential">
        /// The <see cref="Credential"/> to remove from the YubiKey.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Provided credential is null. 
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded if the authentication is required or
        /// the <c>DeleteCommand</c> failed. 
        /// </exception>
        /// <exception cref="SecurityException">
        /// Unable to verify password either because <c>KeyCollector</c> was canceled by the user or
        /// an incorrect password was provided if authentication is required.
        /// </exception>
        public void RemoveCredential(Credential credential)
        {
            var deleteCommand = new DeleteCommand(credential);
            OathResponse deleteResponse = Connection.SendCommand(deleteCommand);

            if (deleteResponse.Status == ResponseStatus.AuthenticationRequired)
            {
                VerifyPassword();
                deleteResponse = Connection.SendCommand(deleteCommand);
            }

            if (deleteResponse.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(deleteResponse.StatusMessage);
            }
        }

        /// <summary>
        /// Removes an existing credential from the YubiKey.
        /// </summary>
        /// <param name="issuer">
        /// The issuer is an optional string indicating the provider or service.
        /// </param>
        /// <param name="account">
        /// The account name that usually is the user's email address.
        /// </param>
        /// <param name="type">
        /// Indicates the <see cref="CredentialType"/> of the credential as either HOTP or TOTP.
        /// The default value is TOTP.
        /// </param>
        /// <param name="period">
        /// Indicates the <see cref="CredentialPeriod"/> of the credential in seconds for TOTP code.
        /// It can only be 15, 30, or 60 seconds. For HOTP should be set to zero
        /// (<see cref="CredentialPeriod.Undefined"/>). The default value is 30.
        /// </param>
        /// <returns>
        /// The <c>Credential</c> that was removed from the YubiKey.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded if the authentication is required,
        /// the <c>DeleteCommand</c> failed, or the provided account, type, or period is invalid.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Unable to verify password either because <c>KeyCollector</c> was canceled by the user or
        /// an incorrect password was provided if authentication is required.
        /// </exception>
        public Credential RemoveCredential(
            string? issuer,
            string account,
            CredentialType type = CredentialType.Totp,
            CredentialPeriod period = CredentialPeriod.Period30)
        {
            var credential = new Credential
            {
                Issuer = issuer,
                AccountName = account,
                Type = type,
                Period = period
            };

            RemoveCredential(credential);

            return credential;
        }

        /// <summary>
        /// Renames an existing credential on the YubiKey by setting new issuer and account names.
        /// </summary>
        /// <remarks>
        /// This command is only available on the YubiKeys with firmware version 5.3.0 and later.
        /// </remarks>
        /// <param name="credential">
        /// The <c>Credential</c> to rename.
        /// </param>
        /// <param name="newIssuer">
        /// The new issuer to set on the credential. The issuer is optional and can be null.
        /// </param>
        /// <param name="newAccount">
        /// The new account name to set on the credential.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The credential to rename is null. Or the new account name to set is null, empty,
        /// or consists only of white-space characters.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded if the authentication is required,
        /// the <c>RenameCommand</c> is not supported on this YubiKey, or the <c>RenameCommand</c> failed. 
        /// </exception>
        /// <exception cref="SecurityException">
        /// Unable to verify password either because <c>KeyCollector</c> was canceled by the user or
        /// an incorrect password was provided if authentication is required.
        /// </exception>
        public void RenameCredential(Credential credential, string? newIssuer, string newAccount)
        {
            if (!_yubiKeyDevice.HasFeature(YubiKeyFeature.OathRenameCredential))
            {
                throw new InvalidOperationException(ExceptionMessages.RenameCommandNotSupported);
            }

            var renameCommand = new RenameCommand(credential, newIssuer, newAccount);
            RenameResponse renameResponse = Connection.SendCommand(renameCommand);

            if (renameResponse.Status == ResponseStatus.AuthenticationRequired)
            {
                VerifyPassword();
                renameResponse = Connection.SendCommand(renameCommand);
            }

            if (renameResponse.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(renameResponse.StatusMessage);
            }
        }

        /// <summary>
        /// Renames an existing credential on the YubiKey by setting new issuer and account names.
        /// </summary>
        /// <remarks>
        /// This command is only available on the YubiKeys with firmware version 5.3.0 and later.
        /// </remarks>
        /// <param name="currentIssuer">
        /// The current credential's issuer.
        /// </param>
        /// <param name="currentAccount">
        /// The current credential's account name.
        /// </param>
        /// <param name="newIssuer">
        /// The new issuer to set on the credential. The issuer is optional and can be null.
        /// </param>
        /// <param name="newAccount">
        /// The new account name to set on the credential.
        /// </param>
        /// <param name="currentType">
        /// Indicates the <see cref="CredentialType"/> of the current credential as either HOTP or TOTP.
        /// The default value is TOTP.
        /// </param>
        /// <param name="currentPeriod">
        /// Indicates the <see cref="CredentialPeriod"/> of the current credential in seconds for TOTP code.
        /// It can only be 15, 30, or 60 seconds. For HOTP should be set to zero
        /// (<see cref="CredentialPeriod.Undefined"/>). The default value is 30.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded if the authentication is required,
        /// the <c>RenameCommand</c> is not supported on this YubiKey, the <c>RenameCommand</c> failed,
        /// or the provided current or new account, type, or period is invalid.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Unable to verify password either because <c>KeyCollector</c> was canceled by the user or
        /// an incorrect password was provided if authentication is required.
        /// </exception>
        public Credential RenameCredential(
            string? currentIssuer,
            string currentAccount,
            string? newIssuer,
            string newAccount,
            CredentialType currentType = CredentialType.Totp,
            CredentialPeriod currentPeriod = CredentialPeriod.Period30)
        {
            var credential = new Credential
            {
                Issuer = currentIssuer,
                AccountName = currentAccount,
                Type = currentType,
                Period = currentPeriod
            };

            RenameCredential(credential, newIssuer, newAccount);

            return credential;
        }
    }
}
