using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace PER.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SampleSemanticAnalyzer : DiagnosticAnalyzer {
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(rule);

    private static readonly DiagnosticDescriptor rule = new("PER0001",
        "Method requires body or head",
        "Method '{0}' requires {1}",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Callers of methods requiring body or head should verify that they are not null or require body or head themselves."
    );

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        // TODO: allow on constructors
        context.RegisterOperationAction(AnalyzeOperation, OperationKind.MethodBody);
    }

    private static void AnalyzeOperation(OperationAnalysisContext context) {
        if (context.Operation is not IMethodBodyOperation operation ||
            operation.SemanticModel is null)
            return;

        (bool requiresBody, bool requiresHead) = Util.GetRequiresBodyHead(context.ContainingSymbol);
        // TODO: this apparently doesnt work with properties
        List<(IInvocationOperation op, bool body, bool head)> invocations = operation.ChildOperations.Flatten(x =>
            x is IMethodBodyOperation ? Array.Empty<IOperation>() : x.ChildOperations)
            .OfType<IInvocationOperation>()
            .Select(x => {
                (bool body, bool head) = Util.GetRequiresBodyHead(x.TargetMethod);
                return (x, body, head);
            })
            .ToList();
        int bodyReqCalls = invocations.Count(x => x.body);
        int headReqCalls = invocations.Count(x => x.head);

        List<(Diagnostic d, bool body, bool head)> diagnostics = operation.SemanticModel.GetDiagnostics().Select(x => {
            (bool body, bool head) = Util.IsRelevantDiagnostic(operation.SemanticModel, x);
            return (x, body, head);
        }).ToList();
        int bodyDiagnostics = diagnostics.Count(x => x.body);
        int headDiagnostics = diagnostics.Count(x => x.head);

        foreach ((IInvocationOperation op, bool body, bool head) in invocations) {
            if (!requiresBody && body)
                context.ReportDiagnostic(Diagnostic.Create(rule, op.Syntax.GetLocation(), op.TargetMethod.Name, "body"));
            if (!requiresHead && head)
                context.ReportDiagnostic(Diagnostic.Create(rule, op.Syntax.GetLocation(), op.TargetMethod.Name, "head"));
        }

        if (requiresBody && bodyDiagnostics == 0 && bodyReqCalls == 0) {
            // TODO: report unnecessary RequiresBody
        }
        if (requiresHead && headDiagnostics == 0 && headReqCalls == 0) {
            // TODO: report unnecessary RequiresHead
        }
    }
}
