// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.NetCore.Analyzers.Runtime
{
    using static MicrosoftNetCoreAnalyzersResources;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class UseCancellationTokenInAsyncMethods : DiagnosticAnalyzer
    {
        internal const string RuleId = "TEMP2000";

        internal static readonly DiagnosticDescriptor Rule = DiagnosticDescriptorHelper.Create(
            RuleId,
            // TODO
            CreateLocalizableResourceString(nameof(ForwardCancellationTokenToInvocationsTitle)),
            // TODO
            CreateLocalizableResourceString(nameof(ForwardCancellationTokenToInvocationsMessage)),
            DiagnosticCategory.Reliability,
            RuleLevel.IdeSuggestion,
            // TODO
            CreateLocalizableResourceString(nameof(ForwardCancellationTokenToInvocationsDescription)),
            isPortedFxCopRule: false,
            isDataflowRule: false
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationStartAction(context =>
            {
                if (!RequiredSymbols.TryGetSymbols(context.Compilation, out var symbols))
                    return;

                context.RegisterSymbolAction(c => Analyze(c, symbols), SymbolKind.NamedType);
            });
        }

        internal sealed class RequiredSymbols
        {
            public static bool TryGetSymbols(Compilation compilation, [NotNullWhen(true)] out RequiredSymbols? symbols)
            {
                symbols = default;

                var typeProvider = WellKnownTypeProvider.GetOrCreate(compilation);

                if (!typeProvider.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemThreadingTasksTask, out var systemThreadingTasksTask))
                    return false;

                if (!typeProvider.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemThreadingTasksTask1, out var systemThreadingTasksTask1))
                    return false;

                if (!typeProvider.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemThreadingTasksValueTask, out var systemThreadingTasksValueTask))
                    return false;

                if (!typeProvider.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCollectionsGenericIAsyncEnumerable1, out var systemCollectionsGenericIAsyncEnumerable1))
                    return false;

                if (!typeProvider.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemThreadingCancellationToken, out var systemThreadingCancellationToken))
                    return false;

                symbols = new RequiredSymbols()
                {
                    SystemThreadingTasksTask = systemThreadingTasksTask,
                    SystemThreadingTasksTask1 = systemThreadingTasksTask1,
                    SystemThreadingTasksValueTask = systemThreadingTasksValueTask,
                    SystemCollectionsGenericIAsyncEnumerable1 = systemCollectionsGenericIAsyncEnumerable1,
                    SystemThreadingCancellationToken = systemThreadingCancellationToken,
                };
                return true;
            }

            public INamedTypeSymbol? SystemThreadingTasksTask { get; init; }
            public INamedTypeSymbol? SystemThreadingTasksTask1 { get; init; }
            public INamedTypeSymbol? SystemThreadingTasksValueTask { get; init; }
            public INamedTypeSymbol? SystemCollectionsGenericIAsyncEnumerable1 { get; init; }
            public INamedTypeSymbol? SystemThreadingCancellationToken { get; init; }
        }

        private static void Analyze(SymbolAnalysisContext context, RequiredSymbols symbols)
        {
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;
            string name = namedTypeSymbol.Name;

            var methodOverloadsByName = namedTypeSymbol.GetMembers().OfType<IMethodSymbol>().GroupBy(m => m.Name);
            foreach (var methodsOverloads in methodOverloadsByName)
            {
                var first = methodsOverloads.First();
                //first.GetOverloads
                if (MethodHasAsyncCompatibleReturnType(first, symbols))
                {
                    if (!methodsOverloads.Any(method => HasCancellationTokenArgument(method, symbols)))
                    {
                        var diagnostic = first.Locations.First().CreateDiagnostic(Rule);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private static bool HasCancellationTokenArgument(IMethodSymbol method, RequiredSymbols symbols)
        {
            return method.Parameters.Any(p => p.Type.Equals(symbols.SystemThreadingCancellationToken));
        }

        private static bool MethodHasAsyncCompatibleReturnType(IMethodSymbol methodSymbol, RequiredSymbols symbols)
        {
            if (methodSymbol.ReturnType is null)
                return false;

            bool Matches(INamedTypeSymbol? expectedType)
                => methodSymbol.ReturnType.OriginalDefinition.Equals(expectedType);

            return Matches(symbols.SystemThreadingTasksTask)
                || Matches(symbols.SystemThreadingTasksTask1)
                || Matches(symbols.SystemThreadingTasksValueTask)
                || Matches(symbols.SystemCollectionsGenericIAsyncEnumerable1);
        }
    }
}
