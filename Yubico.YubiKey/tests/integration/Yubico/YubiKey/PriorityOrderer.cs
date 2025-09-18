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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Yubico.YubiKey;

public class PriorityOrderer : ITestCaseOrderer
{
    public const string TypeName = "Yubico.YubiKey.PriorityOrderer";
    public const string AssembyName = "Yubico.YubiKey.IntegrationTests";

    public static readonly ConcurrentDictionary<string, ConcurrentQueue<string>>
        QueuedTests = new();

    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(
        IEnumerable<TTestCase> testCases)
        where TTestCase : ITestCase
    {
        return testCases.OrderBy(GetOrder);
    }

    private static int GetOrder<TTestCase>(
        TTestCase testCase)
        where TTestCase : ITestCase
    {
        // Enqueue the test name.
        QueuedTests
            .GetOrAdd(
                testCase.TestMethod.TestClass.Class.Name,
                key => new ConcurrentQueue<string>())
            .Enqueue(testCase.TestMethod.Method.Name);

        // Order the test based on the attribute.
        var attr = testCase.TestMethod.Method
            .ToRuntimeMethod()
            .GetCustomAttribute<TestPriorityAttribute>();
        return attr?.Priority ?? 0;
    }
}
