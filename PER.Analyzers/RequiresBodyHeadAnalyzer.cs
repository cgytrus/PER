using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace PER.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RequiresBodyHeadAnalyzer : DiagnosticAnalyzer {
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(per0001, per0003, per0002);

    private static readonly DiagnosticDescriptor per0001 = new("PER0001",
        "Method requires body or head",
        "Method '{0}' requires {1}",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Callers of methods requiring body or head should verify that they are not null or require body or head themselves."
    );

    private static readonly DiagnosticDescriptor per0002 = new("PER0002",
        "Unused body or head requirement",
        "Unused {1} requirement on '{0}'",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Body or head requirement on a method that does not use body or head."
    );

    private static readonly DiagnosticDescriptor per0003 = new("PER0003",
        "Attribute overrides inherited body or head requirement state",
        "Attribute on '{0}' overrides inherited {1} requirement state",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Body/head requirement cannot override inherited state."
    );

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(FindMissingRequires, OperationKind.MethodBody, OperationKind.ConstructorBody);
        context.RegisterSemanticModelAction(VerifyRequires);
    }

    private static void FindMissingRequires(OperationAnalysisContext context) {
        if (context.Operation.SemanticModel is null)
            return;

        (bool requiresBody, bool requiresHead) = Util.GetRequires(context.ContainingSymbol);
        foreach ((IOperation operation, ISymbol symbol) in Util.FindInvocations(context.Operation)) {
            (bool body, bool head) = Util.GetRequires(symbol);
            Location location = operation.Syntax.GetLocation();
            string name = symbol.ToMinimalDisplayString(context.Operation.SemanticModel, location.SourceSpan.Start);
            if (!requiresBody && body)
                context.ReportDiagnostic(Diagnostic.Create(per0001, location, name, "body"));
            if (!requiresHead && head)
                context.ReportDiagnostic(Diagnostic.Create(per0001, location, name, "head"));
        }
    }

    private static void VerifyRequires(SemanticModelAnalysisContext context) {
        VerifyRequires(context, context.SemanticModel.Compilation.Assembly.GlobalNamespace);
    }

    private static void VerifyRequires(SemanticModelAnalysisContext context, ISymbol symbol) {
        ImmutableArray<AttributeData> attributes = symbol.GetAttributes();
        AttributeData? bodyAttr = attributes.FirstOrDefault(x =>
            x.AttributeClass?.ToDisplayString() == "PER.Abstractions.Meta.RequiresBodyAttribute");
        AttributeData? headAttr = attributes.FirstOrDefault(x =>
            x.AttributeClass?.ToDisplayString() == "PER.Abstractions.Meta.RequiresHeadAttribute");
        if (bodyAttr is not null || headAttr is not null) {
            ((bool used, bool invalid) body, (bool used, bool invalid) head) = Util.VerifyRequires(context.SemanticModel, symbol);
            if (bodyAttr is not null) {
                if (!body.used) {
                    context.ReportDiagnostic(Diagnostic.Create(per0002,
                        bodyAttr.ApplicationSyntaxReference?.GetSyntax().GetLocation(), symbol.Name, "body"));
                }
                if (body.invalid) {
                    context.ReportDiagnostic(Diagnostic.Create(per0003,
                        bodyAttr.ApplicationSyntaxReference?.GetSyntax().GetLocation(), symbol.Name, "body"));
                }
            }
            if (headAttr is not null) {
                if (!head.used) {
                    context.ReportDiagnostic(Diagnostic.Create(per0002,
                        headAttr.ApplicationSyntaxReference?.GetSyntax().GetLocation(), symbol.Name, "head"));
                }
                if (head.invalid) {
                    context.ReportDiagnostic(Diagnostic.Create(per0003,
                        headAttr.ApplicationSyntaxReference?.GetSyntax().GetLocation(), symbol.Name, "head"));
                }
            }
        }
        if (symbol is not INamespaceOrTypeSymbol type)
            return;
        foreach (ISymbol member in type.GetMembers())
            VerifyRequires(context, member);
    }
}
