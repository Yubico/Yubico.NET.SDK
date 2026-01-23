// Copyright 2026 Yubico AB
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

using Xunit;

namespace Yubico.YubiKit.Management.UnitTests;

public class ManagementSessionTests
{
    [Fact]
    public void IManagementSession_InheritsIAsyncDisposable()
    {
        // Verify that IManagementSession inherits from IAsyncDisposable
        Assert.True(typeof(IAsyncDisposable).IsAssignableFrom(typeof(IManagementSession)));
    }

    [Fact]
    public void ManagementSession_ImplementsIAsyncDisposable()
    {
        // Verify that ManagementSession implements IAsyncDisposable
        Assert.True(typeof(IAsyncDisposable).IsAssignableFrom(typeof(ManagementSession)));
    }
}
