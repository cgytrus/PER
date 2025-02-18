using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace PER.Analyzers;

using Reqs = IEnumerable<Util.BodyHeadSource>;

public static class Util {
    private static bool IsRequiresBody(AttributeData x) =>
        x.AttributeClass?.ToDisplayString() == "PER.Abstractions.Meta.RequiresBodyAttribute";

    private static bool IsRequiresHead(AttributeData x) =>
        x.AttributeClass?.ToDisplayString() == "PER.Abstractions.Meta.RequiresHeadAttribute";

    public static IEnumerable<(IOperation, ISymbol)> FindInvocations(IOperation operation) => operation.ChildOperations
        .Flatten(x => x is IMethodBodyOperation ? Array.Empty<IOperation>() : x.ChildOperations)
        .Where(x => x is IInvocationOperation or IPropertyReferenceOperation or IEventReferenceOperation or
            IObjectCreationOperation { Constructor: not null } or IObjectCreationOperation { Type: not null })
        .Select<IOperation, (IOperation, ISymbol)>(x => (x, x switch {
            IInvocationOperation invocation => invocation.TargetMethod,
            IPropertyReferenceOperation property => property.Property,
            IEventReferenceOperation e => e.Event,
            IObjectCreationOperation { Constructor: not null } creation => creation.Constructor!,
            IObjectCreationOperation { Type: not null } creation => creation.Type!,
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
            case ITypeSymbol type:
                return type.AllInterfaces;
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

    private static ISymbol? GetOverriddenSymbol(ISymbol? symbol) {
        return symbol switch {
            IMethodSymbol { OverriddenMethod: not null } sym => sym.OverriddenMethod,
            IPropertySymbol { OverriddenProperty: not null } sym => sym.OverriddenProperty,
            IEventSymbol { OverriddenEvent: not null } sym => sym.OverriddenEvent,
            ITypeSymbol { BaseType: not null } sym => sym.BaseType,
            _ => null
        };
    }

    public readonly struct BodyHeadSource(ISymbol symbol, AttributeData attr) {
        public ISymbol symbol { get; } = symbol;
        public Location? location { get; } = attr.ApplicationSyntaxReference?.GetSyntax().GetLocation();
    }

    public static (BodyHeadSource? body, BodyHeadSource? head) GetRequiresSelf(ISymbol symbol) {
        ImmutableArray<AttributeData> attributes = symbol.GetAttributes();
        AttributeData? body = attributes.FirstOrDefault(IsRequiresBody);
        AttributeData? head = attributes.FirstOrDefault(IsRequiresHead);
        return (
            body is null ? null : new BodyHeadSource(symbol, body),
            head is null ? null : new BodyHeadSource(symbol, head)
        );
    }

    public static (Reqs bodies, Reqs heads, (Reqs bodies, Reqs heads) invalid) GetRequires(ISymbol symbol) {
        Reqs bodies = [];
        Reqs heads = [];
        (Reqs bodies, Reqs heads) invalid = ([], []);

        bool usedInherited = false;

        ISymbol? overriddenSymbol = GetOverriddenSymbol(symbol);
        if (overriddenSymbol is not null) {
            (Reqs bodies, Reqs heads, (Reqs bodies, Reqs heads) invalid) inherited = GetRequires(overriddenSymbol);
            bodies = bodies.Concat(inherited.bodies);
            heads = heads.Concat(inherited.heads);
            invalid.bodies = invalid.bodies.Concat(inherited.invalid.bodies);
            invalid.heads = invalid.heads.Concat(inherited.invalid.heads);
            usedInherited = true;
        }
        foreach ((Reqs bodies, Reqs heads, (Reqs bodies, Reqs heads) invalid) inherited in
            AllInterfaceDeclarations(symbol).Select(GetRequires)) {
            bodies = bodies.Concat(inherited.bodies);
            heads = heads.Concat(inherited.heads);
            invalid.bodies = invalid.bodies.Concat(inherited.invalid.bodies);
            invalid.heads = invalid.heads.Concat(inherited.invalid.heads);
            usedInherited = true;
        }

        (BodyHeadSource? body, BodyHeadSource? head) = GetRequiresSelf(symbol);

        if (symbol is ITypeSymbol) {
            if (body is not null)
                bodies = bodies.Append(body.Value);
            if (head is not null)
                heads = heads.Append(head.Value);
            return (bodies, heads, invalid);
        }

        if (usedInherited) {
            // types allow overwriting requires state because it wouldn't let you create
            // an instance of the type without the requires state already being valid in the first place
            // as opposed to methods where you can create the instance somewhere without the required state
            // then call the method through casting to a base type that doesnt have the body/head requirement
            // (with types theres nothing to cast before calling the constructor)
            // TODO: report PER0003 if body or head is not null
            if (body is not null)
                invalid.bodies = invalid.bodies.Append(body.Value);
            if (head is not null)
                invalid.heads = invalid.heads.Append(head.Value);
            return (bodies, heads, invalid);
        }

        (bool bodies, bool heads) usedContaining = (false, false);
        if (symbol.ContainingSymbol is not null) {
            (Reqs bodies, Reqs heads, (Reqs bodies, Reqs heads) invalid) containing =
                GetRequires(symbol.ContainingSymbol);
            bodies = bodies.Concat(containing.bodies);
            heads = heads.Concat(containing.heads);
            invalid.bodies = invalid.bodies.Concat(containing.invalid.bodies);
            invalid.heads = invalid.heads.Concat(containing.invalid.heads);
            if (!usedContaining.bodies && containing.bodies.Any())
                usedContaining.bodies = true;
            if (!usedContaining.heads && containing.heads.Any())
                usedContaining.heads = true;
        }

        if (!usedContaining.bodies && body is not null)
            bodies = bodies.Append(body.Value);
        if (!usedContaining.heads && head is not null)
            heads = heads.Append(head.Value);
        return (bodies, heads, invalid);
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
