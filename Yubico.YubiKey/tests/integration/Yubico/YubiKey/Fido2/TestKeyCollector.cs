// Copyright 2025 Yubico AB
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
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace Yubico.YubiKey.Fido2;

public class TestKeyCollector(bool useComplexCreds = false)
{
    public List<KeyEntryRequest> CapturedRequests { get; } = [];

    public bool HandleRequest(
        KeyEntryData data)
    {
        CapturedRequests.Add(data.Request);

        switch (data.Request)
        {
            case KeyEntryRequest.VerifyFido2Pin:
            case KeyEntryRequest.SetFido2Pin:
                data.SubmitValue(useComplexCreds
                    ? FidoSessionIntegrationTestBase.ComplexPin.Span
                    : FidoSessionIntegrationTestBase.SimplePin.Span);
                break;

            case KeyEntryRequest.ChangeFido2Pin:
                if (data.IsRetry)
                {
                    data.SubmitValues(FidoSessionIntegrationTestBase.SimplePin.Span,
                        FidoSessionIntegrationTestBase.ComplexPin.Span);
                }
                else
                {
                    data.SubmitValues(FidoSessionIntegrationTestBase.ComplexPin.Span,
                        FidoSessionIntegrationTestBase.SimplePin.Span);
                }

                break;

            case KeyEntryRequest.Release:
                break;

            case KeyEntryRequest.TouchRequest:
                Debug.Assert(true, "Touch requested");
                Console.WriteLine("YubiKey requires touch");
                break;

            default:
                throw new NotSupportedException($"Request {data.Request} not supported by this test");
        }

        return true;
    }
    
    public void ResetRequestCounts() => CapturedRequests.Clear();
    
    public void VerifyRequestSequence(
        params KeyEntryRequest[] expectedSequence)
    {
        Assert.Equal(expectedSequence, CapturedRequests);
    }

    public void VerifyRequestCount(
        KeyEntryRequest request,
        int expectedCount)
    {
        var actualCount = CapturedRequests.Count(r => r == request);
        Assert.Equal(expectedCount, actualCount);
    }
}
