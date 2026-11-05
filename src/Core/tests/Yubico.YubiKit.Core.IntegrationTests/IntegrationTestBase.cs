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

namespace Yubico.YubiKit.Core.IntegrationTests;

/// <summary>
/// Base for one-shot applet integration tests. Tracking tests explicitly own their own monitor.
/// </summary>
public abstract class IntegrationTestBase : IAsyncDisposable
{
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}