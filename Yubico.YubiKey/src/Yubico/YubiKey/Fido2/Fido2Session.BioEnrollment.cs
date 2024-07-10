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
using System.Collections.Generic;
using System.Security;
using Yubico.Core.Logging;
using Yubico.YubiKey.Fido2.Commands;

namespace Yubico.YubiKey.Fido2
{
    // This portion of the Fido2Session class deals with the BioEnroll operations.
    public sealed partial class Fido2Session
    {
        /// <summary>
        ///     Get the biometric method the YubiKey uses. If the YubiKey is not a
        ///     Bio series device, this will return "None".
        /// </summary>
        /// <remarks>
        ///     Note that the <see cref="AuthenticatorInfo.UvModality" /> property
        ///     also indicates the modality. It is defined as a bit field with the
        ///     specific bits defined in the FIDO standard's Registry of Predefined
        ///     Values, section 3.1. For example, in that bit field, the bit in
        ///     position 1 (the integer 2) is defined as
        ///     <c>USER_VERIFY_FINGERPRINT_INTERNAL</c>.
        /// </remarks>
        /// <returns>
        ///     A <c>BioModality</c> value, indicating the biometric method or else
        ///     "None" for non-Bio series devices.
        /// </returns>
        public BioModality GetBioModality()
        {
            _log.LogInformation("Get BioModality.");

            var cmd = new GetBioModalityCommand();
            GetBioModalityResponse rsp = Connection.SendCommand(cmd);
            int modality = rsp.Status == ResponseStatus.Success
                ? rsp.GetData()
                : 0;

            return modality switch
            {
                1 => BioModality.Fingerprint,
                _ => BioModality.None
            };
        }

        /// <summary>
        ///     Get the fingerprint sensor info, which is the "fingerprint kind"
        ///     (touch or swipe), maximum capture count, and the maximum length, in
        ///     bytes, of a template friendly name.
        /// </summary>
        /// <remarks>
        ///     If the connected YubiKey does not have a fingerprint sensor, this
        ///     method will throw an exception. Hence, it would be a good idea to call
        ///     <see cref="GetBioModality" /> and verify the modality is
        ///     <see cref="BioModality.Fingerprint" /> before calling this method.
        /// </remarks>
        /// <returns>
        ///     A <c>FingerprintSensorInfo</c> object.
        /// </returns>
        /// <exception cref="NotSupportedException">
        ///     The connected YubiKey does not support reading fingerprints.
        /// </exception>
        public FingerprintSensorInfo GetFingerprintSensorInfo()
        {
            _log.LogInformation("Get fingerprint sensor info.");

            var cmd = new GetFingerprintSensorInfoCommand();
            GetFingerprintSensorInfoResponse rsp = Connection.SendCommand(cmd);
            return rsp.Status == ResponseStatus.Success
                ? rsp.GetData()
                : throw new NotSupportedException(ExceptionMessages.NotSupportedByYubiKeyVersion);
        }

        /// <summary>
        ///     Get a list of all the bio enrollments on a YubiKey.
        /// </summary>
        /// <remarks>
        ///     This method returns a list of <see cref="TemplateInfo" />, one for
        ///     each enrollment. If there are no enrollments, the list will be empty,
        ///     it will have a <c>Count</c> of zero.
        /// </remarks>
        /// <returns>
        ///     A new <c>List</c> of <see cref="TemplateInfo" />.
        /// </returns>
        /// <exception cref="Ctap2DataException">
        ///     The YubiKey could not return the list, likely because BioEnrollment
        ///     is not supported.
        /// </exception>
        public IReadOnlyList<TemplateInfo> EnumerateBioEnrollments()
        {
            ReadOnlyMemory<byte> currentToken = GetAuthToken(
                forceNewToken: false, PinUvAuthTokenPermissions.BioEnrollment);

            var enumCmd = new BioEnrollEnumerateCommand(currentToken, AuthProtocol);
            BioEnrollEnumerateResponse enumRsp = Connection.SendCommand(enumCmd);
            if (enumRsp.CtapStatus == CtapStatus.PinAuthInvalid)
            {
                currentToken = GetAuthToken(forceNewToken: false, PinUvAuthTokenPermissions.BioEnrollment);
                enumCmd = new BioEnrollEnumerateCommand(currentToken, AuthProtocol);
                enumRsp = Connection.SendCommand(enumCmd);
            }

            return enumRsp.GetData();
        }

