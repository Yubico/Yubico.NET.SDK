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
using System.Security.Cryptography;
using Yubico.Core.Cryptography;

namespace Yubico.YubiKey
{
    /// <summary>
    /// This delegate defines the signature of a method that can be called to let
    /// the SDK know that the user is canceling an operation.
    /// </summary>
    /// <remarks>
    /// Generally a KeyCollector indicates a user cancels by returning
    /// <c>false</c>. However, for some requests, such as
    /// <see cref ="KeyEntryRequest.TouchRequest"/> and
    /// <see cref ="KeyEntryRequest.EnrollFingerprint"/>, the KeyCollector's
    /// return value is ignored because it is an asynchronous call. In those
    /// cases, the SDK will supply an implementation of this delegate that your
    /// KeyCollector can call to notify the SDK that the user has canceled.
    /// </remarks>
    public delegate void SignalUserCancel();

    /// <summary>
    /// This class contains methods and data that describe the state of the
    /// process to provide keys, PINs, and other sensitive data to the SDK.
    /// </summary>
    /// <remarks>
    /// At times, the SDK will need the caller to provide keys, PINs, or other
    /// sensitive data. Generally, this will be done through a delegate
    /// (callback). The caller provides a method the SDK can call requesting the
    /// key, PIN, or whatever element is needed. This method will take as an
    /// argument an instance of this <c>KeyEntryData</c> class.
    /// <p>
    /// When the SDK calls the delegate, it will pass an instance of this class,
    /// which contains information the method can use to perform its operations.
    /// For example, the SDK, when calling the delegate, must describe what
    /// element it is requesting, and whether this is the first request, or a
    /// subsequent request because the previous data returned did not verify.
    /// </p>
    /// </remarks>
    public sealed class KeyEntryData
    {
        /// <summary>
        /// This indicates what the SDK is requesting.
        /// </summary>
        /// <remarks>
        /// Note that a delegate MUST NEVER throw an exception if the
        /// <c>Request</c> is <c>KeyEntryRequest.Release</c>. The <c>Release</c>
        /// is called from inside a <c>finally</c> block, and it is a bad idea to
        /// throw exceptions from inside <c>finally</c>.
        /// </remarks>
        public KeyEntryRequest Request { get; set; }

        /// <summary>
        /// This is the number of retries remaining before the element requested
        /// is blocked. This can be null if the element is one that is never
        /// blocked or the retries remaining count is not known yet because the
        /// <c>KeyEntryData</c> represents the initial request.
        /// </summary>
        /// <remarks>
        /// For some elements there is a retry count. It is the number of times
        /// in a row a wrong value can be entered for verification before the
        /// element is blocked. Other elements have no limitation. For example,
        /// the PIV PIN starts out with a retry count of 3 (this count can be
        /// changed). If you try to verify the PIN but enter the wrong value, the
        /// retries remaining will be decremented to 2. Verify using the correct
        /// PIN and the retries remaining returns to 3. If it is decremented to
        /// 0, the PIN is blocked, and the YubiKey PIV application will not be
        /// able to perform operations that require the PIN, even if the correct
        /// PIN is entered later. Restore the PIN using the PUK.
        /// <p>
        /// There are some elements that have no limit. For example, the PIV
        /// management key is a triple-DES key, and you can try and fail to
        /// authenticate that key as many times as you want and it will never be
        /// blocked.
        /// </p>
        /// <p>
        /// This property starts out as null because the number of retries
        /// remaining is not known until the YubiKey is contacted. If an attempt
        /// to verify an element that has a retry count is made, and the value is
        /// incorrect, the YubiKey will report the number of retries remaining.
        /// If that happens, this property will be set with the number.
        /// </p>
        /// <p>
        /// If the correct value is given, the YubiKey will not report the
        /// retries remaining. If that happens, this property will be set to null.
        /// </p>
        /// <p>
        /// If the item requested has no limited retry count, this will be null,
        /// even if a previous attempt made to authenticate it had failed.
        /// </p>
        /// <p>
        /// If the item requested has a limited retry count, and this is a call
        /// to get the item after a previous call failed, and this number is 0,
        /// that means the item is blocked.
        /// </p>
        /// <p>
        /// If the element requested is one that has a retry account, and this is
        /// not null, then you know the request is a "retry", that the previous
        /// attempt failed. There is another property, <c>isRetry</c>, that
        /// specifically indicates if the call is a retry or not, and it is valid
        /// for all elements, those that have a retry count and those that do
        /// not. So you will likely use that property to determine if a request
        /// is a retry or not.
        /// </p>
        /// </remarks>
        public int? RetriesRemaining { get; set; }

