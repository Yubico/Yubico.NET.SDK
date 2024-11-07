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

using System.Diagnostics;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Scp.Commands
{
    internal class ScpResponse : YubiKeyResponse
    {
        public ScpResponse(ResponseApdu responseApdu) :
            base(responseApdu)
        {

        }

        public virtual new ResponseStatus Status => StatusWord switch
        {
            SWConstants.Success => ResponseStatus.Success,
            _ => ResponseStatus.Failed
        };

        public virtual void ThrowIfFailed(string? message = null)
        {
            switch (StatusWord)
            {
                case SWConstants.Success:
                    Debug.Assert(Status == ResponseStatus.Success);
                    return;
                default:
                    throw new SecureChannelException(AddStatusWord(message ?? StatusMessage)); 
            }

            string AddStatusWord(string message) => $"{message} (StatusWord: {StatusWord})";
        }
    }
}
