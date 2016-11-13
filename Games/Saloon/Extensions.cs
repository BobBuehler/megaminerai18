using System;
using System.Collections.Generic;

static class Extensions
{
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var s in source)
        {
            action(s);
        }
    }

    public static T MinByValue<T, K>(this IEnumerable<T> source, Func<T, K> selector)
    {
        var comparer = Comparer<K>.Default;

        var enumerator = source.GetEnumerator();
        enumerator.MoveNext();

        var min = enumerator.Current;
        var minV = selector(min);

        while (enumerator.MoveNext())
        {
            var s = enumerator.Current;
            var v = selector(s);
            if (comparer.Compare(v, minV) < 0)
            {
                min = s;
                minV = v;
            }
        }
        return min;
    }

    public static T MaxByValue<T, K>(this IEnumerable<T> source, Func<T, K> selector)
    {
        var comparer = Comparer<K>.Default;

        var enumerator = source.GetEnumerator();
        enumerator.MoveNext();

        var max = enumerator.Current;
        var maxV = selector(max);

        while (enumerator.MoveNext())
        {
            var s = enumerator.Current;
            var v = selector(s);
            if (comparer.Compare(v, maxV) > 0)
            {
                max = s;
                maxV = v;
            }
        }
        return max;
    }

    public static IEnumerable<T> While<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        var enumerator = source.GetEnumerator();
        while (enumerator.MoveNext() && predicate(enumerator.Current))
        {
            yield return enumerator.Current;
        }
    }

    public static Func<T, TResult> Memoize<T, TResult>(this Func<T, TResult> func, IDictionary<T, TResult> cache = null)
    {
        cache = cache ?? new Dictionary<T, TResult>();
        return t =>
        {
            TResult result;
            if (!cache.TryGetValue(t, out result))
            {
                result = func(t);
                cache[t] = result;
            }
            return result;
        };
    }

    public static Func<T, TResult> LRUMemoize<T, TResult>(this Func<T, TResult> func, int capacity)
    {
        var cache = new LRUCache<T, TResult>(capacity);
        cache.OnMiss = delegate (T input, out TResult output) { output = func(input); return true; };
        return t => cache[t];
    }

    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
    {
        return new HashSet<T>(source);
    }
}