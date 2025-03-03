// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.NetCore.Analyzers.Runtime;
using Xunit;

using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.NetCore.Analyzers.Runtime.UseCancellationTokenInAsyncMethods, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.NetAnalyzers.UnitTests.Microsoft.NetCore.Analyzers.Runtime
{
    public class UseCancellationTokenInAsyncMethodsTests
    {
        private static DiagnosticDescriptor RuleId => UseCancellationTokenInAsyncMethods.Rule;

        [Fact]
        public Task Temp()
        {
            string testCode = $@"
using System;
using System.Threading;
using System.Threading.Tasks;

public class SomeClass
{{
    public void Void()
    {{
    }}

    public int Int() => 5;

    public Task {{|#0:MethodWithoutCancellationTokenOverload|}}()
    {{
        return Task.CompletedTask;
    }}

    public Task MethodWithoutCancellationTokenOverload(int another)
    {{
        return Task.CompletedTask;
    }}

    public Task MethodWithCancellationTokenOverload()
    {{
        return Task.CompletedTask;
    }}

    public Task MethodWithCancellationTokenOverload(CancellationToken ct)
    {{
        return Task.CompletedTask;
    }}
}}";
            return VerifyCS.VerifyAnalyzerAsync(testCode, VerifyCS.Diagnostic(RuleId).WithLocation(0));
        }
    }
}
