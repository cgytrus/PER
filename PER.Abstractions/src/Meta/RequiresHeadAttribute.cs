using System;

namespace PER.Abstractions.Meta;

[AttributeUsage(AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Constructor |
    AttributeTargets.Method |
    AttributeTargets.Property |
    AttributeTargets.Event |
    AttributeTargets.Interface, Inherited = false)]
public class RequiresHeadAttribute : Attribute;
