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

namespace Yubico.YubiKey
{
    /// <summary>
    /// An interface which allows for retrieval of returned data.
    /// </summary>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <remarks>
    /// Implementations of <see cref="IYubiKeyResponse"/> which also need to return data should implement
    /// this interface. Doing so provides callers a uniform means of retrieving data. Data returned can
    /// either be basic data types (strings, integers, etc.) or can be classes or structures representing
    /// complex data.
    /// </remarks>
    public interface IYubiKeyResponseWithData<out TData> : IYubiKeyResponse
    {
        /// <summary>
        /// Gets the data from the YubiKey response.
        /// </summary>
        /// <remarks>
        /// If the method cannot return the data, it will throw an exception.
        /// This happens when the <see cref="IYubiKeyResponse.Status"/> property indicates
        /// an error, or the data returned from the YubiKey was malformed or
        /// incomplete.
        /// <para>
        /// For example,
        /// <code language="csharp">
        /// IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);
        /// GetDataCommand getDataCommand = new GetDataCommand(PivDataTag.Chuid);
        /// GetDataResponse getDataResponse = connection.SendCommand(getDataCommand);
        /// if (getDataResponse.Status == ResponseStatus.Success)
        /// {
        ///     byte[] getChuid = getDataResponse.GetData();
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        /// <returns>
        /// The data returned by the YubiKey, presented in a manner specific to each
        /// implementation.
        /// </returns>
        TData GetData();
    }
}