        /// <summary>
        ///     Try to enroll a fingerprint. This will require several samples. See
        ///     also the <xref href="Fido2BioEnrollment">User's Manual entry</xref>
        ///     on Bio Enrollment.
        /// </summary>
        /// <remarks>
        ///     This method will call the <see cref="KeyCollector" /> when it needs
        ///     the user to provide a fingerprint sample. Because one sample is not
        ///     enough, this method will call the <c>KeyCollector</c> several times.
        ///     When the YubiKey has collected enough samples, this method will call
        ///     the <c>KeyCollector</c> with a request of <c>Release</c>. It will
        ///     return a new <see cref="TemplateInfo" /> containing the template ID
        ///     and the friendly name (a more detailed discussion of the friendly
        ///     name is given below).
        ///     <para>
        ///         Beginning with the second sample, the <see cref="KeyEntryData" /> will
        ///         include the <see cref="BioEnrollSampleResult" /> of the previous
        ///         sample. In this way, the <c>KeyCollector</c> can report to the user
        ///         any problems with the previous sample along with the number of "good"
        ///         samples the YubiKey still needs in order to enroll.
        ///     </para>
        ///     <para>
        ///         When the SDK calls the <c>KeyCollector</c> for fingerprint
        ///         enrollment, it is possible to cancel the operation. See also
        ///         <see cref="KeyEntryData.SignalUserCancel" />. In that case,
        ///         this method will throw an exception. Note that it is possible
        ///         (although extremely unlikely) that the SDK does not get the cancel
        ///         message "in time" and a fingerprint can be enrolled nonetheless.
        ///     </para>
        ///     <para>
        ///         When a fingerprint is enrolled, the YubiKey creates a new template
        ///         and gives it a ID number (the templateId, which will be unique on
        ///         that YubiKey). When operating on or specifying a fingerprint, it is
        ///         generally necessary to supply the templateId. However, because
        ///         templateIds are binary byte arrays, it is not practical to require a
        ///         user to specify a templateId. Hence, the user has the option of
        ///         assigning a friendly name to each fingerprint template. In that way,
        ///         a user can specify a template based on the name, and the code can use
        ///         its associated templateId. If you want, you can supply the friendly
        ///         name at the time the fingerprint is enrolled. If you don't want to
        ///         assign a friendly name, pass in null for the <c>friendlyName</c> arg.
        ///         In that case, you can add a friendly name later by calling the method
        ///         <see cref="SetBioTemplateFriendlyName" />.
        ///     </para>
        ///     <para>
        ///         The <c>TemplateInfo</c> this method returns will contain the provided
        ///         name if the fingerprint is enrolled. It is possible the
        ///         <see cref="TemplateInfo.FriendlyName" /> property is empty. This
        ///         happens when the caller passes null for the <c>friendlyName</c>, or
        ///         the name passed in is not accepted. This method will not set the
        ///         friendly name if there is already a template on the YubiKey that has
        ///         the name, or if the name is too long. Note that
        ///         <see cref="SetBioTemplateFriendlyName" /> will set the friendly name to
        ///         whatever you provide, even if there is an entry with that name
        ///         already.
        ///     </para>
        ///     <para>
        ///         It is possible that a given YubiKey will have no limit on the number
        ///         of "bad" samples. For example, suppose a YubiKey requires 5 quality
        ///         matching samples to enroll a fingerprint. It is possible some samples
        ///         are rejected, such as when the finger sampled is quite a bit off
        ///         center. But until 5 good samples are provided, the YubiKey will
        ///         continue to ask for another, no matter how many bad ones are given.
        ///         If you want a limit on bad samples, you can enforce it in the
        ///         <c>KeyCollector</c> and use the
        ///         <see cref="KeyEntryData.SignalUserCancel" /> method.
        ///     </para>
        ///     <para>
        ///         It is also possible that for some YubiKey versions, if there are too
        ///         many "bad" fingerprint samples, the YubiKey's maximum sample count
        ///         could be exhausted and the YubiKey "gives up" on this enrollment. In
        ///         that case this method will throw an exception.
        ///     </para>
        ///     <para>
        ///         The YubiKey will also have a time limit for the user to provide a
        ///         sample. This is measured from the moment the YubiKey receives the
        ///         command. The SDK will call the <c>KeyCollector</c> announcing a
        ///         fingerprint is needed at about the same time as it sends the command
        ///         to the YubiKey. For some YubiKey versions, this timeout can be around
        ///         28 seconds. The FIDO2 standard specifies that a user can specify a
        ///         different timeout. Hence, this method has an argument of
        ///         <c>timeoutMilliseconds</c>. However, not all YubiKey versions support
        ///         this feature. That is, it is possible a particular YubiKey will
        ///         ignore this argument and use only its default timeout.
        ///     </para>
        /// </remarks>
        /// <param name="friendlyName">
        ///     The friendly name you want to give the template. If null or
        ///     <c>""</c>, there will be no friendly name. You can add a friendly
        ///     name later by calling the method <see cref="SetBioTemplateFriendlyName" />.
        /// </param>
        /// <param name="timeoutMilliseconds">
        ///     The timeout the caller would like the YubiKey to enforce. This is
        ///     optional and can be null. It is also possible the YubiKey ignores
        ///     this value.
        /// </param>
        /// <returns>
        ///     The <c>TemplateInfo</c> of the template just enrolled.
        /// </returns>
        /// <exception cref="TimeoutException">
        ///     A fingerprint was not sampled within a specified time.
        /// </exception>
        /// <exception cref="SecurityException">
        ///     Too many bad samples were given before the required number of good
        ///     samples were provided.
        /// </exception>
        /// <exception cref="Fido2Exception">
        ///     There was not enough space on the YubiKey for another template.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///     The user canceled the operation.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     The YubiKey was not able to complete the process for some reason
        ///     described in the exception's message.
        /// </exception>
        public TemplateInfo EnrollFingerprint(string? friendlyName, int? timeoutMilliseconds)
        {
            _log.LogInformation("Try to enroll a fingerprint.");

            Func<KeyEntryData, bool> keyCollector = EnsureKeyCollector();

            // Enumerate the current templates to see if there is a matching
            // friendly name. If there is, we won't set the new template's name
            // to something already taken.
            // This also allows us to get an AuthToken with "be" permissions.
            // If we call the Begin command, then we must also call on the
            // KeyCollector to request a fingerprint. But if the AuthToken
            // doesn't work (maybe it has been expired or it does not have the
            // "be" permission), then we have to retract the call for fingerprint
            // and make a call for PIN.
            // Incidentally, enumerating will add some time to this method, but
            // the process of enrolling a fingerprint is so time consuming
            // already, a few milliseconds won't matter.
            ReadOnlyMemory<byte> currentToken = GetAuthToken(
                forceNewToken: false, PinUvAuthTokenPermissions.BioEnrollment);

            var enumCmd = new BioEnrollEnumerateCommand(currentToken, AuthProtocol);
            BioEnrollEnumerateResponse enumRsp = Connection.SendCommand(enumCmd);
            if (enumRsp.CtapStatus == CtapStatus.PinAuthInvalid)
            {
                currentToken = GetAuthToken(forceNewToken: false, PinUvAuthTokenPermissions.BioEnrollment);
                enumCmd = new BioEnrollEnumerateCommand(currentToken, AuthProtocol);
                enumRsp = Connection.SendCommand(enumCmd);
            }

            // If there was an error other than PinAuthInvalid, this call will
            // throw an exception.
            IReadOnlyList<TemplateInfo> templateList = enumRsp.GetData();

            string returnName = "";
            if (!string.IsNullOrEmpty(friendlyName))
            {
                returnName = friendlyName!;
                foreach (TemplateInfo templateInfo in templateList)
                {
                    if (returnName!.Equals(templateInfo.FriendlyName, StringComparison.Ordinal))
                    {
                        returnName = "";
                        break;
                    }
                }
            }

            CtapStatus status;
            string generalErrorMsg = ExceptionMessages.UnknownFido2Status;
            ReadOnlyMemory<byte> templateId = ReadOnlyMemory<byte>.Empty;

            var keyEntryData = new KeyEntryData
            {
                Request = KeyEntryRequest.EnrollFingerprint
            };

            using var fingerprintTask = new TouchFingerprintTask(
                keyCollector,
                keyEntryData,
                Connection,
                CtapConstants.CtapBioEnrollCmd
                );

            try
            {
                var beginCmd = new BioEnrollBeginCommand(timeoutMilliseconds, currentToken, AuthProtocol);
                BioEnrollBeginResponse beginRsp = Connection.SendCommand(beginCmd);
                var currentRsp = (IYubiKeyResponseWithData<BioEnrollSampleResult>)beginRsp;
                status = fingerprintTask.IsUserCanceled
                    ? CtapStatus.KeepAliveCancel
                    : beginRsp.CtapStatus;

                generalErrorMsg = beginRsp.StatusMessage;

                while (status == CtapStatus.Ok)
                {
                    BioEnrollSampleResult enrollResult = currentRsp.GetData();
                    if (enrollResult.RemainingSampleCount <= 0)
                    {
                        templateId = enrollResult.TemplateId;
                        break;
                    }

                    keyEntryData.LastBioEnrollSampleResult = enrollResult;
                    fingerprintTask.SdkUpdate(keyEntryData);
                    var nextCmd = new BioEnrollNextSampleCommand(
                        enrollResult.TemplateId,
                        timeoutMilliseconds,
                        currentToken,
                        AuthProtocol);

                    BioEnrollNextSampleResponse nextRsp = Connection.SendCommand(nextCmd);
                    currentRsp = nextRsp;
                    status = fingerprintTask.IsUserCanceled
                        ? CtapStatus.KeepAliveCancel
                        : nextRsp.CtapStatus;

                    generalErrorMsg = nextRsp.StatusMessage;
                }

                if (status == CtapStatus.Ok && !string.IsNullOrEmpty(returnName))
                {
                    var nameCmd = new BioEnrollSetFriendlyNameCommand(
                        templateId, returnName, currentToken, AuthProtocol);

                    Fido2Response nameRsp = Connection.SendCommand(nameCmd);

                    if (nameRsp.Status != ResponseStatus.Success)
                    {
                        returnName = "";
                    }
                }
            }
            finally
            {
                keyEntryData.Clear();
                keyEntryData.Request = KeyEntryRequest.Release;
                fingerprintTask.SdkUpdate(keyEntryData);
            }

            if (status == CtapStatus.Ok)
            {
                return new TemplateInfo(templateId, returnName);
            }

            // The only way to reach this code is if the BioEnrollment had
            // started, but not yet completed. So cancel the operation.
            var cancelCmd = new BioEnrollCancelCommand();
            _ = Connection.SendCommand(cancelCmd);

            switch (status)
            {
                case CtapStatus.UserActionTimeout:
                    throw new TimeoutException(ExceptionMessages.Fido2FingerprintTimeout);

                case CtapStatus.LimitExceeded:
                    throw new SecurityException(ExceptionMessages.Fido2NoMoreRetries);

                case CtapStatus.FpDatabaseFull:
                    throw new Fido2Exception(ExceptionMessages.FingerprintDatabaseFull);

                case CtapStatus.KeepAliveCancel:
                case CtapStatus.ErrOther:
                    throw new OperationCanceledException(ExceptionMessages.FingerprintCollectionCancelled);

                default:
                    throw new InvalidOperationException(generalErrorMsg);
            }
        }