        /// <summary>
        /// Indicates if the current request for an item has already been tried
        /// and was incorrect. That is, is the current request the initial
        /// request or did a previous attempt fail and the SDK is requesting the
        /// <c>KeyCollector</c> try again? If <c>true</c>, the request is for a
        /// retry, if <c>false</c>, the request is the initial attempt.
        /// </summary>
        /// <remarks>
        /// Note that enrolling a fingerprint generally requires several samples,
        /// and each sample is not a retry, but rather more information the
        /// YubiKey uses to build a template. Hence, after each fingerprint
        /// sample, this property will not be true, even if the previous sample
        /// was considered a failure. Rather, look at the
        /// <see cref="LastBioEnrollSampleResult"/> property to get information
        /// about the previous attempt.
        /// </remarks>
        public bool IsRetry { get; set; }

        /// <summary>
        /// Indicates if the current request for an item has violated PIN complexity.
        /// </summary>
        public bool IsViolatingPinComplexity { get; set; }

        /// <summary>
        /// This is the result of the last fingerprint sample. This will be null
        /// if the <c>Request</c> is for something other than
        /// <see cref="KeyEntryRequest.EnrollFingerprint"/> or if it is the first
        /// call to Enroll.
        /// </summary>
        /// <remarks>
        /// When a caller wants to enroll a fingerprint, the SDK will call the
        /// <c>KeyCollector</c> with a <see cref="Request"/> of
        /// <see cref="KeyEntryRequest.EnrollFingerprint"/>. For the first call
        /// to the <c>KeyCollector</c>, there is no previous sample, so this will
        /// be null. For each subsequent call, the SDK will provide the result
        /// from the most recent sample. This includes the sample status (such as
        /// <see cref="BioEnrollSampleStatus.FpGood"/> or
        /// <see cref="BioEnrollSampleStatus.FpPoorQuality"/>), and the number of
        /// quality samples needed to complete the enrollment.
        /// <para>
        /// Your <c>KeyCollector</c> will have this information which you can
        /// pass on to the user. For example, one property in the
        /// <c>BioEnrollSampleResult</c> is the reason a fingerprint sample was
        /// not accepted. Letting the user know this reason could help them make
        /// a better sample next time.
        /// </para>
        /// </remarks>
        public BioEnrollSampleResult? LastBioEnrollSampleResult { get; set; }

        /// <summary>
        /// For some operations, this property is an implementation of a delegate
        /// the KeyCollector can call to indicate the user is canceling the
        /// operation. If it is null, report cancellation normally, by having the
        /// KeyCollector return <c>false</c>. If it is not null, use the supplied
        /// delegate to report user cancellation.
        /// </summary>
        /// <remarks>
        /// Currently this is valid only when the <c>Request</c> is either
        /// <see cref="KeyEntryRequest.TouchRequest"/>,
        /// <see cref="KeyEntryRequest.EnrollFingerprint"/>, or
        /// <see cref="KeyEntryRequest.VerifyFido2Uv"/>. For all other
        /// requests, this will be null.
        /// <para>
        /// The normal way to indicate that a user is canceling an operation is
        /// to have the KeyCollector return <c>false</c>. However, for some
        /// operations, such as Touch and Fingerprint, the KeyCollector is called
        /// on a separate thread and the return is ignored. That is, it is an
        /// asynchronous call. The main thread performing the YubiKey operation
        /// will not see the KeyCollector's return. Hence, to indicate user
        /// cancellation in these cases, this delegate is provided.
        /// </para>
        /// <para>
        /// Your KeyCollector is called with an instance of this
        /// <c>KeyEntryData</c> class. If this property is not null, you can save
        /// this delegate. Later on, if the user cancels, you can call the
        /// delegate. If the YubiKey has not completed the operation or timed out
        /// by the time it receives notification of user cancellation, the SDK
        /// can cancel. Note that generally, user cancellation results in an
        /// <c>OperationCanceledException</c>.
        /// </para>
        /// </remarks>
        public SignalUserCancel? SignalUserCancel { get; private set; }

        private Memory<byte> _currentValue;

        private Memory<byte> _newValue;

        internal KeyEntryData(SignalUserCancel signalUserCancel)
            : this()
        {
            SignalUserCancel = signalUserCancel;
        }

        /// <summary>
        /// Create a new instance of the <c>KeyEntryData</c> class.
        /// </summary>
        /// <remarks>
        /// Note that this class can contain sensitive data and it is a good idea
        /// to call the <c>Clear</c> method when done with it. The <c>Clear</c>
        /// is not called automatically, because this class does not implement
        /// <c>IDisposable</c>. Hence, if your code creates an instance of this
        /// class, you should make sure to call <c>Clear</c> as soon as possible.
        /// </remarks>
        public KeyEntryData()
        {
            _currentValue = Memory<byte>.Empty;
            _newValue = Memory<byte>.Empty;
            IsRetry = false;
            IsViolatingPinComplexity = false;
        }

