using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PER.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RequiresBodyHeadAnalyzer : DiagnosticAnalyzer {
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(per0001, per0002, per0003);

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
        //if (!Debugger.IsAttached)
        //    return;
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSemanticModelAction(VerifyRequires);
    }

    private static void VerifyRequires(SemanticModelAnalysisContext context) {
        (Dictionary<ISymbol, Location?> bodies, Dictionary<ISymbol, Location?> heads) visited = ([], []);
        (HashSet<ISymbol> bodies, HashSet<ISymbol> heads) used = ([], []);

        foreach (ISymbol symbol in context.SemanticModel.SyntaxTree.GetRoot().DescendantNodes()
            .Where(x => x is ClassDeclarationSyntax or StructDeclarationSyntax or InterfaceDeclarationSyntax)
            .Select(x => context.SemanticModel.GetDeclaredSymbol(x))
            .OfType<ISymbol>()) {
            VisitSymbol(symbol);
        }

        foreach (KeyValuePair<ISymbol, Location?> pair in visited.bodies
            .Where(pair => !used.bodies.Contains(pair.Key))) {
            context.ReportDiagnostic(Diagnostic.Create(per0002, pair.Value,
                pair.Key.ToMinimalDisplayString(context.SemanticModel, pair.Value?.SourceSpan.Start ?? 0), "body"));
        }
        foreach (KeyValuePair<ISymbol, Location?> pair in visited.heads
            .Where(pair => !used.heads.Contains(pair.Key))) {
            context.ReportDiagnostic(Diagnostic.Create(per0002, pair.Value,
                pair.Key.ToMinimalDisplayString(context.SemanticModel, pair.Value?.SourceSpan.Start ?? 0), "head"));
        }

        return;

        void MarkVisited(ISymbol symbol) {
            (Util.BodyHeadSource? body, Util.BodyHeadSource? head) = Util.GetRequiresSelf(symbol);
            if (body is not null)
                visited.bodies[body.Value.symbol] = body.Value.location;
            if (head is not null)
                visited.heads[head.Value.symbol] = head.Value.location;
        }
        void VisitSymbol(ISymbol symbol) {
            MarkVisited(symbol);
            //if (symbol.IsAbstract) {
            //    (IEnumerable<Util.BodyHeadSource> bodies, IEnumerable<Util.BodyHeadSource> heads,
            //        (IEnumerable<Util.BodyHeadSource> bodies, IEnumerable<Util.BodyHeadSource> heads) invalid)
            //        requires = Util.GetRequires(symbol);
            //    foreach (Util.BodyHeadSource body in requires.bodies)
            //        used.bodies.Add(body.symbol);
            //    foreach (Util.BodyHeadSource head in requires.heads)
            //        used.heads.Add(head.symbol);
            //}
            foreach (IMethodSymbol method in Util.GetMethods(symbol)) {
                MarkVisited(method);
                (IEnumerable<Util.BodyHeadSource> bodies, IEnumerable<Util.BodyHeadSource> heads,
                    (IEnumerable<Util.BodyHeadSource> bodies, IEnumerable<Util.BodyHeadSource> heads) invalid)
                    requires = Util.GetRequires(method);
                if (method.IsAbstract) {
                    foreach (Util.BodyHeadSource body in requires.bodies)
                        used.bodies.Add(body.symbol);
                    foreach (Util.BodyHeadSource head in requires.heads)
                        used.heads.Add(head.symbol);
                }
                foreach (IOperation operation in method.DeclaringSyntaxReferences
                    .Select(x => x.GetSyntax()).OfType<MethodDeclarationSyntax>()
                    .Select(x => x.Body as SyntaxNode ?? x.ExpressionBody).OfType<SyntaxNode>()
                    .Select(x => context.SemanticModel.GetOperation(x)).OfType<IOperation>()) {
                    foreach ((IOperation invocation, ISymbol invokedSym) in Util.FindInvocations(operation)) {
                        (IEnumerable<Util.BodyHeadSource> bodies, IEnumerable<Util.BodyHeadSource> heads,
                            (IEnumerable<Util.BodyHeadSource>, IEnumerable<Util.BodyHeadSource>))
                            invoked = Util.GetRequires(invokedSym);
                        Location location = invocation.Syntax.GetLocation();
                        string name = invokedSym.ToMinimalDisplayString(context.SemanticModel, location.SourceSpan.Start);
                        VisitBodiesHeads(context, invoked.bodies, requires.bodies, used.bodies, location, name, "body");
                        VisitBodiesHeads(context, invoked.heads, requires.heads, used.heads, location, name, "head");
                    }
                }
                foreach (Util.BodyHeadSource body in requires.invalid.bodies) {
                    context.ReportDiagnostic(Diagnostic.Create(per0003, body.location, method.Name, "body"));
                }
                foreach (Util.BodyHeadSource head in requires.invalid.heads) {
                    context.ReportDiagnostic(Diagnostic.Create(per0003, head.location, method.Name, "head"));
                }
            }

            if (symbol is not INamespaceOrTypeSymbol type)
                return;
            foreach (ISymbol member in type.GetMembers())
                VisitSymbol(member);
        }
    }

    private static void VisitBodiesHeads(SemanticModelAnalysisContext context, IEnumerable<Util.BodyHeadSource> invoked,
        IEnumerable<Util.BodyHeadSource> requires, HashSet<ISymbol> usedRequires, Location location,
        params object?[]? messageArgs) {
        IEnumerable<Util.BodyHeadSource> invokedList = invoked.ToList();
        if (!invokedList.Any())
            return;
        IEnumerable<Util.BodyHeadSource> requiresList = requires.ToList();
        if (requiresList.Any()) {
            foreach (Util.BodyHeadSource requiresBody in requiresList)
                usedRequires.Add(requiresBody.symbol);
            return;
        }
        foreach (Util.BodyHeadSource invokedBody in invokedList) {
            context.ReportDiagnostic(Diagnostic.Create(per0001, location,
                invokedBody.symbol.Locations, messageArgs));
        }
    }
}
