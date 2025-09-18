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
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Yubico.Core.Logging;

public class LogTests : IDisposable
{
    private readonly ILoggerFactory _originalFactory = Log.Instance;

#pragma warning disable CA1816
    public void Dispose()
#pragma warning restore CA1816
    {
        // Reset to the original factory after each test
        Log.Instance = _originalFactory;
    }

    // Ensure that the default LoggerFactory is created when no configuration is provided.
    [Fact]
    public void DefaultLoggerFactory_IsCreated_WhenNoConfigurationProvided()
    {
        // Act
        var loggerFactory = Log.Instance;

        // Assert
        Assert.NotNull(loggerFactory);
        ILogger logger = loggerFactory.CreateLogger<LogTests>();
        Assert.NotNull(logger);
    }

    // Ensure that LoggerFactory can be replaced manually using the Instance property.
    [Fact]
    public void ManualLoggerFactory_SettingInstance_OverridesDefaultFactory()
    {
        // Arrange
        var mockLoggerFactory = Substitute.For<ILoggerFactory>();
        Log.Instance = mockLoggerFactory;

        // Act
        var actualFactory = Log.Instance;

        // Assert
        Assert.Same(mockLoggerFactory, actualFactory);
    }

    // Ensure that LoggerFactory can be replaced manually using the Instance property.
    // Remove this once we remove Log.Legacy.cs
    [Fact]
    public void Legacy_ManualLoggerFactory_SettingInstance_OverridesDefaultFactory()
    {
        // Arrange
        var mockLoggerFactory = Substitute.For<ILoggerFactory>();
#pragma warning disable CS0618 // Type or member is obsolete
        Log.LoggerFactory = mockLoggerFactory;
#pragma warning restore CS0618 // Type or member is obsolete

        // Act
        var actualFactory = Log.Instance;

        // Assert
        Assert.Same(mockLoggerFactory, actualFactory);
    }
}
