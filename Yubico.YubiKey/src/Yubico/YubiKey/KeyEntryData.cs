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

namespace Yubico.YubiKey
{
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
        public bool IsRetry { get; set; }

        private Memory<byte> _currentValue;

        private Memory<byte> _newValue;

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
        }
    }
}
