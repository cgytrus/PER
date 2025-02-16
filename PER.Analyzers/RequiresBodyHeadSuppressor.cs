using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PER.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SampleSemanticSuppressor : DiagnosticSuppressor {
    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = ImmutableArray.Create(rule);

    private static readonly SuppressionDescriptor rule = new("PER0001",
        "CS8602",
        "Suppress null references to PER.Abstractions.Globals as they are checked by RequiresBody and RequiresHead attributes."
    );

    public override void ReportSuppressions(SuppressionAnalysisContext context) {
        foreach (Diagnostic diagnostic in context.ReportedDiagnostics) {
            if (diagnostic.Location.SourceTree is null)
                continue;
            SemanticModel semanticModel = context.GetSemanticModel(diagnostic.Location.SourceTree);
            ISymbol? symbol = semanticModel.GetEnclosingSymbol(diagnostic.Location.SourceSpan.Start);
            if (symbol is null)
                continue;
            (bool requiresBody, bool requiresHead) = Util.GetRequiresBodyHead(symbol);
            (bool body, bool head) = Util.IsRelevantDiagnostic(semanticModel, diagnostic);
            if (body && head && requiresBody && requiresHead ||
                body && requiresBody ||
                head && requiresHead)
                context.ReportSuppression(Suppression.Create(rule, diagnostic));
        }
    }
}
