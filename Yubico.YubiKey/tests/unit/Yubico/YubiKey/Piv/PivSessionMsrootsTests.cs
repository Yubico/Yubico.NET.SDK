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
using System.IO;
using Xunit;

namespace Yubico.YubiKey.Piv;

public class PivSessionMsrootsTests : PivSessionUnitTestBase
{
    [Fact]
    public void Write_TooMuchData_ThrowsOutOfRangeException()
    {
        var inputData = new byte[16000];
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => PivSessionMock.WriteMsroots(inputData));
    }

    [Fact]
    public void Write_NoKeyCollector_ThrowsInvalidOpException()
    {
        var inputData = new byte[100];
        PivSessionMock.KeyCollector = null;
        _ = Assert.Throws<InvalidOperationException>(() => PivSessionMock.WriteMsroots(inputData));
    }

    [Fact]
    public void Write_KeyCollectorFalse_ThrowsCanceledException()
    {
        var inputData = new byte[100];
        PivSessionMock.KeyCollector = ReturnFalseKeyCollectorDelegate;
        _ = Assert.Throws<OperationCanceledException>(() => PivSessionMock.WriteMsroots(inputData));
    }

    [Fact]
    public void WriteStream_TooMuchData_ThrowsOutOfRangeException()
    {
        var inputData = new byte[16000];
        var memStream = new MemoryStream(inputData);
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => PivSessionMock.WriteMsrootsStream(memStream));
    }

    [Fact]
    public void WriteStream_NoKeyCollector_ThrowsInvalidOpException()
    {
        var inputData = new byte[100];
        var memStream = new MemoryStream(inputData);
        PivSessionMock.KeyCollector = null;
        _ = Assert.Throws<InvalidOperationException>(() => PivSessionMock.WriteMsrootsStream(memStream));
    }

    [Fact]
    public void WriteStream_KeyCollectorFalse_ThrowsCanceledException()
    {
        var inputData = new byte[100];
        var memStream = new MemoryStream(inputData);
        PivSessionMock.KeyCollector = ReturnFalseKeyCollectorDelegate;
        _ = Assert.Throws<OperationCanceledException>(() => PivSessionMock.WriteMsrootsStream(memStream));
    }

    [Fact]
    public void Delete_NoKeyCollector_ThrowsInvalidOpException()
    {
        PivSessionMock.KeyCollector = null;
        _ = Assert.Throws<InvalidOperationException>(() => PivSessionMock.DeleteMsroots());
    }

    [Fact]
    public void Delete_KeyCollectorFalse_ThrowsCanceledException()
    {
        PivSessionMock.KeyCollector = ReturnFalseKeyCollectorDelegate;
        _ = Assert.Throws<OperationCanceledException>(() => PivSessionMock.DeleteMsroots());
    }

    private static bool ReturnFalseKeyCollectorDelegate(
        KeyEntryData _)
    {
        return false;
    }
}