        /// <summary>
        ///     Set the friendly name of a template. If the template already has a
        ///     friendly name, this will replace it.
        /// </summary>
        /// <remarks>
        ///     If the template already has a friendly name, this method will replace
        ///     it.
        ///     <para>
        ///         This method will not check to see if the YubiKey has a fingerprint
        ///         entry with the given name already. In other words, using this method
        ///         allows you to use the same friendly name for more than one template.
        ///     </para>
        /// </remarks>
        /// <param name="templateId">
        ///     The ID for the template which will be given the name.
        /// </param>
        /// <param name="friendlyName">
        ///     The friendly name you want to give the template.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     The YubiKey rejects the friendly name, likely because it is too long.
        /// </exception>
        public void SetBioTemplateFriendlyName(ReadOnlyMemory<byte> templateId, string friendlyName)
        {
            ReadOnlyMemory<byte> currentToken = GetAuthToken(
                forceNewToken: false, PinUvAuthTokenPermissions.BioEnrollment);

            var nameCmd = new BioEnrollSetFriendlyNameCommand(templateId, friendlyName, currentToken, AuthProtocol);
            Fido2Response nameRsp = Connection.SendCommand(nameCmd);
            if (nameRsp.CtapStatus == CtapStatus.PinAuthInvalid)
            {
                currentToken = GetAuthToken(forceNewToken: false, PinUvAuthTokenPermissions.BioEnrollment);
                nameCmd = new BioEnrollSetFriendlyNameCommand(templateId, friendlyName, currentToken, AuthProtocol);
                nameRsp = Connection.SendCommand(nameCmd);
            }

            if (nameRsp.Status != ResponseStatus.Success)
            {
                throw new ArgumentException(nameRsp.StatusMessage);
            }
        }

