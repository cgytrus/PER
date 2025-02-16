using System;
using System.Collections.Generic;

namespace PER.Analyzers;

public static class IEnumerableExtensions {
    public static IEnumerable<T> Flatten<T>(this IEnumerable<T> collection, Func<T, IEnumerable<T>> selector) {
        foreach (T x in collection) {
            yield return x;
            foreach (T y in Flatten(selector(x), selector)) {
                yield return y;
            }
        }
    }
}
