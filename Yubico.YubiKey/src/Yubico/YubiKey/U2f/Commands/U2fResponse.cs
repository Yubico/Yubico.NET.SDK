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
using System.Diagnostics;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.U2f.Commands
{
    /// <summary>
    /// Base class for all U2F responses. Use this class to represent the status of a U2F command,
    /// or one of its derived classes to retrieve the full response.
    /// </summary>
    /// <seealso cref="Yubico.YubiKey.IYubiKeyResponse" />
    public class U2fResponse : YubiKeyResponse
    {
        public U2fResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {

        }

        public virtual new ResponseStatus Status => StatusWord switch
        {
            SWConstants.ConditionsNotSatisfied => ResponseStatus.ConditionsNotSatisfied,
            _ => _Status
        };

        public virtual void ThrowIfFailed()
        {
            switch (StatusWord)
            {
                default:
                    _ThrowIfFailed();
                    break;
            }
        }

        private ResponseStatus _Status => StatusWord switch
        {
            SWConstants.Success => ResponseStatus.Success,
            _ => ResponseStatus.Failed
        };

        private void _ThrowIfFailed()
        {
            switch (StatusWord)
            {
                case SWConstants.Success:
                    Debug.Assert(Status == ResponseStatus.Success);
                    return;
                default:
                    throw new Exception(); 
            }
        }
    }
}