        /// <summary>
        ///     Try to remove a template from a YubiKey. If there is no enrollment on
        ///     the YubiKey for the given template ID, this method will do nothing
        ///     and return <c>true</c>.
        /// </summary>
        /// <param name="templateId">
        ///     The ID for the template which will be removed.
        /// </param>
        /// <returns>
        ///     A boolean, <c>true</c> if the entry was removed, <c>false</c>
        ///     otherwise.
        /// </returns>
        public bool TryRemoveBioTemplate(ReadOnlyMemory<byte> templateId)
        {
            ReadOnlyMemory<byte> currentToken = GetAuthToken(
                forceNewToken: false, PinUvAuthTokenPermissions.BioEnrollment);

            var removeCmd = new BioEnrollRemoveCommand(templateId, currentToken, AuthProtocol);
            Fido2Response removeRsp = Connection.SendCommand(removeCmd);
            if (removeRsp.CtapStatus == CtapStatus.PinAuthInvalid)
            {
                currentToken = GetAuthToken(forceNewToken: false, PinUvAuthTokenPermissions.BioEnrollment);
                removeCmd = new BioEnrollRemoveCommand(templateId, currentToken, AuthProtocol);
                removeRsp = Connection.SendCommand(removeCmd);
            }

            return removeRsp.Status == ResponseStatus.Success || removeRsp.CtapStatus == CtapStatus.InvalidOption;
        }
    }
}
