namespace Xiangyao.ZLinq;

/// <summary>
/// Zero-allocation LINQ alternatives optimized for performance-critical paths.
/// Provides span-based and allocation-free implementations of common LINQ operations.
/// </summary>
public static class ZLinqExtensions
{
    /// <summary>
    /// Zero-allocation FirstOrDefault implementation for arrays with predicate.
    /// </summary>
    public static T? FirstOrDefault<T>(this T[] source, Func<T, bool> predicate)
    {
        for (int i = 0; i < source.Length; i++)
        {
            if (predicate(source[i]))
            {
                return source[i];
            }
        }
        return default;
    }

    /// <summary>
    /// Zero-allocation FirstOrDefault implementation for ReadOnlySpan with predicate.
    /// </summary>
    public static T? FirstOrDefault<T>(this ReadOnlySpan<T> source, Func<T, bool> predicate)
    {
        for (int i = 0; i < source.Length; i++)
        {
            if (predicate(source[i]))
            {
                return source[i];
            }
        }
        return default;
    }

    /// <summary>
    /// Zero-allocation FirstOrDefault implementation for arrays.
    /// </summary>
    public static T? FirstOrDefault<T>(this T[] source)
    {
        return source.Length > 0 ? source[0] : default;
    }

    /// <summary>
    /// Zero-allocation FirstOrDefault implementation for ReadOnlySpan.
    /// </summary>
    public static T? FirstOrDefault<T>(this ReadOnlySpan<T> source)
    {
        return source.Length > 0 ? source[0] : default;
    }

    /// <summary>
    /// Optimized Where operation that filters in-place using a pre-allocated buffer.
    /// Returns the number of items that passed the filter.
    /// </summary>
    public static int WhereInPlace<T>(this T[] source, Func<T, bool> predicate, T[] buffer)
    {
        int count = 0;
        for (int i = 0; i < source.Length && count < buffer.Length; i++)
        {
            if (predicate(source[i]))
            {
                buffer[count++] = source[i];
            }
        }
        return count;
    }

    /// <summary>
    /// Zero-allocation Any implementation for arrays with predicate.
    /// </summary>
    public static bool Any<T>(this T[] source, Func<T, bool> predicate)
    {
        for (int i = 0; i < source.Length; i++)
        {
            if (predicate(source[i]))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Zero-allocation Any implementation for ReadOnlySpan with predicate.
    /// </summary>
    public static bool Any<T>(this ReadOnlySpan<T> source, Func<T, bool> predicate)
    {
        for (int i = 0; i < source.Length; i++)
        {
            if (predicate(source[i]))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Zero-allocation Count implementation for arrays with predicate.
    /// </summary>
    public static int Count<T>(this T[] source, Func<T, bool> predicate)
    {
        int count = 0;
        for (int i = 0; i < source.Length; i++)
        {
            if (predicate(source[i]))
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Zero-allocation Count implementation for ReadOnlySpan with predicate.
    /// </summary>
    public static int Count<T>(this ReadOnlySpan<T> source, Func<T, bool> predicate)
    {
        int count = 0;
        for (int i = 0; i < source.Length; i++)
        {
            if (predicate(source[i]))
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Optimized Select operation that transforms items in-place using a pre-allocated buffer.
    /// Returns the number of items processed (same as source length).
    /// </summary>
    public static int SelectInPlace<TSource, TResult>(this TSource[] source, Func<TSource, TResult> selector, TResult[] buffer)
    {
        int count = Math.Min(source.Length, buffer.Length);
        for (int i = 0; i < count; i++)
        {
            buffer[i] = selector(source[i]);
        }
        return count;
    }
}