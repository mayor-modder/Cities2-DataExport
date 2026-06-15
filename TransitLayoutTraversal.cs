using System;
using System.Collections.Generic;

namespace CS2DataExport;

internal static class TransitLayoutTraversal
{
    public static int SumLeafValues<T>(
        T root,
        Func<T, IEnumerable<T>> childSelector,
        Func<T, int> leafValueSelector,
        IEqualityComparer<T>? comparer = null)
        where T : notnull
    {
        if (childSelector == null)
        {
            throw new ArgumentNullException(nameof(childSelector));
        }

        if (leafValueSelector == null)
        {
            throw new ArgumentNullException(nameof(leafValueSelector));
        }

        var visited = new HashSet<T>(comparer);
        return SumLeafValuesCore(root, childSelector, leafValueSelector, visited);
    }

    private static int SumLeafValuesCore<T>(
        T node,
        Func<T, IEnumerable<T>> childSelector,
        Func<T, int> leafValueSelector,
        HashSet<T> visited)
        where T : notnull
    {
        if (!visited.Add(node))
        {
            return 0;
        }

        IEnumerable<T>? children = childSelector(node);
        if (children == null)
        {
            return leafValueSelector(node);
        }

        int total = 0;
        bool hasChildren = false;
        foreach (T child in children)
        {
            hasChildren = true;
            total += SumLeafValuesCore(child, childSelector, leafValueSelector, visited);
        }

        return hasChildren ? total : leafValueSelector(node);
    }
}
