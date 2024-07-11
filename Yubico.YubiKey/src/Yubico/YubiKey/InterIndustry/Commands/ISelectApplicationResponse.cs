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
using System.Text;

namespace Yubico.YubiKey.InterIndustry.Commands
{
    /// <summary>
    /// Represents the results of a select application command that returns a<see cref="ISelectApplicationData" />.
    /// </summary>
    /// <typeparam name="TData">Specific type of data returned by the Select Application command.  NOTE: This is argument is covariant.</typeparam>
    public interface ISelectApplicationResponse<out TData> : IYubiKeyResponseWithData<TData>
        where TData : ISelectApplicationData
    {

    }
}
