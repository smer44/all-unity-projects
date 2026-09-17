using System;
using System.Collections.Generic;

public static class ListYUtil 
{

    public static Dictionary<T, int> ToIndexDictionary<T>(T[] items)
    {
        var dict = new Dictionary<T, int>(items.Length);

        for (int i = 0; i < items.Length; i++)
        {
            var key = items[i] ?? throw new ArgumentException("List contains null at index " + i, nameof(items));
            if (!dict.TryAdd(key, i))
                throw new ArgumentException($"Duplicate key encountered at index {i}.", nameof(items));
        }

        return dict;
    }



    /// <summary>
    /// Returns:
    ///   (first minus items also in second) + (items in second that are not in first)
    /// Preserves the relative order of kept items from <paramref name="first"/>,
    /// then appends new items from <paramref name="second"/> in their order.
    /// </summary>
    public static string[] SwapBySecondArray(string[] first, string[] second, StringComparer comparer = null)
    {
        comparer ??= StringComparer.Ordinal;

        first  ??= Array.Empty<string>();
        second ??= Array.Empty<string>();

        var secondSet = new HashSet<string>(second, comparer);
        var firstSet  = new HashSet<string>(first, comparer);

        var result = new List<string>(first.Length + second.Length);

        // Keep items from first that are NOT present in second
        foreach (var s in first)
        {
            if (!secondSet.Contains(s))
                result.Add(s);
        }

        // Append items from second that were NOT in first (avoid duplicates from second)
        var added = new HashSet<string>(comparer);
        foreach (var s in second)
        {
            if (!firstSet.Contains(s) && added.Add(s))
                result.Add(s);
        }

        return result.ToArray();
    }


    /// <summary>
    /// Returns true if both arrays contain the same unique strings, regardless of order.
    /// Assumes each item appears at most once within an array.
    /// </summary>
    public static bool UnorderedEqual(string[] a, string[] b, IEqualityComparer<string> comparer = null)
    {
        comparer ??= StringComparer.Ordinal;

        var set = new HashSet<string>(a, comparer);
        if (set.Count != a.Length) throw new ArgumentException("Array 'a' contains duplicate items.");

        // Since lengths are equal and items are unique, set equality reduces to: all b items in set.
        for (int i = 0; i < b.Length; i++)
        {
            if (!set.Contains(b[i])) return false;
        }

        return true;
    }    

}
