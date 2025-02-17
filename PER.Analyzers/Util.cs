using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace PER.Analyzers;

public static class Util {
    private static bool IsRequiresBody(AttributeData x) =>
        x.AttributeClass?.ToDisplayString() == "PER.Abstractions.Meta.RequiresBodyAttribute";

    private static bool IsRequiresHead(AttributeData x) =>
        x.AttributeClass?.ToDisplayString() == "PER.Abstractions.Meta.RequiresHeadAttribute";

    public static IEnumerable<(IOperation, ISymbol)> FindInvocations(IOperation operation) => operation.ChildOperations
        .Flatten(x => x is IMethodBodyOperation ? Array.Empty<IOperation>() : x.ChildOperations)
        .Where(x => x is IInvocationOperation or IPropertyReferenceOperation or IEventReferenceOperation)
        .Select<IOperation, (IOperation, ISymbol)>(x => (x, x switch {
            IInvocationOperation invocation => invocation.TargetMethod,
            IPropertyReferenceOperation property => property.Property,
            IEventReferenceOperation e => e.Event,
            _ => throw new ArgumentOutOfRangeException(nameof(x))
        }));

    private static IEnumerable<ISymbol> AllInterfaceDeclarations(ISymbol symbol) {
        switch (symbol) {
            case IMethodSymbol method:
                if (method.ExplicitInterfaceImplementations.Length > 0)
                    return method.ExplicitInterfaceImplementations;
                break;
            case IPropertySymbol property:
                if (property.ExplicitInterfaceImplementations.Length > 0)
                    return property.ExplicitInterfaceImplementations;
                break;
            case IEventSymbol e:
                if (e.ExplicitInterfaceImplementations.Length > 0)
                    return e.ExplicitInterfaceImplementations;
                break;
            default:
                return Array.Empty<ISymbol>();
        }
        if (symbol is not { DeclaredAccessibility: Accessibility.Public,
            ContainingType.TypeKind: TypeKind.Class or TypeKind.Struct })
            return Array.Empty<ISymbol>();
        return symbol.ContainingType.AllInterfaces
            .Where(x => {
                string? nameToLookFor = symbol is IMethodSymbol {
                    MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet or
                    MethodKind.EventAdd or MethodKind.EventRemove or MethodKind.EventRaise
                } method ? method.AssociatedSymbol?.Name : symbol.Name;
                return nameToLookFor is not null && x.MemberNames.Contains(nameToLookFor);
            })
            .SelectMany(x => x.GetMembers(symbol.Name)
                .Where(y => SymbolEqualityComparer.Default.Equals(
                    symbol.ContainingType.FindImplementationForInterfaceMember(y), symbol))
            )
            .Distinct(SymbolEqualityComparer.Default);
    }

    public static ((ISymbol, Location?)? body, (ISymbol, Location?)? head) GetRequiresSelf(ISymbol symbol) {
        ImmutableArray<AttributeData> attributes = symbol.GetAttributes();
        AttributeData? body = attributes.FirstOrDefault(IsRequiresBody);
        AttributeData? head = attributes.FirstOrDefault(IsRequiresHead);
        return (
            body is null ? null : (symbol, body.ApplicationSyntaxReference?.GetSyntax().GetLocation()),
            head is null ? null : (symbol, head.ApplicationSyntaxReference?.GetSyntax().GetLocation())
        );
    }

    public static ((ISymbol, Location?)? body, (ISymbol, Location?)? head) GetRequires(ISymbol symbol) {
        // go up overrides
        while (symbol is IMethodSymbol { OverriddenMethod: not null } overrideMethod)
            symbol = overrideMethod.OverriddenMethod;
        while (symbol is IPropertySymbol { OverriddenProperty: not null } overrideProperty)
            symbol = overrideProperty.OverriddenProperty;
        while (symbol is IEventSymbol { OverriddenEvent: not null } overrideEvent)
            symbol = overrideEvent.OverriddenEvent;

        // then up interface to the interface declaration
        while (symbol is IMethodSymbol or IPropertySymbol or IEventSymbol) {
            ISymbol? impl = AllInterfaceDeclarations(symbol).FirstOrDefault();
            if (impl is null)
                break;
            symbol = impl;
        }

        ((ISymbol, Location?)? containingBody, (ISymbol, Location?)? containingHead) = symbol.ContainingSymbol is null ?
            (null, null) : GetRequires(symbol.ContainingSymbol);
        if (containingBody is not null && containingHead is not null)
            return (containingBody, containingHead);

        ((ISymbol, Location?)? body, (ISymbol, Location?)? head) = GetRequiresSelf(symbol);
        return (containingBody ?? body, containingHead ?? head);
    }

    public static IEnumerable<IMethodSymbol> GetMethods(ISymbol? symbol) {
        switch (symbol) {
            case IMethodSymbol method:
                yield return method;
                break;
            case IPropertySymbol property:
                if (property.GetMethod is not null)
                    yield return property.GetMethod;
                if (property.SetMethod is not null)
                    yield return property.SetMethod;
                break;
            case IEventSymbol e:
                if (e.AddMethod is not null)
                    yield return e.AddMethod;
                if (e.RemoveMethod is not null)
                    yield return e.RemoveMethod;
                if (e.RaiseMethod is not null)
                    yield return e.RaiseMethod;
                break;
        }
    }
}
