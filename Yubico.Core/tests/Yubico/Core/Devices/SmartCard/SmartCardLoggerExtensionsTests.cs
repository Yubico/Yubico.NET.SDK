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

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Xunit;
using Yubico.PlatformInterop;

namespace Yubico.Core.Devices.SmartCard.UnitTests
{
    public class SmartCardLoggerExtensionsTests
    {
        // -----------------------------------------------------------------------------------------
        // Step 6: Verify that knownRecoverable flag downgrades failure logs to Debug
        // -----------------------------------------------------------------------------------------

        [Fact]
        public void SCardApiCall_WhenKnownRecoverableTrue_AndNonSuccess_LogsAtDebug()
        {
            // Arrange
            var fakeLogger = new FakeLogger();
            const string apiName = "SCardEstablishContext";
            const uint errorCode = ErrorCode.SCARD_E_INVALID_HANDLE;

            // Act
            fakeLogger.SCardApiCall(apiName, errorCode, knownRecoverable: true);

            // Assert
            Assert.Single(fakeLogger.LogEntries);
            LogEntry entry = fakeLogger.LogEntries[0];
            Assert.Equal(LogLevel.Debug, entry.Level);
            Assert.Contains(apiName, entry.Message);
            Assert.Contains("FAILED", entry.Message);
            Assert.Contains("known recoverable", entry.Message);
        }

        [Fact]
        public void SCardApiCall_WhenKnownRecoverableFalse_AndNonSuccess_LogsAtError()
        {
            // Arrange
            var fakeLogger = new FakeLogger();
            const string apiName = "SCardEstablishContext";
            const uint errorCode = ErrorCode.SCARD_E_INVALID_HANDLE;

            // Act
            fakeLogger.SCardApiCall(apiName, errorCode, knownRecoverable: false);

            // Assert
            Assert.Single(fakeLogger.LogEntries);
            LogEntry entry = fakeLogger.LogEntries[0];
            Assert.Equal(LogLevel.Error, entry.Level);
            Assert.Contains(apiName, entry.Message);
            Assert.Contains("FAILED", entry.Message);
        }

        [Fact]
        public void SCardApiCall_WhenSuccess_LogsAtInformationRegardlessOfKnownRecoverable()
        {
            // Arrange
            var fakeLogger = new FakeLogger();
            const string apiName = "SCardEstablishContext";
            const uint successCode = ErrorCode.SCARD_S_SUCCESS;

            // Act - knownRecoverable: true
            fakeLogger.SCardApiCall(apiName, successCode, knownRecoverable: true);

            // Assert
            Assert.Single(fakeLogger.LogEntries);
            LogEntry entry = fakeLogger.LogEntries[0];
            Assert.Equal(LogLevel.Information, entry.Level);
            Assert.Contains("successfully", entry.Message);

            // Reset and test knownRecoverable: false
            fakeLogger.LogEntries.Clear();
            fakeLogger.SCardApiCall(apiName, successCode, knownRecoverable: false);

            Assert.Single(fakeLogger.LogEntries);
            entry = fakeLogger.LogEntries[0];
            Assert.Equal(LogLevel.Information, entry.Level);
            Assert.Contains("successfully", entry.Message);
        }

        [Fact]
        public void SCardApiCall_OriginalOverload_AndNonSuccess_LogsAtError()
        {
            // Arrange
            var fakeLogger = new FakeLogger();
            const string apiName = "SCardEstablishContext";
            const uint errorCode = ErrorCode.SCARD_E_INVALID_HANDLE;

            // Act - original single-argument overload
            fakeLogger.SCardApiCall(apiName, errorCode);

            // Assert
            Assert.Single(fakeLogger.LogEntries);
            LogEntry entry = fakeLogger.LogEntries[0];
            Assert.Equal(LogLevel.Error, entry.Level);
            Assert.Contains(apiName, entry.Message);
            Assert.Contains("FAILED", entry.Message);
        }

        // -----------------------------------------------------------------------------------------
        // FakeLogger — Minimal ILogger implementation that captures log calls
        // -----------------------------------------------------------------------------------------

        private sealed class FakeLogger : ILogger
        {
            public List<LogEntry> LogEntries { get; } = new List<LogEntry>();

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                string message = formatter(state, exception);
                LogEntries.Add(new LogEntry(logLevel, message));
            }
        }

        private sealed class LogEntry
        {
            public LogLevel Level { get; }
            public string Message { get; }

            public LogEntry(LogLevel level, string message)
            {
                Level = level;
                Message = message;
            }
        }
    }
}
