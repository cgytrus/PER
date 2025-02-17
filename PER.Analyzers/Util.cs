using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace PER.Analyzers;

public static class Util {
    public static IEnumerable<(IOperation, ISymbol)> FindInvocations(IOperation operation) => operation.ChildOperations
        .Flatten(x => x is IMethodBodyOperation ? Array.Empty<IOperation>() : x.ChildOperations)
        .Where(x => x is IInvocationOperation or IPropertyReferenceOperation)
        .Select<IOperation, (IOperation, ISymbol)>(x => (x, x switch {
            IInvocationOperation invocation => invocation.TargetMethod,
            IPropertyReferenceOperation property => property.Property,
            _ => throw new ArgumentOutOfRangeException(nameof(x))
        }));

    public static (bool body, bool head) GetRequires(ISymbol symbol) {
        while (symbol is IMethodSymbol { OverriddenMethod: not null } overrideMethod)
            symbol = overrideMethod.OverriddenMethod;
        while (symbol is IMethodSymbol { ExplicitInterfaceImplementations.Length: > 0 } method)
            symbol = method.ExplicitInterfaceImplementations[0];
        (bool containingBody, bool containingHead) = symbol.ContainingSymbol is null ? (false, false) :
            GetRequires(symbol.ContainingSymbol);
        if (containingBody && containingHead)
            return (true, true);
        ImmutableArray<AttributeData> attributes = symbol.GetAttributes();
        return (
            containingBody || attributes.Any(x => x.AttributeClass?.ToDisplayString() == "PER.Abstractions.Meta.RequiresBodyAttribute"),
            containingHead || attributes.Any(x => x.AttributeClass?.ToDisplayString() == "PER.Abstractions.Meta.RequiresHeadAttribute")
        );
    }

    private static ((bool used, bool invalid) body, (bool used, bool invalid) head) VerifyRequires(ISymbol? symbol) {
        if (symbol is null)
            return ((false, false), (false, false));
        ImmutableArray<AttributeData> attributes = symbol.GetAttributes();
        ((bool used, bool invalid) body, (bool used, bool invalid) head) current = (
            (
                attributes.Any(x =>
                    x.AttributeClass?.ToDisplayString() == "PER.Abstractions.Meta.RequiresBodyAttribute"),
                false
            ),
            (
                attributes.Any(x =>
                    x.AttributeClass?.ToDisplayString() == "PER.Abstractions.Meta.RequiresHeadAttribute"),
                false
            )
        );
        if (symbol.ContainingSymbol is not null && !current.body.used && !current.head.used) {
            (bool containingBody, bool containingHead) = GetRequires(symbol.ContainingSymbol);
            current.body.used = current.body.used || containingBody;
            current.head.used = current.head.used || containingHead;
        }

        if (symbol is not IMethodSymbol method)
            return current;
        if (method.OverriddenMethod is not null && MergeOverridden(GetRequires(method.OverriddenMethod)))
            return current;
        if (method.ExplicitInterfaceImplementations.Any(x => MergeOverridden(GetRequires(x))))
            return current;
        _ = method.ContainingSymbol switch {
            ITypeSymbol type => type.AllInterfaces
                .SelectMany(x => x.GetMembers(method.Name)
                    .OfType<IMethodSymbol>()
                    .Where(y => y.Parameters
                        .Zip(method.Parameters, (a, b) => SymbolEqualityComparer.Default.Equals(a, b))
                        .All(z => z)))
                .Any(x => MergeOverridden(GetRequires(x))),
            IPropertySymbol property => (property.ContainingSymbol as ITypeSymbol)?.AllInterfaces
                .SelectMany(x => x.GetMembers(property.Name)
                    .OfType<IPropertySymbol>()
                    .Select(y => SymbolEqualityComparer.Default.Equals(property.GetMethod, method) ?
                        y.GetMethod : y.SetMethod)
                    .OfType<IMethodSymbol>())
                .Any(x => MergeOverridden(GetRequires(x))) ?? false,
            IEventSymbol e => (e.ContainingSymbol as ITypeSymbol)?.AllInterfaces
                .SelectMany(x => x.GetMembers(e.Name)
                    .OfType<IEventSymbol>()
                    .Select(y => SymbolEqualityComparer.Default.Equals(e.AddMethod, method) ? y.AddMethod :
                        SymbolEqualityComparer.Default.Equals(e.RemoveMethod, method) ? y.RemoveMethod :
                        y.RaiseMethod)
                    .OfType<IMethodSymbol>())
                .Any(x => MergeOverridden(GetRequires(x))) ?? false,
            _ => false
        };

        return current;

        bool MergeOverridden((bool body, bool head) overridden) {
            if (overridden.body)
                current.body.used = true;
            else if (current.body.used)
                current.body.invalid = true;
            if (overridden.head)
                current.head.used = true;
            else if (current.head.used)
                current.head.invalid = true;
            return current is { head: { used: true, invalid: true }, body: { used: true, invalid: true } };
        }
    }

    public static ((bool used, bool invalid) body, (bool used, bool invalid) head) VerifyRequires(SemanticModel semanticModel, ISymbol? symbol) {
        ((bool used, bool invalid) body, (bool used, bool invalid) head) current = ((false, false), (false, false));

        switch (symbol) {
            case IMethodSymbol method:
                _ = method.DeclaringSyntaxReferences
                    .Select(x => x.GetSyntax())
                    .OfType<MethodDeclarationSyntax>()
                    .Select(x => x.Body as SyntaxNode ?? x.ExpressionBody)
                    .Any(body => body is not null && semanticModel.GetOperation(body) is { } operation &&
                        FindInvocations(operation).Any(x => MergeRecursion(VerifyRequires(x.Item2))));
                break;
            case IPropertySymbol property:
                if (MergeRecursion(VerifyRequires(semanticModel, property.GetMethod)))
                    break;
                MergeRecursion(VerifyRequires(semanticModel, property.SetMethod));
                break;
            case IEventSymbol e:
                if (MergeRecursion(VerifyRequires(semanticModel, e.AddMethod)))
                    break;
                if (MergeRecursion(VerifyRequires(semanticModel, e.RemoveMethod)))
                    break;
                MergeRecursion(VerifyRequires(semanticModel, e.RaiseMethod));
                break;
            case ITypeSymbol type:
                type.GetMembers().Any(x => MergeRecursion(VerifyRequires(semanticModel, x)));
                break;
        }

        return current;

        bool MergeRecursion(((bool used, bool invalid) body, (bool used, bool invalid) head) child) {
            if (child.body.used)
                current.body.used = true;
            if (child.body.invalid)
                current.body.invalid = true;
            if (child.head.used)
                current.head.used = true;
            if (child.head.invalid)
                current.head.invalid = true;
            return current is { head: { used: true, invalid: true }, body: { used: true, invalid: true } };
        }
    }
}
