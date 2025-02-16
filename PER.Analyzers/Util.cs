using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace PER.Analyzers;

public static class Util {
    public static (bool body, bool head) GetRequiresBodyHead(ISymbol symbol) {
        // TODO: handle inheritance
        // TODO: disallow overwriting inherited state
        ImmutableArray<AttributeData> attributes = symbol.GetAttributes();
        return (
            attributes.Any(x => x.AttributeClass?.ToDisplayString() == "PER.Abstractions.Meta.RequiresBodyAttribute"),
            attributes.Any(x => x.AttributeClass?.ToDisplayString() == "PER.Abstractions.Meta.RequiresHeadAttribute")
        );
    }

    public static (bool body, bool head) IsRelevantDiagnostic(SemanticModel semanticModel, Diagnostic diagnostic) {
        if (diagnostic.Id != "CS8602")
            return (false, false);
        ITypeSymbol? globals = semanticModel.Compilation.GlobalNamespace
            .GetNamespaceMembers().FirstOrDefault(x => x.Name == "PER")?
            .GetNamespaceMembers().FirstOrDefault(x => x.Name == "Abstractions")?
            .GetTypeMembers("Globals").FirstOrDefault();
        Location location = diagnostic.Location;
        string? name = location.SourceTree?.ToString().Substring(location.SourceSpan.Start, location.SourceSpan.Length);
        return semanticModel.LookupSymbols(location.SourceSpan.Start, globals, name).OfType<IPropertySymbol>().Any() ?
            (name is "resources" or "game", name is "renderer" or "screens" or "input" or "audio") : (false, false);
    }
}