        /// <summary>
        /// Submit the requested value, when there is only one value to submit.
        /// </summary>
        /// <remarks>
        /// When the <c>KeyCollector</c> delegate obtains the value to return
        /// (the key, PIN, password, or whatever was requested), it calls this
        /// method to load it into the <c>KeyEntryData</c> object. The code that
        /// called the <c>KeyCollector</c> will be able to get the data returned
        /// using the <c>GetCurrentValue</c> method.
        /// <p>
        /// Note that the <c>KeyEntryData</c> object will copy the value passed
        /// in (it will not simply copy a reference). Therefore, it is safe to
        /// overwrite the data once <c>SubmitValue</c> has completed.
        /// </p>
        /// <p>
        /// This method will store the value submitted as the current value.
        /// </p>
        /// </remarks>
        /// <param name="value">
        /// The actual key, PIN, password, or whatever was requested.
        /// </param>
        public void SubmitValue(ReadOnlySpan<byte> value)
        {
            CryptographicOperations.ZeroMemory(_currentValue.Span);
            _currentValue = new Memory<byte>(value.ToArray());
        }

        /// <summary>
        /// Submit the requested values, when there are two values to submit.
        /// This is generally used when changing or resetting a value.
        /// </summary>
        /// <remarks>
        /// When the <c>KeyCollector</c> delegate obtains the values to return
        /// (both the current and new keys, PINs, passwords, or whatever was
        /// requested), it calls this method to load them into the
        /// <c>KeyEntryData</c> object. The code that called the
        /// <c>KeyCollector</c> will be able to get the data returned using the
        /// appropriate <c>GetValue</c> methods.
        /// <p>
        /// Note that the <c>KeyEntryData</c> object will copy the values passed
        /// in (it will not simply copy references). Therefore, it is safe to
        /// overwrite the data once <c>SubmitValues</c> has completed.
        /// </p>
        /// <p>
        /// For example, suppose the PIV PIN is being changed. The SDK will call
        /// the <c>KeyCollector</c> with the <c>Request</c> of
        /// <c>ChangePivPin</c>. The <c>KeyCollector</c> will collect the
        /// current PIN and a new PIN, then call <c>SubmitValues</c> with both of
        /// those values.
        /// </p>
        /// </remarks>
        /// <param name="currentValue">
        /// The current key, PIN, password, or whatever was requested.
        /// </param>
        /// <param name="newValue">
        /// A new key, PIN, password, or whatever was requested.
        /// </param>
        public void SubmitValues(ReadOnlySpan<byte> currentValue, ReadOnlySpan<byte> newValue)
        {
            CryptographicOperations.ZeroMemory(_currentValue.Span);
            CryptographicOperations.ZeroMemory(_newValue.Span);
            _currentValue = new Memory<byte>(currentValue.ToArray());
            _newValue = new Memory<byte>(newValue.ToArray());
        }

        /// <summary>
        /// Return a reference to the submitted current value.
        /// </summary>
        /// <remarks>
        /// There are two possible values: a current and new. For many case,
        /// there will only be a current. But for some operations (such as
        /// changing a PIV PIN), there will be both. To get the current value
        /// (e.g. the PIN if verifying the PIN, or the current PIN if changing
        /// the PIN), call this method. If you want to get the new value, call
        /// the <c>GetNewValue</c> method.
        /// <p>
        /// Note that this method returns a reference to the value, which means
        /// that the actual data returned will be overwritten after the call to
        /// <c>Clear</c>.
        /// </p>
        /// </remarks>
        /// <returns>
        /// A reference to the byte array that is the value requested.
        /// </returns>
        public ReadOnlyMemory<byte> GetCurrentValue() => _currentValue;

        /// <summary>
        /// Return a reference to the submitted new value.
        /// </summary>
        /// <remarks>
        /// There are two possible values: a current and new. For many case,
        /// there will only be a current. But for some operations (such as
        /// changing a PIV PIN), there will be both. To get the new value (e.g.
        /// the new PIN if changing the PIN), call this method. If you want to
        /// get the current value, call the <c>GetCurrentValue</c> method.
        /// <p>
        /// Note that this method returns a reference to the value, which means
        /// that the actual data returned will be overwritten after the call to
        /// <c>Clear</c>.
        /// </p>
        /// </remarks>
        /// <returns>
        /// A reference to the byte array that is the value requested.
        /// </returns>
        public ReadOnlyMemory<byte> GetNewValue() => _newValue;

        /// <summary>
        /// Clear any sensitive data in the object.
        /// </summary>
        public void Clear()
        {
            CryptographicOperations.ZeroMemory(_currentValue.Span);
            CryptographicOperations.ZeroMemory(_newValue.Span);
            RetriesRemaining = null;
            LastBioEnrollSampleResult = null;
            SignalUserCancel = null;
            IsRetry = false;
            IsViolatingPinComplexity = false;
        }
    }
}
